using HarmonyLib;
using UnityEngine;
using System;
using System.Reflection;
using System.Linq;
using TMPro;

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
        private static string originalPromptText = null;
        private static bool isShowingCustomPrompt = false;

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
                        // Since the game doesn't show prompts for held items,
                        // we'll use the primary prompt and show it manually
                        if (!isShowingCustomPrompt)
                        {
                            __instance.interactPromptPrimary.SetActive(true);
                            
                            if (__instance.interactPromptText != null)
                            {
                                originalPromptText = __instance.interactPromptText.text;
                                __instance.interactPromptText.text = "Open Backpack (Shift+E)";
                                isShowingCustomPrompt = true;
                            }
                        }
                    }
                    else if (isShowingCustomPrompt)
                    {
                        // Restore and hide
                        if (__instance.interactPromptText != null && originalPromptText != null)
                        {
                            __instance.interactPromptText.text = originalPromptText;
                        }
                        __instance.interactPromptPrimary.SetActive(false);
                        isShowingCustomPrompt = false;
                        originalPromptText = null;
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error in GUIManager_LateUpdate_Patch: {ex.Message}");
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
