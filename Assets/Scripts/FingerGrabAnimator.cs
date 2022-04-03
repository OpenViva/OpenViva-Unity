using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.Jobs;
using Unity.Collections;


namespace viva{


public class FingerGrabAnimator{

    public readonly Grabber grabber;
    private Model model;
    private BipedBone startThumb;
    private float fingerRadius;
    private float[] fingerLengths = new float[5];
    private float[] fingerBends = new float[15];
    private List<Model.PoseInfo> restFingerPose;
    private bool rightHand;
    private object animateTarget;
    private Grabbable.Type? currentAnimateType = null;


    public FingerGrabAnimator( Grabber _grabber, Model _model, bool _rightHand ){
        if( _grabber == null ) throw new System.Exception("Cannot create FingerGrabAnimator from a null Grabber!");
        if( _model == null ) throw new System.Exception("Cannot create FingerGrabAnimator without a Model!");
        grabber = _grabber;
        model = _model;
        rightHand = _rightHand;
        startThumb = rightHand ? BipedBone.THUMB0_R : BipedBone.THUMB0_L;

        for( int i=0; i<fingerLengths.Length; i++ ){
            var fingerBone = model.bipedProfile[ startThumb+i*6+2 ];
            if( fingerBone != null ) fingerLengths[i] = fingerBone.transform.localPosition.magnitude;
        }
        fingerRadius = fingerLengths[0]*0.3f;

        restFingerPose = model.ExtractFromSpawnPose( rightHand ? BipedMask.FINGERS_R : BipedMask.FINGERS_L );
    }

    private bool IsFingerValid( int fingerIndex ){
        return fingerLengths[ fingerIndex ] > 0;
    }

    public void ResetFingers(){
        foreach( var pose in restFingerPose ){
            if( pose != null ) pose.target.localRotation = pose.localRot;
        }
    }

    public void AnimateGripFingers( float thumb, float index, float middle, float ring, float pinky ){
        if( currentAnimateType.HasValue ) return;
        GripFingerFree( 0, rightHand ? Vector3.LerpUnclamped( new Vector3( 45, 20, 0 ), new Vector3( 45, 90, 90 ), grabber.grip ) : Vector3.LerpUnclamped( new Vector3( 45, -20, 0 ), new Vector3( 45, -90, -90 ), grabber.grip ), thumb );
        GripFingerFree( 1, new Vector3( 10, 0, 0 ), index );
        GripFingerFree( 2, new Vector3( 0, 0, 0 ), middle );
        GripFingerFree( 3, new Vector3( -10, 0, 0 ), ring );
        GripFingerFree( 4, new Vector3( -20, 0, 0 ), pinky );
    }

    public void SetupAnimation( Grabbable grabbable ){
        if( grabbable == null ) return;
        StopAnimation();
        currentAnimateType = grabbable.type;

        switch( grabbable.type ){
        case Grabbable.Type.CYLINDER:
            animateTarget = grabbable.collider as CapsuleCollider;
            grabber.character.ragdoll.onPostMap += AnimateOnCylinder;
            break;
        case Grabbable.Type.BOX:
            animateTarget = grabbable.collider as BoxCollider;
            grabber.character.ragdoll.onPostMap += AnimateOnBox;
            break;
        }
    }

    public void StopAnimation(){
        if( !currentAnimateType.HasValue ) return;
        switch( currentAnimateType.Value ){
        case Grabbable.Type.CYLINDER:
            grabber.character.ragdoll.onPostMap -= AnimateOnCylinder;
            break;
        case Grabbable.Type.BOX:
            grabber.character.ragdoll.onPostMap -= AnimateOnBox;
            break;
        }
        currentAnimateType = null;
        ResetFingers();
    }

    private void AnimateOnBox(){
        GripFingerBox( 1, Vector3.zero, !rightHand, true );
        GripFingerBox( 2, Vector3.zero, !rightHand, true );
        GripFingerBox( 3, Vector3.zero, !rightHand, true );
        GripFingerBox( 4, Vector3.zero, !rightHand, true );
    }

    private void AnimateOnCylinder(){
        GripFingerCylinder( 0, rightHand ? new Vector3( 0, 160, 45 ) : new Vector3( 0, 200, -45 ), !rightHand, false );
        GripFingerCylinder( 1, Vector3.zero, !rightHand, true );
        GripFingerCylinder( 2, Vector3.zero, !rightHand, true );
        GripFingerCylinder( 3, Vector3.zero, !rightHand, true );
        GripFingerCylinder( 4, Vector3.zero, !rightHand, true );
    }

