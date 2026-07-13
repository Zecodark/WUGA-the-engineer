using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class CombatInputController : MonoBehaviour
{
    [SerializeField] private PlayerInput input;
    [SerializeField] private Animator animator;
    [SerializeField] private EquipmentSystem equipmentSystem;
    [FormerlySerializedAs("fallbackCombatLayerName")]
    [SerializeField] private string fullBodyCombatLayerName = "Combat Layer";
    [FormerlySerializedAs("combatLayerName")]
    [SerializeField] private string armsCombatLayerName = "Arms Layer";
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string drawWeaponTrigger = "drawWeapon";
    [SerializeField] private string sheathWeaponTrigger = "sheathWeapon";
    [SerializeField] private string attackTrigger = "attack";
    [SerializeField] private string drawWeaponState = "playerDraw1";
    [SerializeField] private string sheathWeaponState = "playerSheat1";
    [SerializeField] private string attackState = "Combat_Attack";
    [SerializeField] private string comboAttackState = "combat_Attack2";
    [SerializeField] private string fullBodyReadyState = "Combat Blend Tree";
    [SerializeField] private string armsReadyState = "Combat";
    [SerializeField, Min(0f)] private float movingSpeedThreshold = 0.1f;
    [SerializeField, Min(0f)] private float sheathLayerOffDelay = 0.85f;
    [SerializeField, Min(0.1f)] private float attackDuration = 0.82f;
    [SerializeField, Min(0.1f)] private float comboAttackDuration = 0.78f;
    [SerializeField, Min(0f)] private float comboQueueStartTime = 0.22f;
    [SerializeField, Min(0f)] private float comboQueueEndTime = 0.68f;
    [SerializeField, Min(0f)] private float attackReturnTransition = 0.08f;
    [SerializeField, Min(0f)] private float drawReadyDelay = 0.65f;

    private bool weaponDrawn;
    private int fullBodyCombatLayerIndex = -1;
    private int armsCombatLayerIndex = -1;
    private int activeCombatLayerIndex = -1;
    private int speedParameterHash;
    private bool hasSpeedParameter;
    private float sheathLayerOffTimer = -1f;
    private int comboStep;
    private float attackTimer;
    private bool comboQueued;
    private float readyLockTimer;

    public bool IsWeaponDrawn => weaponDrawn;

    private void Awake()
    {
        if (input == null)
            input = GetComponentInChildren<PlayerInput>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (equipmentSystem == null)
            equipmentSystem = GetComponent<EquipmentSystem>();

        if (equipmentSystem == null)
            equipmentSystem = GetComponentInChildren<EquipmentSystem>();

        if (equipmentSystem == null)
            equipmentSystem = GetComponentInParent<EquipmentSystem>();

        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            fullBodyCombatLayerIndex = animator.GetLayerIndex(fullBodyCombatLayerName);
            armsCombatLayerIndex = animator.GetLayerIndex(armsCombatLayerName);
            CacheAnimatorParameters();
            SetAllCombatLayerWeights(0f);
        }

        if (equipmentSystem != null)
            equipmentSystem.SetSheathedVisual();
    }

    private void Update()
    {
        if (input == null)
            return;

        if (input.DrawWeaponTriggered)
        {
            if (weaponDrawn)
                SheathWeapon();
            else
                DrawWeapon();
        }

        if (input.AttackTriggered)
            Attack();

        TickAttackCombo();
        UpdateActiveCombatLayer();
        TickSheathLayerOff();
    }

    public void DrawWeapon()
    {
        weaponDrawn = true;
        sheathLayerOffTimer = -1f;
        readyLockTimer = drawReadyDelay;
        int targetLayerIndex = SelectCombatLayer();
        SetActiveCombatLayer(targetLayerIndex);

        if (equipmentSystem != null)
            equipmentSystem.DrawWeapon();

        ResetTrigger(sheathWeaponTrigger);
        SetTrigger(drawWeaponTrigger);
        PlayCombatState(drawWeaponState, targetLayerIndex, 0.03f);
    }

    public void SheathWeapon()
    {
        weaponDrawn = false;
        ResetAttackCombo();
        readyLockTimer = -1f;
        int targetLayerIndex = SelectCombatLayer();
        SetActiveCombatLayer(targetLayerIndex);

        ResetTrigger(drawWeaponTrigger);
        SetTrigger(sheathWeaponTrigger);
        PlayCombatState(sheathWeaponState, targetLayerIndex, 0.03f);
        sheathLayerOffTimer = sheathLayerOffDelay;
    }

    public void Attack()
    {
        if (!weaponDrawn)
            return;

        int targetLayerIndex = SelectCombatLayer();
        SetActiveCombatLayer(targetLayerIndex);

        ResetTrigger(sheathWeaponTrigger);
        ResetTrigger(drawWeaponTrigger);
        SetTrigger(attackTrigger);

        if (comboStep == 1)
        {
            if (attackTimer <= comboQueueEndTime)
                comboQueued = true;

            return;
        }

        if (comboStep == 2)
            return;

        comboStep = 1;
        attackTimer = 0f;
        comboQueued = false;
        readyLockTimer = -1f;
        PlayCombatState(attackState, targetLayerIndex, 0.03f);
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed)
            Attack();
    }

    public void OnAttack()
    {
        Attack();
    }

    public void FinishSheath()
    {
        weaponDrawn = false;
        ResetAttackCombo();
        readyLockTimer = -1f;
        sheathLayerOffTimer = -1f;
        SetAllCombatLayerWeights(0f);

        if (equipmentSystem != null)
            equipmentSystem.SetSheathedVisual();
    }

    private void SetTrigger(string triggerName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(triggerName))
            return;

        animator.SetTrigger(triggerName);
    }

    private void ResetTrigger(string triggerName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(triggerName))
            return;

        animator.ResetTrigger(triggerName);
    }

    private void SetAllCombatLayerWeights(float weight)
    {
        if (animator == null)
            return;

        if (fullBodyCombatLayerIndex >= 0)
            animator.SetLayerWeight(fullBodyCombatLayerIndex, weight);

        if (armsCombatLayerIndex >= 0)
            animator.SetLayerWeight(armsCombatLayerIndex, weight);

        if (Mathf.Approximately(weight, 0f))
            activeCombatLayerIndex = -1;
    }

    private void SetActiveCombatLayer(int layerIndex)
    {
        if (animator == null || layerIndex < 0)
            return;

        if (fullBodyCombatLayerIndex >= 0)
            animator.SetLayerWeight(fullBodyCombatLayerIndex, layerIndex == fullBodyCombatLayerIndex ? 1f : 0f);

        if (armsCombatLayerIndex >= 0)
            animator.SetLayerWeight(armsCombatLayerIndex, layerIndex == armsCombatLayerIndex ? 1f : 0f);

        activeCombatLayerIndex = layerIndex;
    }

    private int SelectCombatLayer()
    {
        if (IsMovingForCombat() && armsCombatLayerIndex >= 0)
            return armsCombatLayerIndex;

        if (fullBodyCombatLayerIndex >= 0)
            return fullBodyCombatLayerIndex;

        return armsCombatLayerIndex;
    }

    private bool IsMovingForCombat()
    {
        if (animator == null || !hasSpeedParameter)
            return false;

        return animator.GetFloat(speedParameterHash) > movingSpeedThreshold;
    }

    private void UpdateActiveCombatLayer()
    {
        if (!weaponDrawn || comboStep > 0 || sheathLayerOffTimer >= 0f || animator == null)
            return;

        if (readyLockTimer > 0f)
        {
            readyLockTimer -= Time.deltaTime;
            return;
        }

        int targetLayerIndex = SelectCombatLayer();
        if (targetLayerIndex < 0)
            return;

        string readyState = GetReadyState(targetLayerIndex);
        bool layerChanged = targetLayerIndex != activeCombatLayerIndex;
        bool needsReadyState = !animator.IsInTransition(targetLayerIndex) &&
                               !animator.GetCurrentAnimatorStateInfo(targetLayerIndex).IsName(readyState);

        if (!layerChanged && !needsReadyState)
            return;

        SetActiveCombatLayer(targetLayerIndex);
        PlayCombatState(readyState, targetLayerIndex, 0.08f);
    }

    private void TickSheathLayerOff()
    {
        if (sheathLayerOffTimer < 0f)
            return;

        sheathLayerOffTimer -= Time.deltaTime;

        if (sheathLayerOffTimer > 0f)
            return;

        FinishSheath();
    }

    private void TickAttackCombo()
    {
        if (comboStep <= 0 || animator == null)
            return;

        attackTimer += Time.deltaTime;

        if (comboStep == 1)
        {
            if (comboQueued && attackTimer >= comboQueueStartTime)
            {
                comboStep = 2;
                attackTimer = 0f;
                comboQueued = false;
                PlayCombatState(comboAttackState, activeCombatLayerIndex, 0.05f);
                return;
            }

            if (attackTimer >= attackDuration)
            {
                ReturnToReadyState();
                return;
            }
        }

        if (comboStep == 2 && attackTimer >= comboAttackDuration)
            ReturnToReadyState();
    }

    private void ReturnToReadyState()
    {
        int targetLayerIndex = SelectCombatLayer();
        SetActiveCombatLayer(targetLayerIndex);
        PlayCombatState(GetReadyState(targetLayerIndex), targetLayerIndex, attackReturnTransition);
        ResetAttackCombo();
        readyLockTimer = 0.12f;
    }

    private void ResetAttackCombo()
    {
        comboStep = 0;
        attackTimer = 0f;
        comboQueued = false;
    }

    private string GetReadyState(int layerIndex)
    {
        if (layerIndex == fullBodyCombatLayerIndex)
            return fullBodyReadyState;

        return armsReadyState;
    }

    private void PlayCombatState(string stateName, int layerIndex, float transitionDuration)
    {
        if (animator == null ||
            layerIndex < 0 ||
            string.IsNullOrWhiteSpace(stateName))
        {
            return;
        }

        animator.CrossFadeInFixedTime(
            stateName,
            transitionDuration,
            layerIndex
        );
    }

    private void CacheAnimatorParameters()
    {
        if (animator == null || string.IsNullOrWhiteSpace(speedParameter))
            return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Float &&
                parameter.name == speedParameter)
            {
                hasSpeedParameter = true;
                speedParameterHash = Animator.StringToHash(speedParameter);
                return;
            }
        }
    }
}
