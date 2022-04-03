using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



namespace viva{


/// <summary>
/// The class used to represent 16-bone physics ragdolls. You can control the strength of each animated muscle or the ragdoll as a whole with
/// muscleLimit and pinLimit.
/// </summary>
public partial class BipedRagdoll: Ragdoll{
    
    public static readonly int emotionID = "emotion".GetHashCode();
    public static readonly int eyesID = "eyes".GetHashCode();
    public static readonly int ikID = "ik".GetHashCode();
    public static readonly int headID = "head".GetHashCode();
    public static readonly int rightArmID = "right arm".GetHashCode();
    public static readonly int leftArmID = "left arm".GetHashCode();
    private static readonly int radiusID = Shader.PropertyToID("_Radius");


    public class ArmObstacleFix{
        private Character self;
        public IKHandle handle;
        public ArmIK armIK;
        private Grabber grabber;
        private float lastCheck;
        private float minY = 0;
        private float timeout = 0.0f;

        public ArmObstacleFix( Character _self, ArmIK _armIK, Grabber _grabber ){
            self = _self;
            armIK = _armIK;
            grabber = _grabber;
        }

        public void Check(){
            if( Time.time-lastCheck < 0.125f ) return;
            lastCheck = Time.time;
            var pos = grabber.rigidBody.worldCenterOfMass;
			if( Physics.Raycast( pos, Vector3.down, out WorldUtil.hitInfo, grabber.width*4.0f, WorldUtil.defaultMask, QueryTriggerInteraction.Ignore ) ){
				minY = WorldUtil.hitInfo.point.y+grabber.width*2.0f;
				Tools.DrawDiagCross( WorldUtil.hitInfo.point, Color.cyan, 0.1f, Time.fixedDeltaTime*2 );
                timeout = 1.1f;
                
                if( handle == null ){
                    armIK.AddRetargeting(
                        delegate( out Vector3 target, out Vector3 pole, out Quaternion handRotation ){
                            target = armIK.hand.position;
                            target.y = Mathf.Max( target.y, minY );

                            pole = self.model.armature.TransformPoint( ( new Vector3( self.model.bipedProfile.armLength*grabber.sign, 0, -self.model.bipedProfile.armLength ) ) );
                            handRotation = armIK.hand.rotation;

                            timeout = Mathf.Max( 0, timeout-Time.deltaTime);
                            handle.maxWeight = timeout*0.95f;
                        }
                        ,out handle,
                        -1
                    );
                }
			}
        }
    }

    [SerializeField]
    private MeshCollider visionCollider;
    [SerializeField]
    private SphereCollider spacing;

	/// <summary> The class controlling where the character's head points at.
    public HeadLookAt headLookAt { get; private set; }
	/// <summary> The class controlling where the character's eyes look at.
    public EyeLookAt eyeLookAt { get; private set; }
    /// <summary> The hip's muscle.
    public Muscle hips { get{ return muscles[0]; } }
    /// <summary> The lower spine's muscle.
    public Muscle lowerSpine { get{ return muscles[1]; } }
    /// <summary> The upper spine's muscle.
    public Muscle upperSpine { get{ return muscles[2]; } }
    /// <summary> The head's muscle.
    public Muscle head { get{ return muscles[3]; } }
    /// <summary> The right upper leg muscle.
    public Muscle rightUpperLeg { get{ return muscles[4]; } }
    /// <summary> The left upper leg muscle.
    public Muscle leftUpperLeg { get{ return muscles[5]; } }
    /// <summary> The right leg muscle.
    public Muscle rightLeg { get{ return muscles[6]; } }
    /// <summary> The left leg muscle.
    public Muscle leftLeg { get{ return muscles[7]; } }
    /// <summary> The right foot's muscle.
    public Muscle rightFoot { get{ return muscles[8]; } }
    /// <summary> The left foot's muscle.
    public Muscle leftFoot { get{ return muscles[9]; } } 
    /// <summary> The right upper arm muscle.
    public Muscle rightUpperArm { get{ return muscles[10]; } }
    /// <summary> The left upper arm muscle.
    public Muscle leftUpperArm { get{ return muscles[11]; } }
    /// <summary> The right upper arm muscle.
    public Muscle rightArm { get{ return muscles[12]; } }
    /// <summary> The left upper arm muscle.
    public Muscle leftArm { get{ return muscles[13]; } }
    /// <summary> The right hand's muscle.
    public Muscle rightHand { get{ return muscles[14]; } } 
    /// <summary> The left hand's muscle.
    public Muscle leftHand { get{ return muscles[15]; } }
    public ListenerBipedCollision onCollisionEnter { get; private set; } = new ListenerBipedCollision( "onCollisionEnter" );
    public ListenerBipedCollision onCollisionExit { get; private set; } = new ListenerBipedCollision( "onCollisionExit" );
	/// <summary> The right arm CharacterIK. Use this for dynamically animating the arm.
    public ArmIK rightArmIK;
	/// <summary> The left arm CharacterIK. Use this for dynamically animating the arm.
    public ArmIK leftArmIK;
    private ArmObstacleFix rightArmObstacleFix;
    private ArmObstacleFix leftArmObstacleFix;
	/// <summary> The right hand grabber for grabbing physics objects.
    public Grabber rightHandGrabber { get; private set; }
	/// <summary> The left hand grabber for grabbing physics objects.
    public Grabber leftHandGrabber { get; private set; }
	/// <summary> The system handling when objects are seen
    public Vision vision { get; private set; } = null;
    private List<DynamicBoneBoxCollider> handBoxColliders = new List<DynamicBoneBoxCollider>();
    private IKHandle rightHoldHandle;
    private IKHandle leftHoldHandle;
    private bool hasOutline = false;
    private Material outlineMat;
    private int smileBlendShapeIndex = -1;

    

