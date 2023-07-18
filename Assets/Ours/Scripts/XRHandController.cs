using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public enum HandType {
    Left, Right
}

public class XRHandController : MonoBehaviour
{
    // Start is called before the first frame update
    public HandType handType;
    public float thumbMoveSpeed = 0.1f;

    private Animator animator;
    private InputDevice inputDevice;

    private float indexValue;
    private float thumbValue;
    private float threeFingerValue;
    private bool inputDeviceValid;

    void Start()
    {
        inputDeviceValid = false;
        animator = GetComponent<Animator>();
        //inputDevice = GetInputDevice();
        
    }

    void GetInputDevice() {
        InputDeviceCharacteristics controllerCharacteristic = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller;

        if(handType == HandType.Left){
            controllerCharacteristic = controllerCharacteristic | InputDeviceCharacteristics.Left;
        }
        else{
            controllerCharacteristic = controllerCharacteristic | InputDeviceCharacteristics.Right;
        }

        List<InputDevice> inputDevices = new List<InputDevice>();
        //Debug.Log(inputDevices);
        InputDevices.GetDevicesWithCharacteristics(controllerCharacteristic, inputDevices);

        if(inputDevices != null && inputDevices.Count > 0){
            inputDevice = inputDevices[0];
            inputDeviceValid = true;
        }

        //return inputDevices[0];
    }

    void AnimateHand() {
        inputDevice.TryGetFeatureValue(CommonUsages.trigger, out indexValue);
        inputDevice.TryGetFeatureValue(CommonUsages.grip, out threeFingerValue);

        inputDevice.TryGetFeatureValue(CommonUsages.primaryTouch, out bool primaryTouched);
        inputDevice.TryGetFeatureValue(CommonUsages.secondaryTouch, out bool secondaryTouched);

        if (primaryTouched || secondaryTouched){
            thumbValue += thumbMoveSpeed;
        }
        else{
            thumbValue -= thumbMoveSpeed;
        }

        thumbValue = Mathf.Clamp(thumbValue, 0, 1);

        animator.SetFloat("Index", indexValue);
        animator.SetFloat("ThreeFingers", threeFingerValue);
        animator.SetFloat("Thumb", thumbValue);
    }

    // Update is called once per frame
    void Update()
    {   

        //bool looking = true;
        //Debug.Log(new List<InputDevice>().Count);
        //if(new List<InputDevice>().Count != 0 && looking){
        //    looking = false;
        //    inputDevice = GetInputDevice();
        //}
        //if(!looking){
        //   AnimateHand();
        //}
        if(!inputDeviceValid){
            GetInputDevice();
        }
        AnimateHand();
    }
}
