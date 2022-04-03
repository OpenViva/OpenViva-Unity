using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{


public static class WorldUtil{

    public static RaycastHit hitInfo = new RaycastHit();
    public static RaycastHit[] singleHitInfoResult = new RaycastHit[]{ new RaycastHit() };
    public static RaycastHit[] shortHitInfoResult = new RaycastHit[]{ new RaycastHit(), new RaycastHit(), new RaycastHit() };
    public static RaycastHit[] bigHitInfoResult = new RaycastHit[]{ new RaycastHit(), new RaycastHit(), new RaycastHit(), new RaycastHit(), new RaycastHit(), new RaycastHit(), new RaycastHit(), new RaycastHit() };

    public static readonly int defaultMask = LayerMask.GetMask("Default");
    public static readonly int defaultLayer = LayerMask.NameToLayer("Default");
    public static readonly int characterCollisionsMask = LayerMask.GetMask("characterCollisions");
    public static readonly int ragdollCapsuleMask = LayerMask.GetMask("ragdollCapsule");
    public static readonly int selfCharacterCollisionsMask = LayerMask.GetMask("selfCharacterCollisions");
    public static readonly int uiMask = LayerMask.GetMask("UI");
    public static readonly int ragdollCapsuleLayer = LayerMask.NameToLayer("ragdollCapsule");
    public static readonly int characterCollisionsLayer = LayerMask.NameToLayer("characterCollisions");
    public static readonly int selfCharacterCollisionsLayer = LayerMask.NameToLayer("selfCharacterCollisions");
    public static readonly int grabbablesLayer = LayerMask.NameToLayer("grabbables");
    public static readonly int grabbablesMask = LayerMask.GetMask("grabbables");
    public static readonly int transparentFX = LayerMask.NameToLayer("TransparentFX");
    public static readonly int itemsLayer = LayerMask.NameToLayer("items");
    public static readonly int itemsStaticLayer = LayerMask.NameToLayer("itemsStatic");
    public static readonly int waterLayer = LayerMask.NameToLayer("Water");
    public static readonly int zonesLayer = LayerMask.NameToLayer("zones");
    public static readonly int navLayer = LayerMask.NameToLayer("nav");
    public static readonly int inertLayer = LayerMask.NameToLayer("TransparentFX");
    public static readonly int cameraLayer = LayerMask.NameToLayer("cameras");
    public static readonly int itemsMask = LayerMask.GetMask("items");
    public static readonly int itemsStaticMask = LayerMask.GetMask("itemsStatic");
    public static readonly int waterMask = LayerMask.GetMask("Water");
    public static readonly int navMask = LayerMask.GetMask("nav");
    public static readonly int objectDetectorLayer = LayerMask.NameToLayer("objectDetector");
	private static ContactPoint[] dummyContactPoint = new ContactPoint[4];

    
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
}


}