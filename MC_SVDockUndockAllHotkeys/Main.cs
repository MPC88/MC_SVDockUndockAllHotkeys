
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace MC_SVDockUndockAllHotkeys
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string pluginGuid = "mc.starvalor.fleethotkeys";
        public const string pluginName = "SV Fleet Hotkeys";
        public const string pluginVersion = "1.0.1";

        private static ConfigEntry<KeyCodeSubset> cfgModifier;
        private static ConfigEntry<KeyCodeSubset> cfgDockAll;
        private static ConfigEntry<KeyCodeSubset> cfgUndockAll;
        private static ConfigEntry<KeyCodeSubset> cfgDumpAll;

        private static List<int> droneEquipIDs;

        public void Awake()
        {
            cfgDockAll = Config.Bind("Keybinds",
                "1. Dock all",
                KeyCodeSubset.D,
                "Key to dock all.  If Modifier key is set, must be pressed with that key.");
            cfgUndockAll = Config.Bind("Keybinds",
                "2. Undock all",
                KeyCodeSubset.A,
                "Key to undock all.  If Modifier key is set, must be pressed with that key.");
            cfgDumpAll = Config.Bind("Keybinds",
                "3. Cargo to station",
                KeyCodeSubset.S,
                "Command fleet to drop cargo to station.");
            cfgModifier = Config.Bind("Keybinds",
                "4. Modifier key",
                KeyCodeSubset.LeftAlt,
                "Set to \"None\" to disable modifer key.");
        }

        public void Update()
        {
            if (GameManager.instance == null || !GameManager.instance.inGame || FleetControl.instance == null)
                return;

            if (cfgModifier.Value == KeyCodeSubset.None || Input.GetKey((KeyCode)cfgModifier.Value))
            {
                if (Input.GetKeyDown((KeyCode)cfgDockAll.Value))
                    FleetControl.instance.DockFleet(true);
                if (Input.GetKeyDown((KeyCode)cfgUndockAll.Value))
                    FleetControl.instance.LaunchFleet();
                if (Input.GetKeyDown((KeyCode)cfgDumpAll.Value))
                    FleetControl.instance.UnloadFleetCargo();
            }
        }

        [HarmonyPatch(typeof(FleetControl), nameof(FleetControl.UnloadFleetCargo))]
        [HarmonyPrefix]
        private static void UnloadFleetCargo_Pre(FleetControl __instance, Transform ___shipsTrans, CargoSystem ___playerCS)
        {
            if (!(__instance.pc != null && PChar.Char.GetFleetSize > 0))
                return;

            if (!Inventory.instance.inStation)
            {
                InfoPanelControl.inst.ShowWarning(Lang.Get(0, 385), 4, false);
                return;
            }

            DropCargo(___shipsTrans, ___playerCS);
            DropPassengers(___shipsTrans, ___playerCS);
        }

        private static void DropCargo(Transform shipsTrans, CargoSystem playerCS)
        {
            Station currStation = Inventory.instance.currStation;
            int num = 0;
            for (int i = 0; i < shipsTrans.childCount; i++)
            {
                AIMercenaryCharacter aiMercChar = shipsTrans.GetChild(i).GetComponent<FleetMemberSlot>().aiMercChar;

                if (aiMercChar.hangarDocked || aiMercChar.dockedStationID == currStation.id)
                {
                    List<CargoItem> cargo = aiMercChar.shipData.cargo;
                    int num2 = 0;
                    for (int j = 0; j < cargo.Count; j++)
                    {
                        CargoItem cargoItem = cargo[j];
                        if (cargoItem.stockStationID == -1 && cargoItem.itemType < 5 &&
                            ((cargoItem.isDronePart && !HasDroneBay(aiMercChar.shipData.equipments)) ||
                              (cargoItem.isAmmo && !NeedsThisAmmo(cargoItem.itemID, aiMercChar.shipData.weapons))))
                        {
                            int stationID;
                            switch (cargoItem.itemType)
                            {
                                case 1:
                                    stationID = -3;
                                    break;
                                case 2:
                                    stationID = -4;
                                    break;
                                case 3:
                                    stationID = (ItemDB.GetItem(cargoItem.itemID).canBeStashed ? -2 : currStation.id);
                                    break;
                                default:
                                    stationID = currStation.id;
                                    break;
                            }
                            playerCS.StoreItem(cargoItem.itemType, cargoItem.itemID, cargoItem.rarity, cargoItem.qnt, cargoItem.pricePaid, stationID, -1, cargoItem.extraData);
                            num2 += cargoItem.qnt;
                            cargo.RemoveAt(j);
                            j--;
                        }
                    }
                    if (num2 > 0)
                    {
                        SideInfo.AddMsg(Lang.Get(0, 386, aiMercChar.CommanderName(12), num2));
                    }
                    num += num2;
                }
            }
        }

        private static bool NeedsThisAmmo(int ammoID, List<EquipedWeapon> weapons)
        {
            foreach (EquipedWeapon weapon in weapons)
                if (ammoID == GameData.data.weaponList[weapon.weaponIndex].ammo.itemID)
                    return true;

            return false;
        }

        private static bool HasDroneBay(List<InstalledEquipment> installedEquipments)
        {
            if (droneEquipIDs == null)
            {
                droneEquipIDs = new List<int>();
                foreach (Equipment equip in AccessTools.StaticFieldRefAccess<List<Equipment>>(typeof(EquipmentDB), "equipments"))
                    if (equip.equipName.Contains("Drone Bay"))
                        droneEquipIDs.Add(equip.id);
            }

            if (droneEquipIDs.Count > 0)
                foreach (InstalledEquipment ie in installedEquipments)
                    if (droneEquipIDs.Contains(ie.equipmentID))
                        return true;

            return false;
        }

        private static void DropPassengers(Transform shipsTrans, CargoSystem playerCS)
        {
            Station currStation = Inventory.instance.currStation;
            int num = 0;
            if (shipsTrans == null || playerCS == null)
                return;

            for (int i = 0; i < shipsTrans.childCount; i++)
            {
                AIMercenaryCharacter aiMercChar = shipsTrans.GetChild(i).GetComponent<FleetMemberSlot>().aiMercChar;

                if (aiMercChar.hangarDocked || aiMercChar.dockedStationID == currStation.id)
                {
                    List<CargoItem> cargo = aiMercChar.shipData.cargo;
                    int num2 = 0;
                    for (int j = 0; j < cargo.Count; j++)
                    {
                        CargoItem cargoItem = cargo[j];
                        if (cargoItem.itemType == 5)
                        {
                            playerCS.StoreItem(cargoItem.itemType, cargoItem.itemID, cargoItem.rarity, cargoItem.qnt, cargoItem.pricePaid, currStation.id, -1, cargoItem.extraData);
                            num2 += cargoItem.qnt;
                            cargo.RemoveAt(j);
                            j--;
                        }
                    }
                    if (num2 > 0)
                    {
                        SideInfo.AddMsg(aiMercChar.CommanderName(12) + " unloaded " + num2 + " passengers.");
                    }
                    num += num2;
                }
            }
        }
    }
}
