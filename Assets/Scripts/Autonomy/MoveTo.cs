using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


namespace viva{

public delegate Nav.PathRequest[] PathRequestCallback( Vector3 targetPos );

/// <summary>Performs pathfinding on the character to reach a destination. The character must be in an AnimationState that applies forward motion to move along the waypoints.</summary>
public partial class MoveTo : Task {

	private float keepDistance;
	public Vector3[] path { get; private set; } = null;	//path must always be > 0
	public int targetNodeIndex { get; private set; } = 0;
	private PlayAnimation playMoveAnimation;
	private FaceTargetBody faceTargetBody;
	public readonly Target target = new Target();
	private Vector3 lastNavSearchTargetPos;
	private Vector3 lastNavSearchPoint;
	private float lastPathTime;
	private float stuckTimer = 0.0f;
	private Vector3 lastNonstuckPos = Vector3.zero;
	private bool wiggling = false;
	public PathRequestCallback pathRequest;
	public float distanceToStartWalking;
	public bool useToClosestPoint = false;
	private string nearbyTargetBodySet = null;
	private float nearbyTargetLength = 2f; 
	private float pathLengthLeft;
	private bool isNearby = false;
	public float smoothForwardBlend;
	public ListenerItemValidate onItemBlockingPath = new ListenerItemValidate( "onItemBlockingPath" );
	private Task navBreak;
	private float randomMaxForward;

	public readonly static float minNodeDist = 0.25f;
	public readonly static float minNodeSqDist = 0.125f;


	/// <summary>
	/// Attempts to move a character to a specified destination. The destination is set with AttemptFollowPath()
	/// </summary>
	/// <param name="_autonomy">The Autonomy of the parent character. Exception will be thrown if it is null.</param>
	/// <param name="_keepDistance">The minimum distance required to be near the target.</param>
	/// <param name="_distanceToStartWalking">The distance needed before character starts walking to target. A value of 0 will always walk. A value of less than 0 will disable walking.</param>
	/// <example>
    /// The following makes the character follow the specified transform and keep 2 meters apart.
    /// <code>
	/// var followObject = new Moveto( character.autonomy, 2.0f );
	/// followObject.target.SetTargetTransform( //...some transform
    /// </code>
    /// </example>
	public MoveTo( Autonomy _autonomy, float _keepDistance=0.0f, float _distanceToStartWalking=2f ):base(_autonomy){
		name = "move to";
		keepDistance = _keepDistance;
		distanceToStartWalking = _distanceToStartWalking;

		playMoveAnimation = new PlayAnimation( self.autonomy, null, "idle", false, -1 );
		playMoveAnimation.name = "move to animation";
		AddPassive( playMoveAnimation );

		faceTargetBody = new FaceTargetBody( _autonomy );
		faceTargetBody.name = "move to face target";
		onFixedUpdate += DrawPath;
		onFixedUpdate += UpdatePathfinding;
		onFail += Stop;
		onSuccess += Stop;
		onInterrupted += Stop;
		AddPassive( faceTargetBody );
		playMoveAnimation.onEnterAnimation += delegate{ smoothForwardBlend = 0; SetForwardLocomotion(0); };

		onFixedUpdate += target._InternalHandleChange;

		onInterrupted += delegate{
			SetIsNearby( false );
		};

		float navBreakStart = 0f;
		navBreak = new Task( autonomy );
		navBreak.onRegistered += delegate{
			navBreakStart = self.locomotionForward.position;
		};
		navBreak.onFixedUpdate += delegate{
			smoothForwardBlend = Mathf.Clamp01( smoothForwardBlend-Time.deltaTime*1.75f);
			self.locomotionForward.SetPosition( navBreakStart*Tools.EaseInOutQuad( smoothForwardBlend ) );
		};

		randomMaxForward = 0.92f+0.08f*Random.value;
	}

	private void SetForwardLocomotion( float value ){
		if( value == 0f ){
			navBreak.StartConstant( VivaScript._internalDefault, "nav break" );
		}else{
			autonomy.RemoveTask( "nav break" );
			self.locomotionForward.SetPosition( Mathf.Min( randomMaxForward, value ) );
		};
	}