    public BipedBone? FindBipedBone( Muscle muscle ){
        for( int i=0; i<muscles.Length; i++ ){
            var candidate = muscles[i];
            if( muscle == candidate ) return (BipedBone)i;
        }
        return null;
    }

    private void SetupHandGrabberDetection( Grabber grabber, bool rightSide ){

        var handBoxCollider = rightHand.colliders[0] as BoxCollider;
        grabber.fingerAnimator = new FingerGrabAnimator( grabber, model, rightSide );
        grabber.width = handBoxCollider.size.z;

        var container = new GameObject( "_GRAB_DETECT_" );
        container.layer = WorldUtil.grabbablesLayer;
        container.transform.SetParent( grabber.transform, false );
        grabber.AddColliderDetector( container.AddComponent<ColliderDetector>(), false );

        grabber.detectSphere = container.AddComponent<SphereCollider>();
        float grabSphereSize = model.bipedProfile.hipHeight*grabber.character.scale*0.125f;
        var middleStart = model.bipedProfile[ rightSide ? BipedBone.MIDDLE0_R : BipedBone.MIDDLE0_L ];
        var middleEnd = model.bipedProfile[ rightSide ? BipedBone.MIDDLE2_R : BipedBone.MIDDLE2_L ];
        var hand = model.bipedProfile[ rightSide ? BipedBone.HAND_R : BipedBone.HAND_L ];
        if( middleStart != null && middleEnd != null ){
            container.transform.position = ( middleStart.transform.position+middleEnd.transform.position )/2;
        }else{
            container.transform.position = hand.transform.position;
        }
        container.transform.localPosition = hand.transform.InverseTransformPoint( container.transform.position );
        grabber.detectSphere.radius = grabSphereSize/container.transform.lossyScale.x;
        grabber.detectSphere.isTrigger = true;
    }

    private ArmIK CreateArmIK( bool rightArm, Character character ){

        var profile = model.bipedProfile;
        TwoBoneIK ik;
        Transform shoulder;
        Transform endBone;
        float sign;
        if( rightArm ){
            ik = new TwoBoneIK(
                profile[ BipedBone.UPPER_ARM_R ].transform,
                Quaternion.Euler(-90,180,0),
                profile[ BipedBone.ARM_R ].transform,
                Quaternion.Euler(90,0,0),
                profile[ BipedBone.HAND_R ].transform
            );
            shoulder = profile[ BipedBone.SHOULDER_R ].transform;
            endBone = profile[ BipedBone.HAND_R ].transform;
            sign = 1.0f;
        }else{
            ik = new TwoBoneIK(
                profile[ BipedBone.UPPER_ARM_L ].transform,
                Quaternion.Euler(-90,180,0),
                profile[ BipedBone.ARM_L ].transform,
                Quaternion.Euler(90,0,0),
                profile[ BipedBone.HAND_L ].transform
            );
            shoulder = profile[ BipedBone.SHOULDER_L ].transform;
            endBone = profile[ BipedBone.HAND_L ].transform;
            sign = -1.0f;
        }
        return new ArmIK( character, ik, endBone, profile[ BipedBone.LOWER_SPINE ].transform, sign, shoulder );
    }

    private void ExtractAndApplyGrabOffsets( Muscle handMuscle, Grabber grabber ){
        foreach( var collider in handMuscle.colliders ){
            var bc = collider as BoxCollider;
            if( bc ){
                var thinnest = Mathf.Min( bc.size.x, Mathf.Min( bc.size.y, bc.size.z ) );
                grabber.grabOffset = grabber.transform.InverseTransformPoint( bc.transform.TransformPoint( bc.center ) );
                grabber.grabOffset += Vector3.right*thinnest*grabber.sign/grabber.transform.lossyScale.x*0.625f;

                var dynamicBoneBoxCollider = collider.gameObject.AddComponent<DynamicBoneBoxCollider>();
                dynamicBoneBoxCollider.center = bc.center;
                dynamicBoneBoxCollider.size = bc.size;
                handBoxColliders.Add( dynamicBoneBoxCollider );
            }
        }
    }

