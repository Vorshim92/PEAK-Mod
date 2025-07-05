using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

namespace HeightMeterMod
{
    public class HeightMeterUI : MonoBehaviour
    {
        // UI Root
        private GameObject uiRoot;
        private Canvas canvas;
        
        // Main UI elements
        private GameObject altitudeBar;
        private GameObject peakLabel;
        private GameObject baseLabel;
        private RectTransform barRect;
        
        // Player indicators
        private Dictionary<Character, PlayerHeightIndicator> playerIndicators = new Dictionary<Character, PlayerHeightIndicator>();
        private Dictionary<int, List<PlayerHeightIndicator>> heightGroups = new Dictionary<int, List<PlayerHeightIndicator>>();
        private float groupingThreshold = 0.02f; // 2% di differenza di altezza per considerarli "stesso livello"

        private Queue<PlayerHeightIndicator> indicatorPool = new Queue<PlayerHeightIndicator>();
        
        // Progress markers
        private List<GameObject> progressMarkers = new List<GameObject>();
        
        // UI settings
        private const float BAR_WIDTH = 12f;
        private const float BAR_HEIGHT = 400f;
        private const float LEFT_OFFSET = 80f;
        private const float BOTTOM_OFFSET = 140f;

        // References
        private HeightCalculator heightCalculator;
        private TMP_FontAsset mainFont;
        private StaminaBar staminaBar;
        private RectTransform staminaBarRect;
        private bool hasInitializedPosition = false;
        private float lastStaminaY = -1f;


        
        public bool IsVisible => uiRoot?.activeSelf ?? false;

        public void Initialize(HeightCalculator calculator)
        {
            heightCalculator = calculator;

            // Ottieni il riferimento alla StaminaBar una volta sola
            if (GUIManager.instance != null && GUIManager.instance.bar != null)
            {
                staminaBar = GUIManager.instance.bar;
                staminaBarRect = staminaBar.fullBar; // Usa fullBar invece di staminaBarOutline
                Utils.LogInfo("StaminaBar reference obtained from GUIManager");
            }
            else
            {
                Utils.LogWarning("Could not get StaminaBar reference from GUIManager");
            }

            // Find game font
            FindGameFont();

            // Create UI
            CreateUI();

            // Create progress markers
            if (PluginConfig.showProgressMarkers.Value)
            {
                CreateProgressMarkers();
            }

            Utils.LogInfo("HeightMeterUI initialized");
            
            
        }
        
        private void FindGameFont()
        {
            // Try to find the game's font
            var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            mainFont = fonts.FirstOrDefault(f => f.faceInfo.familyName.Contains("Daruma")) 
                      ?? fonts.FirstOrDefault();
        }
        
        private void CreateUI()
        {
            // Create root GameObject
            uiRoot = new GameObject("HeightMeterUI");
            uiRoot.transform.SetParent(transform, false);
            
            // Create Canvas
            canvas = uiRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // Make sure it's on top
            
            var canvasScaler = uiRoot.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            
            uiRoot.AddComponent<GraphicRaycaster>();
            
            // Create altitude bar background
            CreateAltitudeBar();
            
            // Create labels
            // CreateLabels();
        }
        
        private void Update() 
        {
            if (staminaBarRect == null) return;
    
            // Posiziona la barra dell'altimetro relativa alla stamina bar
            if (!hasInitializedPosition || ShouldUpdatePosition())
            {
                UpdateBarPosition();
            }
        }
        
        private bool ShouldUpdatePosition()
        {
            // Controlla se la stamina bar si è mossa significativamente
            float currentStaminaY = staminaBarRect.anchoredPosition.y;
            return Mathf.Abs(lastStaminaY - currentStaminaY) > 0.1f;
        }

