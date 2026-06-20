using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerInput input;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private Transform cam;
    [SerializeField] private bool lockCursor = true;

    private void Awake()
    {
        if (input == null) input = GetComponentInChildren<PlayerInput>(true);
        if (movement == null) movement = GetComponentInChildren<PlayerMovement>(true);
        if (cam == null && Camera.main != null) cam = Camera.main.transform;
    }

    private void Start() => ApplyCursorState(lockCursor);

    private void Update()
    {
        if (input == null || movement == null) return;
        if (cam == null && Camera.main != null) cam = Camera.main.transform;
        movement.Move(input.moveDirection, cam, input.SprintHeld);
        if (input.JumpTriggered) movement.Jump();

        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ApplyCursorState(false);
        else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState == CursorLockMode.None) ApplyCursorState(true);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && lockCursor) ApplyCursorState(true);
    }

    private static void ApplyCursorState(bool shouldLock)
    {
        Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !shouldLock;
    }
}
