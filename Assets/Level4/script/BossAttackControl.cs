using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class BossAttackControl : MonoBehaviour
{
    [Header("Target")]
    [SerializeField]
    private Transform target;

    [SerializeField]
    private WugaHealt targetHealth;

    [Header("Attack Control")]
    [SerializeField]
    private bool waitForOpeningDialog = true;

    [SerializeField, Min(0.1f)]
    private float attackInterval = 3f;

    [SerializeField, Min(0)]
    private int attackDamage = 10;

    [SerializeField, Min(0f)]
    private float attackRange = 35f;

    [SerializeField, Min(0f)]
    private float attackHitDelay = 0.45f;

    [SerializeField]
    private bool faceTarget = true;

    [SerializeField]
    private bool rotateTowardTarget = true;

    [SerializeField]
    private Transform rotationRoot;

    [SerializeField]
    private float rotationYawOffset = 90f;

    [SerializeField, Min(0f)]
    private float rotationSpeed = 360f;

    [Header("Projectile Attack")]
    [SerializeField]
    private bool useProjectileAttack = true;

    [Tooltip("Titik keluarnya projectile. Bisa menggunakan posisi databomb.")]
    [SerializeField]
    private Transform projectileSpawnPoint;

    [Tooltip("Masukkan root GameObject databomb, bukan child FX.")]
    [SerializeField]
    [FormerlySerializedAs("projectilePrefab")]
    private GameObject projectileTemplate;

    [Tooltip("Sembunyikan databomb asli karena hanya digunakan sebagai template.")]
    [SerializeField]
    private bool hideProjectileTemplateOnPlay = true;

    [SerializeField, Min(0.1f)]
    private float projectileSpeed = 20f;

    [SerializeField, Min(0f)]
    private float projectileUpwardBias = 0.35f;

    [SerializeField, Min(0f)]
    private float projectileSpawnForwardOffset = 1.1f;

    [SerializeField, Min(0f)]
    private float projectileSpawnUpOffset = 0.35f;

    [Header("Animation")]
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private string idleStateName = "Idle";

    [SerializeField]
    private string attackStateName = "Boss_Attack_Throw";

    [SerializeField]
    private string stunStateName = "Boss_Fall_Down";

    [SerializeField]
    private string attackTriggerName = "Attack";

    [SerializeField]
    private string stunTriggerName = "Stun";

    [SerializeField, Min(0f)]
    private float crossFadeDuration = 0.08f;

    [SerializeField]
    private bool holdStunPose = true;

    [SerializeField, Min(0f)]
    private float stunPoseSettleTime = 0.9f;

    [SerializeField, Range(0f, 1f)]
    private float stunHoldNormalizedTime = 0.98f;

    private Coroutine attackRoutine;
    private Coroutine stunRoutine;

    private bool isStunned;
    private bool attacksAllowed;

    private BossHealt bossHealth;

    private float animatorSpeedBeforeStun = 1f;

    public bool IsStunned => isStunned;
    public bool AttacksAllowed => attacksAllowed;

    public float AttackInterval
    {
        get => attackInterval;

        set => attackInterval =
            Mathf.Max(0.1f, value);
    }

    private void Awake()
    {
        bossHealth = GetComponent<BossHealt>();

        if (bossHealth == null)
        {
            bossHealth =
                GetComponentInChildren<BossHealt>();
        }

        ResolveReferences();
    }

    private void Start()
    {
        ResolveReferences();

        /*
         * Databomb asli hanya menjadi template.
         * Hasil clone-nya yang dilempar sebagai projectile.
         */
        if (hideProjectileTemplateOnPlay &&
            projectileTemplate != null &&
            projectileTemplate.scene.IsValid())
        {
            projectileTemplate.SetActive(false);
        }

        if (!waitForOpeningDialog)
            BeginAttacking();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (Application.isPlaying &&
            !waitForOpeningDialog)
        {
            BeginAttacking();
        }
    }

    private void OnDisable()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        if (stunRoutine != null)
        {
            StopCoroutine(stunRoutine);
            stunRoutine = null;
        }

        RestoreAnimatorSpeed();
    }

    private void Update()
    {
        if (!Application.isPlaying ||
            !attacksAllowed ||
            isStunned)
        {
            return;
        }

        RotateTowardTarget(false);
    }

    private void ResolveReferences()
    {
        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>();
        }

        if ((rotationRoot == null ||
             rotationRoot == transform) &&
            animator != null)
        {
            rotationRoot = animator.transform;
        }

        WugaHealt foundWugaHealth =
            targetHealth != null
                ? targetHealth
                : FindSceneWugaHealth();

        Transform bestTarget =
            FindBestTargetTransform(foundWugaHealth);

        if (bestTarget != null)
        {
            bool targetNeedsReplacement =
                target == null ||
                !target.gameObject.activeInHierarchy;

            if (foundWugaHealth != null &&
                target == foundWugaHealth.transform)
            {
                targetNeedsReplacement = true;
            }

            if (targetNeedsReplacement)
                target = bestTarget;
        }

        if (target == null)
        {
            GameObject wuga =
                GameObject.Find("WUGA");

            if (wuga == null)
            {
                wuga = GameObject.Find("Third person Character");
            }

            if (wuga != null)
                target = wuga.transform;
        }

        if (target == null)
        {
            GameObject player =
                GameObject.FindGameObjectWithTag("Player");

            if (player != null)
                target = player.transform;
        }

        if (targetHealth == null &&
            foundWugaHealth != null)
        {
            targetHealth = foundWugaHealth;
        }

        if (targetHealth == null &&
            target != null)
        {
            targetHealth =
                target.GetComponentInParent<WugaHealt>();

            if (targetHealth == null)
            {
                targetHealth =
                    target.GetComponentInChildren<WugaHealt>();
            }
        }

        /*
         * Jika Projectile Template belum diisi,
         * cari child dengan nama tepat "databomb".
         */
        if (projectileTemplate == null)
        {
            Transform databomb =
                FindChildRecursive(
                    transform,
                    "databomb"
                );

            if (databomb != null)
            {
                projectileTemplate =
                    databomb.gameObject;
            }
        }

        /*
         * Jika databomb adalah object dalam Scene,
         * posisinya dapat digunakan sebagai Spawn Point.
         */
        if (projectileSpawnPoint == null &&
            projectileTemplate != null &&
            projectileTemplate.scene.IsValid())
        {
            projectileSpawnPoint =
                projectileTemplate.transform;
        }
    }

    public void BeginAttacking()
    {
        attacksAllowed = true;

        ResolveReferences();
        RotateTowardTarget(true);

        if (!Application.isPlaying ||
            !isActiveAndEnabled ||
            attackRoutine != null)
        {
            return;
        }

        attackRoutine =
            StartCoroutine(AttackLoop());
    }

    public void StopAttacking()
    {
        attacksAllowed = false;

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
    }

    public void EnterStun(float duration)
    {
        if (!isActiveAndEnabled)
            return;

        if (stunRoutine != null)
            StopCoroutine(stunRoutine);

        stunRoutine =
            StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator AttackLoop()
    {
        yield return new WaitForSeconds(
            attackInterval
        );

        while (enabled && attacksAllowed)
        {
            if (CanAttack())
            {
                yield return StartCoroutine(
                    PerformAttack()
                );
            }

            yield return new WaitForSeconds(
                attackInterval
            );
        }

        attackRoutine = null;
    }

    private bool CanAttack()
    {
        if (!attacksAllowed ||
            isStunned ||
            target == null)
        {
            return false;
        }

        if (!useProjectileAttack &&
            targetHealth == null)
        {
            return false;
        }

        if (bossHealth != null &&
            bossHealth.IsDead)
        {
            return false;
        }

        float distanceToTarget =
            Vector3.Distance(
                transform.position,
                target.position
            );

        return distanceToTarget <= attackRange;
    }

    private IEnumerator PerformAttack()
    {
        RotateTowardTarget(true);
        PlayAttackAnimation();

        yield return new WaitForSeconds(
            attackHitDelay
        );

        /*
         * Diperiksa lagi karena Boss bisa terkena stun
         * ketika animasi serangan sedang berjalan.
         */
        if (!CanAttack())
            yield break;

        if (useProjectileAttack)
        {
            FireProjectile();
        }
        else if (targetHealth != null)
        {
            targetHealth.TakeDamage(attackDamage);
        }
    }

    private void FireProjectile()
    {
        ResolveReferences();

        /*
         * Tidak lagi membuat Sphere bawaan Unity.
         * Kalau template kosong, projectile dibatalkan.
         */
        if (projectileTemplate == null)
        {
            Debug.LogError(
                "Projectile Template kosong. " +
                "Masukkan root GameObject databomb ke kolom " +
                "Projectile Template pada BossAttackControl.",
                this
            );

            return;
        }

        /*
         * Script BossProjectiel harus dipasang permanen
         * pada root databomb.
         */
        BossProjectiel templateProjectile =
            projectileTemplate.GetComponent<BossProjectiel>();

        if (templateProjectile == null)
        {
            Debug.LogError(
                "BossProjectiel belum dipasang pada root databomb. " +
                "Pilih databomb lalu Add Component > BossProjectiel.",
                projectileTemplate
            );

            return;
        }

        Transform spawnPoint =
            projectileSpawnPoint;

        if (spawnPoint == null)
        {
            if (projectileTemplate.scene.IsValid())
            {
                spawnPoint =
                    projectileTemplate.transform;
            }
            else
            {
                spawnPoint = transform;
            }
        }

        Vector3 spawnPosition =
            spawnPoint.position;

        Vector3 aimPoint =
            GetTargetAimPoint();

        Vector3 direction =
            BuildProjectileDirection(
                spawnPosition,
                aimPoint
            );

        spawnPosition +=
            direction * projectileSpawnForwardOffset +
            Vector3.up * projectileSpawnUpOffset;

        direction =
            BuildProjectileDirection(
                spawnPosition,
                aimPoint
            );

        Quaternion spawnRotation =
            Quaternion.LookRotation(
                direction,
                Vector3.up
            );

        GameObject projectile =
            Instantiate(
                projectileTemplate,
                spawnPosition,
                spawnRotation
            );

        projectile.transform.SetParent(
            null,
            true
        );

        projectile.name =
            $"{projectileTemplate.name}_Projectile";

        BossProjectiel bossProjectile =
            projectile.GetComponent<BossProjectiel>();

        if (bossProjectile == null)
        {
            Debug.LogError(
                "Hasil clone databomb tidak memiliki BossProjectiel.",
                projectile
            );

            Destroy(projectile);
            return;
        }

        /*
         * Jika template disembunyikan, hasil clone awalnya ikut inactive.
         * Aktifkan clone sebelum memanggil Launch().
         */
        projectile.SetActive(true);

        ActivateProjectileVisuals(projectile);

        bossProjectile.SetTarget(target);

        bossProjectile.Launch(
            direction,
            projectileSpeed,
            attackDamage,
            gameObject
        );
    }

    private Vector3 BuildProjectileDirection(
        Vector3 spawnPosition,
        Vector3 aimPoint)
    {
        Vector3 direction =
            aimPoint - spawnPosition;

        if (direction.sqrMagnitude <= 0.001f)
            return transform.forward;

        return (
            direction +
            Vector3.up * projectileUpwardBias
        ).normalized;
    }

    private void ActivateProjectileVisuals(
        GameObject projectile)
    {
        if (projectile == null)
            return;

        foreach (Renderer renderer
                 in projectile.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is ParticleSystemRenderer)
                continue;

            if (!renderer.gameObject.activeSelf)
                renderer.gameObject.SetActive(true);

            renderer.enabled = true;
        }

        foreach (Light projectileLight
                 in projectile.GetComponentsInChildren<Light>(true))
        {
            if (!projectileLight.gameObject.activeSelf)
                projectileLight.gameObject.SetActive(true);

            projectileLight.enabled = true;
        }

        foreach (ParticleSystem particleSystem
                 in projectile.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (!particleSystem.gameObject.activeSelf)
                particleSystem.gameObject.SetActive(true);

            if (!particleSystem.gameObject.activeInHierarchy)
                continue;

            if (particleSystem.isPlaying)
            {
                particleSystem.Stop(
                    true,
                    ParticleSystemStopBehavior
                        .StopEmittingAndClear
                );
            }

            particleSystem.Clear(true);
            particleSystem.Play(true);
        }
    }

    private Vector3 GetTargetAimPoint()
    {
        if (target == null)
        {
            return transform.position +
                   transform.forward;
        }

        Collider targetCollider =
            target.GetComponentInChildren<Collider>();

        if (targetCollider != null)
            return targetCollider.bounds.center;

        return target.position + Vector3.up;
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

    private Transform FindChildRecursive(
        Transform root,
        string childName)
    {
        if (root == null)
            return null;

        foreach (Transform child in root)
        {
            if (child.name == childName)
                return child;

            Transform nested =
                FindChildRecursive(
                    child,
                    childName
                );

            if (nested != null)
                return nested;
        }

        return null;
    }

    private void RotateTowardTarget(bool instant)
    {
        if (!faceTarget ||
            !rotateTowardTarget ||
            target == null)
        {
            return;
        }

        Transform pivot =
            rotationRoot != null
                ? rotationRoot
                : transform;

        Vector3 direction =
            target.position - pivot.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion desiredRotation =
            Quaternion.LookRotation(
                direction.normalized,
                Vector3.up
            ) *
            Quaternion.Euler(
                0f,
                rotationYawOffset,
                0f
            );

        pivot.rotation =
            instant || rotationSpeed <= 0f
                ? desiredRotation
                : Quaternion.RotateTowards(
                    pivot.rotation,
                    desiredRotation,
                    rotationSpeed * Time.deltaTime
                );
    }

    private IEnumerator StunRoutine(float duration)
    {
        isStunned = true;

        animatorSpeedBeforeStun =
            animator != null &&
            animator.speed > 0f
                ? animator.speed
                : 1f;

        RestoreAnimatorSpeed();
        PlayStunAnimation();

        float settleTime =
            Mathf.Min(
                Mathf.Max(0f, duration),
                stunPoseSettleTime
            );

        if (settleTime > 0f)
        {
            yield return new WaitForSeconds(
                settleTime
            );
        }

        if (holdStunPose)
            HoldStunPose();

        float remainingTime =
            Mathf.Max(
                0f,
                duration - settleTime
            );

        if (remainingTime > 0f)
        {
            yield return new WaitForSeconds(
                remainingTime
            );
        }

        RestoreAnimatorSpeed();

        isStunned = false;

        PlayIdleAnimation();
        ResolveReferences();

        stunRoutine = null;
    }

    private void PlayAttackAnimation()
    {
        PlayTriggerOrState(
            attackTriggerName,
            attackStateName
        );
    }

    private void PlayStunAnimation()
    {
        PlayTriggerOrState(
            stunTriggerName,
            stunStateName
        );
    }

    private void PlayIdleAnimation()
    {
        PlayStateIfAvailable(
            idleStateName,
            0f
        );
    }

    private void HoldStunPose()
    {
        if (animator == null ||
            animator.runtimeAnimatorController == null ||
            string.IsNullOrWhiteSpace(stunStateName))
        {
            return;
        }

        if (PlayStateIfAvailable(
                stunStateName,
                Mathf.Clamp01(stunHoldNormalizedTime)))
        {
            animator.Update(0f);
            animator.speed = 0f;
        }
    }

    private void RestoreAnimatorSpeed()
    {
        if (animator == null)
            return;

        animator.speed =
            animatorSpeedBeforeStun > 0f
                ? animatorSpeedBeforeStun
                : 1f;
    }

    private void PlayTriggerOrState(
        string triggerName,
        string stateName)
    {
        if (animator == null ||
            animator.runtimeAnimatorController == null)
        {
            return;
        }

        if (HasAnimatorTrigger(triggerName))
        {
            animator.ResetTrigger(triggerName);
            animator.SetTrigger(triggerName);
            return;
        }

        if (string.IsNullOrWhiteSpace(stateName))
            return;

        animator.CrossFade(
            stateName,
            crossFadeDuration
        );
    }

    private bool PlayStateIfAvailable(
        string stateName,
        float normalizedTime)
    {
        if (animator == null ||
            animator.runtimeAnimatorController == null ||
            string.IsNullOrWhiteSpace(stateName))
        {
            return false;
        }

        int stateHash =
            Animator.StringToHash(stateName);

        if (!animator.HasState(0, stateHash))
            return false;

        animator.Play(
            stateHash,
            0,
            normalizedTime
        );

        return true;
    }

    private bool HasAnimatorTrigger(
        string triggerName)
    {
        if (animator == null ||
            string.IsNullOrWhiteSpace(triggerName))
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter
                 in animator.parameters)
        {
            if (parameter.name == triggerName &&
                parameter.type ==
                AnimatorControllerParameterType.Trigger)
            {
                return true;
            }
        }

        return false;
    }

    private void OnValidate()
    {
        attackInterval =
            Mathf.Max(0.1f, attackInterval);

        attackDamage =
            Mathf.Max(0, attackDamage);

        attackRange =
            Mathf.Max(0f, attackRange);

        attackHitDelay =
            Mathf.Max(0f, attackHitDelay);

        rotationSpeed =
            Mathf.Max(0f, rotationSpeed);

        projectileSpeed =
            Mathf.Max(0.1f, projectileSpeed);

        projectileUpwardBias =
            Mathf.Max(0f, projectileUpwardBias);

        projectileSpawnForwardOffset =
            Mathf.Max(0f, projectileSpawnForwardOffset);

        projectileSpawnUpOffset =
            Mathf.Max(0f, projectileSpawnUpOffset);

        crossFadeDuration =
            Mathf.Max(0f, crossFadeDuration);

        stunPoseSettleTime =
            Mathf.Max(0f, stunPoseSettleTime);
    }
}