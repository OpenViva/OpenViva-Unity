using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;



namespace viva{

public delegate void OnNavSearchFinish( Vector3[] path, Vector3 navSearchPoint ,Vector3 navPointDir );

public class Nav: MonoBehaviour{

	private static readonly int MAX_SEARCHES_PER_FRAME = 1;

	public static bool IsOnWalkableFloor( Vector3 pos, float minDist ){
		return NavMesh.SamplePosition( pos, out navTest, minDist, NavMesh.AllAreas );
	}

	private static NavMeshHit navTest = new NavMeshHit();

	public abstract class PathRequest{

		public abstract Vector3? GetNextSearchPoint();
		public abstract Vector3 GetCurrentSearchPointDirection();
	}

	private Character self;
	private Coroutine navSearchCoroutine = null;

	public bool searching { get{ return navSearchCoroutine!=null; } }

	

	private void Awake(){
		self = gameObject.GetComponent<Character>();
	}


	public class LineSearch: PathRequest{

		private Vector3 lineStart;
		private Vector3 lineDiff;
		private float height;
		private int steps;
		private int currentStep = 0;

		public LineSearch( Vector3 _lineStart, Vector3 _lineEnd, float _height, int _steps, float shrinkPercent=0.0f ){
			
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
			if( Physics.Raycast( pos, Vector3.down, out WorldUtil.hitInfo, height, WorldUtil.defaultMask|WorldUtil.itemsMask|WorldUtil.itemsStaticMask ) ){
				return WorldUtil.hitInfo.point+Vector3.up*0.01f;	//pad to stay above floor
			}else{
				Debug.DrawLine( pos, pos+Vector3.down*height, Color.red, 3.0f );
				return null;
			}
		}

		public override Vector3 GetCurrentSearchPointDirection(){
			return Vector3.Cross( -lineDiff, Vector3.up ).normalized;
		}
	}

	public class PointSearch: PathRequest{

		private readonly Vector3 point;
		private readonly Vector3 dir;
		private bool searched = false;

