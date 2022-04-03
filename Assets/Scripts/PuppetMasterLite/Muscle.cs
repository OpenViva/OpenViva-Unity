using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

public delegate void GrabContextCallback( GrabContext context );
public delegate void ColliderReturnFunc( Collider collider );

/// <summary> The class controlling animated ConfigurableJoints
public class Muscle{

    public readonly Collider[] colliders;

    /// <summary> The configurable joint being controlled by this muscle.
    public ConfigurableJoint joint;
    /// <summary> The bone of the animation rig that this muscle is animated by.
    public Transform target;
    /// <summary> The percent limit of the muscle. Adding a limit of 0 will disable the muscle and turn the limb floppy.
    public readonly LimitGroup strengthLimit;
    private List<GrabContext> contexts = new List<GrabContext>();
    public void IterateGrabContexts( GrabContextCallback onContext ){
        for( int i=contexts.Count; i-->0; ){
            onContext( contexts[i] );
        }
    }
    
    public Transform transform { get; private set; }
    /// <summary> The Rigidbody of the muscle itself
    public Rigidbody rigidBody { get; private set; }
    public Vector3 positionOffset { get; private set; }
    
    private JointDrive slerpDrive = new JointDrive();
    private Quaternion defaultLocalRotation = Quaternion.identity;
    private Quaternion toJointSpaceInverse = Quaternion.identity;
    private Quaternion toJointSpaceDefault = Quaternion.identity;
    private Quaternion targetAnimatedRotation = Quaternion.identity;
    private Quaternion defaultTargetLocalRotation = Quaternion.identity;
    private Quaternion toParentSpace = Quaternion.identity;
    private Quaternion localRotationConvert = Quaternion.identity;
    private Quaternion targetAnimatedWorldRotation = Quaternion.identity;
    private Quaternion defaultRotation = Quaternion.identity;
    private Vector3 defaultPosition;
    private Vector3 defaultTargetLocalPosition;
    private float lastJointDriveRotationWeight, lastRotationDamper;
    private bool initiated;
    private Transform connectedBodyTarget;
    private Transform connectedBodyTransform;
    private Transform targetParent;
    private bool directTargetParent;
    private Vector3 targetVelocity;
    private Vector3 targetAnimatedCenterOfMass;
    public Grabbable[] grabbables { get; private set; }
    public float pinForceMult = 0.15f;

    
    private void UpdateStrength(){
        if( strengthLimit.value==0 ){
            lastRotationDamper = 0;
            slerpDrive.positionSpring = 0;
            slerpDrive.maximumForce = 0;
            slerpDrive.positionDamper = 0;
            joint.slerpDrive = slerpDrive;
        }
    }

    public Muscle( ConfigurableJoint _joint, Rigidbody _rigidBody, Transform _target, Collider[] _colliders, Grabbable[] _grabbables ){
        joint = _joint;
        rigidBody = _rigidBody;
        target = _target;
        transform = rigidBody.transform;
        strengthLimit = new LimitGroup( UpdateStrength );
        colliders = _colliders;
        grabbables = _grabbables;
    }
    
    public Vector3 GetClosestColliderPoint( Vector3 point ){
        float shortestSqDist = Mathf.Infinity;
        Vector3 closest = Vector3.zero;
        foreach( var collider in colliders ){
            var candidate = collider.ClosestPoint( point );
            float sqDist = Vector3.SqrMagnitude( point-candidate );
            if( sqDist < shortestSqDist ){
                shortestSqDist = sqDist;
                closest = candidate;
            }
        }
        return closest;
    }

    public void InitializeGrabListener( Character source, RigidBodyGrabListener grabListener ){
        grabListener.onGrab += delegate( GrabContext grab ){    /// ???
            contexts.Add( grab );
        };
        grabListener.onDrop += delegate( GrabContext grab ){
            contexts.Remove( grab );
        };
        foreach( var grabbable in grabbables ){
            Grabbable._InternalSetup( source, rigidBody, grabbable, new GrabbableSettings(){ uprightOnly=0, freelyRotate=false } );
        }
    }
    
