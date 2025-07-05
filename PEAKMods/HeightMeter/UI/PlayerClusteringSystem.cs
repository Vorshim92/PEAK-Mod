using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace HeightMeterMod
{
    /// <summary>
    /// Sistema intelligente per raggruppare e distribuire visivamente
    /// indicatori di giocatori che si trovano ad altezze simili
    /// </summary>
    public class PlayerClusteringSystem : MonoBehaviour
    {
        [System.Serializable]
        public class ClusterConfig
        {
            public float heightThreshold = 0.02f;    // 2% di differenza per raggruppare
            public float verticalSpacing = 25f;      // Spaziatura verticale per stacking
            public bool alternateOffset = true;      // Offset alternato per leggibilità
        }
        
        public class PlayerCluster
        {
            public float averageHeight;
            public List<PlayerHeightIndicator> indicators = new List<PlayerHeightIndicator>();
            
            public void CalculateAverageHeight()
            {
                if (indicators.Count == 0) return;
                averageHeight = indicators.Average(i => i.CurrentPosition);
            }
        }
        
        private ClusterConfig config;
        private Dictionary<PlayerHeightIndicator, Vector2> targetOffsets = new Dictionary<PlayerHeightIndicator, Vector2>();
        private List<PlayerCluster> activeClusters = new List<PlayerCluster>();
        
        // Cache per performance
        private int lastIndicatorCount = 0;
        private float lastClusteringTime = 0f;
        private const float CLUSTERING_INTERVAL = 0.1f; // Ricalcola ogni 100ms
        
        public void Initialize(ClusterConfig clusterConfig)
        {
            config = clusterConfig;
        }
        
        public void ProcessIndicators(Dictionary<Character, PlayerHeightIndicator> indicators)
        {
            // Ottimizzazione: ricalcola solo se necessario
            if (ShouldRecalculateClusters(indicators.Count))
            {
                RecalculateClusters(indicators.Values.ToList());
                lastIndicatorCount = indicators.Count;
                lastClusteringTime = Time.time;
            }
            
            // Applica offset smooth
            ApplyOffsets();
        }
        
        private bool ShouldRecalculateClusters(int currentCount)
        {
            return currentCount != lastIndicatorCount || 
                   Time.time - lastClusteringTime > CLUSTERING_INTERVAL;
        }
        
        private void RecalculateClusters(List<PlayerHeightIndicator> indicators)
        {
            activeClusters.Clear();
            targetOffsets.Clear();
            
            if (indicators.Count <= 1)
            {
                // Reset offset per indicatore singolo
                if (indicators.Count == 1)
                {
                    targetOffsets[indicators[0]] = Vector2.zero;
                }
                return;
            }
            
            // Ordina per altezza
            var sortedIndicators = indicators
                .Where(i => i != null && i.gameObject.activeSelf)
                .OrderBy(i => i.CurrentPosition)
                .ToList();
            
            // Crea clusters
            foreach (var indicator in sortedIndicators)
            {
                var cluster = FindOrCreateCluster(indicator);
                cluster.indicators.Add(indicator);
            }
            
            // Calcola offset per ogni cluster
            foreach (var cluster in activeClusters)
            {
                CalculateClusterOffsets(cluster);
            }
        }
        
        private PlayerCluster FindOrCreateCluster(PlayerHeightIndicator indicator)
        {
            foreach (var cluster in activeClusters)
            {
                if (Mathf.Abs(cluster.averageHeight - indicator.CurrentPosition) <= config.heightThreshold)
                {
                    // Ricalcola media includendo il nuovo indicatore
                    cluster.CalculateAverageHeight();
                    return cluster;
                }
            }
            
            // Crea nuovo cluster
            var newCluster = new PlayerCluster();
            newCluster.averageHeight = indicator.CurrentPosition;
            activeClusters.Add(newCluster);
            return newCluster;
        }
        
        private void CalculateClusterOffsets(PlayerCluster cluster)
        {
            int count = cluster.indicators.Count;
            if (count == 1)
            {
                targetOffsets[cluster.indicators[0]] = Vector2.zero;
                return;
            }
            
            // Ordina per nome per consistenza visiva
            cluster.indicators.Sort((a, b) => 
                a.character.name.CompareTo(b.character.name));
            
            // NUOVO: Usa sempre stacking verticale per i cluster
            CalculateVerticalStackingOffsets(cluster);
        }
        
        private void CalculateVerticalStackingOffsets(PlayerCluster cluster)
        {
            int count = cluster.indicators.Count;
            
            // Impila verticalmente i nomi
            for (int i = 0; i < count; i++)
            {
                // Offset verticale: ogni nome è spostato di 25 pixel sopra il precedente
                float yOffset = i * config.verticalSpacing;
                
                // Piccolo offset orizzontale alternato per leggibilità (zigzag)
                float xOffset = 0f;
                if (config.alternateOffset && count > 2)
                {
                    xOffset = (i % 2 == 0) ? 0f : 30f;
                }
                
                targetOffsets[cluster.indicators[i]] = new Vector2(xOffset, yOffset);
            }
        }
        
        private void ApplyOffsets()
        {
            foreach (var kvp in targetOffsets)
            {
                if (kvp.Key != null && kvp.Key.gameObject.activeSelf)
                {
                    kvp.Key.SetOffset(kvp.Value);
                }
            }
        }
        
        // API pubblica
        public Vector2 GetIndicatorOffset(PlayerHeightIndicator indicator)
        {
            return targetOffsets.TryGetValue(indicator, out Vector2 offset) ? offset : Vector2.zero;
        }
        
        public int GetClusterCount() => activeClusters.Count;
        
        public PlayerCluster GetLargestCluster()
        {
            return activeClusters.OrderByDescending(c => c.indicators.Count).FirstOrDefault();
        }
        
        // Debug visualization
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || activeClusters == null) return;
            
            foreach (var cluster in activeClusters)
            {
                Gizmos.color = Color.yellow;
                foreach (var indicator in cluster.indicators)
                {
                    if (indicator != null && targetOffsets.TryGetValue(indicator, out Vector2 offset))
                    {
                        Vector3 worldPos = indicator.transform.position;
                        Gizmos.DrawWireSphere(worldPos, 10f);
                    }
                }
            }
        }
    }
}