using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LimitCamera : MonoBehaviour
{
    public GameObject Player;

    private void LateUpdate()
    {
        if (Player == null)
        {
            Player = GameObject.FindGameObjectWithTag("Player");
            if (Player == null) return;
        }

        transform.position = new Vector3(Player.transform.position.x, 40, Player.transform.position.z);
    }
}
