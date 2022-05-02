using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


namespace viva{


public class AutonomyMoveTo : Autonomy.Task {

	public delegate LocomotionBehaviors.PathRequest[] NavSearchCallback( Vector3 target );


	private const float refreshMoveToInterval = 0.7f;
	private const float readFailTimeout = 1.0f;
	private float? readFailStartTime = null;

	private readonly TaskTarget.ReadTargetCallback overrideFinalCornerLookAtPos = null;
	public float distance;
	private LocomotionBehaviors.PathCache pathID = new LocomotionBehaviors.PathCache();
	private Vector3? nearbyTargetPos = null;
	private Vector3? lastPathTargetPos = null;
	public readonly TaskTarget actualTargetPos;
	private readonly TaskTarget.ReadTargetCallback readTargetLocation;
	private RaycastHit[] raycastResults = new RaycastHit[8];
	private float lastRefreshTime = 0.0f;
	private float lastRaycastCheck = 0.0f;
	public NavSearchCallback onGeneratePathRequest;
	private bool nearTarget = false;
	private float targetDistance = 0.0f;
	public List<SlidingDoor> ignoreSlidingDoors = new List<SlidingDoor>();
	public BodyState preferredBodyState;
	private readonly AutonomyEnforceBodyState enforceBodyState;
	public bool keepDistance = true;
	public bool allowRunning = false;


	public AutonomyMoveTo( Autonomy _autonomy, string _name, TaskTarget.ReadTargetCallback _readTargetLocation, float _distance, BodyState _preferredBodyState, TaskTarget.ReadTargetCallback _overrideFinalCornerLookAtPos = null ):base(_autonomy,_name){
		readTargetLocation = _readTargetLocation;
		distance = _distance;
		onGeneratePathRequest = DefaultPathRequest;
		preferredBodyState = _preferredBodyState;

		overrideFinalCornerLookAtPos = _overrideFinalCornerLookAtPos;

		enforceBodyState = new AutonomyEnforceBodyState( self.autonomy, _name+" body state", preferredBodyState );
		AddPassive( enforceBodyState );
		AddPassive( new AutonomyFaceDirection( autonomy, _name+" face dir", DefaultReadPathLookAtTarget ) );

		onFixedUpdate += OnFixedUpdate;
		onRegistered += OnRegistered;

		actualTargetPos = new TaskTarget( autonomy.self );
	}

	private void DefaultReadPathLookAtTarget( TaskTarget target ){
		
		if( self.locomotion.path == null || self.locomotion.currCorner < 0 || self.locomotion.currCorner >= self.locomotion.path.Length ){
			return;
		}
		//if on last path corner
		if( self.locomotion.currCorner == self.locomotion.path.Length-1 ){
			if( overrideFinalCornerLookAtPos != null ){
				overrideFinalCornerLookAtPos.Invoke( target );
			}else{
				readTargetLocation?.Invoke( target );
			}
		}else{
			//if still in the middle of a path	
			target.SetTargetPosition( self.locomotion.path[ self.locomotion.currCorner ] );
		}
	}
	

	private bool IsDoorInTheWay(){
		
		//check every so often
		if( Time.time-lastRaycastCheck < 0.25f ){
			return false;
		}
		lastRaycastCheck = Time.time;

		if( self.locomotion.path == null || self.locomotion.currCorner >= self.locomotion.path.Length ){
			return false;
		}
		Vector3 targetPos = self.locomotion.path[ self.locomotion.currCorner ];
		Vector3 source = self.spine1RigidBody.transform.position;
		Vector3 dir = targetPos-source;
		float dirLength = dir.magnitude;
		if( dirLength == 0.0f ){
			return false;
		}
		dir /= dirLength;

		dirLength = Mathf.Min( dirLength, 2.0f );
		source -= dir*0.2f;	//erode backwards to prevent up close detection

		Debug.DrawLine( source, source+dir*dirLength, Color.blue, 0.25f );
		int results = Physics.SphereCastNonAlloc( source, 0.1f, dir, raycastResults, dirLength, Instance.itemsOnlyMask, QueryTriggerInteraction.Ignore );
		float leastDist = Mathf.Infinity;
		SlidingDoor targetSlidingDoor = null;
		for( int i=0; i<results; i++ ){
			var result = raycastResults[i];

			//Only BoxColliders will be supported
			BoxCollider boxCollider = result.collider as BoxCollider;
			if( boxCollider ){
				SlidingDoor slidingDoor = boxCollider.GetComponent<SlidingDoor>();
				if( slidingDoor ){
					if( !ignoreSlidingDoors.Contains( slidingDoor ) ){
						float targetPosSide = Tools.GetSide( targetPos, slidingDoor.transform );
						float currentSide = Tools.GetSide( self.floorPos, slidingDoor.transform );
						if( targetPosSide != currentSide && result.distance < leastDist ){
							targetSlidingDoor = slidingDoor;
							leastDist = result.distance;
						}
					}
				}
			}
		}
		if( targetSlidingDoor ){
			//inject new task
			autonomy.Interrupt( new AutonomyWaitForIdle( autonomy, "wait to idle in" ) );
			autonomy.Interrupt( new AutonomyOpenSlidingDoor( autonomy, name+" sliding door", targetPos, targetSlidingDoor, this ) );
			autonomy.Interrupt( new AutonomyWaitForIdle( autonomy, "wait to idle out" ) );
			
			return true;
		}else{
			return false;
		}
	}

