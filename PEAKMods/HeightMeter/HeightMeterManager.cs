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
        private bool isFullyInitialized = false;


        private void Awake()
        {
            // Crea i componenti. Ora sono "dormienti".
            heightCalculator = gameObject.AddComponent<HeightCalculator>();
            playerTracker = gameObject.AddComponent<PlayerTracker>();
            ui = gameObject.AddComponent<HeightMeterUI>();
            
            Utils.LogInfo("HeightMeterManager and components created.");
        }
        
        private void Start()
        {
            // Tenta di inizializzare. Non c'è bisogno di coroutine o attese complesse.
            TryInitialize();
        }

        
        private void TryInitialize()
        {
            Utils.LogInfo("Attempting to initialize HeightMeter...");

            // Tenta di inizializzare il calcolatore.
            // Se fallisce, non siamo nello stato di gioco corretto. Ci fermiamo qui.
            if (!heightCalculator.Initialize())
            {
                Utils.LogWarning("Initialization failed: Not in a valid game state. Manager will remain idle.");
                return;
            }

            // --- SUCCESSO! Siamo sulla montagna. Attiviamo tutto. ---
            Utils.LogInfo("Valid game state detected. Proceeding with full initialization.");

            try
            {
                // 1. Iscriviti agli eventi del tracker
                playerTracker.OnPlayerAdded += OnPlayerAdded;
                playerTracker.OnPlayerRemoved += OnPlayerRemoved;
                Utils.LogInfo("Step 1: Subscribed to PlayerTracker events.");

                // 2. Inizializza la UI
                ui.Initialize(heightCalculator);
                Utils.LogInfo("Step 2: HeightMeterUI initialized.");

                // 3. Inizializza il PlayerTracker
                playerTracker.Initialize();
                Utils.LogInfo("Step 3: PlayerTracker initialized.");
                
                // 4. Ciclo di sicurezza per aggiungere giocatori già tracciati
                foreach (var character in playerTracker.GetTrackedCharacters())
                {
                    OnPlayerAdded(character);
                }
                Utils.LogInfo("Step 4: Ensured all tracked players are added to UI.");

                isFullyInitialized = true;
                Utils.LogInfo("SUCCESS: HeightMeter is now fully active.");
            }
            catch (System.Exception ex)
            {
                Utils.LogError("CRITICAL ERROR during initialization sequence!");
                Utils.LogError(ex.ToString());
            }
        }

        
        private void Update()
        {
            if (!isFullyInitialized || !PluginConfig.isPluginEnable.Value)
            {
                return; // Non fare nulla se non siamo attivi
            }
            
            // La logica di Update è corretta
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
            if (playerTracker != null)
            {
                playerTracker.OnPlayerAdded -= OnPlayerAdded;
                playerTracker.OnPlayerRemoved -= OnPlayerRemoved;
            }
        }
    }
}