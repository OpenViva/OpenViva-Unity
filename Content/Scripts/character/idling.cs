using UnityEngine;
using System.Collections;
using viva;


public class Idling: VivaScript{

    private readonly Character character;
    private float confuseCount = 0;
    private float faceLookTimer = 0;
    private float idleTimer = 0;
    private FaceTargetBody faceLookAt;


    public Idling( Character _character ){
        character = _character;
        idleTimer = Random.value*8f;
        
        SetupAnimations();

        var listenForIdle = new Task( character.autonomy );
        listenForIdle.onFixedUpdate += ListenForIdle;
        listenForIdle.StartConstant( this, "idle listener" );

        faceLookAt = new FaceTargetBody( character.autonomy, 0.8f, 30f, 0.2f );
        faceLookAt.tags.Add("idle");

        character.biped.lookTarget.onChanged.AddListener( this, CheckIfRandomConfused );
    }

    private void SetupAnimations(){

        //stand tired animations
        var stand = character.animationSet.GetBodySet("stand");
        var standSurprised1 = stand.Single( "surprised", "stand_face_prox_happy_surprise", false );
        standSurprised1.nextState = stand["idle"];
        standSurprised1.AddEvent( Event.Voice(0.1f,"startle"));
        standSurprised1.curves[BipedRagdoll.headID] = new Curve(0.5f,0.1f);

        // var standSurprised2 = new AnimationSingle( viva.Animation.Load("stand_face_prox_angry_surprise"), character, false );
        // standSurprised2.nextState = stand["idle"];
        // stand["surprised"] = standSurprised2;

        var idle2 = stand.Single( "idle2", "stand_idle2", false );
        idle2.nextState = stand["idle"];
        idle2.AddEvent( Event.Voice(0.1f,"thinking long") );
        idle2.AddEvent( Event.Voice(0.8f,"thinking short") );
        idle2.curves[BipedRagdoll.headID] = new Curve(0.5f,0.1f);

        var standConfused = stand.Single( "confused", "stand_confused", false );
		standConfused.AddEvent( Event.Voice(0,"confused") );
        standConfused.nextState = stand["idle"];
    }

    private bool Allowed(){
        return ( character.autonomy.HasTag( "idle" ) && character.locomotionForward.position <= 0.001f );
    }

    private void ListenForIdle(){
        if( Allowed() ){
            var lookBody = character.biped.lookTarget.target as Rigidbody;
            if( lookBody ){
                CheckRandomLook( lookBody );
                CheckActSurprised( lookBody );
            }
        }else{
            character.autonomy.RemoveTask( faceLookAt );
        }
    }

    private void CheckRandomLook( Rigidbody lookBody ){

        faceLookTimer -= Time.deltaTime;
        if( faceLookTimer <= 0 ){
            faceLookTimer = 3f+Random.value*3f;
            var parentItem = lookBody.GetComponent<Item>();
            if( parentItem && parentItem.IsBeingGrabbedByCharacter( character ) ) return;
            faceLookAt.target.SetTargetRigidBody( lookBody );
            faceLookAt.Reset();
            faceLookAt.StartConstant( this, "idle look" );
        }

        idleTimer -= Time.deltaTime;
        if( idleTimer < 0f ){
            idleTimer = 16f+Random.value*20f;

            var playIdle = new PlayAnimation( character.autonomy, null, "idle2" );
            var altIdleAnim = character.animationSet.GetBodySet( character.mainAnimationLayer.currentBodySet.name )["idle2"];
            playIdle.tags.Add("idle");
            //if task is interrupted by anything, immediately return to idle
            playIdle.onInterrupted += delegate{
                var stillInIdleAnim = character.mainAnimationLayer.player.currentState == altIdleAnim;
                var transitioning = character.mainAnimationLayer.player.isTransitioning;
                if( stillInIdleAnim && !transitioning ){
                    character.mainAnimationLayer.player.Play( this, character.mainAnimationLayer.currentBodySet.name, "idle" );
                }
                playIdle.Succeed();
            };
            playIdle.onFixedUpdate += delegate{
                if( !Allowed() ){
                    var returnToIdle = new PlayAnimation( character.autonomy, null, "idle", true, 0, false );
                    returnToIdle.Start( this, "return to idle" );
                    playIdle.Succeed();
                }
            };
            playIdle.Start( this, "idle alternative" );
        }
    }

    private void CheckActSurprised( Rigidbody lookBody ){
        var lookCharacter = Util.GetCharacter( lookBody );
        if( lookCharacter == character ) return;
        var item = lookBody.GetComponent<Item>();
        if( item && character.IsGrabbing( item ) ) return;

        var head = character.biped.head.rigidBody.transform;
        var flatHeadForward = head.forward;
        flatHeadForward.y *= 0.5f;

        var flatVel = lookBody.velocity.normalized;

        if( Vector3.SqrMagnitude( head.position-lookBody.worldCenterOfMass ) < 4.0f && lookBody.velocity.sqrMagnitude > 25.0f &&
            Vector3.Dot( flatVel, flatHeadForward ) < -0.65f ){
            ActSurprised( lookBody );
            if( lookCharacter ){
                character.biped.lookTarget.SetTargetCharacter( lookCharacter );
            }else{
                character.biped.lookTarget.SetTargetRigidBody( lookBody );
            }
        }
    }

    private void CheckIfRandomConfused(){
        if( !character.autonomy.HasTag("idle") ) return;
        var lookObj = character.biped.lookTarget.target as Rigidbody;
        confuseCount++;
        if( lookObj != null && confuseCount > 350 ){
            confuseCount = 0;
            ActConfused( lookObj );
        }
    }

    private void ActConfused( Rigidbody target ){
        var playConfusedAnim = new PlayAnimation( character.autonomy, null, "confused", true, 0 );
        playConfusedAnim.Start( this, "confused" );
    }

    private void ActSurprised( Rigidbody target ){
        //dont act surprised if theres no animation to play (prevents idling without task)
        if( character.mainAnimationLayer.currentBodySet["surprised"] == null ) return;

        var playSurpriseAnim = new PlayAnimation( character.autonomy, null, "surprised", true, 0 );
        playSurpriseAnim.Start( this, "surprised" );

        var faceScareTarget = new FaceTargetBody( character.autonomy, 10, 20, 0.01f );
        faceScareTarget.target.SetTargetRigidBody( target );
        faceScareTarget.onSuccess += delegate{
            playSurpriseAnim.RemovePassive( faceScareTarget );
        };
        faceScareTarget.name = "face surprise rigidBody";

        playSurpriseAnim.AddPassive( faceScareTarget );
    }
}