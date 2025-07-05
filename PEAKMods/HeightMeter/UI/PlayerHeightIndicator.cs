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
        
        // References
        public Character character;
        private RectTransform barRect;
        private TMP_FontAsset font;
        
        // Position settings
        private float leftOffset;
        private float bottomOffset;
        private float barHeight;
        
        // Smoothing
        private float targetPosition;
        private float currentPosition;
        private float smoothSpeed = 5f;
        
        private float horizontalOffset = 0f;
        private float targetHorizontalOffset = 0f;

        public float CurrentPosition => currentPosition; // Proprietà pubblica per HeightMeterUI

        
        public void Setup(TMP_FontAsset gameFont, RectTransform altitudeBar, float left, float bottom, float height)
        {
            // Debug logs
            Debug.Log($"Transform type: {transform.GetType().Name}");
            Debug.Log($"Has RectTransform: {GetComponent<RectTransform>() != null}");

            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            Debug.Log($"RectTransform after get: {rectTransform != null}");

            font = gameFont;
            barRect = altitudeBar;
            leftOffset = left;
            bottomOffset = bottom;
            barHeight = height;

            CreateUIElements();
        }
        
        private void CreateUIElements()
        {
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
            markerRect.anchoredPosition = new Vector2(0f, 0f);
            
            markerImage = marker.AddComponent<Image>();
            
            // Create label container
            label = new GameObject("Label");
            label.transform.SetParent(transform, false);
            
            var labelRect = label.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0.5f);
            labelRect.anchorMax = new Vector2(0f, 0.5f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = new Vector2(25f, 0f);
            
            // Add background to label
            var bgImage = label.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.0f);
            
            // Create name text
            var nameObj = new GameObject("Name");
            nameObj.transform.SetParent(label.transform, false);
            
            nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.font = font;
            nameText.fontSize = 18f;
            nameText.alignment = TextAlignmentOptions.Left;

            
            var nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(0f, 0.5f);
            nameRect.pivot = new Vector2(0f, 0.5f);
            nameRect.anchoredPosition = new Vector2(5f, 8f);
            
            // Create height text
            var heightObj = new GameObject("Height");
            heightObj.transform.SetParent(label.transform, false);
            
            heightText = heightObj.AddComponent<TextMeshProUGUI>();
            heightText.font = font;
            heightText.fontSize = 18f;
            heightText.fontStyle = FontStyles.Bold;
            heightText.alignment = TextAlignmentOptions.Left;

            var heightRect = heightObj.GetComponent<RectTransform>();
            heightRect.anchorMin = new Vector2(0f, 0.5f);
            heightRect.anchorMax = new Vector2(0f, 0.5f);
            heightRect.pivot = new Vector2(0f, 0.5f);
            heightRect.anchoredPosition = new Vector2(5f, -10f);
            
            // Create checkpoint text
            var checkpointObj = new GameObject("Checkpoint");
            checkpointObj.transform.SetParent(label.transform, false);
            
            checkpointText = checkpointObj.AddComponent<TextMeshProUGUI>();
            checkpointText.font = font;
            checkpointText.fontSize = 12f;
            checkpointText.alignment = TextAlignmentOptions.Left;
            checkpointText.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);
            
            var checkpointRect = checkpointObj.GetComponent<RectTransform>();
            checkpointRect.anchorMin = new Vector2(0f, 0.5f);
            checkpointRect.anchorMax = new Vector2(0f, 0.5f);
            checkpointRect.pivot = new Vector2(0f, 0.5f);
            checkpointRect.anchoredPosition = new Vector2(5f, -28f);
        }
        
        public void Initialize(Character targetCharacter)
        {
            character = targetCharacter;
    
            var playerColor = character.refs.customization.PlayerColor;
            
           // Forza una luminosità minima
            Color.RGBToHSV(playerColor, out float h, out float s, out float v);
            v = Mathf.Max(v, 0.9f); // Minimo 90% luminosità
            s = Mathf.Min(s, 0.7f); // Massimo 70% saturazione
            
            var brightColor = Color.HSVToRGB(h, s, v);
            
            markerImage.color = playerColor; // Marker colore originale
            nameText.color = brightColor;     // Testo più luminoso
            
            // RIMUOVI TUTTI GLI OUTLINE!
            // Niente outlineColor o outlineWidth
            
            nameText.text = character.refs.view.Owner.NickName;
            
            if (character.IsLocal)
            {
                var rect = marker.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(20f, 6f);
                nameText.fontStyle = FontStyles.Bold;
                // NO outline extra per il local player
            }
        }
        
        public void UpdatePosition(float normalizedHeight, float heightInMeters)
        {
            // Formato come ProgressMap: "NomePlayer 123m" tutto su una riga
            string displayText = $"{character.refs.view.Owner.NickName} {heightInMeters:F0}m";
            nameText.text = displayText;
            
            // Nascondi heightText visto che ora è tutto in nameText
            heightText.gameObject.SetActive(false);
            
            targetPosition = normalizedHeight;
            UpdateLabelSize();
        }
        
        public void SetHorizontalOffset(float offset)
        {
            targetHorizontalOffset = offset;
        }

        
        public void UpdateNextCheckpoint(HeightCalculator.CheckpointInfo checkpoint)
        {
            if (checkpoint != null)
            {
                checkpointText.text = $"Next: {checkpoint.Name} ({checkpoint.DistanceInMeters:F0}m)";
                checkpointText.gameObject.SetActive(true);
            }
            else
            {
                checkpointText.gameObject.SetActive(false);
            }

            UpdateLabelSize();
        }

        private void Update()
        {
            // Smooth position interpolation
            currentPosition = Mathf.Lerp(currentPosition, targetPosition, Time.deltaTime * smoothSpeed);

            // Nuovo: movimento orizzontale
            horizontalOffset = Mathf.Lerp(horizontalOffset, targetHorizontalOffset, Time.deltaTime * smoothSpeed);

            // Update position
            float yPosition = bottomOffset + (barHeight * currentPosition);
            float xPosition = leftOffset + horizontalOffset;
            rectTransform.anchoredPosition = new Vector2(xPosition, yPosition);

            // Fade out when at extremes
            float alpha = 1f;
            if (currentPosition < 0.05f || currentPosition > 0.95f)
            {
                alpha = Mathf.InverseLerp(0f, 0.05f, Mathf.Min(currentPosition, 1f - currentPosition));
            }

            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = alpha;
            
            // Debug per vedere l'alpha
            if (character.IsLocal)
            {
                Utils.LogDebug($"Player alpha: {alpha}, position: {currentPosition}");
            }
        }
        
        private void UpdateLabelSize()
        {
            // Calculate required size
            float width = Mathf.Max(
                nameText.GetPreferredValues().x,
                heightText.GetPreferredValues().x,
                checkpointText.gameObject.activeSelf ? checkpointText.GetPreferredValues().x : 0f
            ) + 10f;
            
            float height = checkpointText.gameObject.activeSelf ? 60f : 45f;
            
            label.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
        }
        
        private void OnDestroy()
        {
            character = null;
        }
    }
}