	public void OnFixedUpdate(){
		//wait for slidingDoor to finish
		if( actualTargetPos.lastReadPos.HasValue && !IsDoorInTheWay() ){
			MoveToTargetPosition( actualTargetPos.lastReadPos.Value );
			
			if( isAPassive || isARequirement ){
				self.locomotion.ApplyMovement( false );
			}else{
				self.locomotion.ApplyMovement( true );
			}
		}
	}
	
	protected void OnRegistered(){
		lastPathTargetPos = null;
		Reset();
		lastRaycastCheck = Time.time;
	}

	public override bool? Progress(){
		actualTargetPos.lastReadPos = null;
		readTargetLocation?.Invoke( actualTargetPos );
		if( onGeneratePathRequest != DefaultPathRequest ){	//pick move pos if path request differs
			if( nearbyTargetPos.HasValue ){
				actualTargetPos.lastReadPos = nearbyTargetPos.Value;
			}
		}
		if( !actualTargetPos.lastReadPos.HasValue ){
			if( actualTargetPos.type == TaskTarget.TargetType.IN_HIERARCHY ){
				return true;
			}else{
				if( !readFailStartTime.HasValue ){
					readFailStartTime = Time.time;
				}else if( Time.time-readFailStartTime > readFailTimeout ){
					return false;
				}
				return null;
			}
		}
		readFailStartTime = null;
		
		if( nearbyTargetPos.HasValue ){
			targetDistance = GetFlatSqDist( nearbyTargetPos.Value, self.floorPos );
			//reduce the distance at first to ensure it reaches a stable distance
			float minNearDist = LocomotionBehaviors.minCornerDist*( 0.75f+System.Convert.ToInt32( nearTarget )*0.5f );
			nearTarget = targetDistance < minNearDist*minNearDist;
			
			if( nearTarget ){
				//ensure the nearPos is close to the last target read pos
				float minValidNearTargetDist = LocomotionBehaviors.minCornerDist+distance;
				if( GetFlatSqDist( nearbyTargetPos.Value, actualTargetPos.lastReadPos.Value ) > minValidNearTargetDist*minValidNearTargetDist ){
					nearbyTargetPos = null;
					lastPathTargetPos = null;
					nearTarget = false;
					return null;
				}
				return true;
			}else{
				return null;
			}
		}else if( GetFlatSqDist( actualTargetPos.lastReadPos.Value, self.floorPos ) < LocomotionBehaviors.minSqCornerDist ){
			return true;
		}else{
			return null;
		}
		//SpecialLog("", true);
	}

	public static float GetFlatSqDist( Vector3 a, Vector3 b ){
		Vector3 diff = b-a;
		diff.y = 0.0f;
		return Vector3.SqrMagnitude( diff );
	}

