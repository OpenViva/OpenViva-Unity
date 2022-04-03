using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using System.IO;


namespace viva{

public class EyeLookAt{

	private readonly Character self;
	public Transform rightEye { get; private set; }
	public Transform leftEye { get; private set; }
	public Transform head {get{ return self.biped.head.target; }}
	public LimitGroup strengthLimit { get; private set; } = new LimitGroup();
	private float randomLookTimer;
	private float randomLookTimeout;
	private Vector3 randomLookOffsetTarget;
	private Vector3 randomLookOffset;
	private float eyeDegrees;
	private float blinkTimer = 0.0f;
	private float targetBlink = 0.0f;
	private int blinkBlendShapeIndex;


	public EyeLookAt( Character _self, float _eyeDegrees ){
		self = _self;
		rightEye = GetEye( BipedBone.EYEBALL_R );
		leftEye = GetEye( BipedBone.EYEBALL_L );
		eyeDegrees = _eyeDegrees;
		var blinkBlendShape = self.model.bipedProfile.blendShapeBindings[ (int)RagdollBlendShape.BLINK ];
		blinkBlendShapeIndex = blinkBlendShape==null? -1 : self.model.skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex( blinkBlendShape );
	}

	private Transform GetEye( BipedBone bone ){
		var boneInfo = self.model.bipedProfile[ bone ];
		if( boneInfo == null ) return null;
		return boneInfo.transform;
	}

    public void Apply(){
		if( strengthLimit.value == 0 ) return;
		var target = self.biped.lookTarget;

		randomLookTimer += Time.deltaTime;
		if( randomLookTimer > randomLookTimeout ){
			randomLookTimer = 0;
			randomLookTimeout = Random.value*2.2f;
			randomLookOffsetTarget = Random.insideUnitSphere*eyeDegrees/100.0f;
		}
		randomLookOffset = Vector3.LerpUnclamped( randomLookOffset, randomLookOffsetTarget, Time.deltaTime*16.0f );
		
		var targetPos = target.Read();
		if( targetPos.HasValue ){
			ApplyLookAt( rightEye, targetPos.Value );
			ApplyLookAt( leftEye, targetPos.Value );
		}else{
			ApplyLookAt( rightEye, Vector3.forward );
			ApplyLookAt( leftEye, Vector3.forward );
		}
		ApplyBlinkAnimation();
    }

	public void ResetRandomLookTimer(){
		randomLookTimeout = 2+Random.value*4.0f;
		randomLookTimer = 0;
	}

	private void ApplyLookAt( Transform eye, Vector3 targetPos ){
		if( eye == null ) return;
		var look = targetPos+randomLookOffset-eye.position;
		if( look.sqrMagnitude > Mathf.Epsilon ){
			var targetLook = Quaternion.LookRotation( look, head.up );
			var animStrength = self.mainAnimationLayer.player.SampleCurve( BipedRagdoll.eyesID, 1 );
			eye.rotation = Quaternion.RotateTowards( head.rotation, targetLook, eyeDegrees*animStrength );
		}
	}

	public void Blink(){
		blinkTimer = 0;
	}

	private void ApplyBlinkAnimation(){
		if( blinkBlendShapeIndex < 0 ) return;
		
		blinkTimer -= Time.deltaTime;
		if( blinkTimer <= 0 ){
			blinkTimer = 0.3f+Random.value*4.0f;
			targetBlink = 1.0f;
		}else{
			targetBlink *= 0.825f;
		}
		var currBlink = self.model.skinnedMeshRenderer.GetBlendShapeWeight( blinkBlendShapeIndex );
		self.model.skinnedMeshRenderer.SetBlendShapeWeight( blinkBlendShapeIndex, Mathf.Max( currBlink, targetBlink ) );
	}
}

}