using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.Management;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;



namespace viva{

public partial class KeyboardCharacterControls : CameraControls{

    public class GrabNearbyAnimatorInfo{
        public float availableGrabAlpha;
        private Character character;
        public bool rightSide;
        public float downTime = 0.0f;
        public float targetGripping;
        public Quaternion idleOffset;
        public IKHandle offerHandle;
        public bool recentlyOffered;
        public Grabber grabber { get{ return rightSide ? character.biped.rightHandGrabber : character.biped.leftHandGrabber; } }

        public GrabNearbyAnimatorInfo( Character _character, bool _rightSide, Quaternion _idleOffset ){
            character = _character;
            rightSide = _rightSide;
            idleOffset = _idleOffset;
        }
    }

    [SerializeField]
    private float accel = 5.0f;
    [SerializeField]
    private float movementFriction = 0.9f;
    [SerializeField]
    private float heightSmooth = 0.01f;
    [SerializeField]
    private PauseBook pauseBook;

    private IKHandle rightArmIK;
    private IKHandle leftArmIK;
    private float armAnim = 0;
    private GrabNearbyAnimatorInfo rightGrabAnimatorInfo;
    private GrabNearbyAnimatorInfo leftGrabAnimatorInfo;
    private bool shiftDown = false;
    private Coroutine stopGestureCoroutine = null;
    private Interaction interaction;
    private InteractionInfo interactionInfo;
    public LimitGroup enableLook { get; private set; }


    protected override void BindCharacter( Character character ){
        if( character == null ) return;
        character.ragdoll.SetMusclePinMode( true );

        character.onReset += OnCharacterReset;
                
        rightGrabAnimatorInfo = new GrabNearbyAnimatorInfo( character, true, Quaternion.Euler(0,-90,-90) );
        leftGrabAnimatorInfo = new GrabNearbyAnimatorInfo( character, false, Quaternion.Euler(0,90,90) );

        character.biped.rightHandGrabber.detectSphere.gameObject.SetActive( false );
        character.biped.leftHandGrabber.detectSphere.gameObject.SetActive( false );
        BindGrabUtilities( character );
        player.gestures.BindToTrackedHands( character.biped.rightHand.rigidBody.transform, character.biped.leftHand.rigidBody.transform, false );
        
        player.character.SetBipedAnimationLayers( new BipedMask[]{
            BipedMask.SPINE_CHAIN|BipedMask.LEG_CHAIN_L|BipedMask.LEG_CHAIN_R|BipedMask.SHOULDER_R|BipedMask.SHOULDER_L,
            BipedMask.ARM_CHAIN_L|BipedMask.ARM_CHAIN_R|BipedMask.FINGERS_L|BipedMask.FINGERS_R
            },
            null,
            1,
            0
        );
        enableLook = new LimitGroup( null, 0, 1, true );

        character.biped.rightLeg.rigidBody.gameObject.SetActive( false );
        character.biped.leftLeg.rigidBody.gameObject.SetActive( false );

        DetachHands();
    }
    protected override void UnbindCharacter( Character character ){
        if( character == null ) return;
        character.ragdoll.SetMusclePinMode( false );

        character.onReset -= OnCharacterReset;

        rightArmIK?.Kill();
        leftArmIK?.Kill();
        
        character.biped.rightHandGrabber.detectSphere.gameObject.SetActive( true );
        character.biped.leftHandGrabber.detectSphere.gameObject.SetActive( true );
        UnbindGrabUtilities( character );
        player.gestures.UnbindFromTrackedHands();

        character.SetBipedAnimationLayers( null, null, 0, 0 );

        character.biped.rightLeg.rigidBody.gameObject.SetActive( true );
        character.biped.leftLeg.rigidBody.gameObject.SetActive( true );

        // AttachHands();
    }

