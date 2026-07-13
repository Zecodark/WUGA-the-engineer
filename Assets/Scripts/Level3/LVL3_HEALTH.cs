using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LVL3_HEALTH : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField, Min(0f)] private float invulnerabilityTime = 0.75f;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private Animator animator;
    [SerializeField] private Text healthText;
    [SerializeField] private Level3GameOver gameOver;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private string healthTextFormat = "{0}/{1}";
    [SerializeField] private string hitTrigger = "Hit";
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField, Min(0f)] private float generatedHitEffectLifetime = 0.45f;
    [SerializeField] private UnityEvent<int, int> onHealthChanged;
    [SerializeField] private UnityEvent onDeath;

    private int currentHealth;
    private float nextDamageTime;
    private bool isDead;
    private bool hasHitTrigger;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;

    private void Reset()
    {
        movement = GetComponentInChildren<PlayerMovement>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Awake()
    {
        if (movement == null)
            movement = GetComponentInChildren<PlayerMovement>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (healthText == null)
            healthText = FindHealthText();

        if (gameOver == null)
            gameOver = FindFirstObjectByType<Level3GameOver>(FindObjectsInactive.Include);

        if (playerController == null)
            playerController = GetComponentInParent<PlayerController>();

        CacheHitTrigger();
        currentHealth = maxHealth;
        UpdateHealthText();
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        TakeDamage(amount, Vector3.zero, transform.position, 0f);
    }

    public void TakeDamage(int amount, Vector3 hitDirection, Vector3 hitPoint, float knockbackForce)
    {
        if (isDead || amount <= 0 || Time.time < nextDamageTime)
            return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        nextDamageTime = Time.time + invulnerabilityTime;
        UpdateHealthText();
        onHealthChanged?.Invoke(currentHealth, maxHealth);

        Debug.Log($"[LVL3_HEALTH] Player terkena {amount} damage. HP: {currentHealth}/{maxHealth}", this);
        PlayHitFeedback(hitDirection, hitPoint, knockbackForce);

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (isDead || amount <= 0)
            return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateHealthText();
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void PlayHitFeedback(Vector3 hitDirection, Vector3 hitPoint, float knockbackForce)
    {
        if (movement != null && knockbackForce > 0f)
            movement.ApplyKnockback(hitDirection, knockbackForce);

        if (animator != null && hasHitTrigger)
            animator.SetTrigger(hitTrigger);

        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);
            return;
        }

        SpawnGeneratedHitEffect(hitPoint);
    }

    private void SpawnGeneratedHitEffect(Vector3 hitPoint)
    {
        GameObject effect = new GameObject("Level3 Generated Hit Effect");
        effect.transform.position = hitPoint;

        ParticleSystem particles = effect.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.18f;
        main.startLifetime = 0.25f;
        main.startSpeed = 1.8f;
        main.startSize = 0.08f;
        main.startColor = new Color(0.05f, 0.55f, 1f, 0.95f);
        main.maxParticles = 18;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.12f;

        particles.Play(true);
        Destroy(effect, generatedHitEffectLifetime);
    }

    private void CacheHitTrigger()
    {
        if (animator == null || string.IsNullOrWhiteSpace(hitTrigger))
            return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == hitTrigger)
            {
                hasHitTrigger = true;
                return;
            }
        }
    }

    public void ResetHealth()
    {
        isDead = false;
        nextDamageTime = 0f;
        currentHealth = maxHealth;
        UpdateHealthText();
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        UpdateHealthText();

        if (playerController != null)
            playerController.LockInput();

        if (gameOver != null)
            gameOver.ShowGameOver();

        onDeath?.Invoke();
        Debug.Log("[LVL3_HEALTH] Player mati di Level 3.", this);
    }

    private void UpdateHealthText()
    {
        if (healthText == null)
            return;

        healthText.text = string.Format(
            healthTextFormat,
            currentHealth,
            maxHealth
        );
    }

    private Text FindHealthText()
    {
        Text[] texts = FindObjectsByType<Text>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Text text in texts)
        {
            if (text != null && text.name == "Health Text")
                return text;
        }

        return null;
    }
}