    protected override void FixedUpdate(){
        base.FixedUpdate();
        lookTarget._InternalHandleChange();
        rightArmObstacleFix.Check();
        leftArmObstacleFix.Check();

        spacing.transform.position = hips.rigidBody.worldCenterOfMass;
    }
    
    public override void PinToAnimation( AnimationLayer maskedLayer=null ){
        //not allowed to have more pin than muscle

        foreach( var muscleIndex in maskedLayer.bipedMuscleIndices ){
            var muscle = muscles[ muscleIndex ];
            muscle.Read();
            muscle.Pin( pinLimit.value, muscleIndex<4||maskedLayer.character.isPossessed );
            muscle.MuscleRotation( muscleLimit.value, defaultMuscleSpringConstant, defaultMuscleDamperConstant+System.Convert.ToInt32(muscleIndex==0)*40 );   //fixes root motion
        }
    }

    public override void SnapArmatureToPhysics(){
        DebugArmature( Color.red );
        var targetMovementBodyPos = hips.rigidBody.transform.position+( model.armature.position-hips.target.position );

        var oldHipsPos = hips.rigidBody.transform.position;
        var sqDist = Vector3.SqrMagnitude( ( movementBody.transform.position-targetMovementBodyPos )*distanceSnapMult );
        var finalMovementBodyPos = Vector3.Lerp( movementBody.transform.position, targetMovementBodyPos, Mathf.Clamp01(sqDist)*0.5f );
        movementBody.transform.position = finalMovementBodyPos;
        hips.rigidBody.transform.position = oldHipsPos;
        
        var targetHeight = hips.target.position.y-model.armature.position.y;
        var hipPos = hips.rigidBody.transform.position;
        float floorY;
        if( !surface.HasValue ){
            floorY = Mathf.Min( rightFoot.rigidBody.transform.position.y, leftFoot.rigidBody.transform.position.y )-model.bipedProfile.footHeight*model.scale;
            floorY = Mathf.Max( model.armature.position.y, floorY );
            hipPos.y = Mathf.Lerp( hipPos.y, floorY+targetHeight, 0.5f );
        }else{
            var deltaHeight = ( surface.Value.y+targetHeight )-hips.target.position.y;
            floorY = surface.Value.y-deltaHeight*0.5f;
            hipPos.y = floorY+targetHeight;
        }
        capsuleCollider.height = Mathf.Max( ( hipPos.y-floorY )/model.scale, capsuleCollider.radius*2f );
        hipPos.y = floorY+( hipPos.y-floorY )/2f;
        capsuleCollider.center = transform.InverseTransformPoint( hipPos );
    }

    protected override void LateUpdate(){
        base.LateUpdate();
        if( Camera.main ){
            outlineMat.SetFloat( radiusID, 0.0015f/Mathf.Max( 0.001f, model.renderer.transform.lossyScale.y ) );
            if( VivaPlayer.user && VivaPlayer.user.character && VivaPlayer.user.character.ragdoll == this ){
                SetEnableOutlineMaterial( false );
                return;
            }
            SetEnableOutlineMaterial( Vector3.SqrMagnitude( Camera.main.transform.position-movementBody.worldCenterOfMass ) < 16.0f );
        }
    }

    public void SetEnableOutlineMaterial( bool enable ){
        if( enable == hasOutline ) return;
        hasOutline = enable;

        var materials = new List<Material>( model.renderer.sharedMaterials );
        if( enable ){
            if( !materials.Contains( outlineMat ) ) materials.Add( outlineMat );
        }else{
            materials.Remove( outlineMat );
        }
        model.skinnedMeshRenderer.materials = materials.ToArray();
    }

    public override void RetargetArmature(){
        //snap to nearest forward armature look rotation based on physics hips
        var oldRotation = model.armature.rotation;
        var rootTargetLocalRot = Quaternion.Inverse( model.armature.rotation )*root.target.rotation;
        model.armature.rotation = root.rigidBody.transform.rotation*Quaternion.Inverse( rootTargetLocalRot );
        var look = Tools.FlatForward( model.armature.forward );
        if( look != Vector3.up ){
            model.armature.rotation = Quaternion.LookRotation( look, Vector3.up );
        }else{
            model.armature.rotation = oldRotation;
        }
    }

