using UnityEngine;
using System.Linq;

namespace HeightMeterMod
{
    public class HeightCalculator : MonoBehaviour
    {
        // Height bounds
        private float baseHeight = 0f;
        private float peakHeight = 1920f; // Default fallback
        
        // Progress points from the game
        private MountainProgressHandler.ProgressPoint[] progressPoints;
        
        // Conversion factor
        private float metersPerUnit = 1f;
        
        public bool IsInitialized { get; private set; }
        
        public bool Initialize()
        {
            // Try to get progress points from the game
            var progressHandler = Object.FindAnyObjectByType<MountainProgressHandler>();
            
            if (progressHandler?.progressPoints != null && progressHandler.progressPoints.Length > 0)
            {
                progressPoints = progressHandler.progressPoints;
                
                // Calculate height bounds from progress points
                baseHeight = progressPoints[0].transform.position.z;
                peakHeight = progressPoints.Last().transform.position.z;
                
                // Calculate conversion factor
                float totalUnits = peakHeight - baseHeight;
                if (totalUnits > 0) // Evita divisione per zero
                {
                    metersPerUnit = 1920f / totalUnits;
                }
                
                Utils.LogInfo($"Height bounds initialized: Base={baseHeight:F2}, Peak={peakHeight:F2}, MetersPerUnit={metersPerUnit:F2}");
                IsInitialized = true; // Imposta lo stato su inizializzato
                return true; // Successo!
            }
            else
            {
                Utils.LogWarning("Could not find progress points. Waiting for valid game state...");
                IsInitialized = false; // Non inizializzato
                return false; // Fallimento!
            }
        }
        
        // Get normalized height (0-1) for UI positioning
        public float GetNormalizedHeight(float zPosition)
        {
            if (peakHeight <= baseHeight) return 0f;
            
            return Mathf.Clamp01((zPosition - baseHeight) / (peakHeight - baseHeight));
        }
        
        // Get height in meters for display
        public float GetHeightInMeters(float zPosition)
        {
            float relativeHeight = zPosition - baseHeight;
            return Mathf.Max(0f, relativeHeight * metersPerUnit);
        }
        
        // Get the next checkpoint info
        public CheckpointInfo GetNextCheckpoint(float currentZ)
        {
            if (progressPoints == null || progressPoints.Length == 0)
                return null;
                
            // Trova il prossimo checkpoint non raggiunto E VALIDO
            foreach (var point in progressPoints.Where(p => p != null && p.transform != null))
            {
                if (!point.Reached && point.transform.position.z > currentZ)
                {
                    float distanceInMeters = (point.transform.position.z - currentZ) * metersPerUnit;
                    return new CheckpointInfo
                    {
                        Name = point.title,
                        DistanceInMeters = distanceInMeters,
                        NormalizedHeight = GetNormalizedHeight(point.transform.position.z)
                    };
                }
            }

            
            // Player is above all checkpoints
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
        
        // Get all progress points for UI markers
        public ProgressMarker[] GetProgressMarkers()
        {
            if (progressPoints == null) return new ProgressMarker[0];
            
            // Aggiungi un filtro .Where() per ignorare i punti con transform nullo
            return progressPoints
                .Where(p => p != null && p.transform != null) // <-- QUESTA È LA CORREZIONE
                .Select(p => new ProgressMarker
                {
                    Name = p.title,
                    NormalizedHeight = GetNormalizedHeight(p.transform.position.z),
                    HeightInMeters = GetHeightInMeters(p.transform.position.z),
                    IsReached = p.Reached
                }).ToArray();
        }

        
        // Helper classes for data
        public class CheckpointInfo
        {
            public string Name { get; set; }
            public float DistanceInMeters { get; set; }
            public float NormalizedHeight { get; set; }
        }
        
        public class ProgressMarker
        {
            public string Name { get; set; }
            public float NormalizedHeight { get; set; }
            public float HeightInMeters { get; set; }
            public bool IsReached { get; set; }
        }
    }
}