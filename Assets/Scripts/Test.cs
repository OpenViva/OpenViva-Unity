using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace viva{

public class Test : MonoBehaviour{

    private Grabber grabber;
    public Grabber test;

    private void Awake(){
        grabber = GetComponent<Grabber>();
    }

    private void FixedUpdate(){
        var plank1 = GameObject.Find("grab_cube");
        if( plank1 ){
            var grabbable = plank1.GetComponentInChildren<Grabbable>();

            Tools.DrawCross( grabber.worldGrabCenter, Color.green, 0.01f, Time.fixedDeltaTime );
            var bx = grabbable.GetComponent<BoxCollider>();
            Tools.DrawDiagCross( Tools.ClosestPointOnBoxColliderSurface( grabber.worldGrabCenter, bx ), Color.cyan, 0.01f, Time.fixedDeltaTime);

            // if( !grabbable.GetBestGrabPose( out Vector3 grabPos, out Quaternion grabRot, grabber ) ){
            //     return;
            // }
            // test.ApplyGrabPose( grabPos, grabRot );
            // // Tools.DrawCross( grabPos, Color.green, 0.04f, Time.fixedDeltaTime );
            // Debug.DrawLine( grabPos, grabber.worldGrabCenter, Color.magenta, Time.fixedDeltaTime );
        }
    }
}

}