    protected override void OnSetup( Character character ){
        model.armature.rotation = Quaternion.identity;
        model.ApplySpawnPose( false );
        
        //move mesh bounding box inside hips
        var hipsInfo = model.bipedProfile[ BipedBone.HIPS ];
        Vector3 oldHipPos = hipsInfo.transform.position;

        hipsInfo.transform.position = oldHipPos;

        muscles = new Muscle[ BipedProfile.humanMuscles.Length ];
        for( int i=0; i<muscles.Length; i++ ){

            var ragdollBone = BipedProfile.humanMuscles[i];
            var boneInfo = model.bipedProfile[ ragdollBone ];
            boneInfo.rigidBody = rigidBodies[i];
            boneInfo.rigidBody.maxAngularVelocity = 50.0f;  //SteamVR recommends 50
            boneInfo.rigidBody.transform.position = boneInfo.transform.position;
            boneInfo.rigidBody.transform.rotation = boneInfo.transform.rotation;
            boneInfo.rigidBody.velocity = Vector3.zero;
            boneInfo.rigidBody.angularVelocity = Vector3.zero;

            boneInfo.joint = boneInfo.rigidBody.GetComponent<ConfigurableJoint>();
            if( boneInfo.joint.connectedBody != null ){
                boneInfo.joint.connectedAnchor = boneInfo.joint.connectedBody.transform.InverseTransformPoint( boneInfo.transform.position );
            }

            //attach colliders from model
            var boneColliders = FindComponentChildren( boneInfo.transform, WorldUtil.itemsLayer );
            var mirrorVariant = BipedProfile.GetBoneMirrorVariant( ragdollBone );
            if( mirrorVariant.HasValue ){
                var mirrorBoneInfo = model.bipedProfile[ mirrorVariant.Value ];
                var mirrorBoneColliders = FindComponentChildren( mirrorBoneInfo.transform, WorldUtil.itemsLayer );
                bool isLeftSide = ragdollBone.ToString().EndsWith("L");
                if( boneColliders.Length == 0 ){
                    if( mirrorBoneColliders.Length == 0 ){
                        //improvise colliders
                        boneColliders = ImproviseColliders( ragdollBone );
                    }else{
                        boneColliders = MirrorColliderChildren( mirrorBoneColliders, boneInfo.transform );
                    }
                }else if( !isLeftSide ){    //only mirror if currently on a ride side BipedBone (prevent duplicates)
                    if( mirrorBoneColliders.Length == 0 ){
                        MirrorColliderChildren( boneColliders, mirrorBoneInfo.transform );
                    }
                }
            }else if( boneColliders.Length == 0 ){
                boneColliders = ImproviseColliders( ragdollBone );
            }
            if( boneColliders == null || boneColliders.Length == 0 ){
                throw new System.Exception("Model template is incomplete for "+ragdollBone);
            }

            //calculate top of head
            if( ragdollBone == BipedBone.HEAD ){
                float topOfHeadY = boneInfo.rigidBody.transform.position.y;
                var head = boneInfo.transform;
                var headCenter = Vector3.zero;
                foreach( var boneCollider in boneColliders ){
                    topOfHeadY = Mathf.Max( topOfHeadY, boneCollider.bounds.max.y );
                    headCenter += boneCollider.bounds.center;
                }
                headCenter /= boneColliders.Length;

                var eye = model.bipedProfile[ BipedBone.EYEBALL_R ];
                //if eye bone is not present, then use center of head
                if( eye == null || eye.transform == null ){
                    model.bipedProfile.localHeadForeheadY = Mathf.LerpUnclamped( topOfHeadY, head.transform.position.y, 0.4f );
                    model.bipedProfile.localHeadEyeCenter = head.InverseTransformPoint( headCenter )*head.transform.lossyScale.x;
                }else{
                    model.bipedProfile.localHeadForeheadY = Mathf.LerpUnclamped( topOfHeadY, eye.transform.position.y, 0.8f );
                    model.bipedProfile.localHeadEyeCenter = head.transform.InverseTransformPoint( eye.transform.position )*eye.transform.lossyScale.x;
                }
                model.bipedProfile.localHeadEyeCenter.x = 0;

                model.bipedProfile.localHeadForeheadY = head.transform.InverseTransformPoint( Vector3.up*model.bipedProfile.localHeadForeheadY ).y; //localize
            }else if( ragdollBone == BipedBone.UPPER_ARM_R ){
                foreach( var boneCollider in boneColliders ){
                    var cc = boneCollider as CapsuleCollider;
                    if( cc ){
                        model.bipedProfile.armThickness = cc.radius;
                    }
                }
            }
            //extract and reparent colliders
            PhysicMaterial physicMat;
            if( ragdollBone == BipedBone.FOOT_L || ragdollBone == BipedBone.FOOT_R ){
                physicMat = BuiltInAssetManager.main.ragdollFootPhysicMaterial;
            }else if( ragdollBone == BipedBone.HAND_L || ragdollBone == BipedBone.HAND_R || ragdollBone == BipedBone.ARM_L || ragdollBone == BipedBone.ARM_R ){
                physicMat = BuiltInAssetManager.main.ragdollHandPhysicMaterial;
            }else{
                physicMat = BuiltInAssetManager.main.ragdollActivePhysicMaterial;
            }
            Vector3 avgCenter = Vector3.zero;
            foreach( var boneCollider in boneColliders ){
                boneCollider.transform.SetParent( boneInfo.rigidBody.transform, true );
                boneCollider.gameObject.layer = boneInfo.rigidBody.gameObject.layer;    //characterCollisionsLayer
                boneCollider.material = physicMat;
                avgCenter += boneCollider.bounds.center;
            }
            boneInfo._InternalCreateColliderDetector();
            boneInfo.rigidBody.ResetCenterOfMass();
            boneInfo.rigidBody.ResetInertiaTensor();
            boneInfo.rigidBody.centerOfMass = boneInfo.rigidBody.centerOfMass;
            boneInfo.rigidBody.inertiaTensor = boneInfo.rigidBody.inertiaTensor;
            boneInfo.rigidBody.inertiaTensorRotation = boneInfo.rigidBody.inertiaTensorRotation;
            
            Grabbable[] grabbables;
            switch( ragdollBone ){
            case BipedBone.UPPER_ARM_R:
            case BipedBone.UPPER_ARM_L:
            case BipedBone.ARM_R:
            case BipedBone.ARM_L:
            case BipedBone.HAND_R:
            case BipedBone.HAND_L:
            case BipedBone.UPPER_LEG_R:
            case BipedBone.UPPER_LEG_L:
            case BipedBone.LEG_R:
            case BipedBone.LEG_L:
                grabbables = Grabbable.BuidGrabbables( boneColliders );
                break;
            default:
                grabbables = new Grabbable[0];
                break;
            }

            var muscle = new Muscle( boneInfo.joint, boneInfo.rigidBody, boneInfo.transform, boneColliders, grabbables );
            muscles[i] = muscle;
            
            var grabListener = boneInfo.rigidBody.gameObject.GetComponent<RigidBodyGrabListener>();
            muscle.InitializeGrabListener( character, grabListener );

            var currentRagdollBone = ragdollBone;
            boneInfo.colliderDetector.character = character;
            boneInfo.colliderDetector.onCollisionEnter += delegate( Collision collision ){ onCollisionEnter.Invoke( currentRagdollBone, collision ); };
            boneInfo.colliderDetector.onCollisionExit += delegate( Collision collision ){ onCollisionExit.Invoke( currentRagdollBone, collision ); };
        }
        root = hips;
        
        model.SetDeltaTransform( hips.target );
        SetMusclePinMode( false );
        float scale = model.armature.lossyScale.y;
        model.bipedProfile.localHeadForeheadY /= scale;

        spacing.transform.localScale = Vector3.one/( model.bipedProfile.hipHeight*0.2f )/model.scale;
        
        //apply collision ignores
        
        IgnoreBonePhysics( BipedBone.LOWER_SPINE, BipedBone.UPPER_LEG_L );
        IgnoreBonePhysics( BipedBone.LOWER_SPINE, BipedBone.UPPER_LEG_R );
        IgnoreBonePhysics( BipedBone.HIPS, BipedBone.UPPER_SPINE );
        IgnoreBonePhysics( BipedBone.UPPER_LEG_R, BipedBone.UPPER_LEG_L );
        IgnoreBonePhysics( BipedBone.HEAD, BipedBone.UPPER_SPINE );
        IgnoreBonePhysics( BipedBone.UPPER_ARM_L, BipedBone.UPPER_ARM_R );
        IgnoreBonePhysics( BipedBone.HIPS, BipedBone.UPPER_ARM_R );
        IgnoreBonePhysics( BipedBone.HIPS, BipedBone.UPPER_ARM_L );
        IgnoreBonePhysics( BipedBone.LOWER_SPINE, BipedBone.UPPER_ARM_R );
        IgnoreBonePhysics( BipedBone.LOWER_SPINE, BipedBone.UPPER_ARM_L );
        IgnoreBonePhysics( BipedBone.UPPER_LEG_R, BipedBone.UPPER_ARM_R );
        IgnoreBonePhysics( BipedBone.UPPER_LEG_L, BipedBone.UPPER_ARM_L );
        IgnoreBonePhysics( BipedBone.UPPER_SPINE, BipedBone.UPPER_ARM_R );
        IgnoreBonePhysics( BipedBone.UPPER_SPINE, BipedBone.UPPER_ARM_L );
        IgnoreBonePhysics( BipedBone.HEAD, BipedBone.UPPER_ARM_R );
        IgnoreBonePhysics( BipedBone.HEAD, BipedBone.UPPER_ARM_L );
        IgnoreBonePhysics( BipedBone.HEAD, BipedBone.UPPER_SPINE );
        IgnoreBonePhysics( BipedBone.FOOT_L, BipedBone.FOOT_R );

        foreach (Muscle m in muscles){
            m.Initiate( muscles );
        }
        
        outlineMat = new Material( BuiltInAssetManager.main.modeOutlineMaterial );
        outlineMat.SetTexture( "_BaseColorMap", model.renderer.sharedMaterials[0].GetTexture("_BaseColorMap") );

        headLookAt = new HeadLookAt( character );
        eyeLookAt = new EyeLookAt( character, character.characterSettings.eyeDegrees );

        visionCollider.transform.localScale = new Vector3( 6, 8, 8 )/visionCollider.transform.lossyScale.x;
        model.armature.localPosition = Vector3.zero;
        headLookAt.lookOffset = head.target.parent.rotation*Quaternion.Inverse( model.bipedProfile.spineTposeDeltas[3] );

        rightArmIK = CreateArmIK( true, character );
        leftArmIK = CreateArmIK( false, character );


        
        if( vision ) GameObject.DestroyImmediate( vision );
        vision = Vision.CreateVision( head.rigidBody.gameObject );
        vision._InternalSetup( character );

        var smileBinding = model.profile.blendShapeBindings[(int)RagdollBlendShape.SMILE];
        if( !string.IsNullOrEmpty( smileBinding ) ){
            smileBlendShapeIndex = model.skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex( smileBinding );
        }
    }

