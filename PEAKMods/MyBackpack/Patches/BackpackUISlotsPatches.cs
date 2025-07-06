using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

namespace BackpackViewerMod.Patches
{
    // Modello per la nostra UI, legato a un TrackedPlayer
    public class PlayerBackpackDisplay
    {
        public TrackedPlayer TrackedPlayer { get; }
        public GameObject MainContainer { get; set; }
        public TextMeshProUGUI NameLabel { get; set; }
        public List<Image> IconImages { get; } = new List<Image>();

        public PlayerBackpackDisplay(TrackedPlayer trackedPlayer)
        {
            TrackedPlayer = trackedPlayer;
        }
    }

    // Classe principale per la gestione della UI degli zaini
    public static class BackpackUISlotsPatches
    {
        // [ARCHITECT'S NOTE] Costanti per un layout pulito e configurabile
        private const float PADDING_RIGHT = -15f;
        private const float PADDING_TOP = -15f;
        private const float HORIZONTAL_SPACING = 10f;
        private const float VERTICAL_SPACING = 10f; // Riservato per usi futuri

        // Cache delle UI create, per non distruggere e ricreare oggetti
        private static readonly Dictionary<Character, PlayerBackpackDisplay> uiDisplayPool = new Dictionary<Character, PlayerBackpackDisplay>();
        // Lista delle UI attualmente attive, aggiornata dagli eventi
        private static List<PlayerBackpackDisplay> activeDisplays = new List<PlayerBackpackDisplay>();

        private static Transform parentCanvas;
        private static TMP_FontAsset gameFont; // <-- NUOVA cache per il font


        // Metodo chiamato da Plugin.cs per inizializzare il sistema
        public static void Initialize()
        {
            PlayerManager.OnTrackedPlayersChanged += OnTrackedPlayersChanged;
            Utils.LogInfo("Backpack UI System Initialized and subscribed to PlayerManager events.");
        }

        // Metodo chiamato da Plugin.cs per lo spegnimento pulito
        public static void Shutdown()
        {
            PlayerManager.OnTrackedPlayersChanged -= OnTrackedPlayersChanged;
            CleanupAllDisplays();
            Utils.LogInfo("Backpack UI System Shutdown and unsubscribed from events.");
        }

        // [ARCHITECT'S NOTE] Questo è il CUORE del sistema reattivo.
        // Viene eseguito SOLO quando PlayerManager notifica un cambiamento.
        private static void OnTrackedPlayersChanged()
        {
            var playersWithBackpacks = PlayerManager.PlayersWithBackpacks;

            foreach (var trackedPlayer in playersWithBackpacks)
            {
                if (!uiDisplayPool.ContainsKey(trackedPlayer.Character))
                {
                    var newDisplay = CreateDisplayForPlayer(trackedPlayer);
                    if (newDisplay != null) // Controlla che la creazione sia andata a buon fine
                    {
                        uiDisplayPool[trackedPlayer.Character] = newDisplay;
                    }
                }
            }

            activeDisplays = playersWithBackpacks
                .Where(p => uiDisplayPool.ContainsKey(p.Character)) // Assicurati che esista nel pool
                .Select(p => uiDisplayPool[p.Character])
                .ToList();

            // Sincronizza lo stato di visibilità di TUTTE le UI nel pool
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
            static void Postfix(GUIManager __instance)
            {
                // [ARCHITECT'S NOTE] Ora questo blocco serve solo per il primo caching.
                // La logica di creazione non dipende più da questo.
                if (parentCanvas == null)
                {
                    EnsureCanvasIsAvailable();
                }
                if (parentCanvas == null) return;

                // Uscita anticipata se la feature è disattivata, nascondendo le UI se necessario
                if (!PluginConfig.showPlayerBackpackSlots.Value)
                {
                    if (activeDisplays.Any(d => d.MainContainer.activeSelf))
                    {
                        HideAllDisplays();
                    }
                    return;
                }
                
                // [ARCHITECT'S NOTE] Logica di LateUpdate minimale: solo posizionamento e aggiornamento icone
                // Nessuna iterazione di AllCharacters, nessuna logica complessa. Solo rendering.

                var localPlayerDisplay = activeDisplays.FirstOrDefault(d => d.TrackedPlayer.Character.IsLocal);
                var otherPlayerDisplays = activeDisplays.Where(d => !d.TrackedPlayer.Character.IsLocal).ToList();

                float nextHorizontalOffset = PADDING_RIGHT;

                // 1. Posiziona la UI del giocatore locale
                if (localPlayerDisplay != null)
                {
                    PositionLocalPlayerUI(localPlayerDisplay);
                    UpdateDisplayIcons(localPlayerDisplay);
                    
                    // Prepara l'offset per gli altri giocatori
                    nextHorizontalOffset -= (localPlayerDisplay.MainContainer.GetComponent<RectTransform>().sizeDelta.x + HORIZONTAL_SPACING);
                }
                
                // 2. Posiziona le UI degli altri giocatori
                if (PluginConfig.showOtherPlayerBackpackSlots.Value)
                {
                    PositionOtherPlayersUI(otherPlayerDisplays, nextHorizontalOffset);
                }
                else // Se l'opzione è disattivata, assicuriamoci che siano nascoste
                {
                     foreach(var display in otherPlayerDisplays)
                     {
                         if(display.MainContainer.activeSelf) display.MainContainer.SetActive(false);
                     }
                }
            }
        }