        private void UpdateBarPosition()
        {
            if (staminaBar == null || staminaBarRect == null) return;
    
            // Debug per capire cosa sta succedendo
            bool hasExtraBar = staminaBar.extraBar.gameObject.activeSelf;
            Utils.LogDebug($"UpdateBarPosition - ExtraBar active: {hasExtraBar}");
            
            // Usa il fullBar invece di staminaBarOutline
            RectTransform barToFollow = staminaBar.fullBar; // Prova questo
            float staminaY = barToFollow.anchoredPosition.y;
            float staminaHeight = barToFollow.rect.height;
            
            // Se c'è l'extra bar, aggiungi il suo offset
            float extraOffset = 0f;
            if (hasExtraBar)
            {
                // L'extra bar è posizionata sotto, quindi dobbiamo considerare la sua altezza
                extraOffset = staminaBar.extraBar.sizeDelta.y + 10f; // Altezza extra bar + margine
                Utils.LogDebug($"ExtraBar height: {staminaBar.extraBar.sizeDelta.y}");
            }
            
            // Posiziona sopra tutto con margine
            float targetY = staminaY + staminaHeight + extraOffset + 20f;
            
            Utils.LogDebug($"Target Y: {targetY}, Current Y: {barRect.anchoredPosition.y}");
            
            // Movimento smooth
            float currentY = barRect.anchoredPosition.y;
            float newY = Mathf.Lerp(currentY, targetY, Time.deltaTime * 8f);
            barRect.anchoredPosition = new Vector2(LEFT_OFFSET, newY);
            
            lastStaminaY = staminaY;
            hasInitializedPosition = true;
        }


        private void LateUpdate()
        {
            if (playerIndicators.Count <= 1) return; // Non serve se c'è solo 1 player
            
            // Raggruppa i player per altezza simile
            heightGroups.Clear();
            
            foreach (var kvp in playerIndicators)
            {
                var indicator = kvp.Value;
                int heightBucket = Mathf.RoundToInt(indicator.CurrentPosition / groupingThreshold);
                
                if (!heightGroups.ContainsKey(heightBucket))
                    heightGroups[heightBucket] = new List<PlayerHeightIndicator>();
                    
                heightGroups[heightBucket].Add(indicator);
            }
            
            // Sposta lateralmente i player allo stesso livello
            foreach (var group in heightGroups.Values)
            {
                if (group.Count > 1)
                {
                    // Ordina per nome per consistenza
                    group.Sort((a, b) => a.character.name.CompareTo(b.character.name));
                    
                    for (int i = 0; i < group.Count; i++)
                    {
                        // Calcola offset orizzontale
                        float totalWidth = (group.Count - 1) * 120f; // 120 pixel tra ogni player
                        float startX = -totalWidth / 2f;
                        float offset = startX + (i * 120f);
                        
                        group[i].SetHorizontalOffset(offset);
                    }
                }
                else if (group.Count == 1)
                {
                    group[0].SetHorizontalOffset(0f); // Reset al centro se da solo
                }
            }
        }


        
        private void CreateAltitudeBar()
        {
            altitudeBar = new GameObject("AltitudeBar");
            altitudeBar.transform.SetParent(uiRoot.transform, false);

            barRect = altitudeBar.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 0f);
            barRect.anchorMax = new Vector2(0f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.anchoredPosition = new Vector2(LEFT_OFFSET, BOTTOM_OFFSET);
            barRect.sizeDelta = new Vector2(BAR_WIDTH, BAR_HEIGHT);

            var barImage = altitudeBar.AddComponent<Image>();
            barImage.color = new Color(0.75f, 0.75f, 0.69f, 0.4f);

            // Add gradient overlay
            CreateGradientOverlay();
        }
        
        private void CreateGradientOverlay()
        {
            var gradientObj = new GameObject("Gradient");
            gradientObj.transform.SetParent(altitudeBar.transform, false);
            
            var gradientRect = gradientObj.AddComponent<RectTransform>();
            gradientRect.anchorMin = Vector2.zero;
            gradientRect.anchorMax = Vector2.one;
            gradientRect.sizeDelta = Vector2.zero;
            gradientRect.anchoredPosition = Vector2.zero;
            
            var gradient = gradientObj.AddComponent<Image>();
            gradient.color = new Color(1f, 1f, 1f, 0.2f);
        }
        
        private void CreateLabels()
        {
            // Peak label
            peakLabel = CreateLabel("PEAK", new Vector2(LEFT_OFFSET, BOTTOM_OFFSET + BAR_HEIGHT + 10f));
            
            // Base label
            baseLabel = CreateLabel("BASE", new Vector2(LEFT_OFFSET, BOTTOM_OFFSET - 30f));
        }
        
