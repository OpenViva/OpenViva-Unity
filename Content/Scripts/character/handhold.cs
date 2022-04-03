using viva;
using UnityEngine;
using System.Collections;


public class Handhold: VivaScript{

    private readonly Character character;
    private bool bothHands = false;
    private bool? handholdSide = null;
    private Task handholdTask = null;


    public Handhold( Character _character ){
        character = _character;

        SetupAnimations();
        SetupGrabListeners();
    }

    private void SetupAnimations(){
        
        //falling animations
        var stand = character.animationSet.GetBodySet("stand");
        var handholdStartRight = stand.Single( "handhold start right", "stand_handhold_happy_embarrassed_right", false );
        var handholdRightRun = new AnimationSingle( viva.Animation.Load("stand_handhold_happy_pull_hard_right"), character, true );

        var standHandholdRight = stand.Mixer(
            "handhold right",
            new AnimationNode[]{
                new AnimationSingle( viva.Animation.Load("body_stand_happy_idle1"), character, true ),
                new AnimationSingle( viva.Animation.Load("walk"), character, true ),
                handholdRightRun
            },
            new Weight[]{
                character.GetWeight("idle"),
                character.GetWeight("walking"),
                character.GetWeight("running"),
            }
        );
        handholdStartRight.nextState = standHandholdRight;

        var handholdStartLeft = stand.Single( "handhold start left", "stand_handhold_happy_embarrassed_left", false );
        var handholdLeftRun = new AnimationSingle( viva.Animation.Load("stand_handhold_happy_pull_hard_left"), character, true );

        var standHandholdLeft = stand.Mixer(
            "handhold left",
            new AnimationNode[]{
                new AnimationSingle( viva.Animation.Load("body_stand_happy_idle1"), character, true ),
                new AnimationSingle( viva.Animation.Load("walk"), character, true ),
                handholdLeftRun
            },
            new Weight[]{
                character.GetWeight("idle"),
                character.GetWeight("walking"),
                character.GetWeight("running"),
            }
        );
        handholdStartLeft.nextState = standHandholdLeft;
    }

    private void SetupGrabListeners(){
        
        var rightLimb = new Muscle[]{ character.biped.rightArm, character.biped.rightHand };
        BeginHandholdWhenGrabbed( character.biped.rightArm, rightLimb );
        BeginHandholdWhenGrabbed( character.biped.rightHand, rightLimb );

        var leftLimb = new Muscle[]{ character.biped.leftArm, character.biped.leftHand };
        BeginHandholdWhenGrabbed( character.biped.leftArm, leftLimb );
        BeginHandholdWhenGrabbed( character.biped.leftHand, leftLimb );
    }

    private void BeginHandholdWhenGrabbed( Muscle muscle, Muscle[] limb ){
        foreach( var grabbable in muscle.grabbables ){
            grabbable.onGrabbed.AddListener( this, delegate( GrabContext context ){
                Grabber grabber = context.grabber;
                foreach( var limbMuscle in limb ) limbMuscle.strengthLimit.Add( "handhold", 0.0f );
                OnHandholdGrab( character.biped.FindBipedBone( muscle ).Value, grabber );

                if( grabber.character == VivaPlayer.user.character ){
                    if( grabber.sign == 1 ){
                        grabber.character.biped.rightUpperArm.strengthLimit.Add( "handhold", 0.0f );
                        grabber.character.biped.rightArm.strengthLimit.Add( "handhold", 0.0f );
                        grabber.character.biped.rightHand.strengthLimit.Add( "handhold", 0.0f );
                    }else{
                        grabber.character.biped.leftUpperArm.strengthLimit.Add( "handhold", 0.0f );
                        grabber.character.biped.leftArm.strengthLimit.Add( "handhold", 0.0f );
                        grabber.character.biped.leftHand.strengthLimit.Add( "handhold", 0.0f );
                    }
                }
            } );
            grabbable.onReleased.AddListener( this, delegate( GrabContext context ){
                Grabber grabber = context.grabber;
                foreach( var limbMuscle in limb ) limbMuscle.strengthLimit.Remove( "handhold" );
                OnHandholdRelease( character.biped.FindBipedBone( muscle ).Value );
                
                if( grabber.character == VivaPlayer.user.character ){
                    if( grabber.sign == 1 ){
                        grabber.character.biped.rightUpperArm.strengthLimit.Remove( "handhold" );
                        grabber.character.biped.rightArm.strengthLimit.Remove( "handhold" );
                        grabber.character.biped.rightHand.strengthLimit.Remove( "handhold" );
                    }else{
                        grabber.character.biped.leftUpperArm.strengthLimit.Remove( "handhold" );
                        grabber.character.biped.leftArm.strengthLimit.Remove( "handhold" );
                        grabber.character.biped.leftHand.strengthLimit.Remove( "handhold" );
                    }
                }
            } );
        }
    }

    private void OnHandholdGrab( BipedBone bone, Grabber source ){
        bool? sourceRightSide = BipedProfile.IsOnRightSide( bone );
        if( !sourceRightSide.HasValue ) return;

        if( !handholdSide.HasValue ){
            handholdSide = sourceRightSide.Value;
            BeginBehavior( handholdSide.Value, source );
        }else if( !bothHands && sourceRightSide.Value != handholdSide.Value ){
            bothHands = true;
        }
    }

    private void OnHandholdRelease( BipedBone bone ){
        bool? sourceRightSide = BipedProfile.IsOnRightSide( bone );
        if( !sourceRightSide.HasValue ) return;

        if( bothHands ){
            bothHands = false;
        }else{
            handholdSide = false;
            StopBehavior();
        }
    }

    private void StopBehavior(){
        if( handholdTask == null ) return;
        character.autonomy.RemoveTask( handholdTask );
        handholdTask = null;
        handholdSide = null;
        
        var playReturnToIdle = new PlayAnimation( character.autonomy, null, "idle", true, 0, false );
        playReturnToIdle.Start( this, "return to idle" );
    }

    private void BeginBehavior( bool side, Grabber source ){
        
        var facePlayer = new FaceTargetBody( character.autonomy, 1.0f );
        facePlayer.onSuccess += facePlayer.Reset;   //repeat infinitely
        facePlayer.target.SetTargetTransform( source.transform );
        facePlayer.onFixedUpdate += delegate{
            character.locomotionForward.SetPosition( source.character.locomotionForward.position );
        };
        facePlayer.onUnregistered += delegate{
            character.locomotionForward.SetPosition( 0 );
        };

        var playHandhold = new PlayAnimation( character.autonomy, null, side ? "handhold start right" : "handhold start left" );
        playHandhold.onSuccess += delegate{
            facePlayer.RemovePassive( playHandhold );
        };
        playHandhold.onFixedUpdate += delegate{
            if( playHandhold.hasAnimationControl && Mathf.Abs( source.character.locomotionForward.position ) > 0.3f ){
                character.altAnimationLayer.player.context.mainPlaybackState.SetNormalizedTime( 1.01f );
            }
        };
        playHandhold.onExitAnimation += delegate{
            character.biped.lookTarget.SetTargetTransform( source.character.biped.head.target );
        };

        facePlayer.AddPassive( playHandhold );
        
        facePlayer.Start( this, "handhold" );

        handholdTask = facePlayer;
    }
}
