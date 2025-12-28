using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputs : MonoBehaviour
{

    public PlayerController _PlayerController;
    public CameraController _CameraController;

    XRIDefaultInputActions _inputs;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _inputs = new XRIDefaultInputActions();
    }

    void OnEnable()
    {
        _inputs.Enable();
        _inputs.NonVRPlayer.Move.performed += _fiMovement;
        _inputs.NonVRPlayer.Move.canceled += _fiMovement;
        _inputs.NonVRPlayer.Camera.performed += _fiCamera;
        _inputs.NonVRPlayer.Camera.canceled += _fiCamera;
        _inputs.NonVRPlayer.Jump.performed += _fiJump;
        //_inputs.NonVRPlayer.Attack.started += ClickOn;
        //_inputs.NonVRPlayer.Attack.canceled += ClickOff;
    }

    void OnDisable()
    {
        _inputs.Disable();
    }

    void _fiMovement(InputAction.CallbackContext context)
    {
        _PlayerController._fiMovement(context.ReadValue<Vector2>());
    }

    void _fiJump(InputAction.CallbackContext context)
    {
        _PlayerController._fiJump();
    }
    void _fiCamera(InputAction.CallbackContext context)
    {
        _CameraController._fiCamera(context.ReadValue<Vector2>());
    }

    void _fClickOn(InputAction.CallbackContext context)
    {
        //_PlayerController._fClickPress();
    }
    
    void _fClickOff(InputAction.CallbackContext context)
    {
        //_PlayerController._fClickRelease();
    }
    
}