using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace viva{


public static class GamePhysics {

	private static RaycastHit testHit = new RaycastHit();
	private static ContactPoint[] dummyContactPoint = new ContactPoint[4];

	public static bool GetRaycastInfo( Vector3 point, Vector3 dir, float length, int mask, QueryTriggerInteraction queryTrigger=QueryTriggerInteraction.Collide, float padding=0.01f ){

		if( Physics.Raycast( point, dir, out testHit, length, mask, queryTrigger ) ){
			testHit.point += testHit.normal*padding;
			return true;
		}
		return false;
	}

	public static Vector3? getRaycastPos( Vector3 point, Vector3 dir, float length, int mask, QueryTriggerInteraction queryTrigger=QueryTriggerInteraction.Collide, float padding=0.01f ){
		
		Vector3? pos = null;
		if( Physics.Raycast( point, dir, out testHit, length, mask, queryTrigger ) ){
			pos = testHit.point+testHit.normal*padding;
		}
		return pos;
	}
	
	public static Vector3? AverageContactPosition( Collision collision, int maxContacts ){
		maxContacts = Mathf.Min( maxContacts, dummyContactPoint.Length );
		int contactCount = Mathf.Min( maxContacts, collision.GetContacts( dummyContactPoint ) );
		if( contactCount == 0 ){
			return null;
		}
		Vector3 avg = Vector3.zero;
		for( int i=0; i<contactCount; i++ ){
			avg += dummyContactPoint[i].point;
		}
		return avg/contactCount;
	}

	
	public static RaycastHit result(){
		return testHit;
	}
}

}