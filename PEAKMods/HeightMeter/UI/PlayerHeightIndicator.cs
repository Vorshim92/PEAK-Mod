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
            // [ARCHITECT'S NOTE] Validazione robusta all'inizio del metodo.
            if (rectTransform == null)
            {
                Utils.LogError("CreateUIElements: rectTransform is null!");
                return;
            }

            // Configura il RectTransform principale
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(0f, 0f);
            rectTransform.pivot = new Vector2(0f, 0.5f);

            // --- Marker (invariato) ---
            marker = new GameObject("Marker");
            marker.transform.SetParent(transform, false);
            var markerRect = marker.AddComponent<RectTransform>();
            markerRect.anchorMin = new Vector2(0f, 0.5f);
            markerRect.anchorMax = new Vector2(0f, 0.5f);
            markerRect.pivot = new Vector2(0.5f, 0.5f);
            markerRect.sizeDelta = new Vector2(16f, 4f);
            if (barRect != null) {
                float barWidth = barRect.sizeDelta.x;
                markerRect.anchoredPosition = new Vector2(barWidth / 2f, 0f);
            } else {
                markerRect.anchoredPosition = new Vector2(0f, 0f);
            }
            markerImage = marker.AddComponent<Image>();

            // --- Label Container (Riprogettato) ---
            label = new GameObject("Label");
            label.transform.SetParent(transform, false);
            labelRect = label.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0.5f);
            labelRect.anchorMax = new Vector2(0f, 0.5f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = new Vector2(25f, 0f);

            // [ARCHITECT'S NOTE] #1: RIMOZIONE del background solido.
            // Il componente Image non viene più aggiunto al label.
            // var bgImage = label.AddComponent<Image>(); // RIMOSSO

            // [ARCHITECT'S NOTE] #2: AGGIUNTA del ContentSizeFitter per un layout dinamico.
            var layoutGroup = label.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.MiddleLeft;
            layoutGroup.childControlWidth = true; // Permette ai figli di controllare la larghezza
            layoutGroup.childControlHeight = true; // Permette ai figli di controllare l'altezza

            var contentFitter = label.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // --- Name Text (Migliorato) ---
            var nameObj = new GameObject("Name");
            nameObj.transform.SetParent(label.transform, false);
            nameText = nameObj.AddComponent<TextMeshProUGUI>();
            
            // [ARCHITECT'S NOTE] #3: APPLICAZIONE dell'outline e ombra al font.
            ApplyFontStyles(nameText, font, 16f, FontStyles.Bold);

            nameText.alignment = TextAlignmentOptions.Left;
            nameText.raycastTarget = false;
            
            // Il RectTransform del testo non ha più una posizione fissa, sarà gestito dal VerticalLayoutGroup.
            
            // --- Checkpoint Text (Migliorato) ---
            var checkpointObj = new GameObject("Checkpoint");
            checkpointObj.transform.SetParent(label.transform, false);
            checkpointText = checkpointObj.AddComponent<TextMeshProUGUI>();
            
            ApplyFontStyles(checkpointText, font, 12f, FontStyles.Normal, new Color(0.8f, 0.8f, 0.8f, 0.9f));
            
            checkpointText.alignment = TextAlignmentOptions.Left;
            checkpointText.raycastTarget = false;
            checkpointText.gameObject.SetActive(false);
            
            // La sizeDelta del label non è più fissa. Sarà determinata dal contenuto.
            // labelRect.sizeDelta = new Vector2(150f, 40f); // RIMOSSO
        }
        
        /// <summary>
        /// Metodo helper centralizzato per applicare stili coerenti a TextMeshPro.
        /// Questo include il font, la dimensione, il colore e l'effetto outline/shadow.
        /// </summary>
        private void ApplyFontStyles(TextMeshProUGUI textComponent, TMP_FontAsset fontAsset, float fontSize, FontStyles style, Color? color = null)
        {
            if (textComponent == null) return;
            
            textComponent.font = fontAsset;
            textComponent.fontSize = fontSize;
            textComponent.fontStyle = style;
            if (color.HasValue) textComponent.color = color.Value;
            
            // [ARCHITECT'S NOTE] #4: La tecnica di leggibilità chiave.
            // Abilitiamo l'outline direttamente sul materiale del font.
            // Questo crea un contorno nitido senza usare componenti esterni.
            textComponent.outlineWidth = 0.15f; // Spessore relativo alla dimensione del font
            textComponent.outlineColor = new Color(0, 0, 0, 0.75f); // Colore dell'outline
        
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