﻿/*
 * Copyright 2021 Matt Addison
 * Copyright 2020 Peter Han
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software
 * and associated documentation files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or
 * substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
 * BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using PeterHan.PLib;
using System.Collections.Generic;

namespace InsulatedAirlockDoor {
	/// <summary>
	/// Handles Duplicant navigation through an airlock door.
	/// </summary>
	public sealed class InsulatedAirlockDoorTransitionLayer : TransitionDriver.OverrideLayer {
		/// <summary>
		/// The doors to be opened.
		/// </summary>
		private readonly IDictionary<InsulatedAirlockDoor, DoorRequestType> doors;

		public InsulatedAirlockDoorTransitionLayer(Navigator navigator) : base(navigator) {
			doors = new Dictionary<InsulatedAirlockDoor, DoorRequestType>(4);
		}

		/// <summary>
		/// Adds a door if it is present in this cell.
		/// </summary>
		/// <param name="doorCell">The cell to check for the door.</param>
		/// <param name="navCell">The cell of the Duplicant navigating the door.</param>
		private void AddDoor(int doorCell, int navCell) {
			if (Grid.HasDoor[doorCell]) {
				var door = Grid.Objects[doorCell, (int)ObjectLayer.Building].
					GetComponentSafe<InsulatedAirlockDoor>();
				if (door != null && door.isSpawned && !doors.ContainsKey(door))
					RequestOpenDoor(door, doorCell, navCell);
			}
		}

		/// <summary>
		/// For each door, checks to see if all are in a state where the Duplicant may pass.
		/// </summary>
		/// <returns>true if doors are open, or false otherwise.</returns>
		private bool AreAllDoorsOpen() {
			bool open = true;
			InsulatedAirlockDoor door;
			foreach (var pair in doors)
				if ((door = pair.Key) != null) {
					switch (pair.Value) {
					case DoorRequestType.EnterLeft:
					case DoorRequestType.ExitLeft:
						if (!door.IsLeftOpen) {
							open = false;
							break;
						}
						break;
					case DoorRequestType.EnterRight:
					case DoorRequestType.ExitRight:
						if (!door.IsRightOpen) {
							open = false;
							break;
						}
						break;
					}
				}
			return open;
		}

		public override void BeginTransition(Navigator navigator, Navigator.
				ActiveTransition transition) {
			base.BeginTransition(navigator, transition);
			int cell = Grid.PosToCell(navigator);
			int targetCell = Grid.OffsetCell(cell, transition.x, transition.y);
			ClearTransitions();
			AddDoor(targetCell, cell);
			// If duplicant is inside a tube they are only 1 cell tall
			if (navigator.CurrentNavType != NavType.Tube)
				AddDoor(Grid.CellAbove(targetCell), cell);
			// Include any other offsets
			foreach (var offset in transition.navGridTransition.voidOffsets)
				AddDoor(Grid.OffsetCell(cell, offset), cell);
			// If not open, start a transition with the dupe waiting for the door
			if (doors.Count > 0 && !AreAllDoorsOpen()) {
				transition.anim = navigator.NavGrid.GetIdleAnim(navigator.CurrentNavType);
				transition.isLooping = false;
				transition.end = transition.start;
				transition.speed = 1.0f;
				transition.animSpeed = 1.0f;
				transition.x = 0;
				transition.y = 0;
				transition.isCompleteCB = AreAllDoorsOpen;
			}
		}

		/// <summary>
		/// Clears all pending transitions.
		/// </summary>
		private void ClearTransitions() {
			InsulatedAirlockDoor door;
			foreach (var pair in doors)
				if ((door = pair.Key) != null) {
					switch (pair.Value) {
					case DoorRequestType.EnterLeft:
						door.EnterLeft?.Finish();
						break;
					case DoorRequestType.EnterRight:
						door.EnterRight?.Finish();
						break;
					case DoorRequestType.ExitLeft:
						door.ExitLeft?.Finish();
						break;
					case DoorRequestType.ExitRight:
						door.ExitRight?.Finish();
						break;
					}
				}
			doors.Clear();
		}

		public override void Destroy() {
			base.Destroy();
			ClearTransitions();
		}

		public override void EndTransition(Navigator navigator, Navigator.
				ActiveTransition transition) {
			base.EndTransition(navigator, transition);
			ClearTransitions();
		}

		/// <summary>
		/// Requests a door to open if necessary.
		/// </summary>
		/// <param name="door">The door that is being traversed.</param>
		/// <param name="doorCell">The cell where the navigator is moving.</param>
		/// <param name="navCell">The cell where the navigator is standing now.</param>
		private void RequestOpenDoor(InsulatedAirlockDoor door, int doorCell, int navCell) {
			int baseCell = door.GetBaseCell(), dx;
			// Based on coordinates, determine what is required of the door
			CellOffset targetOffset = Grid.GetOffset(baseCell, doorCell), navOffset =
				Grid.GetOffset(doorCell, navCell);
			dx = targetOffset.x;
			if (dx > 0) {
				// Right side door
				if (navOffset.x > 0) {
					doors.Add(door, DoorRequestType.EnterRight);
					door.EnterRight?.Queue();
				} else {
					doors.Add(door, DoorRequestType.ExitRight);
					door.ExitRight?.Queue();
				}
			} else if (dx < 0) {
				// Left side door
				if (navOffset.x > 0) {
					doors.Add(door, DoorRequestType.ExitLeft);
					door.ExitLeft?.Queue();
				} else {
					doors.Add(door, DoorRequestType.EnterLeft);
					door.EnterLeft?.Queue();
				}
			} // Else, entering center cell which is "always" passable
		}

		/// <summary>
		/// The types of requests that can be made of an airlock door.
		/// </summary>
		private enum DoorRequestType {
			EnterLeft, EnterRight, ExitLeft, ExitRight
		}
	}
}