    public override void _InternalReset( Character character ){
        base._InternalReset( character );
        headLookAt._InternalReset();
        rightArmIK.Kill( false );
        leftArmIK.Kill( false );

        if( !rightHandGrabber ){
            rightHandGrabber = Grabber._InternalCreateGrabber( character, this, RagdollMuscle.HAND_R );
            SetupHandGrabberDetection( rightHandGrabber, true );
            rightArmObstacleFix = new ArmObstacleFix( character, rightArmIK, rightHandGrabber );
            ExtractAndApplyGrabOffsets( rightHand, rightHandGrabber );
        }
        if( !leftHandGrabber ){
            leftHandGrabber = Grabber._InternalCreateGrabber( character, this, RagdollMuscle.HAND_L );
            SetupHandGrabberDetection( leftHandGrabber, false );
            leftArmObstacleFix = new ArmObstacleFix( character, leftArmIK, leftHandGrabber );
            ExtractAndApplyGrabOffsets( leftHand, leftHandGrabber );
        }

        character.characterDetector.onCharacterNearby._InternalAddListener( OnOtherCharacterNearby );
        character.characterDetector.onCharacterFar._InternalAddListener( OnOtherCharacterFar );
        vision._InternalReset();
        EnableHandGrabAnimations();
        ResetAutoLook();
        surface = null;   //start off character as not on floor

        spacing.gameObject.SetActive( !character.isPossessed );
    }
    
