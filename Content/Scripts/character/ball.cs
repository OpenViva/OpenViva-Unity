using System.Collections;
using UnityEngine;
using viva;


public class Ball: VivaScript{

    private Character character;
    private Item ball;
    private Character lastThrower;

    public Ball( Character _character ){
        character = _character;

        SetupAnimations( character );

        character.biped.lookTarget.onChanged.AddListener( this, OnLookTargetChanged );

        Achievement.Add( "Play ball with a character", "Find a ball and throw it at someone", "ball" );
    }

    private void OnLookTargetChanged(){
        var rigidBody = character.biped.lookTarget.target as Rigidbody;
        if( rigidBody ){
            var item = Util.GetItem( rigidBody );
            if( item ){
                OnItemSeen( item );
            }else{
                var character = Util.GetCharacter( rigidBody );
                if( character && character.biped ){
                    var itemsOnRightHand = character.biped.rightHandGrabber.GetAllItems();
                    foreach( var itemOnHand in itemsOnRightHand ) OnItemSeen( itemOnHand );

                    var itemsOnLeftHand = character.biped.rightHandGrabber.GetAllItems();
                    foreach( var itemOnHand in itemsOnLeftHand ) OnItemSeen( itemOnHand );
                }
            }
        }

    }

    private void OnItemSeen( Item item ){
        if( !item ) return;
        if( item.HasAttribute("ball") ){
            var currentGrabbers = item.GetCharactersGrabbing();
            Character candidateTarget = null;
            foreach( var currentGrabber in currentGrabbers ){
                if( currentGrabber == character ) continue;
                candidateTarget = currentGrabber;
                break;
            }
            if( candidateTarget ){
                var askToPlay = new Timer( character.autonomy, 0.5f+Random.value );
                askToPlay.onSuccess += delegate{

                    if( character.IsGrabbing("ball") || ( ball && ball.HasAttribute("thrown") ) ){
                        character.autonomy.RemoveTask( askToPlay );
                    }else{
                        askToPlay.timeToWait = 5+Random.value*4;
                        askToPlay.Reset();
                        string waveAnim;
                        if( !character.biped.rightHandGrabber.grabbing &&
                            !character.biped.leftHandGrabber.grabbing ){
                            switch( Random.Range(0,5) ){
                            case 0:
                                waveAnim = "distant wave left";
                                break;
                            case 1:
                                waveAnim = "distant wave right";
                                break;
                            default:
                                waveAnim = "distant wave2";
                                break;
                            }
                        }else if( character.biped.leftHandGrabber.grabbing ){
                            waveAnim = "distant wave right";
                        }else{
                            waveAnim = "distant wave left";
                        }
                        var waveToPlay = new PlayAnimation( character.autonomy, null, waveAnim, true, 0.5f, false );
                        
                        var faceGrabber = new FaceTargetBody( character.autonomy );
                        faceGrabber.target.SetTargetCharacter( candidateTarget );

                        waveToPlay.AddPassive( faceGrabber );
                        
                        waveToPlay.Start( this, "wave to play" );
                    
                    }
                };
                askToPlay.tags.Add("idle");

                character.autonomy.RemoveTask( "ask to play" );
                askToPlay.Start( this, "ask to play" );
                lastThrower = candidateTarget;
                SetBall( item );
            }
        }
    }

    private void SetBall( Item _ball ){
        if( ball ){
            ball.onAttributeChanged.RemoveListener( this, OnBallAttributeChanged );
        }
        ball = _ball;
        if( ball ){
            ball.onAttributeChanged.AddListener( this, OnBallAttributeChanged );
        }
    }

    private void OnBallAttributeChanged( Item item, Attribute attribute ){
        if( attribute.name == "thrown" && attribute.count > 0 ){
            ChaseAfterBall();
        }
    }

    private void ChaseAfterBall(){
        if( !ball || ball.IsBeingGrabbedByCharacter( character ) ) return;

        var pickupBall = new Pickup( character.autonomy, ball );
        pickupBall.moveTo.distanceToStartWalking = -1;

        pickupBall.onSuccess += ThrowBallAtThrower;

        pickupBall.moveTo.FailOnStopGesture();
        pickupBall.playPickup.FailOnStopGesture();
        pickupBall.FailOnStopGesture();

        pickupBall.Start( this, "pickup thrown ball" );

		AchievementManager.main.CompleteAchievement( "Play ball with a character", true );
    }

