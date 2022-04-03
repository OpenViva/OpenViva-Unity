using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace viva{

public delegate void GrabContextReturnFunc( GrabContext grabContext );

public class GrabContext : MonoBehaviour{


    public static GrabContext CreateCrabContext( Grabbable _grabbable, Grabber _grabber, Character source, bool smoothStart=false ){
        if( _grabbable == null || _grabber == null ){
            Debug.LogError("Grabbable or Grabber cannot be null");
            return null;
        }
        var rigidBody = _grabbable.GetComponentInParent<Rigidbody>();
        if( rigidBody == null ){
            Debug.LogError("Grabbable is not a parent of a Rigidbody!");
            return null;
        }

        var grabSound = Sound.Create( _grabber.rigidBody.centerOfMass, _grabber.rigidBody.transform );
        grabSound.Play( Sound.Load("generic","pickup") );

        var oldPos = _grabber.rigidBody.transform.position;
        var oldRot = _grabber.rigidBody.transform.rotation;

        if( !_grabbable.GetBestGrabPose( out Vector3 grabPos, out Quaternion grabRot, _grabber ) ){
            Debug.LogError("Could not apply best grab pose!");
            return null;
        }
        _grabber.ApplyGrabPose( grabPos, grabRot );

        var connectedBody = _grabber.rigidBody;

        var context = rigidBody.gameObject.AddComponent<GrabContext>();
        context.grabber = _grabber;
        context.grabbable = _grabbable;
        context.rigidBody = rigidBody;

        var configJoint = rigidBody.gameObject.AddComponent<ConfigurableJoint>();
        context.configJoint = configJoint;
        configJoint.autoConfigureConnectedAnchor = false;
        configJoint.connectedBody = connectedBody;
        configJoint.connectedAnchor = connectedBody.transform.InverseTransformPoint( _grabber.worldGrabCenter );
        configJoint.anchor = rigidBody.transform.InverseTransformPoint( _grabber.worldGrabCenter );
        configJoint.massScale = 8;  //hands are heavier

        context.source = source;
        context.startRigidRotation = rigidBody.transform.rotation;
        context.startConnectedRotation = connectedBody.transform.rotation;
        context.SetTargetRotation( rigidBody.transform.rotation );
        
        if( _grabbable.settings != null && _grabbable.settings.freelyRotate ){
            configJoint.angularYMotion = ConfigurableJointMotion.Locked;
            configJoint.angularZMotion = ConfigurableJointMotion.Locked;
        }else{
            configJoint.rotationDriveMode = RotationDriveMode.Slerp;
        }

        var distanceToGrab = Vector3.Distance( oldPos, _grabber.rigidBody.transform.position );

        if( source && source.isBiped ){
            if( source.biped.rightHandGrabber == _grabber ){
                context.AnimateHand( source.biped.rightHand.target );
            }else if( source.biped.leftHandGrabber == _grabber ){
                context.AnimateHand( source.biped.leftHand.target );
            }
        }
  
        _grabber.rigidBody.transform.position = oldPos;
        _grabber.rigidBody.transform.rotation = oldRot;

        context.StartCoroutine( context.FadeIn( smoothStart ? distanceToGrab : 0.0f ) );

        //callbacks should be at the end to prevent race conditions
        var pickupEventCallback = rigidBody.gameObject.GetComponent<RigidBodyGrabListener>();
        if( pickupEventCallback ){
            pickupEventCallback.onGrab?.Invoke( context );
        }

        _grabber.onGrabbed.Invoke( context );
        if( context.IsValid() ) _grabbable.onGrabbed.Invoke( context );
        return context;
    } 

    private ConfigurableJoint configJoint;
    private Quaternion startRigidRotation;
    private Quaternion startConnectedRotation;
    public Grabber grabber { get; private set; }
    public Grabbable grabbable { get; private set; }
    public Rigidbody rigidBody { get; private set; }
    public Character source { get; private set; }
    private Quaternion startDeltaRotation;
    private Vector3 startDeltaPosition;
    private Quaternion targetRotation;
    private Transform animateTransform;
    private Vector3 oldAnimSourcePos;
    private float strength;
    public float timeStarted { get; private set; }

    private void Awake(){
        timeStarted = Time.time;
    }
    
    private void AnimateHand( Transform _animateTransform ){
        if( _animateTransform == null ) return;
        animateTransform = _animateTransform;
        oldAnimSourcePos = animateTransform.localPosition;
        startDeltaRotation = Quaternion.Inverse( startRigidRotation )*startConnectedRotation;
        startDeltaPosition = rigidBody.transform.InverseTransformPoint( grabber.rigidBody.transform.position );
        source.mainAnimationLayer.player.onModifyAnimation += RestoreGrabAnimation;
        source.ragdoll.onPostMap += ForceGrabAnimation;
    }

    private void RestoreGrabAnimation(){
        animateTransform.localPosition = oldAnimSourcePos;
    }

    private void ForceGrabAnimation(){
        if( !configJoint ) return;

        var targetRot = configJoint.transform.rotation*startDeltaRotation;
        var targetPos = Vector3.LerpUnclamped( animateTransform.position, rigidBody.transform.TransformPoint( startDeltaPosition ), strength );

        Quaternion oldHandRot = animateTransform.rotation;
        animateTransform.position = targetPos;
        animateTransform.rotation = Quaternion.LerpUnclamped( oldHandRot, targetRot, strength );
    }
    
    private IEnumerator FadeIn( float duration ){
        if( duration > 0 ){
            float timer = 0;
            while( timer < duration ){
                if( !configJoint ) yield break;

                timer += Time.deltaTime;
                var alpha = Mathf.Clamp01( timer/duration );
                SetStrength( alpha );
                CheckEnhancePositionConstraint();

                yield return new WaitForFixedUpdate();
            }
        }
        SetStrength(1f);
    }

    private void LockPosition(){
        configJoint.xMotion = ConfigurableJointMotion.Limited;
        configJoint.yMotion = ConfigurableJointMotion.Limited;
        configJoint.zMotion = ConfigurableJointMotion.Limited;
    }

    private bool CheckEnhancePositionConstraint(){
        if( configJoint.yMotion != ConfigurableJointMotion.Free ) return true;
        if( !configJoint.connectedBody ) return true;

        var delta = Vector3.SqrMagnitude( configJoint.connectedBody.transform.TransformPoint( configJoint.connectedAnchor )-rigidBody.transform.TransformPoint( configJoint.anchor ) );
        if( delta < 0.015f ){
            return true;
        }
        return false;
    }

    public void SetStrength( float _strength ){
        if( !configJoint || !rigidBody ) return;
        var drive = configJoint.slerpDrive;

        drive.positionSpring = _strength*1250;
        drive.positionDamper = _strength*50;
        configJoint.slerpDrive = drive;

        drive.positionSpring = _strength*10000;
        drive.positionDamper = _strength*300;
        drive.maximumForce = 200f;

        configJoint.xDrive = drive;
        configJoint.yDrive = drive;
        configJoint.zDrive = drive;
        strength = _strength;
        if( strength == 1f ) LockPosition();
    }

    public void SetTargetRotation( Quaternion worldRotation ){
        if( configJoint == null || configJoint.connectedBody == null ) return;
        targetRotation = configJoint.CalculateTargetRotation( startConnectedRotation*( Quaternion.Inverse( configJoint.connectedBody.transform.rotation )*worldRotation ), startRigidRotation );
        configJoint.targetRotation = targetRotation;
    }

    public void SetAnchor( Vector3 localPos ){
        if( configJoint == null ) return;
        configJoint.anchor = localPos;
    }

    public bool IsValid(){
        return configJoint != null && configJoint.connectedBody != null;
    }

    public void _InternalCleanup(){
        if( rigidBody ){
            var pickupEventCallback = rigidBody.gameObject.GetComponent<RigidBodyGrabListener>();
            if( pickupEventCallback ){
                pickupEventCallback.onDrop?.Invoke( this );
            }
        } 
        StopAllCoroutines();
        if( configJoint ){
            GameObject.Destroy( configJoint ); //destroy GrabContext
            configJoint = null;
        }
        if( animateTransform ){
            animateTransform.localPosition = oldAnimSourcePos;
            source.mainAnimationLayer.player.onModifyAnimation -= RestoreGrabAnimation;
            source.ragdoll.onPostMap -= ForceGrabAnimation;
        }

        if( grabbable ) grabbable.onReleased.Invoke( this );
        if( grabber ) grabber.onReleased.Invoke( this );
    }
}


}