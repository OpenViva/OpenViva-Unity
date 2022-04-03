using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



namespace viva{


/// <summary>
/// The class used to represent 16-bone physics ragdolls. You can control the strength of each animated muscle or the ragdoll as a whole with
/// muscleLimit and pinLimit.
/// </summary>
public abstract partial class Ragdoll: MonoBehaviour{

    public static float pinForceMultBipedNPC = 0.15f;
    public static float pinForceMultAnimalNPC = 0.5f;
    public static float pinForceMultUser = 0.5f;
    
    public static readonly float defaultMuscleSpringConstant = 10f;
    public static readonly float defaultMuscleDamperConstant = 5f;
    
    [Range(0,1)]
    [SerializeField]
    protected float minGroundDot = 0.4f;
    [SerializeField]
    private CapsuleCollider m_capsuleCollider;
    public CapsuleCollider capsuleCollider { get{ return m_capsuleCollider; } }
    [SerializeField]
    private Rigidbody m_movementBody;
    public Rigidbody movementBody { get{ return m_movementBody; } }
    [SerializeField]
    protected float distanceSnapMult = 10f;
    [SerializeField]
    public Rigidbody[] rigidBodies;
    [Range(0f, 1f)] public float mappingWeight = 1f;
    /// <summary> The LimitGroup controlling the maximum muscle percent when applying animations on the ragdoll.
    /// Adding a limit of 0 percent will make the ragdoll collapse on the floor and have zero influence from an animation.
    [SerializeField]
    public LimitGroup muscleLimit = new LimitGroup();
    [SerializeField]
    /// <summary> The LimitGroup controlling the maximum pin percent when applying animations on the ragdoll.
    /// Adding a limit of 0 percent will make the ragdoll collapse on the floor but retain their animation pose.
    public LimitGroup pinLimit = new LimitGroup();
    public Muscle[] muscles { get; protected set; }
    
    /// <summary> The model that this BipedRagdoll is bound to.
    public Model model;
    public GenericCallback onPreMap; 
    public GenericCallback onPostMap;
    private bool fixedUpdateFrame = false;
    private int grabbablesGrabbed = 0;
    public bool isBeingGrabbed { get{ return grabbablesGrabbed>0; } }
    private List<DynamicBone> m_dynamicBones = new List<DynamicBone>();
    public IList<DynamicBone> dynamicBones { get{ return m_dynamicBones.AsReadOnly(); } }
    public Vector3? surface { get; protected set; }
    public Collider surfaceCollider { get; protected set; }
    public bool isOnWater { get; private set; }
    public Muscle root { get; protected set; }


    
    
    private void OnCollisionStay( Collision collision ){
        var minGroundContactY = transform.TransformPoint( capsuleCollider.center+Vector3.down*( capsuleCollider.height/2-capsuleCollider.radius ) ).y;
        for( int i=0; i<collision.contactCount; i++ ){
            var contact = collision.GetContact(i);
            if( contact.point.y > minGroundContactY ) continue;
            if( Vector3.Dot( contact.normal, Vector3.up ) < minGroundDot ) continue;
            surface = new Vector3( capsuleCollider.bounds.center.x, capsuleCollider.bounds.min.y, capsuleCollider.bounds.center.z );
            surfaceCollider = contact.otherCollider;
            break;
        }
    }
    
    private void OnTriggerStay( Collider collider ){
        if( collider.gameObject.layer == WorldUtil.waterLayer ){
            isOnWater = true;
        }
    }
    
    private void OnTriggerExit( Collider collider ){
        if( collider.gameObject.layer == WorldUtil.waterLayer ){
            isOnWater = false;
        }
    }

    public void SetMusclePinMode( bool player ){
        if( player ){
            foreach( var muscle in muscles ) muscle.pinForceMult = pinForceMultUser;
        }else{
            if( this as BipedRagdoll ){
                foreach( var muscle in muscles ) muscle.pinForceMult = pinForceMultBipedNPC;
            }else{
                foreach( var muscle in muscles ) muscle.pinForceMult = pinForceMultAnimalNPC;
            }
            root.pinForceMult = 1f;
        }
    }

    protected void ApplyGravity(){
        if( pinLimit.value == 1f ){
            movementBody.AddForce( Physics.gravity, ForceMode.Acceleration );
        }else{
            movementBody.AddForce( Physics.gravity*pinLimit.value, ForceMode.Acceleration );
            foreach( var muscle in muscles ){
                muscle.rigidBody.AddForce( Physics.gravity*(1f-pinLimit.value), ForceMode.Acceleration );
            }
        }
    }

    public virtual void _InternalReset( Character character ){
        onPreMap = null;
        onPostMap = null;
        grabbablesGrabbed = 0;
        
        foreach( var muscle in muscles ){
            muscle._InternalReset();
            foreach( var grabbable in muscle.grabbables ){
                grabbable.gameObject.layer = WorldUtil.grabbablesLayer;
                grabbable.onGrabbed._InternalAddListener( OnLimbGrabbed );
                grabbable.onReleased._InternalAddListener( OnLimbReleased );
            }
        }
        pinLimit._InternalReset();
    }
    
