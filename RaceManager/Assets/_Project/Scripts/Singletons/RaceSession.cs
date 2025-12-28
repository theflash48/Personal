using UnityEngine;

/// <summary>
/// Guarda información de la sesión actual de carrera (código de sala, rol, etc.)
/// Persistente entre escenas. No usa FindObjectOfType ni métodos obsoletos.
/// </summary>
public class RaceSession : MonoBehaviour
{
    /// <summary>
    /// Instancia única de RaceSession.
    /// </summary>
    public static RaceSession Instance { get; private set; }

    /// <summary>
    /// Código de sala activo (8 dígitos).
    /// </summary>
    public string RoomCode { get; private set; }

    /// <summary>
    /// Rol del dispositivo dentro de la sala.
    /// </summary>
    public string DeviceRole { get; private set; }

    /// <summary>
    /// Asegura que el objeto no se destruya entre escenas.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Guarda el código de sala cuando se crea o se une.
    /// </summary>
    public void SetRoomCode(string code)
    {
        RoomCode = code;
    }

    /// <summary>
    /// Guarda el rol del dispositivo (Director, Asistente, Panel, Semáforo, etc.)
    /// </summary>
    public void SetDeviceRole(string role)
    {
        DeviceRole = role;
    }

    /// <summary>
    /// Limpia todos los datos de sesión.
    /// </summary>
    public void Clear()
    {
        RoomCode = string.Empty;
        DeviceRole = string.Empty;
    }
}