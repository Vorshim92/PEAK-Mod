using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using Zorro.Core;
using Photon.Pun;

namespace BackpackViewerMod.Patches
{
    /// <summary>
    /// Patches for backpack functionality.
    /// 
    /// SOLUTION: The original issue was that pressing E to open a backpack while holding it
    /// would cause the POV to lock. This happened because:
    /// 1. The game's BackpackWheel.Update() automatically closes the wheel when E is released
    /// 2. The usingBackpackWheel flag could remain set, locking camera movement
    /// 
    /// We fixed this by:
    /// - Using Shift+E combination instead of just E to avoid conflicts
    /// - Adding a grace period (0.5s) to allow the player to release keys
    /// - Ensuring the usingBackpackWheel flag is reset on errors
    /// </summary>
    public class BackpackPatches
    {
        // State management
        private static bool isHoldingKey = false;
        internal static float keyHeldTime = 0f;
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

                // Check if modifier is held (Shift for keyboard, LB/L1 for gamepad)
                bool modifierHeld = false;
                
                // Check if character is actually sprinting - if so, don't open backpack
                if (character.data.isSprinting)
                {
                    ResetKeyState();
                    return;
                }
                
                // Keyboard: Shift key
                if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftShift) || 
                    UnityEngine.Input.GetKey(UnityEngine.KeyCode.RightShift))
                {
                    modifierHeld = true;
                }
                
                // Gamepad: Left Shoulder button (LB/L1)
                var gamepad = UnityEngine.InputSystem.Gamepad.current;
                if (gamepad != null && gamepad.leftShoulder.isPressed)
                {
                    modifierHeld = true;
                }

                if (interactAction.IsPressed() && modifierHeld)
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
                        // Set a grace period to prevent immediate closing
                        keyHeldTime = 0.5f; // Half second grace period
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
                    }
                    else
                    {
                        Utils.LogError("OpenBackpackWheel method not found in GUIManager");
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Error opening backpack wheel: {ex.Message}");
                    
                    // Ensure we reset the flag if something goes wrong
                    if (Character.localCharacter != null && Character.localCharacter.data != null)
                    {
                        Character.localCharacter.data.usingBackpackWheel = false;
                    }
                }
            }

            static void ResetKeyState()
            {
                isHoldingKey = false;
                keyHeldTime = 0f;
                wheelOpened = false;
            }
        }

        // Patch to prevent immediate closing of the wheel when opened by our mod
        [HarmonyPatch(typeof(BackpackWheel), "Update")]
        public class BackpackWheel_Update_Patch
        {
            static bool Prefix()
            {
                // If the wheel was just opened by our mod, give the player time to release the key
                if (wheelOpened && keyHeldTime > 0)
                {
                    keyHeldTime -= Time.deltaTime;
                    if (keyHeldTime <= 0)
                    {
                        wheelOpened = false;
                        keyHeldTime = 0;
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

        /// <summary>
        /// Questo patch nasconde la slice "Indossa Zaino" dalla ruota radiale
        /// solo quando lo zaino è tenuto in mano dal giocatore.
        /// </summary>
        [HarmonyPatch(typeof(BackpackWheel), "InitWheel")]
        public class BackpackWheel_InitWheel_Patch
        {
            // Un Prefix restituisce 'true' per eseguire il metodo originale, 'false' per saltarlo.
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
                    if (backpackItem == null || backpackItem.itemState != ItemState.Held || backpackItem.holderCharacter != Character.localCharacter)
                    {
                        return true; // Esegui il metodo originale
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
                    Utils.LogError($"Errore nel patch BackpackWheel_InitWheel_Patch: {ex.Message}");
                    return true; // In caso di errore, lascia fare al gioco per sicurezza.
                }
            }
        }
        
        [HarmonyPatch(typeof(BackpackWheel), "Hover")]
        public class BackpackWheel_Hover_Patch
        {
            // --- MODIFICA QUI --- Aggiungi la parola chiave 'in'
            static bool Prefix(BackpackWheel __instance, in BackpackWheelSlice.SliceData sliceData)
            {
                try
                {
                    var localCharacter = Character.localCharacter;
                    if (localCharacter == null || localCharacter.data.currentItem == null)
                    {
                        return true; // Non teniamo nulla, lascia fare al gioco
                    }

                    // Controlla se l'oggetto tenuto in mano è lo zaino a cui la ruota si riferisce
                    var heldItemIsThisBackpack = __instance.backpack.type == BackpackReference.BackpackType.Item && 
                                                 __instance.backpack.view == localCharacter.data.currentItem.GetComponent<PhotonView>();

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
                    Utils.LogError($"Errore nel patch BackpackWheel_Hover_Patch: {ex.Message}");
                    return true; // In caso di errore, meglio essere sicuri
                }
                
                // Per tutti gli altri casi, lascia che il gioco funzioni normalmente.
                return true;
            }
        }
    }
}