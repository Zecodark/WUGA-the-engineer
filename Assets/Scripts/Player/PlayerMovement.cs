using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] public CharacterController controller;
    [SerializeField] public float speed = 6f;
    [SerializeField] public float jumpHeight = 2f;
    [SerializeField] public float gravity = -9.81f;
    [SerializeField] private Animator animator;
    [SerializeField] private string moveParameter = "move";
    [SerializeField] public float turnSmoothTime = .1f;
    public Transform cam;

    private Vector3 velocity;
    private bool isGrounded;
    public float turnSmoothVelocity;
    private int jumpCount = 0;
    private int maxJumps = 2;
    private bool isCarrying = false;

    // Set true hanya kalau kamu menambahkan kembali parameter "IsRolling" (Bool) di Animator.
    private static readonly bool HasRollingParam = false;
    private bool isRolling = false;
    private bool hasMoveParameter;
    private AnimatorControllerParameterType moveParameterType;
    private bool wasMoving;
    private int baseLayerIndex;
    private Vector3 knockbackVelocity;

    private void Awake()
    {
        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            baseLayerIndex = animator.GetLayerIndex("Base Layer");
            if (baseLayerIndex < 0)
                baseLayerIndex = 0;
        }

        CacheAnimatorParameters();
    }

    void Update()
    {
        
        if (animator == null || controller == null)
            return;

        isGrounded = controller.isGrounded;
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsCarrying", isCarrying);

        if (HasRollingParam)
            animator.SetBool("IsRolling", isRolling);
    }

    public void Move(Vector2 input, Transform cam)
    {
        if (animator == null || controller == null)
            return;

        isGrounded = controller.isGrounded;
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsCarrying", isCarrying);

        if (HasRollingParam)
            animator.SetBool("IsRolling", isRolling);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;

            if (jumpCount > 0)
            {
                jumpCount = 0;
                isRolling = false;
            }
        }

        Vector3 direction = new Vector3(input.x, 0f, input.y).normalized;
        bool isMoving = direction.sqrMagnitude >= 0.01f;

        // Speed di-damping biar transisi idle <-> jalan mulus.
        animator.SetFloat("Speed", direction.magnitude, 0.15f, Time.deltaTime);
        SetMoveParameter(isMoving);

        if (isMoving)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * speed * Time.deltaTime);
        }
        else
        {
            wasMoving = false;
        }

        ApplyKnockbackMotion();

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    public void ApplyKnockback(Vector3 direction, float force)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f || force <= 0f)
            return;

        knockbackVelocity = direction.normalized * force;
    }

    public void Jump()
    {
        if (jumpCount < maxJumps)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpCount++;
            animator.SetFloat("JumpCount", jumpCount);

            if (jumpCount == 1)
            {
                animator.ResetTrigger("DoubleJump");
                animator.SetTrigger("Jump");        // lompatan pertama
                animator.CrossFadeInFixedTime("Jump", 0.05f, baseLayerIndex);
            }
            else if (jumpCount == 2)
            {
                animator.ResetTrigger("Jump");
                animator.SetTrigger("DoubleJump");  // lompatan kedua
                animator.CrossFadeInFixedTime("Double Jump", 0.05f, baseLayerIndex);
            }
        }
    }

    public void SetCarrying(bool carrying)
    {
        isCarrying = carrying;
        if (animator != null)
            animator.SetBool("IsCarrying", isCarrying);
    }

    private void SetMoveParameter(bool isMoving)
    {
        if (animator == null || !hasMoveParameter)
            return;

        if (moveParameterType == AnimatorControllerParameterType.Bool)
        {
            animator.SetBool(moveParameter, isMoving);
        }
        else if (moveParameterType == AnimatorControllerParameterType.Trigger && isMoving)
        {
            if (!wasMoving)
                animator.SetTrigger(moveParameter);
        }

        wasMoving = isMoving;
    }

    private void CacheAnimatorParameters()
    {
        if (animator == null || string.IsNullOrWhiteSpace(moveParameter))
            return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if ((parameter.type == AnimatorControllerParameterType.Bool ||
                 parameter.type == AnimatorControllerParameterType.Trigger) &&
                parameter.name == moveParameter)
            {
                hasMoveParameter = true;
                moveParameterType = parameter.type;
                return;
            }
        }
    }

    private void ApplyKnockbackMotion()
    {
        if (knockbackVelocity.sqrMagnitude < 0.0001f)
            return;

        controller.Move(knockbackVelocity * Time.deltaTime);
        knockbackVelocity = Vector3.MoveTowards(knockbackVelocity, Vector3.zero, 18f * Time.deltaTime);
    }
}
