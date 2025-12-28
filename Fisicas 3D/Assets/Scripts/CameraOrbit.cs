using UnityEngine;

public class CameraOrbit : MonoBehaviour
{
    public float rotationSpeed = 5f; // Velocidad de rotación
    public float minVerticalAngle = -30f; // Ángulo mínimo vertical
    public float maxVerticalAngle = 60f; // Ángulo máximo vertical
    public float distance = 5f; // Distancia de la cámara al objeto

    private float currentX = 0f; // Rotación acumulada en el eje vertical
    private float currentY = 0f; // Rotación acumulada en el eje horizontal

    public float mouseSensitivity = 2f;

    void Update()
    {
        if (Input.GetMouseButton(1))
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            // Capturar entrada del ratón
            float inputCameraX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float inputCameraY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            // Actualizar las rotaciones acumuladas
            currentY += inputCameraX; // Rotación horizontal
            currentX -= inputCameraY; // Rotación vertical
            currentX = Mathf.Clamp(currentX, minVerticalAngle, maxVerticalAngle); // Limitar el ángulo vertical

            // Aplicar las rotaciones
            transform.rotation = Quaternion.Euler(currentX, currentY, 0f);
        }

        if (Input.GetMouseButtonUp(1))
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
