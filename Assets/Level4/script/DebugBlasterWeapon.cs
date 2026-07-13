using UnityEngine;
using UnityEngine.InputSystem;

public class DebugBlasterWeapon : MonoBehaviour
{
    [Header("Projectile Control")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform muzzle;
    [SerializeField, Min(0)] private int projectileDamage = 10;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 28f;
    [SerializeField, Min(0.01f)] private float projectileScale = 0.14f;
    [SerializeField, Min(0.05f)] private float fireCooldown = 0.18f;
    [SerializeField, Min(1f)] private float aimDistance = 80f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Owner")]
    [SerializeField] private GameObject owner;

    private float nextFireTime;

    public int ProjectileDamage
    {
        get => projectileDamage;
        set => projectileDamage = Mathf.Max(0, value);
    }

    private void Awake()
    {
        if (muzzle == null)
            muzzle = transform.Find("Muzzle");

        if (owner == null)
            owner = GetComponentInParent<PlayerController>()?.gameObject;
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Fire(Camera.main != null ? Camera.main.transform : null);
        }
    }

    public void Fire(Transform cameraTransform)
    {
        if (Time.time < nextFireTime)
            return;

        nextFireTime = Time.time + fireCooldown;

        Transform spawnPoint = muzzle != null ? muzzle : transform;
        Vector3 direction = GetAimDirection(cameraTransform, spawnPoint.position);
        GameObject projectile = CreateProjectile(spawnPoint.position, direction);

        DebugBlasterProjectile blasterProjectile = projectile.GetComponent<DebugBlasterProjectile>();
        if (blasterProjectile == null)
            blasterProjectile = projectile.AddComponent<DebugBlasterProjectile>();

        blasterProjectile.Launch(direction, projectileSpeed, projectileDamage, owner);
    }

    private Vector3 GetAimDirection(Transform cameraTransform, Vector3 spawnPosition)
    {
        if (cameraTransform == null)
            return transform.forward;

        Ray aimRay = new Ray(cameraTransform.position, cameraTransform.forward);
        Vector3 aimPoint = aimRay.GetPoint(aimDistance);

        if (Physics.Raycast(aimRay, out RaycastHit hit, aimDistance, hitMask, QueryTriggerInteraction.Ignore))
            aimPoint = hit.point;

        Vector3 direction = aimPoint - spawnPosition;
        if (direction.sqrMagnitude <= 0.001f)
            return cameraTransform.forward;

        return direction.normalized;
    }

    private GameObject CreateProjectile(Vector3 spawnPosition, Vector3 direction)
    {
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

        if (projectilePrefab != null)
        {
            GameObject prefabProjectile = Instantiate(projectilePrefab, spawnPosition, rotation);
            prefabProjectile.name = "DebugBlasterBullet";
            return prefabProjectile;
        }

        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.name = "DebugBlasterBullet";
        projectile.transform.SetPositionAndRotation(spawnPosition, rotation);
        projectile.transform.localScale = Vector3.one * projectileScale;

        return projectile;
    }

    private void OnValidate()
    {
        projectileDamage = Mathf.Max(0, projectileDamage);
        projectileSpeed = Mathf.Max(0.1f, projectileSpeed);
        fireCooldown = Mathf.Max(0.05f, fireCooldown);
        aimDistance = Mathf.Max(1f, aimDistance);
    }
}
