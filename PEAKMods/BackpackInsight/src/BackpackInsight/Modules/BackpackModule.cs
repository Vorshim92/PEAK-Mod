using BepInEx.Logging;
using BackpackInsight.Services;

namespace BackpackInsight.Modules
{
    public class BackpackModule
    {
        private static readonly ManualLogSource Logger = Plugin.Log;
        
        private BackpackService _backpackService = null!;
        private ConfigService _configService = null!;
        private bool _isInitialized;

        public void Initialize()
        {
            if (_isInitialized)
                return;

            Logger.LogInfo("Initializing BackpackModule with PEAKLib integration...");

            try
            {
                // Initialize services
                _configService = new ConfigService();
                _backpackService = new BackpackService(_configService);

                // Initialize configuration
                InitializeConfig();

                _isInitialized = true;
                Logger.LogInfo("BackpackModule initialized successfully!");
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Failed to initialize BackpackModule: {ex}");
            }
        }

        private void InitializeConfig()
        {
            _configService.Initialize();
            
            // Log current configuration
            Logger.LogInfo($"Configuration loaded - HoldTime: {_configService.HoldTime.Value}s, " +
                          $"EnablePrompts: {_configService.EnablePrompts.Value}");
        }

        public void Update()
        {
            if (!_isInitialized)
                return;

            _backpackService?.Update();
        }

        public void Cleanup()
        {
            if (!_isInitialized)
                return;

            Logger.LogInfo("Cleaning up BackpackModule...");

            _backpackService?.Cleanup();
            _isInitialized = false;
        }
    }
}