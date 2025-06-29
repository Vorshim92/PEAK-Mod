using HarmonyLib;
using UnityEngine;
using System;
using System.Reflection;

namespace BackpackViewerMod.Patches
{
    public class UIPatches
    {
        [HarmonyPatch(typeof(GUIManager), "RefreshInteractablePrompt")]
        public class GUIManager_RefreshInteractablePrompt_Patch
        {
            static void Postfix(GUIManager __instance)
            {
                try
                {
                    if (!PluginConfig.showHoldPrompt.Value)
                        return;

                    var localCharacter = Character.localCharacter;
                    if (localCharacter == null)
                        return;
                    
                    var currentItem = localCharacter.data?.currentItem;
                    if (currentItem != null && currentItem.GetType().Name == "Backpack")
                    {
                        if (!__instance.wheelActive && !__instance.windowBlockingInput)
                        {
                            ShowBackpackPrompt(__instance);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error in RefreshInteractablePrompt patch: {ex.Message}");
                }
            }
            
            static void ShowBackpackPrompt(GUIManager guiManager)
            {
                try
                {
                    var interactPromptHold = guiManager.interactPromptHold;
                    if (interactPromptHold != null)
                    {
                        interactPromptHold.SetActive(true);
                    }
                    
                    var interactPromptText = guiManager.interactPromptText;
                    if (interactPromptText != null)
                    {
                        var textProp = interactPromptText.GetType().GetProperty("text");
                        if (textProp != null)
                        {
                            if (PluginConfig.useHoldMethod.Value)
                            {
                                textProp.SetValue(interactPromptText, "hold to open backpack");
                            }
                            else if (PluginConfig.useSecondaryAction.Value)
                            {
                                textProp.SetValue(interactPromptText, "right click to open");
                            }
                        }
                    }
                    
                    var interactPromptPrimary = guiManager.interactPromptPrimary;
                    if (interactPromptPrimary != null)
                    {
                        interactPromptPrimary.SetActive(true);
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error showing backpack prompt: {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(GUIManager), "UpdateThrow")]
        public class GUIManager_UpdateThrow_Patch
        {
            static void Postfix(GUIManager __instance)
            {
                try
                {
                    var localCharacter = Character.localCharacter;
                    if (localCharacter == null || !PluginConfig.useHoldMethod.Value)
                        return;

                    var currentItem = localCharacter.data?.currentItem;
                    if (currentItem != null && currentItem.GetType().Name == "Backpack")
                    {
                        var keyHeldTime = GetKeyHeldTime();
                        if (keyHeldTime > 0f && keyHeldTime < PluginConfig.holdTime.Value)
                        {
                            ShowHoldProgress(__instance, keyHeldTime / PluginConfig.holdTime.Value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error in UpdateThrow patch: {ex.Message}");
                }
            }

            static float GetKeyHeldTime()
            {
                var field = typeof(BackpackPatches).GetField("keyHeldTime", 
                    BindingFlags.NonPublic | BindingFlags.Static);
                
                if (field != null)
                {
                    return (float)field.GetValue(null);
                }
                
                return 0f;
            }

            static void ShowHoldProgress(GUIManager guiManager, float progress)
            {
                try
                {
                    var throwGO = guiManager.throwGO;
                    var throwBar = guiManager.throwBar;
                    
                    if (throwGO != null && throwBar != null)
                    {
                        throwGO.SetActive(true);
                        
                        var fillProp = throwBar.GetType().GetProperty("fillAmount");
                        if (fillProp != null)
                        {
                            fillProp.SetValue(throwBar, progress);
                        }
                        
                        var colorProp = throwBar.GetType().GetProperty("color");
                        if (colorProp != null)
                        {
                            colorProp.SetValue(throwBar, Color.cyan);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error showing hold progress: {ex.Message}");
                }
            }
        }
    }
}