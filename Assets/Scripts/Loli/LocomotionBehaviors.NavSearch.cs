using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


namespace viva{


public partial class LocomotionBehaviors : Job {

	public delegate void OnNavSearchFinish( Vector3[] path, Vector3 navSearchPoint ,Vector3 navPointDir );

	public abstract class PathRequest{

		public abstract Vector3? GetNextSearchPoint();
		public abstract Vector3 GetCurrentSearchPointDirection();
	}

	public class NavSearchLine: PathRequest{

		private Vector3 lineStart;
		private Vector3 lineDiff;
		private float height;
		private int steps;
		private int currentStep = 0;

		public NavSearchLine( Vector3 _lineStart, Vector3 _lineEnd, float _height, int _steps, float shrinkPercent=0.0f ){
			
			shrinkPercent *= 0.5f;
			lineDiff = _lineEnd-_lineStart;
			lineStart = _lineStart+lineDiff*shrinkPercent;
			lineDiff *= ( 1.0f-shrinkPercent*2.0f );
			height = _height;
			steps = _steps;

			Debug.DrawLine( lineStart, _lineEnd, Color.yellow, 5.0f );
			Debug.DrawLine( (_lineStart+_lineEnd)/2.0f, (_lineStart+_lineEnd)/2.0f+GetCurrentSearchPointDirection()*0.5f, Color.yellow, 5.0f );
		}

		public override Vector3? GetNextSearchPoint(){
			if( currentStep > steps ){
				return null;
			}
			Vector3 pos = lineStart+( (float)currentStep++/steps )*lineDiff+Vector3.up*0.1f;
			if( GamePhysics.GetRaycastInfo( pos, -Vector3.up, height+0.1f, Instance.wallsMask ) ){
				return GamePhysics.result().point+Vector3.up*0.01f;	//pad to stay above floor
			}else{
				Tools.DrawCross( pos-Vector3.up*height, Color.red, 0.1f );
				return null;
			}
		}

		public override Vector3 GetCurrentSearchPointDirection(){
			return Vector3.Cross( -lineDiff, Vector3.up ).normalized;
		}
	}

	public class NavSearchPoint: PathRequest{

		private readonly Vector3 point;
		private readonly Vector3 dir;
		private bool searched = false;

		public NavSearchPoint( Vector3 _point, Vector3 _dir ){
			
			point = _point;
			dir = _dir;
		}

		public override Vector3? GetNextSearchPoint(){
			if( searched ){
				return null;
			}
			searched = true;
			return point;
		}

		public override Vector3 GetCurrentSearchPointDirection(){
			return dir;
		}
	}

	public class NavSearchCircle: PathRequest{

		public class TestTowardsInnerCircle{
			public float finalRadius = 0.0f;
			public int stepsInwards = 2;

			public TestTowardsInnerCircle( float _finalRadius = 0.0f, int _stepsInwards = 2 ){
				finalRadius = _finalRadius;
				stepsInwards = _stepsInwards;
			}
		}

		private readonly Vector3 center;
		private readonly float radius;
		private readonly float testHeight;
		private readonly int vertices;
		private float currentStep = 0.0f;
		private Vector3 dir;
		private readonly int eyeSightMask;
		private readonly TestTowardsInnerCircle subTest;
		private int stepsInwardCount = 0;

		public NavSearchCircle( Vector3 _center, float _radius, float _testHeight, int _vertices, int _eyeSightMask, TestTowardsInnerCircle _subTest=null ){
			
			center = _center;
			radius = _radius;
			testHeight = _testHeight;
			vertices = _vertices;
			eyeSightMask = _eyeSightMask;
			subTest = _subTest;
			if( subTest != null ){
				subTest.stepsInwards = Mathf.Max( subTest.stepsInwards, 2 );
			}
		}

