using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


namespace viva{

[RequireComponent(typeof(Rigidbody))]
public class VerticalMovingPlatform : MonoBehaviour{
        
    [SerializeField]
    private float height = 0.1f;
    [SerializeField]
    private float ease = 2.0f;
    [SerializeField]
    private float maxMove = 0.1f;
    [SerializeField]
    private Rigidbody rigidBody;
    private float startY = 0;
    private bool goUp = false;


    private void Awake(){
        startY = transform.localPosition.y;
    }

    private void FixedUpdate(){
        float targetY = startY;
        if( goUp ) targetY += height;

        float delta = (targetY-rigidBody.transform.position.y)/ease;
        delta = Mathf.Min( Mathf.Abs( delta ), maxMove )*Mathf.Sign( delta );

        rigidBody.MovePosition( rigidBody.transform.position+Vector3.up*delta*Time.fixedDeltaTime );

        if( Mathf.Abs( delta ) <= 0.01f  ) goUp = !goUp;
    }
}

}