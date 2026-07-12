using System.Collections.Generic;
using UnityEngine;

public class BossProjectiel : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField, Min(0)]
    private int damage = 10;

    [Header("Area Effect Damage")]
    [Tooltip("Jarak area damage dari posisi DataBomb ketika meledak.")]
    [SerializeField, Min(0.1f)]
    private float areaEffectRadius = 3f;

    [Tooltip("Layer yang dapat diperiksa oleh area damage.")]
    [SerializeField]
    private LayerMask areaDamageLayer = ~0;

    [Header("Hit Effect")]
    [SerializeField]
    private bool spawnHitEffect = true;

    [SerializeField, Min(0.1f)]
    private float hitEffectDuration = 0.45f;

    [SerializeField, Min(1)]
    private int hitEffectBurstCount = 32;

    [SerializeField]
    private Color hitEffectColor = new Color(1f, 0.26f, 0.04f, 1f);

    [Header("Projectile")]
    [SerializeField, Min(0.1f)]
    private float lifetime = 8f;

    [SerializeField]
    private bool useGravity = false;

    [SerializeField]
    private bool followTarget = true;

    [SerializeField, Min(0.1f)]
    private float homingDuration = 1.25f;

    [SerializeField, Min(1f)]
    private float maxTurnDegreesPerSecond = 85f;

    [SerializeField, Min(0f)]
    private float aimOffsetRadius = 1.8f;

    [SerializeField, Min(0f)]
    private float spawnCollisionGraceTime = 0.6f;

    [Tooltip("Jika aktif, DataBomb meledak ketika menyentuh tembok/lantai.")]
    [SerializeField]
    private bool explodeOnEnvironmentHit = true;

    [Header("Projectile Hitbox")]
    [Tooltip("Ukuran minimum collider DataBomb saat terbang.")]
    [SerializeField, Min(0.05f)]
    private float minHitRadius = 0.35f;

    [Tooltip("Ukuran maksimum collider DataBomb saat terbang.")]
    [SerializeField, Min(0.05f)]
    private float maxHitRadius = 0.9f;

    [SerializeField]
    private float destroyBelowY = -10f;

    private GameObject owner;
    private Transform target;
    private Rigidbody rb;

    private bool hasHit;
    private bool hasLaunched;

    private float currentSpeed;
    private float launchTime;
    private float spawnedAt;
    private float lastTargetSearchTime = -999f;

    private Vector3 aimOffset;

    private void Awake()
    {
        /*
         * Jangan memasang Rigidbody atau Collider di Awake.
         * Object databomb yang ada pada Boss hanya menjadi template.
         *
         * Rigidbody dan Collider baru disiapkan ketika Launch()
         * dipanggil pada hasil clone projectile.
         */
    }

    private void OnEnable()
    {
        /*
         * Saat hasil clone projectile diaktifkan,
         * kondisi runtime direset.
         */
        hasHit = false;
        hasLaunched = false;

        owner = null;
        target = null;
        rb = null;

        currentSpeed = 0f;
        launchTime = 0f;
        spawnedAt = Time.time;
        lastTargetSearchTime = -999f;

        aimOffset = Vector3.zero;
    }

    private void Update()
    {
        /*
         * Databomb asli yang menjadi template
         * tidak menjalankan perilaku projectile.
         */
        if (!hasLaunched)
            return;

        if (transform.position.y <= destroyBelowY)
        {
            Destroy(gameObject);
            return;
        }

        if (followTarget &&
            target == null &&
            Time.time - lastTargetSearchTime >= 0.25f)
        {
            FindTarget();
        }
    }

    public void Launch(
        Vector3 direction,
        float speed,
        int projectileDamage,
        GameObject projectileOwner)
    {
        if (hasLaunched)
            return;

        hasLaunched = true;

        EnsureCollider();
        EnsureRigidbody();

        damage = Mathf.Max(0, projectileDamage);
        owner = projectileOwner;

        currentSpeed = Mathf.Max(0.1f, speed);
        launchTime = Time.time;
        spawnedAt = Time.time;
        aimOffset = CreateAimOffset();

        if (rb != null)
        {
            rb.useGravity = useGravity;

            rb.linearVelocity =
                direction.normalized * currentSpeed;
        }

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(
                direction.normalized,
                Vector3.up
            );
        }

        if (followTarget && target == null)
            FindTarget();

        /*
         * Hanya projectile hasil clone yang dihancurkan.
         * Databomb asli pada hierarchy tidak dihancurkan.
         */
        Destroy(gameObject, lifetime);
    }

    public void SetTarget(Transform newTarget)
    {
        if (newTarget == null)
        {
            target = null;
            return;
        }

        WugaHealt health =
            newTarget.GetComponentInParent<WugaHealt>();

        if (health == null)
        {
            health =
                newTarget.GetComponentInChildren<WugaHealt>();
        }

        target =
            FindBestTargetTransform(health) ?? newTarget;
    }

    private void EnsureCollider()
    {
        /*
         * Menonaktifkan collider child.
         * Projectile menggunakan satu SphereCollider pada root databomb.
         */
        foreach (Collider childCollider
                 in GetComponentsInChildren<Collider>(true))
        {
            if (childCollider != null &&
                childCollider.gameObject != gameObject)
            {
                childCollider.enabled = false;
            }
        }

        /*
         * Menonaktifkan collider root selain SphereCollider.
         */
        foreach (Collider rootCollider
                 in GetComponents<Collider>())
        {
            if (rootCollider != null &&
                !(rootCollider is SphereCollider))
            {
                rootCollider.enabled = false;
            }
        }

        SphereCollider projectileCollider =
            GetComponent<SphereCollider>();

        if (projectileCollider == null)
        {
            projectileCollider =
                gameObject.AddComponent<SphereCollider>();
        }

        FitSphereColliderToRenderers(projectileCollider);

        projectileCollider.enabled = true;
        projectileCollider.isTrigger = true;
    }

    private void FitSphereColliderToRenderers(
        SphereCollider sphereCollider)
    {
        Renderer[] renderers =
            GetComponentsInChildren<Renderer>(true);

        bool hasBounds = false;

        Bounds bounds = new Bounds(
            transform.position,
            Vector3.zero
        );

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null ||
                renderer is ParticleSystemRenderer ||
                renderer is TrailRenderer)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        /*
         * Jika DataBomb hanya berupa particle dan tidak memiliki mesh,
         * gunakan nilai Min Hit Radius.
         */
        if (!hasBounds)
        {
            sphereCollider.center = Vector3.zero;
            sphereCollider.radius = minHitRadius / GetLargestWorldScale();
            return;
        }

        sphereCollider.center =
            transform.InverseTransformPoint(bounds.center);

        float worldRadius = Mathf.Clamp(
            bounds.extents.magnitude,
            minHitRadius,
            maxHitRadius
        );

        sphereCollider.radius =
            worldRadius / GetLargestWorldScale();
    }

    private float GetLargestWorldScale()
    {
        float largestScale = Mathf.Max(
            Mathf.Abs(transform.lossyScale.x),
            Mathf.Abs(transform.lossyScale.y),
            Mathf.Abs(transform.lossyScale.z)
        );

        return largestScale <= 0.001f ? 1f : largestScale;
    }

    private void EnsureRigidbody()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        rb.useGravity = useGravity;
        rb.isKinematic = false;

        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;
    }

    private void FindTarget()
    {
        if (target != null)
            return;

        lastTargetSearchTime = Time.time;

        WugaHealt wugaHealth =
            FindSceneWugaHealth();

        if (wugaHealth != null)
        {
            target =
                FindBestTargetTransform(wugaHealth);

            return;
        }

        GameObject wuga =
            GameObject.Find("WUGA");

        if (wuga == null)
        {
            wuga = GameObject.Find("Third person Character");
        }

        if (wuga == null)
        {
            wuga =
                GameObject.FindGameObjectWithTag("Player");
        }

        if (wuga != null)
            SetTarget(wuga.transform);
    }

    private WugaHealt FindSceneWugaHealth()
    {
        foreach (WugaHealt health
                 in Resources.FindObjectsOfTypeAll<WugaHealt>())
        {
            if (health == null ||
                !health.gameObject.scene.IsValid())
            {
                continue;
            }

            return health;
        }

        return null;
    }

    private Transform FindBestTargetTransform(
        WugaHealt health)
    {
        if (health == null)
            return null;

        CharacterController characterController =
            health.GetComponentInChildren<CharacterController>();

        if (characterController != null &&
            characterController.enabled &&
            characterController.gameObject.activeInHierarchy)
        {
            return characterController.transform;
        }

        foreach (Collider targetCollider
                 in health.GetComponentsInChildren<Collider>())
        {
            if (targetCollider != null &&
                targetCollider.enabled &&
                !targetCollider.isTrigger &&
                targetCollider.gameObject.activeInHierarchy)
            {
                return targetCollider.transform;
            }
        }

        Animator targetAnimator =
            health.GetComponentInChildren<Animator>();

        if (targetAnimator != null &&
            targetAnimator.gameObject.activeInHierarchy)
        {
            return targetAnimator.transform;
        }

        return health.transform;
    }

    private void FixedUpdate()
    {
        if (!hasLaunched)
            return;

        if (!followTarget ||
            target == null ||
            rb == null ||
            hasHit)
        {
            return;
        }

        if (Time.time - launchTime > homingDuration)
        {
            RotateAlongVelocity();
            return;
        }

        Vector3 targetPosition =
            target.position;

        Collider targetCollider =
            target.GetComponentInChildren<Collider>();

        if (targetCollider != null)
        {
            targetPosition =
                targetCollider.bounds.center;
        }
        else
        {
            targetPosition += Vector3.up;
        }

        targetPosition += aimOffset;

        Vector3 desiredDirection =
            (targetPosition - transform.position).normalized;

        float distanceToTarget =
            Vector3.Distance(
                transform.position,
                targetPosition
            );

        if (distanceToTarget <= 0.6f)
            return;

        Vector3 currentDirection =
            rb.linearVelocity.sqrMagnitude > 0.001f
                ? rb.linearVelocity.normalized
                : transform.forward;

        float maximumRadians =
            maxTurnDegreesPerSecond *
            Mathf.Deg2Rad *
            Time.fixedDeltaTime;

        Vector3 steeredDirection =
            Vector3.RotateTowards(
                currentDirection,
                desiredDirection,
                maximumRadians,
                0f
            );

        rb.linearVelocity =
            steeredDirection.normalized *
            Mathf.Max(currentSpeed, 0.1f);

        RotateAlongVelocity();
    }

    private Vector3 CreateAimOffset()
    {
        if (aimOffsetRadius <= 0f)
            return Vector3.zero;

        Vector2 randomOffset =
            Random.insideUnitCircle * aimOffsetRadius;

        return new Vector3(
            randomOffset.x,
            Mathf.Abs(randomOffset.y) * 0.25f,
            randomOffset.y
        );
    }

    private void RotateAlongVelocity()
    {
        if (rb == null ||
            rb.linearVelocity.sqrMagnitude <= 0.001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(
            rb.linearVelocity.normalized,
            Vector3.up
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasLaunched)
            return;

        TryHit(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!hasLaunched)
            return;

        TryHit(collision.gameObject);
    }

    private void TryHit(GameObject other)
    {
        if (hasHit ||
            other == null ||
            IsOwnedByProjectileOwner(other))
        {
            return;
        }

        WugaHealt directHealth =
            other.GetComponent<WugaHealt>();

        if (directHealth == null)
        {
            directHealth =
                other.GetComponentInParent<WugaHealt>();
        }

        if (directHealth == null)
        {
            directHealth =
                other.GetComponentInChildren<WugaHealt>();
        }

        bool isPlayer =
            other.CompareTag("Player") ||
            other.name.Contains("WUGA") ||
            other.name.Contains("MainCharacter") ||
            other.name.Contains("Third person Character") ||
            directHealth != null;

        Collider hitCollider =
            other.GetComponent<Collider>();

        /*
         * Abaikan trigger yang bukan karakter WUGA.
         */
        if (hitCollider != null &&
            hitCollider.isTrigger &&
            !isPlayer)
        {
            return;
        }

        if (!isPlayer)
        {
            if (!explodeOnEnvironmentHit)
                return;

            /*
             * Jika mode ledak saat kena environment diaktifkan,
             * tetap beri waktu agar projectile tidak langsung mengenai
             * Boss atau benda dekat titik spawn.
             */
            if (Time.time - spawnedAt < spawnCollisionGraceTime)
                return;
        }

        hasHit = true;

        ApplyAreaEffectDamage();

        Destroy(gameObject);
    }

    private void ApplyAreaEffectDamage()
    {
        Collider[] collidersInsideArea =
            Physics.OverlapSphere(
                transform.position,
                areaEffectRadius,
                areaDamageLayer,
                QueryTriggerInteraction.Collide
            );

        /*
         * Mencegah satu WUGA menerima damage lebih dari sekali
         * karena memiliki beberapa collider.
         */
        HashSet<WugaHealt> damagedTargets =
            new HashSet<WugaHealt>();

        foreach (Collider detectedCollider
                 in collidersInsideArea)
        {
            if (detectedCollider == null)
                continue;

            WugaHealt health =
                detectedCollider.GetComponent<WugaHealt>();

            if (health == null)
            {
                health =
                    detectedCollider
                        .GetComponentInParent<WugaHealt>();
            }

            if (health == null)
            {
                health =
                    detectedCollider
                        .GetComponentInChildren<WugaHealt>();
            }

            if (health == null)
                continue;

            if (damagedTargets.Contains(health))
                continue;

            damagedTargets.Add(health);

            health.TakeDamage(damage);
            SpawnHitEffect(health, detectedCollider);

            Debug.Log(
                $"WUGA terkena ledakan DataBomb. " +
                $"Damage: {damage}, " +
                $"Radius: {areaEffectRadius}"
            );
        }
    }

    private void SpawnHitEffect(
        WugaHealt health,
        Collider hitCollider)
    {
        if (!spawnHitEffect || health == null)
            return;

        Vector3 effectPosition =
            health.transform.position + Vector3.up;

        if (hitCollider != null)
            effectPosition = hitCollider.bounds.center;

        GameObject effect =
            new GameObject("DataBombHitEffect");

        effect.transform.position = effectPosition;

        ParticleSystem particles =
            effect.AddComponent<ParticleSystem>();

        particles.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        ParticleSystem.MainModule main =
            particles.main;

        main.playOnAwake = false;
        main.duration = hitEffectDuration;
        main.loop = false;
        main.startLifetime =
            new ParticleSystem.MinMaxCurve(0.18f, 0.34f);
        main.startSpeed =
            new ParticleSystem.MinMaxCurve(1.6f, 3.4f);
        main.startSize =
            new ParticleSystem.MinMaxCurve(0.06f, 0.17f);
        main.startColor = hitEffectColor;
        main.simulationSpace =
            ParticleSystemSimulationSpace.World;
        main.maxParticles =
            Mathf.Max(hitEffectBurstCount, 8);

        ParticleSystem.EmissionModule emission =
            particles.emission;

        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(
                0f,
                (short)Mathf.Clamp(
                    hitEffectBurstCount,
                    1,
                    120
                )
            )
        });

        ParticleSystem.ShapeModule shape =
            particles.shape;

        shape.shapeType =
            ParticleSystemShapeType.Sphere;
        shape.radius = 0.28f;

        Light hitLight =
            effect.AddComponent<Light>();

        hitLight.type = LightType.Point;
        hitLight.color = hitEffectColor;
        hitLight.range = 2.4f;
        hitLight.intensity = 2.4f;

        particles.Play(true);

        Destroy(
            effect,
            hitEffectDuration + 0.6f
        );
    }

    private bool IsOwnedByProjectileOwner(
        GameObject other)
    {
        if (owner == null || other == null)
            return false;

        return other == owner ||
               other.transform.IsChildOf(owner.transform) ||
               owner.transform.IsChildOf(other.transform);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            areaEffectRadius
        );
    }

    private void OnValidate()
    {
        damage =
            Mathf.Max(0, damage);

        lifetime =
            Mathf.Max(0.1f, lifetime);

        areaEffectRadius =
            Mathf.Max(0.1f, areaEffectRadius);

        hitEffectDuration =
            Mathf.Max(0.1f, hitEffectDuration);

        hitEffectBurstCount =
            Mathf.Max(1, hitEffectBurstCount);

        minHitRadius =
            Mathf.Max(0.05f, minHitRadius);

        maxHitRadius =
            Mathf.Max(minHitRadius, maxHitRadius);

        homingDuration =
            Mathf.Max(0.1f, homingDuration);

        maxTurnDegreesPerSecond =
            Mathf.Max(1f, maxTurnDegreesPerSecond);

        aimOffsetRadius =
            Mathf.Max(0f, aimOffsetRadius);

        spawnCollisionGraceTime =
            Mathf.Max(0f, spawnCollisionGraceTime);
    }
}
