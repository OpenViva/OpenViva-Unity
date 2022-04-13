using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace viva{

	public class LoliHandState: HandState{
		
		public class HoldBlendMax{
			public float value = 1.0f;
		}
		[SerializeField]
		private Collider[] armAndHandColliders;

		private Loli.ArmIK m_holdArmIK;
		public Loli.ArmIK holdArmIK { get{ return m_holdArmIK; } }
		public readonly Loli.ArmIK.RetargetingInfo holdRetargeting = new Loli.ArmIK.RetargetingInfo();
		private Vector3 cachedIKTargetPos = Vector3.zero; 
		private Vector3 cachedIKPolePos = Vector3.zero;
		private Quaternion cachedIKHandRotation = Quaternion.identity;
		private Tools.EaseBlend cacheIKEaseBlend = new Tools.EaseBlend();
		[SerializeField]
		private Quaternion m_handRestPoseRotation;
		public Quaternion handRestPoseRotation { get{ return m_handRestPoseRotation; } }
		private Vector3 cachedSelfItemPos = Vector3.zero;
		private Quaternion cachedSelfItemRot = Quaternion.identity;
		private Set<HoldBlendMax> holdBlendMaxes = new Set<HoldBlendMax>();
		private float[] animationHoldBlendMap;
		private HoldBlendMax animationHoldBlendMax = new HoldBlendMax();


		public void RequestSetHoldBlendMax( HoldBlendMax m ){
			if( m != null ){
				holdBlendMaxes.Add( m );
			}
		}
		public void RequestUnsetHoldBlendMax( HoldBlendMax m ){
			if( m != null ){
				holdBlendMaxes.Remove( m );
			}
		}

		public void SetupAnimationHoldBlendMap( float[] _animationHoldBlendMap ){
			animationHoldBlendMap = _animationHoldBlendMap;
			//register animation holdBlendMax
			if( _animationHoldBlendMap != null ){
				holdBlendMaxes.Add( animationHoldBlendMax );
			}else{
				holdBlendMaxes.Remove( animationHoldBlendMax );
			}
		}

		public void UpdateAnimationHoldBlendMap(){
			if( animationHoldBlendMap != null ){
				Loli loli = owner as Loli;
				float normTime = loli.GetLayerAnimNormTime(1)%1.0f;
				int sampleIndex = Mathf.FloorToInt( (float)animationHoldBlendMap.Length*normTime );
				float start = animationHoldBlendMap[ sampleIndex ];
				if( sampleIndex >= animationHoldBlendMap.Length-1 ){
					animationHoldBlendMax.value = start;
				}else{
					float end = animationHoldBlendMap[ sampleIndex+1 ];
					float lerp = sampleIndex-normTime;
					animationHoldBlendMax.value = Mathf.LerpUnclamped( start, end, lerp );
				}
			}
		}

		private int GetAnimatorHandLayer(){
			return rightSide?3:4;
		}

		public void ApplyIK(){

			Loli loli = owner as Loli;
			loli.animator.SetLayerWeight( GetAnimatorHandLayer(), Mathf.Clamp01( blendProgress*4.0f ) );

			if( heldItem != null ){
				SetupItemHoldIK( heldItem );
				
				float max = 1.0f;
				foreach( var holdBlendMax in holdBlendMaxes.objects ){
					max = Mathf.Min( max, holdBlendMax.value );
				}
				holdArmIK.Apply( holdRetargeting, max );
			}
		}
		
		protected override void OnPostPickupItem(){
			base.OnPostPickupItem();
			cacheIKEaseBlend.reset(0);
			cacheIKEaseBlend.StartBlend( 1.0f, 0.5f );
			
			SetupIgnoreHeldItemCollisions( true );
			CheckItemAchievements( heldItem );

			//initialize animator layer
			Loli loli = owner as Loli;
			loli.animator.CrossFade(
				Loli.holdAnimationStates[ (int)heldItem.settings.loliHeldAnimation ],
				0.0f,
				GetAnimatorHandLayer(),
				0.0f
			);
			// SetHandMuscleForHolding( true );

			CacheIKTransforms();
		}

		private void SetupIgnoreHeldItemCollisions( bool ignore ){
			if( heldItem == null ){
				foreach( var collider in heldItem.colliders ){
					foreach( var armCollider in armAndHandColliders ){
						Physics.IgnoreCollision( collider, armCollider, ignore );
					}
				}
			}
		}

		protected override void OnPreDropItem(){
			base.OnPreDropItem();
			
			SetupIgnoreHeldItemCollisions( false );
			// SetHandMuscleForHolding( false );
		}

		protected override void GetRigidBodyBlendConnectedAnchor( out Vector3 targetLocalPos, out Quaternion targetLocalRot ){
			targetLocalPos = fingerAnimator.hand.InverseTransformPoint( fingerAnimator.targetBone.position );
			targetLocalRot = Quaternion.Inverse( fingerAnimator.hand.rotation )*fingerAnimator.targetBone.rotation;
		}
		
		protected override void OnPreApplyHoldingTransform( Item targetItem ){
			Loli loli = owner as Loli;
			if( targetItem != null ){
				loli.animator.SetLayerWeight( GetAnimatorHandLayer(), 1.0f );
				loli.SetHandAnimationImmediate( GetAnimatorHandLayer(), targetItem.GetPreferredLoliHeldAnimation( this ) );
				loli.puppetMaster.ApplyCurrentPose();
			}else{
				//TODO
			}
			
			cachedSelfItemPos = selfItem.transform.position;
			cachedSelfItemRot = selfItem.transform.rotation;
			selfItem.transform.position = fingerAnimator.hand.position;
			selfItem.transform.rotation = fingerAnimator.hand.rotation;
		}

		protected override void OnPostApplyHoldingTransform( Transform grabTransform ){
			base.OnPostApplyHoldingTransform( grabTransform );
			selfItem.transform.position = cachedSelfItemPos;
			selfItem.transform.rotation = cachedSelfItemRot;
			Loli loli = owner as Loli;
			loli.animator.SetLayerWeight( GetAnimatorHandLayer(), 0.0f );
		}

		public void InitializeIK(){
			
			m_holdArmIK = new Loli.ArmIK( rightSide );
			float rotation;
			char suffix;
			if( rightSide ){
				suffix = 'r';
				rotation = 180.0f;
			}else{
				suffix = 'l';
				rotation = 0.0f;
			}

			Loli loli = owner as Loli;
			m_holdArmIK.spine2 = loli.spine2;
			if( rightSide ){
				m_holdArmIK.shoulder = loli.shoulder_r;
			}else{
				m_holdArmIK.shoulder = loli.shoulder_l;
			}
			m_holdArmIK.shoulderLocalBaseRotation = m_holdArmIK.shoulder.localRotation;
			Transform armControl = m_holdArmIK.shoulder.Find( "armControl_"+suffix );
			m_holdArmIK.upperArm = armControl.Find("upperArm_"+suffix);
			Transform forearmControl = armControl.Find( "forearmControl_"+suffix );
			m_holdArmIK.arm = forearmControl.Find( "arm_"+suffix );
			m_holdArmIK.wrist = forearmControl.Find( "wrist_"+suffix );
			m_holdArmIK.hand = forearmControl.Find( "hand_"+suffix );
			float armLength = Vector3.Distance( armControl.position, forearmControl.position );
			float forearmLength = Vector3.Distance( forearmControl.position, m_holdArmIK.hand.position );

			m_holdArmIK.ik = new Loli.TwoBoneIK( armControl, armLength, Quaternion.Euler( 180.0f-rotation, 90.0f, 90.0f ), forearmControl, forearmLength, Quaternion.Euler( rotation, 90.0f, 90.0f ) );
		}

		private void CacheIKTransforms(){
			
			Loli loli = owner as Loli;
			cachedIKTargetPos = holdArmIK.WorldToHoldSpace( selfItem.rigidBody.position );
			cachedIKPolePos = holdArmIK.WorldToHoldSpace( m_holdArmIK.arm.position );
			cachedIKHandRotation = selfItem.rigidBody.rotation;
		}

		private void SetupItemHoldIK( Item item ){
			cacheIKEaseBlend.Update( Time.deltaTime );
			//animate hand grabbing item with IK
			Loli loli = owner as Loli;
			Vector3 handtarget = Vector3.LerpUnclamped(
				cachedIKTargetPos,
				item.settings.IKTarget,
				cacheIKEaseBlend.value
			);
			Vector3 handPole = Vector3.LerpUnclamped(
				cachedIKPolePos,
				item.settings.IKPole, 
				cacheIKEaseBlend.value
			);

			Debug.DrawLine( holdArmIK.HoldSpaceToWorld( handtarget ), holdArmIK.HoldSpaceToWorld( handPole ), Color.green, 0.1f );

			//if object maintains yaw, pick up in a way to not spill contents
			Quaternion targetHandRot = Quaternion.LerpUnclamped(
				cachedIKHandRotation,
				loli.spine2RigidBody.rotation,
				cacheIKEaseBlend.value
			)*Quaternion.Euler(
				item.settings.IKHandEuler.x%360.0f*cacheIKEaseBlend.value,
				item.settings.IKHandEuler.y%360.0f*cacheIKEaseBlend.value*holdArmIK.sign,
				item.settings.IKHandEuler.z%360.0f*cacheIKEaseBlend.value*holdArmIK.sign
			);
			holdRetargeting.target = handtarget;
			holdRetargeting.pole = handPole;
			holdRetargeting.handRotation = targetHandRot;
		}

		public bool UpdateHoldItemInteractTimer( ref float timerVariable, float waitAmount ){
			
			if( Time.time-timerVariable > waitAmount ){
				timerVariable = Time.time;
				return true;
			}
			return false;
		}
		
		private void CheckItemAchievements( Item item ){
			if( item == null ){
				return;
			}
			Loli shinobu = owner as Loli;
			//achievements
			if( shinobu.rightHandState.heldItem != null &&
				shinobu.leftHandState.heldItem != null ){
				if( shinobu.rightHandState.heldItem.settings.itemType == Item.Type.DONUT &&
					shinobu.leftHandState.heldItem.settings.itemType == Item.Type.DONUT ){
					GameDirector.player.CompleteAchievement(Player.ObjectiveType.GIVE_2_DONUTS);
				}
			}
			if( item.settings.itemType == Item.Type.WATER_REED ){
				GameDirector.player.CompleteAchievement( Player.ObjectiveType.FIND_SHINOBU_A_WATER_REED );
			}
		}
	}
}