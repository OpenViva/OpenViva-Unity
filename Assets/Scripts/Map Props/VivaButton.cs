using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


namespace viva{

[RequireComponent(typeof(Rigidbody))]
public class VivaButton : MonoBehaviour{
        
    [SerializeField]
    private float downY = 0.1f;
    [SerializeField]
    private UnityEvent onDown;
    private bool reset = false;
    private float startY = 0;
    [SerializeField]
    private Rigidbody rigidBody;
    [SerializeField]
    private AudioClip buttonIn;
    [SerializeField]
    private AudioClip buttonOut;


    private void Awake(){
        startY = transform.localPosition.y;
    }

    private void OnCollisionEnter( Collision collision ){
        enabled = true;
    }

    private void FixedUpdate(){
        if( transform.localPosition.y < downY ){
            if( reset ){
                PlaySound( buttonIn );
                onDown.Invoke();
            }
            reset = false;
        }else{
            if( !reset ){
                PlaySound( buttonOut );
            }
            reset = true;
        }
        if( rigidBody.IsSleeping() ) enabled = false;
    }

    private void PlaySound( AudioClip clip ){
        // var handle = Sound.Create( transform.position );
        // handle.PlayOneShot( clip );
    }
}

}