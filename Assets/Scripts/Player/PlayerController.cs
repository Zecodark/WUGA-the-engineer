using UnityEngine;

public class PlayerController : MonoBehaviour
{

    [SerializeField] private PlayerInput input;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private Transform cam;
    private bool inputLocked;

    public bool IsInputLocked => inputLocked;

    void Update()
    {

        if (inputLocked)
        {
            movement.Move(Vector2.zero, cam);
            return;
        }

        movement.Move(input.moveDirection, cam);

        if (input.JumpTriggered)
        {
            movement.Jump();
        }
    }

    

    public void LockInput()
    {
        inputLocked = true;
        Debug.Log("[PlayerController] Input LOCKED");
    }

    public void UnlockInput()
    {
        inputLocked = false;
        Debug.Log("[PlayerController] Input UNLOCKED");
    }


}