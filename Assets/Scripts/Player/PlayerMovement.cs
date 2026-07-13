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

    [Header("Movement Audio")]
    [SerializeField] private AudioSource runAudioSource;
    [SerializeField] private AudioSource jumpAudioSource;
    [SerializeField] private AudioClip runSound;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField, Range(0f, 1f)] private float runVolume = 0.65f;
    [SerializeField, Range(0f, 1f)] private float jumpVolume = 0.9f;

    [Header("Collision Safety")]
    [SerializeField] private bool enableFallRecovery = true;
    [SerializeField] private float fallRecoveryY = 3f;
    [SerializeField, Min(0f)] private float recoveryHeightOffset = 0.15f;

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
    private Vector3 lastSafePosition;
    private bool hasSafePosition;
    private Quaternion spawnRotation;
    private bool stabilizeAnimatorOnFirstFrame;
    private Vector3 controllerDrivenPosition;
    private Quaternion controllerDrivenRotation;
    private bool hasControllerDrivenPose;

    private void Awake()
    {
        Vector3 spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            baseLayerIndex = animator.GetLayerIndex("Base Layer");
            if (baseLayerIndex < 0)
                baseLayerIndex = 0;

            // Beberapa clip Wuga memiliki offset root dari file FBX. Rebind
            // animator lebih dulu, lalu kembalikan controller ke spawn agar
            // evaluasi frame pertama tidak melempar karakter keluar map.
            animator.Rebind();
            animator.Update(0f);
        }

        CacheAnimatorParameters();
        ConfigureAudioSources();
        RestoreControllerTransform(spawnPosition, spawnRotation);
        lastSafePosition = transform.position;
        hasSafePosition = true;
        stabilizeAnimatorOnFirstFrame = animator != null;
        CaptureControllerDrivenPose();
    }

    private void LateUpdate()
    {
        if (stabilizeAnimatorOnFirstFrame && animator != null)
        {
            stabilizeAnimatorOnFirstFrame = false;

            // Evaluasi Animator pertama terjadi setelah Awake dan dapat menulis
            // offset root FBX sekali lagi. Toggle Animator setelah evaluasi itu,
            // lalu pulihkan spawn seperti alur yang stabil di runtime.
            animator.enabled = false;
            RestoreControllerTransform(lastSafePosition, spawnRotation);
            animator.enabled = true;
            CaptureControllerDrivenPose();
        }

        RestoreControllerDrivenPose();
    }

    private void OnAnimatorMove()
    {
        // Jalankan koreksi tepat setelah Animator mengevaluasi root clip,
        // sebelum frame berikutnya memakai posisi native CharacterController.
        if (hasControllerDrivenPose)
        {
            RestoreControllerTransform(
                controllerDrivenPosition,
                controllerDrivenRotation
            );
        }
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
            lastSafePosition = transform.position;
            hasSafePosition = true;

            if (jumpCount > 0)
            {
                jumpCount = 0;
                isRolling = false;
            }
        }

        Vector3 direction = new Vector3(input.x, 0f, input.y).normalized;
        bool isMoving = direction.sqrMagnitude >= 0.01f;

        UpdateRunAudio(isMoving && isGrounded);

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
        RecoverIfBelowMap();
        CaptureControllerDrivenPose();
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
            PlayJumpSound();

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

    private void RecoverIfBelowMap()
    {
        if (!enableFallRecovery || !hasSafePosition ||
            transform.position.y >= fallRecoveryY)
        {
            return;
        }

        controller.enabled = false;
        transform.position = lastSafePosition + Vector3.up * recoveryHeightOffset;
        controller.enabled = true;

        velocity = Vector3.zero;
        knockbackVelocity = Vector3.zero;
        jumpCount = 0;
        isRolling = false;

        if (animator != null)
        {
            animator.SetFloat("JumpCount", 0f);
            animator.SetBool("IsGrounded", true);
        }

        Debug.LogWarning("[PlayerMovement] Wuga dikembalikan ke posisi aman karena jatuh keluar map.", this);
    }

    private void RestoreControllerTransform(Vector3 position, Quaternion rotation)
    {
        if (controller == null)
        {
            transform.SetPositionAndRotation(position, rotation);
            return;
        }

        bool wasEnabled = controller.enabled;

        if (wasEnabled)
            controller.enabled = false;

        transform.SetPositionAndRotation(position, rotation);

        if (wasEnabled)
            controller.enabled = true;
    }

    private void CaptureControllerDrivenPose()
    {
        controllerDrivenPosition = transform.position;
        controllerDrivenRotation = transform.rotation;
        hasControllerDrivenPose = true;
    }

    private void RestoreControllerDrivenPose()
    {
        if (!hasControllerDrivenPose)
            return;

        bool positionChanged =
            (transform.position - controllerDrivenPosition).sqrMagnitude > 0.000001f;
        bool rotationChanged =
            Quaternion.Angle(transform.rotation, controllerDrivenRotation) > 0.01f;

        if (positionChanged || rotationChanged)
        {
            // CharacterController menyimpan posisi native terpisah. Toggle
            // component agar koreksi transform juga menyinkronkan kapsulnya.
            RestoreControllerTransform(
                controllerDrivenPosition,
                controllerDrivenRotation
            );
        }
    }

    private void ConfigureAudioSources()
    {
        if (runAudioSource != null)
        {
            runAudioSource.playOnAwake = false;
            runAudioSource.loop = true;
            runAudioSource.clip = runSound;
            runAudioSource.volume = runVolume;
        }

        if (jumpAudioSource != null)
        {
            jumpAudioSource.playOnAwake = false;
            jumpAudioSource.loop = false;
            jumpAudioSource.volume = jumpVolume;
        }
    }

    private void UpdateRunAudio(bool shouldPlay)
    {
        if (runAudioSource == null || runSound == null)
            return;

        if (shouldPlay)
        {
            if (!runAudioSource.isPlaying)
                runAudioSource.Play();
        }
        else if (runAudioSource.isPlaying)
        {
            runAudioSource.Stop();
        }
    }

    private void PlayJumpSound()
    {
        if (jumpAudioSource != null && jumpSound != null)
            jumpAudioSource.PlayOneShot(jumpSound);
    }

    private void OnDisable()
    {
        if (runAudioSource != null)
            runAudioSource.Stop();
    }
}
