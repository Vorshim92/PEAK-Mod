using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;
using System.Collections;
using Photon.Realtime;
using ExitGames.Client.Photon;
using BackpackUISlotsPatches = BackpackViewerMod.Patches.BackpackUISlotsPatches;
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
    
    public class PlayerManager
    {
        public event Action<List<TrackedPlayer>> OnTrackedPlayersChanged;

        private readonly Dictionary<Photon.Realtime.Player, TrackedPlayer> trackedPlayers = new Dictionary<Photon.Realtime.Player, TrackedPlayer>();
        private Coroutine updateCoroutine;
        private readonly PhotonCallbacksHandler photonCallbacks;
        
        public PlayerManager(BackpackUISlotsPatches uiManager)
        {
            this.OnTrackedPlayersChanged += uiManager.OnTrackedPlayersChanged;
            photonCallbacks = new PhotonCallbacksHandler(this);
            Initialize();
        }

        private void Initialize()
        {
            PhotonNetwork.AddCallbackTarget(photonCallbacks);
            updateCoroutine = Plugin.Instance.StartCoroutine(StatusCheckCoroutine());
            Utils.LogInfo("PlayerManager instance created and initialized.");
        }

        public void Shutdown()
        {
            if (updateCoroutine != null)
            {
                Plugin.Instance.StopCoroutine(updateCoroutine);
            }
            PhotonNetwork.RemoveCallbackTarget(photonCallbacks);
            trackedPlayers.Clear();
            OnTrackedPlayersChanged?.Invoke(new List<TrackedPlayer>());
            Utils.LogInfo("PlayerManager instance shut down.");
        }

        private IEnumerator StatusCheckCoroutine()
        {
            yield return new WaitForSeconds(2.0f); 

            while (true)
            {
                DiscoverUntrackedPlayers();
                UpdateBackpackStatus();
                yield return new WaitForSeconds(1.0f);
            }
        }
        
        private void DiscoverUntrackedPlayers()
        {
            if (PhotonNetwork.PlayerList == null) return;
            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (player != null && !trackedPlayers.ContainsKey(player))
                {
                    AddPlayer(player);
                }
            }
        }

        private void AddPlayer(Photon.Realtime.Player player)
        {
            if (player == null || trackedPlayers.ContainsKey(player)) return;
            var character = Character.AllCharacters.FirstOrDefault(c => c?.refs?.view?.Owner == player);
            if (character != null)
            {
                trackedPlayers[player] = new TrackedPlayer(character);
                Utils.LogInfo($"PlayerManager: Successfully tracked {player.NickName} (ID: {character.refs.view.Owner.ActorNumber}).");
                UpdateBackpackStatus(); 
            }
        }
        
        private void RemovePlayer(Photon.Realtime.Player player)
        {
            if (player == null || !trackedPlayers.ContainsKey(player)) return;
            Utils.LogInfo($"PlayerManager: Untracking {player.NickName}.");
            trackedPlayers.Remove(player);
            UpdateBackpackStatus();
        }
        
        private void UpdateBackpackStatus()
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
                }
            }

            if (hasChanged)
            {
                var playersWithBackpacks = trackedPlayers.Values.Where(p => p.HasBackpack).ToList();
                OnTrackedPlayersChanged?.Invoke(playersWithBackpacks);
            }
        }
        
        private class PhotonCallbacksHandler : IInRoomCallbacks, IConnectionCallbacks, IMatchmakingCallbacks
        {
            private readonly PlayerManager owner;
            public PhotonCallbacksHandler(PlayerManager owner) { this.owner = owner; }
            public void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer) => owner.AddPlayer(newPlayer);
            public void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer) => owner.RemovePlayer(otherPlayer);
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
            public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged) { }
        }
    }
}