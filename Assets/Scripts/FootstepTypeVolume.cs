using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

public class FootstepTypeVolume : MonoBehaviour{

    [SerializeField]
    private FootstepInfo.Type type;
    

    public void OnTriggerEnter( Collider collider ){
        FootstepInfo footstepInfo = collider.GetComponent<FootstepInfo>();
        if( footstepInfo == null ){
            return;
        }
        footstepInfo.AddtoFootstepRegion( type );
    }
    
    public void OnTriggerExit( Collider collider ){
        FootstepInfo footstepInfo = collider.GetComponent<FootstepInfo>();
        if( footstepInfo == null ){
            return;
        }
        footstepInfo.RemoveFromFootstepRegion( type );
    }
}

}