using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

public delegate void UpdateIKInfoCallback( out Vector3 target, out Vector3 pole, out Quaternion handRotation );



/// <summary>
/// Class for handling a CharacterIK handle. The most recent handle will be used to animate the IK while the rest will fade away and be removed.
/// </summary>
/// <example>
/// The following example makes the right arm of a character point towards the center of the map.
/// <code>
/// character.biped.rightArmIK.AddRetargeting( delegate( out Vector3 target, out Vector3 pole, out Quaternion handRotation ){
///     target = new Vector3( 0, 2, 0 );
///     pole = new Vector3( 0, 0, 0 );
///  
///     handRotation = Quaternion.identity;
/// }, out IKHandle activeHandle );
///
/// //Once you are done you can stop the influence of this retargeting with:
/// activeHandle.Kill();
/// </code>
/// </example>
public class IKHandle{
    public float weight = 0.0f;
    public readonly int priority;
    public float maxWeight = 1.0f;
    private readonly UpdateIKInfoCallback updateHandle;
    public Vector3 target = Vector3.zero;
    public Vector3 pole = Vector3.zero;
    public Quaternion endRotation = Quaternion.identity;
    public float speed = 2f;
    
    /// <summary> Check if handle is still in use </summary>
	/// <returns> true: Still allowed to influence animation. false: Is being blended away.</returns>
    public bool alive { get; private set; } = true;

    public IKHandle( UpdateIKInfoCallback _updateHandle, int _priority ){
        priority = _priority;
        updateHandle = _updateHandle;
        if( updateHandle == null ) throw new System.Exception("Update IK Info Callback cannot be null!");
    }

    public void Update(){
        updateHandle.Invoke( out target, out pole, out endRotation );
    }
    
    /// <summary>
    /// Start blending away from the animation. Once the weight reaches 0, the handle will be removed from the CharacterIK.
    /// </summary>
    public void Kill(){
        alive = false;
    }
}

/// <summary>
/// For an example using CharacterIK, see IKHandle.
/// The base class for 2 bone inverse kinematics used by Characters during the animation phase.
/// </summary>
public abstract class CharacterIK{
    
    public readonly string name;
    public readonly TwoBoneIK ik;
    public readonly Transform endBone = null;
    public readonly Transform referenceBone = null;
    public readonly float sign = 1.0f;
    public bool alive { get; private set; } = true;
    public bool hasHandles { get{ return handles.Count > 0; } }
    public LimitGroup strength { get; private set; } = new LimitGroup( null, 0, 1 );
    private float allowedIKBlend = 0;
    
    private List<IKHandle> handles = new List<IKHandle>();


    public CharacterIK( string _name, TwoBoneIK _ik, Transform _endBone, Transform _referenceBone, float _sign ){
        name = _name;
        ik = _ik;
        endBone = _endBone;
        referenceBone = _referenceBone;
        sign = _sign;
    }
    

	/// <summary>
	/// Creates an IKHandle to control when the stop the influence of this new Retargeting
	/// </summary>
	/// <param name="_updateHandle">The UpdateIKInfoCallback that supplies the information to the IK. Exception will be thrown if it is null.</param>
	/// <param name="handle">The IKHandle to control the resulting retargeting object.</param>
    public void AddRetargeting( UpdateIKInfoCallback _updateHandle, out IKHandle handle, int _priority=0 ){
        handle = new IKHandle( _updateHandle, _priority );
        int insertIndex = handles.Count;
        for( int i=0; i<handles.Count; i++ ){
            if( handles[i].priority >= _priority ){
                insertIndex = i;
                break;
            }
        }
        handles.Insert( insertIndex, handle );
    }

	/// <summary> Convert a hold space Vector3 to world space. </summary>
	/// <param name="holdSpacePos">The Vector3 to convert.</param>
	/// <returns> Vector3: The converted position.</returns>
    protected Vector3 HoldSpaceToWorld( Vector3 holdSpacePos ){
        holdSpacePos.x *= sign;
        return referenceBone.TransformPoint( holdSpacePos );
    }
    /// <summary> Convert a world space Vector3 to hold space. </summary>
	/// <param name="worldPos">The Vector3 to convert.</param>
	/// <returns> Vector3: The converted position.</returns>
    protected Vector3 WorldToHoldSpace( Vector3 worldPos ){
        Vector3 result = referenceBone.InverseTransformPoint( worldPos );
        result.x *= sign;
        return result;
    }

    /// <summary> Kill all handles in this CharacterIK. Custom CharacterIKs will be removed when all handles are removed</summary>
    public void Kill( bool fade=true ){
        alive = false;
        foreach( var handle in handles ) handle.Kill();
        if( fade ) handles.Clear();
    }

