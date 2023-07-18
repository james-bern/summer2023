using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class block_reset : MonoBehaviour
{
    // Start is called before the first frame update

    Vector3 init_pos;
    Quaternion init_rotation;

    void Start()
    {
        init_pos = transform.position;
        init_rotation = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("r")) {
            transform.position = init_pos;
            transform.rotation = init_rotation;
            GetComponent<Rigidbody>().velocity = new Vector3(0.0f,0.0f,0.0f);
        }
    }
}
