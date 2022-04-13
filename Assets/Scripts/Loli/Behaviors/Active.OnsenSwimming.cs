using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{


public class OnsenSwimming : ActiveBehaviors.ActiveTask {

	[System.Serializable]
	public class OnsenSwimmingSession: SerializedTaskData{
		[VivaFileAttribute]
		public VivaSessionAsset activePoolAsset {get; set;}
		[VivaFileAttribute]
		public VivaSessionAsset activeTowelAsset {get; set;}
		[VivaFileAttribute]
		public VivaSessionAsset activeBasketAsset {get; set;}

		public OnsenPool pool { get{ return activePoolAsset as OnsenPool; } }
		public Towel towel { get{ return activeTowelAsset as Towel; } }
		public ChangingRoomBasket basket { get{ return activeBasketAsset as ChangingRoomBasket; } }
	}

	public OnsenSwimmingSession swimmingSession { get{ return session as OnsenSwimmingSession; } }

	private AutonomyEmpty currentClerkSession;


	public OnsenSwimming( Loli _self ):base(_self,ActiveBehaviors.Behavior.ONSEN_SWIMMING,new OnsenSwimmingSession()){
	}

	public override void OnDeactivate(){
		if( currentClerkSession != null ){
			currentClerkSession.FlagForFailure();
			currentClerkSession = null;
			Debug.LogError("FAILED CLERK SESSION");
		}
	}

	private bool IsWearingSwimmingClothes(){
		foreach( var instance in self.outfitInstance.attachmentInstances ){
			if( instance.sourceClothingPiece.wearType == ClothingPreset.WearType.FULL_BODY ){
				return true;
			}
		}
		return false;
	}

	public override void OnActivate(){
		
		GameDirector.player.objectFingerPointer.selectedLolis.Remove( self );
		self.characterSelectionTarget.OnUnselected();
		
		GoToPool();
	}

	public void GoToPool(){
		if( swimmingSession == null ){
			self.active.SetTask( self.active.idle );
			return;
		}

		if( !IsWearingSwimmingClothes() ){
			GoToBasketAndSwapClothes( swimmingSession.towel );
		}else{
			GoToPoolAndSwim();
		}
	}

	public bool AttemptChangeOutOfSwimmingClothes(){
		
		if( swimmingSession == null || swimmingSession.activeBasketAsset == null ){
			return false;
		}
		
		if( !IsWearingSwimmingClothes() ){
			return false;
		}

		var goToBasket = new AutonomyMoveTo( self.autonomy, "go to basket", delegate( TaskTarget target ){
			target.SetTargetPosition( swimmingSession.activeBasketAsset.transform.position );
		},
		1.0f,
		BodyState.STAND );
		goToBasket.onSuccess += delegate(){
			
			if( swimmingSession.activeBasketAsset == null ){
				self.SetOutfit( swimmingSession.basket.outfit );
				swimmingSession.basket.SetDisposedOutfit( null );
			}

			self.active.SetTask( self.active.idle );
		};
		goToBasket.onFail += delegate{ self.active.SetTask( self.active.idle ); };
		self.autonomy.SetAutonomy( goToBasket );

		return true;
	}

	private void GoToBasketAndSwapClothes( Towel towel ){

		if( swimmingSession == null || swimmingSession.activeBasketAsset == null ){
			EndConfused();
			return;
		}

		if( towel == null ){
			towel = self.GetItemIfHeldByEitherHand<Towel>();

			if( towel == null ){
				GoPickupTowel();
				return;
			}
		}
		
		//go to basket while holding towel
		var maintainTowel = new AutonomyPickup( self.autonomy, "pickup towel", towel, self.GetPreferredHandState( towel ), false );
		//NEED TO MAKE IT A MAINTAIN ITEM AUTONOMY
		var goToBasket = new AutonomyMoveTo( self.autonomy, "go to basket", delegate( TaskTarget target ){
			target.SetTargetPosition( swimmingSession.activeBasketAsset.transform.position );
		},
		1.0f,
		BodyState.STAND );
		goToBasket.AddRequirement( maintainTowel );

		goToBasket.onSuccess += delegate(){
			
			if( towel != null && towel.mainOwner == self ){
				
				swimmingSession.basket.SetDisposedOutfit( self.outfit );
				Outfit resetOutfit = Outfit.Create(
				new string[]{
					"towelWrap"
					},
					true
				);
				self.SetOutfit( resetOutfit );

				towel.mainOccupyState.AttemptDrop();

				GoToPool();
			}
		};
		goToBasket.onFail += delegate{ self.active.SetTask( self.active.idle ); };

		self.autonomy.SetAutonomy( goToBasket );
	}

	private void GoToPoolAndSwim(){

		if( swimmingSession.pool == null ){
			self.active.SetTask( self.active.idle );
			return;
		}

		Vector3 poolTarget = swimmingSession.pool.GetRandomWaterFloorPoint();

		var goToPool = new AutonomyMoveTo( self.autonomy, "go to pool", delegate( TaskTarget target ){
			target.SetTargetPosition( poolTarget );
		}, 0.3f,
		BodyState.STAND
		);
		goToPool.onSuccess += SwimAroundUntilWall;
		goToPool.onFail += delegate{ self.active.SetTask( self.active.idle ); };

		self.autonomy.SetAutonomy( goToPool );
	}

	private void SwimAroundUntilWall(){
		var swimAroundPerpetual = new AutonomyEmpty( self.autonomy, "swim around perpetual", delegate{ return null; } );
		var swimAroundMove = new AutonomyMoveTo( self.autonomy, "swim around", delegate( TaskTarget target ){
			target.SetTargetPosition( self.floorPos+self.anchor.forward*0.8f );
		}, 0.1f,
		BodyState.SQUAT );
		swimAroundMove.onFixedUpdate += delegate{ CheckHitWall( swimAroundMove ); };
		swimAroundMove.onFail += delegate{
			var wait = new AutonomyWait( self.autonomy, "swim around reset wait", 1.0f );
			self.autonomy.SetAutonomy( wait );
			wait.onSuccess += SwimAroundUntilWall;
		};

		swimAroundPerpetual.AddPassive( swimAroundMove );

		self.autonomy.SetAutonomy( swimAroundPerpetual );
	}

	private void CheckHitWall( AutonomyMoveTo parentTask ){
		
		int hits = 0;
		Vector3 wallNorm = Vector3.zero;
		Vector3 wallPos = Vector3.zero;
		for( int i=-1; i<=1; i++ ){
			Vector3 source = self.floorPos;
			Vector3 dir = self.anchor.forward+self.anchor.right*i*0.1f;
			Debug.DrawLine( source, source+dir.normalized*1.0f, Color.green, 0.1f );
			if( GamePhysics.GetRaycastInfo( source, dir, 1.0f, Instance.wallsMask, QueryTriggerInteraction.Ignore ) ){
				hits++;
				wallNorm += GamePhysics.result().normal;
				wallPos += GamePhysics.result().point;
			}
		}
		if( hits == 3 ){
			wallNorm /= 3;
			wallPos /= 3;

			var moveToRelax = new AutonomyMoveTo( self.autonomy, "move to relax",
			delegate( TaskTarget target ){
				target.SetTargetPosition( wallPos );
			}, 0.1f, BodyState.SQUAT,
			delegate( TaskTarget target ){
				target.SetTargetPosition( wallPos+wallNorm );
			}
			);
			moveToRelax.onGeneratePathRequest = delegate( Vector3 target ){
				return new LocomotionBehaviors.PathRequest[]{
					new LocomotionBehaviors.NavSearchCircle( target, 0.4f, 1.0f, 6, Instance.wallsMask,
					new LocomotionBehaviors.NavSearchCircle.TestTowardsInnerCircle( 0.2f, 3 ) )
				};
			};

			var faceRelax = new AutonomyFaceDirection( self.autonomy, "face relax",
			delegate( TaskTarget target ){
				target.SetTargetPosition( wallPos+wallNorm );
			});

			var perpetuate = new AutonomyEmpty( self.autonomy, "perpetuate relax", delegate{ return null; } );
			var relax = new AutonomyPlayAnimation( self.autonomy, "relax", Loli.Animation.SQUAT_TO_RELAX );

			moveToRelax.onRegistered += delegate{ relax.Reset(); };

			relax.AddRequirement( moveToRelax );
			relax.AddRequirement( faceRelax );

			perpetuate.AddRequirement( relax );

			self.autonomy.SetAutonomy( perpetuate );
		}
	}

	private void GoPickupTowel(){

		if( swimmingSession.pool == null ){
			self.active.SetTask( self.active.idle );
			return;
		}
		var reception = swimmingSession.pool.onsenReception;

		var receptionClient = new AutonomyEmpty( self.autonomy, "reception client", delegate{ return null; } );

		//ensure one loli at a time using reception desk as a client
		var receptionFilterUse = new AutonomyFilterUse( self.autonomy, "reception client filter use", reception.receptionBell.filterUse, 2.0f );

		//wait in line
		var waitInLine = new AutonomyMoveTo( self.autonomy, "wait in line", delegate( TaskTarget target ){
			target.SetTargetPosition( reception.localQueueStart.position+reception.localQueueStart.forward*0.4f*receptionFilterUse.queueIndex );
		}, 0.1f, BodyState.STAND, delegate( TaskTarget target ){
			target.SetTargetPosition( reception.transform.TransformPoint( reception.localClientWaitZonePos ) );
		} );

		receptionFilterUse.AddPassive( waitInLine );
		receptionClient.AddRequirement( receptionFilterUse );

		//setup hit bell on reception desk
		var hitBell = new AutonomyPlayAnimation( self.autonomy, "hit bell anim", Loli.Animation.STAND_PICKUP_RIGHT );
		hitBell.loop = true;
		hitBell.onRegistered += delegate{ hitBell.OverrideAnimations( GetHitBellAnimation() ); };
		hitBell.onRegistered += delegate{
			new BlendController( hitBell.entryAnimation==Loli.Animation.STAND_PICKUP_RIGHT ? self.rightLoliHandState : self.leftLoliHandState, hitBell.entryAnimation, OnHitBellIKControl );
		};
		hitBell.onSuccess += WaitForClerkSession;
		hitBell.onFail += delegate{ self.active.SetTask( self.active.idle ); };


		var listenForBellHit = new AutonomyEmpty( self.autonomy, "listen for bell" );
		listenForBellHit.onCharacterCollisionEnter += delegate( CharacterCollisionCallback ccc, Collision collision ){
			if( collision.collider.GetComponent<ReceptionBell>() ){ hitBell.FlagForSuccess(); }
		};
		
		var faceBell = new AutonomyFaceDirection( self.autonomy, "face bell", delegate( TaskTarget target ){
			target.SetTargetPosition( reception.receptionBell.transform.position );
		}, 1.0f, 5.0f );

		var goToBell = new AutonomyMoveTo( self.autonomy, "go to bell", delegate( TaskTarget target ){
			target.SetTargetPosition( reception.transform.TransformPoint( reception.localClientWaitZonePos ) );
		}, 0.0f, BodyState.STAND );

		hitBell.AddRequirement( goToBell );
		hitBell.AddRequirement( faceBell );
		hitBell.AddPassive( listenForBellHit );

		//link hit bell after receiving filter use owner
		receptionFilterUse.onSuccess += delegate{
			receptionFilterUse.RemovePassive( waitInLine );
			receptionClient.AddRequirement( hitBell );
		};

		self.autonomy.SetAutonomy( receptionClient );
	}

	private void WaitForClerkSession(){
		
		if( swimmingSession.pool == null ){
			self.active.SetTask( self.active.idle );
			return;
		}
		if( !swimmingSession.pool.onsenReception.CreateClerkSession( self, WaitForReceptionTowel ) ){
			EndConfused();
		}
	}

	private void WaitForReceptionTowel( AutonomyEmpty clerkSession ){

		if( swimmingSession.pool == null ){
			self.active.SetTask( self.active.idle );
			return;
		}
		var reception = swimmingSession.pool.onsenReception;
		if( clerkSession != null ){
			currentClerkSession = clerkSession;

			var waitForAttenderTowel = new AutonomyEmpty( self.autonomy, "wait for towel", delegate(){
				if( currentClerkSession == null ){
					return false;
				}
				swimmingSession.activeTowelAsset = currentClerkSession.self.GetItemIfHeldByEitherHand<Towel>();
				if( swimmingSession.activeTowelAsset == null ){
					return null;
				}
				return true;
			} );

			waitForAttenderTowel.onSuccess += delegate(){
				if( currentClerkSession != null ){
					GoToBasketAndSwapClothes( currentClerkSession.self.GetItemIfHeldByEitherHand<Towel>() );
				}
			};
			waitForAttenderTowel.onFail += delegate{ self.active.SetTask( self.active.idle ); };
			waitForAttenderTowel.AddRequirement( currentClerkSession );	//clerk session must be valid throughout

			self.autonomy.SetAutonomy( waitForAttenderTowel );
		}else{
			EndConfused();
		}
	}

	private void EndConfused(){
		var goToWaitZone = CreateWaitAtClientPostRingWaitZone();
		var playAnim = LoliUtility.CreateSpeechAnimation( self, AnimationSet.CONFUSED, SpeechBubble.INTERROGATION );
		if( goToWaitZone != null ){
			playAnim.onSuccess += delegate{ self.autonomy.SetAutonomy( goToWaitZone ); };
			self.autonomy.SetAutonomy( playAnim );
		}else{
			self.autonomy.SetAutonomy( playAnim );
		}
		playAnim.onSuccess += delegate{ self.active.SetTask( self.active.idle ); };
		playAnim.onFail += delegate{ self.active.SetTask( self.active.idle ); };
	}

	private AutonomyMoveTo CreateWaitAtClientPostRingWaitZone(){
		
		if( swimmingSession.pool == null ){
			return null;
		}
		var reception = swimmingSession.pool.onsenReception;
		var waitAtWaitPos = new AutonomyMoveTo( self.autonomy, "wait at wait pos", delegate( TaskTarget target ){
			target.SetTargetPosition( reception.localClientPostRingWaitZone.position );
		}, 0.6f, BodyState.STAND, delegate( TaskTarget target ){
				target.SetTargetPosition( reception.localClientPostRingWaitZone.position+reception.localClientPostRingWaitZone.forward );
			}
		);
		return waitAtWaitPos;
	}

	private Loli.Animation GetHitBellAnimation(){
		if( self.rightHandState.occupied ){
			return Loli.Animation.STAND_PICKUP_LEFT;
		}else{
			return Loli.Animation.STAND_PICKUP_RIGHT;
		}
	}
	
	private float OnHitBellIKControl( BlendController blendController ){
		if( swimmingSession.pool == null ){
			return 0.0f;
		}
		Vector3 targetPos = swimmingSession.pool.onsenReception.receptionBell.transform.position;
		self.animator.SetFloat( Instance.pickupHeightID, 1.0f ); 
		self.animator.SetFloat( Instance.pickupReachID, 1.0f ); 

		float progress = self.GetLayerAnimNormTime(1);
		float lerp = Tools.GetClampedRatio( 0.35f, 0.5f, progress )-Tools.GetClampedRatio( 0.5f, 0.7f, progress );
		
		blendController.armIK.OverrideWorldRetargetingTransform(
			blendController.retargetingInfo,
			targetPos+Vector3.up*0.1f, //pad a bit higher to properly reach item
			AutonomyPickup.CalculatePickupPole( blendController.armIK.sign, self ),
			blendController.targetHandState.fingerAnimator.hand.rotation
		);
		return lerp;
	}
	
}

}