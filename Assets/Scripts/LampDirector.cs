using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace viva{


public class LampDirector : MonoBehaviour {

    [HideInInspector]
    [SerializeField]
    private List<Lamp> mapLamps = new List<Lamp>();
    [Range(5,30)]
    [SerializeField]
    private float globalLampChangeDuration = 20.0f;

    private Coroutine turnOnLampsCoroutine = null;


    public void UpdateDaySegmentLampState( bool instant ){
        
        bool turnOn = (GameDirector.skyDirector.daySegment == SkyDirector.DaySegment.NIGHT);
        
        if( turnOnLampsCoroutine != null ){
            GameDirector.instance.StopCoroutine( turnOnLampsCoroutine );
        }
        if( instant ){
            foreach( Lamp lamp in mapLamps ){
                if( lamp ){
                    lamp.SetOn( turnOn );
                }
            }
        }else{
            SetAllLamps( turnOn );
        }
    }

    private void SetAllLamps( bool on ){

        turnOnLampsCoroutine = GameDirector.instance.StartCoroutine( GradualSetAllLamps( on ) );
    }
    
    private IEnumerator GradualSetAllLamps( bool on ){
        
        List<Lamp> copy = new List<Lamp>(mapLamps);
        while( copy.Count > 0 ){
            
            int randomIndex = Random.Range(0, copy.Count);
            Lamp target = copy[ randomIndex ];
            if( target ){
                target.SetOn( on );
            }
            copy.RemoveAt( randomIndex );

            yield return new WaitForSeconds( globalLampChangeDuration/mapLamps.Count );
        }
        turnOnLampsCoroutine = null;
    }
}

}