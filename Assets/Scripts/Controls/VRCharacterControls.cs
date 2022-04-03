using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.Management;
using UnityEngine.XR;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem;
using UnityEngine.XR.OpenXR.Features;



namespace viva{

public partial class VRCharacterControls : CameraControls{
    
    public static Quaternion controllerHandOffset = Quaternion.Euler( 0, 0, 180 );

    private enum AirMode{
        JUMPED,
        IN_AIR,
        WAITING_TO_LAND,
        ON_GROUND
    }

    private class Footstep{
        
        public Vector3 target;
        public Vector3 pole;
        public Quaternion footRotation;
        private float lastStepDistance = 0;
        private Vector3 groundNormal = Vector3.up;
        private Vector3 lastHitHipsPos;
        private Vector3 lastGroundNormal;

        public void RecalculateFootstep( Transform foot, VivaPlayer player, float footSign ){
            var profile = player.character.model.bipedProfile;
            var modelStepHeight = player.character.model.bipedProfile.footHeight*player.character.scale;
            var hips = player.character.biped.hips.target;
            Vector3 hipForward = Tools.FlatForward( hips.forward );
            float stepHeight = profile.hipHeight*player.character.scale+modelStepHeight;
            float delta = Time.deltaTime*8.0f;
            float angleDelta = Time.deltaTime*8.0f;
            
            target = foot.position+Vector3.up*profile.hipHeight*player.character.scale;
            target -= player.character.model.armature.forward*profile.hipHeight*player.character.scale*0.1f;
            Vector3 projForward;
            if( Physics.Raycast( target, Vector3.down, out WorldUtil.hitInfo, stepHeight, WorldUtil.defaultMask, QueryTriggerInteraction.Ignore ) ){
                if( WorldUtil.hitInfo.distance < lastStepDistance ){
                    lastStepDistance = WorldUtil.hitInfo.distance;
                }else{
                    lastStepDistance = Mathf.MoveTowards( lastStepDistance, WorldUtil.hitInfo.distance, delta );
                }
                groundNormal = Vector3.MoveTowards( groundNormal, WorldUtil.hitInfo.normal, angleDelta );
                projForward = Vector3.ProjectOnPlane( hipForward, WorldUtil.hitInfo.normal );

                lastHitHipsPos = player.character.biped.hips.rigidBody.position;
            }else{
                if( Vector3.SqrMagnitude( lastHitHipsPos-player.character.biped.hips.rigidBody.position ) > 0.002f ){
                    lastStepDistance = Mathf.MoveTowards( lastStepDistance, stepHeight, delta );
                    groundNormal = Vector3.MoveTowards( groundNormal, Vector3.up, angleDelta );
                }
                projForward = hipForward;
            }
            target += Vector3.down*lastStepDistance+groundNormal*modelStepHeight;

            pole = target+groundNormal+hipForward;
            var constrainPlanePos = hips.position-hips.right*profile.footCenterDistance*player.character.scale*footSign;
            var footHipDelta = pole-constrainPlanePos;
            if( Mathf.Sign( -Vector3.Dot( hips.right, footHipDelta ) ) != footSign ){
                pole = constrainPlanePos+Vector3.ProjectOnPlane( footHipDelta, hips.right );
            }
            footRotation = Quaternion.LookRotation( projForward, groundNormal )*player.character.model.bipedProfile.footForwardOffset;
        }
    }

    [SerializeField]
    private TrackedPoseDriver m_headTPD;
    public TrackedPoseDriver headTPD { get{ return m_headTPD;} }
    [SerializeField]
    private TrackedPoseDriver m_rightHandTPD;
    public TrackedPoseDriver rightHandTPD { get{ return m_rightHandTPD;} }
    [SerializeField]
    private TrackedPoseDriver m_leftHandTPD;
    public TrackedPoseDriver leftHandTPD { get{ return m_leftHandTPD;} }
    [Range(0,1)]
    [SerializeField]
    private float m_playerFriction = 0.5f;
    public float playerFriction { get{ return m_playerFriction; } }
    [Range(0,1)]
    [SerializeField]
    private float standSmoothing = 0.5f;
    [SerializeField]
    private bool m_rightHandedness = true;
    public bool rightHandedness { get{ return m_rightHandedness; } }
    [SerializeField]
    private float crouchSpinePitch = -90.0f;
    [SerializeField]
    private Vector3 defaultModelEyeOffset;
    [SerializeField]
    private float maxMoveSpeed = 1f;
    [SerializeField]
    private float moveAccel = 0.5f;
    [Range(0,1)]
    [SerializeField]
    private float moveFriction = 0.9f;
    [Range(0,1)]
    [SerializeField]
    private float stationaryFriction = 0.3f;
    [SerializeField]
    private Vector3 handOffset = Vector3.zero;
    [SerializeField]
    private PauseBook pauseBook;

