using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class PortalFinishTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameOver gameOver;

    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";

    private bool portalUnlocked;
    private bool resultTriggered;
    private readonly Collider[] overlapResults = new Collider[16];

    private void Awake()
    {
        Collider portalCollider = GetComponent<Collider>();
        portalCollider.isTrigger = true;

        Rigidbody portalBody = GetComponent<Rigidbody>();
        portalBody.isKinematic = true;
        portalBody.useGravity = false;

        if (gameOver == null)
            gameOver = FindFirstObjectByType<GameOver>();
    }

    public void SetPortalUnlocked(bool unlocked)
    {
        portalUnlocked = unlocked;

        if (!unlocked)
            resultTriggered = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryFinish(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryFinish(other);
    }

    private void FixedUpdate()
    {
        if (!portalUnlocked || resultTriggered)
            return;

        BoxCollider box = GetComponent<BoxCollider>();

        if (box == null)
            return;

        Vector3 scale = transform.lossyScale;
        Vector3 halfExtents = Vector3.Scale(
            box.size * 0.5f,
            new Vector3(
                Mathf.Abs(scale.x),
                Mathf.Abs(scale.y),
                Mathf.Abs(scale.z)
            )
        );

        int count = Physics.OverlapBoxNonAlloc(
            transform.TransformPoint(box.center),
            halfExtents,
            overlapResults,
            transform.rotation,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < count; i++)
        {
            if (TryFinish(overlapResults[i]))
                return;
        }
    }

    private bool TryFinish(Collider other)
    {
        if (!portalUnlocked ||
            resultTriggered ||
            other == null ||
            !IsWuga(other))
        {
            return false;
        }

        resultTriggered = true;

        if (gameOver == null)
            gameOver = FindFirstObjectByType<GameOver>();

        if (gameOver != null)
            gameOver.CompleteLevel();
        else
            Debug.LogError(
                "[PortalFinishTrigger] GameOver tidak ditemukan.",
                this
            );

        return true;
    }

    private bool IsWuga(Collider other)
    {
        if (other.CompareTag(playerTag))
            return true;

        Transform root = other.transform.root;
        return root != null && root.CompareTag(playerTag);
    }
}
