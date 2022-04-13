using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

public class GlobalAmbienceVolume : MonoBehaviour{

    [SerializeField]
    private Ambience ambience;

    public void OnTriggerEnter( Collider collider ){
        Camera camera = collider.GetComponent<Camera>();
        if( camera != null ){
            GameDirector.instance.ambienceDirector.EnterAmbience( ambience );
        }
    }
    
    public void OnTriggerExit( Collider collider ){
        Camera camera = collider.GetComponent<Camera>();
        if( camera != null ){
            GameDirector.instance.ambienceDirector.ExitAmbience( ambience );
        }
    }
}

}