using System;
using BepInEx.Configuration;
using UnityEngine.InputSystem;

namespace BackpackViewerMod
{
    public static class PluginConfig
    {
        // General
        public static ConfigEntry<bool> isPluginEnable;
        
        // Keybinds
        public static ConfigEntry<float> holdTime;
        public static ConfigEntry<bool> useSecondaryAction;
        public static ConfigEntry<bool> useHoldMethod;
        
        // Visual
        public static ConfigEntry<bool> showHoldPrompt;
        
        // Advanced
        public static ConfigEntry<bool> useDynamicCalls;

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
                
                // Keybind settings
                holdTime = config.Bind(
                    "Keybinds",
                    "HoldTime",
                    0.25f,
                    "Time in seconds to hold the interact key before opening backpack"
                );
                
                useSecondaryAction = config.Bind(
                    "Keybinds",
                    "UseSecondaryAction",
                    true,
                    "Allow using secondary action (right click) to open backpack"
                );
                
                useHoldMethod = config.Bind(
                    "Keybinds",
                    "UseHoldMethod",
                    true,
                    "Allow holding interact key to open backpack"
                );
                
                // Visual settings
                showHoldPrompt = config.Bind(
                    "Visual",
                    "ShowHoldPrompt",
                    true,
                    "Show 'hold to open' prompt when holding backpack"
                );
                
                // Advanced settings
                useDynamicCalls = config.Bind(
                    "Advanced",
                    "UseDynamicCalls",
                    false,
                    "Use dynamic instead of reflection for method calls (experimental)"
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
                holdTime = config.Bind("Keybinds", "HoldTime", 0.25f);
                useSecondaryAction = config.Bind("Keybinds", "UseSecondaryAction", true);
                useHoldMethod = config.Bind("Keybinds", "UseHoldMethod", true);
                showHoldPrompt = config.Bind("Visual", "ShowHoldPrompt", true);
                useDynamicCalls = config.Bind("Advanced", "UseDynamicCalls", false);
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