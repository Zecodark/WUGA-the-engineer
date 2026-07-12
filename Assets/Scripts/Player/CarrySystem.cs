using UnityEngine;
using HPhysic;

public class CarrySystem : MonoBehaviour
{
    [SerializeField] private Transform carryPosition;
    [SerializeField] private Transform cableCarryPosition; // Posisi tangan untuk kabel
    [SerializeField] private Animator animator;

    private GameObject currentItem;
    private Transform originalParent;
    private Vector3 originalScale;
    private Collider[] itemColliders;
    private Rigidbody[] itemRigidbodies;

    // Cable-specific tracking
    private GameObject currentCable;
    private Transform cableOriginalParent;
    private Rigidbody cableRigidbody;
    private Transform cableCarryContainer;
    private PhysicCable cablePhysics;
    private Collider[] cableColliders;
    private Collider[] playerColliders;
    private bool cableOriginalUseGravity;
    private bool cableOriginalIsKinematic;
    private RigidbodyInterpolation cableOriginalInterpolation;

    // Container bantu agar item tidak terkena scale aneh dari tulang karakter (rig)
    // yang dapat menyebabkan bug bounds rendering dan membuat kamera menjauh.
    private Transform carryContainer;

    private Transform cameraTarget;
    private Vector3 originalCameraTargetLocalPos;

    public bool IsCarrying() => currentItem != null || currentCable != null;
    public bool IsCarryingItem() => currentItem != null;
    public bool IsCarryingCable() => currentCable != null;
    public GameObject GetCurrentItem() => currentItem;
    public GameObject GetCurrentCable() => currentCable;

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

        // Cable carry position: cari "CableCarryPosition" atau fallback ke RightHand
        if (cableCarryPosition == null)
            cableCarryPosition = FindChildByName(transform, "CableCarryPosition");

        if (cableCarryPosition == null &&
            animator != null &&
            animator.isHuman)
        {
            cableCarryPosition =
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

        // Container khusus kabel (ikut tangan)
        cableCarryContainer = new GameObject("CableCarryContainer").transform;
        cableCarryContainer.SetParent(transform);
        cableCarryContainer.localPosition = Vector3.zero;
        cableCarryContainer.localRotation = Quaternion.identity;
        cableCarryContainer.localScale = Vector3.one;

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

        // Cable container ikut posisi tangan
        if (cableCarryContainer != null && cableCarryPosition != null)
        {
            cableCarryContainer.position = cableCarryPosition.position;
            cableCarryContainer.rotation = cableCarryPosition.rotation;
        }

        if (currentCable != null && cableRigidbody == null)
            SnapCableToCarryPosition();

        // Cegah animasi "Carry" dari Mixamo/Blender menggeser CameraTarget secara ekstrem
        if (cameraTarget != null)
        {
            cameraTarget.localPosition = originalCameraTargetLocalPos;
        }
    }

    private void FixedUpdate()
    {
        if (currentCable == null || cableCarryPosition == null)
            return;

        if (cableRigidbody == null)
        {
            SnapCableToCarryPosition();
            return;
        }

        Vector3 targetPosition = GetCableCarryTargetPosition();

        cableRigidbody.MovePosition(targetPosition);
        cableRigidbody.MoveRotation(cableCarryPosition.rotation);
        cableRigidbody.linearVelocity = Vector3.zero;
        cableRigidbody.angularVelocity = Vector3.zero;
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

        if (!IsCarryingItem())
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

    // ========== CABLE CARRY ==========

    public bool CarryCable(GameObject cableEnd)
    {
        if (cableEnd == null || cableCarryPosition == null || IsCarrying())
            return false;

        currentCable = cableEnd;
        cableOriginalParent = cableEnd.transform.parent;
        cableRigidbody = cableEnd.GetComponent<Rigidbody>();
        cablePhysics = cableEnd.GetComponentInParent<PhysicCable>();
        SetCablePlayerCollisionIgnored(true);

        // Ujung kabel tetap di hierarchy kabel agar joint chain tidak melawan parenting tangan.
        // Saat dipegang, rigidbody kinematic digerakkan lewat FixedUpdate supaya sinkron dengan physics.
        if (cableRigidbody != null)
        {
            cableOriginalUseGravity = cableRigidbody.useGravity;
            cableOriginalIsKinematic = cableRigidbody.isKinematic;
            cableOriginalInterpolation = cableRigidbody.interpolation;
            cableRigidbody.useGravity = false;
            cableRigidbody.isKinematic = true;
            cableRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            cableRigidbody.linearVelocity = Vector3.zero;
            cableRigidbody.angularVelocity = Vector3.zero;
        }

        SnapCableToCarryPosition();

        Debug.Log($"[CarrySystem] Cable {cableEnd.name} dipegang di tangan.", cableEnd);
        return true;
    }

    public void DropCable()
    {
        if (!IsCarryingCable())
            return;

        // Kembalikan ke parent asli
        currentCable.transform.SetParent(cableOriginalParent, true);

        // Hidupkan rigidbody lagi
        if (cableRigidbody != null)
        {
            cableRigidbody.useGravity = cableOriginalUseGravity;
            cableRigidbody.isKinematic = cableOriginalIsKinematic;
            cableRigidbody.interpolation = cableOriginalInterpolation;
            cableRigidbody.linearVelocity = Vector3.zero;
            cableRigidbody.angularVelocity = Vector3.zero;
        }

        Debug.Log($"[CarrySystem] Cable {currentCable.name} dilepas.", currentCable);

        SetCablePlayerCollisionIgnored(false);

        currentCable = null;
        cableOriginalParent = null;
        cableRigidbody = null;
        cablePhysics = null;
        cableColliders = null;
        playerColliders = null;
    }

    private void SnapCableToCarryPosition()
    {
        if (currentCable == null || cableCarryPosition == null)
            return;

        currentCable.transform.position = GetCableCarryTargetPosition();
        currentCable.transform.rotation = cableCarryPosition.rotation;
    }

    private Vector3 GetCableCarryTargetPosition()
    {
        Vector3 targetPosition = cableCarryPosition.position;

        if (cablePhysics != null && currentCable != null)
            return cablePhysics.ClampHeldPosition(currentCable.transform, targetPosition);

        return targetPosition;
    }

    private void SetCablePlayerCollisionIgnored(bool ignored)
    {
        if (ignored)
        {
            GameObject cableRoot = cablePhysics != null ? cablePhysics.gameObject : currentCable;
            cableColliders = cableRoot.GetComponentsInChildren<Collider>(true);
            playerColliders = GetComponentsInChildren<Collider>(true);
        }

        if (cableColliders == null || playerColliders == null)
            return;

        foreach (Collider cableCollider in cableColliders)
        {
            if (cableCollider == null)
                continue;

            foreach (Collider playerCollider in playerColliders)
            {
                if (playerCollider == null || cableCollider == playerCollider)
                    continue;

                Physics.IgnoreCollision(cableCollider, playerCollider, ignored);
            }
        }
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
