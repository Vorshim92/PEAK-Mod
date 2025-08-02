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
                    
                    var holdTimeRequired = Plugin.ModConfig.HoldTime.Value;
                    
                    if (keyHeldTime >= holdTimeRequired)
                    {
                        OpenBackpackWheel(backpackItem, character);
                        isOpeningBackpack = true;
                        openBackpackGracePeriod = Plugin.ModConfig.GracePeriod.Value;
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
                    try 
                    {
                        var localChar = AccessTools.PropertyGetter(typeof(Character), "localCharacter")?.Invoke(null, null) as Character;
                        if (localChar?.data != null)
                        {
                            localChar.data.usingBackpackWheel = false;
                        }
                    }
                    catch { }
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
                    try 
                    {
                        var localChar = AccessTools.PropertyGetter(typeof(Character), "localCharacter")?.Invoke(null, null) as Character;
                        if (localChar != null && !localChar.input.interactIsPressed)
                        {
                            return true; // Let it run normally now
                        }
                    }
                    catch { }
                }
                
                return true; // Run the original method
            }
        }

        /// <summary>
        /// Questo patch nasconde la slice "Indossa Zaino" dalla ruota radiale
        /// solo quando lo zaino è tenuto in mano dal giocatore.
        /// </summary>
        [HarmonyPatch(typeof(BackpackWheel), "InitWheel")]
        public class BackpackWheel_InitWheel_Patch
        {
            static bool Prefix(BackpackWheel __instance, in BackpackReference bp)
            {
                try
                {
                    Item backpackItem = null;
                    if (bp.type == BackpackReference.BackpackType.Item)
                    {
                        backpackItem = bp.view.GetComponent<Item>();
                    }

                    // Se lo zaino NON è tenuto in mano dal giocatore, lascia fare al gioco.
                    if (backpackItem == null || backpackItem.itemState != ItemState.Held)
                    {
                        return true; // Esegui il metodo originale
                    }
                    
                    // Check if held by local character
                    var localChar = AccessTools.PropertyGetter(typeof(Character), "localCharacter")?.Invoke(null, null) as Character;
                    if (backpackItem.holderCharacter != localChar)
                    {
                        return true;
                    }

                    // --- SE ARRIVIAMO QUI, LO ZAINO È IN MANO! PRENDIAMO IL CONTROLLO. ---

                    // Eseguiamo manualmente una versione "pulita" di InitWheel.
                    // 1. Imposta lo zaino di riferimento
                    __instance.backpack = bp;
                    __instance.chosenSlice = Zorro.Core.Optionable<BackpackWheelSlice.SliceData>.None;
                    __instance.chosenItemText.text = "";

                    // 2. Inizializza le slice con gli oggetti dentro lo zaino
                    ItemSlot[] itemSlots = bp.GetData().itemSlots;
                    for (byte b = 0; b < itemSlots.Length; b++)
                    {
                        if ((int)b < __instance.slices.Length - 1)
                        {
                            __instance.slices[b + 1].InitItemSlot(new System.ValueTuple<BackpackReference, byte>(bp, b), __instance);
                        }
                    }

                    // 3. Nascondi la slice "indossa"
                    if (__instance.slices != null && __instance.slices.Length > 0)
                    {
                        __instance.slices[0].gameObject.SetActive(false);
                    }

                    // 4. Salta la parte problematica del codice originale!
                    // Non impostiamo __instance.currentlyHeldItem. Questo impedisce di "riporre" lo zaino.
                    __instance.currentlyHeldItem.enabled = false;

                    // 5. Attiva la ruota
                    __instance.gameObject.SetActive(true);

                    // 6. Impedisci al gioco di eseguire il suo InitWheel, che annullerebbe il nostro lavoro.
                    return false; // Salta il metodo originale
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Errore nel patch BackpackWheel_InitWheel_Patch: {ex.Message}");
                    return true; // In caso di errore, lascia fare al gioco per sicurezza.
                }
            }
        }
        
        [HarmonyPatch(typeof(BackpackWheel), "Hover")]
        public class BackpackWheel_Hover_Patch
        {
            static bool Prefix(BackpackWheel __instance, in BackpackWheelSlice.SliceData sliceData)
            {
                try
                {
                    var localChar = AccessTools.PropertyGetter(typeof(Character), "localCharacter")?.Invoke(null, null) as Character;
                    if (localChar == null || localChar.data.currentItem == null)
                    {
                        return true; // Non teniamo nulla, lascia fare al gioco
                    }

                    // Controlla se l'oggetto tenuto in mano è lo zaino a cui la ruota si riferisce
                    var heldItemIsThisBackpack = __instance.backpack.type == BackpackReference.BackpackType.Item && 
                                                 __instance.backpack.view == localChar.data.currentItem.GetComponent<PhotonView>();

                    if (heldItemIsThisBackpack)
                    {
                        // Siamo nel nostro caso speciale.
                        
                        // Se la slice è quella per "indossare", la saltiamo (è comunque invisibile)
                        if(sliceData.isBackpackWear) return true;

                        // Controlla se la slice è uno slot vuoto
                        ItemSlot itemSlot = __instance.backpack.GetData().itemSlots[(int)sliceData.slotID];
                        if (itemSlot.IsEmpty())
                        {
                            // È uno slot vuoto. Non mostriamo nessun testo.
                            __instance.chosenItemText.text = "";
                            __instance.chosenSlice = Zorro.Core.Optionable<BackpackWheelSlice.SliceData>.None;
                            
                            // Impediamo al gioco di eseguire la sua logica e mostrare "stash"
                            return false; 
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Errore nel patch BackpackWheel_Hover_Patch: {ex.Message}");
                    return true; // In caso di errore, meglio essere sicuri
                }
                
                // Per tutti gli altri casi, lascia che il gioco funzioni normalmente.
                return true;
            }
        }
    }
}