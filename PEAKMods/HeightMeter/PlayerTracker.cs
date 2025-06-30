using System;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace HeightMeterMod
{
    public class PlayerTracker : MonoBehaviourPunCallbacks
    {
        // Events
        public event Action<Character> OnPlayerAdded;
        public event Action<Character> OnPlayerRemoved;
        
        // Tracked characters
        private Dictionary<Player, Character> trackedPlayers = new Dictionary<Player, Character>();
        
        public void Initialize()
        {
            // Track all existing characters
            foreach (var character in Character.AllCharacters)
            {
                AddCharacter(character);
            }
            
            Utils.LogInfo($"Started tracking {trackedPlayers.Count} players");
        }
        
        public IEnumerable<Character> GetTrackedCharacters()
        {
            // Clean up any null references
            List<Player> toRemove = new List<Player>();
            
            foreach (var kvp in trackedPlayers)
            {
                if (kvp.Value == null || kvp.Value.refs == null)
                {
                    toRemove.Add(kvp.Key);
                }
            }
            
            foreach (var player in toRemove)
            {
                trackedPlayers.Remove(player);
            }
            
            return trackedPlayers.Values;
        }
        
        private void AddCharacter(Character character)
        {
            if (character == null || character.refs?.view?.Owner == null)
                return;
                
            var player = character.refs.view.Owner;
            
            if (!trackedPlayers.ContainsKey(player))
            {
                trackedPlayers[player] = character;
                OnPlayerAdded?.Invoke(character);
            }
        }
        
        private void RemoveCharacter(Character character)
        {
            if (character == null || character.refs?.view?.Owner == null)
                return;
                
            var player = character.refs.view.Owner;
            
            if (trackedPlayers.ContainsKey(player))
            {
                trackedPlayers.Remove(player);
                OnPlayerRemoved?.Invoke(character);
            }
        }
        
        // Photon callbacks
        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            Utils.LogInfo($"Player entered room: {newPlayer.NickName}");
            
            // Character might not be spawned immediately
            StartCoroutine(WaitForCharacterSpawn(newPlayer));
        }
        
        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Utils.LogInfo($"Player left room: {otherPlayer.NickName}");
            
            if (trackedPlayers.TryGetValue(otherPlayer, out Character character))
            {
                RemoveCharacter(character);
            }
        }
        
        private System.Collections.IEnumerator WaitForCharacterSpawn(Player player)
        {
            // Wait up to 5 seconds for character to spawn
            float timeout = 5f;
            float elapsed = 0f;
            
            while (elapsed < timeout)
            {
                var character = PlayerHandler.GetPlayerCharacter(player);
                if (character != null)
                {
                    AddCharacter(character);
                    yield break;
                }
                
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
            
            Utils.LogWarning($"Timeout waiting for character spawn: {player.NickName}");
        }
        
        private void OnDestroy()
        {
            // Clear all tracked players
            foreach (var character in trackedPlayers.Values)
            {
                OnPlayerRemoved?.Invoke(character);
            }
            
            trackedPlayers.Clear();
        }
    }
}