		public PointSearch( Vector3 _point, Vector3 _dir ){
			
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

	public class SearchCircle: PathRequest{

		private readonly Vector3 center;
		private readonly float radius;
		private readonly float testHeight;
		private readonly int vertices;
		private int currentVertex = 0;
		private Vector3 dir;
		private readonly float finalRadius;
		private int growthStepCount = -1;
		private readonly int growthSteps;

		public SearchCircle( Vector3 _center, float _radius, float _testHeight, int _vertices, float? _finalRadius = null){
			center = _center+Vector3.up*MoveTo.minNodeDist;
			radius = Mathf.Max( 0.1f, _radius );
			testHeight = _testHeight+MoveTo.minNodeDist*2;
			vertices = Mathf.Clamp( _vertices, 2, 8 );
			finalRadius = _finalRadius.HasValue ? _finalRadius.Value : radius;
			growthSteps = Mathf.Min( finalRadius==radius ? 0 : Mathf.FloorToInt( ( finalRadius-radius )/MoveTo.minNodeDist )+2, 4 );
		}

		public override Vector3? GetNextSearchPoint(){
			while( true ){
				
				float radiusLerp;
				if( growthSteps == 0 ){
					radiusLerp = 0;
					currentVertex++;
				}else{
					if( ++growthStepCount >= growthSteps ){
						growthStepCount = 0;
						currentVertex++;
					}
					radiusLerp = (float)growthStepCount/growthSteps;
				}
				if( currentVertex >= vertices ){
					return null;
				}
				float testRadius = Mathf.LerpUnclamped( radius, finalRadius, radiusLerp );

				float radian = 2.0f*Mathf.PI*( (float)currentVertex/vertices );
				dir = new Vector3( Mathf.Cos( radian ), 0.0f, Mathf.Sin( radian ) );
				Vector3 edgePoint = center+dir*testRadius;

				//test direct visibility
				var mask = WorldUtil.defaultMask|WorldUtil.itemsMask|WorldUtil.itemsStaticMask|WorldUtil.waterMask;
				if( Physics.Raycast( center, edgePoint-center, out WorldUtil.hitInfo, radius, mask ) ){
					Debug.DrawLine( center, edgePoint, Color.black, 3.0f );
					continue;
				}

				//test downwards to find local floor
				if( Physics.Raycast( edgePoint, Vector3.down, out WorldUtil.hitInfo, testHeight, mask ) ){
					return WorldUtil.hitInfo.point+Vector3.up*0.01f;	//pad to stay above floor
				}else{
					Debug.DrawLine( edgePoint, edgePoint+Vector3.down*testHeight, Color.red, 3.0f );
				}
			}
		}

		public override Vector3 GetCurrentSearchPointDirection(){
			return dir;
		}
	}

	public bool RequestNavSearch( PathRequest[] requests, OnNavSearchFinish onFinish, Vector3? toClosestPoint=null ){
		if( requests == null ){
			return false;
		}
		if( navSearchCoroutine != null ){
			return false;
		}
		if( onFinish == null ){
			Debugger.LogError("Nav onFinish cannot be null!");
			return false;
		}
		Debug.Log("Continuous nav search...");
		navSearchCoroutine = StartCoroutine( NavSearch( requests, onFinish, toClosestPoint ) );
		return true;
	}

	public bool IsSearchingNav(){
		return navSearchCoroutine!=null;
	}

	private IEnumerator NavSearch( PathRequest[] requests, OnNavSearchFinish onFinish, Vector3? toClosestPoint=null ){

		yield return null; //wait 1 frame so it's not an immediate return

		while( !self.ragdoll.surface.HasValue ){
			yield return null;
		}

		Vector3 navGroundTest;
		if( self.ragdoll.surface.HasValue && self.onWater.active && self.onWater.active.surfaceY > self.ragdoll.surface.Value.y ){
			navGroundTest = new Vector3( self.ragdoll.surface.Value.x, self.onWater.active.surfaceY+MoveTo.minNodeDist, self.ragdoll.surface.Value.z );
		}else{
			navGroundTest = self.ragdoll.surface.Value;
		}
		//find max point for water for accurate sampling
		if( self.onWater.active ) navGroundTest.y = Mathf.Max( navGroundTest.y, self.onWater.active.surfaceY );
		if( !NavMesh.SamplePosition( navGroundTest, out navTest, MoveTo.minNodeDist*2.0f, NavMesh.AllAreas ) ){
			Debugger.LogError("Character not near walkable position");
			Tools.DrawCross( navGroundTest, Color.red, 0.1f, 3 );
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
				if( ++steps >= MAX_SEARCHES_PER_FRAME ){
					steps = 0;
					yield return new WaitForFixedUpdate();
				}
				Vector3? testPoint = request.GetNextSearchPoint();
				if( !testPoint.HasValue ){
					break;
				}
				if( IsOnWalkableFloor( testPoint.Value, MoveTo.minNodeDist+0.05f ) ){	//tiny padding
					NavMeshPath cachePath = new NavMeshPath();
					if( !NavMesh.CalculatePath( sourceNavPos, testPoint.Value, NavMesh.AllAreas, cachePath ) ){
						Tools.DrawCross( testPoint.Value-Vector3.down*0.2f, Color.black, 0.05f );
						continue;
					}
					if( cachePath.status == NavMeshPathStatus.PathPartial ){
						continue;
					}

					Tools.DrawDiagCross( testPoint.Value, Color.yellow, 0.02f );

					//find path closest to target and shortest path
					float pathCost;
					if( toClosestPoint.HasValue ){
						pathCost = ( toClosestPoint.Value-testPoint.Value).sqrMagnitude;
					}else{
						pathCost = PathSqLength( cachePath.corners );
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
						Tools.DrawCross( testPoint.Value, Color.green, 0.02f );
					}
				}else{
					Tools.DrawDiagCross( testPoint.Value, Color.red, 0.01f );
				}
			}
		}
		if( onFinish != null ){
			onFinish( finalPath, navSearchPoint, navPointDir );
		}
		navSearchCoroutine = null;
	}

	public static float PathSqLength( Vector3[] path ){
		float length = 0.0f;
		for( int j=path.Length-1,i=0; i<path.Length; j=i++ ){
			length += Vector3.SqrMagnitude( path[i]-path[j] );
		}
		return length;
	}

	public static float PathLength( Vector3[] path ){
		float length = 0.0f;
		for( int j=0,i=1; i<path.Length; j=i++ ){
			length += Vector3.Magnitude( path[i]-path[j] );
		}
		return length;
	}
}

}