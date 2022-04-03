using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace viva{

public delegate void GrabberCallback( Grabber grabber );

public class Grabber : MonoBehaviour{

    public static Grabber _InternalCreateGrabber( Character character, Ragdoll ragdoll, RagdollMuscle muscleIndex ){
        
        if( character == null ) throw new System.Exception("Cannot create Grabber with a null Character");
        if( ragdoll == null ) throw new System.Exception("Cannot create Grabber with a null Ragdoll");

        var target = ragdoll.muscles[ (int)muscleIndex ].rigidBody.gameObject;
        if( target == null ) throw new System.Exception("Cannot create Grabber with a null target");
        Grabber grabber = target.GetComponent<Grabber>();
        if( grabber && grabber._internalBuiltIn && grabber.character == null ){ //only once
            grabber._InternalSetup( character );
            character._InternalRegisterGrabber( grabber );
        }else{
            if( grabber ) return grabber;
            grabber = target.AddComponent<Grabber>();
            grabber._InternalSetup( character );
            character._InternalRegisterGrabber( grabber );
        }

        return grabber;
    }


    [SerializeField]
    private bool internalBuiltIn;
    public bool _internalBuiltIn { get{ return internalBuiltIn; } }
    [SerializeField]
    private Vector3 m_grabEulerOffset;
    public Vector3 grabEulerOffset { get{ return m_grabEulerOffset; } }
    [Range(-1,1)]
    [SerializeField]
    private float m_sign;
    public float sign { get{ return m_sign; } }
    [SerializeField]
    public float width;
    
    [Range(0,1)]
    public float debug;
    public Vector3 debugVec;
    public Vector3 debugVec2;
    public Character character { get; private set; }
    private List<GrabContext> contexts = new List<GrabContext>();
    public SphereCollider detectSphere;
    public FingerGrabAnimator fingerAnimator;
    public List<Grabbable> nearbys = new List<Grabbable>();
    public bool grabbing { get{ return contexts.Count>0; } }
    public float grip { get; private set; }
    public ListenerOnGrabContext onGrabbed;
    public ListenerOnGrabContext onReleased;
    public ListenerGrabbable onGrabbableNearby;
    public ListenerGrabbable onGrabbableTooFar;
    public string signName { get{
        if( sign == 1 ) return "right";
        if( sign == -1 ) return "left";
        return "generic";
    } }
    public Vector3 grabOffset = new Vector3( 0.05f, 0, 0 );
    public Vector3 worldGrabCenter { get{ return rigidBody ? rigidBody.transform.TransformPoint( grabOffset ) : Vector3.zero; } }
    public int contextCount { get{ return contexts.Count; } }
    public Rigidbody rigidBody { get; private set; }
    public float timeSinceLastRelease { get; private set; }
    public Grabbable mainGrabbed { get{
        if( contexts.Count > 0 ) return contexts[0]?.grabbable;
        return null;
    } }


    private void Awake(){
        rigidBody = gameObject.GetComponent<Rigidbody>();
    }

    private void _InternalSetup( Character _character ){
        character = _character;
        onGrabbed = new ListenerOnGrabContext( contexts, "onGrabbed", true );
        onReleased = new ListenerOnGrabContext( null, "onReleased", false );
        onGrabbableNearby = new ListenerGrabbable( nearbys, "onGrabbableNearby" );
        onGrabbableTooFar = new ListenerGrabbable( (Grabbable[])null, "onGrabbableTooFar" );
        onGrabbed._InternalAddListener( OnGrabbed );
        onReleased._InternalAddListener( OnReleased );
    }

    public void _InternalReset(){
        var safeCopy = contexts.ToArray();

        foreach( var context in safeCopy ) Viva.Destroy( context );
        contexts.Clear();

        onReleased._InternalReset();
        onGrabbed._InternalReset();

        onGrabbed._InternalAddListener( OnGrabbed );
        onReleased._InternalAddListener( OnReleased );
    }

    private void OnGrabbed( GrabContext context ){
        contexts.Add( context );
        fingerAnimator?.SetupAnimation( context.grabbable );
        context.grabbable.onGrabbed._InternalAddListener( OnStopGrabbableShare );
    }   

    private void OnReleased( GrabContext context ){
        contexts.Remove( context );
        fingerAnimator?.StopAnimation();
        timeSinceLastRelease = Time.time;

        if( context.grabbable.parentItem ){
            foreach( var grabbable in context.grabbable.parentItem.grabbables ){
                grabbable.onGrabbed._InternalRemoveListener( OnStopGrabbableShare );
            }
        }else if( context.grabbable.parentCharacter ){
            foreach( var muscle in context.grabbable.parentCharacter.ragdoll.muscles ){
                foreach( var grabbable in muscle.grabbables ){
                    grabbable.onGrabbed._InternalRemoveListener( OnStopGrabbableShare );
                }
            }
        }
    }

    private void OnStopGrabbableShare( GrabContext context ){
        if( context.source != character ){
            //destroy all by me
            if( context.grabbable.parentItem ){
                var selfGrabContexts = context.grabbable.parentItem.GetGrabContextsByGrabber( this );
                foreach( var fromContext in selfGrabContexts ) Viva.Destroy( fromContext );
            }else if( context.grabbable.parentCharacter ){
                var selfGrabContexts = context.grabbable.parentCharacter.GetGrabContexts( this );
                foreach( var fromContext in selfGrabContexts ) Viva.Destroy( fromContext );
            }
        }
    }

    public void DoNotShareCurrentGrabbables(){
        foreach( var context in contexts ){
            if( !context.grabbable.parentItem ) continue;
            foreach( var grabbable in context.grabbable.parentItem.grabbables ){
                grabbable.onGrabbed._InternalAddListener( OnStopGrabbableShare );
            }
        }
    }

    public GrabContext GetGrabContext( int index ){
        return contexts[ index ];
    }
    
    public Grabbable GetRandomGrabbable(){
        if( contexts.Count == 0 ){
            return null;
        }
        return contexts[ Random.Range( 0, contexts.Count ) ].grabbable;
    }

    public void SetGrip( float _grip ){
        grip = Mathf.Clamp01( _grip );
    }

    public void ApplyGrabPose( Vector3 worldPos, Quaternion worldRot ){
        rigidBody.transform.rotation = worldRot;
        rigidBody.transform.position = worldPos-( worldGrabCenter-rigidBody.transform.position );
    }
    public GrabContext IsGrabbing( VivaInstance instance ){
        foreach( var context in contexts ){
            if( context.grabbable.parent == instance ) return context;
        }
        return null;
    }
    public GrabContext IsGrabbing( string attribute ){
        if( attribute == null ) return null;
        foreach( var context in contexts ){
            if( !context || !context.grabbable || !context.grabbable.parentItem ) continue;
            if( context.grabbable.parentItem.HasAttribute( attribute ) ) return context;
        }
        return null;
    }
    public GrabContext IsGrabbing( AttributeRequest attributeRequest ){
        foreach( var context in contexts ){
            if( !context || !context.grabbable || !context.grabbable.parentItem ) continue;
            if( context.grabbable.parentItem.HasAttributes( attributeRequest ) ) return context;
        }
        return null;
    }
    public GrabContext IsGrabbing( Grabbable grabbable ){
        foreach( var context in contexts ){
            if( !context || !context.grabbable ) continue;
            if( context.grabbable == grabbable ){
                return context;
            }
        }
        return null;
    }
    public GrabContext IsGrabbingCharacter( string characterName ){
        if( characterName == null ) return null;
        foreach( var context in contexts ){
            if( !context || !context.grabbable || !context.grabbable.parentCharacter ) continue;
            if( context.grabbable.parentCharacter._internalSettings.name == characterName ) return context;
        }
        return null;
    }
    public List<Item> FindItems( string attribute ){
        var result = new List<Item>();
        foreach( var context in contexts ){
            if( !context || !context.grabbable || !context.grabbable.parentItem ) continue;
            if( context.grabbable.parentItem.HasAttribute( attribute ) ) result.Add( context.grabbable.parentItem );
        }
        return result;
    }

    public void Drop( Item item ){
        var grabContexts = item.GetGrabContextsByGrabber( this );
        foreach( var grabContext in grabContexts ){
            Viva.Destroy( grabContext );
        }
    }
    
    public List<Character> IsGrabbingCharacters(){
        var result = new List<Character>();
        foreach( var context in contexts ){
            if( !context || !context.grabbable || !context.grabbable.parentCharacter ) continue;
            result.Add( context.grabbable.parentCharacter );
        }
        return result;
    }

    public void AddColliderDetector( ColliderDetector colliderDetector, bool itemsOnly ){
        if( colliderDetector == null ) return;
        if( itemsOnly ){
            colliderDetector.onColliderEnter += HandleColliderItemsOnlyEnter;
        }else{
            colliderDetector.onColliderEnter += HandleColliderEnter;
        }
        colliderDetector.onColliderExit += HandleColliderExit;
    }

    public void RemoveColliderDetector( ColliderDetector colliderDetector, bool itemsOnly ){
        if( colliderDetector == null ) return;
        if( itemsOnly ){
            colliderDetector.onColliderEnter -= HandleColliderItemsOnlyEnter;
        }else{
            colliderDetector.onColliderEnter -= HandleColliderEnter;
        }
        colliderDetector.onColliderExit -= HandleColliderExit;
    }

    private void OnDrawGizmos(){
        Gizmos.DrawWireSphere( worldGrabCenter, 0.003f );
    }

    public GrabContext Grab( Grabbable grabbable, bool? smoothStartOverride=null ){
        if( grabbable == null ) return null;
        var existing = IsGrabbing( grabbable );
        if( existing ) return existing;

        SetGrip( 1.0f );
        bool shouldSmoothStart = false;
        if( !smoothStartOverride.HasValue ){
            if( character && character.isPossessed ){
                shouldSmoothStart = character.possessor.isUsingKeyboard;
            }
        }else{
            shouldSmoothStart = smoothStartOverride.Value;
        }
        return GrabContext.CreateCrabContext( grabbable, this, character, shouldSmoothStart );
    }

    public void ReleaseAll(){
        var safeCopy = contexts.ToArray();
        foreach( var context in safeCopy ) Viva.Destroy( context );
        if( contexts.Count != 0 ) Debug.LogError("FATAL ERROR CONTEXTS WAS NOT CLEARED");
        fingerAnimator?.StopAnimation();
    }

    public List<Item> GetAllItems(){
        var items = new List<Item>();
        foreach( var context in contexts ){
            if( !context || !context.grabbable || !context.grabbable.parentItem ) continue;
            items.Add( context.grabbable.parentItem );
        }
        return items;
    }

    private void HandleColliderEnter( Collider collider ){
        ValidateColliderEnter( collider, false );
    }

    private void HandleColliderItemsOnlyEnter( Collider collider ){
        ValidateColliderEnter( collider, true );
    }

    private void ValidateColliderEnter( Collider collider, bool itemsOnly ){
        if( collider.gameObject.layer != WorldUtil.grabbablesLayer ) return;

        var grabbable = collider.gameObject.GetComponent<Grabbable>();
        if( grabbable ){
            if( itemsOnly ){
                if( grabbable.parentItem == null ) return;  //ignore non items
                if( grabbable.isBeingGrabbed ) return;  //ignore those grabbed
            }

            if( character && grabbable.parentCharacter == character ) return;
            nearbys.Add( grabbable );
            onGrabbableNearby.Invoke( grabbable );
        }
    }

    public void AddNearbyGrabbable( Grabbable grabbable ){
        if( !grabbable || nearbys.Contains( grabbable ) ) return;
        nearbys.Add( grabbable );
        onGrabbableNearby.Invoke( grabbable );
    }

    public void RemoveNearbyGrabbable( Grabbable grabbable ){
        if( !grabbable ) return;
        var index = nearbys.IndexOf( grabbable );
        if( index != -1 ){
            nearbys.RemoveAt( index );
            onGrabbableTooFar?.Invoke( grabbable );
        }
    }

    private void HandleColliderExit( Collider collider ){
        if( collider.gameObject.layer != WorldUtil.grabbablesLayer ) return;
        
        var grabbable = collider.gameObject.GetComponent<Grabbable>();
        if( grabbable ){
            nearbys.Remove( grabbable );
            onGrabbableTooFar?.Invoke( grabbable );
        }
    }

    public Grabbable GetClosestGrabbable(){
        var testPos = worldGrabCenter;
        Grabbable grabbable = null;
        float shortestDist = Mathf.Infinity;
        for( int i=nearbys.Count; i-->0; ){
            var candidate = nearbys[i];
            if( candidate == null ){
                nearbys.RemoveAt(i);
                continue;
            }
            if( !candidate.enabled ) continue;
            if( character && candidate.parentCharacter == character ) continue;   //not allowed to grab self
            float candidateDist = candidate.GetNearbyDistance( testPos );
            if( candidateDist < shortestDist ){
                shortestDist = candidateDist;
                grabbable = candidate;
            }
        }
        return grabbable;
    }
}

}