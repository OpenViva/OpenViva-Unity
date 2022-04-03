using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

public class MusicOverrideTrigger : MonoBehaviour{

    private int counter = 0;

    private void Awake(){
    }
    
    private void OnTriggerEnter( Collider collider ){
        var camera = collider.GetComponent<Camera>();
        if( !camera ) return;

        if( counter == 0 ){
            AmbienceManager.main.SetOverrideMusic( AmbienceManager.Music.ONSEN );
        }
        counter++;
    }

    private void OnTriggerExit( Collider collider ){
        var camera = collider.GetComponent<Camera>();
        if( !camera ) return;

        counter--;
        if( counter == 0 ){
            AmbienceManager.main.SetOverrideMusic( null );
        }
    }
}

}