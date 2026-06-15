using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class PlayerInput : MonoBehaviour
{
  
    [SerializeField]private InputAction moveAction;
    [SerializeField]private InputAction jumpAction;
    public Vector2 moveDirection {get; private set;}
    public bool JumpTriggered {get; private set;}
  
  
  
      void Start()
    {
        moveAction.Enable();
        jumpAction.Enable(); 
    }

    void Update()
    {
        moveDirection = moveAction.ReadValue<Vector2>();
        JumpTriggered = jumpAction.triggered;
    }
}
