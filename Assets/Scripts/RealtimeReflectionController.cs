using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;


namespace viva{


public class RealtimeReflectionController : MonoBehaviour {

    [SerializeField]
    public ReflectionProbe reflectionProbe;
    
    private float refreshTimer = 0.0f;
    public float refreshTimeout = 1.0f;
    public float maxRefreshTimeout = 10.0f;
    private Vector3 lastPos = Vector3.zero;

    public void Update(){
        
        refreshTimer += Time.deltaTime;
        if( refreshTimer > refreshTimeout ){
            if( refreshTimer > maxRefreshTimeout || Vector3.SqrMagnitude( transform.position-lastPos ) > 0.16f ){
                lastPos = transform.position;
                refreshTimer = 0.0f;
                reflectionProbe.RenderProbe();
            }
        }
    }
}

}