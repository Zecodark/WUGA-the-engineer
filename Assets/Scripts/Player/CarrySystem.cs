using UnityEngine;
using System.Collections;

public class CarrySystem : MonoBehaviour
{
[SerializeField] private Transform carryPosition;
[SerializeField] private Animator animator;

private GameObject currentItem;
private bool isCarrying = false;

public bool IsCarrying() => isCarrying;
public GameObject GetCurrentItem() => currentItem;

public void CarryItem(GameObject item)
{
    if (isCarrying) return;

    currentItem = item;
    isCarrying = true;

    // Disable collider saat carry
    Collider col = item.GetComponent<Collider>();
    if (col != null) col.enabled = false;

    // Play carry animation
    animator.SetBool("IsCarrying", true);

    // Mulai coroutine untuk smooth carry
    StartCoroutine(SmoothCarry(item));
}

IEnumerator SmoothCarry(GameObject item)
{
    float duration = 0.3f;
    float elapsed = 0f;

    Vector3 startPos = item.transform.position;
    Quaternion startRot = item.transform.rotation;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;

        item.transform.position = Vector3.Lerp(startPos, carryPosition.position, t);
        item.transform.rotation = Quaternion.Lerp(startRot, carryPosition.rotation, t);
        
        yield return null;
    }

    // Setelah smooth attach ke parent
    item.transform.SetParent(carryPosition);
    item.transform.localPosition = Vector3.zero;
    item.transform.localRotation = Quaternion.identity;

    //Collider col = item.GetComponent<Collider>();
    //if (col != null) col.enabled = false;
    Debug.Log("Carrying: " + item.name);

}


public void DropItem()
 {
    if(!isCarrying) return;

    // Detach item dari carry position
    currentItem.transform.SetParent(null);

    // Enable collider
    Collider col = currentItem.GetComponent<Collider>();
    if (col != null) col.enabled = true;

    // Reset animation
    animator.SetBool("IsCarrying", false);

    currentItem = null;
    isCarrying = false;

    Debug.Log("Dropped Item");
 }

}