    private void DetachHands(){
        var character = player.character;
        character.biped.rightHand.rigidBody.transform.SetParent( character.biped.upperSpine.rigidBody.transform, true );
        character.biped.rightHand.joint.connectedBody = character.biped.upperSpine.rigidBody;
        character.biped.rightHand.Initiate( character.biped.muscles );
        character.biped.rightUpperArm.rigidBody.gameObject.SetActive( false );

        character.biped.leftHand.rigidBody.transform.SetParent( character.biped.upperSpine.rigidBody.transform, true );
        character.biped.leftHand.joint.connectedBody = character.biped.upperSpine.rigidBody;
        character.biped.leftHand.Initiate( character.biped.muscles );
        character.biped.leftUpperArm.rigidBody.gameObject.SetActive( false );
    }

    private void AddControls( Character character ){
        character.GetInput( Input.E ).onDown._InternalAddListener( ToggleRightHandOffer );
        character.GetInput( Input.Q ).onDown._InternalAddListener( ToggleLeftHandOffer );
        character.GetInput( Input.W ).onDown._InternalAddListener( player.movement.KeyboardForwardDown );
        character.GetInput( Input.W ).onUp._InternalAddListener( player.movement.KeyboardForwardUp );
        character.GetInput( Input.A ).onDown._InternalAddListener( player.movement.KeyboardLeftDown );
        character.GetInput( Input.A ).onUp._InternalAddListener( player.movement.KeyboardLeftUp );
        character.GetInput( Input.S ).onDown._InternalAddListener( player.movement.KeyboardBackwardDown );
        character.GetInput( Input.S ).onUp._InternalAddListener( player.movement.KeyboardBackwardUp );
        character.GetInput( Input.D ).onDown._InternalAddListener( player.movement.KeyboardRightDown );
        character.GetInput( Input.D ).onUp._InternalAddListener( player.movement.KeyboardRightUp );
        if( UI.main != null ) character.GetInput( Input.P ).onUp._InternalAddListener( UI.main.ToggleUI );
        character.GetInput( Input.Tab ).onDown._InternalAddListener( ToggleTasks );
        character.GetInput( Input.Space ).onDown._InternalAddListener( Jump );
        character.GetInput( Input.V ).onDown._InternalAddListener( ToggleRagdoll );
        character.GetInput( Input.LeftShift ).onDown._InternalAddListener( ShiftDown );
        character.GetInput( Input.LeftShift ).onUp._InternalAddListener( ShiftUp );
        character.GetInput( Input.RightAction ).onDown._InternalAddListener( HandRight );
        character.GetInput( Input.LeftAction ).onDown._InternalAddListener( HandLeft );
        character.GetInput( Input.RightAction ).onUp._InternalAddListener( ReleaseRight );
        character.GetInput( Input.LeftAction ).onUp._InternalAddListener( ReleaseLeft );
        character.GetInput( Input.LeftShift ).onDown._InternalAddListener( player.movement.KeyboardLeftShiftDown );
        character.GetInput( Input.LeftShift ).onUp._InternalAddListener( player.movement.KeyboardLeftShiftUp );
        character.GetInput( Input.F ).onDown._InternalAddListener( StartFollowGesture );
        character.GetInput( Input.F ).onUp._InternalAddListener( EndFollowGesture );
        character.GetInput( Input.R ).onDown._InternalAddListener( StartHelloGesture );
        character.GetInput( Input.LeftControl ).onDown._InternalAddListener( ToggleCrouch );
        character.onScroll._InternalAddListener( ArmAnimScroll );


        character.biped.rightHandGrabber.onReleased._InternalAddListener( OnRightGrabberRelease );
        character.biped.leftHandGrabber.onReleased._InternalAddListener( OnLeftGrabberRelease );
    }

    private int menuFlip = 1;
    private void ToggleTasks(){
        if (Scene.main.sceneSettings.type == "main"){
            menuFlip = ++menuFlip%2;
        }else{
            menuFlip = ++menuFlip%3;
        }
        switch( menuFlip ){
        case 0:
            pauseBook.SetOpen( true );
            break;
        case 1:
            pauseBook.SetOpen( false );
            if( UI.main && !UI.main.isUIActive ) UI.main.ToggleUI();
            break;
        case 2:
            if( UI.main && UI.main.isUIActive ) UI.main.ToggleUI();
            break;
        }
    }

