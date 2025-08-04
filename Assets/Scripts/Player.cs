using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Player : NetworkBehaviour
{
    void PlayerMovement()
    {
        if (isLocalPlayer)
        {
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");
            Vector3 moveDirection = new Vector3(moveX, 0, moveZ);
            transform.position = transform.position + moveDirection * Time.deltaTime * 5f;
        }
    }
    void Update()
    {
        PlayerMovement();
    }
}
