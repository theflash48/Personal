using UnityEngine;
using TMPro;
using Mirror;

/// <summary>
/// Gestiona la UI de lobby: crear y unir sala y manejo de eventos de sala.
/// </summary>
public class LobbyUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_InputField codeInput;
    [SerializeField] private UnityEngine.UI.Button createButton;
    [SerializeField] private UnityEngine.UI.Button joinButton;
    [SerializeField] private GameObject errorPanel;
    [SerializeField] private TMP_Text errorText;

    [Header("Server Config")]
    [SerializeField] private string serverAddress = "127.0.0.1";

    private CustomNetworkManager networkManager;

    private void Awake()
    {
        networkManager = NetworkManager.singleton as CustomNetworkManager;
        // Suscribirse a eventos de sala
        CustomNetworkManager.OnRoomCreated += HandleRoomCreated;
        CustomNetworkManager.OnClientJoinedRoom += HandleRoomJoined;
        CustomNetworkManager.OnRoomClosed += HandleRoomClosed;
    }

    private void OnDestroy()
    {
        // Desuscribirse
        CustomNetworkManager.OnRoomCreated -= HandleRoomCreated;
        CustomNetworkManager.OnClientJoinedRoom -= HandleRoomJoined;
        CustomNetworkManager.OnRoomClosed -= HandleRoomClosed;
    }

    private void Start()
    {
        SetErrorVisible(false, string.Empty);
        createButton.onClick.AddListener(OnCreateClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
    }

    private void OnCreateClicked()
    {
        string code = codeInput.text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            SetErrorVisible(true, "Introduce al menos un dígito de sala.");
            return;
        }

        SetErrorVisible(false, string.Empty);
#if UNITY_STANDALONE_WIN
        networkManager.StartServer();
#else
        networkManager.networkAddress = serverAddress;
        networkManager.StartClient();
#endif
        networkManager.CmdCreateRoom(code);
    }

    private void OnJoinClicked()
    {
        string code = codeInput.text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            SetErrorVisible(true, "Introduce el código de la sala.");
            return;
        }

        SetErrorVisible(false, string.Empty);
#if UNITY_STANDALONE_WIN
        networkManager.StartServer();
#else
        networkManager.networkAddress = serverAddress;
        networkManager.StartClient();
#endif
        networkManager.CmdJoinRoom(code);
    }

    private void HandleRoomCreated(string code)
    {
        // La sala se creó correctamente en el cliente
        Debug.Log($"Sala creada: {code}");
        // Aquí pasar a la siguiente escena (p.ej. RaceSetupScene)
        // UnityEngine.SceneManagement.SceneManager.LoadScene("RaceSetupScene");
    }

    private void HandleRoomJoined(string code, int connectionId)
    {
        // El cliente se unió con éxito a la sala
        Debug.Log($"Unido a sala: {code}");
        // Carga de escena siguiente
        // UnityEngine.SceneManagement.SceneManager.LoadScene("RaceSetupScene");
    }

    private void HandleRoomClosed(string code)
    {
        // La sala fue cerrada (todos los dispositivos desconectados)
        Debug.Log($"Sala cerrada: {code}");
        // Volver al lobby o limpiar UI
        SetErrorVisible(true, $"Sala {code} cerrada.");
    }

    private void SetErrorVisible(bool visible, string message)
    {
        if (errorPanel != null)
            errorPanel.SetActive(visible);
        if (errorText != null)
            errorText.text = message;
    }
}