    public void Apply(){
        allowedIKBlend = Mathf.MoveTowards( allowedIKBlend, strength.value, Time.deltaTime*2.0f );
        //normalize weights first
        for( int i=handles.Count; i-->0; ){
            var handle = handles[i];
            try{
                handle.Update();
            }catch( System.Exception e ){
                Debugger.LogError(e.ToString());
                handle.Kill();
            }
            if( handle.alive ){
                handle.weight = Mathf.Clamp( handle.weight+Time.deltaTime*handle.speed, 0, handle.maxWeight );
            }else{
                handle.weight = Mathf.Clamp( handle.weight-Time.deltaTime*handle.speed, 0, handle.maxWeight );
            }
            
            if( handle.weight == 0 ){
                if( !handle.alive ) handles.RemoveAt(i);
                continue;
            }
            var weight = Tools.EaseInOutQuad( handle.weight*allowedIKBlend );
            
            Quaternion oldP0Rotation = ik.p0.rotation;
            Quaternion oldP1Rotation = ik.p1.rotation;
            ik.Solve( handle.target, handle.pole );
            ik.p0.rotation = Quaternion.LerpUnclamped( oldP0Rotation, ik.p0.rotation, weight );
            ik.p1.rotation = Quaternion.LerpUnclamped( oldP1Rotation, ik.p1.rotation, weight );
            endBone.rotation = Quaternion.LerpUnclamped( endBone.rotation, handle.endRotation, weight );

            Tools.DrawDiagCross(handle.pole, Color.magenta, 0.05f*i, Time.fixedDeltaTime );
            Tools.DrawCross(handle.target, Color.magenta, 0.05f*i, Time.fixedDeltaTime );
        }
    }
}


public class ArmIK: CharacterIK{

    public readonly Character character;
    public Transform upperArm { get{ return ik.p0; } }
    public Transform arm { get{ return ik.p1; } }
    public Transform hand { get{ return endBone; } }
    public Transform shoulder = null;
    private float shoulderIKUpRange = 75.0f;
    private float shoulderIKForwardRange = 35.0f;
    private Quaternion shoulderLocalBaseRotation;
    public bool rightSide { get{ return sign==1.0f; } }
    public Rigidbody rigidBody { get{ return rightSide ? character.biped.rightHand.rigidBody : character.biped.leftHand.rigidBody; } }
    public float armLength { get{ return ik.r0+ik.r1; } }

    public ArmIK( Character _character, TwoBoneIK _ik, Transform _endBone, Transform _referenceBone, float _sign, Transform _shoulder )
            :base( _sign==1?"right":"left"+" arm", _ik, _endBone, _referenceBone, _sign ){
        character = _character;
        shoulder = _shoulder;
    }
}

public class SpineIK: CharacterIK{

    public Transform bone0 { get{ return ik.p0; } }
    public Transform bone1 { get{ return ik.p1; } }
    public Transform bone2 { get{ return endBone; } }


    public static SpineIK CreateSpineIK( BipedProfile profile ){
        TwoBoneIK ik;
        Transform endBone;
        ik = new TwoBoneIK(
            profile[ BipedBone.LOWER_SPINE ].transform,
            Quaternion.Euler(-90,0,180),
            profile[ BipedBone.UPPER_SPINE ].transform,
            Quaternion.Euler(90,0,0),
            profile[ BipedBone.NECK ].transform
        );
        endBone = profile[ BipedBone.HEAD ].transform;
        return new SpineIK( ik, endBone, profile[ BipedBone.HIPS ].transform );
    }

    public SpineIK( TwoBoneIK _ik, Transform _endBone, Transform _referenceBone ):base( "spine", _ik, _endBone, _referenceBone, 1.0f ){
    }
}

public class LegIK: CharacterIK{

    public Transform bone0 { get{ return ik.p0; } }
    public Transform bone1 { get{ return ik.p1; } }
    public Transform bone2 { get{ return endBone; } }


    public static LegIK CreateLegIK( BipedProfile profile, bool rightLeg ){
        TwoBoneIK ik;
        Transform endBone;
        ik = new TwoBoneIK(
            profile[ rightLeg ? BipedBone.UPPER_LEG_R : BipedBone.UPPER_LEG_L ].transform,
            Quaternion.Euler(90,0,0),
            profile[ rightLeg ? BipedBone.LEG_R : BipedBone.LEG_L ].transform,
            Quaternion.Euler(-90,180,0),
            profile[ rightLeg ? BipedBone.FOOT_R : BipedBone.FOOT_L ].transform
        );
        endBone = profile[ rightLeg ? BipedBone.FOOT_R : BipedBone.FOOT_L ].transform;
        return new LegIK( rightLeg?"right":"left"+" leg", ik, endBone, profile[ BipedBone.HIPS ].transform );
    }

    public LegIK( string _name, TwoBoneIK _ik, Transform _endBone, Transform _referenceBone )
            :base( _name, _ik, _endBone, _referenceBone, 1.0f ){
    }
}

}
