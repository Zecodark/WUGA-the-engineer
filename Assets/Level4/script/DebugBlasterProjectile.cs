using UnityEngine;

public class DebugBlasterProjectile : MonoBehaviour
{
    [SerializeField, Min(0)] private int damage = 10;
    [SerializeField, Min(0.1f)] private float lifetime = 4f;
    [SerializeField] private ParticleSystem flightParticles;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private Light projectileLight;
    [SerializeField] private Color laserColor = new Color(0.15f, 0.95f, 1f, 1f);
    [SerializeField, Min(0f)] private float impactEffectLifetime = 0.45f;

    private GameObject owner;
    private bool hasHit;

    private void Awake()
    {
        Collider projectileCollider = GetComponent<Collider>();
        if (projectileCollider == null)
            projectileCollider = gameObject.AddComponent<SphereCollider>();

        projectileCollider.isTrigger = true;

        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();

        body.useGravity = false;
        body.isKinematic = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (flightParticles == null)
            flightParticles = GetComponentInChildren<ParticleSystem>();

        if (trailRenderer == null)
            trailRenderer = GetComponentInChildren<TrailRenderer>();
            
        if (trailRenderer == null)
        {
            trailRenderer = gameObject.AddComponent<TrailRenderer>();
            trailRenderer.time = 0.25f;
            trailRenderer.startWidth = 0.15f;
            trailRenderer.endWidth = 0f;
            trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
            trailRenderer.emitting = true;
            trailRenderer.minVertexDistance = 0.05f;
        }

        if (projectileLight == null)
            projectileLight = GetComponentInChildren<Light>();
            
        if (projectileLight == null)
        {
            projectileLight = gameObject.AddComponent<Light>();
            projectileLight.type = LightType.Point;
            projectileLight.range = 3f;
            projectileLight.intensity = 2f;
        }

        ApplyLaserColor();
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public void Launch(Vector3 direction, float speed, int projectileDamage, GameObject projectileOwner)
    {
        damage = Mathf.Max(0, projectileDamage);
        owner = projectileOwner;

        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
            body.linearVelocity = direction.normalized * speed;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHit(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryHit(collision.gameObject);
    }

    private void TryHit(GameObject other)
    {
        if (hasHit || other == null || IsOwnedByProjectileOwner(other))
            return;

        BossHealt boss = other.GetComponentInParent<BossHealt>();
        if (boss == null)
            boss = other.GetComponentInChildren<BossHealt>();

        if (boss != null)
        {
            hasHit = true;
            boss.TakeDamage(damage);
            SpawnImpactEffect(transform.position);
            Destroy(gameObject);
            return;
        }

        if (!other.CompareTag("Player"))
        {
            hasHit = true;
            SpawnImpactEffect(transform.position);
            Destroy(gameObject);
        }
    }

    private bool IsOwnedByProjectileOwner(GameObject other)
    {
        return owner != null &&
               (other == owner || other.transform.IsChildOf(owner.transform));
    }

    private void ApplyLaserColor()
    {
        if (trailRenderer != null)
        {
            trailRenderer.startColor = laserColor;
            trailRenderer.endColor = new Color(laserColor.r, laserColor.g, laserColor.b, 0f);
        }

        if (projectileLight != null)
        {
            projectileLight.color = laserColor;
        }

        if (flightParticles != null)
        {
            ParticleSystem.MainModule main = flightParticles.main;
            main.startColor = laserColor;
        }
    }

    private void SpawnImpactEffect(Vector3 position)
    {
        if (impactEffectLifetime <= 0f)
            return;

        GameObject impact = new GameObject("DebugBlasterLaserImpact");
        impact.transform.position = position;

        ParticleSystem particles = impact.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.playOnAwake = false;
        main.duration = impactEffectLifetime;
        main.loop = false;
        main.startLifetime = 0.18f;
        main.startSpeed = 3.5f;
        main.startSize = 0.08f;
        main.startColor = laserColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, 18)
        });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.08f;

        particles.Play(true);

        Destroy(impact, impactEffectLifetime);
    }
}
