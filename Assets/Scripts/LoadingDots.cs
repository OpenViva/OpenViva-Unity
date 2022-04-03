using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingDots : MonoBehaviour{

    private void FixedUpdate(){
        float deg = (360.0f/11.0f)*Mathf.Floor( Time.time*10.0f );
        transform.localEulerAngles = new Vector3( 0, 0, deg );
    }
}
