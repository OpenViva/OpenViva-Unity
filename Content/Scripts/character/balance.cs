using UnityEngine;
using System.Collections;
using viva;


//includes animations for swimming
public class Balance: VivaScript{

    private readonly Character character;
    private float lastBalanceTimer = 0;
    private const float reqStableRegainBalanceTime = 0.5f;
    private float lastUnstableRegainBalanceTime = 0;
    private bool underwaterToggle;
    private bool needToDryOff = false;


    public Balance( Character _character ){
        character = _character;
        SetupAnimations();

        lastBalanceTimer = Time.time+2.0f;
        if( !character.isPossessed ){
            character.autonomy.onFixedUpdate.AddListener( this, ListenForBalanceLost );
            character.biped.onCollisionEnter.AddListener( this, ListenForHitOnHead );
        }
        character.onWater.AddListener( this, OnWater );
        // character.GetInput(Input.Space).onDown.AddListener( this,LoseBalance );
    }

    private void OnWater( Water water ){
        if( water ){
            character.autonomy.onFixedUpdate.AddListener( this, CheckStartSwimming );
        }else{
            character.autonomy.onFixedUpdate.RemoveListener( this, CheckStartSwimming );
        }
    }

    private bool IsUnderwater(){
        if( character.isPossessed ) return false;
        if( !character.onWater.active ) return false;
        if( character.onWater.active.disableSwimming ) return false;
        if( character.biped.surface.HasValue ) return false;

        var hipY = character.biped.hips.rigidBody.worldCenterOfMass.y;
        var togglePadding = character.biped.model.bipedProfile.hipHeight*character.scale*0.25f;
        if( !underwaterToggle ){
            underwaterToggle = hipY < character.onWater.active.surfaceY;
        }else{
            underwaterToggle = hipY-togglePadding < character.onWater.active.surfaceY;
        }
        return underwaterToggle;
    }

    private void CheckStartSwimming(){
        if( IsUnderwater() ){
            WhenInWater();
            needToDryOff = true;
        }else{
            WhenNotOnWater();
        }
    }

    private void WhenInWater(){
        if( character.altAnimationLayer.currentBodySet.name != "swim" ){
            character.autonomy.RemoveTask( "stand up" );
            character.autonomy.RemoveTask( "regain balance" );
            character.altAnimationLayer.player.Play( this, "swim", "idle" );
            character.biped.pinLimit.Remove( "standing up" );

            var swimTask = new PlayAnimation( character.autonomy, "swim", "idle", false, -1, false );
            swimTask.onFixedUpdate += delegate{
                var pin = swimTask.GetPlayedNormalizedTime()/2f;
                character.biped.pinLimit.Set( "in water", pin );
                if( pin>1f ) swimTask.Succeed();
            };
            swimTask.onSuccess += delegate{
                character.biped.pinLimit.Remove( "in water" );
            };
            swimTask.Start( this, "swim" );
        }
    }

    private void WhenNotOnWater(){
        if( character.altAnimationLayer.currentBodySet.name == "swim" ){
            character.autonomy.RemoveTask( "regain balance" );
            character.autonomy.RemoveTask( "swim" );
            character.biped.pinLimit.Remove( "in water" );
            if( !character.biped.isOnWater ){
                CheckDryOff();
                character.altAnimationLayer.player.Play( this, "stand", "idle", 0.5f );
            }else{
                LoseBalance();
            }
        }
    }

    private void ListenForHitOnHead( BipedBone source, Collision collision ){

        // if( character.biped.pinLimit.Get( this ).HasValue ) return;   //ignore if already falling
        var sourceCharacter = Util.GetCharacter( collision.rigidbody );
        if( sourceCharacter == character ) return;

        var sourceItem = Util.GetItem( collision.rigidbody );
        //cannot get hit on head by items currently held
        if( sourceItem && character.IsGrabbing( sourceItem ) ) return;

        const float headHitMin = 72.0f;
        const float bodyHitMin = 275.0f;
        const float groundHitMin = 250.0f;

        float minSqBodyHit;
        float impactForce = collision.relativeVelocity.sqrMagnitude;
        if( Util.IsImmovable( collision.collider ) ){
            minSqBodyHit = groundHitMin;
        }else{
            minSqBodyHit = source == BipedBone.HEAD ? headHitMin : bodyHitMin;
            if( collision.rigidbody ) impactForce *= collision.rigidbody.mass;
        }
        if( impactForce > minSqBodyHit ){
            LoseBalance();
        }
    }

