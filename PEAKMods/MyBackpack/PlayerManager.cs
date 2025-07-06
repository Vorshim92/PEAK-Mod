using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;
using System.Collections;
using Photon.Realtime;
using ExitGames.Client.Photon;

namespace BackpackViewerMod
{
    public class TrackedPlayer
    {
        public Character Character { get; }
        public bool HasBackpack { get; set; }
        public int ActorID { get; }

        public TrackedPlayer(Character character)
        {
            Character = character;
            HasBackpack = false;
            ActorID = character.refs.view.Owner.ActorNumber;
        }
    }
    
    public static class PlayerManager
    {
        public static event Action OnTrackedPlayersChanged;

        private static readonly Dictionary<Photon.Realtime.Player, TrackedPlayer> trackedPlayers = new Dictionary<Photon.Realtime.Player, TrackedPlayer>();
        private static Coroutine updateCoroutine;
        
        public static List<TrackedPlayer> PlayersWithBackpacks { get; private set; } = new List<TrackedPlayer>();

        public static void Initialize()
        {
            if (Plugin.Instance != null)
            {
                PhotonNetwork.AddCallbackTarget(photonCallbacks);
                updateCoroutine = Plugin.Instance.StartCoroutine(StatusCheckCoroutine()); // Rinominato per chiarezza
                Utils.LogInfo("PlayerManager Initialized.");
            }
            // La scansione iniziale è ancora utile per i casi in cui tutto è già pronto
            InitialScan(); 
        }

        public static void Shutdown()
        {
            if (Plugin.Instance != null && updateCoroutine != null)
            {
                Plugin.Instance.StopCoroutine(updateCoroutine);
            }
            PhotonNetwork.RemoveCallbackTarget(photonCallbacks);
            trackedPlayers.Clear();
            PlayersWithBackpacks.Clear();
            OnTrackedPlayersChanged?.Invoke();
            Utils.LogInfo("PlayerManager Shutdown.");
        }

        // [ARCHITECT'S NOTE] La coroutine ora è il cuore pulsante del sistema.
        private static IEnumerator StatusCheckCoroutine()
        {
            // Breve attesa iniziale per dare tempo alla scena di caricarsi completamente
            yield return new WaitForSeconds(2.0f); 

            while (true)
            {
                // 1. Cerca giocatori che sono nella stanza ma non ancora tracciati
                DiscoverUntrackedPlayers();
                
                // 2. Aggiorna lo stato degli zaini per i giocatori tracciati
                UpdateBackpackStatus();
                
                // Attendi prima del prossimo ciclo completo
                yield return new WaitForSeconds(1.0f);
            }
        }
        
        // [ARCHITECT'S NOTE] Nuovo metodo per la scoperta continua di giocatori.
        private static void DiscoverUntrackedPlayers()
        {
            if (PhotonNetwork.PlayerList == null) return;

            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (player != null && !trackedPlayers.ContainsKey(player))
                {
                    // Tenta di aggiungere il giocatore se non è già tracciato
                    AddPlayer(player);
                }
            }
        }

        private static void InitialScan()
        {
            // Questo metodo ora serve come primo tentativo rapido al caricamento.
            // La coroutine gestirà i casi in cui fallisce.
            DiscoverUntrackedPlayers();
        }

        private static void AddPlayer(Photon.Realtime.Player player)
        {
            // La condizione di uscita anticipata ora è solo sul player, non sulla chiave
            if (player == null) return;
            if(trackedPlayers.ContainsKey(player)) return; // Aggiunta per sicurezza

            var character = Character.AllCharacters.FirstOrDefault(c => c?.refs?.view?.Owner == player);
            if (character != null)
            {
                trackedPlayers[player] = new TrackedPlayer(character);
                Utils.LogInfo($"PlayerManager: Successfully tracked {player.NickName} (ID: {character.refs.view.Owner.ActorNumber}).");
                // Un aggiornamento immediato è utile quando si aggiunge un nuovo giocatore
                UpdateBackpackStatus(); 
            }
            // Non è più necessario un LogWarning qui, perché il sistema riproverà automaticamente
        }
        
        private static void RemovePlayer(Photon.Realtime.Player player)
        {
            if (player == null || !trackedPlayers.ContainsKey(player)) return;
            
            Utils.LogInfo($"PlayerManager: Untracking {player.NickName}.");
            trackedPlayers.Remove(player);
            UpdateBackpackStatus();
        }
        
        private static void UpdateBackpackStatus()
        {
            bool hasChanged = false;

            var toRemove = trackedPlayers.Where(kvp => kvp.Key == null || kvp.Value.Character == null).Select(kvp => kvp.Key).ToList();
            if (toRemove.Any())
            {
                foreach (var key in toRemove) trackedPlayers.Remove(key);
                hasChanged = true;
            }

            foreach (var trackedPlayer in trackedPlayers.Values)
            {
                BackpackSlot backpackSlot = trackedPlayer.Character.player.backpackSlot;
                bool currentlyHasBackpack = !backpackSlot.IsEmpty();

                if (trackedPlayer.HasBackpack != currentlyHasBackpack)
                {
                    trackedPlayer.HasBackpack = currentlyHasBackpack;
                    hasChanged = true;
                    Utils.LogInfo($"Player {trackedPlayer.Character.name} backpack status changed to: {currentlyHasBackpack}");
                }
            }

            if (hasChanged)
            {
                PlayersWithBackpacks = trackedPlayers.Values.Where(p => p.HasBackpack).ToList();
                Utils.LogInfo($"Invoking OnTrackedPlayersChanged. {PlayersWithBackpacks.Count} players with backpacks.");
                OnTrackedPlayersChanged?.Invoke();
            }
        }
        
        private static readonly PhotonCallbacksHandler photonCallbacks = new PhotonCallbacksHandler();

        private class PhotonCallbacksHandler : IConnectionCallbacks, IMatchmakingCallbacks, IInRoomCallbacks
        {
            public void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer) => AddPlayer(newPlayer);
            public void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer) => RemovePlayer(otherPlayer);

            // Metodi non usati ma richiesti dall'interfaccia
            public void OnConnected() { }
            public void OnConnectedToMaster() { }
            public void OnDisconnected(DisconnectCause cause) { }
            public void OnRegionListReceived(RegionHandler regionHandler) { }
            public void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }
            public void OnCustomAuthenticationFailed(string debugMessage) { }
            public void OnFriendListUpdate(List<FriendInfo> friendList) { }
            public void OnCreatedRoom() { }
            public void OnCreateRoomFailed(short returnCode, string message) { }
            public void OnJoinedRoom() { }
            public void OnJoinRoomFailed(short returnCode, string message) { }
            public void OnJoinRandomFailed(short returnCode, string message) { }
            public void OnLeftRoom() { }
            public void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps) { }
            public void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient) { }
            public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged) { } // <-- AGGIUNGI QUESTA RIGA

        }
    }
}