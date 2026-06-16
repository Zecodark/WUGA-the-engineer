using UnityEngine;

public class GrabInteraction : MonoBehaviour
{
    [SerializeField] private ItemData itemData;
    [SerializeField] private float grabRange = 2f;
    [SerializeField] private GameObject interactionPrompt;

    private bool isGrabbed = false;


    void Update()
    {
        if (isGrabbed) return;

        // Cek player dalam range
        bool playerInRange = CheckPlayerProximity();
        Debug.Log("Player in range: " + playerInRange);

        // Tampilkan atau sembunyikan prompt
        if (interactionPrompt != null)
        interactionPrompt.SetActive(playerInRange);
        
        Debug.Log("G Pressed: " + Input.GetKeyDown(KeyCode.G));
        // Handle input grab
        if (playerInRange && Input.GetKeyDown(KeyCode.G))
        {
            StartGrab();
        }
    }

    // Physics.OverlapSphere = cek semua collider dalam radius
    bool CheckPlayerProximity()
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position, // Posisi item
            grabRange, // Radius
            LayerMask.GetMask("Player")
        );
        return colliders.Length > 0;
    }
    
    void StartGrab()
    {
        Debug.Log("Grab Started!");
        isGrabbed = true;

        if (interactionPrompt != null)
        interactionPrompt.SetActive(false);

        InventoryManager.Instance.AddItem(itemData);
        Destroy(gameObject);
    }
}