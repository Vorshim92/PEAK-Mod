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
        public static ConfigEntry<bool> useHoldMethod;
        
        // Visual
        public static ConfigEntry<bool> showHoldPrompt;
        
        public static ConfigEntry<bool> showPlayerBackpackSlots;
        public static ConfigEntry<bool> showOtherPlayerBackpackSlots;
        public static ConfigEntry<float> otherPlayerSlotsScale;

        
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
                    0.75f,
                    "Time in seconds to hold the interact key before opening backpack"
                );


                useHoldMethod = config.Bind(
                    "Keybinds",
                    "UseHoldMethod",
                    true,
                    "Allow holding interact key to open backpack"
                );

                // Visual settings
                // NOTA: Questa opzione ora non è più necessaria perché il prompt appare con il sistema del gioco.
                // Puoi decidere di rimuoverla o di lasciarla nel caso tu voglia
                // disabilitare il prompt "Hold to Open" in futuro. Per ora la lascio.
                showHoldPrompt = config.Bind(
                    "Visual",
                    "ShowHoldPrompt",
                    true,
                    "Show 'Hold to Open' prompt when holding a backpack."
                );

                // Visual (UI Estesa)
                showPlayerBackpackSlots = config.Bind(
                    "Visual.ExtendedUI",
                    "ShowPlayerBackpackSlots",
                    true,
                    "Mostra gli slot dello zaino del tuo personaggio sulla UI."
                );

                showOtherPlayerBackpackSlots = config.Bind(
                    "Visual.ExtendedUI",
                    "ShowOtherPlayerBackpackSlots",
                    true,
                    "Mostra anche gli slot degli zaini degli altri giocatori vicini."
                );

                otherPlayerSlotsScale = config.Bind(
                    "Visual.ExtendedUI",
                    "OtherPlayerSlotsScale",
                    0.8f,
                    "La scala (dimensione) degli slot degli altri giocatori rispetto a quelli del tuo personaggio (es. 0.8 = 80%)."
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
                useHoldMethod = config.Bind("Keybinds", "UseHoldMethod", true);
                showHoldPrompt = config.Bind("Visual", "ShowHoldPrompt", true);
                showPlayerBackpackSlots = config.Bind("Visual.ExtendedUI", "ShowPlayerBackpackSlots", true);
                showOtherPlayerBackpackSlots = config.Bind("Visual.ExtendedUI", "ShowOtherPlayerBackpackSlots", true);
                otherPlayerSlotsScale = config.Bind("Visual.ExtendedUI", "OtherPlayerSlotsScale", 0.8f);

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