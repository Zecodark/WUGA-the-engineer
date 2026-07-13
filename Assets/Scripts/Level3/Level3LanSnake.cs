using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class Level3LanSnake : MonoBehaviour
{
    private enum SnakeState
    {
        Idle,
        Patrol,
        Turning,
        Charge
    }

    [Header("Health")]
    [SerializeField, Min(1)] private int maxHealth = 50;
    [SerializeField] private UnityEvent<int, int> onHealthChanged;
    [SerializeField] private UnityEvent onDeath;

    [Header("Guard Rhythm")]
    [SerializeField, Min(0.1f)] private float guardRadius = 4f;
    [SerializeField, Min(0f)] private float initialIdleTime = 0.8f;
    [SerializeField, Min(0f)] private float stopBeforeTurnTime = 0.45f;
    [SerializeField, Min(0.01f)] private float turnAroundTime = 0.45f;
    [SerializeField, Min(0.1f)] private float moveSpeed = 3f;
    [SerializeField, Min(0.05f)] private float wallCheckDistance = 0.8f;
    [SerializeField, Min(0.01f)] private float wallCheckRadius = 0.35f;
    [SerializeField] private bool stayOnStartHeight = true;
    [SerializeField] private LayerMask obstacleLayers = ~0;

    [Header("Damage Player")]
    [SerializeField, Min(0)] private int playerDamage = 20;
    [SerializeField, Min(0f)] private float damageCooldown = 0.75f;
    [SerializeField, Min(0f)] private float knockbackForce = 4f;

    [Header("Hit Reaction")]
    [SerializeField, Min(0f)] private float hitKnockbackForce = 2.8f;
    [SerializeField, Min(0f)] private float hitKnockbackDamping = 9f;
    [SerializeField, Min(0f)] private float hitPauseTime = 0.18f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string moveParameter = "move";
    [SerializeField] private string idleStateName = "Armature|Snake_Idle";
    [SerializeField] private string moveStateName = "Armature|Snake_Chase_InPlace";
    [SerializeField, Min(0f)] private float animationTransitionTime = 0.12f;

    private readonly Dictionary<LVL3_HEALTH, float> nextDamageTimes = new();
    private Rigidbody body;
    private Collider snakeCollider;
    private Vector3 guardCenter;
    private Vector3 moveDirection;
    private Vector3 knockbackVelocity;
    private Quaternion turnFromRotation;
    private Quaternion turnToRotation;
    private float fixedHeight;
    private float stateTimer;
    private float hitPauseTimer;
    private int currentHealth;
    private bool isDead;
    private SnakeState state;
    private bool lastMovingAnimation;
    private bool hasMovingAnimation;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        snakeCollider = GetComponent<Collider>();

        body.isKinematic = true;
        body.useGravity = false;
        snakeCollider.isTrigger = true;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.enabled = true;
            animator.speed = 1f;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        guardCenter = transform.position;
        fixedHeight = transform.position.y;
        ExcludePlayerLayerFromObstacles();
        moveDirection = GetBodyForwardDirection();
        currentHealth = maxHealth;
        onHealthChanged?.Invoke(currentHealth, maxHealth);
        EnterIdle(initialIdleTime);
    }

    private void FixedUpdate()
    {
        if (isDead)
            return;

        TickState(Time.fixedDeltaTime);
    }

    private void TickState(float deltaTime)
    {
        ApplyKnockbackMotion(deltaTime);

        switch (state)
        {
            case SnakeState.Idle:
                TickIdle(deltaTime);
                break;
            case SnakeState.Patrol:
                TickPatrol(deltaTime);
                break;
            case SnakeState.Turning:
                TickTurning(deltaTime);
                break;
        }
    }

    private void TickIdle(float deltaTime)
    {
        PlayMoveAnimation(false);
        stateTimer -= deltaTime;

        if (stateTimer <= 0f)
            EnterPatrol();
    }

    private void TickPatrol(float deltaTime)
    {
        if (hitPauseTimer > 0f)
        {
            hitPauseTimer -= deltaTime;
            PlayMoveAnimation(false);
            return;
        }

        if (ShouldTurnAround(body.position))
        {
            EnterTurning();
            return;
        }

        MoveForward(moveSpeed, deltaTime);
        PlayMoveAnimation(true);
    }

    private void TickTurning(float deltaTime)
    {
        PlayMoveAnimation(false);
        stateTimer -= deltaTime;

        if (stateTimer > turnAroundTime)
            return;

        float turnProgress = Mathf.InverseLerp(turnAroundTime, 0f, stateTimer);
        body.MoveRotation(Quaternion.Slerp(turnFromRotation, turnToRotation, turnProgress));

        if (stateTimer <= 0f)
            EnterPatrol();
    }

    private void MoveForward(float speed, float deltaTime)
    {
        Vector3 nextPosition = body.position + moveDirection * (speed * deltaTime);
        if (stayOnStartHeight)
            nextPosition.y = fixedHeight;

        body.MovePosition(nextPosition);
    }

    public void TakeDamage(int amount)
    {
        TakeDamage(amount, Vector3.zero, 0f);
    }

    public void TakeDamage(int amount, Vector3 hitDirection, float force)
    {
        if (isDead || amount <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        onHealthChanged?.Invoke(currentHealth, maxHealth);

        Debug.Log($"[Level3LanSnake] {name} terkena {amount} damage. HP: {currentHealth}/{maxHealth}", this);
        ApplyKnockback(hitDirection, force > 0f ? force : hitKnockbackForce);

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        PlayMoveAnimation(false, true);
        onDeath?.Invoke();
        gameObject.SetActive(false);
    }

    private void EnterIdle(float duration)
    {
        state = SnakeState.Idle;
        stateTimer = duration;
        PlayMoveAnimation(false, true);
    }

    private void EnterPatrol()
    {
        state = SnakeState.Patrol;
        PlayMoveAnimation(true, true);
    }

    private void EnterTurning()
    {
        state = SnakeState.Turning;
        stateTimer = stopBeforeTurnTime + turnAroundTime;
        moveDirection = -moveDirection.FlattenedNormalized();
        turnFromRotation = body.rotation;
        turnToRotation = turnFromRotation * Quaternion.Euler(0f, 180f, 0f);
        PlayMoveAnimation(false, true);
    }

    private bool ShouldTurnAround(Vector3 currentPosition)
    {
        Vector3 fromCenter = currentPosition - guardCenter;
        fromCenter.y = 0f;

        if (guardRadius > 0f &&
            fromCenter.sqrMagnitude >= guardRadius * guardRadius &&
            Vector3.Dot(moveDirection, fromCenter) > 0f)
        {
            return true;
        }

        Vector3 origin = currentPosition + Vector3.up * 0.15f;
        return Physics.SphereCast(
            origin,
            wallCheckRadius,
            moveDirection,
            out RaycastHit hit,
            wallCheckDistance,
            obstacleLayers,
            QueryTriggerInteraction.Ignore
        ) && hit.collider != null && !hit.collider.transform.IsChildOf(transform);
    }

    private void ExcludePlayerLayerFromObstacles()
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
            obstacleLayers &= ~(1 << playerLayer);
    }

    private Vector3 GetBodyForwardDirection()
    {
        return (-transform.right).FlattenedNormalized();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDamagePlayer(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryDamagePlayer(other);
    }

    private void TryDamagePlayer(Collider other)
    {
        LVL3_HEALTH health = other.GetComponentInParent<LVL3_HEALTH>();
        if (health == null || health.IsDead)
            return;

        if (nextDamageTimes.TryGetValue(health, out float nextTime) && Time.time < nextTime)
            return;

        Vector3 hitDirection = (health.transform.position - transform.position).FlattenedNormalized();
        health.TakeDamage(playerDamage, hitDirection, other.bounds.ClosestPoint(transform.position), knockbackForce);
        nextDamageTimes[health] = Time.time + damageCooldown;
        hitPauseTimer = Mathf.Max(hitPauseTimer, hitPauseTime);
    }

    private void ApplyKnockback(Vector3 direction, float force)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f || force <= 0f)
            return;

        knockbackVelocity = direction.normalized * force;
        hitPauseTimer = Mathf.Max(hitPauseTimer, hitPauseTime);
        PlayMoveAnimation(false, true);
    }

    private void ApplyKnockbackMotion(float deltaTime)
    {
        if (knockbackVelocity.sqrMagnitude < 0.0001f)
            return;

        Vector3 nextPosition = body.position + knockbackVelocity * deltaTime;
        if (stayOnStartHeight)
            nextPosition.y = fixedHeight;

        body.MovePosition(nextPosition);
        knockbackVelocity = Vector3.MoveTowards(knockbackVelocity, Vector3.zero, hitKnockbackDamping * deltaTime);
    }

    private void PlayMoveAnimation(bool moving, bool force = false)
    {
        if (animator == null)
            return;

        animator.speed = 1f;

        if (!string.IsNullOrWhiteSpace(moveParameter))
            animator.SetBool(moveParameter, moving);

        if (!force && hasMovingAnimation && lastMovingAnimation == moving)
            return;

        string stateName = moving ? moveStateName : idleStateName;
        if (!string.IsNullOrWhiteSpace(stateName))
            animator.CrossFadeInFixedTime(stateName, animationTransitionTime, 0);

        lastMovingAnimation = moving;
        hasMovingAnimation = true;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? guardCenter : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(center, guardRadius);

        Gizmos.color = Color.green;
        Vector3 forward = Application.isPlaying ? moveDirection : GetBodyForwardDirection();
        Gizmos.DrawLine(transform.position, transform.position + forward * 2f);
    }
}

internal static class Level3VectorExtensions
{
    public static Vector3 FlattenedNormalized(this Vector3 value)
    {
        value.y = 0f;
        return value.sqrMagnitude < 0.0001f ? Vector3.forward : value.normalized;
    }
}
