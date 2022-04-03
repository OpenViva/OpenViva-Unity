using System.Collections;
using UnityEngine;
using viva;


public class BallItem: VivaScript{

    private Item item;
    private ItemUserListener itemUserListener;

    public BallItem( Item _item ){
        item = _item;

        itemUserListener = new ItemUserListener( item, this, OnPickedUp, OnDropped );
    }

    private void OnPickedUp( Character newUser, GrabContext context ){
        if( newUser.isPossessed ){
            if( newUser.biped.rightHandGrabber == context.grabber ){
                newUser.GetInput( Input.RightAction ).onDown.AddListener( this, Throw );
            }else{
                newUser.GetInput( Input.LeftAction ).onDown.AddListener( this, Throw );
            }
        }
        item.RemoveAttribute( "thrown" );
    }

    private void OnDropped( Character oldUser, GrabContext context ){
        if( oldUser.isPossessed ){
            if( oldUser.biped.rightHandGrabber == context.grabber ){
                oldUser.GetInput( Input.RightAction ).onDown.RemoveListener( this, Throw );
            }else{
                oldUser.GetInput( Input.LeftAction ).onDown.RemoveListener( this, Throw );
            }
            if( oldUser.isPossessedByVR && item.rigidBody.velocity.sqrMagnitude > 1f ){
                Throw();
            }
        }
    }

    private void Throw(){
        item.DropByCharacter( itemUserListener.character );
        item.rigidBody.AddForce( Camera.main.transform.forward*7f+Vector3.up, ForceMode.VelocityChange );
        item.AddAttribute( "thrown" );
    }
} 