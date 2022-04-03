using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace viva{

public class ColliderDetector : MonoBehaviour{

    public ColliderReturnFunc onColliderEnter;
    public ColliderReturnFunc onColliderExit;


    public void OnTriggerEnter( Collider collider ){
        onColliderEnter?.Invoke( collider );
    }

    public void OnTriggerExit( Collider collider ){
        onColliderExit?.Invoke( collider );
    }
}

}