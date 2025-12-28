using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerControler : NetworkBehaviour
{
    public SpriteRenderer sr;
    public float playerSpeed = 2;
    // Start is called before the first frame update
    void Start()
    {
        Color color = new Color(Random.value, Random.value, Random.value);
        sr.color = color;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;
        Inputs();
    }

    void Inputs()
    {
        float xInput = Input.GetAxis("Horizontal");
        float yInput = Input.GetAxis("Vertical");
        transform.Translate(new Vector2(xInput, yInput) * playerSpeed * Time.deltaTime);
    }
}
