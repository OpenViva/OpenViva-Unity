using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


namespace viva{

public class ScaleWithCamera : MonoBehaviour
{
    [SerializeField]
    private float scale = 1.0f;

    private void LateUpdate(){
        if( Camera.main == null ) return;
        var dist = Vector3.Distance( Camera.main.transform.position, transform.position );
        transform.localScale = Vector3.one*dist*scale;
    }

}

}