using System.Collections.Generic;
using UnityEngine;

public class Level3PlayerAttackHitbox : MonoBehaviour
{
    [SerializeField] private PlayerInput input;
    [SerializeField] private CombatInputController combatInputController;
    [SerializeField] private Transform attackOrigin;
    [SerializeField, Min(0)] private int damage = 25;
    [SerializeField, Min(0.1f)] private float range = 1.6f;
    [SerializeField, Range(0f, 180f)] private float forwardAngle = 95f;
    [SerializeField, Min(0f)] private float hitDelay = 0.18f;
    [SerializeField, Min(0.01f)] private float activeTime = 0.22f;
    [SerializeField, Min(0f)] private float sameTargetCooldown = 0.45f;
    [SerializeField, Min(0f)] private float snakeKnockbackForce = 3.2f;
    [SerializeField] private LayerMask targetLayers = ~0;

    private readonly Collider[] hitResults = new Collider[16];
    private readonly Dictionary<Level3LanSnake, float> nextHitTimes = new();
    private float hitStartTime = -1f;
    private float hitEndTime = -1f;
    private bool hitTriggeredThisAttack;

    private void Awake()
    {
        if (input == null)
            input = GetComponentInChildren<PlayerInput>();

        if (combatInputController == null)
            combatInputController = GetComponent<CombatInputController>();

        if (combatInputController == null)
            combatInputController = GetComponentInChildren<CombatInputController>();

        if (attackOrigin == null)
            attackOrigin = transform;
    }

    private void Update()
    {
        if (input != null && input.AttackTriggered && CanStartAttack())
            QueueHit();

        if (hitStartTime < 0f || Time.time < hitStartTime || Time.time > hitEndTime)
            return;

        if (!hitTriggeredThisAttack)
        {
            DamageTargetsInRange();
            hitTriggeredThisAttack = true;
        }
    }

    public void AnimationHit()
    {
        if (!CanStartAttack())
            return;

        DamageTargetsInRange();
    }

    private bool CanStartAttack()
    {
        return combatInputController == null || combatInputController.IsWeaponDrawn;
    }

    private void QueueHit()
    {
        hitStartTime = Time.time + hitDelay;
        hitEndTime = hitStartTime + activeTime;
        hitTriggeredThisAttack = false;
    }

    private void DamageTargetsInRange()
    {
        Vector3 origin = attackOrigin.position + attackOrigin.forward * (range * 0.5f);
        int hitCount = Physics.OverlapSphereNonAlloc(
            origin,
            range,
            hitResults,
            targetLayers,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = hitResults[i];
            if (hit == null)
                continue;

            Level3LanSnake snake = hit.GetComponentInParent<Level3LanSnake>();
            if (snake == null || snake.IsDead || !IsInFront(snake.transform.position))
                continue;

            if (nextHitTimes.TryGetValue(snake, out float nextTime) && Time.time < nextTime)
                continue;

            Vector3 knockbackDirection = (snake.transform.position - attackOrigin.position).FlattenedNormalized();
            snake.TakeDamage(damage, knockbackDirection, snakeKnockbackForce);
            nextHitTimes[snake] = Time.time + sameTargetCooldown;
        }
    }

    private bool IsInFront(Vector3 targetPosition)
    {
        Vector3 toTarget = targetPosition - attackOrigin.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.0001f)
            return true;

        return Vector3.Angle(attackOrigin.forward, toTarget.normalized) <= forwardAngle * 0.5f;
    }

    private void OnDrawGizmosSelected()
    {
        Transform originTransform = attackOrigin != null ? attackOrigin : transform;
        Vector3 origin = originTransform.position + originTransform.forward * (range * 0.5f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin, range);
    }
}
