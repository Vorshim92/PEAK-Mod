using HarmonyLib;
using UnityEngine;
using UnityEngine.UI; // NECESSARIO per Image
using System.Collections.Generic;
using System;

namespace BackpackViewerMod.Patches
{
    public class BackpackUISlotsPatches
    {
        // Lista che conterrà i componenti Image delle nostre icone, non l'intero InventoryItemUI.
        private static readonly List<Image> backpackIconImages = new List<Image>();
        // Lista per i GameObject degli slot, per poterli nascondere/mostrare.
        private static readonly List<GameObject> backpackSlotObjects = new List<GameObject>();
        
        private static bool isInitialized = false;

        [HarmonyPatch(typeof(GUIManager), "Start")]
        public class GUIManager_Start_Patch
        {
            static void Postfix(GUIManager __instance)
            {
                if (isInitialized) return;

                try
                {
                    Utils.LogInfo("Creazione degli slot UI per lo zaino (metodo pulito)...");
                    
                    // Genitore: la canvas principale della UI.
                    Transform parentCanvas = __instance.hudCanvas.transform;
                    
                    for (int i = 0; i < 4; i++)
                    {
                        // 1. Creiamo un GameObject per lo slot (lo sfondo)
                        GameObject slotBg = new GameObject($"BackpackSlot_BG_{i}");
                        slotBg.transform.SetParent(parentCanvas, false);

                        Image bgImage = slotBg.AddComponent<Image>();
                        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f); // Sfondo scuro semi-trasparente

                        RectTransform bgRect = slotBg.GetComponent<RectTransform>();
                        
                        // --- Posizionamento in alto a destra ---
                        bgRect.anchorMin = new Vector2(1, 1); // Ancora in alto a destra
                        bgRect.anchorMax = new Vector2(1, 1); // Ancora in alto a destra
                        bgRect.pivot = new Vector2(1, 1);     // Il punto di rotazione/scala è l'angolo in alto a destra
                        bgRect.sizeDelta = new Vector2(60, 60); // Dimensione dello slot
                        
                        // Posizione: dall'angolo, sposta a sinistra e in basso.
                        // -10 di padding dal bordo.
                        // i * 65 per creare la colonna verticale.
                        bgRect.anchoredPosition = new Vector2(-10, -10 - (i * 65));

                        // 2. Creiamo un GameObject per l'icona dell'oggetto, come figlio dello sfondo
                        GameObject iconObj = new GameObject($"BackpackSlot_Icon_{i}");
                        iconObj.transform.SetParent(slotBg.transform, false);
                        
                        Image iconImage = iconObj.AddComponent<Image>();
                        iconImage.color = Color.white;
                        
                        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
                        iconRect.anchorMin = new Vector2(0, 0); // Ancora agli angoli del genitore (lo sfondo)
                        iconRect.anchorMax = new Vector2(1, 1);
                        iconRect.pivot = new Vector2(0.5f, 0.5f);
                        iconRect.sizeDelta = new Vector2(-10, -10); // Un po' più piccolo dello sfondo per creare un bordo
                        
                        // Aggiungiamo alla nostra lista
                        backpackSlotObjects.Add(slotBg);
                        backpackIconImages.Add(iconImage);
                        
                        slotBg.SetActive(false); // Inizialmente nascosto
                    }
                    
                    isInitialized = true;
                    Utils.LogInfo($"Creati {backpackSlotObjects.Count} slot UI puliti per lo zaino.");
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Errore durante la creazione degli slot UI per lo zaino: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        [HarmonyPatch(typeof(GUIManager), "UpdateItems")]
        public class GUIManager_UpdateItems_Patch
        {
            static void Postfix(GUIManager __instance)
            {
                if (!isInitialized || backpackSlotObjects.Count == 0) return;
                
                try
                {
                    var localPlayer = Character.localCharacter?.player;
                    if (localPlayer == null) return;

                    bool hasBackpack = !localPlayer.backpackSlot.IsEmpty();

                    // Gestire la visibilità
                    for (int i = 0; i < backpackSlotObjects.Count; i++)
                    {
                        if (backpackSlotObjects[i].activeSelf != hasBackpack)
                            backpackSlotObjects[i].SetActive(hasBackpack);
                    }

                    // Sincronizzare le icone
                    if (hasBackpack)
                    {
                        ItemInstanceData backpackInstanceData = localPlayer.backpackSlot.data;
                        BackpackData backpackData;

                        if (backpackInstanceData != null && backpackInstanceData.TryGetDataEntry(DataEntryKey.BackpackData, out backpackData))
                        {
                            for (int i = 0; i < backpackIconImages.Count; i++)
                            {
                                if (i < backpackData.itemSlots.Length)
                                {
                                    ItemSlot currentSlotData = backpackData.itemSlots[i];
                                    Image currentIconImage = backpackIconImages[i];

                                    if (currentSlotData.IsEmpty())
                                    {
                                        currentIconImage.enabled = false;
                                    }
                                    else
                                    {
                                        currentIconImage.enabled = true;
                                        // Dobbiamo convertire la Texture2D dell'item in uno Sprite.
                                        Texture2D itemTexture = currentSlotData.prefab.UIData.icon;
                                        if (itemTexture != null)
                                        {
                                            currentIconImage.sprite = Sprite.Create(itemTexture, new Rect(0, 0, itemTexture.width, itemTexture.height), new Vector2(0.5f, 0.5f));
                                        }
                                        else
                                        {
                                            currentIconImage.enabled = false;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError($"Errore durante l'aggiornamento degli slot UI dello zaino: {ex.Message}");
                }
            }
        }
    }
}