    private void ShiftDown(){
        shiftDown = true;
    }

    private void ShiftUp(){
        shiftDown = false;
        AchievementManager.main?.CompleteAchievement("TEST",true);
    }

    private void HandLeft(){
        if( InputManager.cursorActive ) return;
        leftGrabAnimatorInfo.downTime = Time.time;
        leftGrabAnimatorInfo.targetGripping = 1f;
        
        player.gestures.StartPointing( Camera.main.transform, Vector3.forward );
    }
    
    private void ReleaseLeft(){
        if( InputManager.cursorActive ) return;
        if( shiftDown ){
            player.character.biped.leftHandGrabber.ReleaseAll();
        }else if( Time.time-leftGrabAnimatorInfo.downTime < 0.3f && !leftGrabAnimatorInfo.grabber.grabbing ){
            leftGrabAnimatorInfo.grabber.Grab( leftGrabAnimatorInfo.grabber.GetClosestGrabbable() );
        }
        leftGrabAnimatorInfo.targetGripping = 0f;

        player.gestures.StopPointing();
    }
    
    private void HandRight(){
        if( InputManager.cursorActive ) return;
        rightGrabAnimatorInfo.downTime = Time.time;
        rightGrabAnimatorInfo.targetGripping = 1f;

        player.gestures.StartPointing( Camera.main.transform, Vector3.forward );
    }

    private void ReleaseRight(){
        if( InputManager.cursorActive ) return;
        if( shiftDown ){
            player.character.biped.rightHandGrabber.ReleaseAll();
        }else if( Time.time-rightGrabAnimatorInfo.downTime < 0.3f && !rightGrabAnimatorInfo.grabber.grabbing ){
            rightGrabAnimatorInfo.grabber.Grab( rightGrabAnimatorInfo.grabber.GetClosestGrabbable() );
        }
        rightGrabAnimatorInfo.targetGripping = 0f;

        player.gestures.StopPointing();
    }

    private void ToggleRightHandOffer(){
        if( InputManager.cursorActive ) return;
        ToggleOffer( rightGrabAnimatorInfo, player.character.biped.rightArmIK );
    }

    private void ToggleLeftHandOffer(){
        if( InputManager.cursorActive ) return;
        ToggleOffer( leftGrabAnimatorInfo, player.character.biped.leftArmIK );
    }

    private void ToggleOffer( GrabNearbyAnimatorInfo info, ArmIK armIK ){
        if( info.offerHandle == null ){
            float offerTimer = 0;
            armIK.AddRetargeting( delegate( out Vector3 target, out Vector3 pole, out Quaternion handRotation ){
                var camera = Camera.main.transform;
                target = armIK.hand.transform.position+camera.forward;
                pole = player.character.model.armature.position;

                handRotation = Camera.main.transform.rotation*info.idleOffset*Quaternion.Euler(0,-armIK.sign*150.0f,0);
                
                if( !info.recentlyOffered && info.offerHandle != null && info.offerHandle.alive ){
                    offerTimer += Time.deltaTime;
                    if( offerTimer > 0.5f ){
                        info.recentlyOffered = true;
                        player.gestures.FireGesture( Gesture.PRESENT_START, armIK.sign==1 );
                    }
                    if( info.grabber.grabbing ){
                    }
                }
            }, out info.offerHandle );
        }else{
            info.offerHandle.Kill();
            info.offerHandle = null;
            if( info.recentlyOffered ){
                info.recentlyOffered = false;
                player.gestures.FireGesture( Gesture.PRESENT_END, armIK.sign==1, true );
            }
        }
    }

    private void ArmAnimScroll( float value ){
        if( InputManager.cursorActive ) return;
        armAnim = Mathf.Clamp( armAnim+value*0.0005f, -1, 1 );
    }

