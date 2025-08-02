using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Reflection;
using System.Linq;
using TMPro;
using Zorro;
using Zorro.ControllerSupport;

namespace BackpackViewerMod.Patches
{
    /// <summary>
    /// UI patches to show custom prompts for backpack interactions.
    /// 
    /// ISSUE: The game now shows "E - lunge" prompt when holding a backpack,
    /// which conflicts with our custom prompt. We hide all default prompts
    /// and show only our "Shift+E to Open Backpack" prompt.
    /// </summary>
    public class UIPatches
    {
        private static GameObject customPromptContainer = null;
        private static TextMeshProUGUI customPromptText = null;

        [HarmonyPatch(typeof(GUIManager), "LateUpdate")]
        public class GUIManager_LateUpdate_Patch
        {
            static void Postfix(GUIManager __instance)
            {
                try
                {
                    var localCharacter = Character.localCharacter;
                    if (localCharacter == null) return;

                    var currentItem = localCharacter.data?.currentItem;
                    bool holdingBackpack = currentItem != null && currentItem.GetType().Name == "Backpack";
                    
                    bool shouldShow = holdingBackpack && !__instance.wheelActive && !__instance.windowBlockingInput;

                    if (shouldShow && !(__instance.throwGO && __instance.throwGO.activeSelf))
                    {
                        // Create our custom prompt if it doesn't exist
                        if (customPromptContainer == null)
                        {
                            CreateCustomPrompt(__instance);
                        }
                        
                        if (customPromptContainer != null && !customPromptContainer.activeSelf)
                        {
                            customPromptContainer.SetActive(true);
                            
                            // Update text based on input device
                            if (customPromptText != null)
                            {
                                var currentScheme = Zorro.ControllerSupport.InputHandler.GetCurrentUsedInputScheme();
                                if (currentScheme == Zorro.ControllerSupport.InputScheme.Gamepad)
                                {
                                    var gamepadType = Zorro.ControllerSupport.InputHandler.GetGamepadType();
                                    
                                    // Different button prompts based on controller type
                                    if (gamepadType == Zorro.ControllerSupport.GamepadType.Dualshock || 
                                        gamepadType == Zorro.ControllerSupport.GamepadType.Dualsense)
                                    {
                                        // PlayStation: L1 + Square
                                        customPromptText.text = "Open Backpack (L1+□)";
                                    }
                                    else
                                    {
                                        // Xbox/Generic: LB + X
                                        customPromptText.text = "Open Backpack (LB+X)";
                                    }
                                }
                                else
                                {
                                    customPromptText.text = "Open Backpack (Shift+E)";
                                }
                            }
                        }
                    }
                    else
                    {
                        // Hide our custom prompt
                        if (customPromptContainer != null && customPromptContainer.activeSelf)
                        {
                            customPromptContainer.SetActive(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error in GUIManager_LateUpdate_Patch: {ex.Message}");
                }
            }
            
            static void CreateCustomPrompt(GUIManager guiManager)
            {
                try
                {
                    
                    // Find a template prompt to copy style from
                    var templatePrompt = guiManager.interactPromptPrimary;
                    if (templatePrompt == null) 
                    {
                        Utils.LogError("Template prompt is null!");
                        return;
                    }
                    
                    
                    // Create a simple container instead of cloning the entire prompt
                    customPromptContainer = new GameObject("BackpackModPrompt");
                    customPromptContainer.transform.SetParent(templatePrompt.transform.parent, false);
                    
                    // Add RectTransform and configure it
                    var rectTransform = customPromptContainer.AddComponent<RectTransform>();
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    
                    // Position below the primary prompt
                    var templateRect = templatePrompt.GetComponent<RectTransform>();
                    rectTransform.anchoredPosition = templateRect.anchoredPosition + new Vector2(0, -50);
                    rectTransform.sizeDelta = new Vector2(400, 30); // Wider container
                    
                    
                    // Clone the structure of the template prompt
                    var templateChildren = templatePrompt.GetComponentsInChildren<Transform>();
                    
                    // Look for the key icon and text components in the template
                    GameObject keyIconObj = null;
                    GameObject textObj = null;
                    
                    foreach (var child in templateChildren)
                    {
                        if (child.name.Contains("Key") || child.name.Contains("Icon") || child.name.Contains("Input"))
                        {
                            keyIconObj = child.gameObject;
                        }
                        else if (child.GetComponent<TextMeshProUGUI>() != null && child != templatePrompt.transform)
                        {
                            textObj = child.gameObject;
                        }
                    }
                    
                    // Clone the key icon if found
                    if (keyIconObj != null)
                    {
                        var clonedIcon = GameObject.Instantiate(keyIconObj, customPromptContainer.transform);
                        clonedIcon.name = "KeyIcon";
                        // Keep its relative position
                    }
                    
                    // Clone the text object if found, otherwise create new
                    if (textObj != null)
                    {
                        var clonedText = GameObject.Instantiate(textObj, customPromptContainer.transform);
                        clonedText.name = "Text";
                        customPromptText = clonedText.GetComponent<TextMeshProUGUI>();
                        customPromptText.text = "Open Backpack (Shift+E)";
                        
                        // Force the text to be wider
                        var textRect = clonedText.GetComponent<RectTransform>();
                        
                        // FIX: Impostiamo pivot e anchor corretti PRIMA di posizionare
                        textRect.pivot = new Vector2(0f, 0.5f); // Pivot a sinistra
                        textRect.anchorMin = new Vector2(0f, 0.5f);
                        textRect.anchorMax = new Vector2(0f, 0.5f);
                        
                        // Ora impostiamo dimensione e posizione
                        textRect.sizeDelta = new Vector2(350, textRect.sizeDelta.y);
                        textRect.anchoredPosition = new Vector2(60f, -2f); // 60 pixel a destra, 2 pixel più in basso
                        
                        // Also ensure text doesn't wrap
                        customPromptText.overflowMode = TextOverflowModes.Overflow;
                        customPromptText.textWrappingMode = TextWrappingModes.NoWrap;
                        
                        // IMPORTANTE: Imposta l'allineamento del testo
                        customPromptText.alignment = TextAlignmentOptions.Center;
                    }
                    else
                    {
                        // Fallback: create text manually
                        textObj = new GameObject("Text");
                        textObj.transform.SetParent(customPromptContainer.transform, false);
                        customPromptText = textObj.AddComponent<TextMeshProUGUI>();
                        
                        // Copy all properties from template text if available
                        var templateText = templatePrompt.GetComponentInChildren<TextMeshProUGUI>();
                        if (templateText != null)
                        {
                            customPromptText.font = templateText.font;
                            customPromptText.fontSize = templateText.fontSize;
                            customPromptText.color = templateText.color;
                            customPromptText.alignment = templateText.alignment;
                            customPromptText.fontStyle = templateText.fontStyle;
                            customPromptText.outlineWidth = templateText.outlineWidth;
                            customPromptText.outlineColor = templateText.outlineColor;
                            
                            // Copy rect transform settings but override width
                            var templateTextRect = templateText.GetComponent<RectTransform>();
                            var textRect = textObj.GetComponent<RectTransform>();
                            textRect.anchorMin = templateTextRect.anchorMin;
                            textRect.anchorMax = templateTextRect.anchorMax;
                            textRect.anchoredPosition = templateTextRect.anchoredPosition;
                            textRect.sizeDelta = new Vector2(350, templateTextRect.sizeDelta.y); // Force wider text
                            textRect.pivot = templateTextRect.pivot;
                            
                        }
                        
                        customPromptText.text = "Open Backpack (Shift+E)";
                    }
                    
                    // Start hidden
                    customPromptContainer.SetActive(false);
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error creating custom prompt: {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(GUIManager), "UpdateThrow")]
        public class GUIManager_UpdateThrow_Patch
        {
            static bool Prefix(GUIManager __instance)
            {
                try
                {
                    var localCharacter = Character.localCharacter;
                    if (localCharacter == null) return true;

                    var currentItem = localCharacter.data?.currentItem;
                    if (currentItem != null && currentItem.GetType().Name == "Backpack")
                    {
                        var keyHeldTime = BackpackPatches.keyHeldTime;
                        
                        // Show progress only when Shift is held
                        bool shiftHeld = UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftShift) || 
                                        UnityEngine.Input.GetKey(UnityEngine.KeyCode.RightShift);

                        if (keyHeldTime > 0f && shiftHeld)
                        {
                            ShowHoldProgress(__instance, keyHeldTime / PluginConfig.holdTime.Value);
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error in UpdateThrow prefix patch: {ex.Message}");
                }
                
                return true;
            }

            static void ShowHoldProgress(GUIManager guiManager, float progress)
            {
                if (guiManager.throwGO != null && guiManager.throwBar != null)
                {
                    guiManager.throwGO.SetActive(true);
                    var fillProp = guiManager.throwBar.GetType().GetProperty("fillAmount");
                    fillProp?.SetValue(guiManager.throwBar, Mathf.Clamp01(progress));
                    
                    var colorProp = guiManager.throwBar.GetType().GetProperty("color");
                    colorProp?.SetValue(guiManager.throwBar, Color.cyan);
                }
            }
        }
    }
}