        private static void HideAllDisplays()
        {
            foreach (var display in uiDisplayPool.Values)
            {
                if (display.MainContainer != null && display.MainContainer.activeSelf)
                {
                    display.MainContainer.SetActive(false);
                }
            }
            activeDisplays.Clear();
        }

        private static void CleanupAllDisplays()
        {
            if (uiDisplayPool.Count > 0)
            {
                foreach (var display in uiDisplayPool.Values)
                {
                    if (display.MainContainer != null)
                    {
                        GameObject.Destroy(display.MainContainer);
                    }
                }
                uiDisplayPool.Clear();
            }
            activeDisplays.Clear();
        }
        
        // [ARCHITECT'S NOTE] Questo metodo è stato aggiunto per garantire che il canvas sia valido.
        private static bool EnsureCanvasIsAvailable()
        {
            if (parentCanvas == null)
            {
                if (GUIManager.instance != null && GUIManager.instance.hudCanvas != null)
                {
                    parentCanvas = GUIManager.instance.hudCanvas.transform;
                    Utils.LogInfo("Successfully cached HUD Canvas transform.");
                }
            }
            return parentCanvas != null;
        }
        
        // Metodo helper per creare gli oggetti UI per un giocatore
        private static PlayerBackpackDisplay CreateDisplayForPlayer(TrackedPlayer trackedPlayer)
        {
            // [ARCHITECT'S NOTE] Controllo "Just-In-Time" del canvas.
            if (!EnsureCanvasIsAvailable())
            {
                Utils.LogError("Cannot create player display: HUD Canvas is not available.");
                return null; // Impossibile creare la UI, esci.
            }

            Character character = trackedPlayer.Character;
            var display = new PlayerBackpackDisplay(trackedPlayer);

            bool isLocal = character.IsLocal;
            float scale = isLocal ? 1.0f : PluginConfig.otherPlayerSlotsScale.Value;

            display.MainContainer = new GameObject($"BackpackDisplay_{character.characterName}");
            display.MainContainer.transform.SetParent(parentCanvas, false);
            var mainRect = display.MainContainer.AddComponent<RectTransform>();
            mainRect.anchorMin = new Vector2(1, 1);
            mainRect.anchorMax = new Vector2(1, 1);
            mainRect.pivot = new Vector2(1, 1);

            // Etichetta con il nome
            var labelObj = new GameObject("NameLabel");
            labelObj.transform.SetParent(display.MainContainer.transform, false);
            display.NameLabel = labelObj.AddComponent<TextMeshProUGUI>();
            display.NameLabel.text = character.characterName;
            display.NameLabel.alignment = TextAlignmentOptions.TopRight;
            ApplyTMProStyle(display.NameLabel, 16 * scale, character.refs.customization.PlayerColor);

            var labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(1, 1);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.pivot = new Vector2(1, 1);
            labelRect.sizeDelta = new Vector2(120 * scale, 22 * scale);
            labelRect.anchoredPosition = Vector2.zero;

            // Slot degli oggetti
            float slotSize = 60 * scale;
            float slotSpacing = 5 * scale;
            for (int i = 0; i < 4; i++)
            {
                var slotBg = new GameObject($"Slot_BG_{i}");
                slotBg.transform.SetParent(display.MainContainer.transform, false);
                slotBg.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.6f);
                var bgRect = slotBg.GetComponent<RectTransform>();
                bgRect.anchorMin = new Vector2(1, 1);
                bgRect.anchorMax = new Vector2(1, 1);
                bgRect.pivot = new Vector2(1, 1);
                bgRect.sizeDelta = new Vector2(slotSize, slotSize);
                bgRect.anchoredPosition = new Vector2(0, -labelRect.sizeDelta.y - (i * (slotSize + slotSpacing)));

                var iconObj = new GameObject($"Icon_{i}");
                iconObj.transform.SetParent(slotBg.transform, false);
                var iconImage = iconObj.AddComponent<Image>();
                iconImage.raycastTarget = false;
                var iconRect = iconObj.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.sizeDelta = new Vector2(-10 * scale, -10 * scale);
                display.IconImages.Add(iconImage);
            }