		public override Vector3? GetNextSearchPoint(){

			while( currentStep < vertices ){
				
				float testRadius;
				bool testVisibility;
				if( subTest == null ){
					currentStep++;
					testRadius = radius;
					testVisibility = true;
				}else{
					testRadius = Mathf.LerpUnclamped( radius, subTest.finalRadius, (float)stepsInwardCount/(subTest.stepsInwards-1) );
					testVisibility = stepsInwardCount == 0;
					if( ++stepsInwardCount >= subTest.stepsInwards ){
						stepsInwardCount = 0;
						currentStep++;
					}
				}
				float radian = 2.0f*Mathf.PI*(currentStep/vertices);
				dir = new Vector3( Mathf.Cos( radian ), 0.0f, Mathf.Sin( radian ) );
				Vector3 edgePoint = center+dir*testRadius;

				//test direct visibility
				if( Physics.Raycast( center, dir, radius, eyeSightMask ) ){
					if( subTest != null ){
						currentStep++;
						stepsInwardCount = 0;
					}
					continue;
				}
				//test downwards to find local floor
				if( GamePhysics.GetRaycastInfo( edgePoint, Vector3.down, testHeight, Instance.wallsMask ) ){
					return GamePhysics.result().point+Vector3.up*0.01f;	//pad to stay above floor
				}else{
					Tools.DrawCross( edgePoint+Vector3.down*testHeight, Color.red, 2.0f );
				}
			}
			return null;
		}

		public override Vector3 GetCurrentSearchPointDirection(){
			return dir;
		}
	}

	private Coroutine continuousNavSearchCoroutine = null;


	public bool AttemptContinuousNavSearch( PathRequest[] requests, OnNavSearchFinish onFinish, Vector3? toClosestPoint=null ){
		if( continuousNavSearchCoroutine != null ){
			return false;
		}

		//disable nav search if shinobu is an anchor transition
		if( self.anchorActive ){
			// Debug.LogError("Skipping nav search due to anchor transition...");
			if( continuousNavSearchCoroutine != null ){
				GameDirector.instance.StopCoroutine( continuousNavSearchCoroutine );
				continuousNavSearchCoroutine = null;
			}
			return false;
		}
		if( onFinish == null ){
			Debug.LogError("onFinish cannot be null!");
			return false;
		}
		// Debug.Log("Continuous nav search...");
		continuousNavSearchCoroutine = GameDirector.instance.StartCoroutine( ContinuousNavSearch( requests, onFinish, toClosestPoint ) );
		return true;
	}

	public bool IsSearchingNav(){
		return continuousNavSearchCoroutine!=null;
	}

	private IEnumerator ContinuousNavSearch( PathRequest[] requests, OnNavSearchFinish onFinish, Vector3? toClosestPoint=null ){

		yield return null; //wait 1 frame so it's not an immediate return

		//ensure shinobu is in the NavMesh
		if( !NavMesh.SamplePosition( self.floorPos, out navTest, minCornerDist*2.0f, NavMesh.AllAreas ) ){
			Debug.LogError("[Locomotion] Character not near walkable position");
			Tools.DrawCross( self.floorPos, Color.red, 0.5f );
			requests = new PathRequest[]{};
		}
		Vector3[] finalPath = null;
		Vector3 navPointDir = Vector3.zero;
		Vector3 navSearchPoint = Vector3.zero;
		Vector3 sourceNavPos = navTest.position;
		float shortestCost = Mathf.Infinity;

		for( int i=0; i<requests.Length; i++ ){
			PathRequest request = requests[i];
			int steps = 0;
			while( true ){
				if( ++steps > 10 ){	//max searches per frame
					steps = 0;
					yield return new WaitForSeconds( 0.05f );
				}
				Vector3? testPoint = request.GetNextSearchPoint();
				if( !testPoint.HasValue ){
					break;
				}
				if( isOnWalkableFloor( testPoint.Value ) ){
					NavMeshPath cachePath = new NavMeshPath();
					if( !NavMesh.CalculatePath( sourceNavPos, testPoint.Value, NavMesh.AllAreas, cachePath ) ){
						Tools.DrawCross( testPoint.Value-Vector3.down*0.2f, Color.red, 0.05f );
						continue;
					}
					Tools.DrawCross( testPoint.Value, Color.yellow, 0.06f );

					//find path closest to target and shortest path
					float pathCost;
					if( toClosestPoint.HasValue ){
						pathCost = ( toClosestPoint.Value-testPoint.Value).sqrMagnitude;
					}else{
						pathCost = PathSqLength( cachePath );
					}
					if( pathCost < shortestCost ){
						shortestCost = pathCost;
						navSearchPoint = testPoint.Value;
						finalPath = cachePath.corners;
						//set last point close to the original final point
						Vector3 toTestPoint = testPoint.Value-navTest.position;
						float toTestPointDistance = toTestPoint.magnitude;
						if( toTestPointDistance != 0.0f ){
							toTestPoint /= toTestPointDistance;
						}
						//allowed to be a bit outside of navigation bounds
						finalPath[ finalPath.Length-1 ] = navTest.position+toTestPoint*Mathf.Min( toTestPointDistance, 0.15f );

						navPointDir = request.GetCurrentSearchPointDirection();
						Tools.DrawCross( testPoint.Value, Color.green, 0.05f );
					}
				}else{
					Tools.DrawCross( testPoint.Value, Color.red, 0.15f );
				}
			}
		}
		if( onFinish != null ){
			onFinish( finalPath, navSearchPoint, navPointDir );
		}
		continuousNavSearchCoroutine = null;
	}

