using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnOff : MonoBehaviour
{
    void OnTriggerEnter(Collider other) {
        Destroy(gameObject);
    }
}
