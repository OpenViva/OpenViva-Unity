using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using System.IO;
using System.Linq;


namespace viva{

/// <summary>
/// The class used to represent all in-game characters
/// </summary>
public partial class BipedRagdoll: Ragdoll{

    private bool autoLook = false;
    public Target lookTarget { get; private set; } = new Target();
    private int grabbableCheckIndex = 0;
    private float mostInterest = 0;
    private float interestResetTimer = 0;
    private float holdInterestTimer = 0;
    private Rigidbody awarenessRigidBody = null;


    private void ResetAutoLook(){
        lookTarget._InternalReset();
        lookTarget.onChanged._InternalAddListener( headLookAt.ResetHeadVelocity );
        lookTarget.onChanged._InternalAddListener( OnLookTargetChanged );

        autoLook = false;
        awarenessRigidBody = null;
        SetAutoLook( !rightHandGrabber.character.isPossessed );
    }

    private void UpdateAwareness(){
        
        holdInterestTimer -= Time.deltaTime;
        if( holdInterestTimer < 0 ){
            interestResetTimer -= Time.deltaTime;
            if( interestResetTimer < 0 ){
                interestResetTimer = 0.75f+Random.value*5.0f;
                mostInterest = Mathf.NegativeInfinity;
            }
            Rigidbody newTarget = null;
            for( int i=0; i<8; i++ ){
                var grabbable = vision.GetGrabbableAtMemorySlot( grabbableCheckIndex++%Vision.grabbableMemoryMax );
                if( grabbable == null || !grabbable.enabled ) continue; //ignore disabled objects

                float interest = CalculateInterest( grabbable );
                if( interest > mostInterest ){
                    mostInterest = interest;
                    var parentCharacter = grabbable.parentCharacter;
                    if( parentCharacter && parentCharacter.isBiped && Random.value < 0.333f )
                        newTarget = parentCharacter.biped.head.rigidBody;
                    else
                        newTarget = grabbable.rigidBody;
                }
            }
            if( newTarget ){
                holdInterestTimer = 0.5f;
                mostInterest *= 1.05f;
                awarenessRigidBody = newTarget;
                lookTarget.SetTargetRigidBody( awarenessRigidBody );
            }
        }
    }

    public Character GetLookAtCharacter(){
        var rigidBody = lookTarget.target as Rigidbody;
        if( !rigidBody ) return null;
        return Util.GetCharacter( rigidBody );
    }

    private float CalculateInterest( Grabbable grabbable ){
        if( grabbable.rigidBody == null ) return 0; //imported items have no rigidBody
        
        var sqVel = grabbable.rigidBody.velocity.sqrMagnitude;

        float interestPad;
        if( grabbable.parentCharacter ){
            if( grabbable.parentCharacter == this ){
                return Mathf.NegativeInfinity;  //do not allow looking at self
            }
            //interest based on height from the floor
            interestPad = ( grabbable.rigidBody.position.y-grabbable.parentCharacter.model.armature.position.y )*16.0f;
        }else if( grabbable.parentItem ){
            if( grabbable.parentItem.IsBeingGrabbedByCharacter( rightHandGrabber.character ) ){
                return Mathf.NegativeInfinity;  //do not allow looking at items held by self
            }else{
                interestPad = 2;    //items receive a natural larger interest value
            }
        }else{
            interestPad = 0;
        }
        return sqVel+interestPad;
    }

    private void OnLookTargetChanged(){
        if( lookTarget.target as Rigidbody == awarenessRigidBody ) return;
        holdInterestTimer = 1f;
        mostInterest *= 1.5f;
        if( Random.value > 0.5f ) eyeLookAt.Blink();
    }

    private void SetAutoLook( bool _autoLook ){
        if( _autoLook == autoLook ) return;
        autoLook = _autoLook;
        if( autoLook ){
            rightHandGrabber.character.autonomy.onFixedUpdate._InternalAddListener( UpdateAwareness );
        }else{
            rightHandGrabber.character.autonomy.onFixedUpdate._InternalRemoveListener( UpdateAwareness );
        }
    }
}

}