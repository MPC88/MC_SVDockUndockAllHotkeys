
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace MC_SVDockUndockAllHotkeys
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string pluginGuid = "mc.starvalor.dockundockallhotkeys";
        public const string pluginName = "SV Dock/Undock All Hotkeys";
        public const string pluginVersion = "1.0.0";

        private static ConfigEntry<KeyCodeSubset> cfgModifier;
        private static ConfigEntry<KeyCodeSubset> cfgDockAll;
        private static ConfigEntry<KeyCodeSubset> cfgUndockAll;

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
            cfgModifier = Config.Bind("Keybinds",
                "3. Modifier key",
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
            }
        }
    }
}
