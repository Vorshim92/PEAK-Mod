using BepInEx;
using System;
using BackpackViewerMod.Patches;

namespace BackpackViewerMod
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static Plugin Instance { get; private set; }

        private PlayerManager playerManagerInstance;
        private BackpackUISlotsPatches uiManagerInstance;

        private void Awake()
        {
            Instance = this;
            Utils.Initialize(Logger);
            Utils.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION} starting new session...");

            try
            {
                PluginConfig.ConfigBind(Config);
                if (!PluginConfig.isPluginEnable.Value)
                {
                    Utils.LogWarning("Plugin is disabled in config!");
                    return;
                }

                PatchManager.PatchAll();

                uiManagerInstance = new BackpackUISlotsPatches();
                playerManagerInstance = new PlayerManager(uiManagerInstance);

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
            Utils.LogInfo("Plugin session is ending. Shutting down systems...");

            playerManagerInstance?.Shutdown();
            uiManagerInstance?.Shutdown();

            playerManagerInstance = null;
            uiManagerInstance = null;

            PatchManager.UnpatchAll();
            Utils.LogInfo("Plugin systems fully shut down.");
        }
    }
}