using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class XR_VR_TEST : MonoBehaviour
{
    [SerializeField]
    private XRNode XRnode;
    private InputDevice inputDevice;

    private void OnEnable(){
        List<InputDevice> devices = new List<InputDevice>();
        inputDevice = InputDevices.GetDeviceAtXRNode( XRnode );
        Debug.LogError("[XR] Binded to "+inputDevice.name+" @"+inputDevice.characteristics);
    } 

    private bool pauseMenuDown = false;

    void Update(){
        
        if( !inputDevice.isValid ){
            return;
        }
        inputDevice.TryGetFeatureValue( CommonUsages.primaryButton, out bool primaryDown );
        // inputDevice.TryGetFeatureValue( CommonUsages.grip, out float grip );
        // inputDevice.TryGetFeatureValue( CommonUsages.triggerButton, out bool trigger );
        // inputDevice.TryGetFeatureValue( CommonUsages.primary2DAxis, out Vector2 axis );
        // inputDevice.TryGetFeatureValue( CommonUsages.primary2DAxisClick, out bool axisClick );

        Debug.Log(primaryDown);
    }
}
