using System;
using BepInEx.Configuration;
using UnityEngine.InputSystem;

namespace HeightMeterMod
{
    public static class PluginConfig
    {
        // General
        public static ConfigEntry<bool> isPluginEnable;

        
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