            mainRect.sizeDelta = new Vector2(slotSize, labelRect.sizeDelta.y + (4 * slotSize) + (3 * slotSpacing));
            display.MainContainer.SetActive(false); // Inizia nascosto, sarà attivato dall'evento
            return display;
        }

        // Metodo helper per applicare uno stile standard al testo
        private static void ApplyTMProStyle(TextMeshProUGUI tmp, float fontSize, Color color, FontStyles style = FontStyles.Bold)
        {
            if (gameFont == null)
            {
                // [ARCHITECT'S NOTE] API Obsoleta sostituita con FindAnyObjectByType, come suggerito da Unity.
                var connectionLog = Object.FindAnyObjectByType<PlayerConnectionLog>();
                if (connectionLog != null && connectionLog.text != null && connectionLog.text.font != null)
                {
                    gameFont = connectionLog.text.font;
                    Utils.LogInfo($"Game font '{gameFont.name}' found and cached.");
                }
                else
                {
                    // [ARCHITECT'S NOTE] Integrata la logica di fallback preferita dall'utente.
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

            if (gameFont != null)
            {
                tmp.font = gameFont;
            }
            
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.raycastTarget = false;

            var outline = tmp.gameObject.GetComponent<Outline>() ?? tmp.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.8f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
        }

        // Posiziona la UI del giocatore locale nel suo punto fisso
        private static void PositionLocalPlayerUI(PlayerBackpackDisplay display)
        {
            var rect = display.MainContainer.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(PADDING_RIGHT, PADDING_TOP);
        }

        // Posiziona le UI degli altri giocatori in sequenza
        private static void PositionOtherPlayersUI(List<PlayerBackpackDisplay> otherDisplays, float startingHorizontalOffset)
        {
            float currentHorizontalOffset = startingHorizontalOffset;
            // [ARCHITECT'S NOTE] Ora ordiniamo usando il nostro ID pulito e cachato.
            foreach (var display in otherDisplays.OrderBy(d => d.TrackedPlayer.ActorID))
            {
                var rect = display.MainContainer.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(currentHorizontalOffset, PADDING_TOP);
                UpdateDisplayIcons(display);
                
                currentHorizontalOffset -= (rect.sizeDelta.x + HORIZONTAL_SPACING);
            }
        }

        
        // Aggiorna le icone degli oggetti per una data UI
        private static void UpdateDisplayIcons(PlayerBackpackDisplay display)
        {
            ItemInstanceData backpackInstanceData = display.TrackedPlayer.Character.player.backpackSlot.data;
            if (backpackInstanceData == null) return;

            if (backpackInstanceData.TryGetDataEntry(DataEntryKey.BackpackData, out BackpackData backpackData))
            {
                for (int i = 0; i < display.IconImages.Count && i < backpackData.itemSlots.Length; i++)
                {
                    ItemSlot slotData = backpackData.itemSlots[i];
                    Image icon = display.IconImages[i];
                    if (slotData.IsEmpty() || slotData.prefab == null || slotData.prefab.UIData == null)
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
    
    // Estensione helper per creare Sprite da Texture2D in modo pulito e sicuro
    public static class TextureExtensions
    {
        public static Sprite ToSprite(this Texture2D texture)
        {
            if (texture == null) return null;
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
    }
}