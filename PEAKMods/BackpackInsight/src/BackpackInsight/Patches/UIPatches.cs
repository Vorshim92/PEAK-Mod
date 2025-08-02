using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Reflection;
using TMPro;
using BepInEx.Logging;

namespace BackpackInsight.Patches
{
    /// <summary>
    /// UI patches to show custom prompts for backpack interactions
    /// </summary>
    public class UIPatches
    {
        private static readonly ManualLogSource Logger = Plugin.Log;
        
        private static GameObject customPromptContainer = null;
        private static TextMeshProUGUI customPromptText = null;

        [HarmonyPatch]
        public class GUIManager_LateUpdate_Patch
        {
            static MethodBase TargetMethod()
            {
                var type = AccessTools.TypeByName("GUIManager");
                if (type == null)
                {
                    Logger.LogError("Could not find GUIManager type!");
                    return null;
                }
                return AccessTools.Method(type, "LateUpdate");
            }

            static void Postfix(object __instance)
            {
                try
                {
                    // Check if prompts are enabled in config
                    if (!Plugin.Instance.Config.Bind("General", "EnablePrompts", true).Value)
                        return;

                    var characterType = AccessTools.TypeByName("Character");
                    var localCharacterProp = AccessTools.Property(characterType, "localCharacter");
                    var localCharacter = localCharacterProp?.GetValue(null);
                    if (localCharacter == null) return;

                    var dataField = AccessTools.Field(localCharacter.GetType(), "data");
                    var data = dataField?.GetValue(localCharacter);
                    if (data == null) return;

                    var currentItemField = AccessTools.Field(data.GetType(), "currentItem");
                    var currentItem = currentItemField?.GetValue(data);
                    bool holdingBackpack = currentItem != null && currentItem.GetType().Name == "Backpack";
                    
                    // Check UI blocking conditions
                    var wheelActiveField = AccessTools.Field(__instance.GetType(), "wheelActive");
                    var windowBlockingInputField = AccessTools.Field(__instance.GetType(), "windowBlockingInput");
                    bool wheelActive = (bool)(wheelActiveField?.GetValue(__instance) ?? false);
                    bool windowBlockingInput = (bool)(windowBlockingInputField?.GetValue(__instance) ?? false);
                    
                    var throwGOField = AccessTools.Field(__instance.GetType(), "throwGO");
                    var throwGO = throwGOField?.GetValue(__instance) as GameObject;
                    
                    bool shouldShow = holdingBackpack && !wheelActive && !windowBlockingInput;

                    if (shouldShow && !(throwGO != null && throwGO.activeSelf))
                    {
                        if (customPromptContainer == null)
                        {
                            CreateCustomPrompt(__instance);
                        }
                        
                        if (customPromptContainer != null && !customPromptContainer.activeSelf)
                        {
                            customPromptContainer.SetActive(true);
                            UpdatePromptText();
                        }
                    }
                    else
                    {
                        if (customPromptContainer != null && customPromptContainer.activeSelf)
                        {
                            customPromptContainer.SetActive(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error in GUIManager_LateUpdate_Patch: {ex.Message}");
                }
            }
            
            static void CreateCustomPrompt(object guiManager)
            {
                try
                {
                    var interactPromptPrimaryField = AccessTools.Field(guiManager.GetType(), "interactPromptPrimary");
                    var templatePrompt = interactPromptPrimaryField?.GetValue(guiManager) as GameObject;
                    if (templatePrompt == null) 
                    {
                        Logger.LogError("Template prompt is null!");
                        return;
                    }
                    
                    // Create container
                    customPromptContainer = new GameObject("BackpackModPrompt");
                    customPromptContainer.transform.SetParent(templatePrompt.transform.parent, false);
                    
                    var rectTransform = customPromptContainer.AddComponent<RectTransform>();
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    
                    var templateRect = templatePrompt.GetComponent<RectTransform>();
                    rectTransform.anchoredPosition = templateRect.anchoredPosition + new Vector2(0, -50);
                    rectTransform.sizeDelta = new Vector2(400, 30);
                    
                    // Clone key icon and text structure
                    var templateChildren = templatePrompt.GetComponentsInChildren<Transform>();
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
                    
                    if (keyIconObj != null)
                    {
                        var clonedIcon = GameObject.Instantiate(keyIconObj, customPromptContainer.transform);
                        clonedIcon.name = "KeyIcon";
                    }
                    
                    if (textObj != null)
                    {
                        var clonedText = GameObject.Instantiate(textObj, customPromptContainer.transform);
                        clonedText.name = "Text";
                        customPromptText = clonedText.GetComponent<TextMeshProUGUI>();
                        customPromptText.text = "Open Backpack (Shift+E)";
                        
                        var textRect = clonedText.GetComponent<RectTransform>();
                        textRect.pivot = new Vector2(0f, 0.5f);
                        textRect.anchorMin = new Vector2(0f, 0.5f);
                        textRect.anchorMax = new Vector2(0f, 0.5f);
                        textRect.sizeDelta = new Vector2(350, textRect.sizeDelta.y);
                        textRect.anchoredPosition = new Vector2(60f, -2f);
                        
                        customPromptText.overflowMode = TextOverflowModes.Overflow;
                        customPromptText.textWrappingMode = TextWrappingModes.NoWrap;
                        customPromptText.alignment = TextAlignmentOptions.Center;
                    }
                    
                    customPromptContainer.SetActive(false);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error creating custom prompt: {ex.Message}");
                }
            }

            static void UpdatePromptText()
            {
                if (customPromptText == null) return;

                try
                {
                    // Check input scheme using reflection
                    var inputHandlerType = AccessTools.TypeByName("Zorro.ControllerSupport.InputHandler");
                    if (inputHandlerType != null)
                    {
                        var getCurrentSchemeMethod = AccessTools.Method(inputHandlerType, "GetCurrentUsedInputScheme");
                        if (getCurrentSchemeMethod != null)
                        {
                            var currentScheme = getCurrentSchemeMethod.Invoke(null, null);
                            if (currentScheme != null && currentScheme.ToString() == "Gamepad")
                            {
                                var getGamepadTypeMethod = AccessTools.Method(inputHandlerType, "GetGamepadType");
                                if (getGamepadTypeMethod != null)
                                {
                                    var gamepadType = getGamepadTypeMethod.Invoke(null, null);
                                    if (gamepadType != null)
                                    {
                                        string gamepadTypeName = gamepadType.ToString();
                                        if (gamepadTypeName == "Dualshock" || gamepadTypeName == "Dualsense")
                                        {
                                            customPromptText.text = "Open Backpack (L1+□)";
                                        }
                                        else
                                        {
                                            customPromptText.text = "Open Backpack (LB+X)";
                                        }
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error updating prompt text: {ex.Message}");
                }

                // Default to keyboard prompt
                customPromptText.text = "Open Backpack (Shift+E)";
            }
        }
    }
}