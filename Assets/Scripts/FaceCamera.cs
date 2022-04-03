using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

public class FaceCamera : MonoBehaviour
{
    private void FixedUpdate(){
        if( VivaPlayer.user != null && VivaPlayer.user.camera ) transform.rotation = Quaternion.LookRotation( transform.position-VivaPlayer.user.camera.transform.position, Vector3.up );
    }
}

}