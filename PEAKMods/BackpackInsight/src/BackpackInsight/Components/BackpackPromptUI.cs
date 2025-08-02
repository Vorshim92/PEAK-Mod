using BepInEx.Logging;
using PEAKLib.UI;
using PEAKLib.UI.Elements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zorro.ControllerSupport;

namespace BackpackInsight.Components
{
    /// <summary>
    /// UI component for backpack prompts using PEAKLib.UI properly
    /// </summary>
    public class BackpackPromptUI : PeakElement
    {
        private static readonly ManualLogSource Logger = Plugin.Log;
        
        private PeakText _promptText = null!;
        private GameObject _container = null!;
        private Image _backgroundImage = null!;
        private bool _isInitialized = false;

        public void Initialize(Transform parent)
        {
            if (_isInitialized) return;

            Logger.LogInfo("Initializing BackpackPromptUI with PEAKLib.UI system");

            // Set up the PeakElement base
            gameObject.name = "BackpackInsightPrompt";
            transform.SetParent(parent, false);
            
            // Create container with background
            _container = new GameObject("Container");
            _container.transform.SetParent(transform, false);
            
            // Configure container RectTransform
            var containerRect = _container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = new Vector2(0, Plugin.ModConfig.PromptPositionY.Value);
            
            // Add semi-transparent background
            _backgroundImage = _container.AddComponent<Image>();
            _backgroundImage.color = new Color(0, 0, 0, Plugin.ModConfig.PromptOpacity.Value);
            
            // Add padding with ContentSizeFitter
            var contentFitter = _container.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // Add horizontal layout for icon + text
            var layoutGroup = _container.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 10f;
            layoutGroup.padding = new RectOffset(15, 15, 8, 8);
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;

            // Create PeakText using PEAKLib's text component
            _promptText = MenuAPI.CreateText("Open Backpack (Shift+E)");
            _promptText.transform.SetParent(_container.transform, false);
            _promptText.TextMesh.fontSize = Plugin.ModConfig.PromptFontSize.Value;
            _promptText.TextMesh.alignment = TextAlignmentOptions.Center;
            _promptText.TextMesh.color = Color.white;
            _promptText.TextMesh.fontStyle = FontStyles.Bold;
            
            // Configure text size
            var textRect = _promptText.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(250, 30);

            _isInitialized = true;
            gameObject.SetActive(false);
        }

        public void UpdatePromptText(InputScheme inputScheme)
        {
            if (_promptText == null) return;

            string promptMessage = inputScheme switch
            {
                InputScheme.Gamepad => GetGamepadPrompt(),
                _ => "Open Backpack (Shift+E)"
            };

            _promptText.SetText(promptMessage);
        }

        private string GetGamepadPrompt()
        {
            try
            {
                // Check gamepad type using PEAKLib/game's input system
                var gamepadType = InputHandler.GetGamepadType();
                
                return gamepadType switch
                {
                    GamepadType.Dualshock or GamepadType.Dualsense => "Open Backpack (L1+□)",
                    _ => "Open Backpack (LB+X)"
                };
            }
            catch
            {
                return "Open Backpack (LB+X)";
            }
        }

        public void SetPosition(Vector2 offset)
        {
            if (_container != null)
            {
                var rect = _container.GetComponent<RectTransform>();
                rect.anchoredPosition = offset;
            }
        }

        public void Show()
        {
            if (!_isInitialized) return;
            
            gameObject.SetActive(true);
            
            // Update text based on current input method
            var currentScheme = InputHandler.GetCurrentUsedInputScheme();
            UpdatePromptText(currentScheme);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
        
        // PEAKLib pattern: Clean up resources
        private void OnDestroy()
        {
            Logger.LogInfo("BackpackPromptUI destroyed");
        }
    }
}