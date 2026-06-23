using UnityEngine;

public class CarrySystem : MonoBehaviour
{
    [SerializeField] private Transform carryPosition;
    [SerializeField] private Animator animator;

    private GameObject currentItem;
    private Transform originalParent;
    private Vector3 originalScale;
    private Collider[] itemColliders;
    private Rigidbody[] itemRigidbodies;

    // Container bantu agar item tidak terkena scale aneh dari tulang karakter (rig)
    // yang dapat menyebabkan bug bounds rendering dan membuat kamera menjauh.
    private Transform carryContainer;

    private Transform cameraTarget;
    private Vector3 originalCameraTargetLocalPos;

    public bool IsCarrying() => currentItem != null;
    public GameObject GetCurrentItem() => currentItem;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (carryPosition == null)
            carryPosition = FindChildByName(transform, "CarryPosition");

        if (carryPosition == null &&
            animator != null &&
            animator.isHuman)
        {
            carryPosition =
                animator.GetBoneTransform(HumanBodyBones.RightHand);
        }

        if (carryPosition == null)
        {
            Debug.LogError(
                "[CarrySystem] CarryPosition tidak ditemukan pada hierarchy player.",
                this
            );
        }

        // Buat container terpisah yang terhindar dari skala ekstrem rig
        carryContainer = new GameObject("CarryContainer").transform;
        carryContainer.SetParent(transform);
        carryContainer.localPosition = Vector3.zero;
        carryContainer.localRotation = Quaternion.identity;
        carryContainer.localScale = Vector3.one;

        cameraTarget = FindChildByName(transform, "CameraTarget");
        if (cameraTarget != null)
        {
            originalCameraTargetLocalPos = cameraTarget.localPosition;
        }
    }

    private void LateUpdate()
    {
        if (carryContainer != null && carryPosition != null)
        {
            carryContainer.position = carryPosition.position;
            carryContainer.rotation = carryPosition.rotation;
        }

        // Cegah animasi "Carry" dari Mixamo/Blender menggeser CameraTarget secara ekstrem
        if (cameraTarget != null)
        {
            cameraTarget.localPosition = originalCameraTargetLocalPos;
        }
    }

    public bool CarryItem(GameObject item)
    {
        if (item == null || carryPosition == null || IsCarrying())
            return false;

        currentItem = item;
        originalParent = item.transform.parent;
        originalScale = item.transform.localScale;

        SetItemPhysics(false);

        // Jangan menempelkan item langsung ke carryPosition jika rig memiliki skala aneh
        // (bisa membuat collider/bounds item membesar dan merusak Cinemachine).
        // Sebagai gantinya, tempelkan ke carryContainer yang scalenya normal (1,1,1).
        item.transform.SetParent(carryContainer, false);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
        item.transform.localScale = originalScale;

        if (animator != null)
            animator.SetBool("IsCarrying", true);

        PlayerMovement playerMovement = GetComponentInChildren<PlayerMovement>();
        if (playerMovement != null)
            playerMovement.SetCarrying(true);

        Debug.Log(
            $"[CarrySystem] {item.name} menempel ke {carryPosition.name}.",
            item
        );
        return true;
    }

    public void DropItem()
    {   
        PlayerMovement playerMovement = GetComponentInChildren<PlayerMovement>();
        if (playerMovement != null)
            playerMovement.SetCarrying(false);

        if (!IsCarrying())
            return;

        currentItem.transform.SetParent(originalParent, true);
        currentItem.transform.localScale = originalScale;
        SetItemPhysics(true);

        if (animator != null)
            animator.SetBool("IsCarrying", false);

        currentItem = null;
        originalParent = null;
        itemColliders = null;
        itemRigidbodies = null;
    }

    private void SetItemPhysics(bool enabled)
    {
        if (!enabled)
        {
            itemColliders =
                currentItem.GetComponentsInChildren<Collider>(true);
            itemRigidbodies =
                currentItem.GetComponentsInChildren<Rigidbody>(true);
        }

        if (itemColliders != null)
        {
            foreach (Collider itemCollider in itemColliders)
            {
                if (itemCollider != null)
                    itemCollider.enabled = enabled;
            }
        }

        if (itemRigidbodies != null)
        {
            foreach (Rigidbody body in itemRigidbodies)
            {
                if (body == null)
                    continue;

                body.useGravity = enabled;
                body.isKinematic = !enabled;
                if (!enabled) 
                    body.interpolation = RigidbodyInterpolation.None;
                else
                    body.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }
    }

    private static Transform FindChildByName(
        Transform root,
        string childName)
    {
        foreach (Transform child in root)
        {
            if (child.name == childName)
                return child;

            Transform result = FindChildByName(child, childName);

            if (result != null)
                return result;
        }

        return null;
    }
}