    private void EnableHandGrabAnimations(){
        rightHandGrabber.onGrabbed._InternalAddListener( OnRightGrabbedGrabbable );
        rightHandGrabber.onReleased._InternalAddListener( OnRightReleasedGrabbable );
        leftHandGrabber.onGrabbed._InternalAddListener( OnLeftGrabbedGrabbable );
        leftHandGrabber.onReleased._InternalAddListener( OnLeftReleasedGrabbable );
    }

    public void DisableHandGrabAnimations(){
        rightHandGrabber.onGrabbed._InternalRemoveListener( OnRightGrabbedGrabbable );
        rightHandGrabber.onReleased._InternalRemoveListener( OnRightReleasedGrabbable );
        leftHandGrabber.onGrabbed._InternalRemoveListener( OnLeftGrabbedGrabbable );
        leftHandGrabber.onReleased._InternalRemoveListener( OnLeftReleasedGrabbable );
    }
    private void OnRightReleasedGrabbable( GrabContext grabContext ){
        rightHoldHandle?.Kill();
    }
    private void OnLeftReleasedGrabbable( GrabContext grabContext ){
        leftHoldHandle?.Kill();
    }

    private void OnDestroy(){
        foreach( var muscle in muscles ){
            foreach( var grabbable in muscle.grabbables ){
                grabbable.ReleaseAll();
            }
        }
    }
    
    private void OnRightGrabbedGrabbable( GrabContext grabContext ){
        InitializeItemHoldIK( true, grabContext.grabbable.parent, ref rightHoldHandle, grabContext.grabber.character );
    }
    private void OnLeftGrabbedGrabbable( GrabContext grabContext ){
        InitializeItemHoldIK( false, grabContext.grabbable.parent, ref leftHoldHandle, grabContext.grabber.character );
    }
    
