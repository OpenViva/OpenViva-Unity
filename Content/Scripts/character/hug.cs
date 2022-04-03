using UnityEngine;
using viva;

public class Hug: VivaScript{

    private Character self;
    private WeightManager1D hugSide;

    public Hug( Character character ){
        self = character;
        hugSide = new WeightManager1D(
            new Weight[]{
                self.GetWeight("hug left"),
                self.GetWeight("hug"),
                self.GetWeight("hug right"),
            },
            new float[]{ -45,0,45 }
        );

        SetupAnimations();
        self.biped.onCollisionEnter.AddListener( this, OnRagdollBoneCollision );

        character.onGesture.AddListener( this, ListenForFollowGesture );

        Achievement.Add( "Give a character a hug", "Approach someone from the front and put both arms behind their back" );
	}

	private void ListenForFollowGesture( string gesture, Character caller ){
		if( gesture == "stop" ){
			StopHugging();
		}
	}

    private void OnRagdollBoneCollision( BipedBone ragdollBone, Collision collision ){
        if( collision.contactCount == 0 ) return;
        if( !collision.rigidbody ) return;
        //character must be idling
        if( !self.autonomy.HasTag( "idle" ) ) return;
        switch( ragdollBone ){
        default:    //test ignores other bone touches
            return;
        case BipedBone.UPPER_SPINE:
        case BipedBone.LOWER_SPINE:
        case BipedBone.UPPER_ARM_R:
        case BipedBone.UPPER_ARM_L:
        case BipedBone.HIPS:
        case BipedBone.HEAD:
            break;
        }
        //it touched by the hand of another user
        Character source = Util.GetCharacter( collision.rigidbody );
        if( !source || source==self ) return;
        if( source.isBiped ){
            if( IsPointBehindBack( source.biped.rightHand.rigidBody.worldCenterOfMass ) && 
                IsPointBehindBack( source.biped.leftHand.rigidBody.worldCenterOfMass ) && 
                !IsPointBehindBack( source.biped.hips.rigidBody.worldCenterOfMass ) ){
                HugCharacter( source );
            }
        }
    }
    
    private bool IsPointBehindBack( Vector3 point ){
        return self.biped.lowerSpine.target.InverseTransformPoint( point ).z < -0.1f/self.scale;
    }

    private void SetupAnimations(){

        var stand = self.animationSet.GetBodySet("stand");
        var standHug = self.animationSet.GetBodySet("stand hug");

        var standToStandHug = new AnimationSingle( viva.Animation.Load("stand_to_stand_hug"), self, false, 0.65f );
        standToStandHug.curves[BipedRagdoll.emotionID] = new Curve(1f);
        stand.transitions[ standHug ] = standToStandHug;
        
        var standHugLoopRight = new AnimationSingle( viva.Animation.Load("stand_hug_happy_right"), self, true );
        var standHugLoop = new AnimationSingle( viva.Animation.Load("stand_hug_happy_loop"), self, true );
        var standHugLoopLeft = new AnimationSingle( viva.Animation.Load("stand_hug_happy_left"), self, true );

        var standHugMain = standHug.Mixer( "hug",
            new AnimationNode[]{
                standHugLoopLeft,
                standHugLoop,
                standHugLoopRight
            },
            hugSide.weights
        );
        standHugMain.curves[BipedRagdoll.headID] = new Curve(0f);
        standHugMain.curves[BipedRagdoll.emotionID] = new Curve(1f);

        standToStandHug.nextState = standHugMain;

        var standHugToStand = new AnimationSingle( viva.Animation.Load("stand_hug_to_stand"), self, false, 0.8f );
        standHugToStand.curves[BipedRagdoll.emotionID] = new Curve(1f);
        standHugToStand.nextState = stand["idle"];
        standHug.transitions[ self.animationSet.GetBodySet("stand") ] = standHugToStand;
    }

    public void HugCharacter( Character target ){
        if( !target ) return;

        //current body must have a hug animation
        if( self.animationSet.GetBodySet( self.altAnimationLayer.currentBodySet.name+" hug")["hug"] == null ) return;

        var prevHugTask = self.autonomy.FindTask("hugging");
        if( prevHugTask != null ) return;

        var playHugAnim = new PlayAnimation( self.autonomy, "stand hug", "hug", true, -1 );

        float successfulHugTimer = 0f;
        var followPlayer = new MoveTo( self.autonomy, 0.15f, 0.5f );
        followPlayer.target.SetTargetCharacter( target );
        followPlayer.onRegistered += delegate{
            self.biped.lookTarget.SetTargetTransform( target.biped.head.transform );
        };

        var moveToStanding = new PlayAnimation( self.autonomy, "stand", "idle", true, 0 );
        followPlayer.AddPassive( moveToStanding );

        var faceCharacter = new FaceTargetBody( self.autonomy, 0.2f );
        faceCharacter.target.SetTargetCharacter( target );
        var voiceTimer = 0f;
        playHugAnim.onFixedUpdate += delegate{
            if( playHugAnim.hasAnimationControl && target ){
                successfulHugTimer += Time.deltaTime;
                voiceTimer -= Time.deltaTime;
                if( voiceTimer < 0 ){
                    voiceTimer = 2+Random.value*5f;
                    if( Random.value < 0.15f ){
                        self.PlayVoiceGroup("giggling");
                    }else{
                        self.PlayVoiceGroup("misc");
                    }
                }
                var bearing = Tools.Bearing( self.biped.head.rigidBody.transform, target.biped.head.rigidBody.worldCenterOfMass );
                hugSide.SetPosition( Mathf.MoveTowards( hugSide.position, bearing, Time.deltaTime*100f ) );
            }
        };

        playHugAnim.onEnterAnimation += delegate{
		    AchievementManager.main.CompleteAchievement( "Give a character a hug", true );
        };

        playHugAnim.onExitAnimation += delegate{
            if( successfulHugTimer > 1.5f ){
                playHugAnim.Succeed();
                playHugAnim.RemoveAllPassivesAndRequirements();
            }
        };

        playHugAnim.AddPassive( faceCharacter );

        playHugAnim.AddRequirement( followPlayer );
        playHugAnim.Start( this, "hugging" );
        

        playHugAnim.onSuccess += DistanceFromCharacter;
    }

    private void DistanceFromCharacter(){
        var moveBackwards = new Timer( self.autonomy, 0.7f );
        moveBackwards.onRegistered += delegate{
            self.altAnimationLayer.player.context.speed.Add( "hugging", -1f );
        };

        moveBackwards.onFixedUpdate += delegate{
            self.locomotionForward.SetPosition( Tools.EaseInOutQuad( 1f-Mathf.Abs( moveBackwards.timeLeft-0.35f )/0.35f )*0.5f );
        };

        moveBackwards.onUnregistered += delegate{
            self.altAnimationLayer.player.context.speed.Remove( "hugging" );
            self.locomotionForward.SetPosition(0f);
        };

        var moveToStanding = new PlayAnimation( self.autonomy, "stand", "idle", true, 0.12f );
        moveBackwards.AddRequirement( moveToStanding );

        moveBackwards.Start( this, "hugging end" );
    }

    private void StopHugging(){
        var hugTask = self.autonomy.FindTask("hugging");
        if( hugTask != null ){
            hugTask.Fail("Told to stop");
            hugTask.RemoveAllPassivesAndRequirements();
        }
    }
}