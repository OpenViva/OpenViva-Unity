using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace viva{

public class FireworkExplosion: MonoBehaviour {

    [Range(10,30)]
    [SerializeField]
    private float lifespan = 20;
    [SerializeField]
    private ParticleSystem fireworksPSys;
    [SerializeField]
    private ParticleSystem fireworksSmokeTrailPSys;
    [SerializeField]
    private MeshRenderer flashMR;
    [Range(0,0.4f)]
    [SerializeField]
    private float flashDuration = 0.4f;
    [SerializeField]
    private SoundSet fireworkBooms;
    [SerializeField]
    private Light environmentLight;
    [Range(1,4f)]
    [SerializeField]
    private float lightDuration = 2.0f;

    private float timeStart = 0.0f;
    private static readonly int colorID = Shader.PropertyToID("_Color");
    private static readonly int sizeID = Shader.PropertyToID("_Size");


    public void SetFireworkColor( Color color ){
        flashMR.material.SetColor( colorID, color );
        var main = fireworksPSys.main;
        main.startColor = color;
        environmentLight.color = color;
    }

    public void LateUpdate(){
        float flashAnim = ( Time.time-timeStart )/flashDuration;
        if( flashAnim > 1.0f ){
            flashMR.enabled = false;
        }else{
            flashMR.material.SetFloat( sizeID, Mathf.LerpUnclamped( 0.0f, 4.0f, Mathf.Clamp01( flashAnim ) ) );
        }

        float lightAnim = (Time.time-timeStart)/lightDuration;
        if( lightAnim > 1.0f ){
            environmentLight.enabled = false;
        }else{
            environmentLight.intensity = Mathf.Pow( 1.0f-lightAnim, 3 )*10.0f;
        }
    }

    private void Awake(){
        timeStart = Time.time;

        var seed = (uint)( 1000*Random.value );
        fireworksPSys.randomSeed = seed;
        fireworksSmokeTrailPSys.randomSeed = seed;

        fireworksPSys.gameObject.SetActive( true );
        fireworksSmokeTrailPSys.gameObject.SetActive( true );

        var handle = SoundManager.main.RequestHandle( transform.position );
        handle.maxDistance = 200.0f;
        float delay = Vector3.Distance( GameDirector.instance.mainCamera.transform.position, transform.position )/343;
        handle.PlayDelayed( fireworkBooms.GetRandomAudioClip(), delay );

        SetFireworkColor( Random.ColorHSV( 0.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f ) );

        Destroy( this, lifespan );

        AlertLocalLolis();
    }

    private void AlertLocalLolis(){

        foreach( Character character in GameDirector.characters.objects ){
            Loli loli = character as Loli;
            if( loli == null ){
                continue;
            }
            var diff = character.floorPos-transform.position;
            
            float sqDist = diff.x*diff.x+diff.z*diff.z;
            if( sqDist < 50 ){ //10
                loli.passive.scared.Scare( 4.0f );
                Debug.LogError(sqDist);
            }else if( sqDist < 62500 ){ //250
                var impressedAnim = Loli.Animation.NONE;
                switch( loli.bodyState ){
                case BodyState.STAND:
                    impressedAnim = Loli.Animation.STAND_IMPRESSED1;
                    break;
                case BodyState.FLOOR_SIT:
                    impressedAnim = Loli.Animation.FLOOR_SIT_IMPRESSED1;
                    break;
                case BodyState.SQUAT:
                case BodyState.RELAX:
                    impressedAnim = Loli.Animation.SQUAT_IMPRESSED1;
                    break;
                }
                if( impressedAnim != Loli.Animation.NONE ){
                    
                    Vector3 toFirework = transform.position-loli.head.position;
                    if( Physics.Raycast( loli.head.position, toFirework.normalized, 8.0f, Instance.wallsMask, QueryTriggerInteraction.Ignore ) ){
                        continue;
                    }
                    var playImpressed = new AutonomyPlayAnimation( loli.autonomy, "fireworks impressed", impressedAnim );

                    playImpressed.AddRequirement( new AutonomyFaceDirection( loli.autonomy, "face firework", delegate(TaskTarget target){
                        target.SetTargetPosition( transform.position );
                    } ) );
                    playImpressed.AddRequirement( new AutonomyWait( loli.autonomy, "random wait", Random.value ) );

                    loli.autonomy.Interrupt( playImpressed );

                    loli.SetLookAtTarget( transform );
                    loli.SetViewAwarenessTimeout( 4.0f );
                }
            }
        }
    }
}

}