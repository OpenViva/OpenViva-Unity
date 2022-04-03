using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace viva{

public delegate void GrabbableReturnFunc( Grabbable grabbable );

public class Grabbable : MonoBehaviour{

    public enum Type{
        CYLINDER,
        BOX
    }

    public new Collider collider { get; private set; }
    public Type? type { get; private set; } = null;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    [SerializeField]
    private Rigidbody m_rigidBody;
    public Rigidbody rigidBody { get{ return m_rigidBody; } }
    [SerializeField]
    private VivaInstance m_parent;
    public VivaInstance parent { get{ return m_parent; } }
    public Item parentItem { get{ return parent as Item; } }
    public Character parentCharacter { get{ return parent as Character; } }
    public ListenerOnGrabContext onGrabbed { get; private set; }
    public ListenerOnGrabContext onReleased { get; private set; }
    private List<GrabContext> contexts = new List<GrabContext>();
    public bool isBeingGrabbed { get{ return contexts.Count>0; } }
    public int grabContextCount { get{ return contexts.Count; } }
    private GrabbableSettings defaultSettings = new GrabbableSettings(){ uprightOnly=0, freelyRotate=false };
    public GrabbableSettings settings { get{ return tempOverrideSettings!=null ? tempOverrideSettings : defaultSettings; } }
    private GrabbableSettings tempOverrideSettings;


    public void OverrideSettingsForNextGrab( GrabbableSettings newSettings ){
        tempOverrideSettings = newSettings;
    }

    public static Grabbable[] BuidGrabbables( Collider[] colliders ){
        var grabbables = new Grabbable[ colliders.Length ];
        for( int i=0; i<colliders.Length; i++ ){
            var copyCollider = colliders[i];
            var copyContainer = copyCollider.transform;
            
            var container = new GameObject("grab_"+copyCollider.name).transform;
            container.parent = copyContainer.parent;
            container.localPosition = copyContainer.localPosition;
            container.localRotation = copyContainer.localRotation;
            container.localScale = copyContainer.localScale;
            var cc = container.gameObject.AddComponent<CapsuleCollider>();
            cc.isTrigger = true;
            var grabbable = container.gameObject.AddComponent<Grabbable>();
            // grabbable._InternalInitialize();
            grabbables[i] = grabbable;

            var copyCC = copyCollider as CapsuleCollider;
            if( copyCC ){
                cc.direction = copyCC.direction;
                cc.radius = copyCC.radius;
                cc.height = copyCC.height;
            }else{
                var copyBC = copyCollider as BoxCollider;
                if( copyBC ){
                    cc.direction = 1;
                    cc.center = copyBC.center;
                    cc.radius = copyBC.size.x*2;
                    cc.height = copyBC.size.y;
                }
            }
        }
        return grabbables;
    }
    
    public static void _InternalSetup( VivaInstance parent, Rigidbody rigidbody, Grabbable grabbable, GrabbableSettings grabbableSettings ){
        
        grabbable.collider = grabbable.gameObject.GetComponent<Collider>();
        if( grabbable.collider == null ){
            Debugger.LogError("Grabbable does not have a collider attached to it!");
        }else{
            if( grabbable.collider as CapsuleCollider ) grabbable.type = Type.CYLINDER;
            if( grabbable.collider as BoxCollider ) grabbable.type = Type.BOX;
        }

        grabbable.m_rigidBody = rigidbody;
        grabbable.defaultSettings = grabbableSettings;
        grabbable.m_parent = parent;
        grabbable._InternalReset();
    }

    public void _InternalReset(){
        var safeCopy = contexts.ToArray();
        foreach( var context in safeCopy ) Viva.Destroy( context );

        if( onGrabbed == null ){    //fix race condition when Muscles initialize before Grabbables
            onGrabbed = new ListenerOnGrabContext( contexts, "onGrabbed", true );
            onReleased = new ListenerOnGrabContext( null, "onReleased", false );
        }
        onGrabbed._InternalReset();
        onReleased._InternalReset();
        onGrabbed._InternalAddListener( OnGrabbed );
        onReleased._InternalAddListener( OnReleased );
    }

    public bool IsBeingGrabbedByCharacter( Character targetCharacter ){
        if( targetCharacter == null ) return false;
        foreach( var context in contexts ){
            if( context.grabber != null && context.grabber.character == targetCharacter ) return true;
        }
        return false;
    }

    public List<GrabContext> GetGrabContextsByCharacter( Character targetCharacter ){
        var result = new List<GrabContext>();
        if( targetCharacter == null ) return contexts;
        foreach( var context in contexts ){
            if( context.grabber != null && context.grabber.character == targetCharacter ){
                result.Add( context );
            }
        }
        return result;
    }

    public List<Character> GetCharactersGrabbing(){
        var result = new List<Character>();
        foreach( var context in contexts ){
            if( context.grabber != null && !result.Contains( context.grabber.character ) ){
                result.Add( context.grabber.character );
            }
        }
        return result;
    }

    public GrabContext GetGrabContext( int index ){
        if( index < 0 || index >= grabContextCount ){
            throw new System.Exception("Out of bounds index access in \"GetGrabContext\"");
        }
        return contexts[ index ];
    }

    public List<GrabContext> GetGrabContexts( Grabber grabber ){
        var from = new List<GrabContext>();
        if( grabber == null ) return from;
        foreach( var context in contexts ){
            if( context.grabber && context.grabber == grabber ){
                from.Add( context );
            }
        }
        return from;
    }

    public List<Grabber> GetGrabbers(){
        var grabbers = new List<Grabber>();
        foreach( var context in contexts ){
            var grabber = context.grabber;
            if( grabbers.Contains( grabber ) ) continue;
            grabbers.Add( grabber );
        }
        return grabbers;
    }

    private void OnGrabbed( GrabContext grabContext ){
        contexts.Add( grabContext );
    }
    
    private void OnReleased( GrabContext grabContext ){
        contexts.Remove( grabContext );
        tempOverrideSettings = null;
    }

    public void ReleaseAll(){
        var safeCopy = contexts.ToArray();
        foreach( var context in safeCopy ) Viva.Destroy( context );

        if( contexts.Count != 0 ) Debug.LogError("FATAL ERROR CONTEXTS WAS NOT CLEARED");
    }

    public void Show(){
        if( meshRenderer != null ) return;

        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = BuiltInAssetManager.main.grabbableMaterial;
        meshRenderer.receiveShadows = false;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        
        meshFilter = gameObject.AddComponent<MeshFilter>();

        switch( type ){
        case Type.CYLINDER:
            meshFilter.sharedMesh = BuiltInAssetManager.main.grabbableCylinderMesh;
            var cc = collider as CapsuleCollider;
            meshRenderer.material.SetVector("_Scale",new Vector3( cc.radius, cc.height-cc.radius*2, cc.radius ));
            break;
        }
    }

    public float GetNearbyDistance( Vector3 toPoint ){
        toPoint = transform.InverseTransformPoint( toPoint );   //localize
        switch( type ){
        case Type.CYLINDER:
            var cc = collider as CapsuleCollider;
            var extent = Tools.CapsuleDirectionToVector3( cc.direction )*Mathf.Max( cc.height, cc.radius*2 )/2;
            return Tools.PointToCapsuleSqDistance( cc.center+extent, cc.center-extent, cc.radius, toPoint );
        case Type.BOX:
            var bc = collider as BoxCollider;
            var closestPoint = bc.bounds.ClosestPoint( toPoint );
            return Vector3.Distance( closestPoint, toPoint );
        }
        return Mathf.Infinity;
    }

    public bool GetBestGrabPose( out Vector3 grabPos, out Quaternion grabRot, Grabber grabber ){
        grabPos = Vector3.zero;
        grabRot = Quaternion.identity;
        switch( type ){
        case Type.CYLINDER:
            return CalculateCylinderGrabPose( ref grabPos, ref grabRot, grabber );
        case Type.BOX:
            return CalculateBoxGrabPose( ref grabPos, ref grabRot, grabber );
        }
        return false;
    }

    private bool CalculateBoxGrabPose( ref Vector3 grabPos, ref Quaternion grabRot, Grabber grabber ){
        var bc = collider as BoxCollider;
        if( !bc ) return false;

        grabPos = bc.transform.TransformPoint( bc.center );

        //find nearest axis
        var axes = new Vector3[]{
            bc.transform.right,
            bc.transform.up,
            bc.transform.forward
        };

        var right = grabber.transform.forward*-grabber.sign;
        var up = grabber.transform.right*-grabber.sign;

        float highestDot = 0;
        int? axisA = null;
        for( int i=0; i<3; i++ ){
            var candidateRight = Mathf.Abs( Vector3.Dot( right, axes[i] ) );

            if( candidateRight > highestDot ){
                highestDot = candidateRight;
                axisA = i;
            }
        }

        if( !axisA.HasValue ) return false;

        //find nearest axis line
        var worldCenter = bc.transform.TransformPoint( bc.center );
        var worldSize = bc.size;
        worldSize.x *= bc.transform.lossyScale.x;
        worldSize.y *= bc.transform.lossyScale.y;
        worldSize.z *= bc.transform.lossyScale.z;
        var shortestDist = Mathf.Infinity;
        int? shortestIndex = null;
        var offsetA = axes[ axisA.Value ]*0.5f*worldSize[ axisA.Value ];
        var axisB = ( axisA.Value+1 )%3;
        var axisC = ( axisA.Value+2 )%3;
        var offsetB = axes[ axisB ]*0.5f*worldSize[ axisB ];
        var offsetC = axes[ axisC ]*0.5f*worldSize[ axisC ];
        for( int i=0; i<4; i++ ){
            var signB = (i%2)*2-1;  //-1, 1,-1, 1
            var signC = (i/2)*2-1;  //-1,-1, 1, 1

            var center = worldCenter+offsetB*signB+offsetC*signC;
            var p0 = center+offsetA;
            var p1 = center-offsetA;

            var closestPoint = Tools.ClosestPointToSegment( p0, p1, grabber.worldGrabCenter );
            var dist = Vector3.SqrMagnitude( closestPoint-grabber.worldGrabCenter );
            if( dist > grabber.width*grabber.width ) continue;

            //cannot grab if on other side of cube
            var cornerNormal = axes[ axisB ]*signB+axes[ axisC ]*signC; //no need to normalize because testing with zero
            if( Vector3.Dot( cornerNormal, up ) <= 0 ) continue;
            Debug.DrawLine( closestPoint, closestPoint+cornerNormal.normalized*0.02f, Color.yellow, Time.fixedDeltaTime );

            //axis point must not be upside down relative to hand
            var grabberNormal = (grabber.worldGrabCenter-closestPoint).normalized;
            if( Vector3.Dot( grabberNormal, up ) < 0.4f ) continue;

            if( dist < shortestDist ){
                grabPos = closestPoint;
                shortestDist = dist;
                shortestIndex = i;
            }
        }

        if( !shortestIndex.HasValue ) return false;
        {
            var signB = (shortestIndex.Value%2)*2-1;  //-1, 1,-1, 1
            var signC = (shortestIndex.Value/2)*2-1;  //-1,-1, 1, 1
            
            var cornerNormal = axes[ axisB ]*signB+axes[ axisC ]*signC; //no need to normalize because testing with zero

            //round to nearest axis for rotation snap
            var grabForward = bc.transform.TransformDirection( Tools.RoundToNearestAxis( bc.transform.InverseTransformDirection( grabber.transform.forward ) ) );
            var grabUp = bc.transform.TransformDirection( Tools.RoundToNearestAxis( bc.transform.InverseTransformDirection( grabber.transform.up ) ) );

            Debug.DrawLine( grabPos, grabPos+grabForward*0.04f, Color.blue, Time.fixedDeltaTime );
            Debug.DrawLine( grabPos, grabPos+grabUp*0.04f, Color.green, Time.fixedDeltaTime );
            grabRot = Quaternion.LookRotation( grabForward, grabUp );
        }
        return true;
    }

    private bool CalculateCylinderGrabPose( ref Vector3 grabPos, ref Quaternion grabRot, Grabber grabber ){
        var localGrabCenter = transform.InverseTransformPoint( grabber.worldGrabCenter );   //localize
        var cc = collider as CapsuleCollider;
        if( !cc ) return false;

        var grabEulerOffset = BuiltInAssetManager.main.grabbableCylinderDirectionOffset[ cc.direction ];
        Vector3 localUp;
        Vector3 localForward;
        int coord;
        switch( cc.direction ){
        case 0:
            localUp = Vector3.right;
            localForward = Vector3.up;
            coord = 2;
            break;
        case 1:
            localUp = Vector3.up;
            localForward = Vector3.forward;
            coord = 0;
            break;
        default:
            localUp = Vector3.forward;
            localForward = Vector3.right;
            coord = 1;
            break;
        }

        grabEulerOffset[coord] *= grabber.sign;
        var heightOnly = Mathf.Max( cc.height, cc.radius*2 )/2-cc.radius;
        heightOnly = Mathf.Max( 0.00001f, heightOnly-grabber.width/transform.lossyScale.x );  //make room for grab width by subtracting cylinder height, add bare minimum height so cylinder has direction

        var extent = localUp*heightOnly;

        var localClosestPointSeg = Tools.ClosestPointToSegment( cc.center+extent, cc.center-extent, localGrabCenter );
        var localClosestPointCyl = Tools.ClosestPointToCylinder( cc.center+extent, cc.center-extent, cc.radius, localGrabCenter );

        var worldClosestPointSeg = transform.TransformPoint( localClosestPointSeg );
        var worldClosestPointCyl = transform.TransformPoint( localClosestPointCyl );

        grabPos = worldClosestPointCyl;

        var up = transform.TransformDirection( localUp );

        float signFlip;
        if( settings != null && settings.uprightOnly != 0 ){
            if( settings.preferredGrabDegree >= 0f ){
                signFlip = Mathf.Sign( settings.uprightOnly );
            }else{
                signFlip = Mathf.Sign( settings.uprightOnly )*grabber.sign;
            }
        }else{
            signFlip = System.Convert.ToInt32( Vector3.Dot( up, grabber.rigidBody.transform.forward ) > 0 )*2-1;
            signFlip *= System.Convert.ToInt32( grabber.rigidBody.transform.InverseTransformPoint( worldClosestPointSeg )[coord] > 0 )*2-1;
        }
        var lookDir = worldClosestPointCyl-worldClosestPointSeg;
        if( lookDir.sqrMagnitude <= Mathf.Epsilon ){
            lookDir = transform.forward;
        }
        grabRot = Quaternion.LookRotation( lookDir, up*signFlip )*Quaternion.Euler( grabEulerOffset );
        
        //adjust to rotate to desired rotation forward, if any
        if( settings != null && settings.preferredGrabDegree >= 0f ){
            var forward = transform.TransformDirection( localForward );
            var grabberForward = grabRot*Vector3.right*grabber.sign;
            var degreesToRotate = Vector3.Angle( forward, grabberForward );

            if( Vector3.Dot( Vector3.Cross( forward, up ), grabberForward ) <= 0f ) degreesToRotate = -degreesToRotate;
            
            var adjustRotation = Quaternion.AngleAxis( degreesToRotate+settings.preferredGrabDegree*grabber.sign, up );

            grabPos -= worldClosestPointSeg;
            grabPos = adjustRotation*grabPos;
            grabPos += worldClosestPointSeg;

            grabRot = adjustRotation*grabRot;
        }

        return true;
    }

    public void Hide(){
        if( meshRenderer == null ) return;
        GameObject.Destroy( meshRenderer );
        GameObject.Destroy( meshFilter );
        meshRenderer = null;
    }
}

}