	public void SetNearbyTargetBodySet( string bodySet, float _nearbyTargetLength ){
		nearbyTargetBodySet = bodySet; 
		nearbyTargetLength = _nearbyTargetLength;
	}

    public override bool OnRequirementValidate(){
		Vector3? finalNodePos = target.Read();
        if( !finalNodePos.HasValue ){
			Fail();
			return false;
		}
		if( !self.ragdoll.surface.HasValue ){
			return false;
		}
		Vector3 pos = finalNodePos.Value;
		pos.y = self.ragdoll.surface.Value.y;
		float minSuccessDist = keepDistance+minNodeDist;
		// if( keepDistance == 0 ) minSuccessDist += minNodeDist;
		return _InternalIsCloseToNode( self.ragdoll.surface.Value, pos, minSuccessDist );
    }

	public static bool IsCloseToNode( Vector3 groundPos, Vector3 node, float minDistance ){
		Vector3 cornerDiff = node-groundPos;
		bool heightInRange = Mathf.Abs( cornerDiff.y ) < 0.3f;
		cornerDiff.y = 0.0f;
		Tools.DrawCross( node, Color.white, 0.05f, Time.fixedDeltaTime );
		return cornerDiff.sqrMagnitude < minDistance*minDistance && heightInRange;
	}

	public bool _InternalIsCloseToNode( Vector3 groundPos, Vector3 node, float minDistance ){
		Vector3 cornerDiff = node-groundPos;
		bool heightInRange = cornerDiff.y > -0.3f && cornerDiff.y < self.model.bipedProfile.floorToHeadHeight*self.scale;
		cornerDiff.y = 0.0f;
		Tools.DrawCross( node, Color.white, 0.05f, Time.fixedDeltaTime );
		return cornerDiff.sqrMagnitude < minDistance*minDistance && heightInRange;
	}

	private void RecalculatePathToTarget( Vector3 targetPos ){
		if( self.nav.RequestNavSearch( pathRequest == null ? GenerateDefaultPathRequest( targetPos ) : pathRequest( targetPos ), OnFinishNavSearch, useToClosestPoint ? targetPos : (Vector3?)null ) ){
			lastNavSearchTargetPos = targetPos;
		}
	}

	private Nav.PathRequest[] GenerateDefaultPathRequest( Vector3 targetPos ){
		int vertices = Mathf.Clamp( 1+Mathf.FloorToInt( Mathf.Pow( keepDistance/0.2f, 2 ) ), 1, 8 );
		if( vertices == 2 ){
			vertices = 3;
		}
		return new Nav.PathRequest[]{
			new Nav.SearchCircle( targetPos, keepDistance, 2.0f, vertices )
		};
	}

	private void OnFinishNavSearch( Vector3[] path, Vector3 navSearchPoint ,Vector3 navPointDir ){
		if( finished ){
			Debug.Log("Ignored OnFinishNavSearch ");
			return;
		}
		if( path == null ){
			Fail("path is null");
			return;
		}
		lastNavSearchPoint = navSearchPoint;
		AttemptFollowPath( path );
	}

	private void UpdateLookAtPath( Vector3? nodePos ){
		if( nodePos.HasValue ){
			var sameHeightFinalPos = new Vector3( nodePos.Value.x, nodePos.Value.y+self.model.bipedProfile.floorToHeadHeight*self.scale, nodePos.Value.z );
			if( self.isBiped ) self.biped.lookTarget.SetTargetPosition( sameHeightFinalPos );
			Tools.DrawCross( sameHeightFinalPos, Color.green, 2 );
		}else{
			if( self.isBiped ) self.biped.lookTarget.SetTargetPosition( null );
		}
	}

