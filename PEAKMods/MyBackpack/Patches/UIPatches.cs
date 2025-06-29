using HarmonyLib;
using UnityEngine;
using System;
using System.Reflection;

namespace BackpackViewerMod.Patches
{
    public class UIPatches
    {
        // Questo patch si aggancia al metodo che aggiorna i prompt degli oggetti in basso a destra.
        [HarmonyPatch(typeof(GUIManager), "UpdateItemPrompts")]
        public class GUIManager_UpdateItemPrompts_Patch
        {
            static void Postfix(GUIManager __instance)
            {
                try
                {
                    var localCharacter = Character.localCharacter;
                    if (localCharacter == null || localCharacter.data?.currentItem == null)
                        return;

                    var currentItem = localCharacter.data.currentItem;
                    
                    // Controlliamo se l'oggetto in mano è uno zaino
                    if (currentItem.GetType().Name == "Backpack" && currentItem.itemState == ItemState.Held)
                    {
                        // Se l'opzione "Hold Method" è attiva, mostriamo il relativo prompt.
                        if (PluginConfig.useHoldMethod.Value && __instance.itemPromptMain != null)
                        {
                            // Attiviamo il prompt principale e gli diamo il nostro testo.
                            __instance.itemPromptMain.gameObject.SetActive(true);
                            __instance.itemPromptMain.text = "Hold to Open"; 
                        }

                        // LA PARTE PER L'AZIONE SECONDARIA E' STATA RIMOSSA
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error in UpdateItemPrompts_Patch: {ex.Message}");
                }
            }
        }


        // Questo patch va mantenuto, serve a mostrare la barra di progresso circolare
        // quando si tiene premuto il tasto "E".
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