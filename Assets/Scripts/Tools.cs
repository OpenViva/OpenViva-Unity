using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine.UI;
using System.IO;




namespace viva{

public static class Vector3Extensions{

    public static Vector3 Multiply(this Vector3 a, Vector3 b){
		return new Vector3( a.x*b.x, a.y*b.y, a.z*b.z );
    }
}

public class Set<T>{

	public readonly List<T> objects = new List<T>();
	public int Count { get{return objects.Count;} }
	public void Add( T obj ){
		if( objects.Contains( obj ) ){
			return;
		}
		objects.Add( obj );
	}

	public void Add( T[] array, float length ){
		length = Mathf.Min( array.Length, length );
		for( int i=0; i<length; i++ ){
			Add( array[i] );
		}
	}

	public bool Contains( T obj ){
		return objects.Contains( obj );
	}

	public void Remove( T obj ){
		objects.Remove( obj );
	}
}

public class ShapeKey{
	public Vector3[] deltaVertices;
	public Vector3[] deltaNormals;
	public Vector3[] deltaTangents;

	public ShapeKey( int size ){

		deltaVertices = new Vector3[size];
		deltaNormals = new Vector3[size];
		deltaTangents = new Vector3[size];
	}
}

public class Tuple<T1,T2>{
	public T1 _1;
	public T2 _2;

	public Tuple( T1 T1_, T2 T2_ ){
		_1 = T1_;
		_2 = T2_;
	}
}

public static partial class Tools{

	public static T LoadJson<T>( string filepath, object overwriteTarget=null ) where T:class{
        if( File.Exists( filepath ) ){
			try{
				if( overwriteTarget == null ){
					return JsonUtility.FromJson( File.ReadAllText( filepath ), typeof(T) ) as T;
				}else{
					JsonUtility.FromJsonOverwrite( File.ReadAllText( filepath ), overwriteTarget );
					return overwriteTarget as T;
				}
			}catch( System.Exception e ){
				return null;
			}
        }else{
            return null;
        }
    }

	public static bool SaveJson( object obj, bool prettyPrint, string path ){
		try{
			var json = JsonUtility.ToJson( obj, prettyPrint );
			using( var stream = new FileStream( path, FileMode.Create ) ){
				byte[] data = Tools.UTF8ToByteArray( json );
				stream.Write( data, 0, data.Length );
				stream.Close();
			}
			return true;
        }catch( System.Exception e ){
			return false;
		}
	}

	public static NativeArray<byte> StringToUTF8NativeArray( string text ){
		var bytes = System.Text.Encoding.UTF8.GetBytes( text );
		var result = new NativeArray<byte>( bytes.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory );
		result.CopyFrom( bytes );
		return result;
	}

	public static float GetSide( Vector3 p, Transform reference ){
		return reference.InverseTransformPoint( p ).z > 0 ? 1 : -1;
	}

    public static bool EnsureFolder( string directory ){
        if( !System.IO.Directory.Exists( directory ) ){
			try{
            	System.IO.Directory.CreateDirectory( directory );
			}catch( System.Exception e ){
				return false;
			}
        }
		return true;
    }

	public static bool RayIntersectsRectTransform(RectTransform transform, Ray ray, out Vector3 worldPosition, out float distance){
		var corners = new Vector3[4];
		transform.GetWorldCorners(corners);
		var plane = new Plane(corners[0], corners[1], corners[2]);

		float enter;
		if (plane.Raycast(ray, out enter)){
			var intersection = ray.GetPoint(enter);

			var bottomEdge = corners[3] - corners[0];
			var leftEdge = corners[1] - corners[0];
			var bottomDot = Vector3.Dot(intersection - corners[0], bottomEdge);
			var leftDot = Vector3.Dot(intersection - corners[0], leftEdge);

			// If the intersection is right of the left edge and above the bottom edge.
			if (leftDot >= 0 && bottomDot >= 0)
			{
				var topEdge = corners[1] - corners[2];
				var rightEdge = corners[3] - corners[2];
				var topDot = Vector3.Dot(intersection - corners[2], topEdge);
				var rightDot = Vector3.Dot(intersection - corners[2], rightEdge);

				//If the intersection is left of the right edge, and below the top edge
				if (topDot >= 0 && rightDot >= 0)
				{
					worldPosition = intersection;
					distance = enter;
					return true;
				}
			}
		}
		worldPosition = Vector3.zero;
		distance = 0;
		return false;
	}

