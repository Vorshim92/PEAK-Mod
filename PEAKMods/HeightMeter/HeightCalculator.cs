// File: HeightCalculator.cs

using UnityEngine;
using System.Linq;
using System.Collections.Generic; // Necessario per la Dictionary

namespace HeightMeterMod
{
    public class HeightCalculator : MonoBehaviour
    {
        private float baseHeight = 0f;
        private float peakHeight = 1920f;
        private float metersPerUnit = 1f;

        // --- MODIFICA CHIAVE ---
        // Ora usiamo una lista interna di ProgressMarker, che conterrà i dati FUSI.
        private List<ProgressMarker> allMarkers = new List<ProgressMarker>();
        
        public bool IsInitialized { get; private set; }
        
        // --- MODIFICA CHIAVE ---
        // L'inizializzazione non riceve più dati, li CERCA da sola.
        // Questo è il cuore della nuova architettura.
        public bool Initialize()
        {
            var progressHandler = MountainProgressHandler.Instance;
            var mapHandler = MapHandler.Instance;

            // Controlli di robustezza: assicurati che entrambi i singleton siano pronti.
            if (progressHandler == null || progressHandler.progressPoints == null || progressHandler.progressPoints.Length == 0)
            {
                Utils.LogError("HeightCalculator: MountainProgressHandler non è pronto o non ha punti.");
                return false;
            }
            if (mapHandler == null || mapHandler.segments == null || mapHandler.segments.Length == 0)
            {
                Utils.LogError("HeightCalculator: MapHandler non è pronto o non ha segmenti.");
                return false;
            }

            // Usiamo un dizionario per aggregare e rimuovere duplicati.
            // La chiave è la posizione Z (altitudine), il valore è il nostro ProgressMarker.
            var markerMap = new Dictionary<float, ProgressMarker>();

            // 1. Aggrega i checkpoint MINORI da MountainProgressHandler
            Utils.LogInfo($"Aggregating {progressHandler.progressPoints.Length} minor checkpoints...");
            foreach (var point in progressHandler.progressPoints.Where(p => p != null && p.transform != null))
            {
                float zPos = point.transform.position.z;
                // Arrotonda per evitare problemi di precisione float e unire punti molto vicini
                float key = Mathf.Round(zPos * 10) / 10f; 
                if (!markerMap.ContainsKey(key))
                {
                    markerMap[key] = new ProgressMarker
                    {
                        Name = point.title,
                        RawZPosition = zPos, // Salviamo la Z grezza
                        IsReached = point.Reached
                    };
                }
            }
            
            // 2. Aggrega i segmenti MAGGIORI da MapHandler
            Utils.LogInfo($"Aggregating {mapHandler.segments.Length} major segments...");
            for (int i = 0; i < mapHandler.segments.Length; i++)
            {
                var segment = mapHandler.segments[i];
                if (segment != null && segment.reconnectSpawnPos != null)
                {
                    float zPos = segment.reconnectSpawnPos.position.z;
                    float key = Mathf.Round(zPos * 10) / 10f;
                    
                    // Aggiungi solo se non c'è già un checkpoint minore alla stessa altezza
                    if (!markerMap.ContainsKey(key))
                    {
                        markerMap[key] = new ProgressMarker
                        {
                            // Usa il nome dell'enum Segment corrispondente
                            Name = ((Segment)i).ToString(), 
                            RawZPosition = zPos,
                            // Un segmento è "raggiunto" se il giocatore ha superato la sua Z
                            IsReached = Character.localCharacter != null && Character.localCharacter.Center.z > zPos
                        };
                    }
                }
            }

            // Determina i limiti di altezza DOPO aver raccolto tutti i punti
            if (markerMap.Count == 0)
            {
                Utils.LogError("No markers could be aggregated.");
                return false;
            }

            baseHeight = markerMap.Values.Min(m => m.RawZPosition);
            peakHeight = markerMap.Values.Max(m => m.RawZPosition);
            
            float totalUnits = peakHeight - baseHeight;
            if (totalUnits > 0)
            {
                metersPerUnit = 1920f / totalUnits;
            }
            
            // Ora che abbiamo i limiti e il fattore di conversione, calcoliamo i valori finali
            allMarkers = markerMap.Values.OrderBy(m => m.RawZPosition).ToList();
            foreach (var marker in allMarkers)
            {
                marker.NormalizedHeight = GetNormalizedHeight(marker.RawZPosition);
                marker.HeightInMeters = GetHeightInMeters(marker.RawZPosition);
            }
            
            Utils.LogInfo($"Height bounds finalized: Base={baseHeight:F2}, Peak={peakHeight:F2}, MetersPerUnit={metersPerUnit:F2}");
            Utils.LogInfo($"Aggregation complete. Total unique markers: {allMarkers.Count}");
            IsInitialized = true;
            return true;
        }

        // --- MODIFICA CHIAVE ---
        // GetProgressMarkers ora restituisce semplicemente la lista pre-calcolata.
        public List<ProgressMarker> GetProgressMarkers()
        {
            return allMarkers;
        }

        // --- NUOVA CLASSE INTERNA ---
        // Modifichiamo ProgressMarker per contenere i dati grezzi prima del calcolo finale
        public class ProgressMarker
        {
            public string Name { get; set; }
            public float RawZPosition { get; set; } // Z grezza prima della normalizzazione
            public float NormalizedHeight { get; set; }
            public float HeightInMeters { get; set; }
            public bool IsReached { get; set; }
        }
        
        // Il resto dei metodi (GetNormalizedHeight, GetHeightInMeters, etc.) rimane uguale
        // ma assicurati che usino `baseHeight` e `peakHeight` calcolati internamente.
        public float GetNormalizedHeight(float zPosition)
        {
            if (peakHeight <= baseHeight) return 0f;
            return Mathf.Clamp01((zPosition - baseHeight) / (peakHeight - baseHeight));
        }
        
        public float GetHeightInMeters(float zPosition)
        {
            float relativeHeight = zPosition - baseHeight;
            return Mathf.Max(0f, relativeHeight * metersPerUnit);
        }

        public CheckpointInfo GetNextCheckpoint(float currentZ)
        {
            if (allMarkers == null || allMarkers.Count == 0) return null;

            foreach (var point in allMarkers.Where(p => !p.IsReached && p.RawZPosition > currentZ))
            {
                float distanceInMeters = (point.RawZPosition - currentZ) * metersPerUnit;
                return new CheckpointInfo
                {
                    Name = point.Name,
                    DistanceInMeters = distanceInMeters,
                    NormalizedHeight = GetNormalizedHeight(point.RawZPosition)
                };
            }

            if (currentZ < peakHeight)
            {
                float distanceToPeak = (peakHeight - currentZ) * metersPerUnit;
                return new CheckpointInfo
                {
                    Name = "PEAK",
                    DistanceInMeters = distanceToPeak,
                    NormalizedHeight = 1f
                };
            }
            return null;
        }

        public class CheckpointInfo
        {
            public string Name { get; set; }
            public float DistanceInMeters { get; set; }
            public float NormalizedHeight { get; set; }
        }
    }
}