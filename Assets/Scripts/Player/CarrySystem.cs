using System.Collections;
using UnityEngine;

public class CarrySystem : MonoBehaviour
{
    [Header("Chest Position")]
    [SerializeField] private Vector3 chestOffset =
        new Vector3(0f, 1.2f, 0.65f);
    [SerializeField, Min(0.1f)] private float maximumCarrySize = 0.8f;
    [SerializeField] private Animator animator;

    private Transform carryAnchor;
    private GameObject currentItem;
    private bool isCarrying;
    private Collider[] carriedColliders;
    private bool[] colliderStates;
    private Rigidbody[] carriedBodies;
    private bool[] bodyKinematicStates;
    private bool[] bodyGravityStates;
    private Vector3 originalScale;

    public bool IsCarrying() => isCarrying;
    public GameObject GetCurrentItem() => currentItem;

    private void Awake()
    {
        GameObject anchorObject = new GameObject("RuntimeCarryPosition");
        carryAnchor = anchorObject.transform;
        carryAnchor.SetParent(transform, false);
        carryAnchor.localPosition = chestOffset;
        carryAnchor.localRotation = Quaternion.identity;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public void CarryItem(GameObject item)
    {
        if (isCarrying || item == null || carryAnchor == null)
            return;

        currentItem = item;
        isCarrying = true;
        originalScale = item.transform.localScale;

        DisableItemPhysics(item);
        FitItemForCarrying(item);

        if (animator != null)
            animator.SetBool("IsCarrying", true);

        StartCoroutine(MoveToChest(item));
    }

    private IEnumerator MoveToChest(GameObject item)
    {
        const float duration = 0.25f;
        float elapsed = 0f;
        Vector3 startPosition = item.transform.position;
        Quaternion startRotation = item.transform.rotation;

        while (elapsed < duration && item != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / duration)
            );

            item.transform.position = Vector3.Lerp(
                startPosition,
                carryAnchor.position,
                t
            );
            item.transform.rotation = Quaternion.Slerp(
                startRotation,
                carryAnchor.rotation,
                t
            );
            yield return null;
        }

        if (item == null)
            yield break;

        item.transform.SetParent(carryAnchor, true);
        item.transform.position = carryAnchor.position;
        item.transform.rotation = carryAnchor.rotation;
        Debug.Log("Carrying: " + item.name);
    }

    public void DropItem()
    {
        if (!isCarrying || currentItem == null)
            return;

        currentItem.transform.SetParent(null, true);
        currentItem.transform.localScale = originalScale;
        RestoreItemPhysics();

        if (animator != null)
            animator.SetBool("IsCarrying", false);

        currentItem = null;
        isCarrying = false;
        carriedColliders = null;
        colliderStates = null;
        carriedBodies = null;
        bodyKinematicStates = null;
        bodyGravityStates = null;
    }

    private void DisableItemPhysics(GameObject item)
    {
        carriedColliders = item.GetComponentsInChildren<Collider>(true);
        colliderStates = new bool[carriedColliders.Length];

        for (int i = 0; i < carriedColliders.Length; i++)
        {
            colliderStates[i] = carriedColliders[i].enabled;
            carriedColliders[i].enabled = false;
        }

        carriedBodies = item.GetComponentsInChildren<Rigidbody>(true);
        bodyKinematicStates = new bool[carriedBodies.Length];
        bodyGravityStates = new bool[carriedBodies.Length];

        for (int i = 0; i < carriedBodies.Length; i++)
        {
            Rigidbody body = carriedBodies[i];
            bodyKinematicStates[i] = body.isKinematic;
            bodyGravityStates[i] = body.useGravity;
            body.isKinematic = true;
            body.useGravity = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

    private void RestoreItemPhysics()
    {
        if (carriedColliders != null)
        {
            for (int i = 0; i < carriedColliders.Length; i++)
            {
                if (carriedColliders[i] != null)
                    carriedColliders[i].enabled = colliderStates[i];
            }
        }

        if (carriedBodies != null)
        {
            for (int i = 0; i < carriedBodies.Length; i++)
            {
                Rigidbody body = carriedBodies[i];

                if (body == null)
                    continue;

                body.isKinematic = bodyKinematicStates[i];
                body.useGravity = bodyGravityStates[i];
            }
        }
    }

    private void FitItemForCarrying(GameObject item)
    {
        Renderer[] renderers = item.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        float largestSize = Mathf.Max(
            bounds.size.x,
            bounds.size.y,
            bounds.size.z
        );

        if (largestSize > maximumCarrySize)
            item.transform.localScale *= maximumCarrySize / largestSize;
    }
}
