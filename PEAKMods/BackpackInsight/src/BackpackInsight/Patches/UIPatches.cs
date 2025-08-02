using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Reflection;
using TMPro;
using BepInEx.Logging;
using BackpackInsight.Components;
using Zorro.ControllerSupport;

namespace BackpackInsight.Patches
{
    /// <summary>
    /// UI patches to show custom prompts for backpack interactions
    /// </summary>
    public class UIPatches
    {
        private static readonly ManualLogSource Logger = Plugin.Log;
        
        private static BackpackPromptUI customPrompt = null;

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
                    if (!Plugin.ModConfig.EnablePrompts.Value)
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
                        if (customPrompt == null)
                        {
                            CreateCustomPrompt(__instance);
                        }
                        
                        if (customPrompt != null)
                        {
                            customPrompt.Show();
                        }
                    }
                    else
                    {
                        if (customPrompt != null)
                        {
                            customPrompt.Hide();
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
                    
                    // Create BackpackPromptUI using PEAKLib patterns
                    var promptGO = new GameObject("BackpackInsightPrompt");
                    customPrompt = promptGO.AddComponent<BackpackPromptUI>();
                    customPrompt.Initialize(templatePrompt.transform.parent);
                    
                    // Position below the main prompt
                    var templateRect = templatePrompt.GetComponent<RectTransform>();
                    customPrompt.SetPosition(templateRect.anchoredPosition + new Vector2(0, -60));
                    
                    Logger.LogInfo("BackpackPromptUI created successfully with PEAKLib");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error creating custom prompt: {ex.Message}");
                }
            }

            // UpdatePromptText is now handled inside BackpackPromptUI
        }
    }
}