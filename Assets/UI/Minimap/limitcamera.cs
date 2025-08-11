using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class limitcamera : MonoBehaviour
{
    public GameObject Player;

    private void LateUpdate()
    {
        transform.position = new Vector3(Player.transform.position.x, 400, Player.transform.position.z);
    }
}
