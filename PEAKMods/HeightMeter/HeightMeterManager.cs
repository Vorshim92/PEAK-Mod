using UnityEngine;
using System.Collections;

namespace HeightMeterMod
{
    public class HeightMeterManager : MonoBehaviour
    {
        private HeightCalculator heightCalculator;
        private PlayerTracker playerTracker;
        private HeightMeterUI ui;
        
        private float updateInterval => PluginConfig.updateInterval.Value;
        private float lastUpdateTime;
        private bool isFullyInitialized = false;

        private void Awake()
        {
            heightCalculator = gameObject.AddComponent<HeightCalculator>();
            playerTracker = gameObject.AddComponent<PlayerTracker>();
            ui = gameObject.AddComponent<HeightMeterUI>();
            
            // Iscriviti all'evento della patch
            Patches.MountainProgressPatches.OnProgressPointsAvailable += OnProgressPointsReceived;

            Utils.LogInfo("HeightMeterManager created and waiting for progress points...");
        }

        // NUOVO METODO: Il trigger che avvia tutto
        private void OnProgressPointsReceived(MountainProgressHandler.ProgressPoint[] points)
        {
            Utils.LogInfo("Game state seems ready. Attempting full system initialization...");

            if (isFullyInitialized) return;

            // --- MODIFICA CHIAVE ---
            // Chiama Initialize() senza parametri. Il calcolatore farà il resto.
            if (!heightCalculator.Initialize())
            {
                Utils.LogError("Full initialization failed: HeightCalculator could not aggregate data. Will retry.");
                // Non impostare `isFullyInitialized` a true, così potrebbe riprovare al prossimo trigger
                return;
            }

            // A questo punto, l'inizializzazione può procedere in sicurezza
            try
            {
                playerTracker.OnPlayerAdded += OnPlayerAdded;
                playerTracker.OnPlayerRemoved += OnPlayerRemoved;
                
                ui.Initialize(heightCalculator);
                ui.CreateProgressMarkers(); // Ora userà la lista aggregata

                playerTracker.Initialize();

                isFullyInitialized = true;
                Utils.LogInfo("SUCCESS: HeightMeter is now fully active with aggregated data.");
            }
            catch (System.Exception ex)
            {
                Utils.LogError($"CRITICAL ERROR during initialization sequence: {ex}");
            }
        }

        
        // Il metodo Start() non è più necessario per l'inizializzazione principale.
        // private void Start() { ... } // RIMUOVERE O SVUOTARE

        private void Update()
        {
            if (!isFullyInitialized || !PluginConfig.isPluginEnable.Value) return;
            
            if (!ui.IsVisible) ui.SetVisible(true);
            
            if (Time.time - lastUpdateTime < updateInterval) return;
            lastUpdateTime = Time.time;
            
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
            // Aggiungiamo un controllo di nullità per sicurezza
            if(character == null || character.name == null) {
                Utils.LogWarning("OnPlayerAdded called with a null character.");
                return;
            }
        
            Utils.LogInfo($"Manager received OnPlayerAdded for: {character.name}. Adding to UI.");
            ui.AddPlayerIndicator(character);
        }

        
        private void OnPlayerRemoved(Character character)
        {
            Utils.LogInfo($"Player removed: {character.name}. Removing from UI.");
            ui.RemovePlayerIndicator(character);
        }
        
        private void OnDestroy()
        {
            // Pulisci le iscrizioni agli eventi!
            Patches.MountainProgressPatches.OnProgressPointsAvailable -= OnProgressPointsReceived;
            if (playerTracker != null)
            {
                playerTracker.OnPlayerAdded -= OnPlayerAdded;
                playerTracker.OnPlayerRemoved -= OnPlayerRemoved;
            }
        }
    }
}