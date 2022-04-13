using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Events;

namespace viva{


public class Fuse : MonoBehaviour{

    [SerializeField]
    private MeshRenderer fuseMR;
    [SerializeField]
    private float fuseDuration = 4.0f;
    [SerializeField]
    private Vector3[] localFusePath = new Vector3[0];
    [SerializeField]
    private GameObject fuseFXContainer;
    [SerializeField]
    public UnityEvent onFuseEnd;
    [SerializeField]
    private AudioClip fuseLoop;

    private float timeStart = 0.0f;
    private static readonly int fuseID = Shader.PropertyToID("_Fuse");
    private static readonly int fuseGlowID = Shader.PropertyToID("_FuseGlow");
    private SoundManager.AudioSourceHandle fuseLoopHandle;


    public void OnDrawGizmosSelected(){
        for( int j=0, i=1; i<localFusePath.Length; j=i++){
            float ratio = (float)i/localFusePath.Length;
		    Gizmos.color = new Color( ratio, 1.0f-ratio, 0.0f, 1.0f );
            var pi = transform.TransformPoint( localFusePath[i] );
            var pj = transform.TransformPoint( localFusePath[j] );
            Gizmos.DrawLine( pi, pj );
        }
    }

    public void OnEnable(){
        timeStart = Time.time;
        fuseFXContainer.gameObject.SetActive( true );
        fuseMR.material.SetFloat( fuseGlowID, 1.0f );

        if( fuseLoopHandle != null ){
            fuseLoopHandle.Stop();
        }
        fuseLoopHandle = SoundManager.main.RequestHandle( Vector3.zero, transform );
        fuseLoopHandle.loop = true;
        fuseLoopHandle.Play( fuseLoop );
    }

    public void OnDisable(){
        fuseFXContainer.gameObject.SetActive( false );
        fuseMR.material.SetFloat( fuseGlowID, 0.0f );
        
        if( fuseLoopHandle != null ){
            fuseLoopHandle.Stop();
        }
    }

    public void Update(){
        float ratio = ( Time.time-timeStart )/fuseDuration;
        if( ratio >= 1.0f ){
            this.enabled = false;
            onFuseEnd.Invoke();
        }else{
            fuseMR.material.SetFloat( fuseID, 1.0f-ratio );

            //position fuse fx at the end of the local fuse path
            int lower = (int)( ratio*localFusePath.Length );
            int upper = lower+1;
            float fraction = ratio*localFusePath.Length-lower;

            Vector3 a = localFusePath[ lower ];
            Vector3 b;
            if( upper >= localFusePath.Length ){
                return;
            }else{
                b = localFusePath[ upper ];
            }
            fuseFXContainer.transform.localPosition = Vector3.LerpUnclamped( a, b, fraction );
            fuseFXContainer.transform.localRotation = Quaternion.LookRotation( a-b, Vector3.up );
        }
    }
}

}