	public class EaseBlend{

		private float old = 0.0f;
		private float target = 0.0f;
		private float curr = 0.0f;
		private float timer = 0.0f;
		private float duration = 1.0f;

		public bool finished{ get{ return curr==target; } }

		public float value { get{ return curr; } }

		public EaseBlend(){
		}
		public void Update( float timeDelta ){
			timer = Mathf.Min(timer+timeDelta,duration);
			curr = old+(target-old)*EaseInOutQuad( timer/duration );
		}
		public float getDuration(){
			return duration;
		}
		public void reset( float _target ){
			curr = _target;
			target = _target;
			old = target;
		}
		public void StartBlend( float _target, float _duration ){
			old = curr;
			target = _target;
			timer = 0.0f;
			duration = Mathf.Max( 0.0001f, _duration );	//prevent division by zero in Update()
		}
		public float getTarget(){
			return target;
		}
	}


	public static T EnsureComponent<T>( GameObject parent ) where T : Component{
		T result = parent.GetComponent<T>();
		if( result == null ){
			return parent.AddComponent<T>();
		}
		return result;
	}

	public static T DuplicateComponent<T>(T original, GameObject destination) where T : Component{

		System.Type type = original.GetType();
		Component copy = destination.AddComponent(type);
		System.Reflection.FieldInfo[] fields = type.GetFields();
		foreach (System.Reflection.FieldInfo field in fields)
		{
			field.SetValue(copy, field.GetValue(original));
		}
		return copy as T;
	}
	public static float FlatXZVectorToDegrees( Vector3 vec ){
		if( vec.x == 0.0f && vec.z == 0.0f ){
			return 0;
		}
		return Mathf.Atan2( vec.x, vec.z )*Mathf.Rad2Deg;
	}
	
    public static bool ArchiveFile( string filepath, string destFilepath ){
        filepath = Path.GetFullPath( filepath );
        destFilepath = Path.GetFullPath( destFilepath );
        if( destFilepath != filepath ){
            System.IO.File.Copy( filepath, destFilepath, true );
            Debug.Log("Archived file "+destFilepath);
            return true;
        }
        return false;
    }

	public static byte[] UTF8ToByteArray( string input ){
		return System.Text.Encoding.UTF8.GetBytes( input );
	}
	
	public static string ByteArrayToUTF8( byte[] input, int index, int count ){
		return System.Text.Encoding.UTF8.GetString( input, index, count );
	}

	public static byte[] StringToBase64ByteArray( string input ){
		return System.Convert.FromBase64String( input );
	}
	
	public static string Base64ByteArrayToString( byte[] input, int index, int count ){
		return System.Convert.ToBase64String( input, index, count );
	}

	public static string UTF8ByteArrayToString( byte[] input, int index, int count ){
		return System.Text.Encoding.UTF8.GetString( input, index, count );
	}

	public static float Bearing( Transform source, Vector3 point ){
		
		Vector3 toSource = source.transform.position-point;
		float newFacingYaw = Mathf.Atan2( -toSource.x, -toSource.z )*Mathf.Rad2Deg;
		return Mathf.DeltaAngle( source.eulerAngles.y, newFacingYaw );
	}

	public static float RemapClamped( float low, float high, float lowOut, float highOut, float val ){
		return lowOut+(highOut-lowOut)*Mathf.Clamp01( (val-low)/(high-low) );
	}

	public static float ColorDistance( Color32 c1, Color32 c2 ){
		long rmean = ((long)c1.r + (long)c2.r) / 2;
		long r = (long)c1.r - (long)c2.r;
		long g = (long)c1.g - (long)c2.g;
		long b = (long)c1.b - (long)c2.b;
		return Mathf.Sqrt((((512 + rmean) * r * r) >> 8) + 4 * g * g + (((767 - rmean) * b * b) >> 8));
	}

	public static float Pitch( Transform source, Transform toSource ){
		
		Vector3 local = source.InverseTransformPoint( toSource.position );
		float dist = local.x*local.x+local.z*local.z;
		if( dist == 0.0f ){
			return 0.0f;
		}
		dist = Mathf.Sqrt( dist );
		return Mathf.Atan2( local.y, dist )*Mathf.Rad2Deg;
	}

