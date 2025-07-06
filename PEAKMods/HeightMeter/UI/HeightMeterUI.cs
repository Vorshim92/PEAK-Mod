using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
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
        
        // Systems
        private PlayerClusteringSystem clusteringSystem;
        
        // Player indicators
        private Dictionary<Character, PlayerHeightIndicator> playerIndicators = new Dictionary<Character, PlayerHeightIndicator>();
        private Queue<PlayerHeightIndicator> indicatorPool = new Queue<PlayerHeightIndicator>();
        
        // Progress markers
        private List<GameObject> progressMarkers = new List<GameObject>();
        
        // UI settings
        private const float BAR_WIDTH = 12f;
        private const float BAR_HEIGHT = 400f;
        private const float LEFT_OFFSET = 80f;
        private const float STAMINA_BAR_TOP_MARGIN = 100f;
        private const float ROPE_SPOOL_OFFSET = 120f;

        
        // References
        private HeightCalculator heightCalculator;
        private TMP_FontAsset mainFont;
        private StaminaBar staminaBar;
        
        // State
        private bool isInitialized = false;
        
        public bool IsVisible => uiRoot?.activeSelf ?? false;
        
        public void Initialize(HeightCalculator calculator)
        {
            heightCalculator = calculator;
            
            // Get StaminaBar reference
            if (!TryGetStaminaBarReference()) 
            {
                Utils.LogError("Failed to get StaminaBar reference!");
                return;
            }
            
            // Find game font
            FindGameFont();
            
            // Create UI structure
            CreateUI();
            
            // Initialize systems
            InitializeSystems();
            
            // Create progress markers if enabled
            if (PluginConfig.showProgressMarkers.Value)
            {
                CreateProgressMarkers();
            }
            
            isInitialized = true;
            Utils.LogInfo("HeightMeterUI initialized with enhanced systems");
        }
        
        private bool TryGetStaminaBarReference()
        {
            if (GUIManager.instance?.bar != null)
            {
                staminaBar = GUIManager.instance.bar;
                Utils.LogInfo("StaminaBar reference obtained successfully");
                return true;
            }
            
            // Fallback: try to find it
            staminaBar = Object.FindAnyObjectByType<StaminaBar>();
            if (staminaBar != null)
            {
                Utils.LogInfo("StaminaBar found via fallback method");
                return true;
            }
            
            Utils.LogWarning("Could not find StaminaBar reference");
            return false;
        }
        
        private void InitializeSystems()
        {
            // Initialize Clustering System con la nuova configurazione
            clusteringSystem = gameObject.AddComponent<PlayerClusteringSystem>();
            
            var clusterConfig = new PlayerClusteringSystem.ClusterConfig
            {
                heightThreshold = 0.025f,     // 2.5% threshold per raggruppare
                verticalSpacing = 22f,        // Spaziatura verticale tra i nomi
            };
            
            clusteringSystem.Initialize(clusterConfig);
        }
        
        private void Update()
        {
            if (!isInitialized || staminaBar == null) return;
            
            // Posizionamento dinamico
            UpdateAltimeterPosition();
        }
        
        private void UpdateAltimeterPosition()
        {
            if (barRect == null || staminaBar == null || staminaBar.staminaBarOutline == null) return;
            
            // Mantieni la X originale (LEFT_OFFSET)
            float targetX = LEFT_OFFSET;

            if (Character.localCharacter?.data.currentItem != null)
            {
                RopeSpool ropeSpool;
                if (Character.localCharacter.data.currentItem.TryGetComponent<RopeSpool>(out ropeSpool))
                {
                    targetX = LEFT_OFFSET + ROPE_SPOOL_OFFSET; // Sposta a destra
                }
            }

            // Calcola la Y relativa alla stamina bar
            RectTransform staminaRect = staminaBar.staminaBarOutline;
            float staminaY = staminaRect.anchoredPosition.y;
            
            // Aggiungi offset per extra bar se presente
            float extraOffset = 0f;
            if (staminaBar.extraBar != null && staminaBar.extraBar.gameObject.activeSelf)
            {
                extraOffset = staminaBar.extraBar.sizeDelta.y + 15f;
            }
            
            // Posiziona la barra sopra la stamina bar
           float targetY = staminaY + staminaRect.sizeDelta.y + extraOffset + STAMINA_BAR_TOP_MARGIN;

            // Smooth interpolation
            Vector2 currentPos = barRect.anchoredPosition;
            Vector2 targetPos = new Vector2(targetX, targetY);
            
            barRect.anchoredPosition = Vector2.Lerp(currentPos, targetPos, Time.deltaTime * 8f);
        }
        
        private void FindGameFont()
        {
            var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            mainFont = fonts.FirstOrDefault(f => f.faceInfo.familyName.Contains("Daruma")) 
                      ?? fonts.FirstOrDefault();
                      
            if (mainFont == null)
            {
                Utils.LogWarning("Could not find game font, using default");
            }
        }
        
        private void CreateUI()
        {
            // Create root GameObject
            uiRoot = new GameObject("HeightMeterUI");
            uiRoot.transform.SetParent(transform, false);
            
            // Create Canvas
            canvas = uiRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            var canvasScaler = uiRoot.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            
            uiRoot.AddComponent<GraphicRaycaster>();
            
            // Create altitude bar
            CreateAltitudeBar();
            
            // Create labels if needed
            if (PluginConfig.debugMode?.Value ?? false)
            {
                CreateDebugLabels();
            }
        }
        
        private void CreateAltitudeBar()
        {
            altitudeBar = new GameObject("AltitudeBar");
            altitudeBar.transform.SetParent(uiRoot.transform, false);
            
            barRect = altitudeBar.AddComponent<RectTransform>();
            
            // Usa lo stesso sistema di ancoraggio della stamina bar
            barRect.anchorMin = new Vector2(0f, 0f);
            barRect.anchorMax = new Vector2(0f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.sizeDelta = new Vector2(BAR_WIDTH, BAR_HEIGHT);
            
            // NON impostare una posizione iniziale fissa
            // Lascia che UpdateAltimeterPosition la imposti correttamente
            barRect.anchoredPosition = new Vector2(LEFT_OFFSET, -1000f); // Fuori schermo inizialmente
            
            // Bar background
            var barImage = altitudeBar.AddComponent<Image>();
            barImage.color = new Color(0.75f, 0.75f, 0.69f, 0.4f);
            
            // Add gradient overlay for visual appeal
            CreateGradientOverlay();
            
            // Forza un update immediato della posizione
            StartCoroutine(ForceInitialPosition());
        }
        
        private System.Collections.IEnumerator ForceInitialPosition()
        {
            // Aspetta un frame per essere sicuri che tutto sia inizializzato
            yield return null;
            UpdateAltimeterPosition();
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
            gradient.type = Image.Type.Filled;
            gradient.fillMethod = Image.FillMethod.Vertical;
            gradient.fillOrigin = (int)Image.OriginVertical.Bottom;
            gradient.color = new Color(1f, 1f, 1f, 0.2f);
        }
        
        private void CreateDebugLabels()
        {
            // I label sono già figli della barra tramite il parametro parent
            peakLabel = CreateLabel("PEAK", new Vector2(0f, BAR_HEIGHT + 10f), altitudeBar.transform);
            baseLabel = CreateLabel("BASE", new Vector2(0f, -30f), altitudeBar.transform);
        }
        
        private GameObject CreateLabel(string text, Vector2 localPosition, Transform parent)
        {
            var labelObj = new GameObject($"Label_{text}");
            labelObj.transform.SetParent(parent, false);
            
            var rectTransform = labelObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = localPosition;
            
            var textComponent = labelObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.font = mainFont;
            textComponent.fontSize = 20f;
            textComponent.color = new Color(1f, 1f, 1f, 0.6f);
            textComponent.alignment = TextAlignmentOptions.Center;
            
            var fitter = labelObj.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
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
            markerObj.transform.SetParent(altitudeBar.transform, false); // Figlio della barra!
            
            var markerRect = markerObj.AddComponent<RectTransform>();
            markerRect.anchorMin = new Vector2(0f, marker.NormalizedHeight);
            markerRect.anchorMax = new Vector2(1f, marker.NormalizedHeight);
            markerRect.sizeDelta = new Vector2(0f, 2f);
            markerRect.anchoredPosition = Vector2.zero;
            
            var markerImage = markerObj.AddComponent<Image>();
            markerImage.color = marker.IsReached 
                ? new Color(0.2f, 0.8f, 0.2f, 0.5f)  // Green for reached
                : new Color(1f, 1f, 1f, 0.3f);       // White for unreached
            
            // Add label with better positioning
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
            labelText.fontSize = 11f;
            labelText.color = marker.IsReached 
                ? new Color(0.5f, 1f, 0.5f, 0.7f)
                : new Color(1f, 1f, 1f, 0.5f);
            labelText.alignment = TextAlignmentOptions.Left;
            
            var fitter = labelObj.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            progressMarkers.Add(markerObj);
        }
        
        public void AddPlayerIndicator(Character character)
        {
            if (playerIndicators.ContainsKey(character))
            {
                Utils.LogWarning($"Indicator for {character.refs.view.Owner.NickName} already exists");
                return;
            }
            
            Utils.LogInfo($"Creating indicator for {character.refs.view.Owner.NickName}");
            
            // Get from pool or create new
            PlayerHeightIndicator indicator;
            if (indicatorPool.Count > 0)
            {
                indicator = indicatorPool.Dequeue();
                indicator.gameObject.SetActive(true);
            }
            else
            {
                indicator = CreatePlayerIndicator();
            }
            
            // FIX 2: Assicurati che l'indicatore sia completamente inizializzato
            if (indicator != null)
            {
                indicator.Initialize(character);
                playerIndicators[character] = indicator;
                Utils.LogInfo($"Successfully added indicator for {character.refs.view.Owner.NickName}");
            }
            else
            {
                Utils.LogError("Failed to create player indicator!");
            }
        }
        
        public void RemovePlayerIndicator(Character character)
        {
            if (playerIndicators.TryGetValue(character, out var indicator))
            {
                indicator.Hide();
                indicator.gameObject.SetActive(false);
                indicatorPool.Enqueue(indicator);
                playerIndicators.Remove(character);
                
                Utils.LogInfo($"Removed indicator for {character.refs.view.Owner.NickName}");
            }
        }
        
        public void UpdatePlayerIndicator(Character character, float normalizedHeight, float heightInMeters, HeightCalculator.CheckpointInfo nextCheckpoint)
        {
            if (playerIndicators.TryGetValue(character, out var indicator))
            {
                indicator.UpdatePosition(normalizedHeight, heightInMeters);
                
                if (nextCheckpoint != null)
                {
                    indicator.UpdateNextCheckpoint(nextCheckpoint);
                }
            }
        }
        
        private PlayerHeightIndicator CreatePlayerIndicator()
        {
            var indicatorObj = new GameObject("PlayerIndicator");
            // IMPORTANTE: Rendi l'indicatore figlio della BARRA, non del uiRoot!
            indicatorObj.transform.SetParent(altitudeBar.transform, false);
            
            // Aggiungi il RectTransform
            indicatorObj.AddComponent<RectTransform>();
            
            var indicator = indicatorObj.AddComponent<PlayerHeightIndicator>();
            
            // Ora gli indicatori si muoveranno automaticamente con la barra!
            if (indicator != null && mainFont != null && barRect != null)
            {
                // leftOffset = 0 perché ora è relativo alla barra
                // bottomOffset = 0 perché il bottom della barra è la posizione 0
                indicator.Setup(mainFont, barRect, 0f, 0f, BAR_HEIGHT);
            }
            else
            {
                Utils.LogError($"Missing dependencies for indicator: font={mainFont != null}, barRect={barRect != null}");
            }
            
            return indicator;
        }
        
        private void LateUpdate()
        {
            if (!isInitialized) return;

            // Aggiorna la posizione della nostra UI per prima cosa
            UpdateAltimeterPosition();

            // Poi, processa il clustering dei giocatori
            if (playerIndicators.Count > 0)
            {
                clusteringSystem.ProcessIndicators(playerIndicators);
            }
        }

        
        public void SetVisible(bool visible)
        {
            if (uiRoot != null)
            {
                uiRoot.SetActive(visible);
            }
        }
        
        private void OnDestroy()
        {
            if (uiRoot != null)
            {
                Destroy(uiRoot);
            }
        }
    }
}