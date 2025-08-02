using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Reflection;
using BepInEx.Logging;
using Zorro.Core;
using Photon.Pun;

namespace BackpackInsight.Patches
{
    /// <summary>
    /// Core patches for backpack functionality
    /// </summary>
    public class BackpackPatches
    {
        private static readonly ManualLogSource Logger = Plugin.Log;
        
        // Track key hold state
        private static float keyHeldTime = 0f;
        private static bool wasEPressed = false;
        private static bool wasModifierPressed = false;
        private static bool isOpeningBackpack = false;
        private static float openBackpackGracePeriod = 0f;

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
                        HandleBackpackInput(currentItem, ___character);
                    }
                    else
                    {
                        ResetKeyState();
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error in Character_Update_Patch: {ex.Message}");
                }
            }

            private static void HandleBackpackInput(Item backpackItem, Character character)
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

                // Check if character is sprinting
                if (character.data.isSprinting)
                {
                    ResetKeyState();
                    return;
                }

                // Check for modifier key (Shift for keyboard, LB/L1 for gamepad)
                bool modifierHeld = false;
                
                // Keyboard: Shift key
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    modifierHeld = true;
                }
                
                // Gamepad: Left Shoulder button
                var gamepad = UnityEngine.InputSystem.Gamepad.current;
                if (gamepad != null && gamepad.leftShoulder.isPressed)
                {
                    modifierHeld = true;
                }

                // Check for E key (keyboard) or X/Square button (gamepad)
                bool interactPressed = Input.GetKey(KeyCode.E);
                if (gamepad != null)
                {
                    interactPressed = interactPressed || gamepad.buttonWest.isPressed;
                }

                // Handle grace period
                if (openBackpackGracePeriod > 0f)
                {
                    openBackpackGracePeriod -= Time.deltaTime;
                    if (openBackpackGracePeriod <= 0f)
                    {
                        isOpeningBackpack = false;
                    }
                }

                // Track key state changes
                bool isModifierJustPressed = modifierHeld && !wasModifierPressed;
                bool isEJustPressed = interactPressed && !wasEPressed;

                // Update key hold time
                if (modifierHeld && interactPressed && !isOpeningBackpack)
                {
                    keyHeldTime += Time.deltaTime;
                    
                    var holdTimeRequired = Plugin.Instance.Config.Bind("General", "HoldTime", 0.3f).Value;
                    
                    if (keyHeldTime >= holdTimeRequired)
                    {
                        OpenBackpackWheel(backpackItem, character);
                        isOpeningBackpack = true;
                        openBackpackGracePeriod = 0.5f;
                        keyHeldTime = 0f;
                    }
                }
                else if (!modifierHeld || !interactPressed)
                {
                    if (!isOpeningBackpack)
                    {
                        keyHeldTime = 0f;
                    }
                }

                wasEPressed = interactPressed;
                wasModifierPressed = modifierHeld;
            }

            private static void ResetKeyState()
            {
                keyHeldTime = 0f;
                wasEPressed = false;
                wasModifierPressed = false;
                isOpeningBackpack = false;
                openBackpackGracePeriod = 0f;
            }

            private static void OpenBackpackWheel(Item backpackItem, Character character)
            {
                try
                {
                    var backpackRefType = backpackItem.GetType().Assembly.GetType("BackpackReference");
                    if (backpackRefType == null)
                    {
                        Logger.LogError("BackpackReference type not found");
                        return;
                    }

                    var getFromBackpackMethod = backpackRefType.GetMethod("GetFromBackpackItem",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                    if (getFromBackpackMethod == null)
                    {
                        Logger.LogError("GetFromBackpackItem method not found");
                        return;
                    }

                    var backpackRef = getFromBackpackMethod.Invoke(null, new object[] { backpackItem });

                    var openMethod = typeof(GUIManager).GetMethod("OpenBackpackWheel");
                    if (openMethod != null)
                    {
                        Logger.LogInfo("Opening backpack wheel!");
                        openMethod.Invoke(GUIManager.instance, new object[] { backpackRef });
                    }
                    else
                    {
                        Logger.LogError("OpenBackpackWheel method not found in GUIManager");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error opening backpack wheel: {ex.Message}");
                    
                    // Ensure we reset the flag if something goes wrong
                    if (Character.localCharacter != null && Character.localCharacter.data != null)
                    {
                        Character.localCharacter.data.usingBackpackWheel = false;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(BackpackWheel), "Update")]
        public class BackpackWheel_Update_Patch
        {

            static bool Prefix(object __instance)
            {
                // If the wheel was just opened by our mod, give the player time to release the key
                if (isOpeningBackpack && openBackpackGracePeriod > 0f)
                {
                    openBackpackGracePeriod -= Time.deltaTime;
                    if (openBackpackGracePeriod <= 0f)
                    {
                        isOpeningBackpack = false;
                        openBackpackGracePeriod = 0f;
                    }
                    
                    // Skip the original update logic that would close the wheel
                    if (!Character.localCharacter.input.interactIsPressed)
                    {
                        return true; // Let it run normally now
                    }
                }
                
                return true; // Run the original method
            }
        }
    }
}