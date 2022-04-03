using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

public class ReceptionBell : MonoBehaviour
{
    [SerializeField]
    private AudioClip bell;


    private void OnCollisionEnter( Collision collision ){

        var handle = Sound.Create( transform.position );
        handle.volume = collision.rigidbody.velocity.magnitude;
        handle.PlayOneShot( bell );
    }
}

}