	public static float EaseInOutCubic( float t ){
		return t<0.5 ? 4.0f*t*t*t : (t-1.0f)*(2.0f*t-2.0f)*(2.0f*t-2.0f)+1.0f;
	}
	public static float EaseInOutQuad( float t ){
		return t<0.5f ? 2.0f*t*t : -1.0f+(4.0f-2.0f*t)*t;
	}
	public static float EaseInQuad( float t ){
		return t*t;
	}
	public static float EaseOutQuad( float t ){
		return t*(2.0f-t);
	}

	public static Vector3 ClampWithinSphere( Vector3 point, Vector3 center, float radius ){
		Vector3 dir = point-center;
		float currR = dir.magnitude;
		float clampedR = Mathf.Min( radius, currR );
		return center+(dir/currR)*clampedR;
	}

	public static Vector3 ClosestPointToCapsule( Vector3 lineA, Vector3 lineB, float radius, Vector3 point ){
		var closest = ClosestPointToSegment( lineA, lineB, point );
		closest += ( point-closest ).normalized*radius;
		return closest;
	}

	public static Vector3 CapsuleDirectionToVector3( int dir ){
		switch( dir ){
		case 0:
			return Vector3.right;
		case 1:
			return Vector3.up;
		default:
			return Vector3.forward;
		}
	}

	public static Vector3 ClosestPointToCylinder( Vector3 lineA, Vector3 lineB, float radius, Vector3 point ){
		Vector3 dir = lineB-lineA;
		float l = dir.magnitude;
		if( l == 0.0f ){
			return lineA;
		}
		dir /= l;
		Vector3 v = point-lineA;
		float d = Vector3.Dot( v, dir );
		var circleEnd = lineA+dir*Mathf.Clamp( d, 0, l );
		return circleEnd+Vector3.ProjectOnPlane( point-circleEnd, dir ).normalized*radius;
	}

	public static float PointToCapsuleSqDistance( Vector3 lineA, Vector3 lineB, float radius, Vector3 point ){
		return Vector3.SqrMagnitude( ClosestPointToCapsule( lineA, lineB, radius, point )-point );
	}

	public static float PointToSegmentSqDistance( Vector3 lineA, Vector3 lineB, Vector3 point ){
		return Vector3.SqrMagnitude( ClosestPointToSegment( lineA, lineB, point )-point );
	}

	public static Vector3 ClosestPointToSegment( Vector3 lineA, Vector3 lineB, Vector3 point ){
		Vector3 dir = lineB-lineA;
		float l = dir.magnitude;
		if( l == 0.0f ){
			return lineA;
		}
		dir /= l;
		Vector3 v = point-lineA;
		float d = Vector3.Dot( v, dir );
		return lineA+dir*Mathf.Clamp( d, 0, l );
	}

	public static Vector3 RoundToNearestAxis( Vector3 unitVec ){
		var absVec = unitVec;
		absVec.x = Mathf.Abs( absVec.x );
		absVec.y = Mathf.Abs( absVec.y );
		absVec.z = Mathf.Abs( absVec.z );
		var largest = Mathf.Max( absVec.x, Mathf.Max( absVec.y, absVec.z ) );

		int index = 0;
		if( largest == absVec.y ){
			index = 1;
		}else if( largest == absVec.z ){
			index = 2;
		}

		absVec = Vector3.zero;
		absVec[ index ] = Mathf.Sign( unitVec[index] );
		return absVec;
	}

	public static float SqDistanceToLine( Vector3 linePoint, Vector3 lineDir, Vector3 point){
		return Vector3.Cross( lineDir, point-linePoint ).sqrMagnitude;
	}

