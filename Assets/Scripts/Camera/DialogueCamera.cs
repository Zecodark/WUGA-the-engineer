using UnityEngine;
using System.Collections;

public class DialogueCamera : MonoBehaviour
{
    
    public static DialogueCamera Instance { get; private set; }

    [SerializeField] private Transform dialoguePosition;
    [SerializeField] private float moveSpeed = 5;

    private Transform originalPosition;
    private Transform mainCamera;
    private bool isMoving = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }


    void Start()
    {
        mainCamera = Camera.main.transform;
    }

    public void SetOriginalPosition(Transform pos)
    {
        originalPosition = pos;
    }
    
    public void MoveToDialoguePosition()
    {
        if (originalPosition != null)
            StartCoroutine(MoveCamera(originalPosition));
    }

    IEnumerator MoveCamera(Transform target)
    {
        isMoving = true;
        
        while (Vector3.Distance(mainCamera.position, target.position) > 0.1f)
        {
            mainCamera.position = Vector3.Lerp(mainCamera.position,
            target.position, moveSpeed * Time.deltaTime);
            mainCamera.rotation = Quaternion.Lerp(mainCamera.rotation,
            target.rotation, moveSpeed * Time.deltaTime);
            yield return null;
        }

        mainCamera.position = target.position;
        mainCamera.rotation = target.rotation;
        isMoving = false;
        


    }


}
