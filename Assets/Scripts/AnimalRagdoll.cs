using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



namespace viva{


/// <summary>
/// The class used to represent 16-bone physics ragdolls. You can control the strength of each animated muscle or the ragdoll as a whole with
/// muscleLimit and pinLimit.
/// </summary>
public partial class AnimalRagdoll: Ragdoll{
    
    public ListenerAnimalCollision onCollisionEnter { get; private set; } = new ListenerAnimalCollision( "onCollisionEnter" );
    public ListenerAnimalCollision onCollisionExit { get; private set; } = new ListenerAnimalCollision( "onCollisionExit" );
    

    
    public override void PinToAnimation( AnimationLayer maskedLayer=null ){
        //not allowed to have more pin than muscle
        float pinStrength = Mathf.Min( pinLimit.value, muscleLimit.value );
        for( int muscleIndex=0; muscleIndex<muscles.Length; muscleIndex++ ){
            var muscle = muscles[muscleIndex];
            muscle.Read();
            muscle.Pin( pinStrength, muscle==root );
            muscle.MuscleRotation( muscleLimit.value, defaultMuscleSpringConstant, defaultMuscleDamperConstant );   //fixes root motion
        }
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

    public override void SnapArmatureToPhysics(){
        
        //move root to root target position
        var targetMovementBodyPos = root.rigidBody.transform.position+( model.armature.position-root.target.position );
        var oldRootPos = root.rigidBody.transform.position;
        var sqDist = Vector3.SqrMagnitude( ( movementBody.transform.position-targetMovementBodyPos )*distanceSnapMult );
        var finalMovementBodyPos = Vector3.Lerp( movementBody.transform.position, targetMovementBodyPos, Mathf.Clamp01(sqDist)*0.5f );
        movementBody.transform.position = finalMovementBodyPos;
        root.rigidBody.transform.position = oldRootPos;
        
        //adjust capsule variables
        var targetHeight = root.target.position.y-model.armature.position.y;
        var rootPos = root.rigidBody.transform.position;
        float floorY;
        if( !surface.HasValue || isOnWater ){
            floorY = model.armature.position.y;
            rootPos.y = Mathf.Lerp( rootPos.y, floorY+targetHeight, 0.5f );
        }else{
            var deltaHeight = ( surface.Value.y+targetHeight )-root.target.position.y;
            floorY = surface.Value.y-deltaHeight*0.5f;
            rootPos.y = floorY+targetHeight;
        }
        capsuleCollider.height = Mathf.Max( ( rootPos.y-floorY )/model.scale, capsuleCollider.radius*2f );
        rootPos.y = floorY+( rootPos.y-floorY )/2f;
        capsuleCollider.center = transform.InverseTransformPoint( rootPos );
    }

    private void OnDestroy(){
        foreach( var muscle in muscles ){
            foreach( var grabbable in muscle.grabbables ){
                grabbable.ReleaseAll();
            }
        }
    }

    public Grabbable GetRandomGrabbable(){
        foreach( var muscle in muscles ){
            if( muscle.grabbables.Length > 0 ) return muscle.grabbables[0];
        }
        return null;
    }

    protected override void OnSetup( Character _character ){
        // model.armature.rotation = Quaternion.identity;
        model.ApplySpawnPose( false );

        model.armature.localPosition = Vector3.zero;
        
        //build muscles
        muscles = new Muscle[ model.muscleTemplates.Length ];
        for( int i=0; i<muscles.Length; i++ ){
            var muscleTemplate = model.muscleTemplates[i];
            
            var boneInfo = model.animalProfile[ muscleTemplate.boneName ];

            var container = new GameObject( boneInfo.name );
            container.layer = WorldUtil.characterCollisionsLayer;
            container.transform.position = boneInfo.transform.position;
            container.transform.rotation = boneInfo.transform.rotation;

            boneInfo.rigidBody = container.AddComponent<Rigidbody>();
            boneInfo.rigidBody.mass = muscleTemplate.mass;
            boneInfo.rigidBody.maxAngularVelocity = 50.0f;  //SteamVR recommends 50
            boneInfo.rigidBody.transform.position = boneInfo.transform.position;
            boneInfo.rigidBody.transform.rotation = boneInfo.transform.rotation;
            boneInfo.rigidBody.velocity = Vector3.zero;
            boneInfo.rigidBody.angularVelocity = Vector3.zero;
            boneInfo.rigidBody.useGravity = false;
            boneInfo.rigidBody.angularDrag = 16f;
            boneInfo.joint = container.AddComponent<ConfigurableJoint>();
            var limit = boneInfo.joint.lowAngularXLimit;
            limit.limit = -muscleTemplate.pitch/2f;
            boneInfo.joint.lowAngularXLimit = limit;
            limit.limit = muscleTemplate.pitch/2f;
            boneInfo.joint.highAngularXLimit = limit;
            limit.limit = muscleTemplate.yaw;
            boneInfo.joint.angularYLimit = limit;
            limit.limit = muscleTemplate.roll;
            boneInfo.joint.angularZLimit = limit;

            var boneColliders = FindComponentChildren<Collider>( boneInfo.transform, WorldUtil.itemsLayer );
            foreach( var boneCollider in boneColliders ){
                boneCollider.transform.SetParent( boneInfo.rigidBody.transform, true );
                boneCollider.gameObject.layer = boneInfo.rigidBody.gameObject.layer;    //characterCollisionsLayer
                boneCollider.material = BuiltInAssetManager.main.ragdollActivePhysicMaterial;
            }

            var muscleGrabbables = FindComponentChildren<Grabbable>( boneInfo.transform, WorldUtil.grabbablesLayer );
            foreach( var grabbable in muscleGrabbables ){
                grabbable.transform.SetParent( boneInfo.rigidBody.transform, true );
                // grabbable._InternalInitialize();
            }

            boneInfo._InternalCreateColliderDetector();
            boneInfo.rigidBody.ResetCenterOfMass();
            boneInfo.rigidBody.ResetInertiaTensor();
            boneInfo.rigidBody.centerOfMass = boneInfo.rigidBody.centerOfMass;
            boneInfo.rigidBody.inertiaTensor = boneInfo.rigidBody.inertiaTensor;
            boneInfo.rigidBody.inertiaTensorRotation = boneInfo.rigidBody.inertiaTensorRotation;

            var muscle = new Muscle( boneInfo.joint, boneInfo.rigidBody, boneInfo.transform, boneColliders, muscleGrabbables );
            muscles[i] = muscle;
            
            foreach( var grabbable in muscleGrabbables ){
                Grabbable._InternalSetup( _character, boneInfo.rigidBody, grabbable, new GrabbableSettings(){ uprightOnly=0, freelyRotate=false } );
            }

            var currentBody = boneInfo.rigidBody.name;
            boneInfo.colliderDetector.character = _character;
            boneInfo.colliderDetector.onCollisionEnter += delegate( Collision collision ){ onCollisionEnter.Invoke( currentBody, collision ); };
            boneInfo.colliderDetector.onCollisionExit += delegate( Collision collision ){ onCollisionExit.Invoke( currentBody, collision ); };
        }

        //build parenting hierarchy
        foreach( var muscle in muscles ){
            //find containerParent in hierarchy
            var targetParent = muscle.target.parent;
            Muscle parent = null;
            while( targetParent != model.armature ){
                foreach( var candidate in muscles ){
                    if( candidate.target == targetParent ){
                        parent = candidate;
                        break;
                    }
                }
                if( parent != null ) break;
                targetParent = targetParent.parent;
            }
            if( parent != null ){
                muscle.rigidBody.transform.SetParent( parent.rigidBody.transform, true );
                muscle.joint.connectedBody = parent.rigidBody;
                muscle.joint.xMotion = ConfigurableJointMotion.Locked;
                muscle.joint.yMotion = ConfigurableJointMotion.Locked;
                muscle.joint.zMotion = ConfigurableJointMotion.Locked;
                muscle.joint.angularXMotion = ConfigurableJointMotion.Limited;
                muscle.joint.angularYMotion = ConfigurableJointMotion.Limited;
                muscle.joint.angularZMotion = ConfigurableJointMotion.Limited;
            }else{
                muscle.rigidBody.transform.SetParent( transform, true );
                root = muscle;
            }
        }

        foreach (Muscle m in muscles){
            m.Initiate( muscles );
        }
        
        SetMusclePinMode( false );

        var deltaTransform = root.target;
        while( deltaTransform.parent != model.armature ){
            deltaTransform = deltaTransform.parent;
        }
        model.SetDeltaTransform( deltaTransform );
    }

    private T[] FindComponentChildren<T>( Transform target, int layer ) where T:Component{
        var items = new List<T>();
        for( int i=0; i<target.childCount; i++ ){
            var child = target.GetChild(i);
            CheckAddComponents( items, child, layer );
        }
        return items.ToArray();
    }

    protected override Collider[] FindComponentChildren( Transform target, int layer ){
        return FindComponentChildren<Collider>( target, layer );
    }
}

}