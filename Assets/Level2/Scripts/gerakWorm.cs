using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class gerakWorm : MonoBehaviour
{
    [Header("Gerak Worm")]
    [SerializeField, Min(0f)] private float moveSpeed = 3f;
    [SerializeField, Min(0f)] private float moveDistance = 6f;
    [SerializeField] private Vector3 localMoveDirection = Vector3.forward;

    [Header("Damage")]
    [SerializeField, Min(0)] private int damage = 10;
    [SerializeField, Min(0f)] private float damageCooldown = 0.75f;

    [Header("Collider")]
    [SerializeField] private bool useTriggerCollider = true;

    private Vector3 startPosition;
    private Vector3 moveDirection;
    private Rigidbody body;
    private float nextDamageTime;

    private void Awake()
    {
        startPosition = transform.position;
        PreparePhysics();
        RefreshMoveDirection();
    }

    private void OnEnable()
    {
        startPosition = transform.position;
        nextDamageTime = 0f;
        RefreshMoveDirection();
    }

    private void Update()
    {
        MoveLinearPingPong();
    }

    private void MoveLinearPingPong()
    {
        if (moveSpeed <= 0f || moveDistance <= 0f)
            return;

        float offset =
            Mathf.PingPong(Time.time * moveSpeed, moveDistance * 2f) -
            moveDistance;

        Vector3 nextPosition =
            startPosition + moveDirection * offset;

        if (body != null)
            body.MovePosition(nextPosition);
        else
            transform.position = nextPosition;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDamageWuga(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryDamageWuga(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryDamageWuga(collision.collider);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryDamageWuga(collision.collider);
    }

    private void TryDamageWuga(Collider other)
    {
        if (other == null ||
            Time.time < nextDamageTime ||
            damage <= 0)
        {
            return;
        }

        Level2WugaHealt health =
            other.GetComponentInParent<Level2WugaHealt>();

        if (health == null)
            health = other.GetComponentInChildren<Level2WugaHealt>();

        if (health == null && !IsPlayer(other))
            return;

        if (health == null)
            health = FindFirstObjectByType<Level2WugaHealt>();

        if (health == null || health.IsDead)
            return;

        nextDamageTime = Time.time + damageCooldown;
        health.TakeDamage(damage);
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") ||
               other.transform.root.CompareTag("Player") ||
               other.name.Contains("WUGA") ||
               other.transform.root.name.Contains("Third Person Character");
    }

    private void PreparePhysics()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        Collider wormCollider = GetComponent<Collider>();
        wormCollider.isTrigger = useTriggerCollider;
    }

    private void RefreshMoveDirection()
    {
        Vector3 direction =
            localMoveDirection.sqrMagnitude <= 0.0001f
                ? Vector3.forward
                : localMoveDirection.normalized;

        moveDirection = transform.TransformDirection(direction).normalized;
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        moveDistance = Mathf.Max(0f, moveDistance);
        damage = Mathf.Max(0, damage);
        damageCooldown = Mathf.Max(0f, damageCooldown);

        if (Application.isPlaying)
            RefreshMoveDirection();
    }
}
