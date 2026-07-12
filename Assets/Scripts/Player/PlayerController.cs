using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{

    [SerializeField] private PlayerInput input;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private Transform cam;

    [Header("Combat")]
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject weaponObject;
    [SerializeField] private string drawWeaponTrigger = "drawWeapon";
    [SerializeField] private string sheathWeaponTrigger = "sheathWeapon";
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string combatLayerName = "Combat Layer";
    [SerializeField] private bool useKeyboardFallback = true;
    [SerializeField] private Key drawWeaponKey = Key.Digit3;
    [SerializeField, Min(0f)] private float attackCooldown = 0.45f;
    [SerializeField, Min(0f)] private float sheathLayerDisableDelay = 2.2f;
    [SerializeField] private bool hideWeaponWhenSheathed = true;

    private bool inputLocked;
    private bool weaponDrawn;
    private float nextAttackTime;
    private int combatLayerIndex = -1;
    private Coroutine sheathRoutine;
    private bool hasDrawTrigger;
    private bool hasSheathTrigger;
    private bool hasAttackTrigger;

    public bool IsInputLocked => inputLocked;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        CacheCombatAnimator();
        SetCombatLayerWeight(0f);
        UpdateWeaponVisibility();
    }

    void Update()
    {

        if (inputLocked)
        {
            movement.Move(Vector2.zero, cam);
            return;
        }

        movement.Move(input.moveDirection, cam);

        if (input.JumpTriggered)
        {
            movement.Jump();
        }

        if (IsDrawWeaponPressed())
            ToggleWeapon();

        if (IsAttackPressed())
            Attack();
    }

    

    public void LockInput()
    {
        inputLocked = true;
        Debug.Log("[PlayerController] Input LOCKED");
    }

    public void UnlockInput()
    {
        inputLocked = false;
        Debug.Log("[PlayerController] Input UNLOCKED");
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

    private void ToggleWeapon()
    {
        if (weaponDrawn)
            SheathWeapon();
        else
            DrawWeapon();
    }

    private void DrawWeapon()
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

    private void SheathWeapon()
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

    private void Attack()
    {
        if (!weaponDrawn || Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;
        ResetTriggerIfExists(drawWeaponTrigger, hasDrawTrigger);
        ResetTriggerIfExists(sheathWeaponTrigger, hasSheathTrigger);
        SetTriggerIfExists(attackTrigger, hasAttackTrigger);
    }

    private void CacheCombatAnimator()
    {
        if (animator == null)
            return;

        combatLayerIndex = animator.GetLayerIndex(combatLayerName);
        hasDrawTrigger = HasAnimatorParameter(drawWeaponTrigger);
        hasSheathTrigger = HasAnimatorParameter(sheathWeaponTrigger);
        hasAttackTrigger = HasAnimatorParameter(attackTrigger);
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

    private void UpdateWeaponVisibility()
    {
        if (!hideWeaponWhenSheathed || weaponObject == null)
            return;

        weaponObject.SetActive(weaponDrawn);
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
