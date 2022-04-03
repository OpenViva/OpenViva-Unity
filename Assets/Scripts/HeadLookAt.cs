using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using System.IO;


namespace viva{

public class HeadLookAt{

	private readonly Character self;
	private Quaternion currentLookRot;
	private Vector3 lookLocalPos = Vector3.forward;
	public Quaternion lookOffset = Quaternion.identity;
	public Transform head {get{ return self.biped.head.target; }}
	public LimitGroup strength { get; private set; } = new LimitGroup();
	private float headVelocityStartTime;

	public void _InternalReset(){
		strength._InternalReset();
	}

	public HeadLookAt( Character _self ){
		self = _self;
	}

	public void ResetHeadVelocity(){
		headVelocityStartTime = Time.time;
	}

    public void Apply(){
        
		if( strength.value == 0 ) return;
		var target = self.biped.lookTarget;
		
		var targetPos = target.Read();
		var velEase = Mathf.Clamp01( (Time.time-headVelocityStartTime)*3f );
		if( targetPos.HasValue ){
			
			var neck = self.biped.upperSpine.target;

			Vector3 look = targetPos.Value-self.biped.head.rigidBody.worldCenterOfMass;
			float sqLength = look.magnitude;
			if( sqLength == 0.0f ) return;
			look /= sqLength;
			if( Mathf.Abs( look.y ) == 1.0f ) return;

			Quaternion newLookRot = Quaternion.LookRotation( look, Vector3.up );
			//constrain to forward
			newLookRot = Quaternion.RotateTowards( head.rotation, newLookRot, 45.0f );

			var midLook = Quaternion.Lerp( currentLookRot, newLookRot, Time.deltaTime*10.0f );
			currentLookRot = Quaternion.RotateTowards( currentLookRot, midLook, Time.deltaTime*320.0f*velEase );
		}else{
			currentLookRot = Quaternion.RotateTowards( currentLookRot, head.rotation*lookOffset, Time.deltaTime*320.0f*velEase );
		}

		var animStrength = Tools.EaseInOutQuad( self.mainAnimationLayer.player.SampleCurve( BipedRagdoll.headID, 1 ) );
		head.rotation = Quaternion.LerpUnclamped( head.rotation, currentLookRot, animStrength*strength.value );
    }
}

}