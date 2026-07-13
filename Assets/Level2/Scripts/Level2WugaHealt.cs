using UnityEngine;
using UnityEngine.UI;

public class Level2WugaHealt : MonoBehaviour
{
    [Header("Health WUGA")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("UI Health")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Text healthText;

    [Header("Optional")]
    [SerializeField] private Animator animator;
    [SerializeField] private Level2GameKalah gameKalah;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    private void Awake()
    {
        currentHealth = maxHealth;

        if (healthText == null)
            healthText = FindHealthText("hp_wuga", "WUGA HP");

        if (gameKalah == null)
            gameKalah = FindFirstObjectByType<Level2GameKalah>(
                FindObjectsInactive.Include
            );

        UpdateHealthUI();
    }

    public void TakeDamage(int damageAmount)
    {
        if (IsDead || damageAmount <= 0)
            return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthUI();

        if (animator != null)
            animator.SetTrigger("Hit");

        Debug.Log(
            $"WUGA menerima {damageAmount} damage. " +
            $"HP: {currentHealth}/{maxHealth}"
        );

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(int healAmount)
    {
        if (IsDead || healAmount <= 0)
            return;

        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        if (healthSlider == null)
            UpdateHealthText();

        if (healthSlider != null)
        {
            healthSlider.minValue = 0;
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        UpdateHealthText();
    }

    private void UpdateHealthText()
    {
        if (healthText != null)
            healthText.text = $"WUGA HP: {currentHealth}/{maxHealth}";
    }

    private Text FindHealthText(string objectName, string textMarker)
    {
        GameObject textObject = GameObject.Find(objectName);
        if (textObject != null && textObject.TryGetComponent(out Text directText))
            return directText;

        foreach (Text text in Resources.FindObjectsOfTypeAll<Text>())
        {
            if (text == null || !text.gameObject.scene.IsValid())
                continue;

            if (text.name == objectName ||
                (!string.IsNullOrEmpty(text.text) && text.text.Contains(textMarker)))
                return text;
        }

        return null;
    }

    private void Die()
    {
        if (IsDead)
            return;

        IsDead = true;

        if (animator != null)
            animator.SetTrigger("Die");

        if (gameKalah == null)
            gameKalah = FindFirstObjectByType<Level2GameKalah>(
                FindObjectsInactive.Include
            );

        if (gameKalah != null)
            gameKalah.ShowGameKalah();
        else
            Debug.LogWarning("[WugaHealt] GameKalah tidak ditemukan.", this);

        Debug.Log("WUGA kalah.");
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
    }
}
