using BepInEx;
using System;
using UnityEngine;
using BackpackViewerMod.Patches;

namespace BackpackViewerMod
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static Plugin Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            Utils.Initialize(Logger);
            Utils.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION} is loading!");

            try
            {
                PluginConfig.ConfigBind(Config);
                Utils.LogInfo($"Configuration loaded. Plugin enabled: {PluginConfig.isPluginEnable.Value}");

                if (!PluginConfig.isPluginEnable.Value)
                {
                    Utils.LogWarning("Plugin is disabled in config!");
                    return;
                }

                PatchManager.PatchAll();

                // [ARCHITECT'S NOTE] #1: Ordine di inizializzazione corretto.
                // Prima si inizializza chi ascolta, poi chi parla.
                BackpackUISlotsPatches.Initialize();
                PlayerManager.Initialize();

                Utils.LogInfo("Plugin loaded successfully!");
            }
            catch (Exception ex)
            {
                Utils.LogError($"Failed to load plugin: {ex.Message}");
                Utils.LogError(ex.StackTrace);
            }
        }

        private void OnDestroy()
        {
            Utils.LogInfo("Plugin is being destroyed");
            
            // [ARCHITECT'S NOTE] #2: Ordine di shutdown inverso per pulizia.
            PlayerManager.Shutdown(); 
            BackpackUISlotsPatches.Shutdown();
            PatchManager.UnpatchAll();
        }
    }
}