    public void Initiate(Muscle[] colleagues){
        
        if (joint.connectedBody != null)
        {
            for (int i = 0; i < colleagues.Length; i++)
            {
                if (colleagues[i].joint.GetComponent<Rigidbody>() == joint.connectedBody)
                {
                    connectedBodyTarget = colleagues[i].target;
                }
            }

            joint.autoConfigureConnectedAnchor = false;
            connectedBodyTransform = joint.connectedBody.transform;

            directTargetParent = target.parent == connectedBodyTarget;
        }

        targetParent = connectedBodyTarget != null ? connectedBodyTarget : target.parent;
        toParentSpace = Quaternion.Inverse(targetParentRotation) * parentRotation;
        localRotationConvert = Quaternion.Inverse(targetLocalRotation) * localRotation;

        // Joint space
        Vector3 forward = Vector3.Cross(joint.axis, joint.secondaryAxis).normalized;
        Vector3 up = Vector3.Cross(forward, joint.axis).normalized;

        defaultLocalRotation = localRotation;

        Quaternion toJointSpace = Quaternion.LookRotation(forward, up);
        toJointSpaceInverse = Quaternion.Inverse(toJointSpace);
        toJointSpaceDefault = defaultLocalRotation * toJointSpace;

        // Set joint params
        joint.rotationDriveMode = RotationDriveMode.Slerp;
        joint.configuredInWorldSpace = false;

        // Fix target Transforms
        defaultTargetLocalPosition = target.localPosition;
        defaultTargetLocalRotation = target.localRotation;
        targetAnimatedCenterOfMass = V3Tools.TransformPointUnscaled(target, rigidBody.centerOfMass);

        // Resetting
        if (joint.connectedBody == null)
        {
            defaultPosition = transform.localPosition;
            defaultRotation = transform.localRotation;
        }
        else
        {
            defaultPosition = joint.connectedBody.transform.InverseTransformPoint(transform.position);
            defaultRotation = Quaternion.Inverse(joint.connectedBody.transform.rotation) * transform.rotation;
        }

        Read();

        initiated = true;
    }

    public void FixTargetTransforms()
    {
        if (!initiated) return;

        target.localRotation = defaultTargetLocalRotation;
        target.localPosition = defaultTargetLocalPosition;
    }

    public void _InternalReset(){
        strengthLimit._InternalReset();
        foreach( var grabbable in grabbables ) grabbable._InternalReset();
    }

    public void SetPhysicMaterial( PhysicMaterial mat ){
        foreach( var collider in colliders ){
            collider.material = mat;
        }
    }

    // Reset the Transform to the default state. This is necessary for activating/deactivating the ragdoll without messing it up
    public void RestoreTransform()
    {
        if (!initiated){
            Debugger.LogWarning("Muscle not initiated");
            return;
        }
        if (joint == null){
            Debugger.LogWarning("Muscle joint is missing");
            return;
        }
        
        if (joint.connectedBody == null)
        {
            transform.localPosition = defaultPosition;
            // transform.localRotation = defaultRotation;
        }
        else
        {
            transform.position = joint.connectedBody.transform.TransformPoint(defaultPosition);
            // transform.rotation = joint.connectedBody.transform.rotation * defaultRotation;
        }

        lastRotationDamper = -1f;
    }

    // Moves and rotates the muscle to match it's target
    public void MoveToTarget()
    {
        // Moving rigidbodies only won't animate the pose. MoveRotation does not work on a kinematic Rigidbody that is connected to another by a Joint
        transform.position = target.position;
        transform.rotation = target.rotation;
        rigidBody.MovePosition(transform.position);
        rigidBody.MoveRotation(transform.rotation);
    }

    public void ClearVelocities()
    {
        rigidBody.velocity = Vector3.zero;
        rigidBody.angularVelocity = Vector3.zero;

        targetVelocity = Vector3.zero;
        targetAnimatedCenterOfMass = V3Tools.TransformPointUnscaled(target, rigidBody.centerOfMass);
    }

    public void Read()
    {
        Vector3 tAM = V3Tools.TransformPointUnscaled(target, rigidBody.centerOfMass);
        if (Time.deltaTime > 0f) targetVelocity = (tAM - targetAnimatedCenterOfMass) / Time.deltaTime;
        targetAnimatedCenterOfMass = tAM;
        if (joint.connectedBody != null)
        {
            targetAnimatedRotation = targetLocalRotation * localRotationConvert;
        }

        targetAnimatedWorldRotation = target.rotation;
    }

    // public void Update(float pinWeightMaster, float muscleWeightMaster, float muscleSpring, float muscleDamper, bool angularPinning)
    // {
    //     Pin(pinWeightMaster, 4, 0f, angularPinning);
    //     MuscleRotation(muscleWeightMaster, muscleSpring, muscleDamper);
    // }