    protected void CheckAddComponents<T>( List<T> items, Transform transform, int layer ) where T:Component{
        if( transform.gameObject.layer == layer ){
            var c = transform.GetComponent<T>();
            if( c ) items.Add( c );
        }
    }

    public virtual void OnApplyLateAnimationModifiers( Character character ){}
    public virtual void OnSetIgnoreGrabbableWithBody( Grabbable grabbable, bool ignore ){}
    public abstract void RetargetArmature();

    private void OnLimbGrabbed( GrabContext grabContext ){
        grabbablesGrabbed++;
    }
    
    private void OnLimbReleased( GrabContext grabContext ){
        grabbablesGrabbed--;
    }

    protected abstract Collider[] FindComponentChildren( Transform target, int layer );

    protected void IgnoreBonePhysics( BipedBone bone0, BipedBone bone1 ){
        var boneInfo0 = model.bipedProfile[ bone0 ].rigidBody.transform;
        var boneInfo1 = model.bipedProfile[ bone1 ].rigidBody.transform;
        var c0 = FindComponentChildren( boneInfo0, WorldUtil.characterCollisionsLayer );
        var c1 = FindComponentChildren( boneInfo1, WorldUtil.characterCollisionsLayer );
        for( int i=0; i<c0.Length; i++ ){
            for( int j=0; j<c1.Length; j++ ){
                Physics.IgnoreCollision( c0[i], c1[j], true );
            }
        }
    }

    public void Setup( Model _model, Character source ){ 
        if( _model == null ) throw new System.Exception("Cannot create BipedRagdoll with null model");
        if( _model.profile == null ) throw new System.Exception("Model must have a RagdollProfile");

        model = _model;
        m_dynamicBones = new List<DynamicBone>( model.rootTransform.gameObject.GetComponentsInChildren<DynamicBone>() );
        OnSetup( source );
        model.renderer.gameObject.layer = WorldUtil.characterCollisionsLayer;
    }

    protected abstract void OnSetup( Character source );


    public void DebugArmature( Color color ){
        if( model.bipedProfile != null ){
            for( int i=0; i<BipedProfile.nonOptionalBoneCount; i++ ){
                var ragdollBone = (BipedBone)i;
                var parent = BipedProfile.GetBoneHierarchyParent( ragdollBone );
                if( parent.HasValue ){
                    Debug.DrawLine(
                        model.bipedProfile.bones[ (int)ragdollBone ].transform.position, 
                        model.bipedProfile.bones[ (int)parent.Value ].transform.position,
                        color,
                        Time.fixedDeltaTime
                    );
                }
            }
        }
    }

    public abstract void PinToAnimation( AnimationLayer maskedLayer=null );
    public abstract void SnapArmatureToPhysics();

    public float CalculateMuscleBoundingSphere( Muscle muscle ){
        var colliders = FindComponentChildren( muscle.rigidBody.transform, WorldUtil.characterCollisionsLayer );
        var samplePoints = new List<Vector3>();
        foreach( var collider in colliders ){
            var cc = collider as CapsuleCollider;
            if( cc ){
                Vector3 dir;
                if( cc.direction == 0 ){
                    dir = Vector3.right;
                }else if( cc.direction == 1 ){
                    dir = Vector3.up;
                }else{
                    dir = Vector3.forward;
                }
                samplePoints.Add( ( cc.center+dir*( Mathf.Max( cc.height-cc.radius*2, 0 )/2+cc.radius ) )*collider.transform.localScale.x-muscle.rigidBody.centerOfMass );
                samplePoints.Add( ( cc.center-dir*( Mathf.Max( cc.height-cc.radius*2, 0 )/2+cc.radius ) )*collider.transform.localScale.x-muscle.rigidBody.centerOfMass );
                continue;
            }

            var sc = collider as SphereCollider;
            if( sc ){
                samplePoints.Add( sc.center+Vector3.forward*sc.radius*collider.transform.localScale.x-muscle.rigidBody.centerOfMass );
            }
        }
        float radius = 0.0f;
        foreach( var point in samplePoints ){
            float pointSqLength = point.sqrMagnitude;
            if( pointSqLength > radius ){
                radius = pointSqLength;
            }
        }
        radius = Mathf.Sqrt( radius )*muscle.rigidBody.transform.lossyScale.x;
        return radius;
    }

    protected virtual void FixedUpdate(){
        fixedUpdateFrame = true;
        surface = null;
        surfaceCollider = null;
        ApplyGravity();
    }
    
    protected virtual void LateUpdate(){
        if( fixedUpdateFrame ){
            fixedUpdateFrame = false;
            onPreMap?.Invoke();
            foreach (Muscle m in muscles){
                m.Map(mappingWeight);
            }
            onPostMap?.Invoke();
        }
    }
}

}