	private void UpdatePathfinding(){
		if( !self.ragdoll.surface.HasValue ){
			return;
		}
		var pos = target.Read();
		if( !pos.HasValue ){
			Fail("no target read value");
			return;
		}
		//check if within target keepDistance
		Vector3 flatPosDiff = pos.Value-self.ragdoll.surface.Value;
		float floatPosSqDist = flatPosDiff.x*flatPosDiff.x+flatPosDiff.z*flatPosDiff.z;
		float minSuccessDist = keepDistance+minNodeDist*0.5f;
		if( floatPosSqDist < minSuccessDist*minSuccessDist ){
			Succeed();
			return;
		}
		if( self.nav.searching ) return;
		//Ensure path
		if( path == null ){
			RecalculatePathToTarget( pos.Value );
			return;
		}else{	//ensure path is not outdated
			if( Vector3.SqrMagnitude( lastNavSearchTargetPos-pos.Value ) > minNodeSqDist && Time.time-lastPathTime > 1.0f ){
				lastPathTime = Time.time;
				RecalculatePathToTarget( pos.Value );
				return;
			}
		}

		if( targetNodeIndex < path.Length ){
			
			Vector3 targetNode = path[ targetNodeIndex ];
			if( targetNodeIndex == path.Length-1 ){
				targetNode.x = pos.Value.x;
				targetNode.z = pos.Value.z;
			}

			faceTargetBody.target.SetTargetPosition( targetNode );

			float minDist = targetNodeIndex+1 >= path.Length ? minNodeDist*0.01f : minNodeDist;
			if( _InternalIsCloseToNode( self.ragdoll.surface.Value, targetNode, minDist ) ){	//if reached corner
				pathLengthLeft -= Vector3.Distance( path[ targetNodeIndex-1 ], path[ targetNodeIndex ] );
				targetNodeIndex++;
				stuckTimer = 0;	//reset stuck timer
				
				if( targetNodeIndex >= path.Length ){
					Succeed();
					Stop();
				}else{
					UpdateLookAtPath( targetNodeIndex == path.Length-1 ? (Vector3?)null : path[ targetNodeIndex ] );
				}
			}else{	//if hasn't reached corner yet

				//modulate speed based on bearing from target waypoint
				if( !wiggling ){
					var distance = targetNodeIndex+1 >= path.Length ? Vector3.Distance( self.ragdoll.surface.Value, targetNode ) : pathLengthLeft;
					float bearing = Mathf.Abs( Tools.Bearing( self.model.armature, targetNode ) );

					if( nearbyTargetBodySet != null ){
						if( distance < nearbyTargetLength ){
							SetIsNearby( true );
						}else if( distance > nearbyTargetLength+1f ){	//pad with 1 to toggle break
							SetIsNearby( false );
						}
					}
					
					float runSpeed = 0.45f+Tools.EaseOutQuad( Mathf.Clamp01( ( distance-minSuccessDist )/4.0f ) )*0.55f;
					float inverseSpeed = 24.0f;	//TODO GIVE IT A VARIABLE FOR SPEED
					float targetForward;
					if( distanceToStartWalking == -1 ){
						targetForward = 1f;
					}else{
						targetForward = Mathf.Clamp01( ( distance*inverseSpeed )/bearing )*runSpeed;
					}
					if( bearing > 90 ) targetForward = -targetForward;

					if( CheckItemBlockingPath( targetNode-self.ragdoll.surface.Value ) ){
						targetForward = 0;
					}else{
						CheckStuck();
					}
					smoothForwardBlend = Mathf.Clamp01( smoothForwardBlend+Time.deltaTime*1.75f);
					targetForward *= Tools.EaseInOutQuad( smoothForwardBlend );

					if( distanceToStartWalking > 0 ){
						var maxForward = Tools.RemapClamped( distanceToStartWalking, distanceToStartWalking+1f, 0.5f, 1f, distance );
						targetForward = Mathf.Min( targetForward, maxForward );
					}else if( distanceToStartWalking == 0 ){	//always walk
						targetForward = Mathf.Min( targetForward, 0.5f );
					}
					SetForwardLocomotion( Mathf.MoveTowards( self.locomotionForward.position, targetForward, Time.deltaTime*2f ) );
				}else{
					SetForwardLocomotion( 0f );
				}
			}
		}
	}

	private bool CheckItemBlockingPath( Vector3 direction ){
		var hipSize = self.model.bipedProfile.hipHeight*self.scale;
		if( Physics.Raycast( self.ragdoll.surface.Value+Vector3.up*minNodeDist, direction, out WorldUtil.hitInfo, hipSize, WorldUtil.itemsMask, QueryTriggerInteraction.Ignore ) ){
			var item = WorldUtil.hitInfo.collider.GetComponentInParent<Item>();
			if( item && !item.isBeingGrabbed && onItemBlockingPath.Invoke( item ) ){
				return true;
			}
		}
		return false;
	}

