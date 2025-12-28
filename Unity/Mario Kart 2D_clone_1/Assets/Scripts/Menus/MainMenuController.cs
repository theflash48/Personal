using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class MainMenuControllers : NetworkBehaviour
{
    // Variables
    
    // Estados
    public enum MenuState { Main, MovingLobby, NoMovingLobby }

    // GameObjects
    public NetworkManager networkManager; // Referencia al NetworkManager para manejar la red
    public GameObject mPanelPolicies;     // Panel que muestra las políticas al inicio
    public GameObject mPanelMain;         // Panel principal del menú
    public GameObject mPanelHost;         // Panel que muestra las opciones para ser host
    public GameObject mPanelClient;       // Panel que muestra las opciones para ser cliente
    public GameObject mPanelHelp;         // Panel de ayuda
    public GameObject mLobby;             // Lobby donde se unen los jugadores al host antes de empezar la partida
    public GameObject mMenuCamera;        // Cámara que se usa en el menú principal

    // Components
    public UnityTransport unityTransport; // Componente de transporte de red (UnityTransport)

    void Start()
    {
        mMenuCamera.SetActive(true);       
        mPanelPolicies.SetActive(true);    
        mPanelMain.SetActive(false);       
        mPanelHost.SetActive(false);       
        mPanelClient.SetActive(false);     
        mPanelHelp.SetActive(false);       
        mLobby.SetActive(false);

        // Establece el NetworkManager como Singleton (único en la escena)
        networkManager.SetSingleton();
        SetIpPort("127.0.0.1", 8123);
    }

    // Update is called once per frame
    void Update()
    {
        // Si no es el dueño del objeto (en red), no se ejecuta el código
        if (!IsOwner) return; 
    }

    public void ClickAcceptPolicies()
    {
        mPanelPolicies.SetActive(false); 
        mPanelMain.SetActive(true);      
    }

    public void ClickRejectPolicies()
    {
        ClickExit(); 
    }

    public void ClickExit()
    {
        Application.Quit(); 
    }

    public void ClickHost()
    {
        mPanelMain.SetActive(false); 
        mPanelHost.SetActive(true);  
    }

    public void ClickClient()
    {
        mPanelMain.SetActive(false);  
        mPanelClient.SetActive(true); 
    }

    public void LoadLobby()
    {
        mLobby.SetActive(true);       
        mPanelHost.SetActive(false);  
        mPanelClient.SetActive(false); 
        mMenuCamera.SetActive(false);  
    }

    public void ClickHelp()
    {
        mPanelHelp.SetActive(true); 
    }

    public void ClickBackMain()
    {
        mPanelMain.SetActive(true);    
        mPanelHost.SetActive(false);   
        mPanelClient.SetActive(false); 
        mPanelHelp.SetActive(false);   
    }

    public void ClickStartHosting()
    {
        LoadLobby();                 
        networkManager.StartHost();  // Inicia el host (servidor) en el NetworkManager
    }

    public void ClickStartClient()
    {
        

        // Intentar iniciar la conexión
        networkManager.StartClient();
        
    }

    void SetIpPort(string ip, ushort port)
    {
        
        // Configurar UnityTransport con la IP y el puerto proporcionados
        unityTransport.ConnectionData.Address = ip;
        unityTransport.ConnectionData.Port = port;
    }
}