    public override void OnSetIgnoreGrabbableWithBody( Grabbable grabbable, bool ignore ){
        if( grabbable.parentItem ){
            foreach( var muscle in muscles ){
                Util.IgnorePhysics( muscle.colliders, grabbable.parentItem.colliders, ignore );
            }

            if( !rightHandGrabber.character.isPossessed ){
                for( int i=0; i<rightHandGrabber.contextCount; i++ ){
                    var context = rightHandGrabber.GetGrabContext(i);
                    context?.grabbable?.parentItem?.SetIgnorePhysics( grabbable.parentItem, ignore );
                }
                for( int i=0; i<leftHandGrabber.contextCount; i++ ){
                    var context = leftHandGrabber.GetGrabContext(i);
                    context?.grabbable?.parentItem?.SetIgnorePhysics( grabbable.parentItem, ignore );
                }
            }
            
        }else if( grabbable.parentCharacter ){
            foreach( var muscle in grabbable.parentCharacter.ragdoll.muscles ){
                Util.IgnorePhysics( rightArm.colliders, muscle.colliders, ignore );
                Util.IgnorePhysics( rightUpperArm.colliders, muscle.colliders, ignore );
                Util.IgnorePhysics( rightHand.colliders, muscle.colliders, ignore );
            }
            foreach( var muscle in grabbable.parentCharacter.ragdoll.muscles ){
                Util.IgnorePhysics( leftArm.colliders, muscle.colliders, ignore );
                Util.IgnorePhysics( leftUpperArm.colliders, muscle.colliders, ignore );
                Util.IgnorePhysics( leftHand.colliders, muscle.colliders, ignore );
            }
        }
    }
    
    private void InitializeItemHoldIK( bool rightArm, VivaInstance vivaInstance, ref IKHandle holdHandle, Character character ){
        if( vivaInstance == null ) return;
        
        ArmIK armIK = rightArm ? rightArmIK : leftArmIK;
        holdHandle?.Kill();
        var posBone = model.bipedProfile[ BipedBone.LOWER_SPINE ].transform;

        armIK.AddRetargeting( delegate( out Vector3 target, out Vector3 pole, out Quaternion handRotation ){
            var holdScale = model.bipedProfile.floorToHeadHeight*rightHandGrabber.character.scale*10;
            var holdPosOffset = ( character.isPossessed && vivaInstance._internalSettings.useKeyboardHoldOffsets ? vivaInstance._internalSettings.keyboardHoldPosOffset : vivaInstance._internalSettings.holdPosOffset )*holdScale;
            holdPosOffset.x *= armIK.sign;
			target = armIK.shoulder.position+posBone.transform.TransformDirection( holdPosOffset );
            if( rightHandGrabber.character.isPossessed ){
                target += Camera.main.transform.up*rightHandGrabber.width;
            }

            pole = model.armature.position+model.armature.right*holdScale*armIK.sign*0.05f;

            var holdEuler = character.isPossessed && vivaInstance._internalSettings.useKeyboardHoldOffsets ? vivaInstance._internalSettings.keyboardHoldEulerOffset : vivaInstance._internalSettings.holdEulerOffset;
            holdEuler.y *= rightArm ? rightHandGrabber.sign : leftHandGrabber.sign;
            holdEuler.z *= rightArm ? rightHandGrabber.sign : leftHandGrabber.sign;
            var holdRotationOffset = Quaternion.Euler( holdEuler );
            
            handRotation = posBone.rotation*holdRotationOffset;

            var item = vivaInstance as Item;
            if( item ){
                var keepUpright = item.canSpill ? 1f : 0;
                if( keepUpright > 0 ){
                    handRotation = Quaternion.LerpUnclamped( handRotation, model.armature.rotation*holdRotationOffset, keepUpright );
                }
            }
        }, out holdHandle, 1 );
        if( !character.isPossessed ) holdHandle.speed = 3f;
    }

    private void OnOtherCharacterNearby( Character other ){
        foreach( var otherDynamicBone in other.ragdoll.dynamicBones ){
            foreach( var handBoxCollider in handBoxColliders ){
                otherDynamicBone.colliders.Add( handBoxCollider );
            }
        }
    }

    private void OnOtherCharacterFar( Character other ){
        foreach( var otherDynamicBone in other.ragdoll.dynamicBones ){
            foreach( var handBoxCollider in handBoxColliders ){
                otherDynamicBone.colliders.Remove( handBoxCollider );
            }
        }
    }

    public override void OnApplyLateAnimationModifiers( Character character ){
        
        if( !character.isPossessed ){
            headLookAt.Apply();
            eyeLookAt.Apply();
        }
        rightArmIK.strength.Add( name, character.mainAnimationLayer.player.SampleCurve( rightArmID, 1 ) );
        rightArmIK.Apply();
        leftArmIK.strength.Add( name, character.mainAnimationLayer.player.SampleCurve( leftArmID, 1 ) );
        leftArmIK.Apply();

        //apply emotions
        var emotionBlend = character.mainAnimationLayer.player.SampleCurve( BipedRagdoll.emotionID, 0f );
        if( emotionBlend > 0 && smileBlendShapeIndex != -1 ){
            model.skinnedMeshRenderer.SetBlendShapeWeight( smileBlendShapeIndex, Mathf.LerpUnclamped( model.skinnedMeshRenderer.GetBlendShapeWeight( smileBlendShapeIndex ), 1f, emotionBlend ) );
        }
    }