    private void ToggleRagdoll(){
        if( InputManager.cursorActive ) return;
        if( player.character.biped.pinLimit.value == 1 ){
            player.character.biped.muscleLimit.Add( "kb controls", 0.04f );
            player.character.biped.pinLimit.Add( "kb controls", 0 );
        }else{
            player.character.biped.muscleLimit.Remove( "kb controls" );
            player.character.biped.pinLimit.Remove( "kb controls" );
        }
    }

    private void StopGestureCoroutine(){
        if( stopGestureCoroutine != null ){
            StopCoroutine( stopGestureCoroutine );
            stopGestureCoroutine = null;
        }
    }

    private void StartFollowGesture(){
        if( InputManager.cursorActive ) return;
        StopGestureCoroutine();
        stopGestureCoroutine = StartCoroutine( DelayStopGesture() ); 
    }

    private void StartHelloGesture(){
        if( InputManager.cursorActive ) return;
        PlayArmGestureAnimation( Gesture.HELLO, "hello gesture" );
    }

    private void ToggleCrouch(){
        if( InputManager.cursorActive ) return;
        if( player.character.altAnimationLayer.IsPlaying("crouch") ){
            player.character.altAnimationLayer.player._InternalPlay( player.character.animationSet.GetBodySet("stand legs")[ "idle" ] );
        }else{
            player.character.altAnimationLayer.player._InternalPlay( player.character.animationSet.GetBodySet("stand legs")[ "crouch" ] );
        }
    }

    private IEnumerator DelayStopGesture(){
        yield return new WaitForSeconds( 0.4f );
        stopGestureCoroutine = null;
        PlayArmGestureAnimation( Gesture.STOP, "stop gesture" );
    }

    private void EndFollowGesture(){
        if( stopGestureCoroutine != null ){
            StopGestureCoroutine();
            PlayArmGestureAnimation( Gesture.FOLLOW, "follow gesture" );
        }
    }

    public void PlayTorsoAnimation( string animationGroup, float transitionTime=0.5f ){
        player.character.mainAnimationLayer.player._InternalPlay( player.character.animationSet.GetBodySet( "stand" )[ animationGroup ], transitionTime );
    }

    private void PlayArmGestureAnimation( Gesture gesture, string animationGroupName ){
        PlayTorsoAnimation( animationGroupName );
        player.gestures.FireGesture( gesture, player.gestures.rightGestureHand );
    }

    private void OnRightGrabberRelease( GrabContext context ){
        if( !player ) return;
        if( !player.character.biped.rightHandGrabber.grabbing ){
            rightGrabAnimatorInfo.targetGripping = 0;
            if( rightGrabAnimatorInfo.recentlyOffered ) ToggleRightHandOffer();
        }
    }

    private void OnLeftGrabberRelease( GrabContext context ){
        if( !player ) return;
        if( !player.character.biped.leftHandGrabber.grabbing ){
            leftGrabAnimatorInfo.targetGripping = 0;
            if( leftGrabAnimatorInfo.recentlyOffered ) ToggleLeftHandOffer();
        }
    }

