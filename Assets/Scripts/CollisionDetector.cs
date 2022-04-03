using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace viva{


public class CollisionDetector : MonoBehaviour{


    public CollisionCallback onCollisionEnter;
    public CollisionCallback onCollisionExit;
    public Character character;


    private void OnCollisionEnter( Collision collision ){
        onCollisionEnter?.Invoke( collision );
    }
    
    private void OnCollisionExit( Collision collision ){
        onCollisionExit?.Invoke( collision );
    }
}

}