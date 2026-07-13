using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Serialization;

public class BossHealt : MonoBehaviour
{
    [Header("Health Boss")]
    [SerializeField] private int maxHealth = 250;
    [SerializeField] private int currentHealth;

    [Header("Stun Control")]
    [SerializeField, FormerlySerializedAs("healthDepletedStunDuration"), Min(0f)] private float stunDuration = 3f;
    [SerializeField, FormerlySerializedAs("refillHealthAfterHealthStun")] private bool refillHealthAfterStun = true;

    [Header("UI Health")]
    [SerializeField] private Text healthText;

    [Header("References")]
    [SerializeField] private BossAttackControl attackControl;
    [SerializeField] private Animator animator;
    [SerializeField] private string deathTrigger = "Die";

    private bool isDead;
    private bool isRecoveringFromStun;
    private Coroutine stunRecoveryRoutine;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public float StunDuration => stunDuration;

    private void Awake()
    {
        currentHealth = maxHealth;
        ResolveReferences();
        UpdateHealthUI();
    }

    private void Start()
    {
        ResolveReferences();
        UpdateHealthUI();
    }

    private void ResolveReferences()
    {
        if (attackControl == null)
            attackControl = GetComponent<BossAttackControl>();

        if (attackControl == null)
            attackControl = GetComponentInParent<BossAttackControl>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
            animator = GetComponentInParent<Animator>();

        if (healthText == null)
            healthText = FindHealthText("hp_boss", "BOSS HP");
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead || isRecoveringFromStun || damageAmount <= 0)
            return;

        currentHealth = Mathf.Clamp(currentHealth - damageAmount, 0, maxHealth);
        UpdateHealthUI();

        Debug.Log($"[BossHealt] Boss terkena {damageAmount} damage. HP: {currentHealth}/{maxHealth}", this);

        if (currentHealth <= 0)
        {
            StartStunRecovery();
            return;
        }
    }

    public void ResetHealth()
    {
        isDead = false;
        isRecoveringFromStun = false;
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        if (healthText != null)
            healthText.text = $"BOSS HP: {currentHealth}/{maxHealth}";
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

    private void StartStunRecovery()
    {
        ResolveReferences();

        currentHealth = 0;
        UpdateHealthUI();

        if (attackControl != null)
            attackControl.EnterStun(stunDuration);

        if (stunRecoveryRoutine != null)
            StopCoroutine(stunRecoveryRoutine);

        stunRecoveryRoutine = StartCoroutine(RecoverAfterStun());
    }

    private IEnumerator RecoverAfterStun()
    {
        isRecoveringFromStun = true;

        yield return new WaitForSeconds(stunDuration);

        isRecoveringFromStun = false;
        stunRecoveryRoutine = null;

        if (refillHealthAfterStun)
            currentHealth = maxHealth;

        UpdateHealthUI();
    }

    private void Die()
    {
        isDead = true;

        if (attackControl != null)
            attackControl.enabled = false;

        if (animator != null && HasAnimatorParameter(deathTrigger))
            animator.SetTrigger(deathTrigger);

        Debug.Log("[BossHealt] Boss kalah.", this);
    }

    private bool HasAnimatorParameter(string parameterName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parameterName))
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
            if (parameter.name == parameterName)
                return true;

        return false;
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        stunDuration = Mathf.Max(0f, stunDuration);

        if (!Application.isPlaying)
            currentHealth = maxHealth;
    }
}
