using UnityEngine;

[DefaultExecutionOrder(100)]
public sealed class FixedAngleCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private bool captureCurrentOffsetOnAwake = true;
    [SerializeField] private Vector3 worldOffset;
    [SerializeField, Min(0f)] private float positionSmoothTime = 0.08f;

    private Quaternion fixedRotation;
    private Vector3 followVelocity;

    private void Awake()
    {
        fixedRotation = transform.rotation;

        if (target != null && captureCurrentOffsetOnAwake)
        {
            worldOffset = transform.position - target.position;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 targetPosition = target.position + worldOffset;

        transform.position = positionSmoothTime <= 0f
            ? targetPosition
            : Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref followVelocity,
                positionSmoothTime);

        transform.rotation = fixedRotation;
    }

    private void OnValidate()
    {
        positionSmoothTime = Mathf.Max(0f, positionSmoothTime);
    }
}