    private void OnCharacterReset(){
        enableLook._InternalReset();
        var character = player.character;
        
        character.mainAnimationLayer.player.onAnimate += delegate{
            character.biped.rightHand.UpdateAnchor();
            character.biped.leftHand.UpdateAnchor();
        };
        character.biped.onPostMap += SetFinalCameraTranform;
        AddControls( character );

        rightArmIK?.Kill();
        leftArmIK?.Kill();

        var crouchMove = new AnimationSingle( Animation.Load("CROUCH"), character, true, 0.8f );
        crouchMove.AddEvent( Event.Footstep(.44f,false) );
        crouchMove.AddEvent( Event.Footstep(.96f,true) );

        var standLegs = character.animationSet.GetBodySet("stand legs");
        var crouchLocomotion = standLegs.Mixer(
            "crouch",
            new AnimationNode[]{
                new AnimationSingle( Animation.Load("CROUCH_IDLE2"), character, true ),
                crouchMove,
                crouchMove
            },
            new Weight[]{
                character.GetWeight("idle"),
                character.GetWeight("walking"),
                character.GetWeight("running")
            },
            false
        );
        crouchLocomotion.curves[BipedRagdoll.emotionID] = new Curve(1f);


        var stand = character.animationSet.GetBodySet("stand", character.mainAnimationLayerIndex );
        var torsoIdle = stand.Single( "idle", "torso_idle", true, 1 );
        torsoIdle.curves[BipedRagdoll.ikID] = new Curve(1);

        var gestureComeHere = stand.Single( "follow gesture", "gesture_come_here", false, 1 );
        gestureComeHere.nextState = torsoIdle;
        gestureComeHere.defaultTransitionTime = 0.5f;
        
        var gestureHello = stand.Single( "hello gesture", "GESTURE_HELLO", false, 1 );
        gestureHello.nextState = torsoIdle;
        gestureHello.defaultTransitionTime = 0.5f;

        var gestureStop = stand.Single( "stop gesture", "GESTURE_STOP", false, 1 );
        gestureStop.nextState = torsoIdle;
        gestureStop.defaultTransitionTime = 0.5f;

        PlayTorsoAnimation( "idle" );

        character.altAnimationLayer.player.onModifyAnimation += ApplyKeyboardAnimation;

        character.SetLocomotionFunction( KeyboardRagdollLocomotion );
        character.SetArmatureRetargetFunction( KeyboardArmatureRetarget );

        var armature = player.character.model.armature;
        var camera = player.camera.transform;
        character.biped.rightArmIK.AddRetargeting( delegate( out Vector3 target, out Vector3 pole, out Quaternion handRotation ){
            if( camera == null ){
                target = character.biped.rightHand.target.position;
                pole = armature.position;
                handRotation = character.biped.rightHand.target.rotation;
                return;
            }
            CalculateArmAnim( character.biped.rightArmIK, player, rightGrabAnimatorInfo, out target, out pole, out handRotation );
        }, out rightArmIK, 1 );
        
        character.biped.leftArmIK.AddRetargeting( delegate( out Vector3 target, out Vector3 pole, out Quaternion handRotation ){
            if( camera == null ){
                target = character.biped.leftHand.target.position;
                pole = armature.position;
                handRotation = character.biped.leftHand.target.rotation;
                return;
            }
            CalculateArmAnim( character.biped.leftArmIK, player, leftGrabAnimatorInfo, out target, out pole, out handRotation );

        }, out leftArmIK, 1 );
    }

    private void CalculateArmAnim( ArmIK armIK, VivaPlayer player, GrabNearbyAnimatorInfo grabInfo, out Vector3 target, out Vector3 pole, out Quaternion handRotation ){

        var camera = player.camera.transform;
        var profile = player.character.model.bipedProfile;
        var grabber = grabInfo.grabber;
        target = armIK.shoulder.position;
        target += camera.right*grabber.sign*profile.shoulderWidth*player.character.scale*2.0f;
        target += camera.forward*player.character.biped.rightArmIK.armLength;
        target += camera.up*armAnim*player.character.scale*profile.hipHeight;
        pole = player.character.model.armature.position;
        handRotation = camera.rotation*grabInfo.idleOffset;

        //hand grabbing logic
        if( !grabber.grabbing ){
            var nearbyGrabbable = grabber.GetClosestGrabbable();
            float availableToGrab = System.Convert.ToSingle( nearbyGrabbable );

            float targetAnimGrip = availableToGrab*0.25f+grabInfo.targetGripping*0.8f;
            grabInfo.availableGrabAlpha = Mathf.Clamp01( grabInfo.availableGrabAlpha+( availableToGrab*2-1 )*Time.deltaTime*4.0f );

            if( nearbyGrabbable ){
                var newClosestGrabRot = handRotation*Quaternion.Euler( 0, 0, 45*grabInfo.grabber.sign );
                handRotation = Quaternion.LerpUnclamped( handRotation, newClosestGrabRot, grabInfo.availableGrabAlpha );
            }

            grabber.SetGrip( Mathf.MoveTowards( grabber.grip, targetAnimGrip, Time.deltaTime*8.0f ) );

            // if( armIK.strength.value > 0 ) grabber.fingerAnimator.AnimateGripFingers( grabber.grip, 0, grabber.grip, grabber.grip, grabber.grip );
        }else{
            if( armIK.strength.value > 0 ) grabber.fingerAnimator.AnimateGripFingers( grabber.grip, grabber.grip, grabber.grip, grabber.grip, grabber.grip );
        }
    }

