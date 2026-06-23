using UnityEngine;

public class SimpleNPCFollow : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float followDistance = 3f;
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float hoverHeight = 1.5f;
    [SerializeField] private float hoverSpeed = 2f;

    private bool isFollowing = false;
    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;

        // Auto-cari player kalau tidak di-assign
        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
    }

    void Update()
    {
        if (isFollowing && player != null)
        {
            FollowPlayer();
        }
        else
        {
            Hover();
        }
    }

    void FollowPlayer()
    {
        // Hitung posisi target (di belakang player)
        Vector3 targetPosition = player.position - player.forward * followDistance;
        targetPosition.y = player.position.y + hoverHeight;

        // Smooth follow
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSpeed * Time.deltaTime
        );

        // Face player
        Vector3 direction = player.position - transform.position;
        direction.y = 0;
        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                followSpeed * Time.deltaTime
            );
        }
    }

    void Hover()
    {
        float newY = startPosition.y + Mathf.Sin(Time.time * hoverSpeed) * 0.1f;
        transform.position = new Vector3(
            startPosition.x,
            newY,
            startPosition.z
        );
    }

    public void StartFollowing()
    {
        isFollowing = true;
        Debug.Log("NPC mulai follow player");
    }

    public void StopFollowing()
    {
        isFollowing = false;
        Debug.Log("NPC berhenti follow");
    }

    public bool IsFollowing() => isFollowing;
}