	private float PathSqLength( NavMeshPath path ){
		float length = 0.0f;
		for( int j=path.corners.Length-1,i=0; i<path.corners.Length; j=i++ ){
			length += Vector3.SqrMagnitude( path.corners[i]-path.corners[j] );
		}
		return length;
	}

	// //samples rays extending from pos with length "radius", then each step the radius i shrunk by radiusErode
	// public Vector3? SampleDownNearestFloorPosition( Vector3 pos, float radius, float radiusErode, float height, int steps ){

	// 	//shift a bit up so it doesnt collide with floor
	// 	pos.y += 0.02f;
	// 	Vector3? walkPos = null;
	// 	float sqShortest = Mathf.Infinity;
	// 	float radians = 0.0f;
	// 	float radiansStep = Mathf.PI*2.0f/8.0f;
	// 	for( int i=0; i<8; i++ ){
			
	// 		Vector3 testDir = Vector3.zero;
	// 		testDir.x = Mathf.Cos(radians)*radius;
	// 		testDir.z = Mathf.Sin(radians)*radius;

	// 		Vector3? subClosest = null;
	// 		float subRadius = radius;
	// 		for( int j=0; j<4; j++ ){
				
	// 			Vector3 subPos = pos+testDir*subRadius;
	// 			if( GamePhysics.GetRaycastInfo( subPos, -Vector3.up, height, Instance.wallsMask ) ){
	// 				if( isOnWalkableFloor( GamePhysics.result().point ) ){
	// 					subClosest = GamePhysics.result().point;
	// 					Debug.DrawLine( subPos, subClosest.Value, Color.yellow, 2.0f );
	// 				}else{
	// 					Tools.DrawCross( GamePhysics.result().point, Color.red, 0.05f );
	// 					break;
	// 				}
	// 			}else{
	// 				Tools.DrawCross( subPos, Color.red, 0.04f );
	// 				Tools.DrawCross( subPos-Vector3.up*height, Color.red, 0.04f );
	// 				break;
	// 			}
	// 			subRadius -= radiusErode;
	// 		}
	// 		if( subClosest.HasValue ){
	// 			float dist = Vector3.SqrMagnitude( self.floorPos-subClosest.Value );
	// 			if( dist < sqShortest ){
	// 				sqShortest = dist;
	// 				walkPos = subClosest.Value;
	// 				Debug.DrawLine( subClosest.Value-Vector3.one*0.05f, subClosest.Value+Vector3.one*0.05f, Color.green, 2.0f );
	// 			}
	// 		}
	// 		radians += radiansStep;
	// 	}
	// 	if( walkPos == null ){
	// 		Tools.DrawCross( pos, Color.red );
	// 	}
	// 	return walkPos;
	// }
}

}