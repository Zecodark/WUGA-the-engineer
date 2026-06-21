using System.Collections;
using UnityEngine;

public class ExplosionVFXPlayer : MonoBehaviour
{
    [Header("Semua Particle System Ledakan")]
    public ParticleSystem[] particles;

    [Header("Hancurkan object setelah selesai?")]
    public bool destroyAfterPlay = false;

    [Header("Waktu sebelum dihancurkan")]
    [Min(0f)] public float destroyDelay = 3f;

    private Coroutine destroyCoroutine;

    public void PlayExplosion()
    {
        if (particles == null)
            return;

        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i] == null)
                continue;

            particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles[i].Play(true);
        }

        if (destroyAfterPlay)
        {
            if (destroyCoroutine != null)
                StopCoroutine(destroyCoroutine);

            destroyCoroutine = StartCoroutine(DestroyAfterDelay());
        }
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
}
