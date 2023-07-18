using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LinePos : MonoBehaviour
{

    public GameObject ball;
    public Camera mainCam;
    public float lineScale;
    public bool isGround;
    public GameObject head;
    float dist = 100;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 pos = ball.transform.position;
        if(isGround) transform.parent.gameObject.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
        
        LineRenderer rend = this.GetComponent<LineRenderer>();
        Vector3 camPos = mainCam.transform.position;
        dist = Mathf.Sqrt(Mathf.Pow((pos.x-camPos.x), 2)+Mathf.Pow((pos.y-camPos.y), 2)+Mathf.Pow((pos.z-camPos.z), 2));
        if (isGround){
            rend.SetPosition(0, new Vector3(pos.x, -3, pos.z));
            rend.SetPosition(1, new Vector3(pos.x, pos.y-ball.transform.localScale.x, pos.z)); 
            rend.startWidth = 0.01f + dist/lineScale;
            rend.endWidth = 0.1f * dist/lineScale;
        }
        else {
            Vector3 hpos = head.transform.position;
            rend.SetPosition(0, new Vector3(hpos.x, hpos.y, hpos.z));
            rend.SetPosition(1, new Vector3(pos.x, pos.y, pos.z));
            rend.startWidth = 0.01f + dist/lineScale;
        }
        rend.startColor = Color.white;
        rend.endColor = Color.white;
    }
}
