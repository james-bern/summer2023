using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Demo3 : MonoBehaviour
{
    public GameObject box;
    GameObject[] boxes;
    int maxBoxes = 16;
    //int numEnabled = 0;
    float maxDist = 0.3f;
    bool rightStickHeld = false;
    // Start is called before the first frame update
    void OnEnable()
    {
        boxes = new GameObject[maxBoxes];
        //GenerateBox();
    }

    // Update is called once per frame
    void Update()
    {
        bool value;
        bool rightStickPressed;
        bool rightStickTemp = rightStickHeld;
        rightStickHeld = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxisClick, out value) && value;
        rightStickPressed = (!rightStickTemp && rightStickHeld);

        if(rightStickPressed) GenerateBox();
    }

    void GenerateBox() {
        Vector3 pos = new Vector3(Random.Range(-maxDist, maxDist), Random.Range(-maxDist-0.2f, -0.3f), Random.Range(-maxDist, maxDist));
        Destroy(boxes[0]);
        boxes[0] = Instantiate(box, pos, Quaternion.identity);
    }
}
