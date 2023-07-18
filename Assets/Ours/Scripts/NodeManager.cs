using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeManager : MonoBehaviour
{

    public GameObject nodePrefab;
    public Vector3 startPos;
    public float length;
    public int nextAvalible;
    public int numNodes;
    public GameObject[] nodes;

    // Start is called before the first frame update
    void Awake()
    {
        //GenerateNode(startPos);
        Setup();
    }

    public void Setup() {
        nextAvalible = 0;
        numNodes = nodePrefab.transform.childCount;
        nodes = new GameObject[numNodes];
        int i = 0;
        foreach(Transform child in nodePrefab.transform){
            nodes[i] = child.gameObject;
            child.position = new Vector3(0.0f, -length+length*i/(float)numNodes, 0.0f);
            if(i != nextAvalible) child.gameObject.SetActive(false);
            i++;
        }
        nextAvalible++;
    }

    public bool[] getBools() {
        bool[] bools = new bool[numNodes];
        for(int index = 0; index < numNodes; index++){
            bools[index] = nodes[index].activeSelf;
        }
        return bools;
    }

    public void SetProperties(Vector3 pos) {
        nodes[nextAvalible].transform.position = pos;
        nodes[nextAvalible].SetActive(true);
        nextAvalible++;
    }

    public bool AnyActive() {
        foreach(GameObject node in nodes) {
            if(node.activeSelf) return true;
        }
        return false;
    }

}
