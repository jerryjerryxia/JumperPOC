using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// Interface for enemy compatibility with head stomp and hitbox systems.
    /// Ensures new enemy implementations work with existing systems.
    /// </summary>
    public interface IEnemyBase
    {
        // Health system
        float GetCurrentHealth();
        float GetMaxHealth();
        void TakeDamage(float damage);
        bool IsDead { get; }
        
        // Facing direction for hitbox systems
        bool IsFacingRight { get; }
        
        // Layer information for head stomp compatibility  
        LayerMask PlayerLayer { get; }
        
        // Events for UI integration
        event System.Action<float, float> OnHealthChanged;
        event System.Action OnDamageTaken;
    }
}