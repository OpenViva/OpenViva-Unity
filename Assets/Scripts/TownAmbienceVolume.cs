using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

public class TownAmbienceVolume : MonoBehaviour{

    [Header("Must Be In Camera Layer")]
    private static int townCounter = 0;

    public void OnTriggerEnter( Collider collider ){
		Camera camera = collider.GetComponent<Camera>();
		if( camera == null ){
			return;
		}
        if( townCounter++ == 0 ){
            GameDirector.instance.SetUserInTown( true );
        }
    }

    public void OnTriggerExit( Collider collider ){
		Camera camera = collider.GetComponent<Camera>();
		if( camera == null ){
			return;
		}
        if( --townCounter == 0 ){
            GameDirector.instance.SetUserInTown( false );
        }
    }
}

}