	public static float PointOnRayRatio( Vector3 lineA, Vector3 lineB, Vector3 point ){
		Vector3 dir = lineB-lineA;
		float dirLength = dir.magnitude;
		if( dirLength == 0.0f ){
			return 0.0f;
		}
		Vector3 norm =  dir/dirLength;
		return Vector3.Dot( norm, point-lineA )/dirLength;
	}
	public static T[] CombineArrays<T>( T[] a, T[] b ){
		T[] result = new T[ a.Length+b.Length ];
		System.Buffer.BlockCopy( a, 0, result, 0, a.Length );
		System.Buffer.BlockCopy( b, 0, result, a.Length, b.Length );
		return result;
	}
	public static Bounds CalculateCenterAndBoundingHeight( GameObject obj, float padding ){
		
		List<Component> colliders = new List<Component>();
		obj.transform.GetComponents( typeof(Collider), colliders );
		
		Bounds initBounds = (colliders[0] as Collider).bounds;
		Bounds bounds = new Bounds( initBounds.center, initBounds.size );
		for( int i=1; i<colliders.Count; i++ ){
			Collider collider = colliders[i] as Collider;
			bounds.Encapsulate( collider.bounds );
		}
		bounds.min -= Vector3.one*padding;
		bounds.max += Vector3.one*padding;
		return bounds;
	}
	public static float GetClampedRatio( float lo, float hi, float test ){
		if( lo > hi ){
			return 1.0f-Mathf.Clamp01( (test-lo)/(hi-lo) );
		}else if( hi > lo ){
			return Mathf.Clamp01( (test-lo)/(hi-lo) );
		}else{
			return 0;
		}
	}
	public static void DrawCross( Vector3 pos, Color color, float radius = 0.4f, float duration=4.0f ){
		Debug.DrawLine( pos+new Vector3( radius, 0.0f, 0.0f ), pos-new Vector3( radius, 0.0f, 0.0f ), color, duration );
		Debug.DrawLine( pos+new Vector3( 0.0f, radius, 0.0f ), pos-new Vector3( 0.0f, radius, 0.0f ), color, duration );
		Debug.DrawLine( pos+new Vector3( 0.0f, 0.0f, radius ), pos-new Vector3( 0.0f, 0.0f, radius ), color, duration );
	}
	public static void DrawDiagCross( Vector3 pos, Color color, float radius = 0.4f, float duration=4.0f ){
		Debug.DrawLine( pos+new Vector3( radius, radius, -radius ), pos+new Vector3( -radius, -radius, radius ), color, duration );
		Debug.DrawLine( pos+new Vector3( -radius, radius, radius ), pos+new Vector3( radius, -radius, -radius ), color, duration );
		Debug.DrawLine( pos+new Vector3( -radius, radius, -radius ), pos+new Vector3( radius, -radius, radius ), color, duration );
		Debug.DrawLine( pos+new Vector3( -radius, -radius, -radius ), pos+new Vector3( radius, radius, radius ), color, duration );
	}

	public static Vector2? CircleCircleIntersection( Vector2 p0, Vector2 p1, float r0, float r1, bool positiveSolution ){
		float d = Vector3.Distance( p0, p1 );
		if( d == 0 ) return null;
		if( d >= r0+r1 ) return null;
		if( d <= Mathf.Abs(r0-r1) ) return null;

		var a = (r0*r0-r1*r1+d*d)/( 2*d );
		var h = Mathf.Sqrt( r0*r0-a*a );
		var x2 = p0.x+a*(p1.x-p0.x)/d;   
		var y2 = p0.y+a*(p1.y-p0.y)/d;   
		if( positiveSolution ) return new Vector2( x2-h*(p1.y-p0.y)/d, y2+h*(p1.x-p0.x)/d );
		else return new Vector2( x2+h*(p1.y-p0.y)/d, y2-h*(p1.x-p0.x)/d );
	}

	public static void AverageQuaternion( ref Vector4 cumulative, float weight, Quaternion newRotation, Quaternion firstRotation ){
		if( !AreQuaternionsClose(newRotation, firstRotation) ){
			newRotation = new Quaternion(-newRotation.x, -newRotation.y, -newRotation.z, -newRotation.w);
		}
		cumulative.x += newRotation.x*weight;
		cumulative.y += newRotation.y*weight;
		cumulative.z += newRotation.z*weight;
		cumulative.w += newRotation.w*weight;
	}
	
	public static bool AreQuaternionsClose(Quaternion q1, Quaternion q2){
		return Quaternion.Dot(q1, q2) >= 0.0f;
	}

