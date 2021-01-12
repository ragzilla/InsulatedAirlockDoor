/*
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

using PeterHan.PLib.Buildings;
using UnityEngine;
using TUNING;

namespace InsulatedAirlockDoor {
	/// <summary>
	/// An airlock door that requires power, but allows Duplicants to pass without ever
	/// transmitting liquid or gas (unless set to Open).
	/// </summary>
	public sealed class InsulatedAirlockDoorConfig : IBuildingConfig {
		public const string ID = "InsulatedAirlockDoor";

		/// <summary>
		/// The completed building template.
		/// </summary>
		internal static PBuilding InsulatedAirlockDoorTemplate;

		/// <summary>
		/// Registers this building.
		/// </summary>
		internal static void RegisterBuilding() {
			// Inititialize it here to allow localization to change the strings
			PBuilding.Register(InsulatedAirlockDoorTemplate = new PBuilding(ID,
					InsulatedAirlockDoorStrings.BUILDINGS.PREFABS.INSULATEDAIRLOCKDOOR.NAME) {
				AddAfter = PressureDoorConfig.ID,
				Animation = "insulated_airlock_door_kanim",
				Category = "Base",
				ConstructionTime = 60.0f,
				Decor = TUNING.BUILDINGS.DECOR.PENALTY.TIER1,
				Description = null, EffectText = null,
				Entombs = false,
				Floods = false,
				Height = 2,
				HP = 30,
				LogicIO = {
					LogicPorts.Port.InputPort(InsulatedAirlockDoor.OPEN_CLOSE_PORT_ID, CellOffset.none,
						InsulatedAirlockDoorStrings.BUILDINGS.PREFABS.INSULATEDAIRLOCKDOOR.LOGIC_OPEN,
						InsulatedAirlockDoorStrings.BUILDINGS.PREFABS.INSULATEDAIRLOCKDOOR.LOGIC_OPEN_ACTIVE,
						InsulatedAirlockDoorStrings.BUILDINGS.PREFABS.INSULATEDAIRLOCKDOOR.LOGIC_OPEN_INACTIVE)
				},
				Ingredients = {
					new BuildIngredient(TUNING.MATERIALS.BUILDABLERAW, tier: 7),
					new BuildIngredient(TUNING.MATERIALS.REFINED_METAL, tier: 4),
				},
				// Overheating is not possible on solid tile buildings because they bypass
				// structure temperatures so sim will never send the overheat notification
				Placement = BuildLocationRule.Tile,
				PowerInput = new PowerRequirement(120.0f, new CellOffset(0, 0)),
				RotateMode = PermittedRotations.Unrotatable,
				SceneLayer = Grid.SceneLayer.InteriorWall,
				Tech = "HVAC",
				Width = 3
			});
		}

		public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag) {
			base.ConfigureBuildingTemplate(go, prefab_tag);
			InsulatedAirlockDoorTemplate?.ConfigureBuildingTemplate(go);
		}

		public override BuildingDef CreateBuildingDef() {
			var def = InsulatedAirlockDoorTemplate?.CreateDef();
			def.ForegroundLayer = Grid.SceneLayer.TileMain;
			def.IsFoundation = true;
			def.PreventIdleTraversalPastBuilding = true;
			// /100 multiplier to thermal conductivity
			def.ThermalConductivity = 0.01f;
			def.TileLayer = ObjectLayer.FoundationTile;
			return def;
		}

		public override void DoPostConfigureUnderConstruction(GameObject go) {
			InsulatedAirlockDoorTemplate?.CreateLogicPorts(go);
		}

		public override void DoPostConfigurePreview(BuildingDef def, GameObject go) {
			InsulatedAirlockDoorTemplate?.CreateLogicPorts(go);
		}

		public override void DoPostConfigureComplete(GameObject go) {
			InsulatedAirlockDoorTemplate?.DoPostConfigureComplete(go);
			InsulatedAirlockDoorTemplate?.CreateLogicPorts(go);
			var ad = go.AddOrGet<InsulatedAirlockDoor>();
			ad.EnergyCapacity = 10000.0f;
			ad.EnergyPerUse = 2000.0f;
			var occupier = go.AddOrGet<SimCellOccupier>();
			occupier.doReplaceElement = true;
			occupier.notifyOnMelt = true;
			// go.AddOrGet<Insulator>();
			go.AddOrGet<TileTemperature>();
			go.AddOrGet<AccessControl>().controlEnabled = true;
			go.AddOrGet<KBoxCollider2D>();
			go.AddOrGet<BuildingHP>().destroyOnDamaged = true;
			Prioritizable.AddRef(go);
			go.AddOrGet<CopyBuildingSettings>().copyGroupTag = GameTags.Door;
			go.AddOrGet<Workable>().workTime = 3f;
			Object.DestroyImmediate(go.GetComponent<BuildingEnabledButton>());
			go.GetComponent<KBatchedAnimController>().initialAnim = "closed";
		}
	}
}
