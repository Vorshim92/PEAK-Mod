using BepInEx.Configuration;

namespace BackpackInsight.Config
{
    /// <summary>
    /// Configuration for BackpackInsight that integrates with PEAKLib.ModConfig
    /// </summary>
    public class BackpackInsightConfig
    {
        private readonly ConfigFile _config;
        
        // General Settings
        public ConfigEntry<bool> EnableMod { get; }
        public ConfigEntry<bool> EnablePrompts { get; }
        
        // Input Settings  
        public ConfigEntry<float> HoldTime { get; }
        public ConfigEntry<bool> UseHoldMethod { get; }
        
        // UI Settings
        public ConfigEntry<float> PromptPositionY { get; }
        public ConfigEntry<float> PromptOpacity { get; }
        public ConfigEntry<int> PromptFontSize { get; }
        
        // Advanced Settings
        public ConfigEntry<bool> DebugMode { get; }
        public ConfigEntry<float> GracePeriod { get; }
        
        public BackpackInsightConfig(ConfigFile config)
        {
            _config = config;
            
            // PEAKLib.ModConfig will automatically detect these entries and show them in the mod settings menu
            
            // General
            EnableMod = _config.Bind("General", "Enable Mod", true, 
                "Enable or disable the entire mod");
            
            EnablePrompts = _config.Bind("General", "Enable Prompts", true,
                "Show UI prompts when holding a backpack");
            
            // Input
            HoldTime = _config.Bind("Input", "Hold Time", 0.3f,
                new ConfigDescription("Time in seconds to hold keys before opening backpack",
                    new AcceptableValueRange<float>(0.1f, 2.0f)));
            
            UseHoldMethod = _config.Bind("Input", "Use Hold Method", true,
                "Require holding keys to open backpack");
            
            // UI
            PromptPositionY = _config.Bind("UI", "Prompt Position Y", -100f,
                new ConfigDescription("Vertical offset for the prompt",
                    new AcceptableValueRange<float>(-200f, 200f)));
            
            PromptOpacity = _config.Bind("UI", "Prompt Opacity", 0.7f,
                new ConfigDescription("Background opacity for the prompt",
                    new AcceptableValueRange<float>(0f, 1f)));
            
            PromptFontSize = _config.Bind("UI", "Prompt Font Size", 16,
                new ConfigDescription("Font size for the prompt text",
                    new AcceptableValueRange<int>(12, 24)));
            
            // Advanced - using Tags to hide from UI if needed
            DebugMode = _config.Bind("Advanced", "Debug Mode", false,
                new ConfigDescription("Enable debug logging"));
            
            GracePeriod = _config.Bind("Advanced", "Grace Period", 0.5f,
                new ConfigDescription("Time in seconds to allow key release after opening",
                    new AcceptableValueRange<float>(0.1f, 1.0f)));
        }
    }
}