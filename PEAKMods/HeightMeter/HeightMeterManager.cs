using UnityEngine;
using System.Collections;

namespace HeightMeterMod
{
    public class HeightMeterManager : MonoBehaviour
    {
        // Components
        private HeightCalculator heightCalculator;
        private PlayerTracker playerTracker;
        private HeightMeterUI ui;
        
        // Update interval for performance
        private float updateInterval => PluginConfig.updateInterval.Value;
        private float lastUpdateTime;
        
        private void Awake()
        {
            // Initialize components in order
            heightCalculator = gameObject.AddComponent<HeightCalculator>();
            playerTracker = gameObject.AddComponent<PlayerTracker>();
            ui = gameObject.AddComponent<HeightMeterUI>();
            
            Utils.LogInfo("HeightMeterManager initialized");
        }
        
        private void Start()
        {
            // Initialize height calculator with game data
            StartCoroutine(InitializeAfterDelay());
        }
        
        private IEnumerator InitializeAfterDelay()
        {
            // Wait a frame to ensure game objects are loaded
            yield return new WaitForSeconds(0.5f);
            
            heightCalculator.Initialize();
            
            // Set up UI with calculated bounds
            ui.Initialize(heightCalculator);
            
            // Start tracking players
            playerTracker.Initialize();
            
            // Subscribe to player events
            playerTracker.OnPlayerAdded += OnPlayerAdded;
            playerTracker.OnPlayerRemoved += OnPlayerRemoved;
            
            Utils.LogInfo("HeightMeter fully initialized");
        }
        
        private void Update()
        {
            // Check if mod is enabled
            if (!PluginConfig.isPluginEnable.Value)
            {
                if (ui.IsVisible)
                    ui.SetVisible(false);
                return;
            }
            
            if (!ui.IsVisible)
                ui.SetVisible(true);
            
            // Throttle updates for performance
            if (Time.time - lastUpdateTime < updateInterval)
                return;
                
            lastUpdateTime = Time.time;
            
            // Update all tracked players
            foreach (var character in playerTracker.GetTrackedCharacters())
            {
                UpdatePlayerHeight(character);
            }
        }
        
        private void UpdatePlayerHeight(Character character)
        {
            // Calculate normalized height (0-1)
            float normalizedHeight = heightCalculator.GetNormalizedHeight(character.Center.z);
            
            // Calculate height in meters
            float heightInMeters = heightCalculator.GetHeightInMeters(character.Center.z);
            
            // Get next checkpoint info
            var nextCheckpoint = heightCalculator.GetNextCheckpoint(character.Center.z);
            
            // Update UI
            ui.UpdatePlayerIndicator(character, normalizedHeight, heightInMeters, nextCheckpoint);
        }
        
        private void OnPlayerAdded(Character character)
        {
            Utils.LogInfo($"Player added: {character.refs.view.Owner.NickName}");
            
            // Only show other players if setting is enabled
            if (!character.IsLocal && !PluginConfig.showOtherPlayers.Value)
                return;
                
            ui.AddPlayerIndicator(character);
        }
        
        private void OnPlayerRemoved(Character character)
        {
            Utils.LogInfo($"Player removed: {character.refs.view.Owner.NickName}");
            ui.RemovePlayerIndicator(character);
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (playerTracker != null)
            {
                playerTracker.OnPlayerAdded -= OnPlayerAdded;
                playerTracker.OnPlayerRemoved -= OnPlayerRemoved;
            }
        }
    }
}