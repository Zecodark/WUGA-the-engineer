using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
  
    [SerializeField]private InputAction moveAction;
    [SerializeField]private InputAction jumpAction;
    [SerializeField]private InputAction drawWeaponAction;
    [SerializeField]private InputAction attackAction;
    public Vector2 moveDirection {get; private set;}
    public bool JumpTriggered {get; private set;}
    public bool DrawWeaponTriggered {get; private set;}
    public bool AttackTriggered {get; private set;}
    public bool HasDrawWeaponAction => drawWeaponAction != null;
    public bool HasAttackAction => attackAction != null;
  
  
  
      void Start()
    {
        moveAction.Enable();
        jumpAction.Enable();
        if (drawWeaponAction != null)
            drawWeaponAction.Enable();

        if (attackAction != null)
            attackAction.Enable();
    }

    void Update()
    {
        moveDirection = moveAction.ReadValue<Vector2>();
        JumpTriggered = jumpAction.WasPressedThisFrame();
        DrawWeaponTriggered = drawWeaponAction != null && drawWeaponAction.WasPressedThisFrame();
        AttackTriggered = attackAction != null && attackAction.WasPressedThisFrame();
    }
}
