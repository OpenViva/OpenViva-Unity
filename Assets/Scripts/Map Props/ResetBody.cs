using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


namespace viva{

[RequireComponent(typeof(Rigidbody))]
public class ResetBody : MonoBehaviour{

    private Rigidbody rigidbody;
    private Vector3 spawnPos;
    private Quaternion spawnRot;
    public bool awakenOnSpawn = false;

    private void Awake(){
        rigidbody = gameObject.GetComponent<Rigidbody>();
        spawnPos = rigidbody.transform.position;
        spawnRot = rigidbody.transform.rotation;
        gameObject.SetActive( false );
    }

    public void Respawn(){
        gameObject.SetActive( true );
        rigidbody.transform.position = spawnPos;
        rigidbody.transform.rotation = spawnRot;
        
        if( !awakenOnSpawn )rigidbody.isKinematic = true;
    }

    public void OnCollisionEnter( Collision collision ){
        rigidbody.isKinematic = false;
    }
}

}