using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("PlayerController Reference")]
    PlayerController _cPC;

    private Vector2 _vCameraInput = Vector2.zero;
    
    private float yaw;
    private float pitch;

    void Start()
    {
        _cPC = GetComponentInParent<PlayerController>();
        if (_cPC._v1stPerson)
        {
            if (Application.isFocused && _cPC._vGameStatus == PlayerController._eGameStatus.Playing)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }
    }

    public void _fiCamera(Vector2 input)
    {
        // Importante leer la zona muerta desde el player para que esté actualizada en caso de ser modificada.
        _vCameraInput = (input.magnitude > _cPC._vRightJoystickDeadZone) ? input : Vector2.zero;
    }

    void LateUpdate()
    {
        if (_vCameraInput == Vector2.zero)
            return;
        if (_cPC._v1stPerson)
        {
            // Remap magnitude a [0,1] para escalado
            float t = Mathf.InverseLerp(_cPC._vRightJoystickDeadZone, 1f, _vCameraInput.magnitude);

            // Ajustar yaw (rotación del cuerpo) y pitch (rotación de cámara)
            yaw   += _vCameraInput.x * _cPC._vCameraSensitivity * t * Time.deltaTime;
            pitch -= _vCameraInput.y * _cPC._vCameraSensitivity * t * Time.deltaTime;
            pitch  = Mathf.Clamp(pitch, -80f, 80f);

            // Aplicar rotaciones:
            // 1) girar el cuerpo del jugador en yaw
            transform.parent.localRotation = Quaternion.Euler(0f, yaw, 0f);
            // 2) girar la cámara en pitch (solo local X)
            transform.localRotation        = Quaternion.Euler(pitch, 0f, 0f);
        }
        else // NO BORRAR ESTO, IMPORTANTE PARA FUTURO
        {
            Debug.Log("Third Person Camera Status: Work in Progress");
        }
    }
}
