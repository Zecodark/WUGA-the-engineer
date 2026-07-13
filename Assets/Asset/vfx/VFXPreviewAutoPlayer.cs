using System.Collections;
using UnityEngine;

public class VFXPreviewAutoPlayer : MonoBehaviour
{
    [Header("Particle systems yang diulang saat preview")]
    [SerializeField] private ParticleSystem[] particleSystems;

    [Header("Player ledakan yang di-trigger ulang")]
    [SerializeField] private ExplosionVFXPlayer[] explosionPlayers;

    [Header("Timing preview")]
    [SerializeField, Min(0.2f)] private float replayInterval = 2.4f;
    [SerializeField, Min(0f)] private float startDelay = 0.15f;

    private Coroutine previewRoutine;

    private void OnEnable()
    {
        previewRoutine = StartCoroutine(PreviewLoop());
    }

    private void OnDisable()
    {
        if (previewRoutine != null)
        {
            StopCoroutine(previewRoutine);
            previewRoutine = null;
        }
    }

    [ContextMenu("Play Preview Now")]
    public void PlayPreviewNow()
    {
        PlayParticleSystems();
        PlayExplosionPlayers();
    }

    private IEnumerator PreviewLoop()
    {
        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        while (enabled)
        {
            PlayPreviewNow();
            yield return new WaitForSeconds(replayInterval);
        }
    }

    private void PlayParticleSystems()
    {
        if (particleSystems == null)
        {
            return;
        }

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = particleSystems[i];
            if (particleSystem == null)
            {
                continue;
            }

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Play(true);
        }
    }

    private void PlayExplosionPlayers()
    {
        if (explosionPlayers == null)
        {
            return;
        }

        for (int i = 0; i < explosionPlayers.Length; i++)
        {
            ExplosionVFXPlayer player = explosionPlayers[i];
            if (player == null)
            {
                continue;
            }

            player.PlayExplosion();
        }
    }
}
