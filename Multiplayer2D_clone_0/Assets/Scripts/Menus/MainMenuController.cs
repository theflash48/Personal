using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;


public class MainMenuControllers : MonoBehaviour
{
    // Variables



    // GameObjects
    public NetworkManager networkManager;
    public GameObject mPanelMain;
    public GameObject mPanelHost;
    public GameObject mPanelClient;

    // Components
    public UnityTransport unityTransport;

    void Start()
    {
        mPanelMain.SetActive(true);
        mPanelHost.SetActive(false);
        mPanelClient.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
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

    public void ClickBackMain()
    {
        mPanelMain.SetActive(true);
        mPanelHost.SetActive(false);
        mPanelClient.SetActive(false);
    } 

    public void ClickStartHosting()
    {

        mPanelHost.SetActive(false);
        networkManager.StartHost();
    }

    public void ClickStartClient()
    {

        mPanelClient.SetActive(false);
        networkManager.StartClient();
    }
}
