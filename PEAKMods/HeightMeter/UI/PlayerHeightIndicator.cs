using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HeightMeterMod
{
    public class PlayerHeightIndicator : MonoBehaviour
    {
        // UI Components
        private RectTransform rectTransform;
        private GameObject marker;
        private GameObject label;
        private TextMeshProUGUI nameText;
        private TextMeshProUGUI heightText;
        private TextMeshProUGUI checkpointText;
        private Image markerImage;
        private CanvasGroup canvasGroup;
        private RectTransform labelRect;

        // References
        public Character character { get; private set; }
        private RectTransform barRect;
        private TMP_FontAsset font;
        
        // Position settings
        private float leftOffset;
        private float bottomOffset;
        private float barHeight;
        
        // Enhanced positioning system
        private Vector2 basePosition;
        private Vector2 labelTargetOffset;
        private Vector2 labelCurrentOffset;
        private float targetNormalizedHeight;
        private float currentNormalizedHeight;
        
        // Smoothing
        private float positionSmoothSpeed = 5f;
        private float offsetSmoothSpeed = 8f;
        
        // Visibility
        private float targetAlpha = 1f;
        private float currentAlpha = 1f;
        
        // State
        private bool isSetup = false;
        
        public float CurrentPosition => currentNormalizedHeight;
        
        private void Awake()
        {
            // FIX: Ottieni i componenti in Awake per essere sicuri che esistano
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
            }
            
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        public void Setup(TMP_FontAsset gameFont, RectTransform altitudeBar, float left, float bottom, float height)
        {
            // FIX: Validazione parametri
            if (gameFont == null || altitudeBar == null)
            {
                Utils.LogError($"PlayerHeightIndicator.Setup: Invalid parameters - font:{gameFont != null}, bar:{altitudeBar != null}");
                return;
            }
            
            font = gameFont;
            barRect = altitudeBar;
            leftOffset = left;
            bottomOffset = bottom;
            barHeight = height;
            
            // FIX: Assicurati che rectTransform esista
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
                if (rectTransform == null)
                {
                    Utils.LogError("PlayerHeightIndicator.Setup: RectTransform is null!");
                    return;
                }
            }
            
            CreateUIElements();
            isSetup = true;
        }
        
        private void CreateUIElements()
        {
            // FIX: Validazione prima di procedere
            if (rectTransform == null)
            {
                Utils.LogError("CreateUIElements: rectTransform is null!");
                return;
            }
            
            // Configura il RectTransform principale
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(0f, 0f);
            rectTransform.pivot = new Vector2(0f, 0.5f);
            
            // Create marker (colored bar)
            marker = new GameObject("Marker");
            marker.transform.SetParent(transform, false);
            
            var markerRect = marker.AddComponent<RectTransform>();
            markerRect.anchorMin = new Vector2(0f, 0.5f);
            markerRect.anchorMax = new Vector2(0f, 0.5f);
            markerRect.pivot = new Vector2(0.5f, 0.5f);
            markerRect.sizeDelta = new Vector2(16f, 4f);
            // Calcola il centro della barra principale e posiziona il marker lì
            if (barRect != null) {
                float barWidth = barRect.sizeDelta.x;
                markerRect.anchoredPosition = new Vector2(barWidth / 2f, 0f);
            } else {
                markerRect.anchoredPosition = new Vector2(0f, 0f); // Fallback
            }

            markerImage = marker.AddComponent<Image>();
            
            // Create label container
            label = new GameObject("Label");
            label.transform.SetParent(transform, false);
            
            // --- MODIFICA 4: Salviamo il riferimento a labelRect ---
            labelRect = label.AddComponent<RectTransform>(); // ERA UNA VARIABILE LOCALE, ORA ASSEGNA AL CAMPO
            labelRect.anchorMin = new Vector2(0f, 0.5f);
            labelRect.anchorMax = new Vector2(0f, 0.5f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            // La posizione iniziale del label è a destra del marker
            labelRect.anchoredPosition = new Vector2(25f, 0f); 

            
            // Semi-transparent background
            var bgImage = label.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.2f);
            
            // Create name text
            var nameObj = new GameObject("Name");
            nameObj.transform.SetParent(label.transform, false);
            
            nameText = nameObj.AddComponent<TextMeshProUGUI>();
            if (font != null) nameText.font = font;
            nameText.fontSize = 16f;
            nameText.fontStyle = FontStyles.Bold;
            nameText.alignment = TextAlignmentOptions.Left;
            nameText.raycastTarget = false; // Performance
            
            var nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(0f, 0.5f);
            nameRect.pivot = new Vector2(0f, 0.5f);
            nameRect.anchoredPosition = new Vector2(5f, 0f);
            
            // Create checkpoint text
            var checkpointObj = new GameObject("Checkpoint");
            checkpointObj.transform.SetParent(label.transform, false);
            
            checkpointText = checkpointObj.AddComponent<TextMeshProUGUI>();
            if (font != null) checkpointText.font = font;
            checkpointText.fontSize = 12f;
            checkpointText.alignment = TextAlignmentOptions.Left;
            checkpointText.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);
            checkpointText.raycastTarget = false;
            checkpointText.gameObject.SetActive(false);
            
            var checkpointRect = checkpointObj.GetComponent<RectTransform>();
            checkpointRect.anchorMin = new Vector2(0f, 0.5f);
            checkpointRect.anchorMax = new Vector2(0f, 0.5f);
            checkpointRect.pivot = new Vector2(0f, 0.5f);
            checkpointRect.anchoredPosition = new Vector2(5f, -20f);
            
            // Update label size
            labelRect.sizeDelta = new Vector2(150f, 40f); // Fixed size for now
        }
        
        public void Initialize(Character targetCharacter)
        {
            if (!isSetup)
            {
                Utils.LogError("PlayerHeightIndicator.Initialize: Not setup yet!");
                return;
            }
            
            if (targetCharacter == null)
            {
                Utils.LogError("PlayerHeightIndicator.Initialize: targetCharacter is null!");
                return;
            }
            
            character = targetCharacter;
            
            // FIX: Validazione completa prima di accedere ai membri
            if (character.refs == null || 
                character.refs.customization == null || 
                character.refs.view == null || 
                character.refs.view.Owner == null)
            {
                Utils.LogError("PlayerHeightIndicator.Initialize: Character references are incomplete!");
                return;
            }
            
            // Get player color
            var playerColor = character.refs.customization.PlayerColor;
            
            // Ensure minimum brightness
            Color.RGBToHSV(playerColor, out float h, out float s, out float v);
            v = Mathf.Max(v, 0.8f);
            s = Mathf.Min(s, 0.8f);
            
            var adjustedColor = Color.HSVToRGB(h, s, v);
            
            if (markerImage != null) markerImage.color = playerColor;
            if (nameText != null) nameText.color = adjustedColor;
            
            // Local player special treatment
            if (character.IsLocal)
            {
                if (marker != null)
                {
                    var rect = marker.GetComponent<RectTransform>();
                    if (rect != null) rect.sizeDelta = new Vector2(20f, 6f);
                }
                
                if (nameText != null)
                {
                    nameText.fontSize = 18f;
                    nameText.fontStyle = FontStyles.Bold;
                }
            }
            
            // Show the indicator
            Show();
        }
        
        public void UpdatePosition(float normalizedHeight, float heightInMeters)
        {
            if (!isSetup || nameText == null || character == null) return;
            
            // Combined name + height display
            string playerName = character.refs.view.Owner.NickName;
            nameText.text = $"{playerName} <size=14>{heightInMeters:F0}m</size>";
            
            targetNormalizedHeight = normalizedHeight;
            
            // Update visibility based on position
            UpdateVisibility();
        }
        
        public void SetOffset(Vector2 offset)
        {
            // Applica l'offset solo al label, non all'intero indicatore!
            labelTargetOffset = offset;
        }
        
        public void UpdateNextCheckpoint(HeightCalculator.CheckpointInfo checkpoint)
        {
            if (!isSetup || checkpointText == null) return;
            
            if (checkpoint != null && PluginConfig.showNextCheckpoint.Value)
            {
                checkpointText.text = $"→ {checkpoint.Name} ({checkpoint.DistanceInMeters:F0}m)";
                checkpointText.gameObject.SetActive(true);
            }
            else
            {
                checkpointText.gameObject.SetActive(false);
            }
        }
        
        private void Update()
        {
            if (!isSetup || rectTransform == null) return;
        
            // Smooth height interpolation
            currentNormalizedHeight = Mathf.Lerp(
                currentNormalizedHeight, 
                targetNormalizedHeight, 
                Time.deltaTime * positionSmoothSpeed
            );
            
            // --- MODIFICA 5: Logica di posizionamento separata per marker e label ---
            
            // 1. Posiziona il contenitore principale (e quindi il marker) sulla barra.
            //    Questo non viene più influenzato dall'offset del clustering.
            float yPosition = bottomOffset + (barHeight * currentNormalizedHeight);
            basePosition = new Vector2(leftOffset, yPosition);
            rectTransform.anchoredPosition = basePosition;
            
            // 2. Interpola l'offset del label
            labelCurrentOffset = Vector2.Lerp(
                labelCurrentOffset, 
                labelTargetOffset, 
                Time.deltaTime * offsetSmoothSpeed
            );

            // 3. Applica l'offset solo al RectTransform del label.
            //    Il label si sposterà rispetto al suo genitore (il PlayerIndicator).
            if (labelRect != null)
            {
                // La posizione base del label è a (25, 0) + l'offset calcolato
                labelRect.anchoredPosition = new Vector2(25f, 0f) + labelCurrentOffset;
            }
            
            // Smooth alpha interpolation
            currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * 10f);
            if (canvasGroup != null) canvasGroup.alpha = currentAlpha;
            
            // Scale effect when appearing/disappearing
            float scale = Mathf.Lerp(0.8f, 1f, currentAlpha);
            transform.localScale = Vector3.one * scale;
        }
        
        private void UpdateVisibility()
        {
            // Fade out at extremes
            if (targetNormalizedHeight < 0.05f || targetNormalizedHeight > 0.95f)
            {
                float edgeFade = Mathf.InverseLerp(0f, 0.05f, 
                    Mathf.Min(targetNormalizedHeight, 1f - targetNormalizedHeight));
                targetAlpha = edgeFade;
            }
            else
            {
                targetAlpha = 1f;
            }
            
            // Additional visibility rules
            if (!character.IsLocal && PluginConfig.showOtherPlayers != null && !PluginConfig.showOtherPlayers.Value)
            {
                targetAlpha = 0f;
            }
        }
        
        // Public API
        public void Show() => targetAlpha = 1f;
        public void Hide() => targetAlpha = 0f;
        public Vector2 GetWorldPosition() => transform.position;
        public bool IsVisible() => currentAlpha > 0.1f;
        
        private void OnDestroy()
        {
            character = null;
        }
    }
}