    public void Jump(){
        if( InputManager.cursorActive ) return;
        var legLayer = player.character.altAnimationLayer;
        var jumpAnim = player.character.animationSet.GetBodySet("stand legs")["jump"];
        if( legLayer.IsPlaying( "idle" ) && player.character.biped.surface.HasValue ){
            legLayer.player._InternalPlay( jumpAnim, 0.2f );
            player.character.biped.movementBody.velocity += Vector3.up*VivaPlayer.jumpVelY;
        }
    }

    private void KeyboardArmatureRetarget(){
    }

    private void ApplyKeyboardAnimation(){
        var ragdoll = player.character.biped;
        var movement = player.movement;

        ragdoll.lowerSpine.target.rotation *= Quaternion.Euler( movement.mouseVelSum.y*0.4f, 0, 0f );
        player.character.biped.headLookAt.lookOffset = Quaternion.Inverse( ragdoll.head.target.rotation )*movement.keyboardMouseLook;

        //disable ik if in a special animation
        float allowedToIK = player.character.mainAnimationLayer.player.SampleCurve( BipedRagdoll.ikID, 0 );
        player.character.biped.rightArmIK.strength.Add( "kb controls", allowedToIK );
        player.character.biped.leftArmIK.strength.Add( "kb controls", allowedToIK );
    }

    public Interaction FindAvailableInteraction(){
        if( player.character.biped.rightHandGrabber.contextCount == 0 ) return null;
        if( player.character.biped.leftHandGrabber.contextCount == 0 ) return null;
        var rightItem = player.character.biped.rightHandGrabber.GetGrabContext(0).grabbable.parentItem;
        if( rightItem == null ) return null;
        var leftItem = player.character.biped.leftHandGrabber.GetGrabContext(0).grabbable.parentItem;
        if( leftItem == null ) return null;

        var interaction = player.character.itemInteractions.FindInteraction( rightItem, leftItem, false );
        interactionInfo = new InteractionInfo();
        interactionInfo.user = player.character;
        if( interaction == null ){
            interaction = player.character.itemInteractions.FindInteraction( leftItem, rightItem, false );
            interactionInfo.rightHandHasBaseItem = false;
            interactionInfo.otherItem = rightItem;
            interactionInfo.baseItem = leftItem;
        }else{
            interactionInfo.rightHandHasBaseItem = true;
            interactionInfo.baseItem = rightItem;
            interactionInfo.otherItem = leftItem;
        }
        return interaction;
    }

    public void PlayAvailableInteraction(){
        if( InputManager.cursorActive ) return;
        if( shiftDown ) return;
        interaction = FindAvailableInteraction();
        if( interaction != null ){
            var task = interaction.onTask( interactionInfo );
            var playAnimation = interaction.onTask( interactionInfo ) as PlayAnimation;
            playAnimation.transitionTime = 0.5f;
            if( playAnimation == null ) return; //not allowed to play unless its an animation

            playAnimation.onSuccess += delegate{
                CompleteInteraction();
                if( !playAnimation.ForceSkipToNextState() ){
                    PlayTorsoAnimation( "idle", 0.8f );
                }
            };
            var interactionAnim = player.character.autonomy.FindTask( "_interaction_" );
            if( interactionAnim != null ){
                player.character.autonomy.RemoveTask( interactionAnim );
            }

            StopAvailableInteraction();
            playAnimation.Start( VivaScript._internalDefault, "_interaction_" );
            return;
        }
    }

    public void StopAvailableInteraction(){
        var interactionAnim = player.character.autonomy.FindTask( "_interaction_" );
        if( interactionAnim != null ){
            interactionAnim.Fail("Stopped with keyboard controls");
            PlayTorsoAnimation( "idle", 0.8f );
        }
    }
    public void CompleteInteraction(){
        if( interaction != null ){
            interaction.AttemptComplete( interactionInfo );
            interaction = null;
        }
    }

