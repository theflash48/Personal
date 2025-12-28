using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // States
    public enum MenuState { Off, RightBuckle, LeftBuckle, Pause };
    public MenuState menuState;

    public enum PlayerState { Idle, Movement, Attack1, Attack2 };
    public PlayerState playerState;

    // Variables
    public float playerSpeed; 

    // Components
    Animator animator;

    // GameObject


    // Start is called before the first frame update
    void Start()
    {
        SetMenuState(MenuState.Off);
        SetPlayerState(PlayerState.Idle);
    }

    // Update is called once per frame
    void Update()
    {
        MenuSelector();
    }

    void MenuSelector()
    {
        switch (menuState)
        {
            case MenuState.Off:
                MenuOff();
                break;
            case MenuState.RightBuckle:
                MenuOff();
                break;
            case MenuState.LeftBuckle:
                MenuOff();
                break;
            case MenuState.Pause:
                MenuOff();
                break;

        }
    }


    // Menu Controller
    void MenuOff()
    {
        switch (playerState)
        {
            case PlayerState.Idle:
                PlayerIdle();
                break;
        }
    }
        
    // Player Controller

    void PlayerIdle()
    {
        animator.Play("Idle");

        /////////////////////////////
        
        if (playerState == PlayerState.Idle) SetPlayerState(PlayerState.Movement);

    }

    void PlayerWalking()
    {
        if (Input.GetButton(""))
        {
            animator.Play("Running");
        }
        else
        {
            animator.Play("Walking");
        }
    }


    void PlayerJump()
    {

    }

    // State Setters

    void SetMenuState(MenuState m)
    {
        menuState = m;
    }

    void SetPlayerState(PlayerState p)
    {
        playerState = p;
    }
}
