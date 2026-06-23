using UnityEngine;

public class SimpleBitFollower : MonoBehaviour
{
    [Header("Target Wuga")]
    [SerializeField] private Transform player;

    [Header("Jarak Gerak dari Wuga")]
    [Tooltip("X = jarak minimum, Y = jarak maksimum Bit dari Wuga.")]
    [SerializeField] private Vector2 movementDistance = new(1.5f, 3.5f);

    [Tooltip("X = tinggi minimum, Y = tinggi maksimum dari posisi Wuga.")]
    [SerializeField] private Vector2 heightOffset = new(0.5f, 1.25f);

    [Tooltip("Seberapa jauh Bit boleh melewati jarak maksimum sebelum dipaksa kembali.")]
    [SerializeField, Min(0f)] private float returnTolerance = 0.5f;

    [Header("Jarak Aman dari Wuga")]
    [Tooltip("Ruang kosong tambahan antara collider Bit dan collider Wuga.")]
    [SerializeField, Min(0f)] private float playerClearance = 0.45f;

    [Header("Gerakan Acak")]
    [SerializeField, Min(0.01f)] private float moveSpeed = 2.2f;
    [SerializeField, Min(0.01f)] private float acceleration = 5f;
    [SerializeField, Min(0.01f)] private float arrivalDistance = 0.15f;

    [Tooltip("Rentang waktu sebelum Bit memilih posisi acak baru.")]
    [SerializeField] private Vector2 destinationChangeInterval =
        new(1.5f, 4f);

    [SerializeField, Min(1)] private int destinationSearchAttempts = 12;

    [Header("Rotasi Acak")]
    [SerializeField, Min(0f)] private float rotationSpeed = 120f;

    [Tooltip("Rentang waktu sebelum Bit memilih arah rotasi baru.")]
    [SerializeField] private Vector2 rotationChangeInterval =
        new(0.8f, 2.5f);

    [Header("Hover")]
    [SerializeField, Min(0f)] private float hoverAmount = 0.08f;
    [SerializeField, Min(0f)] private float hoverSpeed = 2f;

    [Header("Deteksi Tembok dan Collider")]
    [Tooltip("Radius tubuh Bit untuk pengecekan tabrakan.")]
    [SerializeField, Min(0.01f)] private float collisionRadius = 0.3f;

    [Tooltip("Posisi pusat deteksi collider relatif terhadap pivot Bit.")]
    [SerializeField] private Vector3 collisionCenterOffset =
        new(0f, 0.35f, 0f);

    [Tooltip("Jarak aman yang dipertahankan dari permukaan collider.")]
    [SerializeField, Min(0.001f)] private float collisionSkin = 0.04f;

    [Tooltip("Pilih layer tembok/lingkungan. Default scene ini memakai layer Default.")]
    [SerializeField] private LayerMask obstacleLayers = 1;
    [SerializeField, Min(0f)] private float blockedRetargetDelay = 0.2f;
    [SerializeField] private Animator animator;


    private bool following;
    private bool hasDestination;
    private Vector3 destination;
    private float currentSpeed;
    private float destinationTimer;
    private float rotationTimer;
    private float targetYaw;
    private float blockedTimer;
    private CharacterController playerCollider;
   

    public bool IsFollowing => following;

    private void Update()
    {
        if (!following)
            return;

        if (animator != null)
            animator.SetBool("IsMoving", following);

        ResolvePlayer();

        if (player == null)
        {
            following = false;
            return;
        }

        UpdateDestination();
        MoveToDestination();
        UpdateRandomRotation();
    }

    public void StartFollowing()
    {
        ResolvePlayer();

        following = player != null;
        currentSpeed = 0f;
        blockedTimer = 0f;
        hasDestination = false;

        if (following)
        {
            ChooseNewDestination();
            ChooseNewRotation();
        }

        Debug.Log(
            following
                ? "[SimpleBitFollower] Gerakan acak Bit dimulai."
                : "[SimpleBitFollower] Gagal: Wuga tidak ditemukan.",
            this
        );
    }

