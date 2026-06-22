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
    }

    public bool CarryItem(GameObject item)
    {
        if (item == null || carryPosition == null || IsCarrying())
            return false;

        currentItem = item;
        originalParent = item.transform.parent;
        originalScale = item.transform.localScale;

        SetItemPhysics(false);

        // Pertahankan ukuran dunia item meskipun CarryPosition berada
        // di bawah model karakter yang memiliki skala kecil.
        item.transform.SetParent(carryPosition, true);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        if (animator != null)
            animator.SetBool("IsCarrying", true);

        Debug.Log(
            $"[CarrySystem] {item.name} menempel ke {carryPosition.name}.",
            item
        );
        return true;
    }

    public void DropItem()
    {
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