    private Collider[] MirrorColliderChildren( Collider[] mirrorColliders, Transform target ){
        var result = new Collider[ mirrorColliders.Length ];
        for( int i=0; i<mirrorColliders.Length; i++ ){
            var mirrorCollider = mirrorColliders[i];
            var container = new GameObject( "mirrored_"+mirrorCollider );
            container.transform.localPosition = mirrorCollider.transform.localPosition;
            container.transform.localRotation = mirrorCollider.transform.localRotation;
            container.transform.localScale = mirrorCollider.transform.localScale;
            container.transform.SetParent( target, false );

            var capsuleCollider = mirrorCollider as CapsuleCollider;
            if( capsuleCollider ){
                var cc = container.AddComponent<CapsuleCollider>();
                cc.height = capsuleCollider.height;
                cc.radius = capsuleCollider.radius;
                cc.direction = capsuleCollider.direction;
                cc.center = capsuleCollider.center;
                result[i] = cc;
            }else{
                var boxCollider = mirrorColliders[i] as BoxCollider;
                if( boxCollider ){
                    var bc = container.AddComponent<BoxCollider>();
                    bc.size = boxCollider.size;
                    bc.center = boxCollider.center;
                    result[i] = bc;
                }
            }
        }
        return result;
    }

    private Collider[] ImproviseColliders( BipedBone ragdollBone ){
        var boneTransform = model.bipedProfile[ ragdollBone ].transform;
        Vector3? boneEnd = null;
        var primaryChild = BipedProfile.GetHierarchyPrimaryChild( ragdollBone );
        if( primaryChild.HasValue ) boneEnd = model.bipedProfile[ primaryChild.Value ].transform.position;

        var container = new GameObject( "_RIGID_" );
        container.transform.SetParent( boneTransform, true );

        bool hasMirrorVariant = BipedProfile.HasMirrorVariant( ragdollBone );
        
        var hipHeight = model.bipedProfile.hipHeight*model.armature.lossyScale.x;
        
        Collider collider;
        if( ragdollBone == BipedBone.HAND_R || ragdollBone == BipedBone.HAND_L ){
            container.transform.position = boneTransform.position;
            container.transform.rotation = boneTransform.rotation;
            var bc = container.AddComponent<BoxCollider>();
            collider = bc;
            bc.size = new Vector3( 0.02f, 0.13f, 0.06f )*hipHeight;
            switch( ragdollBone ){
            case BipedBone.HAND_R:
                bc.center = Vector3.up*bc.size.y/2;
                break;
            case BipedBone.HAND_L:
                bc.center = Vector3.up*bc.size.y/2;
                break;
            }
        }else{
            float mirrorLimbSize = ( ragdollBone == BipedBone.UPPER_LEG_L || ragdollBone == BipedBone.UPPER_LEG_R ) ? 0.06f : 0.04f;
            float nonMirrorLimbSize = ragdollBone == BipedBone.HEAD ? 0.1325f : 0.1125f;
            float relativeLimbSize = hasMirrorVariant ? mirrorLimbSize : nonMirrorLimbSize;
            var cc = container.AddComponent<CapsuleCollider>();
            collider = cc;
            cc.direction = 2;
            cc.radius = hipHeight*relativeLimbSize;
            if( boneEnd.HasValue ){
                
                container.transform.position = ( boneTransform.position+boneEnd.Value )/2.0f;
                Vector3 diff = boneTransform.position-boneEnd.Value;
                cc.height = diff.magnitude-cc.radius;

                if( diff.sqrMagnitude == 0 ) diff = Vector3.forward;
                if( Mathf.Abs( diff.y ) >= 1.0f ) diff = Vector3.forward;
                container.transform.rotation = Quaternion.LookRotation( diff, Vector3.up );

                if( hasMirrorVariant ) cc.height += cc.radius*2;
            }else{
                container.transform.position = boneTransform.position;
                cc.height = ragdollBone == BipedBone.HEAD ? 0 : cc.radius*3.0f;
                switch( ragdollBone ){
                case BipedBone.FOOT_R:
                case BipedBone.FOOT_L:
                    cc.height *= 1.25f;
                    cc.center = -Vector3.up*cc.radius+Vector3.forward*cc.height/2;
                    cc.height *= 1.25f;
                    break;
                case BipedBone.HEAD:
                    cc.center = Vector3.up*cc.radius*0.8f+Vector3.forward*cc.height/2;
                    break;
                }
            }
        }
        return new Collider[]{ collider };
    }

    protected override Collider[] FindComponentChildren( Transform target, int layer ){
        var colliders = new List<Collider>();
        for( int i=0; i<target.childCount; i++ ){
            var child = target.GetChild(i);
            CheckAddComponents<Collider>( colliders, child, layer );
            if( child.name == "NECK" ){
                for( int j=0; j<child.childCount; j++ ){
                    CheckAddComponents<Collider>( colliders, child.GetChild(j), layer );
                }
            }
        }
        return colliders.ToArray();
    }
}

}