    public void StopFollowing()
    {
        following = false;
        currentSpeed = 0f;
        hasDestination = false;
    }

    public void SetMovementDistance(float minimum, float maximum)
    {
        movementDistance = new Vector2(
            Mathf.Max(0.1f, minimum),
            Mathf.Max(minimum, maximum)
        );

        hasDestination = false;
    }

    private void UpdateDestination()
    {
        destinationTimer -= Time.deltaTime;

        Vector3 playerToBit = transform.position - player.position;
        playerToBit.y = 0f;

        bool tooFar =
            playerToBit.magnitude >
            movementDistance.y + returnTolerance;

        bool tooClose =
            playerToBit.magnitude <
            movementDistance.x - arrivalDistance;

        bool arrived =
            hasDestination &&
            Vector3.Distance(transform.position, destination) <=
            arrivalDistance;

        if (!hasDestination ||
            destinationTimer <= 0f ||
            arrived ||
            tooFar ||
            tooClose)
        {
            ChooseNewDestination(tooFar, tooClose);
        }
    }

    private void ChooseNewDestination(
        bool forceReturn = false,
        bool forceAway = false)
    {
        float minimumDistance = Mathf.Max(0.1f, movementDistance.x);
        float maximumDistance =
            Mathf.Max(minimumDistance, movementDistance.y);
        minimumDistance = Mathf.Max(
            minimumDistance,
            GetPlayerSafeRadius()
        );
        maximumDistance = Mathf.Max(minimumDistance, maximumDistance);

        Vector3 awayFromPlayer =
            transform.position - player.position;
        awayFromPlayer.y = 0f;

        if (awayFromPlayer.sqrMagnitude < 0.01f)
            awayFromPlayer = -player.forward;

        for (int i = 0; i < destinationSearchAttempts; i++)
        {
            Vector2 randomDirection;

            if (forceAway && i == 0)
            {
                randomDirection = new Vector2(
                    awayFromPlayer.x,
                    awayFromPlayer.z
                );
            }
            else
            {
                randomDirection = Random.insideUnitCircle;
            }

            if (randomDirection.sqrMagnitude < 0.01f)
                randomDirection = Vector2.right;

            randomDirection.Normalize();

            float distance = forceReturn
                ? minimumDistance
                : forceAway
                    ? Mathf.Min(
                        maximumDistance,
                        minimumDistance + 0.5f
                    )
                    : Random.Range(
                        minimumDistance,
                        maximumDistance
                    );

            Vector3 candidate =
                player.position +
                new Vector3(
                    randomDirection.x * distance,
                    Random.Range(heightOffset.x, heightOffset.y),
                    randomDirection.y * distance
                );

            if (!IsOutsidePlayer(candidate))
                continue;

            if (!IsPositionFree(candidate))
                continue;

            if (!IsPathClear(transform.position, candidate))
                continue;

            destination = candidate;
            hasDestination = true;
            destinationTimer = Random.Range(
                destinationChangeInterval.x,
                destinationChangeInterval.y
            );
            return;
        }

        // Jika semua titik acak terhalang, Bit tetap mendekati Wuga
        // tanpa mencoba menembus collider.
        Vector3 fallbackDirection =
            transform.position - player.position;
        fallbackDirection.y = 0f;

        if (fallbackDirection.sqrMagnitude < 0.01f)
            fallbackDirection = -player.forward;

        destination =
            player.position +
            fallbackDirection.normalized * minimumDistance +
            Vector3.up * Mathf.Lerp(
                heightOffset.x,
                heightOffset.y,
                0.5f
            );

        hasDestination = true;
        destinationTimer = 0.5f;
    }

