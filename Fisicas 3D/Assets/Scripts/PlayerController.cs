using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        bool forwardAxis = Input.GetKey(KeyCode.W);
        if (forwardAxis)
        {
            Debug.Log("Movimiento");
            transform.eulerAngles += new Vector3(10, 0, 0) * Time.deltaTime;
        }
    }
}
