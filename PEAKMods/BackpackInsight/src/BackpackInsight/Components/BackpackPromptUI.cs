using BepInEx.Logging;
using PEAKLib.UI.Elements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackInsight.Components
{
    /// <summary>
    /// UI component for backpack prompts using PEAKLib.UI
    /// </summary>
    public class BackpackPromptUI : PeakElement
    {
        private static readonly ManualLogSource Logger = Plugin.Log;
        
        private TextMeshProUGUI _promptText = null!;
        private Image _keyIcon = null!;
        private bool _isInitialized = false;

        public void Initialize(Transform parent)
        {
            if (_isInitialized) return;

            Logger.LogInfo("Initializing BackpackPromptUI with PEAKLib.UI");

            // Set up the PeakElement
            gameObject.name = "BackpackInsightPrompt";
            transform.SetParent(parent, false);
            
            // Configure RectTransform using PEAKLib patterns
            RectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            RectTransform.pivot = new Vector2(0.5f, 0.5f);
            RectTransform.sizeDelta = new Vector2(400, 30);

            // Create container with horizontal layout
            var layoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 10f;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;

            // Add content size fitter
            var contentSizeFitter = gameObject.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Create text element
            var textObj = new GameObject("PromptText");
            textObj.transform.SetParent(transform, false);
            _promptText = textObj.AddComponent<TextMeshProUGUI>();
            _promptText.text = "Open Backpack (Shift+E)";
            _promptText.fontSize = 14;
            _promptText.alignment = TextAlignmentOptions.Center;
            _promptText.color = Color.white;

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(200, 30);

            _isInitialized = true;
            gameObject.SetActive(false);
        }

        public void UpdatePromptText(string text)
        {
            if (_promptText != null)
                _promptText.text = text;
        }

        public void SetPosition(Vector2 offset)
        {
            RectTransform.anchoredPosition = offset;
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}