using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]public CharacterController controller;
    [SerializeField]public float speed = 6f;
    [SerializeField]public float jumpHeight = 2f;
    [SerializeField]public float gravity = -9.81f;
    [SerializeField]public float turnSmoothTime = .1f;
    public Transform cam;

    private Vector3 velocity;
    private bool isGrounded;
    public float turnSmoothVelocity;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Move(Vector2 input, Transform cam)
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        Vector3 direction = new Vector3(input.x, 0f, input.y).normalized;
        if (direction.magnitude >= 0.1f) 
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * speed * Time.deltaTime);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

public void Jump() 
{
    if(isGrounded)
    {
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }
}






}
