using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class Level3PortalTeleporter : MonoBehaviour
{
    private enum PortalAction
    {
        ShowResultPanel,
        LoadScene
    }

    [Header("Result")]
    [SerializeField] private PortalAction portalAction = PortalAction.ShowResultPanel;
    [SerializeField] private Level3GameOver gameOver;
    [SerializeField] private PlayerController playerController;

    [Header("Scene Load Opsional")]
    [SerializeField] private string nextSceneName = "Level4";
    [SerializeField] private string playerTag = "Player";

    private bool isTriggered;

    private void Awake()
    {
        Collider portalCollider = GetComponent<Collider>();
        portalCollider.isTrigger = true;

        Rigidbody portalBody = GetComponent<Rigidbody>();
        portalBody.isKinematic = true;
        portalBody.useGravity = false;

        if (gameOver == null)
            gameOver = FindFirstObjectByType<Level3GameOver>(FindObjectsInactive.Include);

        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryTeleport(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryTeleport(other);
    }

    private void TryTeleport(Collider other)
    {
        if (isTriggered || other == null || !IsPlayer(other))
            return;

        isTriggered = true;

        if (portalAction == PortalAction.ShowResultPanel)
        {
            ShowResultPanel();
            return;
        }

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogWarning("[Level3Portal] Next Scene Name belum diisi.", this);
            isTriggered = false;
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogWarning(
                $"[Level3Portal] Scene '{nextSceneName}' belum tersedia di Build Settings.",
                this
            );
            isTriggered = false;
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private void ShowResultPanel()
    {
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);

        if (playerController != null)
            playerController.LockInput();

        if (gameOver == null)
            gameOver = FindFirstObjectByType<Level3GameOver>(FindObjectsInactive.Include);

        if (gameOver != null)
        {
            gameOver.CompleteLevel();
        }
        else
        {
            Debug.LogError("[Level3Portal] GameOver tidak ditemukan.", this);
        }
    }

    private bool IsPlayer(Collider other)
    {
        if (other.CompareTag(playerTag))
            return true;

        Transform root = other.transform.root;
        return root != null && root.CompareTag(playerTag);
    }
}
