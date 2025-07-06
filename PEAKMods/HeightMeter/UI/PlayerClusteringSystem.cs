using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace HeightMeterMod
{
    /// <summary>
    /// Sistema intelligente per raggruppare e distribuire visivamente
    /// indicatori di giocatori che si trovano ad altezze simili.
    /// Versione 2.0: Algoritmo O(n log n) e stacking verticale basato sull'altezza.
    /// </summary>
    public class PlayerClusteringSystem : MonoBehaviour
    {
        [System.Serializable]
        public class ClusterConfig
        {
            public float heightThreshold = 0.025f;   // 2.5% di differenza per raggruppare
            public float verticalSpacing = 22f;      // Spaziatura verticale per stacking
        }

        public class PlayerCluster
        {
            public List<PlayerHeightIndicator> indicators = new List<PlayerHeightIndicator>();
            
            // [ARCHITECT'S NOTE] La media non è più necessaria. La logica di clustering
            // si basa sulla prossimità relativa, non su un centroide mobile.
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
            if (ShouldRecalculateClusters(indicators.Count))
            {
                // [ARCHITECT'S NOTE] Il metodo è stato completamente riscritto per efficienza e correttezza.
                UpdateClusters(indicators.Values.ToList());
                lastIndicatorCount = indicators.Count;
                lastClusteringTime = Time.time;
            }

            ApplyOffsets();
        }

        private bool ShouldRecalculateClusters(int currentCount)
        {
            return currentCount != lastIndicatorCount || Time.time - lastClusteringTime > CLUSTERING_INTERVAL;
        }

        /// <summary>
        /// Algoritmo di clustering e calcolo degli offset completamente rivisto.
        /// Complessità: O(n log n) a causa dell'ordinamento iniziale.
        /// </summary>
        private void UpdateClusters(List<PlayerHeightIndicator> allIndicators)
        {
            activeClusters.Clear();
            targetOffsets.Clear();

            var activeIndicators = allIndicators
                .Where(i => i != null && i.gameObject.activeInHierarchy)
                .ToList();

            // Resetta tutti gli offset esistenti. Questo garantisce che gli indicatori
            // che escono da un cluster tornino alla loro posizione di default (0,0).
            foreach (var indicator in activeIndicators)
            {
                targetOffsets[indicator] = Vector2.zero;
            }

            if (activeIndicators.Count <= 1)
            {
                return;
            }

            // [ARCHITECT'S NOTE] #1: Sort-Once. Ordiniamo TUTTI gli indicatori una sola volta.
            // Questo è il fondamento dell'algoritmo efficiente.
            var sortedIndicators = activeIndicators.OrderBy(i => i.CurrentPosition).ToList();

            // [ARCHITECT'S NOTE] #2: Cluster-Once. Creiamo i cluster in un unico passaggio.
            PlayerCluster currentCluster = null;
            for (int i = 0; i < sortedIndicators.Count; i++)
            {
                var indicator = sortedIndicators[i];
                if (currentCluster == null)
                {
                    currentCluster = new PlayerCluster();
                    currentCluster.indicators.Add(indicator);
                    activeClusters.Add(currentCluster);
                }
                else
                {
                    // Controlla se l'indicatore attuale è abbastanza vicino all'ULTIMO indicatore aggiunto al cluster.
                    var lastInCluster = currentCluster.indicators.Last();
                    if (Mathf.Abs(indicator.CurrentPosition - lastInCluster.CurrentPosition) <= config.heightThreshold)
                    {
                        currentCluster.indicators.Add(indicator);
                    }
                    else
                    {
                        // L'indicatore è troppo lontano, inizia un nuovo cluster.
                        currentCluster = new PlayerCluster();
                        currentCluster.indicators.Add(indicator);
                        activeClusters.Add(currentCluster);
                    }
                }
            }

            // [ARCHITECT'S NOTE] #3: Calculate Offsets. Calcoliamo gli offset per ogni cluster.
            foreach (var cluster in activeClusters)
            {
                // Saltiamo i "cluster" con un solo membro, il loro offset è già Vector2.zero.
                if (cluster.indicators.Count <= 1) continue;

                CalculateVerticalStackingOffsets(cluster);
            }
        }

        /// <summary>
        /// Calcola gli offset per un singolo cluster, impilando verticalmente
        /// gli indicatori in base alla loro altezza reale.
        /// </summary>
        private void CalculateVerticalStackingOffsets(PlayerCluster cluster)
        {
            // [ARCHITECT'S NOTE] #4: Height-First Principle. Ordiniamo per altezza DISCENDENTE.
            // Chi è più in alto nel gioco (valore CurrentPosition maggiore) deve stare in cima allo stack (offset Y maggiore).
            var sortedByHeight = cluster.indicators.OrderByDescending(i => i.CurrentPosition).ToList();
            
            int count = sortedByHeight.Count;
            
            for (int i = 0; i < count; i++)
            {
                var indicator = sortedByHeight[i];
                
                // L'offset Y è basato sulla posizione nell'ordinamento (i).
                // L'indicatore più in alto (i=0) ottiene l'offset Y più grande.
                // Invertiamo l'ordine per avere lo stacking verso l'alto
                float yOffset = (count - 1 - i) * config.verticalSpacing;
                
                // [ARCHITECT'S NOTE] #5: Simplicity. L'offset X è sempre 0 per uno stack pulito.
                // La logica "zig-zag" è stata rimossa.
                float xOffset = 0f;
                
                targetOffsets[indicator] = new Vector2(xOffset, yOffset);
            }
        }

        private void ApplyOffsets()
        {
            foreach (var kvp in targetOffsets)
            {
                if (kvp.Key != null && kvp.Key.gameObject.activeInHierarchy)
                {
                    kvp.Key.SetOffset(kvp.Value);
                }
            }
        }
        
        // --- API Pubbliche (invariate) ---
        public Vector2 GetIndicatorOffset(PlayerHeightIndicator indicator)
        {
            return targetOffsets.TryGetValue(indicator, out Vector2 offset) ? offset : Vector2.zero;
        }

        public int GetClusterCount() => activeClusters.Count;
        
        public PlayerCluster GetLargestCluster()
        {
            return activeClusters.OrderByDescending(c => c.indicators.Count).FirstOrDefault();
        }
    }
}