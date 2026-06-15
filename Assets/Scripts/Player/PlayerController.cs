using UnityEngine;

public class PlayerController : MonoBehaviour
{

    [SerializeField] private PlayerInput input;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private Transform cam;

    void Update()
    {

        movement.Move(input.moveDirection, cam);
        if (input.JumpTriggered)
        {
            movement.Jump();
        }
    }
}