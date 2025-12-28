using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public enum _eGameStatus { Playing, Paused }
    public _eGameStatus _vGameStatus = _eGameStatus.Playing;

    [Header("Movimiento")] public float _vMovementSpeed;
    public float _vAcceleration;
    public float _vJumpForce;
    private float _vGravity = -9.81f;

    [Header("Cámara")]
    public float _vCameraSensitivity = 120f;
    public bool _v1stPerson = true;

    [Header("Deadzones")]
    public float _vLeftJoystickDeadZone = 0.2f;
    public float _vRightJoystickDeadZone = 0.2f;

    // Movimiento
    private Vector3 _vInputDirection;   // Lo que el jugador quiere
    private Vector3 _vCurrentDirection; // Lo que realmente se mueve
    private float _vVerticalVelocity;

    // Componentes
    private CharacterController _cCharacterController;

    void Start()
    {
        _cCharacterController = GetComponent<CharacterController>();
    }

    void FixedUpdate()
    {
        if (_vGameStatus != _eGameStatus.Playing) return;

        // Gravedad
        if (_cCharacterController.isGrounded && _vVerticalVelocity < 0)
        {
            _vVerticalVelocity = -2f;
        }
        else
        {
            _vVerticalVelocity += _vGravity * Time.fixedDeltaTime;
        }

        // Suavizar movimiento (aceleración)
        _vCurrentDirection = Vector3.MoveTowards(
            _vCurrentDirection,
            _vInputDirection * _vMovementSpeed,
            _vAcceleration * Time.fixedDeltaTime
        );

        // Transformar movimiento según rotación del jugador
        Vector3 move = transform.TransformDirection(_vCurrentDirection);
        move.y = _vVerticalVelocity;

        _cCharacterController.Move(move * Time.fixedDeltaTime);
    }

    public void _fiMovement(Vector2 input)
    {
        if (input.magnitude > _vLeftJoystickDeadZone)
            _vInputDirection = new Vector3(input.x, 0, input.y).normalized;
        else
            _vInputDirection = Vector3.zero;
    }

    public void _fiJump()
    {
        if (_cCharacterController.isGrounded)
        {
            _vVerticalVelocity = _vJumpForce;
        }
    }
}
