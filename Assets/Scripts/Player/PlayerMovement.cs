using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] public CharacterController controller;
    [SerializeField] public float speed = 4f;
    [SerializeField, Min(1f)] private float sprintMultiplier = 1.55f;
    [SerializeField] public float jumpHeight = 0.9f;
    [SerializeField] public float gravity = -18f;
    [SerializeField] private Animator animator;
    [SerializeField] public float turnSmoothTime = 0.1f;

    private Vector3 velocity;
    private bool isGrounded;
    private float turnSmoothVelocity;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int GroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int JumpHash = Animator.StringToHash("Jump");

    private void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    public void Move(Vector2 input, Transform cameraTransform, bool sprinting)
    {
        if (controller == null) return;
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0f) velocity.y = -2f;

        Vector3 inputDirection = new Vector3(input.x, 0f, input.y);
        float inputMagnitude = Mathf.Clamp01(inputDirection.magnitude);
        if (inputMagnitude > 0.01f)
        {
            Vector3 cameraForward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 cameraRight = cameraTransform != null ? cameraTransform.right : Vector3.right;
            cameraForward.y = 0f; cameraRight.y = 0f; cameraForward.Normalize(); cameraRight.Normalize();
            Vector3 moveDirection = (cameraForward * input.y + cameraRight * input.x).normalized;
            float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            float currentSpeed = speed * (sprinting ? sprintMultiplier : 1f);
            controller.Move(moveDirection * currentSpeed * inputMagnitude * Time.deltaTime);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
        if (animator != null)
        {
            animator.SetBool(GroundedHash, isGrounded);
            animator.SetFloat(SpeedHash, inputMagnitude * (sprinting ? 1.5f : 1f), 0.1f, Time.deltaTime);
        }
    }

    public void Jump()
    {
        if (!isGrounded || controller == null) return;
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        animator?.SetTrigger(JumpHash);
    }
}

