using BepInEx;
using System;
using UnityEngine;

namespace HeightMeterMod
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static Plugin Instance { get; private set; }
        
        // Manager GameObject
        private GameObject heightMeterManager;

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
                
                Utils.LogInfo("Plugin loaded successfully!");
            }
            catch (Exception ex)
            {
                Utils.LogError($"Failed to load plugin: {ex.Message}");
                Utils.LogError(ex.StackTrace);
            }
        }
        
        // Called by patch when run starts
        internal void OnRunStart()
        {
            if (!PluginConfig.isPluginEnable.Value) return;
            
            Utils.LogInfo("Run started, creating HeightMeter manager");
            
            // Resetta lo stato della patch
            Patches.MountainProgressPatches.ResetState();
            
            if (heightMeterManager != null)
            {
                Destroy(heightMeterManager);
            }
            
            heightMeterManager = new GameObject("HeightMeterManager");
            heightMeterManager.AddComponent<HeightMeterManager>();
        }
        
        // Called by patch when run ends
        internal void OnRunEnd()
        {
            if (heightMeterManager != null)
            {
                Utils.LogInfo("Run ended, cleaning up HeightMeter");
                Destroy(heightMeterManager);
                heightMeterManager = null;
            }
        }

        private void OnDestroy()
        {
            Utils.LogInfo("Plugin is being destroyed");
            PatchManager.UnpatchAll();
        }
        
        // Debug logging helper
        internal static void LogDebug(string message)
        {
            if (PluginConfig.debugMode?.Value ?? false)
            {
                Utils.LogDebug($"[DEBUG] {message}");
            }
        }
    }
}