        private GameObject CreateLabel(string text, Vector2 position)
        {
            var labelObj = new GameObject($"Label_{text}");
            labelObj.transform.SetParent(uiRoot.transform, false);
            
            var rectTransform = labelObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(0f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            
            var textComponent = labelObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.font = mainFont;
            textComponent.fontSize = 24f;
            textComponent.color = new Color(1f, 1f, 1f, 0.6f);
            textComponent.alignment = TextAlignmentOptions.Center;
            
            rectTransform.sizeDelta = textComponent.GetPreferredValues();
            
            return labelObj;
        }
        
        private void CreateProgressMarkers()
        {
            var markers = heightCalculator.GetProgressMarkers();
            
            foreach (var marker in markers)
            {
                CreateProgressMarker(marker);
            }
        }
        
        private void CreateProgressMarker(HeightCalculator.ProgressMarker marker)
        {
            var markerObj = new GameObject($"Marker_{marker.Name}");
            markerObj.transform.SetParent(altitudeBar.transform, false);
            
            var markerRect = markerObj.AddComponent<RectTransform>();
            markerRect.anchorMin = new Vector2(0f, marker.NormalizedHeight);
            markerRect.anchorMax = new Vector2(1f, marker.NormalizedHeight);
            markerRect.sizeDelta = new Vector2(0f, 2f);
            markerRect.anchoredPosition = Vector2.zero;
            
            var markerImage = markerObj.AddComponent<Image>();
            markerImage.color = new Color(1f, 1f, 1f, 0.3f);
            
            // Add label
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(markerObj.transform, false);
            
            var labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(1f, 0.5f);
            labelRect.anchorMax = new Vector2(1f, 0.5f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = new Vector2(20f, 0f);
            
            var labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = $"{marker.Name} - {marker.HeightInMeters:F0}m";
            labelText.font = mainFont;
            labelText.fontSize = 12f;
            labelText.color = new Color(1f, 1f, 1f, 0.5f);
            labelText.alignment = TextAlignmentOptions.Center;  // Centrato

            
            labelRect.sizeDelta = labelText.GetPreferredValues();
            
            progressMarkers.Add(markerObj);
        }
        
        public void AddPlayerIndicator(Character character)
        {
            if (playerIndicators.ContainsKey(character))
            {
                Utils.LogWarning($"Indicator for {character.refs.view.Owner.NickName} already exists. Ignoring.");
                return;
            }
            
            // Aggiungi un log qui
            Utils.LogInfo($"UI: Creating indicator for {character.refs.view.Owner.NickName}");
                    
            // Get from pool or create new
            PlayerHeightIndicator indicator;
            if (indicatorPool.Count > 0)
            {
                indicator = indicatorPool.Dequeue();
                indicator.gameObject.SetActive(true);
                Utils.LogInfo("Reusing indicator from pool.");
            }
            else
            {
                indicator = CreatePlayerIndicator();
                Utils.LogInfo("Creating new indicator.");
            }
            
            indicator.Initialize(character);
            playerIndicators[character] = indicator;
        }

        
        public void RemovePlayerIndicator(Character character)
        {
            if (playerIndicators.TryGetValue(character, out var indicator))
            {
                indicator.gameObject.SetActive(false);
                indicatorPool.Enqueue(indicator);
                playerIndicators.Remove(character);
            }
        }
        
        public void UpdatePlayerIndicator(Character character, float normalizedHeight, float heightInMeters, HeightCalculator.CheckpointInfo nextCheckpoint)
        {
            if (playerIndicators.TryGetValue(character, out var indicator))
            {
                indicator.UpdatePosition(normalizedHeight, heightInMeters);
                
                if (PluginConfig.showNextCheckpoint.Value && nextCheckpoint != null)
                {
                    indicator.UpdateNextCheckpoint(nextCheckpoint);
                }
            }
        }
        
        private PlayerHeightIndicator CreatePlayerIndicator()
        {
            var indicatorObj = new GameObject("PlayerIndicator");
            indicatorObj.transform.SetParent(uiRoot.transform, false);
            
            indicatorObj.AddComponent<RectTransform>();
            
            var indicator = indicatorObj.AddComponent<PlayerHeightIndicator>();
            indicator.Setup(mainFont, barRect, LEFT_OFFSET, BOTTOM_OFFSET, BAR_HEIGHT);
            
            return indicator;

        }
        
        public void SetVisible(bool visible)
        {
            if (uiRoot != null)
                uiRoot.SetActive(visible);
        }
        
        private void OnDestroy()
        {
            // Clean up all UI elements
            if (uiRoot != null)
                Destroy(uiRoot);
        }
    }
}