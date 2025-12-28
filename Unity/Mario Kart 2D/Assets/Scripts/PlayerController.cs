using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour

{
    // Variables

    public float speedCurrent;
    public float speedMaxForward;
    public float speedMaxBackward;
    public float speedAcceleration;
    public float brakeResistance;
    public float trackResistance;

    public float rotationSpeed;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;
        Inputs();
        KartPhysics();
    }



    void Inputs()
    {
        // Forwards/Backwards Inputs
        // Speed Value increments while pressing "W" but cannot be more that the max speed
        if (Input.GetKey(KeyCode.W))
        {
            if(speedCurrent >= 0)
            {
                speedCurrent += Time.deltaTime * speedAcceleration;
            }
            else
            {
                speedCurrent += Time.deltaTime * brakeResistance;
            }
            if (speedCurrent > speedMaxForward)
            {
                
                speedCurrent = speedMaxForward;
            }
        }
        // Speed Value decresses while pressing "S" but cannot be less that the half of the negative max speed
        if (Input.GetKey(KeyCode.S))
        {
            if (speedCurrent <= 0)
            {
                speedCurrent -= Time.deltaTime * speedAcceleration;
            }
            else
            {
                speedCurrent -= Time.deltaTime * brakeResistance;
            }
            if (speedCurrent < -speedMaxBackward)
            {
                // 
                speedCurrent = -speedMaxBackward;
            }
        }
        // If neither "W" or "S" are pressed, the kart will start to slow down due to friction with the track
        if (!Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S))
        {
            if (speedCurrent > 0)
            {
                speedCurrent -= Time.deltaTime * trackResistance;
                if (speedCurrent < 0)
                {
                    speedCurrent = 0;
                }
            }
            if (speedCurrent < 0)
            {
                speedCurrent += Time.deltaTime * trackResistance;
                if (speedCurrent > 0)
                {
                    speedCurrent = 0;
                }
            }
        }

        // Turning Inputs
        if (Input.GetKey(KeyCode.A))
        {
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
        }
    }

    void KartPhysics()
    {
        // This is the phisics of the inertia of the kart due to the current speed
        transform.Translate(Vector2.up * Time.deltaTime * speedCurrent);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Wall"))
        {
            if (speedCurrent <= 0)
            {
                speedCurrent = 1;
            }
            else
            {
                speedCurrent = -1;
            }
        }
    }
}
