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
        private Queue<PlayerHeightIndicator> indicatorPool = new Queue<PlayerHeightIndicator>();
        
        // Progress markers
        private List<GameObject> progressMarkers = new List<GameObject>();
        
        // UI settings
        private const float BAR_WIDTH = 12f;
        private const float BAR_HEIGHT = 800f;
        private const float LEFT_OFFSET = 80f;
        private const float BOTTOM_OFFSET = 140f;
        
        // References
        private HeightCalculator heightCalculator;
        private TMP_FontAsset mainFont;
        
        public bool IsVisible => uiRoot?.activeSelf ?? false;
        
        public void Initialize(HeightCalculator calculator)
        {
            heightCalculator = calculator;
            
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
            CreateLabels();
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
            labelText.fontSize = 14f;
            labelText.color = new Color(1f, 1f, 1f, 0.5f);
            
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
            
            // IMPORTANTE: Aggiungi RectTransform PRIMA del tuo componente
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