    private float oldCamPosY = 0;
    private void SetFinalCameraTranform(){
        var camera = player.camera;
        var head = player.character.biped.head.target;
        var newCamPos = head.transform.position+head.transform.rotation*player.character.model.bipedProfile.localHeadEyeCenter;
        if( Mathf.Abs( oldCamPosY-newCamPos.y ) > heightSmooth ){
            oldCamPosY = newCamPos.y+Mathf.Clamp( oldCamPosY-newCamPos.y, -heightSmooth, heightSmooth );
        }else{
            oldCamPosY = Mathf.LerpUnclamped( oldCamPosY, newCamPos.y, Time.deltaTime );
        }
        newCamPos.y = oldCamPosY;

        camera.transform.position = newCamPos;
        camera.transform.rotation = player.movement.keyboardMouseLook;
    }
    
    public void KeyboardRagdollLocomotion(){
        
        float targetForward;
        if( InputManager.cursorActive ){
            targetForward = 0;
        }else{
            var legLayer = player.character.altAnimationLayer;
            var HasTagOrNotBusy = legLayer.IsPlaying( "idle" ) || legLayer.IsPlaying( "crouch" );
            targetForward = player.movement.leftShiftDown ? Mathf.Abs( player.movement.movementDir.z ): Mathf.Abs( player.movement.movementDir.z/2 );
            if( HasTagOrNotBusy ){
                legLayer.player.context.speed.Add( "kb controls", player.movement.movementDir.z >= 0 ? 1 : -1 );
            }else{
                legLayer.player.context.speed.Remove( "kb controls" );
            }
            if( legLayer.player.currentState != null && HasTagOrNotBusy ) legLayer.player.currentState.deltaPosition += Vector3.right*player.movement.movementDir.x*0.0125f;

            var movement = player.movement;
            movement.mouseVelSum -= Viva.input.mouseVelocity*Time.deltaTime*6.0f*enableLook.value;
            movement.mouseVelSum.y = Mathf.Clamp( movement.mouseVelSum.y, -80f, 80f );

            movement.keyboardMouseLook = Quaternion.Euler( movement.mouseVelSum.y, -movement.mouseVelSum.x, 0.0f );
            movement.keyboardMouseLook = Quaternion.RotateTowards( player.character.model.armature.rotation, movement.keyboardMouseLook, 80.0f );
            
            player.character.biped.transform.rotation = Quaternion.Euler( 0, -movement.mouseVelSum.x, 0 );
        }
        var currentForward = player.character.locomotionForward.position;
		player.character.locomotionForward.SetPosition( Mathf.MoveTowards( currentForward, targetForward, Time.deltaTime*2f ) );

        player.character.DefaultLocomotion();
    }

    public override void InitializeInstanceTransform( VivaInstance instance ){

        var camera = VivaPlayer.user.camera.transform;
        float? approxFloorY = instance.CalculateApproximateFloorY();
        float aboveGroundPad = approxFloorY.HasValue ? instance.transform.position.y-approxFloorY.Value : 0;
        const float maxDistance = 8.0f;
        float distance;
        if( Physics.Raycast( camera.position, camera.forward, out WorldUtil.hitInfo, maxDistance, WorldUtil.defaultMask, QueryTriggerInteraction.Ignore ) ){
            distance = WorldUtil.hitInfo.distance;
        }else{
            float? approxRadius = instance.CalculateApproximateRadius();
            distance = approxRadius.HasValue ? Mathf.Max( maxDistance, approxRadius.Value+0.25f ) : 1.0f;
        }
        Vector3 spawnPos = camera.position+camera.forward*distance;
        spawnPos += Vector3.up*aboveGroundPad;

        instance.transform.position = spawnPos;
        instance.transform.rotation = Quaternion.LookRotation( Tools.FlatForward( -camera.forward ), Vector3.up );
    }
}

}