	public static Vector3 ClosestPointOnBoxColliderSurface( Vector3 point, BoxCollider box ){
		var closest = box.ClosestPoint( point );
		var local = box.transform.InverseTransformPoint( closest ) - box.center;
         
		float halfX = (box.size.x * 0.5f);
		float halfY = (box.size.y * 0.5f);
		float halfZ = (box.size.z * 0.5f);
		if( local.x > halfX || local.x < -halfX || 
			local.y > halfY || local.y < -halfY || 
			local.z > halfZ || local.z < -halfZ ){
			return closest;
		}else{
			Vector3 deltaAbs = new Vector3(
				halfX-Mathf.Abs( local.x ),
				halfY-Mathf.Abs( local.y ),
				halfZ-Mathf.Abs( local.z )
			);
			var largest = Mathf.Min( deltaAbs.x, Mathf.Min( deltaAbs.y, deltaAbs.z ) );
			if( largest == deltaAbs.x ){
				local.x = halfX*Mathf.Sign( local.x );
			}else if( largest == deltaAbs.y ){
				local.y = halfY*Mathf.Sign( local.y );
			}else{
				local.z = halfZ*Mathf.Sign( local.z );
			}
			return box.transform.TransformPoint( local );
		}
	}

	public static bool PointInsideBoxCollider( Vector3 point, BoxCollider box ){
         point = box.transform.InverseTransformPoint( point ) - box.center;
         
         float halfX = (box.size.x * 0.5f);
         float halfY = (box.size.y * 0.5f);
         float halfZ = (box.size.z * 0.5f);
         if( point.x < halfX && point.x > -halfX && 
            point.y < halfY && point.y > -halfY && 
            point.z < halfZ && point.z > -halfZ )
             return true;
         else
             return false;
     }

	public static void GizmoArrow( Vector3 a, Vector3 b, float shrinkPercent=0.0f ){
		
		Vector3 diff = b-a;
		a += diff*shrinkPercent*0.5f;
		b -= diff*shrinkPercent*0.5f;
		
		Gizmos.DrawLine( a, b );
		Vector3 norm = (b-a).normalized;
		norm = Quaternion.Euler( 0.0f, -30.0f, 0.0f )*norm;
		Gizmos.DrawLine( b, b-norm*0.04f );
		norm = Quaternion.Euler( 0.0f, 60.0f, 0.0f )*norm;
		Gizmos.DrawLine( b, b-norm*0.04f );
	}

	public static void DrawArrow( Vector3 a, Vector3 b, float shrinkPercent=0.0f ){
		
		Vector3 diff = b-a;
		a += diff*shrinkPercent*0.5f;
		b -= diff*shrinkPercent*0.5f;
		
		Debug.DrawLine( a, b, Color.green, 4.0f );
		Vector3 norm = (b-a).normalized;
		norm = Quaternion.Euler( 0.0f, -30.0f, 0.0f )*norm;
		Debug.DrawLine( b, b-norm*0.04f, Color.green, 4.0f );
		norm = Quaternion.Euler( 0.0f, 60.0f, 0.0f )*norm;
		Debug.DrawLine( b, b-norm*0.04f, Color.green, 4.0f );
	}

	public static int SafeFloorToInt( float f ){
		return Mathf.FloorToInt( f+0.01f );
	}

	public static Vector3 FlatForward( Vector3 forward ){
		forward.y = 0.0001f;
		return forward.normalized;
	}

	public static float CircleArea( float r ){
		return Mathf.PI*r*r;
	}

    public static float ApproximateVolume( Collider collider ){
        var sc = collider as SphereCollider;
        if( sc ){
			var r = sc.radius*collider.transform.lossyScale.y;
            return (4f/3f)*Mathf.PI*r*r*r;
        }
        var cc = collider as CapsuleCollider;
        if( cc ){
			var r = cc.radius*collider.transform.lossyScale.y;
			var h = cc.height*collider.transform.lossyScale.y;
            var heightOnly = Mathf.Max( h, r*2 )/2-r;
            return Mathf.PI*r*r*((4f/3f)*r+heightOnly);
        }
        var bc = collider as BoxCollider;
        if( bc ){
            var size = Vector3.zero;
            size.x = collider.transform.lossyScale.x*bc.size.x;
            size.y = collider.transform.lossyScale.y*bc.size.y;
            size.z = collider.transform.lossyScale.z*bc.size.z;
            return size.x*size.y*size.z;
        }
        var mc = collider as MeshCollider;
        if( mc ){
            return collider.transform.lossyScale.x*mc.sharedMesh.bounds.size.magnitude;
        }
        return 0;
    }