    private void ListenForBalanceLost(){
        // Debug.LogError(character.name+"="+character.biped.pinLimit.ToString());

        if( character.biped.surface.HasValue ){
            lastBalanceTimer = Time.time;

        }else if( Time.time-lastBalanceTimer > 0.4f ){
            lastBalanceTimer = 0.5f; //micro optimization prevent per frame balance checks
            if( !IsUnderwater() ) LoseBalance();
        }
    }

    public void LoseBalance(){
        character.autonomy.RemoveTask( "stand up" );
        character.autonomy.RemoveTask( "swim" );
        StartRegainingBalance();
    }

    private void StartRegainingBalance(){
        //regain balance with knocked out pinless anim
        if( character.autonomy.FindTask( "regain balance" ) == null ){
            character.GetWeight("walking").value = 0;
            character.GetWeight("running").value = 0;

            character.PlayVoiceGroup( "startle short" );

            // character.altAnimationLayer.player.Play( this, character.animationSet["falling"]["falling"], 0.5f );
            character.biped.pinLimit.Add( "standing up", 0 );

            lastUnstableRegainBalanceTime = Time.time;
            var balanceGainListener = new Task( character.autonomy );
            balanceGainListener.onFixedUpdate += delegate{
                CheckIfStationary( balanceGainListener );
            };
            balanceGainListener.onSuccess += StandUp;
            balanceGainListener.onInterrupted += delegate{
                character.altAnimationLayer.player.context.speed.Remove( "waiting for balance" );
            };
            
            balanceGainListener.Start( this, "regain balance", 100 );
        }
    }

    private void CheckIfStationary( Task source ){
        float hipSqVel = character.biped.hips.rigidBody.velocity.sqrMagnitude;
        if( hipSqVel > 1.0f || character.biped.isBeingGrabbed ){
            character.altAnimationLayer.player.Play( this, "falling", "falling", 0.5f );
            character.altAnimationLayer.player.context.speed.Add( "waiting for balance", Mathf.Clamp01( hipSqVel-0.3f )/4 );
            lastUnstableRegainBalanceTime = Time.time;
        }else if( hipSqVel > 0.003f ){
            character.altAnimationLayer.player.Play( this, "falling", "curl", 0.5f );
            character.altAnimationLayer.player.context.speed.Remove( "waiting for balance" );
            lastUnstableRegainBalanceTime = Time.time;
        }else if( Time.time-lastUnstableRegainBalanceTime > reqStableRegainBalanceTime ){
            source.Succeed();
        }
    }

    private void StandUp(){
        if( IsUnderwater() ){
            WhenInWater();
        }else{
            StandUpForGround();
        }
    }

    private void StandUpForGround(){
        if( character.autonomy.FindTask( "stand up" ) == null ){
            string standUpAnim;
            if( character.biped.hips.rigidBody.transform.forward.y > 0.6f ){
                standUpAnim = "face up to stand";
            }else if( character.biped.hips.rigidBody.transform.forward.y < -0.6f ){
                standUpAnim = "face down to stand";
            }else if( character.biped.hips.rigidBody.transform.right.y < 0.0f ){
                standUpAnim = "face side right to stand";
            }else{
                standUpAnim = "face side left to stand";
            }
            CheckDryOff();
            
            var standUpTask = new PlayAnimation( character.autonomy, "falling", standUpAnim, false, 1, false );
            standUpTask.onFixedUpdate += delegate{
                character.biped.pinLimit.Set( "standing up", standUpTask.GetPlayedNormalizedTime() );
                if( character.biped.surface.HasValue) Tools.DrawCross( character.biped.surface.Value,Color.green,0.4f, Time.fixedDeltaTime);
                var p = character.biped.head.rigidBody.worldCenterOfMass;
                if( character.onWater.active) Tools.DrawCross( new Vector3(p.x,character.onWater.active.surfaceY,p.z),Color.red,0.4f, Time.fixedDeltaTime);
            };
            standUpTask.onSuccess += delegate{
                character.biped.pinLimit.Remove( "standing up" );
            };
            
            character.PlayVoiceGroup( "misc" );
            standUpTask.Start( this, "stand up", 99 );
        }
    }