    private void MoveToDestination()
    {
        if (!hasDestination)
            return;

        Vector3 hoverOffset =
            Vector3.up *
            (Mathf.Sin(Time.time * hoverSpeed) * hoverAmount);

        Vector3 desiredPosition = destination + hoverOffset;
        Vector3 toDestination = desiredPosition - transform.position;

        if (toDestination.sqrMagnitude <=
            arrivalDistance * arrivalDistance)
        {
            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                0f,
                acceleration * Time.deltaTime
            );
            return;
        }

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            moveSpeed,
            acceleration * Time.deltaTime
        );

        float moveDistance = Mathf.Min(
            currentSpeed * Time.deltaTime,
            toDestination.magnitude
        );

        Vector3 movement =
            toDestination.normalized * moveDistance;

        movement = PreventPlayerCollision(movement);

        bool movedFreely = MoveWithCollision(movement);

        if (movedFreely)
        {
            blockedTimer = 0f;
        }
        else
        {
            blockedTimer += Time.deltaTime;

            if (blockedTimer >= blockedRetargetDelay)
            {
                blockedTimer = 0f;
                ChooseNewDestination();
            }
        }
    }

    private bool MoveWithCollision(Vector3 movement)
    {
        float distance = movement.magnitude;

        if (distance <= Mathf.Epsilon)
            return true;

        Vector3 direction = movement / distance;

        if (!Physics.SphereCast(
            GetCollisionCenter(transform.position),
            collisionRadius,
            direction,
            out RaycastHit hit,
            distance + collisionSkin,
            obstacleLayers,
            QueryTriggerInteraction.Ignore))
        {
            transform.position += movement;
            return true;
        }

        float safeDistance = Mathf.Max(
            0f,
            hit.distance - collisionSkin
        );

        if (safeDistance > 0f)
            transform.position += direction * safeDistance;

        Vector3 remainingMovement =
            movement - direction * safeDistance;

        Vector3 slideMovement = Vector3.ProjectOnPlane(
            remainingMovement,
            hit.normal
        );

        float slideDistance = slideMovement.magnitude;

        if (slideDistance <= Mathf.Epsilon)
            return false;

        Vector3 slideDirection = slideMovement / slideDistance;

        if (Physics.SphereCast(
            GetCollisionCenter(transform.position),
            collisionRadius,
            slideDirection,
            out RaycastHit slideHit,
            slideDistance + collisionSkin,
            obstacleLayers,
            QueryTriggerInteraction.Ignore))
        {
            slideDistance = Mathf.Max(
                0f,
                slideHit.distance - collisionSkin
            );
        }

        if (slideDistance > 0f)
            transform.position += slideDirection * slideDistance;

        return false;
    }

    private Vector3 PreventPlayerCollision(Vector3 movement)
    {
        if (player == null)
            return movement;

        Vector3 playerCenter = GetPlayerCenter();
        Vector3 bitCenter = GetCollisionCenter(transform.position);
        Vector3 fromPlayer = bitCenter - playerCenter;
        fromPlayer.y = 0f;

        float safeRadius = GetPlayerSafeRadius();
        float currentDistance = fromPlayer.magnitude;

        Vector3 horizontalMovement =
            new(movement.x, 0f, movement.z);

        if (horizontalMovement.sqrMagnitude <= Mathf.Epsilon)
            return movement;

        Vector3 outward = currentDistance > 0.001f
            ? fromPlayer / currentDistance
            : -player.forward;

        outward.y = 0f;
        outward.Normalize();

        if (currentDistance < safeRadius)
        {
            // Jika Bit sudah terlalu dekat, satu-satunya gerakan
            // horizontal yang diizinkan adalah menjauh dari Wuga.
            float horizontalDistance = horizontalMovement.magnitude;
            return
                outward * horizontalDistance +
                Vector3.up * movement.y;
        }

        Vector3 proposedFromPlayer =
            fromPlayer + horizontalMovement;
        proposedFromPlayer.y = 0f;

        if (proposedFromPlayer.magnitude >= safeRadius)
            return movement;

        // Hapus bagian gerak yang menuju ke dalam zona aman.
        float inwardAmount =
            Vector3.Dot(horizontalMovement, -outward);

        if (inwardAmount > 0f)
            horizontalMovement += outward * inwardAmount;

        return horizontalMovement + Vector3.up * movement.y;
    }

    private void UpdateRandomRotation()
    {
        rotationTimer -= Time.deltaTime;

        if (rotationTimer <= 0f)
            ChooseNewRotation();

        Quaternion targetRotation =
            Quaternion.Euler(0f, targetYaw, 0f);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void ChooseNewRotation()
    {
        targetYaw = Random.Range(0f, 360f);
        rotationTimer = Random.Range(
            rotationChangeInterval.x,
            rotationChangeInterval.y
        );
    }

    private bool IsPositionFree(Vector3 position)
    {
        return !Physics.CheckSphere(
            GetCollisionCenter(position),
            collisionRadius + collisionSkin,
            obstacleLayers,
            QueryTriggerInteraction.Ignore
        );
    }

    private bool IsOutsidePlayer(Vector3 position)
    {
        Vector3 offset =
            GetCollisionCenter(position) - GetPlayerCenter();
        offset.y = 0f;

        return offset.sqrMagnitude >=
               GetPlayerSafeRadius() * GetPlayerSafeRadius();
    }

    private bool IsPathClear(Vector3 start, Vector3 end)
    {
        Vector3 path = end - start;
        float distance = path.magnitude;

        if (distance <= Mathf.Epsilon)
            return true;

        return !Physics.SphereCast(
            GetCollisionCenter(start),
            collisionRadius,
            path / distance,
            out _,
            distance,
            obstacleLayers,
            QueryTriggerInteraction.Ignore
        );
    }

    private Vector3 GetCollisionCenter(Vector3 rootPosition)
    {
        return rootPosition + collisionCenterOffset;
    }

    private Vector3 GetPlayerCenter()
    {
        if (playerCollider != null)
            return playerCollider.bounds.center;

        return player != null ? player.position : Vector3.zero;
    }

    private float GetPlayerSafeRadius()
    {
        float characterRadius = 0f;

        if (playerCollider != null)
        {
            Vector3 scale = playerCollider.transform.lossyScale;
            characterRadius =
                playerCollider.radius *
                Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        }

        return characterRadius + collisionRadius + playerClearance;
    }

    private void ResolvePlayer()
    {
        if (player != null)
        {
            if (playerCollider == null)
                playerCollider =
                    player.GetComponent<CharacterController>();

            return;
        }

        PlayerController controller =
            FindFirstObjectByType<PlayerController>();

        if (controller != null)
        {
            CharacterController character =
                controller.GetComponentInChildren<CharacterController>();

            playerCollider = character;
            player = character != null
                ? character.transform
                : controller.transform;
        }
    }

    private void OnValidate()
    {
        movementDistance.x = Mathf.Max(0.1f, movementDistance.x);
        movementDistance.y = Mathf.Max(
            movementDistance.x,
            movementDistance.y
        );

        heightOffset.x = Mathf.Max(collisionRadius, heightOffset.x);
        heightOffset.y = Mathf.Max(heightOffset.x, heightOffset.y);
        playerClearance = Mathf.Max(0f, playerClearance);

        destinationChangeInterval.x = Mathf.Max(
            0.1f,
            destinationChangeInterval.x
        );
        destinationChangeInterval.y = Mathf.Max(
            destinationChangeInterval.x,
            destinationChangeInterval.y
        );

        rotationChangeInterval.x = Mathf.Max(
            0.1f,
            rotationChangeInterval.x
        );
        rotationChangeInterval.y = Mathf.Max(
            rotationChangeInterval.x,
            rotationChangeInterval.y
        );
    }

    private void OnDrawGizmosSelected()
    {
        if (player == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(
            GetPlayerCenter(),
            Mathf.Max(movementDistance.x, GetPlayerSafeRadius())
        );

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(player.position, movementDistance.y);

        if (hasDestination)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(
                GetCollisionCenter(destination),
                collisionRadius
            );
        }
    }
}