    private void ThrowBallAtThrower(){
        if( !lastThrower ) return;
        
        ball.customVariables.Get( this, "thrower" ).value = character;
        var throwAnimSide = ball.GetGrabContextsByGrabber( character.biped.rightHandGrabber ).Count > 0 ? "right" : "left";
        var throwAnim = new PlayAnimation( character.autonomy, null, "throw "+throwAnimSide, true, 0.6f );

        throwAnim.onFail += delegate{
            SetBall( null );
        };
        throwAnim.FailOnStopGesture();

        var faceThrower = new FaceTargetBody( character.autonomy );
        faceThrower.target.SetTargetCharacter( lastThrower );
        faceThrower.onSuccess += delegate{
            throwAnim.RemoveRequirement( faceThrower );
        };
        throwAnim.AddRequirement( faceThrower );

        throwAnim.Start( this, "throw ball" );
    }

    private void SetupAnimations( Character character ){
        var stand = character.animationSet.GetBodySet("stand");

        var distantWaveRight = stand.Single( "distant wave right", "stand_distant_wave1_right", false, 0.8f );
		distantWaveRight.AddEvent( Event.Voice(0.1f,"ecstatic") );
        distantWaveRight.nextState = stand["idle"];
        distantWaveRight.curves[BipedRagdoll.headID] = new Curve(0,0.1f);
        distantWaveRight.curves[BipedRagdoll.rightArmID] = new Curve(0);
        
        var distantWaveLeft = stand.Single( "distant wave left", "stand_distant_wave1_left", false, 0.8f );
		distantWaveLeft.AddEvent( Event.Voice(0.1f,"ecstatic") );
        distantWaveLeft.nextState = stand["idle"];
        distantWaveLeft.curves[BipedRagdoll.headID] = new Curve(0,0.1f);
        distantWaveLeft.curves[BipedRagdoll.leftArmID] = new Curve(0);

        var distantWave2 = stand.Single( "distant wave2", "stand_distant_wave2", false, 0.8f );
		distantWave2.AddEvent( Event.Voice(0.1f,"ecstatic") );
        distantWave2.nextState = stand["idle"];
        distantWave2.curves[BipedRagdoll.headID] = new Curve(0.5f,0.1f);
        distantWave2.curves[BipedRagdoll.rightArmID] = new Curve(0);

        var throwRight = stand.Single( "throw right", "stand_angry_throw_right", false );
		throwRight.AddEvent( Event.Voice(0.4f,"effort") );
        throwRight.AddEvent( Event.Function( 0.4f, this, Thrown ) );
        throwRight.nextState = stand["idle"];
        throwRight.curves[BipedRagdoll.headID] = new Curve(0,0.2f);
        throwRight.curves[BipedRagdoll.rightArmID] = new Curve(0);
        
        var throwLeft = stand.Single( "throw left", "stand_angry_throw_left", false );
		throwLeft.AddEvent( Event.Voice(0.4f,"effort") );
        throwLeft.AddEvent( Event.Function( 0.4f, this, Thrown ) );
        throwLeft.nextState = stand["idle"];
        throwLeft.curves[BipedRagdoll.headID] = new Curve(0,0.1f);
        throwLeft.curves[BipedRagdoll.leftArmID] = new Curve(0);
    }
    
    private void Thrown(){
        if( !ball ) return;
        ball.AddAttribute( "thrown" );
        var grabbers = ball.GetGrabbers();
        foreach( var grabber in grabbers ){
            grabber.Drop( ball );
        }
        var vel = lastThrower.biped.head.rigidBody.worldCenterOfMass-ball.rigidBody.worldCenterOfMass;
        vel = Vector3.ClampMagnitude( vel, 4f );
        ball.rigidBody.AddForce( vel*4+Vector3.up*8f, ForceMode.VelocityChange );
    }
} 