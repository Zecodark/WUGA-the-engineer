using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class WormyLaneAttacker : MonoBehaviour
{
    [Header("Path")]
    [SerializeField] private Transform attackTarget;
    [SerializeField, Min(0.1f)] private float fallbackAttackDistance = 8f;
    [SerializeField] private bool faceMoveDirection = true;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float initialDelay = 0f;
    [SerializeField, Min(0f)] private float idleTime = 3f;
    [SerializeField, Min(0f)] private float randomIdleTime = 1f;
    [SerializeField, Min(0f)] private float warningTime = 0.6f;
    [SerializeField, Min(0f)] private float holdAtEndTime = 0.25f;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float attackSpeed = 14f;
    [SerializeField, Min(0.1f)] private float returnSpeed = 6f;
    [SerializeField, Min(0.01f)] private float arriveDistance = 0.08f;

    [Header("Damage")]
    [SerializeField, Min(0)] private int damage = 15;
    [SerializeField, Min(0f)] private float damageCooldown = 0.75f;
    [SerializeField] private bool damageOnlyWhileAttacking = true;

    [Header("Animation Optional")]
    [SerializeField] private Animator animator;
    [SerializeField] private string idleTrigger = "Idle";
    [SerializeField] private string warningTrigger = "Warning";
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string returnTrigger = "Return";

    private readonly Dictionary<PlayerHealth, float> nextDamageTimes = new();
    private Vector3 homePosition;
    private Quaternion homeRotation;
    private Collider damageCollider;
    private Rigidbody body;
    private bool isAttacking;
    private Coroutine attackRoutine;

    private void Awake()
    {
        homePosition = transform.position;
        homeRotation = transform.rotation;
        damageCollider = GetComponent<Collider>();
        damageCollider.isTrigger = true;
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        attackRoutine = StartCoroutine(AttackLoop());
    }

    private void OnDisable()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        isAttacking = false;
    }

    private IEnumerator AttackLoop()
    {
        yield return new WaitForSeconds(initialDelay);

        while (enabled)
        {
            isAttacking = false;
            PlayTrigger(idleTrigger);
            yield return new WaitForSeconds(idleTime + Random.Range(0f, randomIdleTime));

            PlayTrigger(warningTrigger);
            yield return new WaitForSeconds(warningTime);

            isAttacking = true;
            PlayTrigger(attackTrigger);
            yield return MoveTo(GetAttackPosition(), attackSpeed);

            isAttacking = false;
            yield return new WaitForSeconds(holdAtEndTime);

            PlayTrigger(returnTrigger);
            yield return MoveTo(homePosition, returnSpeed);

            transform.SetPositionAndRotation(homePosition, homeRotation);
        }
    }

    private IEnumerator MoveTo(Vector3 targetPosition, float speed)
    {
        while ((transform.position - targetPosition).sqrMagnitude > arriveDistance * arriveDistance)
        {
            Vector3 nextPosition = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                speed * Time.deltaTime
            );

            if (faceMoveDirection)
                FaceDirection(nextPosition - transform.position);

            transform.position = nextPosition;
            yield return null;
        }
    }

    private Vector3 GetAttackPosition()
    {
        if (attackTarget != null)
            return attackTarget.position;

        return homePosition + transform.forward * fallbackAttackDistance;
    }

    private void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDamage(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryDamage(other);
    }

    private void TryDamage(Collider other)
    {
        if (damageOnlyWhileAttacking && !isAttacking)
            return;

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health == null)
            return;

        if (nextDamageTimes.TryGetValue(health, out float nextTime) && Time.time < nextTime)
            return;

        health.TakeDamage(damage);
        nextDamageTimes[health] = Time.time + damageCooldown;
    }

    private void PlayTrigger(string triggerName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(triggerName))
            return;

        animator.SetTrigger(triggerName);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 start = Application.isPlaying ? homePosition : transform.position;
        Vector3 end = attackTarget != null
            ? attackTarget.position
            : start + transform.forward * fallbackAttackDistance;

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(start, 0.25f);
        Gizmos.DrawWireSphere(end, 0.25f);
    }
}
