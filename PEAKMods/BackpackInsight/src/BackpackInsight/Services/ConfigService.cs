using BepInEx.Configuration;

namespace BackpackInsight.Services
{
    public class ConfigService
    {
        private ConfigFile _config = null!;
        
        // Configuration entries
        public ConfigEntry<float> HoldTime { get; private set; } = null!;
        public ConfigEntry<bool> EnablePrompts { get; private set; } = null!;
        public ConfigEntry<bool> EnableInsights { get; private set; } = null!;
        public ConfigEntry<bool> ShowDebugInfo { get; private set; } = null!;

        public void Initialize()
        {
            _config = Plugin.Instance.Config;
            
            // General settings
            HoldTime = _config.Bind(
                "General",
                "HoldTime",
                0.3f,
                "Time in seconds to hold the key to open backpack (0 for instant)"
            );
            
            EnablePrompts = _config.Bind(
                "General",
                "EnablePrompts",
                true,
                "Enable UI prompts for backpack interactions"
            );
            
            // Insights settings
            EnableInsights = _config.Bind(
                "Insights",
                "EnableInsights",
                true,
                "Enable backpack usage insights and analytics"
            );
            
            // Debug settings
            ShowDebugInfo = _config.Bind(
                "Debug",
                "ShowDebugInfo",
                false,
                "Show debug information in logs"
            );
        }
    }
}