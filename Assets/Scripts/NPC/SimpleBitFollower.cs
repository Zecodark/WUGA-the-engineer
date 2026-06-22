using UnityEngine;

public class SimpleBitFollower : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Follow Position")]
    [SerializeField, Min(0.5f)] private float distanceBehind = 1.8f;
    [SerializeField] private float sideOffset = -0.8f;
    [SerializeField] private float verticalOffset = 0.35f;

    [Header("Movement")]
    [SerializeField, Min(0.01f)] private float smoothTime = 0.3f;
    [SerializeField, Min(0f)] private float rotationSpeed = 8f;

    [Header("Hover")]
    [SerializeField, Min(0f)] private float hoverAmount = 0.08f;
    [SerializeField, Min(0f)] private float hoverSpeed = 2f;

    private bool following;
    private Vector3 movementVelocity;

    public bool IsFollowing => following;

    private void LateUpdate()
    {
        if (!following)
            return;

        ResolvePlayer();

        if (player == null)
            return;

        Vector3 targetPosition =
            player.position -
            player.forward * distanceBehind +
            player.right * sideOffset;

        targetPosition.y =
            player.position.y +
            verticalOffset +
            Mathf.Sin(Time.time * hoverSpeed) * hoverAmount;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref movementVelocity,
            smoothTime
        );

        Vector3 lookDirection = player.position - transform.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(lookDirection);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    public void StartFollowing()
    {
        ResolvePlayer();
        movementVelocity = Vector3.zero;
        following = player != null;

        Debug.Log(
            following
                ? "[SimpleBitFollower] FOLLOW STARTED"
                : "[SimpleBitFollower] FOLLOW FAILED: Player tidak ditemukan.",
            this
        );
    }

    public void StopFollowing()
    {
        following = false;
        movementVelocity = Vector3.zero;
    }

    private void ResolvePlayer()
    {
        if (player != null)
            return;

        PlayerController controller =
            FindFirstObjectByType<PlayerController>();

        if (controller != null)
            player = controller.transform;
    }
}
