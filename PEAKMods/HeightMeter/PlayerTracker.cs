using System;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Linq;

namespace HeightMeterMod
{
    public class PlayerTracker : MonoBehaviourPunCallbacks
    {
        // Events
        public event Action<Character> OnPlayerAdded;
        public event Action<Character> OnPlayerRemoved;
        
        // Tracked characters
                private Dictionary<Photon.Realtime.Player, Character> trackedPlayers = new Dictionary<Photon.Realtime.Player, Character>();
        
        public void Initialize()
        {
            trackedPlayers.Clear();
            Utils.LogInfo("PlayerTracker initializing...");
            
            // 1. Traccia tutti i character esistenti
            foreach (var character in Character.AllCharacters.Where(c => c != null))
            {
                AddCharacter(character);
            }
            
            // 2. Per i player Photon senza character, registra che li stiamo aspettando
            foreach (var photonPlayer in PhotonNetwork.PlayerList)
            {
                if (!trackedPlayers.ContainsKey(photonPlayer))
                {
                    Utils.LogInfo($"Waiting for character spawn: {photonPlayer.NickName}");
                    // Il character verrà aggiunto quando spawna tramite Character.AllCharacters
                }
            }
            
            Utils.LogInfo($"Initial tracking complete: {trackedPlayers.Count}/{PhotonNetwork.PlayerList.Length} players");
        }

        private void Update()
        {
            // Check veloce solo per character non tracciati
            if (trackedPlayers.Count < PhotonNetwork.PlayerList.Length)
            {
                foreach (var character in Character.AllCharacters)
                {
                    if (character?.refs?.view?.Owner != null && !trackedPlayers.ContainsKey(character.refs.view.Owner))
                    {
                        AddCharacter(character);
                    }
                }
            }
        }


        
        public IEnumerable<Character> GetTrackedCharacters()
        {
            // Clean up any null references
            List<Photon.Realtime.Player> toRemove = new List<Photon.Realtime.Player>();

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
                
            var photonPlayer = character.refs.view.Owner;
            
            if (!trackedPlayers.ContainsKey(photonPlayer))
            {
                // Aggiungi il log qui, come suggerito prima.
                Utils.LogInfo($"Adding character to tracker: {photonPlayer.NickName}"); 
                trackedPlayers[photonPlayer] = character;
                OnPlayerAdded?.Invoke(character);
            }
        }
        
        private void RemoveCharacter(Character character)
        {
            if (character == null || character.refs?.view?.Owner == null)
                return;
                
            var photonPlayer = character.refs.view.Owner;
            
            if (trackedPlayers.ContainsKey(photonPlayer))
            {
                trackedPlayers.Remove(photonPlayer);
                OnPlayerRemoved?.Invoke(character);
            }
        }
        
        // Photon callbacks - nota che usa Photon.Realtime.Player
        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            Utils.LogInfo($"Player entered room: {newPlayer.NickName}");
            
            // Character might not be spawned immediately
            StartCoroutine(WaitForCharacterSpawn(newPlayer));
        }
        
        public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
            Utils.LogInfo($"Player left room: {otherPlayer.NickName}");
            
            if (trackedPlayers.TryGetValue(otherPlayer, out Character character))
            {
                RemoveCharacter(character);
            }
        }
        
        private System.Collections.IEnumerator WaitForCharacterSpawn(Photon.Realtime.Player photonPlayer)
        {
            // Wait up to 5 seconds for character to spawn
            float timeout = 30f;
            float elapsed = 0f;
            
            while (elapsed < timeout)
            {
                // Cerca il Character che appartiene a questo Photon.Player
                foreach (var character in Character.AllCharacters)
                {
                    if (character?.refs?.view?.Owner == photonPlayer)
                    {
                        AddCharacter(character);
                        yield break;
                    }
                }
                
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
            
            Utils.LogWarning($"Timeout waiting for character spawn: {photonPlayer.NickName}");
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