using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class GerakKarakter : MonoBehaviour
{
    [Header("Gerak")]
    public float kecepatanLari = 6f;
    public float kecepatanRotasi = 12f;
    public float offsetArahModel = 90f;

    [Header("Lompat")]
    public float kekuatanLompat = 8f;
    public float gravitasi = -25f;
    public float kecepatanUdaraMultiplier = 0.8f;
    public float durasiAbaikanGroundSetelahLompat = 0.15f;
    public bool doubleJumpEnabled = true;

    [Header("Ground Check")]
    public Transform groundCheckPoint;
    public float groundCheckRadius = 0.25f;
    public LayerMask groundLayer;

    [Header("Animasi")]
    public float kecepatanAnimasiLari = 1.2f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsJumpingHash = Animator.StringToHash("IsJumping");
    private static readonly int IsSaltoHash = Animator.StringToHash("IsSalto");
    private static readonly int JumpStateHash = Animator.StringToHash("Base Layer.Jump");
    private static readonly int SaltoStateHash = Animator.StringToHash("Base Layer.Salto");

    private Animator animator;
    private Rigidbody rb;

    private bool diTanah;
    private bool canDoubleJump;
    private bool isDoubleJump;
    private float velocityY;
    private float groundCheckTerkunciSampai;
    private Vector3 moveVelocity;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        animator.applyRootMotion = false;

        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Update()
    {
        CekTanah();

        Vector2 input = BacaInputGerak();
        Vector3 arahGerak = new Vector3(input.x, 0f, input.y);

        if (arahGerak.sqrMagnitude > 1f)
            arahGerak.Normalize();

        bool bergerak = arahGerak.sqrMagnitude > 0.01f;

        float speedAnim = bergerak ? 1f : 0f;
        animator.SetFloat(SpeedHash, speedAnim, 0.1f, Time.deltaTime);
        animator.SetBool(IsJumpingHash, !diTanah);
        animator.speed = bergerak && diTanah ? kecepatanAnimasiLari : 1f;

        float speedAktif = diTanah ? kecepatanLari : kecepatanLari * kecepatanUdaraMultiplier;
        moveVelocity = arahGerak * speedAktif;

        if (bergerak)
            PutarKeArah(arahGerak);

        if (SpasiBaruDitekan())
        {
            if (diTanah)
            {
                Lompat(false);
            }
            else if (doubleJumpEnabled && canDoubleJump && !isDoubleJump)
            {
                Lompat(true);
            }
        }
    }

    private void FixedUpdate()
    {
        if (diTanah && velocityY < 0f)
        {
            velocityY = -2f;
        }
        else
        {
            velocityY += gravitasi * Time.fixedDeltaTime;
        }

        Vector3 finalVelocity = new Vector3(moveVelocity.x, velocityY, moveVelocity.z);
        rb.linearVelocity = finalVelocity;
    }

    private void CekTanah()
    {
        if (Time.time < groundCheckTerkunciSampai)
        {
            diTanah = false;
            return;
        }

        if (groundCheckPoint == null)
        {
            diTanah = false;
            return;
        }

        bool sebelumnyaDiTanah = diTanah;

        diTanah = Physics.CheckSphere(
            groundCheckPoint.position,
            groundCheckRadius,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        if (diTanah && !sebelumnyaDiTanah)
        {
            canDoubleJump = true;
            isDoubleJump = false;
        }
    }

    private void Lompat(bool salto)
    {
        velocityY = kekuatanLompat;
        diTanah = false;
        groundCheckTerkunciSampai = Time.time + durasiAbaikanGroundSetelahLompat;

        if (salto)
        {
            // Jump kedua = Salto
            isDoubleJump = true;
            canDoubleJump = false;
            animator.SetTrigger(IsSaltoHash);
            animator.speed = 1f;
            animator.CrossFadeInFixedTime(SaltoStateHash, 0.08f);
        }
        else
        {
            // Jump pertama = Jump biasa
            canDoubleJump = true;
            isDoubleJump = false;
            animator.speed = 1f;
            animator.SetBool(IsJumpingHash, true);
            animator.CrossFadeInFixedTime(JumpStateHash, 0.08f);
        }
    }

    private void PutarKeArah(Vector3 arah)
    {
        Quaternion arahRotasi = Quaternion.LookRotation(arah, Vector3.up);
        Quaternion targetRotasi = arahRotasi * Quaternion.Euler(0f, offsetArahModel, 0f);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotasi,
            kecepatanRotasi * Time.deltaTime
        );
    }

    private static Vector2 BacaInputGerak()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return Vector2.zero;

        float h = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        float v = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);

        return new Vector2(h, v);
    }

    private static bool SpasiBaruDitekan()
    {
        Keyboard kb = Keyboard.current;
        return kb != null && kb.spaceKey.wasPressedThisFrame;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
    }
}
