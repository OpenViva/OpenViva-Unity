using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace viva{

public class UIArrow : MonoBehaviour{

    public Vector2 direction = new Vector2( -1.0f, 1.0f );
    public float speed = 1.0f;
    
    public void Update(){
        float period = Mathf.Abs( Mathf.Cos( Time.time*speed ) );
        transform.localPosition = new Vector2(
            period*direction.x,
            period*direction.y
        );
    }
}

}