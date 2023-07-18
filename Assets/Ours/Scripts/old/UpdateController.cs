using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateController : MonoBehaviour
{

    public GameObject mySnake;

    void OnEnable()
    {
        //mySnake.transform.GetComponent<snake>().isUpdating = true;
        //mySnake.transform.GetComponent<snake>().curState =  State.Dragging;
        // snake.transform.GetComponent<snake>().dragging = true;
    }

    void OnDisable() {
        //snake.transform.GetComponent<snake>().isUpdating = false;
        // snake.transform.GetComponent<snake>().dragging = false;
        // snake.transform.GetComponent<snake>().curState =  snake.transform.GetComponent<snake>().State.Dragging;
        //mySnake.transform.GetComponent<snake>().ExcuseToUseIEnumeratorAndCoroutinesEvenThoughThereAreDeffinitlyBetterWaysToDoThisAndThisIsntEvenSomthingThatIsVeryNecessaryToDo();
    }
}
