using System;
using BepInEx.Configuration;

namespace HeightMeterMod
{
    public static class PluginConfig
    {
        // General
        public static ConfigEntry<bool> isPluginEnable;
        public static ConfigEntry<bool> showOtherPlayers;
        
        // UI settings
        public static ConfigEntry<bool> showProgressMarkers;
        public static ConfigEntry<bool> showNextCheckpoint;
        public static ConfigEntry<float> uiScale;
        
        // Performance
        public static ConfigEntry<float> updateInterval;
        
        // Debug
        public static ConfigEntry<bool> debugMode;
        
        public static void ConfigBind(ConfigFile config)
        {
            try
            {
                // General settings
                isPluginEnable = config.Bind(
                    "General",
                    "ModEnabled",
                    true,
                    "Enable or disable the mod"
                );
                
                showOtherPlayers = config.Bind(
                    "General", 
                    "ShowOtherPlayers",
                    true,
                    "Show height indicators for other players"
                );
                
                // UI settings
                showProgressMarkers = config.Bind(
                    "UI",
                    "ShowProgressMarkers",
                    true,
                    "Show checkpoint markers on the altitude bar"
                );
                
                showNextCheckpoint = config.Bind(
                    "UI",
                    "ShowNextCheckpoint",
                    false,
                    "Show distance to next checkpoint"
                );
                
                uiScale = config.Bind(
                    "UI",
                    "UIScale",
                    1.0f,
                    new ConfigDescription(
                        "Scale of the UI elements",
                        new AcceptableValueRange<float>(0.5f, 2.0f)
                    )
                );
                
                // Performance section
                updateInterval = config.Bind(
                    "Performance",
                    "UpdateInterval",
                    0.1f,
                    new ConfigDescription(
                        "How often to update the height display (in seconds)",
                        new AcceptableValueRange<float>(0.05f, 1.0f)
                    )
                );
                
                // Debug section
                debugMode = config.Bind(
                    "Debug",
                    "EnableDebugMode",
                    false,
                    "Enable debug logging"
                );

                Utils.LogInfo($"Config binding complete. ModEnabled = {isPluginEnable.Value}");

                // Setup config change listeners
                SetupConfigListeners();
            }
            catch (Exception ex)
            {
                Utils.LogError($"Error binding config: {ex.Message}");
                // Set default values if config fails
                isPluginEnable = config.Bind("General", "ModEnabled", true);
            }
        }

        private static void SetupConfigListeners()
        {
            isPluginEnable.SettingChanged += (sender, args) =>
            {
                if (isPluginEnable.Value)
                {
                    Utils.LogInfo("Plugin enabled via config");
                    PatchManager.PatchAll();
                }
                else
                {
                    Utils.LogInfo("Plugin disabled via config");
                    PatchManager.UnpatchAll();
                }
            };
        }
    }
}