	public static Vector3 DirToVector3( int coord ){
		switch( coord ){
		case 0:
			return Vector3.right;
		case 1:
			return Vector3.up;
		default:
			return Vector3.forward;
		}
	}

	public static float CircleSectorArea( float r, float h ){
		if( r == 0 ) return 0;
		var H = Mathf.Clamp(Mathf.Abs(h),0,r);
		float theta = Mathf.Acos( H/r )*2;
		if( h < 0 ){
			theta = 2*Mathf.PI-theta;
		}
		return r*r*( theta-Mathf.Sin(theta) )/2f;
	}

	public static Quaternion SafeLookRotation( Vector3 from, Vector3 to ){
		var diff = to-from;
		if( diff.sqrMagnitude <= Mathf.Epsilon ) return Quaternion.identity;
		return Quaternion.LookRotation( diff, Vector3.up );
	}
	
	public static Transform SearchTransformFamily( Transform branch, string target ){
		
		if( branch.name == target ){
			return branch;
		}
		Transform result = null;
		for( int i=0; i<branch.childCount; i++ ){
			Transform child = branch.GetChild(i);
			result = SearchTransformFamily( child, target );
			if( result != null ){
				break;
			}
		}
		return result;
	}
	
	public static bool ExploreFile( string filePath ) {
		if (!System.IO.File.Exists(filePath)) {
			return false;
		}
		//Clean up file path so it can be navigated OK
		filePath = System.IO.Path.GetFullPath(filePath);
		System.Diagnostics.Process.Start("explorer.exe", string.Format("/select,\"{0}\"", filePath));
		return true;
	}
	
    public static Texture2D CreateThumbnailTexture( string stringData, int resolution, TextureFormat format, bool mipMap ){
        var data = Tools.StringToBase64ByteArray( stringData );
        Texture2D thumbnailTex = new Texture2D( resolution, resolution, format, mipMap, true );
		try{
			thumbnailTex.LoadRawTextureData( data );
			thumbnailTex.Apply( false, true );
		}catch( System.Exception e ){
			Texture2D.DestroyImmediate( thumbnailTex );
			return null;
		}
        return thumbnailTex;
    }

	public static bool IsOverriden<T>( string methodName ){
		var m = typeof(T).GetMethod( methodName );
		if( m == null ) return false;
        return m.GetBaseDefinition().DeclaringType != m.DeclaringType;
    }

	public static int CombineHashes( int a, int b ){
		int h = 17;
		h = h*37+a;
		return h*37+b;
	}
	
	public static Transform SearchTransformFamily( Transform branch, Transform target ){
		
		if( branch == target ){
			return branch;
		}
		Transform result = null;
		for( int i=0; i<branch.childCount; i++ ){
			Transform child = branch.GetChild(i);
			result = SearchTransformFamily( child, target );
			if( result != null ){
				break;
			}
		}
		return result;
	}
	
	public static T SearchTransformAncestors<T>( Transform branch ) where T:Component{
		//percolate down transforms until a Mechanism is found
		Transform parent = branch;
		while( parent != null ){
			Component[] components = parent.GetComponents(typeof(Component));
			for( int i=0; i<components.Length; i++ ){
				T candidate = components[i] as T;
				if( candidate != null ){
					return candidate;
				}
			}
			parent = parent.parent;
		}
		return null;
	}
	
	public static T SearchTransformFirstAncestors<T>( Transform branch ) where T:Component{
		//percolate down transforms until a Mechanism is found
		Transform parent = branch;
		while( parent != null ){
			Component[] components = parent.GetComponents(typeof(Component));
			for( int i=0; i<components.Length; i++ ){
				T candidate = components[i] as T;
				if( candidate != null ){
					return candidate;
				}
			}
			parent = parent.parent;
		}
		return null;
	}
	
	//returns true if an object that can be highlighted was found
	public static T FindClosestToSphere<T>( Vector3 point, float radius, int mask, QueryTriggerInteraction queryTrigger=QueryTriggerInteraction.Collide ) where T:Component{
		
		Collider[] results = Physics.OverlapSphere( point, radius, mask, queryTrigger );
		if( results == null ){
			return null;
		}
		foreach( Collider result in results ){
			T candidate = SearchTransformAncestors<T>( result.transform );
			if( candidate == null ){
				continue;
			}
			return candidate;
		}
		return null;
	}
}

}