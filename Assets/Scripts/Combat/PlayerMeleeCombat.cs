using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerMeleeCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInput input;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject weaponObject;

    [Header("Animator Parameters")]
    [SerializeField] private string drawWeaponTrigger = "drawWeapon";
    [SerializeField] private string sheathWeaponTrigger = "sheathWeapon";
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string combatLayerName = "Combat Layer";

    [Header("Input Fallback")]
    [SerializeField] private bool useKeyboardFallback = true;
    [SerializeField] private Key drawWeaponKey = Key.Digit3;

    [Header("Combat")]
    [SerializeField, Min(0f)] private float attackCooldown = 0.45f;
    [SerializeField, Min(0f)] private float sheathLayerDisableDelay = 2.2f;
    [SerializeField] private bool hideWeaponWhenSheathed = true;

    private bool weaponDrawn;
    private float nextAttackTime;
    private int combatLayerIndex = -1;
    private Coroutine sheathRoutine;
    private bool hasDrawTrigger;
    private bool hasSheathTrigger;
    private bool hasAttackTrigger;

    public bool WeaponDrawn => weaponDrawn;

    private void Awake()
    {
        if (input == null)
            input = GetComponent<PlayerInput>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        CacheAnimatorParameters();
        CacheCombatLayer();
        SetCombatLayerWeight(0f);
        UpdateWeaponVisibility();
    }

    private void Update()
    {
        if (IsDrawWeaponPressed())
            ToggleWeapon();

        if (IsAttackPressed())
            Attack();
    }

    public void ToggleWeapon()
    {
        if (weaponDrawn)
            SheathWeapon();
        else
            DrawWeapon();
    }

    public void DrawWeapon()
    {
        if (weaponDrawn)
            return;

        weaponDrawn = true;
        StopSheathRoutine();
        ResetTriggerIfExists(sheathWeaponTrigger, hasSheathTrigger);
        ResetTriggerIfExists(attackTrigger, hasAttackTrigger);
        SetCombatLayerWeight(1f);
        UpdateWeaponVisibility();
        SetTriggerIfExists(drawWeaponTrigger, hasDrawTrigger);
    }

    public void SheathWeapon()
    {
        if (!weaponDrawn)
            return;

        weaponDrawn = false;
        ResetTriggerIfExists(drawWeaponTrigger, hasDrawTrigger);
        ResetTriggerIfExists(attackTrigger, hasAttackTrigger);
        SetTriggerIfExists(sheathWeaponTrigger, hasSheathTrigger);
        UpdateWeaponVisibility();

        StopSheathRoutine();
        sheathRoutine = StartCoroutine(DisableCombatLayerAfterDelay());
    }

    public void Attack()
    {
        if (!weaponDrawn || Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;
        ResetTriggerIfExists(drawWeaponTrigger, hasDrawTrigger);
        ResetTriggerIfExists(sheathWeaponTrigger, hasSheathTrigger);
        SetTriggerIfExists(attackTrigger, hasAttackTrigger);
    }

    private bool IsDrawWeaponPressed()
    {
        if (input != null && input.DrawWeaponTriggered)
            return true;

        return useKeyboardFallback &&
               (input == null || !input.HasDrawWeaponAction) &&
               Keyboard.current != null &&
               Keyboard.current[drawWeaponKey].wasPressedThisFrame;
    }

    private bool IsAttackPressed()
    {
        if (input != null && input.AttackTriggered)
            return true;

        return useKeyboardFallback &&
               (input == null || !input.HasAttackAction) &&
               Mouse.current != null &&
               Mouse.current.leftButton.wasPressedThisFrame;
    }

    private void UpdateWeaponVisibility()
    {
        if (!hideWeaponWhenSheathed || weaponObject == null)
            return;

        weaponObject.SetActive(weaponDrawn);
    }

    private void CacheAnimatorParameters()
    {
        if (animator == null)
            return;

        hasDrawTrigger = HasAnimatorParameter(drawWeaponTrigger);
        hasSheathTrigger = HasAnimatorParameter(sheathWeaponTrigger);
        hasAttackTrigger = HasAnimatorParameter(attackTrigger);
    }

    private void CacheCombatLayer()
    {
        if (animator == null || string.IsNullOrWhiteSpace(combatLayerName))
            return;

        combatLayerIndex = animator.GetLayerIndex(combatLayerName);
    }

    private bool HasAnimatorParameter(string parameterName)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
            if (parameter.name == parameterName)
                return true;

        return false;
    }

    private void SetTriggerIfExists(string triggerName, bool exists)
    {
        if (animator == null || !exists)
            return;

        animator.SetTrigger(triggerName);
    }

    private void ResetTriggerIfExists(string triggerName, bool exists)
    {
        if (animator == null || !exists)
            return;

        animator.ResetTrigger(triggerName);
    }

    private void SetCombatLayerWeight(float weight)
    {
        if (animator == null || combatLayerIndex < 0)
            return;

        animator.SetLayerWeight(combatLayerIndex, weight);
    }

    private IEnumerator DisableCombatLayerAfterDelay()
    {
        yield return new WaitForSeconds(sheathLayerDisableDelay);
        SetCombatLayerWeight(0f);
        sheathRoutine = null;
    }

    private void StopSheathRoutine()
    {
        if (sheathRoutine == null)
            return;

        StopCoroutine(sheathRoutine);
        sheathRoutine = null;
    }

}
