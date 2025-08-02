using HarmonyLib;
using UnityEngine;
using System;
using System.Reflection;
using BepInEx.Logging;

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

        [HarmonyPatch]
        public class Character_Update_Patch
        {
            // Dynamic method resolution since we don't have the exact type
            static MethodBase TargetMethod()
            {
                var characterType = AccessTools.TypeByName("Character");
                if (characterType == null)
                {
                    Logger.LogError("Could not find Character type!");
                    return null;
                }
                return AccessTools.Method(characterType, "Update");
            }

            static void Postfix(object __instance)
            {
                try
                {
                    if (__instance == null) return;

                    // Get localCharacter field
                    var localCharacterField = AccessTools.Field(__instance.GetType(), "localCharacter");
                    var localCharacter = localCharacterField?.GetValue(null);
                    if (localCharacter == null || localCharacter != __instance) return;

                    // Get character data
                    var dataField = AccessTools.Field(__instance.GetType(), "data");
                    var data = dataField?.GetValue(__instance);
                    if (data == null) return;

                    // Check if holding backpack
                    var currentItemField = AccessTools.Field(data.GetType(), "currentItem");
                    var currentItem = currentItemField?.GetValue(data);
                    if (currentItem == null || currentItem.GetType().Name != "Backpack") return;

                    // Handle backpack opening logic
                    HandleBackpackInput(__instance, data);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error in Character_Update_Patch: {ex.Message}");
                }
            }

            private static void HandleBackpackInput(object character, object characterData)
            {
                // Check if character is sprinting
                var isSprintingField = AccessTools.Field(characterData.GetType(), "isSprinting");
                bool isSprinting = (bool)(isSprintingField?.GetValue(characterData) ?? false);
                
                if (isSprinting)
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
                        OpenBackpackWheel(character);
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

            private static void OpenBackpackWheel(object character)
            {
                try
                {
                    var guiManagerType = AccessTools.TypeByName("GUIManager");
                    var instanceProp = AccessTools.Property(guiManagerType, "Instance");
                    var guiManager = instanceProp?.GetValue(null);

                    if (guiManager == null)
                    {
                        Logger.LogError("Could not get GUIManager instance");
                        return;
                    }

                    var method = AccessTools.Method(guiManagerType, "OpenBackpackWheel");
                    if (method == null)
                    {
                        Logger.LogError("Could not find OpenBackpackWheel method");
                        return;
                    }

                    Logger.LogInfo("Opening backpack wheel!");
                    method.Invoke(guiManager, new object[] { true });
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error opening backpack wheel: {ex.Message}");
                }
            }
        }

        [HarmonyPatch]
        public class BackpackWheel_Update_Patch
        {
            static MethodBase TargetMethod()
            {
                var type = AccessTools.TypeByName("BackpackWheel");
                if (type == null)
                {
                    Logger.LogError("Could not find BackpackWheel type!");
                    return null;
                }
                return AccessTools.Method(type, "Update");
            }

            static bool Prefix(object __instance)
            {
                if (isOpeningBackpack && openBackpackGracePeriod > 0f)
                {
                    // During grace period, prevent immediate closure
                    return true;
                }
                return true;
            }
        }
    }
}