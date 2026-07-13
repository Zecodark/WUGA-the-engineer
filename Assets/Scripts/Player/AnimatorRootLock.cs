using UnityEngine;

/// <summary>
/// Menjaga root visual tetap menempel pada root fisik karakter.
/// Clip animasi boleh menggerakkan tulang, tetapi tidak boleh memindahkan
/// GameObject yang menjadi parent visual.
/// </summary>
[DefaultExecutionOrder(10000)]
public class AnimatorRootLock : MonoBehaviour
{
    private Vector3 lockedLocalPosition;
    private Quaternion lockedLocalRotation;
    private Transform physicalRoot;
    private CharacterController controller;
    private Vector3 controllerPosition;
    private Quaternion controllerRotation;

    private void Awake()
    {
        lockedLocalPosition = transform.localPosition;
        lockedLocalRotation = transform.localRotation;
        physicalRoot = transform.parent;
        controller = physicalRoot != null
            ? physicalRoot.GetComponent<CharacterController>()
            : null;

        CaptureControllerPose();
    }

    private void Update()
    {
        // Dieksekusi sesudah script gerak, tetapi sebelum evaluasi Animator.
        // Dengan begitu gerakan WASD tetap dipertahankan sementara offset
        // root bawaan clip tidak boleh masuk ke CharacterController.
        CaptureControllerPose();
    }

    private void OnAnimatorMove()
    {
        RestoreLockedPose();
    }

    private void LateUpdate()
    {
        RestoreLockedPose();
    }

    private void CaptureControllerPose()
    {
        if (physicalRoot == null)
            return;

        controllerPosition = physicalRoot.position;
        controllerRotation = physicalRoot.rotation;
    }

    private void RestoreLockedPose()
    {
        if (physicalRoot != null &&
            ((physicalRoot.position - controllerPosition).sqrMagnitude > 0.000001f ||
             Quaternion.Angle(physicalRoot.rotation, controllerRotation) > 0.01f))
        {
            bool wasEnabled = controller != null && controller.enabled;
            if (wasEnabled)
                controller.enabled = false;

            physicalRoot.SetPositionAndRotation(
                controllerPosition,
                controllerRotation
            );

            if (wasEnabled)
                controller.enabled = true;
        }

        transform.SetLocalPositionAndRotation(
            lockedLocalPosition,
            lockedLocalRotation
        );
    }
}