	private void MoveToTargetPosition( Vector3 targetPos ){
		if( !self.locomotion.IsSearchingNav() ){
			if( Time.time-lastRefreshTime < refreshMoveToInterval ){
				return;
			}
			lastRefreshTime = Time.time;

			//apply bodyState change
			float bodyStateChangeSqDist = enforceBodyState.targetBodyState == preferredBodyState ? 4.0f : 3.0f;
			enforceBodyState.SetTargetBodyState( targetDistance < bodyStateChangeSqDist ? preferredBodyState : BodyState.STAND );

			//run to target if far away
			bool tooFarAway = targetDistance > 20.0f;
			if( self.currentAnim != Loli.Animation.STAND_GIDDY_LOCOMOTION ){
				if( tooFarAway && allowRunning && !self.IsTired() ){
					self.SetTargetAnimation( Loli.Animation.STAND_GIDDY_LOCOMOTION );
				}
			}else if( !tooFarAway ){
				if( self.IsTired() ){
					self.SetTargetAnimation( self.bodyStateAnimationSets[ (int)self.bodyState ].GetRandomAnimationSet( AnimationSet.IDLE_TIRED ) );
				}
				if( self.IsHappy() ){
					self.SetTargetAnimation( self.bodyStateAnimationSets[ (int)self.bodyState ].GetRandomAnimationSet( AnimationSet.IDLE_HAPPY ) );
				}else{
					self.SetTargetAnimation( self.bodyStateAnimationSets[ (int)self.bodyState ].GetRandomAnimationSet( AnimationSet.IDLE_ANGRY ) );
				}
			}

			if( nearTarget ){
				float minSqDist = LocomotionBehaviors.minCornerDist+distance;
				if( !keepDistance && targetDistance < minSqDist*minSqDist ){
					return;
				}
				//avoid calculating path with navmesh
				Vector3 toFloorPos = self.floorPos-targetPos;
				toFloorPos.y = 0.0f;
				targetPos += toFloorPos.normalized*distance;
				
				var nearestFloor = GamePhysics.getRaycastPos( targetPos, Vector3.down, 2.0f, Instance.wallsMask, QueryTriggerInteraction.Ignore, 0.1f );
				if( nearestFloor.HasValue && LocomotionBehaviors.isOnWalkableFloor( nearestFloor.Value )){
					//keep actual move to pos
					var oldActualMoveToPos = nearbyTargetPos;
					OnFindPath( new Vector3[]{ nearestFloor.Value }, nearestFloor.Value, Vector3.forward );
					nearbyTargetPos = oldActualMoveToPos;
				}
			}else if( !lastPathTargetPos.HasValue || GetFlatSqDist( lastPathTargetPos.Value, targetPos ) > LocomotionBehaviors.minSqCornerDist ){
				lastPathTargetPos = targetPos;
				
				var pathRequest = onGeneratePathRequest( targetPos );
				if( pathRequest == null || !self.locomotion.AttemptContinuousNavSearch( pathRequest, OnFindPath ) ){
					FlagForFailure();
					Debug.LogError("[MoveTo] Could not begin nav search!");
				}
			}
		}
	}

	private LocomotionBehaviors.PathRequest[] DefaultPathRequest( Vector3 targetPos ){
		int vertices = Mathf.Clamp( 1+Mathf.FloorToInt( Mathf.Pow( distance/0.2f, 2 ) ), 1, 8 );
		if( vertices == 2 ){
			vertices = 3;
		}
		return new LocomotionBehaviors.PathRequest[]{ new LocomotionBehaviors.NavSearchCircle( targetPos, distance, 2.0f, vertices, Instance.wallsMask ) };
	}

	private void OnFindPath( Vector3[] path, Vector3 navSearchPoint, Vector3 navPointDir ){
		if( path == null ){
			Debug.Log("[MoveTo] No Path for "+name);
			Tools.DrawCross( navSearchPoint, Color.red );
			// Debug.Break();
			FlagForFailure();
			return;
		}
		//use closest navSearchPoint as world position
		nearbyTargetPos = navSearchPoint;

        self.locomotion.FollowPath( path, (LocomotionBehaviors.LocomotionCallback)delegate{ lastRefreshTime = Time.time; }, pathID);
	}

	// ------------------------------------------------------------------------------------
	// debug
	private string lastLogLine = "";
	private string logBuffer = "";
	private void SpecialLog(string msg, bool send = false)
	{
		if (msg != "")
			logBuffer += (logBuffer == "" ? msg : ", " + msg);
		if (send && logBuffer != "") {
			if (logBuffer != lastLogLine) {
				Debug.Log("csa: " + logBuffer);
				lastLogLine = logBuffer;
			}
			logBuffer = "";
		}
	}
}

}