using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ObjekTembok : MonoBehaviour
{
    private void Awake()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        box.isTrigger = false;

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }
}