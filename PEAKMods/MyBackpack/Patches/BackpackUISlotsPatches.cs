using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Aggiunto per le etichette con il nome
using System.Collections.Generic;
using System;

namespace BackpackViewerMod.Patches
{
    // Classe helper per contenere gli elementi UI di UN giocatore
    public class PlayerBackpackDisplay
    {
        public GameObject MainContainer { get; set; }
        public TextMeshProUGUI NameLabel { get; set; }
        public List<Image> IconImages { get; } = new List<Image>();
    }

    public class BackpackUISlotsPatches
    {
        // Usiamo un dizionario per tenere traccia della UI di ogni personaggio.
        private static readonly Dictionary<Character, PlayerBackpackDisplay> playerDisplays = new Dictionary<Character, PlayerBackpackDisplay>();
        private static Transform parentCanvas;

        // Si aggancia a LateUpdate, che è il metodo corretto.
        [HarmonyPatch(typeof(GUIManager), "LateUpdate")]
        public class GUIManager_LateUpdate_Patch
        {
            static void Postfix(GUIManager __instance)
            {
                // Se la feature è disabilitata, nascondiamo tutto e usciamo.
                if (!PluginConfig.showPlayerBackpackSlots.Value)
                {
                    if (playerDisplays.Count > 0)
                    {
                        foreach(var display in playerDisplays.Values) GameObject.Destroy(display.MainContainer);
                        playerDisplays.Clear();
                    }
                    return;
                }
                
                // Inizializzazione del canvas, se non già fatto
                if (parentCanvas == null && __instance.hudCanvas != null)
                {
                    parentCanvas = __instance.hudCanvas.transform;
                }
                if (parentCanvas == null) return;

                // 1. Pulisce la UI dei giocatori che si sono disconnessi
                CleanupDisconnectedPlayers();

                // 2. Itera su tutti i personaggi e aggiorna la loro UI
                float totalVerticalOffset = 10f; // Spazio dal bordo superiore dello schermo
                foreach (Character character in Character.AllCharacters)
                {
                    if (character == null || character.player == null) continue;

                    bool isLocalPlayer = character.IsLocal;
                    
                    // Se non è il giocatore locale e l'opzione è disattivata, salta e pulisci
                    if (!isLocalPlayer && !PluginConfig.showOtherPlayerBackpackSlots.Value)
                    {
                        if (playerDisplays.ContainsKey(character))
                        {
                            GameObject.Destroy(playerDisplays[character].MainContainer);
                            playerDisplays.Remove(character);
                        }
                        continue;
                    }

                    // Se il personaggio non ha una UI, creala
                    if (!playerDisplays.ContainsKey(character))
                    {
                        playerDisplays.Add(character, CreateDisplayForPlayer(character));
                    }
                    
                    PlayerBackpackDisplay display = playerDisplays[character];
                    bool hasBackpack = !character.player.backpackSlot.IsEmpty();
                    
                    // Gestisce la visibilità
                    if (display.MainContainer.activeSelf != hasBackpack)
                    {
                        display.MainContainer.SetActive(hasBackpack);
                    }
                    
                    // Se ha uno zaino, posiziona e aggiorna
                    if (hasBackpack)
                    {
                        RectTransform rect = display.MainContainer.GetComponent<RectTransform>();
                        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -totalVerticalOffset);
                        totalVerticalOffset += rect.sizeDelta.y + 10f; // Aggiunge spazio per il prossimo giocatore

                        UpdateDisplayIcons(character, display);
                    }
                }
            }
        }

        private static void CleanupDisconnectedPlayers()
        {
            List<Character> toRemove = new List<Character>();
            foreach (var kvp in playerDisplays)
            {
                if (kvp.Key == null || !Character.AllCharacters.Contains(kvp.Key))
                    toRemove.Add(kvp.Key);
            }
            foreach (var character in toRemove)
            {
                GameObject.Destroy(playerDisplays[character].MainContainer);
                playerDisplays.Remove(character);
            }
        }

        private static PlayerBackpackDisplay CreateDisplayForPlayer(Character character)
        {
            var display = new PlayerBackpackDisplay();
            bool isLocal = character.IsLocal;
            float scale = isLocal ? 1.0f : PluginConfig.otherPlayerSlotsScale.Value;
            
            // Logica di creazione UI (simile a prima, ma più pulita)
            display.MainContainer = new GameObject($"BackpackDisplay_{character.characterName}");
            display.MainContainer.transform.SetParent(parentCanvas, false);
            var mainRect = display.MainContainer.AddComponent<RectTransform>();
            mainRect.anchorMin = new Vector2(1, 1);
            mainRect.anchorMax = new Vector2(1, 1);
            mainRect.pivot = new Vector2(1, 1);
            mainRect.anchoredPosition = new Vector2(-10, -10);

            // Nome
            var labelObj = new GameObject("NameLabel");
            labelObj.transform.SetParent(display.MainContainer.transform, false);
            display.NameLabel = labelObj.AddComponent<TextMeshProUGUI>();
            display.NameLabel.text = character.characterName;
            display.NameLabel.fontSize = 14 * scale;
            display.NameLabel.alignment = TextAlignmentOptions.TopRight;
            display.NameLabel.color = character.refs.customization.PlayerColor;
            var labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(1, 1);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.pivot = new Vector2(1, 1);
            labelRect.sizeDelta = new Vector2(100 * scale, 20 * scale);
            labelRect.anchoredPosition = Vector2.zero;

            // Slot
            float slotSize = 60 * scale;
            for (int i = 0; i < 4; i++)
            {
                var slotBg = new GameObject($"Slot_BG_{i}");
                slotBg.transform.SetParent(display.MainContainer.transform, false);
                slotBg.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
                var bgRect = slotBg.GetComponent<RectTransform>();
                bgRect.anchorMin = new Vector2(1, 1);
                bgRect.anchorMax = new Vector2(1, 1);
                bgRect.pivot = new Vector2(1, 1);
                bgRect.sizeDelta = new Vector2(slotSize, slotSize);
                bgRect.anchoredPosition = new Vector2(0, -labelRect.sizeDelta.y - (i * (slotSize + 5 * scale)));

                var iconObj = new GameObject($"Icon_{i}");
                iconObj.transform.SetParent(slotBg.transform, false);
                var iconImage = iconObj.AddComponent<Image>();
                var iconRect = iconObj.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.sizeDelta = new Vector2(-10 * scale, -10 * scale);
                display.IconImages.Add(iconImage);
            }

            mainRect.sizeDelta = new Vector2(slotSize, labelRect.sizeDelta.y + 4 * slotSize + 3 * (5*scale));
            display.MainContainer.SetActive(false);
            return display;
        }

        private static void UpdateDisplayIcons(Character character, PlayerBackpackDisplay display)
        {
            ItemInstanceData backpackInstanceData = character.player.backpackSlot.data;
            if (backpackInstanceData == null) return;

            if (backpackInstanceData.TryGetDataEntry(DataEntryKey.BackpackData, out BackpackData backpackData))
            {
                for (int i = 0; i < display.IconImages.Count; i++)
                {
                    ItemSlot slotData = backpackData.itemSlots[i];
                    Image icon = display.IconImages[i];
                    if (slotData.IsEmpty())
                    {
                        icon.enabled = false;
                    }
                    else
                    {
                        icon.enabled = true;
                        Texture2D tex = slotData.prefab.UIData.icon;
                        if (tex != null)
                        {
                            icon.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                        }
                    }
                }
            }
        }
    }
}