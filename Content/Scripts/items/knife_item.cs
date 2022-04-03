using System.Collections;
using UnityEngine;
using viva;


public class KnifeItem: VivaScript{

    private Item knife;
    private ItemUserListener userListener;

    public KnifeItem( Item _item ){
        knife = _item;

        userListener = new ItemUserListener( knife, this, OnGrabbed, OnDropped );
    }

    private void SetupAnimations( Character character ){

        var stand = character.animationSet.GetBodySet("stand");

        var knifeRight = stand.Single( "knife right", "stand_kill_chicken_right", false );
        knifeRight.nextState = stand["idle"];
        knifeRight.curves[ BipedRagdoll.rightArmID ] = new Curve(0,0.1f);
        knifeRight.curves[ BipedRagdoll.leftArmID ] = new Curve(0,0.1f);

        var knifeLeft = stand.Single( "knife left", "stand_kill_chicken_left", false );
        knifeLeft.nextState = stand["idle"];
        knifeLeft.curves[ BipedRagdoll.rightArmID ] = new Curve(0,0.1f);
        knifeLeft.curves[ BipedRagdoll.leftArmID ] = new Curve(0,0.1f);
    }

    private void OnGrabbed( Character newOwner, GrabContext context ){
        SetupAnimations( newOwner );
        if( newOwner.isPossessedByKeyboard ){
            newOwner.GetInput( Input.LeftAction ).onDown.AddListener( this, AttemptKillChicken );
            newOwner.GetInput( Input.RightAction ).onDown.AddListener( this, AttemptKillChicken );
        }else{
            var checkForChicken = new Timer( newOwner.autonomy, 1.5f );
            checkForChicken.onSuccess += delegate{
                AttemptKillChicken();
                checkForChicken.Reset();
            };
            checkForChicken.StartConstant( this, "chicken check timer" );
        }
    }

    private void OnDropped( Character oldOwner, GrabContext context ){
        if( oldOwner.isPossessedByKeyboard ){
            oldOwner.GetInput( Input.LeftAction ).onDown.RemoveListener( this, AttemptKillChicken );
            oldOwner.GetInput( Input.RightAction ).onDown.RemoveListener( this, AttemptKillChicken );
        }else{
            oldOwner.autonomy.RemoveTask( "chicken check timer" );
        }
    }

    private void AttemptKillChicken(){
		if( !userListener.character ) return;

        string side = null;
        if( userListener.character.biped.rightHandGrabber.IsGrabbingCharacter("chicken") ){
            side = "left";
        }else if( userListener.character.biped.leftHandGrabber.IsGrabbingCharacter("chicken") ){
            side = "right";
        }
        if( side != null ){
            var swingKnife = new PlayAnimation( userListener.character.autonomy, "stand", "knife "+side, true, 1 );
            swingKnife.Start( this, "knife "+side );
        }
    }
} 