	private void SetIsNearby( bool _isNearby ){
		isNearby = _isNearby;
		if( isNearby ){
			playMoveAnimation.SetTargetBodySet( nearbyTargetBodySet );
		}else{
			playMoveAnimation.SetTargetBodySet( null );
		}
	}

	private void CheckStuck(){
		float stuckSqDist = Vector3.SqrMagnitude( lastNonstuckPos-self.ragdoll.root.rigidBody.position );
		if( stuckSqDist > minNodeSqDist ){
			lastNonstuckPos = self.ragdoll.root.rigidBody.position;
			stuckTimer = 0.0f;
		}else if( self.mainAnimationLayer.IsPlaying("idle") ){
			stuckTimer += Time.deltaTime;
			if( stuckTimer > 2.5f ){
				stuckTimer = 0.0f;
				Wiggle();	//try to unstuck itself
			}
		}
	}

	private void Wiggle(){
		if( wiggling ) return;
		wiggling = true;

		var wiggle = new Task( self.autonomy );
		wiggle.name = "wiggle unstuck for "+name;
		float wiggleMidtime = Time.time+0.5f;
		var moveTo = this;
		self.ragdoll.pinLimit.Add( "wiggling", 1.0f );
		wiggle.onFixedUpdate += delegate{
			float wiggleStrength = 1.0f-Mathf.Min( 0.5f, Mathf.Abs( Time.time-wiggleMidtime ) )*2.0f;
			self.ragdoll.pinLimit.Set( "wiggling", 1.0f-wiggleStrength );
			if( Time.time>wiggleMidtime+0.5f ){
				moveTo.RemovePassive( wiggle );
			}

			Vector3 force = new Vector3( Mathf.Sin( Time.time*4.123f ), 0, Mathf.Cos( Time.time*6.333f ) )*0.5f;
			self.ragdoll.root.rigidBody.AddForce( force*wiggleStrength, ForceMode.VelocityChange );
		};
		wiggle.onInterrupted += delegate{
			self.ragdoll.pinLimit.Remove( "wiggling" );
			wiggling = false;
		};
		AddPassive( wiggle );
	}

	private void DrawPath(){
		if( path != null && path.Length > 0 ){
			for( int i=1,j=0; i<path.Length; j=i++ ){
				Debug.DrawLine( path[i], path[j], Color.white, Time.fixedDeltaTime );
			}
		}
	}

	/// <summary>Stops the current pathfinding.</summary>
	public void Stop(){
		path = null;
		faceTargetBody.target.SetTargetPosition( null );
		SetForwardLocomotion( 0f );
	}

	/// <summary>Check if there is an active pathfinding request.</summary>
	public bool isMoveToActive(){
		if( path == null ){
			return false;
		}
		return targetNodeIndex<path.Length;
	}

	/// <summary>Get the final destination of the current pathfind request.</summary>
	/// <returns>null: There is no pathfinding active. Vector3: The final destination.</returns>
	public Vector3? GetCurrentDestination(){
		if( path == null ){
			return null;
		}
		return path[ path.Length-1 ];
	}
	
	//Follow a path in ascending order
	private bool AttemptFollowPath( Vector3[] newPath ){
		if( newPath == null ){
			Debugger.LogError("Cannot follow null path");
			return false;
		}
		if( !self.ragdoll.surface.HasValue ){
			Debugger.LogError("Self not on ground");
			return false;
		}
		//paths must be of size greater than 0
		if( newPath.Length == 0 ){
			newPath = new Vector3[]{ self.ragdoll.surface.Value };
		}
		
		path = newPath;
		Tools.DrawCross( path[ path.Length-1 ], Color.magenta, 0.1f );
		targetNodeIndex = 1;	//skip initial node
		
		faceTargetBody.target.SetTargetPosition( path[ path.Length-1 ] );
		if( path.Length > 2 ) UpdateLookAtPath( path[1] );

		pathLengthLeft = Nav.PathLength( path );
		SetIsNearby( false );
		return true;
	}
}

}