    private void GripFingerBox( int fingerIndex, Vector3 startLocalEuler, bool clockwise, bool fixBrokenFinger ){
        if( !IsFingerValid( fingerIndex ) ) return;

        float fingerLength = fingerLengths[ fingerIndex ];
        var boneIndex = startThumb+fingerIndex*6;
        var sign = System.Convert.ToSingle(clockwise)*2-1;
        for( int i=0; i<3; i++ ){
            var bone = model.bipedProfile[ boneIndex+i*2 ].transform;
            bone.localEulerAngles = startLocalEuler;

            Vector3 nextBonePos;
            if( i<2 ){
                nextBonePos = model.bipedProfile[ boneIndex+i*2+2 ].transform.position;
            }else{
                nextBonePos = bone.position+bone.up*fingerLength/3f;
            }

            var newBend = GetTargetBoxPitch( bone, nextBonePos );
            newBend *= i==0 ? 1 : -1;
            newBend *= sign;
            float currentBend = fingerBends[ fingerIndex*3+i ];
            if( newBend.HasValue ){
                if( !clockwise ) currentBend = Mathf.Max( currentBend, newBend.Value );
                else currentBend = Mathf.Min( currentBend, newBend.Value );
                currentBend = Mathf.MoveTowards( currentBend, newBend.Value, Time.deltaTime*900.0f );
            }else{
                currentBend = Mathf.LerpUnclamped( currentBend, 0, Time.deltaTime*16.0f );
            }
            //fix broken finger
            if( i >= 1 && fixBrokenFinger ){
                if( ( !clockwise && currentBend > 0 ) || ( clockwise && currentBend < 0 ) ){
                    var oldBone = model.bipedProfile[ startThumb+fingerIndex*6+(i-1)*2 ].transform;
                    float oldBend = oldBone.localEulerAngles.z;
                    oldBend += currentBend/2;
                    oldBone.localEulerAngles = new Vector3( 0, 0, oldBend );
                    oldBend = fingerBends[ fingerIndex*3+(i-1) ];
                    currentBend = 0;
                }
            }

            fingerBends[ fingerIndex*3+i ] = currentBend;
            bone.localEulerAngles = new Vector3( startLocalEuler.x, startLocalEuler.y, startLocalEuler.z+currentBend*grabber.grip );
            startLocalEuler = Vector3.zero;
        }
    }

    private void GripFingerCylinder( int fingerIndex, Vector3 startLocalEuler, bool clockwise, bool fixBrokenFinger ){
        if( !IsFingerValid( fingerIndex ) ) return;
        
        float fingerLength = fingerLengths[ fingerIndex ];
        for( int i=0; i<3; i++ ){
            var bone = model.bipedProfile[ startThumb+fingerIndex*6+i*2 ].transform;
            bone.localEulerAngles = startLocalEuler;
            var newBend = GetTargetCylinderPitch( bone, fingerLength, clockwise );
            float currentBend = fingerBends[ fingerIndex*3+i ];
            if( newBend.HasValue ){
                if( !clockwise ) currentBend = Mathf.Max( currentBend, newBend.Value );
                else currentBend = Mathf.Min( currentBend, newBend.Value );
                currentBend = Mathf.MoveTowards( currentBend, newBend.Value, Time.deltaTime*900.0f );
            }else{
                currentBend = Mathf.LerpUnclamped( currentBend, 0, Time.deltaTime*16.0f );
            }
            //fix broken finger
            if( i >= 1 && fixBrokenFinger ){
                if( ( !clockwise && currentBend > 0 ) || ( clockwise && currentBend < 0 ) ){
                    var oldBone = model.bipedProfile[ startThumb+fingerIndex*6+(i-1)*2 ].transform;
                    float oldBend = oldBone.localEulerAngles.z;
                    oldBend += currentBend/2;
                    oldBone.localEulerAngles = new Vector3( 0, 0, oldBend );
                    oldBend = fingerBends[ fingerIndex*3+(i-1) ];
                    currentBend = 0;
                }
            }

            fingerBends[ fingerIndex*3+i ] = currentBend;
            bone.localEulerAngles = new Vector3( startLocalEuler.x, startLocalEuler.y, startLocalEuler.z+currentBend*grabber.grip );
            startLocalEuler = Vector3.zero;
        }
    }

    private void GripFingerFree( int fingerIndex, Vector3 startLocalEuler, float grip ){
        if( !IsFingerValid(fingerIndex ) ) return;

        float gripBend = rightHand ? -90.0f : 90.0f;
        for( int i=0; i<3; i++ ){
            var bone = model.bipedProfile[ startThumb+fingerIndex*6+i*2 ].transform;
            bone.localEulerAngles = startLocalEuler;
            bone.localEulerAngles = new Vector3( startLocalEuler.x, startLocalEuler.y, startLocalEuler.z+gripBend*grip );
            startLocalEuler = Vector3.zero;
        }
    }

    private float? GetTargetCylinderPitch( Transform bone, float fingerLength, bool clockwise ){
        
        var cc = animateTarget as CapsuleCollider;
        if( cc == null ) return null;
        
        var up = cc.transform.TransformDirection( Tools.CapsuleDirectionToVector3( cc.direction ) );
        var fingerPlane = new Plane( bone.forward, bone.transform.position );
        Ray ray;
        float ellipseMultApprox = Vector3.Dot( up, fingerPlane.normal );
        if( ellipseMultApprox >= 0 ){
            ray = new Ray( cc.transform.position, -up );
        }else{
            ellipseMultApprox = -ellipseMultApprox;
            ray = new Ray( cc.transform.position, up );
        }
        fingerPlane.Raycast( ray, out float enter );
        var intersect = Tools.CircleCircleIntersection(
            Vector2.zero,
            bone.InverseTransformPoint( ray.origin+ray.direction*enter ),
            fingerLength,
            cc.radius*cc.transform.lossyScale.x/bone.lossyScale.x+fingerRadius,
            !clockwise
        );
        if( !intersect.HasValue ) return null;

        Vector3 near = bone.TransformPoint( intersect.Value );
        Tools.DrawCross( near, Color.cyan, 0.005f, Time.fixedDeltaTime );

        return Mathf.Atan2( -intersect.Value.x, intersect.Value.y )*Mathf.Rad2Deg*( ellipseMultApprox*ellipseMultApprox );
    }

    private float? GetTargetBoxPitch( Transform bone, Vector3 nextBonePos ){
        
        var bc = animateTarget as BoxCollider;
        if( bc == null ) return null;
        var closest = Tools.ClosestPointOnBoxColliderSurface( nextBonePos, bc );
        return Vector3.Angle( bone.transform.up, closest-bone.transform.position );
    }
}


}