using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using viva;


public class Give: VivaScript{

	private readonly Character character;

	private int itemIndex;


	public Give( Character _character ){
		character = _character;

		SetupAnimations( character );
		
		character.onGesture.AddListener( this, ListenForGiveGesture );
    }

    private void ListenForGiveGesture( string gesture, Character source ){
        if( gesture == "give" ){
			if( character.autonomy.HasTag("idle") ) GiveItem( source );
        }
	}

	public void GiveItem( Character target ){

		var items = character.biped.rightHandGrabber.GetAllItems();
		items.AddRange( character.biped.leftHandGrabber.GetAllItems() );
		
		if( items.Count == 0 ) return;
		itemIndex = (itemIndex+1)%items.Count;

		var item = items[ itemIndex ];
		var side = character.biped.rightHandGrabber.IsGrabbing( item ) ? "right":"left";
		var giveAnim = new PlayAnimation( character.autonomy, "reach "+side, "reach loop", true, 2, false );

		var waitForDrop = new Task( character.autonomy );
		waitForDrop.onFixedUpdate += delegate{
			if( !target ){
				giveAnim.Fail("Target went missing");
			}else if( !character.IsGrabbing( item ) ){
				giveAnim.Succeed();
			}
		};
		giveAnim.AddPassive( waitForDrop);
		
		var faceTarget = new FaceTargetBody( character.autonomy );
		faceTarget.target.SetTargetCharacter( target );

		giveAnim.AddPassive( faceTarget );

		giveAnim.onSuccess += delegate{
			var reachOut = new PlayAnimation( character.autonomy, "stand", "idle", true, 0, false );
			reachOut.Start( this, "end give item "+side );
		};

		giveAnim.Start( this, "give item "+side );
	}

	private void SetupAnimations( Character character ){
		var stand = character.animationSet.GetBodySet("stand");

		var reachRight = character.animationSet.GetBodySet("reach right");

		var rightLoopRight = reachRight.Single( "reach loop", "stand_reach_out_loop_right", true );
		rightLoopRight.curves[ BipedRagdoll.headID ] = new Curve(
			new CurveKey[]{
				new CurveKey(0.3f,0),
				new CurveKey(0.4f,1),
				new CurveKey(0.6f,1),
				new CurveKey(0.7f,0),
			}
		);
		rightLoopRight.curves[ BipedRagdoll.rightArmID ] = new Curve(0.4f);

		var reachOutRight = reachRight.Single( "reach out", "stand_reach_out_end_right", false, 0.8f );
		reachOutRight.curves[ BipedRagdoll.headID ] = new Curve(0.4f);
		reachOutRight.curves[ BipedRagdoll.rightArmID ] = new Curve(
			new CurveKey[]{
				new CurveKey(0f,0.4f),
				new CurveKey(1f,1),
			}
		);
		reachOutRight.nextState = stand["idle"];

		var reachInRight = stand.Single( "reach in right", "stand_reach_out_start_right", false, 0.7f );
		reachInRight.AddEvent( Event.Voice(0,"misc") );
		reachInRight.nextState = rightLoopRight;
		reachInRight.curves[ BipedRagdoll.rightArmID ] = new Curve(
			new CurveKey[]{
				new CurveKey(0f,1),
				new CurveKey(1f,0.4f),
			}
		);

		stand.transitions[ reachRight ] = reachInRight;
		reachRight.transitions[ stand ] = reachOutRight;

		//left side
		var reachLeft = character.animationSet.GetBodySet("reach left");

		var leftLoopleft = reachLeft.Single( "reach loop", "stand_reach_out_loop_left", true );
		leftLoopleft.curves[ BipedRagdoll.headID ] = new Curve(
			new CurveKey[]{
				new CurveKey(0.3f,1),
				new CurveKey(0.4f,0),
				new CurveKey(0.6f,0),
				new CurveKey(0.7f,1),
			}
		);
		leftLoopleft.curves[ BipedRagdoll.leftArmID ] = new Curve(0.4f);

		var reachOutleft = reachLeft.Single( "reach out", "stand_reach_out_end_left", false, 0.8f );
		reachOutleft.curves[ BipedRagdoll.headID ] = new Curve(0.4f);
		reachOutleft.curves[ BipedRagdoll.leftArmID ] = new Curve(
			new CurveKey[]{
				new CurveKey(0f,0.4f),
				new CurveKey(1f,1),
			}
		);
		reachOutleft.nextState = stand["idle"];

		var reachInleft = stand.Single( "reach in left", "stand_reach_out_start_left", false, 0.7f );
		reachInleft.AddEvent( Event.Voice(0,"misc") );
		reachInleft.nextState = leftLoopleft;
		reachInleft.curves[ BipedRagdoll.leftArmID ] = new Curve(
			new CurveKey[]{
				new CurveKey(0f,1),
				new CurveKey(1f,0.4f),
			}
		);

		stand.transitions[ reachLeft ] = reachInleft;
		reachLeft.transitions[ stand ] = reachOutleft;
	}

	private bool AmIGrabbing( Item item ){
		return character.biped.rightHandGrabber.IsGrabbing( item ) || character.biped.leftHandGrabber.IsGrabbing( item );
	}
}  