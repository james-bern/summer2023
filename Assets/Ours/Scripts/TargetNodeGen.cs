using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetNodeGen : MonoBehaviour
{
    public GameObject target;
    public GameObject head;
    public int num = 5;
    Vector3 pos;
    float slength;

    // Start is called before the first frame update
    void Start()
    {
        slength = -head.transform.position.y*0.9f;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown("space")){
            for(int i = 0; i < num; i++){
                genball();
            }
        }
    }
    void genball() {
        pos = posgen();
        while (Mathf.Abs(pos.magnitude) >= slength || Mathf.Abs(pos.magnitude) <= slength / 2.0f) {Debug.Log("reroll"); pos = posgen();}
        Instantiate(target, pos, Quaternion.identity);
    }
    Vector3 posgen() {
        pos = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, -0.1f), Random.Range(-0.5f, 0.5f));
        return pos;
    }
}