    private Footstep rightFootstep = new Footstep();
    private Footstep leftFootstep = new Footstep();
    public bool? currentRightHandBindings { get; private set;}
    private Vector3 cachedAnimHeadPos;
    private IKHandle rightHandIKHandle;
    private IKHandle leftHandIKHandle;
    private List<Model.PoseInfo> defaultPose = new List<Model.PoseInfo>();
    private AirMode air;
    private float oldRagdollAngularDrag;
    private float layDownPercent;


    public void Start(){
        StartCoroutine( DisplayDelayedOptions() );
    }

    private IEnumerator DisplayDelayedOptions(){
        yield return new WaitForSeconds(1.0f);
        if( VRSettings.firstVR == 0 ){
            VRSettings.firstVR++;
            if( GameUI.main ){
                GameUI.main.OpenTab( "VR Settings" );
                GameUI.main.vrSettings.BeginCalibrateVRBody();
            }
        }
        if( UI.main != null ) UI.main.RepositionForVR();
    }

    public void SetControlHandedness( bool _rightHandedness ){
        UnbindVRControls();
        m_rightHandedness = _rightHandedness;
        BindVRControls( m_rightHandedness );
    }

    protected override void BindCharacter( Character character ){
        if( character == null ) return;
        SetupRagdollForVRIKRig( character.biped );
        BindVRControls( rightHandedness );
        character.onReset += OnCharacterReset;
        character.onRagdollChange += OnRagdollChange;
        SetupDefaultAnimPose();
        BindGrabUtilities( character );
        player.gestures.BindToTrackedHands( character.biped.rightHand.target, character.biped.leftHand.target, true );

        player.character.SetBipedAnimationLayers(
            new BipedMask[]{ BipedMask.ALL },
            new BipedMask[]{ BipedMask.SPINE_CHAIN|BipedMask.LEG_CHAIN_R|BipedMask.LEG_CHAIN_L },
            0,
            0
        );

        transform.SetParent( character.biped.transform, true );
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    protected override void UnbindCharacter( Character character ){
        if( character == null ) return;
        RestoreRagdoll( character.biped );
        UnbindVRControls();
        character.onReset -= OnCharacterReset;
        character.onRagdollChange -= OnRagdollChange;
        UnbindGrabUtilities( character );
        player.gestures.UnbindFromTrackedHands();
        transform.SetParent( null, true );

        character.SetBipedAnimationLayers( null, null, 0, 0 );
    }

    private void OnRagdollChange( Ragdoll oldRagdoll, Ragdoll newRagdoll ){
        if( oldRagdoll ) RestoreRagdoll( oldRagdoll as BipedRagdoll );
        SetupRagdollForVRIKRig( newRagdoll as BipedRagdoll );
    }

    private void SetupDefaultAnimPose(){
        defaultPose = player.character.model.ExtractFromSpawnPose( BipedMask.SPINE_CHAIN|BipedMask.ARM_CHAIN_R|BipedMask.ARM_CHAIN_L );
    }

    private void UnbindVRControls(){
        if( !currentRightHandBindings.HasValue ) return;
        Viva.input.actions.vrControls.rightGrip.performed -= HandleRightGrip;
        Viva.input.actions.vrControls.rightGrip.canceled -= HandleRightGripCancel;
        Viva.input.actions.vrControls.leftGrip.performed -= HandleLeftGrip;
        Viva.input.actions.vrControls.leftGrip.canceled -= HandleLeftGripCancel;
        Viva.input.actions.vrControls.leftButtonB.performed -= Jump;
        Viva.input.actions.vrControls.rightButtonB.performed -= Jump;
        if( currentRightHandBindings.Value ){
            Viva.input.actions.vrControls.rightButtonA.performed -= HandleToggleUIMenu;
            Viva.input.actions.vrControls.leftAnalogPos.performed -= HandleMovement;
            Viva.input.actions.vrControls.leftAnalogPos.canceled -= HandleMovement;
            Viva.input.actions.vrControls.rightAnalogPos.performed -= HandleTurning;
            Viva.input.actions.vrControls.rightAnalogPos.canceled -= HandleTurning;
        }else{
            Viva.input.actions.vrControls.leftButtonA.performed -= HandleToggleUIMenu;
            Viva.input.actions.vrControls.rightAnalogPos.performed -= HandleMovement;
            Viva.input.actions.vrControls.rightAnalogPos.canceled -= HandleMovement;
            Viva.input.actions.vrControls.leftAnalogPos.performed -= HandleTurning;
            Viva.input.actions.vrControls.leftAnalogPos.canceled -= HandleTurning;
        }
        currentRightHandBindings = null;
    }

    private void BindVRControls( bool rightHanded ){
        UnbindVRControls();
        Viva.input.actions.vrControls.rightGrip.performed += HandleRightGrip;
        Viva.input.actions.vrControls.rightGrip.canceled += HandleRightGripCancel;
        Viva.input.actions.vrControls.leftGrip.performed += HandleLeftGrip;
        Viva.input.actions.vrControls.leftGrip.canceled += HandleLeftGripCancel;
        Viva.input.actions.vrControls.leftButtonB.performed += Jump;
        Viva.input.actions.vrControls.rightButtonB.performed += Jump;
        if( rightHanded ){
            Viva.input.actions.vrControls.rightButtonA.performed += HandleToggleUIMenu;
            Viva.input.actions.vrControls.leftAnalogPos.performed += HandleMovement;
            Viva.input.actions.vrControls.leftAnalogPos.canceled += HandleMovement;
            Viva.input.actions.vrControls.rightAnalogPos.performed += HandleTurning;
            Viva.input.actions.vrControls.rightAnalogPos.canceled += HandleTurning;
        }else{
            Viva.input.actions.vrControls.leftButtonA.performed += HandleToggleUIMenu;
            Viva.input.actions.vrControls.rightAnalogPos.performed += HandleMovement;
            Viva.input.actions.vrControls.rightAnalogPos.canceled += HandleMovement;
            Viva.input.actions.vrControls.leftAnalogPos.performed += HandleTurning;
            Viva.input.actions.vrControls.leftAnalogPos.canceled += HandleTurning;
        }
        currentRightHandBindings = rightHanded;
    }

    private void HandleRightGrip( InputAction.CallbackContext ctx ){
        UpdateGrip( player.character.biped.rightHandGrabber, ctx.ReadValue<float>() );
    }

    private void HandleRightGripCancel( InputAction.CallbackContext ctx ){
        UpdateGrip( player.character.biped.rightHandGrabber, 0 );
    }

    private void HandleLeftGrip( InputAction.CallbackContext ctx ){
        UpdateGrip( player.character.biped.leftHandGrabber, ctx.ReadValue<float>() );
    }
    
    private void HandleLeftGripCancel( InputAction.CallbackContext ctx ){
        UpdateGrip( player.character.biped.leftHandGrabber, 0 );
    }

    private void UpdateGrip( Grabber grabber, float value ){
        grabber.SetGrip( value );
        grabber.fingerAnimator.AnimateGripFingers( grabber.grip, 0, grabber.grip, grabber.grip, grabber.grip );
        if( value >= 0.8f ){
            var grabbable = grabber.GetClosestGrabbable();
            if( grabbable ){
                grabber.Grab( grabbable );
            }else{
                player.gestures.StartPointing( grabber.transform, Vector3.up );
            }
        }else if( value <= 0.2f ){
            if( grabber.grabbing ){
                grabber.ReleaseAll();
                grabber.fingerAnimator.AnimateGripFingers( grabber.grip, 0, grabber.grip, grabber.grip, grabber.grip );
            }else{
                player.gestures.StopPointing();
            }
        }
    }

    private int menuFlip=1;

    private void HandleToggleUIMenu( InputAction.CallbackContext ctx ){

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

    private void HandleMovement( InputAction.CallbackContext ctx ){
        player.movement.HandleAnalogMovement( ctx.ReadValue<Vector2>() );
    }
    private void HandleTurning( InputAction.CallbackContext ctx ){
        var vec2 = ctx.ReadValue<Vector2>();
        player.movement.HandleAnalogTurning( vec2 );

        if( GameUI.main ){
            GameUI.main.libraryExplorer.scrollbar.value += vec2.y*0.1f;
        }
    }
    
    private void OnEnable(){
        EnableVR();
        Viva.input.actions.vrControls.Enable();
    }

    public override bool Allowed(){ //return false;
        return EnableVR();
    }
    
    private void OnDisable(){
        DisableVR();
    }

    private Vector3 GetArmPole( Vector3 worldPos, float sign ){
        var refBone = player.character.biped.hips.target;
        worldPos = refBone.InverseTransformPoint( worldPos );
        float zWeight = Mathf.Clamp01( worldPos.z/Mathf.Abs( worldPos.x ) );
        return refBone.position-refBone.right*sign-refBone.forward*0.25f+refBone.up*zWeight;
    }

    private void RestoreRagdoll( BipedRagdoll ragdoll ){

        ragdoll.SetMusclePinMode( false );

        ragdoll.rightLeg.rigidBody.gameObject.SetActive( true );
        ragdoll.leftLeg.rigidBody.gameObject.SetActive( true );

        ragdoll.rightUpperLeg.RestoreTransform();
        ragdoll.leftUpperLeg.RestoreTransform();
        ragdoll.rightLeg.RestoreTransform();
        ragdoll.leftLeg.RestoreTransform();
        ragdoll.rightFoot.RestoreTransform();
        ragdoll.leftFoot.RestoreTransform();
        ragdoll.hips.RestoreTransform();
        ragdoll.lowerSpine.RestoreTransform();
        
        rightHandIKHandle?.Kill();
        leftHandIKHandle?.Kill();

        ragdoll.movementBody.interpolation = RigidbodyInterpolation.None;
        foreach( var muscle in ragdoll.muscles ){
            muscle.rigidBody.interpolation = RigidbodyInterpolation.None;
        }
    }

    public void Jump( InputAction.CallbackContext ctx ){
        if( player.character.altAnimationLayer.IsPlaying( "idle" ) && player.character.biped.surface.HasValue ){
            player.character.altAnimationLayer.player._InternalPlay( player.character.animationSet.GetBodySet("stand legs")["jump"], 0.1f );
        }
    }

    public void SetupRagdollForVRIKRig( BipedRagdoll ragdoll ){

        ragdoll.rightLeg.rigidBody.gameObject.SetActive( false );
        ragdoll.leftLeg.rigidBody.gameObject.SetActive( false );
        cachedAnimHeadPos = ragdoll.head.target.position-player.character.model.armature.position;

        ragdoll.SetMusclePinMode( true );

        ragdoll.movementBody.interpolation = RigidbodyInterpolation.Interpolate;
        foreach( var muscle in player.character.biped.muscles ){
            muscle.rigidBody.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    private void VRArmatureRetarget(){

        var armature = player.character.model.armature;
        var ragdoll = player.character.biped;

        // var targetRotation = Quaternion.LookRotation( Tools.FlatForward( headTPD.transform.forward ), Vector3.up );
        // if( player.character.locomotionForward.position > 0.01f ){
        //     player.character.model.armature.rotation = targetRotation;
        // }else{
        //     float angle = Quaternion.Angle( player.character.model.armature.rotation, targetRotation );
        //     player.character.model.armature.rotation = Quaternion.Lerp( player.character.model.armature.rotation, targetRotation, Time.deltaTime*angle/15.0f );
        // }

        player.character.biped.head.target.rotation = headTPD.transform.rotation;
    }

    private Vector3 GetEyeWorldPosition(){
        return headTPD.transform.TransformPoint( defaultModelEyeOffset );
    }

    private void AnimateForVR(){ 
        foreach( var poseInfo in defaultPose ){
            poseInfo.target.localRotation = poseInfo.localRot;
        }

        var armature = player.character.model.armature;
        var ragdoll = player.character.biped;

        var targetRotation = Quaternion.LookRotation( Tools.FlatForward( headTPD.transform.forward ), Vector3.up );
        if( player.character.locomotionForward.position > 0.01f ){
            player.character.model.armature.rotation = targetRotation;
        }else{
            float angle = Quaternion.Angle( player.character.model.armature.rotation, targetRotation );
            if( angle < 90 ){
                player.character.model.armature.rotation = Quaternion.Lerp( player.character.model.armature.rotation, targetRotation, Time.deltaTime*angle/15.0f );
            }else{
                player.character.model.armature.rotation = targetRotation;
            }
        }

        //animate hips to the same position
        ragdoll.hips.target.localPosition = Vector3.up*player.character.model.bipedProfile.hipHeight;
        
        //crouch based on height of HeadTPD
        var localHeadY = Mathf.Max( 0.1f, GetEyeWorldPosition().y-transform.position.y );
        layDownPercent = Tools.RemapClamped( 0.2f, 0.4f, 1f, 0f, localHeadY );
        var localArmatureHeadY = player.character.model.bipedProfile.floorToHeadHeight*player.character.scale;
        
        var hipsPos = ragdoll.hips.target.position;
        hipsPos += Vector3.up*( localHeadY-localArmatureHeadY );
        ragdoll.hips.target.position = hipsPos;

        var oldHeadHeight = ragdoll.head.target.position.y;
        float crouchHipYOffset = player.character.model.bipedProfile.hipHeight*player.character.scale*0.5f;
        float percentCrouch =  1.0f-Mathf.Clamp01( ( localHeadY-crouchHipYOffset )/( localArmatureHeadY-crouchHipYOffset ) );
        var crouchRotation = Quaternion.Euler( percentCrouch*crouchSpinePitch*( 1f-layDownPercent ), 0, 0 );
        ragdoll.hips.target.localRotation *= Quaternion.Euler( -90f*layDownPercent, 0, 0 );
        ragdoll.upperSpine.target.localRotation *= crouchRotation;
        ragdoll.lowerSpine.target.localRotation *= crouchRotation;

        var localHeadPos = ragdoll.head.target.position-player.character.model.armature.position;
        var localHeadDelta = cachedAnimHeadPos-localHeadPos;
        cachedAnimHeadPos = localHeadPos;

        ragdoll.hips.target.position += Vector3.up*( oldHeadHeight-ragdoll.head.target.position.y );

        ragdoll.head.target.rotation = headTPD.transform.rotation;

        var rigOffsetXZ = ragdoll.head.target.position-GetEyeWorldPosition();
        rigOffsetXZ.y = 0;

        ragdoll.hips.target.position -= rigOffsetXZ;
    }

    protected void CheckIfJumped( int animationLayerIndex ){
        if( animationLayerIndex != player.character.altAnimationLayerIndex ) return;
        if( player.character.mainAnimationLayer.IsPlaying("jump") ){
            air = AirMode.JUMPED;
            player.character.biped.movementBody.velocity += Vector3.up*VivaPlayer.jumpVelY;
        }
    }

    private void OnCharacterReset(){

        player.character.biped.DisableHandGrabAnimations();
        player.character.altAnimationLayer.player.onAnimationChange += CheckIfJumped;
        player.character.mainAnimationLayer.player.onModifyAnimation += AnimateForVR;

        player.character.SetLocomotionFunction( VRLocomotion );
        player.character.SetArmatureRetargetFunction( VRArmatureRetarget );
        var ragdoll = player.character.biped;

        rightHandIKHandle?.Kill();
        leftHandIKHandle?.Kill();
		player.character.biped.rightArmIK.AddRetargeting( delegate( out Vector3 target, out Vector3 pole, out Quaternion handRotation ){
            if( !rightHandTPD || !player.character ){
                target = Vector3.zero;
                pole = Vector3.zero;
                handRotation = Quaternion.identity;
                return;
            }
            handRotation = rightHandTPD.transform.rotation*controllerHandOffset;
            target = GetArmTarget( rightHandTPD.transform, -1f );
            pole = GetArmPole( target, 1.0f );
        }, out rightHandIKHandle );

		player.character.biped.leftArmIK.AddRetargeting( delegate( out Vector3 target, out Vector3 pole, out Quaternion handRotation ){
            if( !leftHandTPD || !player.character ){
                target = Vector3.zero;
                pole = Vector3.zero;
                handRotation = Quaternion.identity;
                return;
            }
            handRotation = leftHandTPD.transform.rotation*controllerHandOffset;
            target = GetArmTarget( leftHandTPD.transform, 1f );
            pole = GetArmPole( target, -1.0f );
        }, out leftHandIKHandle );
        
        var rightLegIK = LegIK.CreateLegIK( player.character.model.bipedProfile, true );
        rightLegIK.AddRetargeting( delegate( out Vector3 target, out Vector3 pole, out Quaternion footRotation ){
            rightFootstep.RecalculateFootstep(
                ragdoll.rightFoot.target,
                player,
                1.0f
            );
            target = rightFootstep.target;
            pole = rightFootstep.pole;
            footRotation = rightFootstep.footRotation;
        }, out IKHandle rightLegIKHandle );

        var leftLegIK = LegIK.CreateLegIK( player.character.model.bipedProfile, false );
        leftLegIK.AddRetargeting( delegate( out Vector3 target, out Vector3 pole, out Quaternion footRotation ){
            leftFootstep.RecalculateFootstep(
                ragdoll.leftFoot.target,
                player,
                -1.0f
            );
            target = leftFootstep.target;
            pole = leftFootstep.pole;
            footRotation = leftFootstep.footRotation;
        }, out IKHandle leftLegIKHandle );

        player.character.altAnimationLayer.player.onModifyAnimation += delegate{
            rightLegIK.Apply();
            leftLegIK.Apply();
        };
    }

    private Vector3 GetArmTarget( Transform handTransform, float sign ){
        var offset = handOffset;
        offset.x *= sign;
        return handTransform.position+handTransform.TransformDirection( offset )*transform.lossyScale.y;
    }

    private bool EnableVR(){
        if( XRGeneralSettings.Instance.Manager.activeLoader == null ){
            Debug.Log("#VR Enabled "+(XRGeneralSettings.Instance.Manager.activeLoader == null));
            XRGeneralSettings.Instance.Manager.InitializeLoaderSync();
            
            if( XRGeneralSettings.Instance.Manager.activeLoader == null ){
                Debug.LogError("Failed to initialize VR");
                return false;
            }else{
                XRGeneralSettings.Instance.Manager.StartSubsystems();
            }
        }
        headTPD.enabled = true;
        headTPD.positionAction = Viva.input.actions.vrControls.centerEyePos;
        headTPD.rotationAction = Viva.input.actions.vrControls.centerEyeRot;
        rightHandTPD.positionAction = Viva.input.actions.vrControls.rightHandPos;
        rightHandTPD.rotationAction = Viva.input.actions.vrControls.rightHandRot;
        leftHandTPD.positionAction = Viva.input.actions.vrControls.leftHandPos;
        leftHandTPD.rotationAction = Viva.input.actions.vrControls.leftHandRot;
        rightHandTPD.enabled = true;
        leftHandTPD.enabled = true;
        return true;
    }

    private void DisableVR(){
        //skip if was never intialized
        if( XRGeneralSettings.Instance.Manager.isInitializationComplete ){
            Debug.LogError("#VR Disabled "+XRGeneralSettings.Instance.Manager.isInitializationComplete);
            XRGeneralSettings.Instance.Manager.StopSubsystems();
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        }
        headTPD.enabled = false;
        rightHandTPD.enabled = false;
        leftHandTPD.enabled = false;
    }

    private void VRLocomotion(){

        player.character.locomotionForward.SetPosition( player.movement.movementDir.z );
        float padding = 0.001f;
        float standPadding = 0;//player.character.model.profile.hipHeight*player.character.scale*0.1f;
        float sphereRadius = 0.08f;
        var ragdoll = player.character.biped;
        var upperSpine = ragdoll.upperSpine;
        var upperSpinePhysicsPos = upperSpine.rigidBody.position;
        float standHeight = upperSpine.target.position.y-player.character.model.armature.position.y;
        float standDistance = standHeight+padding-sphereRadius*2;

        var touchingFloor = Physics.SphereCast(
                    upperSpinePhysicsPos+Vector3.down*sphereRadius,
                    sphereRadius,
                    Vector3.down,
                    out WorldUtil.hitInfo,
                    standDistance+standPadding,
                    WorldUtil.defaultMask|WorldUtil.waterMask,
                    QueryTriggerInteraction.Ignore
                ) && Vector3.Dot( WorldUtil.hitInfo.normal, Vector3.up ) > 0.6f;

        if( touchingFloor || (player.character.biped.surface.HasValue && player.character.biped.isOnWater ) ){
            if( air <= AirMode.IN_AIR ){
                if( air == AirMode.JUMPED ) HandleWorldHitInfoStandingForce();
                if( upperSpine.rigidBody.velocity.y <= 0 ){
                    air = AirMode.WAITING_TO_LAND;
                }
            }else if( air == AirMode.WAITING_TO_LAND ){
                air = AirMode.ON_GROUND;
                player.character.altAnimationLayer.player._InternalPlay( player.character.altAnimationLayer.currentBodySet["idle"] );
                HandleWorldHitInfoStandingForce();
            }
            if( air == AirMode.ON_GROUND ){
                if( !player.character.biped.isOnWater ){  //stand
                    if( WorldUtil.hitInfo.distance < standDistance ){
                        float distanceDelta = standHeight-( WorldUtil.hitInfo.distance+sphereRadius*2 );
                        if( distanceDelta >= 0 ){
                            float targetStandY = upperSpinePhysicsPos.y+Mathf.Max( 0, distanceDelta );

                            var offset = ( targetStandY-upperSpinePhysicsPos.y )*standSmoothing;
                            foreach( var muscle in player.character.biped.muscles ){
                                var pos = muscle.rigidBody.position;
                                pos.y += offset;
                                // muscle.rigidBody.MovePosition( pos );

                                // var vel = muscle.rigidBody.velocity;
                                // vel.y = 0;
                                // muscle.rigidBody.velocity = vel;
                            }
                        }
                    }
                }
                // if( WorldUtil.hitInfo.rigidbody ){
                //     movementVel += WorldUtil.hitInfo.rigidbody.velocity;
                // }
            }
        }else{
            if( air == AirMode.ON_GROUND && upperSpine.rigidBody.velocity.y <= 0 ) air = AirMode.WAITING_TO_LAND;   //fire falling logic if lost ground pos
        }
        if( !player.character.biped.surface.HasValue ) return;
        var targetVel = headTPD.transform.TransformDirection( player.movement.movementDir );
        float l = Mathf.Min( targetVel.magnitude, 1f );
        l *= l;
        l *= l;
        targetVel.y = 0;
        if( l > 0 ) targetVel = targetVel.normalized*l;
        player.character.ApplyLocomotion( targetVel*moveAccel );
    }

    private void HandleWorldHitInfoStandingForce(){
        if( !player.character.biped.surface.HasValue ){
            return;
        }
        var hips = player.character.biped.upperSpine;
        var otherBody = WorldUtil.hitInfo.rigidbody;
        if( otherBody ){
            var force = ( hips.rigidBody.velocity-otherBody.velocity )*hips.rigidBody.mass;
            if( force.y > 0 ) force *= -1;
            otherBody.AddForceAtPosition( force, WorldUtil.hitInfo.point, ForceMode.Impulse );
            var physicsSoundSource = otherBody.GetComponent<PhysicsSoundSource>();
            if( physicsSoundSource ) physicsSoundSource.SimulateContact( WorldUtil.hitInfo.point, hips.rigidBody.velocity.sqrMagnitude );
        }
        player.character.biped.rightFoot.rigidBody.GetComponent<PhysicsSoundSource>().SimulateContact( player.character.biped.surface.Value, hips.rigidBody.velocity.sqrMagnitude );
    }

    public override void InitializeInstanceTransform( VivaInstance instance ){

        var camera = VivaPlayer.user.camera.transform;
        float? approxRadius = instance.CalculateApproximateRadius();
        float? approxFloorY = instance.CalculateApproximateFloorY();
        float distance = approxRadius.HasValue ? Mathf.Max( 1.0f, approxRadius.Value+0.25f ) : 1.0f;
        float aboveGroundPad = approxFloorY.HasValue ? instance.transform.position.y-approxFloorY.Value : 0;

        Vector3 downSamplePos;
        if( Physics.Raycast( camera.position, camera.forward, out WorldUtil.hitInfo, distance, WorldUtil.defaultMask, QueryTriggerInteraction.Ignore ) ){
            downSamplePos = WorldUtil.hitInfo.point+WorldUtil.hitInfo.normal*approxRadius.Value;
        }else{
            downSamplePos = camera.position+camera.forward*distance;
        }

        Vector3 spawnPos;
        if( Physics.Raycast( downSamplePos, Vector3.down, out WorldUtil.hitInfo, 3.0f, WorldUtil.defaultMask, QueryTriggerInteraction.Ignore ) ){
            spawnPos = WorldUtil.hitInfo.point;
        }else{
            spawnPos = downSamplePos+Vector3.down*distance;
        }
        spawnPos += Vector3.up*aboveGroundPad;

        instance.transform.position = spawnPos;
        instance.transform.rotation = Quaternion.LookRotation( Tools.FlatForward( -camera.forward ), Vector3.up );
    }
}

}