    private void CheckDryOff(){
        if( !needToDryOff ) return;
        if( character.isPossessed ) return;
        needToDryOff = false;
        
        var dryAnim = Random.value > 0.5f ? "dry off right" : "dry off left";
        var dryoff = new PlayAnimation( character.autonomy, "stand", dryAnim );
        dryoff.Start( this, "dry off" );
    }

    private void SetupAnimations(){
        
        //falling animations
        var standLocomotion = character.animationSet.GetBodySet("stand")["idle"];
        var falling = character.animationSet.GetBodySet("falling");
        var fallLocomotion = falling.Single( "falling", "falling_loop", true );
        fallLocomotion.defaultTransitionTime = 0;

        falling.Single( "curl", "floor_curl_loop", false );

        var floorFaceUpToStand = falling.Single( "face up to stand", "floor_face_up_to_stand", false );
        floorFaceUpToStand.nextState = standLocomotion;
        
        var floorFaceDownToStand = falling.Single( "face down to stand", "floor_face_down_to_stand", false );
        floorFaceDownToStand.nextState = standLocomotion;

        var floorFaceSideToStandRight = falling.Single( "face side right to stand", "floor_face_side_to_stand_right", false );
        floorFaceSideToStandRight.nextState = standLocomotion;

        var floorFaceSideToStandLeft = falling.Single( "face side left to stand", "floor_face_side_to_stand_left", false );
        floorFaceSideToStandLeft.nextState = standLocomotion;
        
        //swimming animations
        var swim = character.animationSet.GetBodySet("swim");
        var swimIdle = new AnimationSingle( viva.Animation.Load("swim_idle"), character, true );
        var swimForward = new AnimationSingle( viva.Animation.Load("swim_forward"), character, true, 2 );
        swimForward.AddEvent( Event.Function( 0.2f, this, OnSwimForward ) );

        swim.Mixer( "idle",
            new AnimationNode[]{
                swimIdle,
                swimForward,
                swimForward
            },
            new Weight[]{
                character.GetWeight("idle"),
                character.GetWeight("walking"),
                character.GetWeight("running"),
            },
            true
        );

        var stand = character.animationSet.GetBodySet("stand");

        var standDryOffRight = stand.Single( "dry off right","stand_splashed_end_right", false );
        standDryOffRight.nextState = standLocomotion;
        standDryOffRight.curves[BipedRagdoll.headID] = new Curve(0,0.2f);
        standDryOffRight.AddEvent( Event.Voice(0.2f,"disappointed"));

        var standDryOffLeft = stand.Single( "dry off left", "stand_splashed_end_left", false );
        standDryOffLeft.nextState = standLocomotion;
        standDryOffLeft.curves[BipedRagdoll.headID] = new Curve(0,0.2f);
        standDryOffLeft.AddEvent( Event.Voice(0.2f,"disappointed"));
    }

    public void OnSwimForward(){
        if( !character.onWater.active ) return;
        if( !IsUnderwater() ) return;
        var splashPos = character.biped.upperSpine.rigidBody.worldCenterOfMass;
        character.onWater.active.SplashAura( splashPos, 0.3f );
        var sound = Sound.Create( splashPos );
        sound.pitch = 0.75f+Random.value*0.4f;
        sound.Play( "water", "slosh" );
    }
}