    public void Pin(float pinWeightMaster, bool forcePin ){   
        positionOffset = targetAnimatedCenterOfMass - rigidBody.worldCenterOfMass;
        if (float.IsNaN(positionOffset.x)) positionOffset = Vector3.zero;

        float w = pinWeightMaster * strengthLimit.value;

        // if (w <= 0f) return;
        // w = Mathf.Pow(w, pinPow);

        if (Time.deltaTime > 0f) positionOffset /= Time.deltaTime;
        Vector3 force = -rigidBody.velocity + targetVelocity + positionOffset;
        force *= w;
        force /= 1f + positionOffset.sqrMagnitude * 0.02f;
        
        if( forcePin ) rigidBody.AddForce( force*pinForceMult, ForceMode.VelocityChange);

        // Angular pinning
        // if (angularPinning)
        // {
            Vector3 torque = PhysXTools.GetAngularAcceleration(rigidBody.rotation, targetAnimatedWorldRotation);

            torque -= rigidBody.angularVelocity;
            torque *= w;
            rigidBody.AddTorque(torque, ForceMode.VelocityChange);
        // }
    }

    public void MuscleRotation(float weight, float muscleSpring, float muscleDamper)
    {
        float w = muscleSpring * strengthLimit.value * weight * 10f;

        if (joint.connectedBody == null) w = 0f;
        else if (w > 0f) joint.targetRotation = LocalToJointSpace(targetAnimatedRotation);

        float d = muscleDamper * strengthLimit.value * weight;

        if (w == lastJointDriveRotationWeight && d == lastRotationDamper) return;
        lastJointDriveRotationWeight = w;
        lastRotationDamper = d;
        slerpDrive.positionSpring = w;
        slerpDrive.maximumForce = Mathf.Max(w, d);
        slerpDrive.positionDamper = d;
        joint.slerpDrive = slerpDrive;
    }

    public void Map(float masterWeight)
    {
        float w = masterWeight;
        if( w <= 0f || !rigidBody.gameObject.activeInHierarchy ) return;

        if( w >= 1f ){
            target.rotation = transform.rotation;

             //ONLY MAP ROOT to prevent physics changing limb distances
            if (connectedBodyTransform == null) target.position = transform.position;
            // else{
            //     Vector3 relativePosition = connectedBodyTransform.InverseTransformPoint(transform.position);
            //     target.position = connectedBodyTarget.TransformPoint(relativePosition);}
        }else{
            target.rotation = Quaternion.Lerp(target.rotation, transform.rotation, w);

            if( connectedBodyTransform == null ) target.position = Vector3.Lerp(target.position, transform.position, w);
            // else{
            //     Vector3 relativePosition = connectedBodyTransform.InverseTransformPoint(transform.position);
            //     target.position = Vector3.Lerp(target.position, connectedBodyTarget.TransformPoint(relativePosition), w);}
        }
    }
    
    public void OnPhysicsCollider( ColliderReturnFunc onCollider ){
        if( !rigidBody ) return;
        for( int i=0; i<rigidBody.transform.childCount; i++ ){
            var child = rigidBody.transform.GetChild(i);
            if( child.gameObject.layer == WorldUtil.itemsLayer ){
                var collider = child.GetComponent<Collider>();
                if( collider ) onCollider( collider );
            }
        }
    }

    // Update Joint connected anchor
    public void UpdateAnchor()
    {
        //if (state.isDisconnected) return;
        if (joint.connectedBody == null || connectedBodyTarget == null) return;
        if (directTargetParent) return;

        Vector3 anchorUnscaled = joint.connectedAnchor = InverseTransformPointUnscaled(connectedBodyTarget.position, connectedBodyTarget.rotation * toParentSpace, target.position);
        float uniformScaleF = 1f / connectedBodyTransform.lossyScale.x;

        joint.connectedAnchor = anchorUnscaled * uniformScaleF;
    }

    private Quaternion localRotation
    {
        get
        {
            return Quaternion.Inverse(parentRotation) * transform.rotation;
        }
    }

    // Get the rotation of the target
    private Quaternion targetLocalRotation
    {
        get
        {
            return Quaternion.Inverse(targetParentRotation * toParentSpace) * target.rotation;
        }
    }

    private Quaternion parentRotation
    {
        get
        {
            if (joint.connectedBody != null) return joint.connectedBody.rotation;
            if (transform.parent == null) return Quaternion.identity;
            return transform.parent.rotation;
        }
    }

    private Quaternion targetParentRotation
    {
        get
        {
            if (targetParent == null) return Quaternion.identity;
            return targetParent.rotation;
        }
    }

    // Convert a local rotation to local joint space rotation
    private Quaternion LocalToJointSpace(Quaternion localRotation)
    {
        return toJointSpaceInverse * Quaternion.Inverse(localRotation) * toJointSpaceDefault;
    }

    // Inversetransforms a point by the specified position and rotation
    private static Vector3 InverseTransformPointUnscaled(Vector3 position, Quaternion rotation, Vector3 point)
    {
        return Quaternion.Inverse(rotation) * (point - position);
    }
}

}