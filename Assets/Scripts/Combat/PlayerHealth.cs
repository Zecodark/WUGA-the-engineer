using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField, Min(0f)] private float invulnerabilityTime = 0.75f;
    [SerializeField] private UnityEvent<int, int> onHealthChanged;
    [SerializeField] private UnityEvent onDeath;

    private int currentHealth;
    private float nextDamageTime;
    private bool isDead;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (isDead || amount <= 0 || Time.time < nextDamageTime)
            return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        nextDamageTime = Time.time + invulnerabilityTime;
        onHealthChanged?.Invoke(currentHealth, maxHealth);

        Debug.Log($"[PlayerHealth] Took {amount} damage. HP: {currentHealth}/{maxHealth}", this);

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (isDead || amount <= 0)
            return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        onDeath?.Invoke();
        Debug.Log("[PlayerHealth] Player died.", this);
    }
}
