using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace BackpackViewerMod.Patches
{
    public class PlayerBackpackDisplay
    {
        public TrackedPlayer TrackedPlayer { get; }
        public GameObject MainContainer { get; set; }
        public List<Image> IconImages { get; } = new List<Image>();

        public PlayerBackpackDisplay(TrackedPlayer trackedPlayer)
        {
            TrackedPlayer = trackedPlayer;
        }
    }
    
    public class BackpackUISlotsPatches
    {
        private static BackpackUISlotsPatches _instance;

        private const float PADDING_RIGHT = -15f;
        private const float PADDING_TOP = -15f;
        private const float HORIZONTAL_SPACING = 10f;
        private const float DEBUG_SLOT_SIZE = 35f;

        private readonly Dictionary<Character, PlayerBackpackDisplay> uiDisplayPool = new Dictionary<Character, PlayerBackpackDisplay>();
        private List<PlayerBackpackDisplay> activeDisplays = new List<PlayerBackpackDisplay>();

        private Transform parentCanvas;
        private TMP_FontAsset gameFont;

        public BackpackUISlotsPatches()
        {
            _instance = this;
            Utils.LogInfo("Backpack UI System instance created.");
        }

        public void Shutdown()
        {
            CleanupAllDisplays();
            _instance = null;
            Utils.LogInfo("Backpack UI System instance shut down.");
        }

        private bool EnsureCanvasIsAvailable()
        {
            if (parentCanvas == null)
            {
                if (GUIManager.instance != null && GUIManager.instance.hudCanvas != null)
                {
                    parentCanvas = GUIManager.instance.hudCanvas.transform;
                }
            }
            return parentCanvas != null;
        }


        public void OnTrackedPlayersChanged(List<TrackedPlayer> playersWithBackpacks)
        {
            foreach (var trackedPlayer in playersWithBackpacks)
            {
                if (!uiDisplayPool.ContainsKey(trackedPlayer.Character))
                {
                    var newDisplay = CreateDisplayForPlayer(trackedPlayer);
                    if (newDisplay != null)
                    {
                        uiDisplayPool[trackedPlayer.Character] = newDisplay;
                    }
                }
            }
            
            activeDisplays = playersWithBackpacks
                .Where(p => uiDisplayPool.ContainsKey(p.Character))
                .Select(p => uiDisplayPool[p.Character])
                .ToList();

            var activeCharacters = new HashSet<Character>(playersWithBackpacks.Select(p => p.Character));
            foreach (var kvp in uiDisplayPool)
            {
                bool shouldBeActive = activeCharacters.Contains(kvp.Key);
                if (kvp.Value.MainContainer != null && kvp.Value.MainContainer.activeSelf != shouldBeActive)
                {
                    kvp.Value.MainContainer.SetActive(shouldBeActive);
                }
            }
        }
        
        [HarmonyPatch(typeof(GUIManager), "LateUpdate")]
        public class GUIManager_LateUpdate_Patch
        {
            static void Postfix()
            {
                _instance?.OnLateUpdate();
            }
        }
        
        private void OnLateUpdate()
        {
            if (!EnsureCanvasIsAvailable()) return;

            if (!PluginConfig.showPlayerBackpackSlots.Value)
            {
                if (activeDisplays.Any(d => d.MainContainer.activeSelf)) HideAllDisplays();
                return;
            }

            var localPlayerDisplay = activeDisplays.FirstOrDefault(d => d.TrackedPlayer.Character.IsLocal);
            var otherPlayerDisplays = activeDisplays.Where(d => !d.TrackedPlayer.Character.IsLocal).ToList();
            float nextHorizontalOffset = PADDING_RIGHT;

            if (localPlayerDisplay != null)
            {
                PositionUI(localPlayerDisplay, new Vector2(nextHorizontalOffset, PADDING_TOP));
                nextHorizontalOffset -= (localPlayerDisplay.MainContainer.GetComponent<RectTransform>().sizeDelta.x + HORIZONTAL_SPACING);
            }
            
            if (PluginConfig.showOtherPlayerBackpackSlots.Value)
            {
                foreach (var display in otherPlayerDisplays.OrderBy(d => d.TrackedPlayer.ActorID))
                {
                    PositionUI(display, new Vector2(nextHorizontalOffset, PADDING_TOP));
                    nextHorizontalOffset -= (display.MainContainer.GetComponent<RectTransform>().sizeDelta.x + HORIZONTAL_SPACING);
                }
            }
        }

        private void HideAllDisplays()
        {
            foreach (var display in uiDisplayPool.Values)
            {
                if (display.MainContainer != null && display.MainContainer.activeSelf)
                    display.MainContainer.SetActive(false);
            }
            activeDisplays.Clear();
        }

        private void CleanupAllDisplays()
        {
            foreach (var display in uiDisplayPool.Values)
            {
                if (display.MainContainer != null) GameObject.Destroy(display.MainContainer);
            }
            uiDisplayPool.Clear();
            activeDisplays.Clear();
        }
        
        private PlayerBackpackDisplay CreateDisplayForPlayer(TrackedPlayer trackedPlayer)
        {
            if (!EnsureCanvasIsAvailable()) return null;

            Character character = trackedPlayer.Character;
            var display = new PlayerBackpackDisplay(trackedPlayer);
            
            bool isLocal = character.IsLocal;
            float scale = isLocal ? 1.0f : PluginConfig.otherPlayerSlotsScale.Value;
            
            // --- MAIN CONTAINER ---
            display.MainContainer = new GameObject($"BackpackDisplay_{character.characterName}");
            display.MainContainer.transform.SetParent(parentCanvas, false);
            
            var mainLayout = display.MainContainer.AddComponent<VerticalLayoutGroup>();
            mainLayout.childAlignment = TextAnchor.UpperRight;
            mainLayout.spacing = 2 * scale;
            // --- ISTRUZIONE FONDAMENTALE #1 ---
            // Diciamo a questo layout di NON controllare la dimensione dei suoi figli.
            // Questo permette ai figli di avere i propri sistemi di layout (ContentSizeFitter, etc.)
            mainLayout.childControlHeight = false;
            mainLayout.childControlWidth = false;
            mainLayout.childForceExpandHeight = false;
            mainLayout.childForceExpandWidth = false;

            var mainContentFitter = display.MainContainer.AddComponent<ContentSizeFitter>();
            mainContentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            mainContentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var mainRect = display.MainContainer.GetComponent<RectTransform>();
            mainRect.anchorMin = new Vector2(1, 1); 
            mainRect.anchorMax = new Vector2(1, 1); 
            mainRect.pivot = new Vector2(1, 1);

            // --- LABEL ---
            var labelObj = new GameObject("NameLabel");
            labelObj.transform.SetParent(display.MainContainer.transform, false);
            var nameLabel = labelObj.AddComponent<TextMeshProUGUI>();
            nameLabel.text = character.characterName;
            ApplyTMProStyle(nameLabel, 16 * scale, character.refs.customization.PlayerColor);
            nameLabel.alignment = TextAlignmentOptions.TopRight;
            var labelLayoutElement = labelObj.AddComponent<LayoutElement>();
            labelLayoutElement.minWidth = 120 * scale;

            var slotsContainerObj = new GameObject("SlotsContainer");
            slotsContainerObj.transform.SetParent(display.MainContainer.transform, false);

            var slotsLayout = slotsContainerObj.AddComponent<VerticalLayoutGroup>();
            slotsLayout.childAlignment = TextAnchor.UpperRight;
            slotsLayout.spacing = 3 * scale;
            // --- ISTRUZIONE FONDAMENTALE #2 ---
            // Anche questo layout non deve forzare la dimensione dei suoi figli (gli slot).
            slotsLayout.childControlHeight = false;
            slotsLayout.childControlWidth = false;
            
            var slotsContentFitter = slotsContainerObj.AddComponent<ContentSizeFitter>();
            slotsContentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            slotsContentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            float slotSize = DEBUG_SLOT_SIZE * scale;
            Utils.LogInfo($"[ARCHITECT-DIAGNOSTIC] Creazione slot per '{trackedPlayer.Character.characterName}'. Scala: {scale}, Dimensione Slot Calcolata: {slotSize}");
            float borderWidth = 2.5f * scale;

            for (int i = 0; i < 4; i++)
            {
                var slotObj = new GameObject($"Slot_{i}");
                slotObj.transform.SetParent(slotsContainerObj.transform, false); 
                
                // --- ISTRUZIONE FONDAMENTALE #3 ---
                // Diamo allo slot una dimensione preferita tramite LayoutElement.
                // Questo è il modo corretto per comunicare con un LayoutGroup genitore.
                var slotLayoutElement = slotObj.AddComponent<LayoutElement>();
                slotLayoutElement.preferredWidth = slotSize;
                slotLayoutElement.preferredHeight = slotSize;
                
                // Non abbiamo più bisogno di un RectTransform separato per lo slot,
                // perché il LayoutElement e il genitore si occuperanno di tutto.

                var backgroundObj = new GameObject("SlotBackground");
                backgroundObj.transform.SetParent(slotObj.transform, false);
                var slotBackground = backgroundObj.AddComponent<RoundedImageWithBorder>();
                slotBackground.raycastTarget = false;
                slotBackground.cornerRadius = slotSize * 0.25f;
                slotBackground.borderWidth = borderWidth;
                slotBackground.color = Color.white;

                var backgroundRect = backgroundObj.GetComponent<RectTransform>();
                backgroundRect.anchorMin = Vector2.zero;
                backgroundRect.anchorMax = Vector2.one;
                backgroundRect.offsetMin = Vector2.zero;
                backgroundRect.offsetMax = Vector2.zero;
                
                var iconObj = new GameObject($"Icon_{i}");
                iconObj.transform.SetParent(slotObj.transform, false);
                var iconImage = iconObj.AddComponent<Image>();
                iconImage.raycastTarget = false;
                var iconRect = iconObj.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                float iconPadding = 5 * scale; // Ridotto il padding per la nuova dimensione
                iconRect.offsetMin = new Vector2(iconPadding, iconPadding);
                iconRect.offsetMax = new Vector2(-iconPadding, -iconPadding);
                display.IconImages.Add(iconImage);
            }
            
            // Torniamo alla logica semplice: il container viene creato inattivo.
            // Sarà gestito da OnTrackedPlayersChanged.
            display.MainContainer.SetActive(false);
            Utils.LogInfo($"[ARCHITECT V5] Display per {character.characterName} creato con architettura di layout corretta.");
            return display;
        }

        private void ApplyTMProStyle(TextMeshProUGUI tmp, float fontSize, Color color)
        {
            if (gameFont == null) {
                var connectionLog = Object.FindAnyObjectByType<PlayerConnectionLog>();
                if (connectionLog != null && connectionLog.text != null && connectionLog.text.font != null)
                {
                    gameFont = connectionLog.text.font;
                    Utils.LogInfo($"Game font '{gameFont.name}' found and cached.");
                }
                else
                {
                    var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                    gameFont = fonts.FirstOrDefault(f => f.faceInfo.familyName.Contains("Daruma")) 
                            ?? fonts.FirstOrDefault();
                            
                    if (gameFont == null)
                    {
                        Utils.LogWarning("Could not find game font, using default");
                    }
                    else
                    {
                        Utils.LogInfo($"Game font '{gameFont.name}' found via fallback and cached.");
                    }
                }
            }
            if (gameFont != null) tmp.font = gameFont;
            
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.TopRight;
            tmp.raycastTarget = false;
            tmp.outlineWidth = 0.12f;
            tmp.outlineColor = new Color(0, 0, 0, 0.8f);
        }

        private void PositionUI(PlayerBackpackDisplay display, Vector2 anchoredPosition)
        {
            display.MainContainer.GetComponent<RectTransform>().anchoredPosition = anchoredPosition;
            UpdateDisplayIcons(display);
        }
        
        private void UpdateDisplayIcons(PlayerBackpackDisplay display)
        {
            BackpackSlot backpackSlot = display.TrackedPlayer.Character.player.backpackSlot;
            if (backpackSlot.IsEmpty()) return;

            if (backpackSlot.data.TryGetDataEntry(DataEntryKey.BackpackData, out BackpackData backpackData))
            {
                for (int i = 0; i < display.IconImages.Count && i < backpackData.itemSlots.Length; i++)
                {
                    ItemSlot slotData = backpackData.itemSlots[i];
                    Image icon = display.IconImages[i];
                    if (slotData.IsEmpty() || slotData.prefab?.UIData?.icon == null)
                    {
                        icon.enabled = false;
                    }
                    else
                    {
                        icon.enabled = true;
                        icon.sprite = slotData.prefab.UIData.icon.ToSprite();
                    }
                }
            }
        }
    }
    public static class TextureExtensions
    {
        public static Sprite ToSprite(this Texture2D texture)
        {
            if (texture == null) return null;
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
    }
}