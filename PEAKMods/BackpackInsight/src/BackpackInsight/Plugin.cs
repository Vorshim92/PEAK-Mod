using BepInEx;
using BepInEx.Logging;
using BackpackInsight.Modules;
using HarmonyLib;
using PEAKLib.Core;
using PEAKLib.ModConfig;
using PEAKLib.UI;

namespace BackpackInsight;

[BepInAutoPlugin]
[BepInDependency(CorePlugin.Id)]
[BepInDependency(ModConfigPlugin.Id)]
[BepInDependency(UIPlugin.Id)]
public partial class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log { get; private set; } = null!;
    internal static Plugin Instance { get; private set; } = null!;
    
    private Harmony _harmony = null!;
    private BackpackModule _backpackModule = null!;

    private void Awake()
    {
        Instance = this;
        Log = Logger;

        Log.LogInfo($"Plugin {Name} v{Version} is loading...");

        try
        {
            // Initialize Harmony
            _harmony = new Harmony(Info.Metadata.GUID);
            
            // Initialize modules
            InitializeModules();
            
            // Apply patches
            _harmony.PatchAll();
            
            Log.LogInfo($"Plugin {Name} successfully loaded!");
        }
        catch (System.Exception ex)
        {
            Log.LogError($"Failed to initialize {Name}: {ex}");
        }
    }

    private void InitializeModules()
    {
        _backpackModule = new BackpackModule();
        _backpackModule.Initialize();
        
        Log.LogInfo("Modules initialized successfully");
    }

    private void OnDestroy()
    {
        _backpackModule?.Cleanup();
        _harmony?.UnpatchSelf();
    }
}
