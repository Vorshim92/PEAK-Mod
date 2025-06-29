using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace BackpackViewerMod.Patches
{
    public class BackpackPatches
    {
        // State management
        private static bool isHoldingKey = false;
        private static float keyHeldTime = 0f;
        private static bool wheelOpened = false;

        // Main patch to add backpack viewing functionality
        [HarmonyPatch(typeof(CharacterItems), "Update")]
        public class CharacterItems_Update_Patch
        {
            static void Postfix(CharacterItems __instance, Character ___character)
            {
                try
                {
                    if (___character == null || !___character.IsLocal)
                        return;

                    var currentItem = ___character.data?.currentItem;
                    
                    if (currentItem != null && currentItem.GetType().Name == "Backpack")
                    {
                        HandleBackpackInHand(currentItem, ___character);
                    }
                    else
                    {
                        ResetKeyState();
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error in CharacterItems_Update_Patch: {ex.Message}");
                }
            }
            
            static void HandleBackpackInHand(Item backpackItem, Character character)
            {
                var guiManager = GUIManager.instance;
                if (guiManager == null || guiManager.windowBlockingInput || guiManager.wheelActive)
                {
                    ResetKeyState();
                    return;
                }
                
                if (character.data.passedOut || character.data.fullyPassedOut || 
                    character.data.dead || character.data.isClimbingAnything)
                {
                    ResetKeyState();
                    return;
                }
                
                if (PluginConfig.useHoldMethod.Value)
                {
                    HandleHoldMethod(backpackItem, character);
                }
            }
            
            static void HandleHoldMethod(Item backpackItem, Character character)
            {
                var interactAction = GetInteractAction();
                if (interactAction == null)
                {
                    Utils.LogWarning("Interact action not found");
                    return;
                }

                if (interactAction.IsPressed())
                {
                    if (!isHoldingKey)
                    {
                        isHoldingKey = true;
                        keyHeldTime = 0f;
                    }
                    
                    keyHeldTime += Time.deltaTime;
                    
                    if (keyHeldTime >= PluginConfig.holdTime.Value && !wheelOpened)
                    {
                        OpenBackpackWheel(backpackItem, character);
                        wheelOpened = true;
                    }
                }
                else
                {
                    ResetKeyState();
                }
            }
            
            static InputAction GetInteractAction()
            {
                try
                {
                    var field = typeof(CharacterInput).GetField("action_interact", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    
                    if (field != null)
                    {
                        return field.GetValue(null) as InputAction;
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Failed to get interact action: {ex.Message}");
                }
                
                return null;
            }
            
            static void OpenBackpackWheel(Item backpackItem, Character character)
            {
                try
                {
                    var backpackRefType = backpackItem.GetType().Assembly.GetType("BackpackReference");
                    if (backpackRefType == null)
                    {
                        Utils.LogError("BackpackReference type not found");
                        return;
                    }
                    
                    var getFromBackpackMethod = backpackRefType.GetMethod("GetFromBackpackItem", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    
                    if (getFromBackpackMethod == null)
                    {
                        Utils.LogError("GetFromBackpackItem method not found");
                        return;
                    }
                    
                    var backpackRef = getFromBackpackMethod.Invoke(null, new object[] { backpackItem });
                    
                    var openMethod = typeof(GUIManager).GetMethod("OpenBackpackWheel");
                    if (openMethod != null)
                    {
                        openMethod.Invoke(GUIManager.instance, new object[] { backpackRef });
                        Utils.LogInfo("Opened backpack wheel for held backpack");
                    }
                    else
                    {
                        Utils.LogError("OpenBackpackWheel method not found");
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error opening backpack wheel: {ex.Message}");
                }
            }
            
            static void ResetKeyState()
            {
                isHoldingKey = false;
                keyHeldTime = 0f;
                wheelOpened = false;
            }
        }

        [HarmonyPatch]
        public class Backpack_Secondary_Patches
        {
            static bool Prepare()
            {
                return PluginConfig.useSecondaryAction.Value;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(Item), "CanUseSecondary")]
            static void CanUseSecondary_Postfix(ref bool __result, Item __instance)
            {
                try
                {
                    if (__instance.GetType().Name == "Backpack" && __instance.itemState == ItemState.Held)
                    {
                        __result = true;
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error in CanUseSecondary patch: {ex.Message}");
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Item), "StartUseSecondary")]
            static bool StartUseSecondary_Prefix(Item __instance)
            {
                try
                {
                    if (__instance.GetType().Name == "Backpack" && 
                        __instance.itemState == ItemState.Held && 
                        __instance.holderCharacter != null)
                    {
                        var backpackRefType = __instance.GetType().Assembly.GetType("BackpackReference");
                        var getFromBackpackMethod = backpackRefType?.GetMethod("GetFromBackpackItem", 
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        
                        if (getFromBackpackMethod != null)
                        {
                            var backpackRef = getFromBackpackMethod.Invoke(null, new object[] { __instance });
                            
                            var openMethod = typeof(GUIManager).GetMethod("OpenBackpackWheel");
                            if (openMethod != null)
                            {
                                openMethod.Invoke(GUIManager.instance, new object[] { backpackRef });
                                return false;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error in StartUseSecondary patch: {ex.Message}");
                }
                
                return true;
            }
        }
    }
}