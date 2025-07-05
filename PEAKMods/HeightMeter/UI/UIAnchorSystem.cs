using UnityEngine;
using UnityEngine.UI;
using System;

namespace HeightMeterMod
{
    /// <summary>
    /// Sistema reattivo per ancorare elementi UI ad altri elementi con supporto
    /// per offset dinamici e transizioni smooth
    /// </summary>
    public class UIAnchorSystem : MonoBehaviour
    {
        // Eventi per notificare cambiamenti di posizione
        public event Action<Vector2> OnAnchorPositionChanged;
        
        // Configurazione
        [System.Serializable]
        public class AnchorConfig
        {
            public RectTransform target;           // Elemento da seguire
            public Vector2 offset;                 // Offset base
            public float smoothSpeed = 8f;         // Velocità di interpolazione
            public bool considerExtraElements;     // Considera elementi extra (come extraBar)
        }
        
        private AnchorConfig config;
        private RectTransform myTransform;
        
        // Cache per performance
        private Vector2 lastTargetPosition;
        private Vector2 currentPosition;
        private Vector2 targetPosition;
        
        // Riferimenti StaminaBar specifici
        private StaminaBar staminaBar;
        private float lastExtraBarState;
        
        private void Awake()
        {
            myTransform = GetComponent<RectTransform>();
        }
        
        public void Initialize(AnchorConfig anchorConfig, StaminaBar stamBar = null)
        {
            config = anchorConfig;
            staminaBar = stamBar;
            
            if (config.target == null)
            {
                Utils.LogError("UIAnchorSystem: No target specified!");
                enabled = false;
                return;
            }
            
            // Posizione iniziale
            UpdateTargetPosition();
            currentPosition = targetPosition;
            ApplyPosition();
        }
        
        private void LateUpdate()
        {
            if (!IsValid()) return;
            
            // Controlla se il target si è mosso
            if (HasTargetMoved() || HasExtraBarStateChanged())
            {
                UpdateTargetPosition();
                OnAnchorPositionChanged?.Invoke(targetPosition);
            }
            
            // Smooth interpolation
            if (Vector2.Distance(currentPosition, targetPosition) > 0.01f)
            {
                currentPosition = Vector2.Lerp(
                    currentPosition, 
                    targetPosition, 
                    Time.deltaTime * config.smoothSpeed
                );
                ApplyPosition();
            }
        }
        
        private bool HasTargetMoved()
        {
            Vector2 currentTargetPos = config.target.anchoredPosition;
            bool moved = Vector2.Distance(lastTargetPosition, currentTargetPos) > 0.1f;
            
            if (moved)
            {
                lastTargetPosition = currentTargetPos;
            }
            
            return moved;
        }
        
        private bool HasExtraBarStateChanged()
        {
            if (!config.considerExtraElements || staminaBar == null) 
                return false;
            
            float currentExtraState = GetExtraBarInfluence();
            bool changed = Mathf.Abs(lastExtraBarState - currentExtraState) > 0.01f;
            
            if (changed)
            {
                lastExtraBarState = currentExtraState;
            }
            
            return changed;
        }
        
        private float GetExtraBarInfluence()
        {
            if (staminaBar == null || staminaBar.extraBar == null) 
                return 0f;
            
            // Considera sia l'attivazione che la dimensione dell'extraBar
            if (staminaBar.extraBar.gameObject.activeSelf)
            {
                // Usa l'altezza dell'extraBar + margine
                return staminaBar.extraBar.sizeDelta.y + 10f;
            }
            
            return 0f;
        }
        
        private void UpdateTargetPosition()
        {
            Vector2 basePosition = config.target.anchoredPosition;
            float targetHeight = config.target.rect.height;
            
            // Calcola offset dinamico
            float dynamicOffset = 0f;
            
            if (config.considerExtraElements)
            {
                dynamicOffset = GetExtraBarInfluence();
            }
            
            // Posizione finale = base + altezza target + offset configurato + offset dinamico
            targetPosition = new Vector2(
                basePosition.x + config.offset.x,
                basePosition.y + targetHeight + config.offset.y + dynamicOffset
            );
        }
        
        private void ApplyPosition()
        {
            myTransform.anchoredPosition = currentPosition;
        }
        
        private bool IsValid()
        {
            return config != null && 
                   config.target != null && 
                   myTransform != null;
        }
        
        // API pubblica per aggiornamenti manuali
        public void ForceUpdate()
        {
            if (!IsValid()) return;
            
            UpdateTargetPosition();
            currentPosition = targetPosition;
            ApplyPosition();
        }
        
        public void SetOffset(Vector2 newOffset)
        {
            config.offset = newOffset;
            UpdateTargetPosition();
        }
        
        public Vector2 GetCurrentPosition() => currentPosition;
        public Vector2 GetTargetPosition() => targetPosition;
    }
}