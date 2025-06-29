using HarmonyLib;
using UnityEngine;
using System;
using System.Reflection;
using TMPro;

namespace BackpackViewerMod.Patches
{
    public class UIPatches
    {
        private static bool promptShownByMod = false;

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
                        __instance.interactPromptHold.SetActive(true);
                        ModifyDescriptionText(__instance.interactPromptHold, "open", "Hold to Open Backpack");
                        promptShownByMod = true;
                    }
                    else if (promptShownByMod)
                    {
                        __instance.interactPromptHold.SetActive(false);
                        ModifyDescriptionText(__instance.interactPromptHold, "hold to open backpack", "open");
                        promptShownByMod = false;
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error in GUIManager_LateUpdate_Patch: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Trova TUTTI i componenti di testo figli e modifica solo quello che corrisponde.
        /// Questo è il metodo corretto per non toccare l'icona della keybind.
        /// </summary>
        /// <param name="promptGameObject">Il contenitore del prompt (es. interactPromptHold)</param>
        /// <param name="textToFind">Il testo da cercare (case-insensitive)</param>
        /// <param name="newText">Il nuovo testo da impostare</param>
        static void ModifyDescriptionText(GameObject promptGameObject, string textToFind, string newText)
        {
            if (promptGameObject == null) return;
            
            var textComponents = promptGameObject.GetComponentsInChildren<TextMeshProUGUI>();
            
            foreach (var textComponent in textComponents)
            {
                if (textComponent.text.ToLower() == textToFind.ToLower())
                {
                    textComponent.text = newText;
                    break;
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

                        if (keyHeldTime > 0f)
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
                    if (fillProp != null) fillProp.SetValue(guiManager.throwBar, Mathf.Clamp01(progress));
                    
                    var colorProp = guiManager.throwBar.GetType().GetProperty("color");
                    if (colorProp != null) colorProp.SetValue(guiManager.throwBar, Color.cyan);
                }
            }
        }
    }
}
