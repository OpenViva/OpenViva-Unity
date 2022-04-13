using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

public partial class Item : VivaSessionAsset {

	public class Attachment{
		public readonly float oldMass;
		public readonly float oldDrag;
		public readonly float oldAngularDrag;
		public readonly Transform parent;
		public readonly Item parentItem;

		public Attachment( Rigidbody rigidBody, Transform _newParent, Item _parentItem ){
			oldMass = rigidBody.mass;
			oldDrag = rigidBody.drag;
			oldAngularDrag = rigidBody.angularDrag;
			parentItem = _parentItem;
			parent = _newParent;
		}
	}

	public delegate void OccupyStateCallback( OccupyState oldMainOccupyState, OccupyState newMainOccupyState );
	
	//DO NOT CHANGE ORDER
	public enum Type{
		NONE,
		CHARACTER,
		DONUT,
		WATER_GLASS,
		HAT,
		DUCKY,
		WATER_REED,
		POLAROID_CAMERA,
		POLAROID_FRAME,
		VALVE,
		CHARACTER_HAIR,
		SOAP,
		TOWEL,
		LANTERN,
		CANDLE,
		REINS,
		BAG,
		STRAWBERRY,
		BLUEBERRY,
		PEACH,
		CANTALOUPE,
		EGG,
		EGG_SHELL,
		CHICKEN,
		PESTLE,
		MORTAR,
		WHEAT_SPIKE,
		LID,
		FLOUR_JAR,
		MIXING_BOWL,
		POT,
		MIXING_SPOON,
		KNIFE,
		PASTRY,
		MILK_CANISTER,
		POKER_CARD,
		UNUSED,
		FIREWORK_ROCKET
	}
	public enum SpawnSleepMode{
		NONE,
		SLEEP,
		SLEEP_KINEMATIC
	}
	public enum PickupReasons{
		NONE=0,
		BEING_PRESENTED=1,
		HIGHLY_DESIRABLE=2,
	}
	
	public enum Attributes{
		NONE=0,
		DISABLE_PICKUP=4,
		DO_NOT_LOOK_AT=8,
	}

	[SerializeField]
	private ItemSettings m_settings;
	public ItemSettings settings { get{ return m_settings; } }
	[SerializeField]
	private Rigidbody m_rigidBody;
	public Rigidbody rigidBody { get{ return m_rigidBody; } }
	public int attributes { get{ return m_attributes; } }
	[SerializeField]
	private List<Character> m_owners = new List<Character>();
	public Character mainOwner { get{ return m_owners.Count==0? null : m_owners[0]; } }
	[SerializeField]
	protected SpawnSleepMode spawnSleepMode = SpawnSleepMode.SLEEP;
	[SerializeField]
	private Vector3 m_indicatorOffset;
	public Vector3 indicatorOffset { get{ return m_indicatorOffset; } }
	[SerializeField]
	protected StatusBar statusBar;
	[SerializeField]
	private Collider[] m_colliders = new Collider[0];
	public Collider[] colliders { get{ return m_colliders; } }

	private int m_pickupReasons = 0;
	public int pickupReasons { get{ return m_pickupReasons; } }
	private int m_attributes = 0;
	private List<OccupyState> m_occupyStates = new List<OccupyState>();
	public OccupyState mainOccupyState { get{ return m_occupyStates.Count==0? null : m_occupyStates[0]; } }
	public int occupyStateCount { get{ return m_occupyStates.Count; } }
	private bool m_isBeingDestroyed = false;
	public bool isBeingDestroyed { get{ return m_isBeingDestroyed; } }
	protected bool awakeWithoutRigidBody = false;
	public bool partOfHeldItemHierarchy = false;
	public OccupyStateCallback onMainOccupyStateChanged;
	public Attachment attachment { get; private set; }
	public bool isAttached { get{ return attachment != null; } }
	private List<Item> attachments = new List<Item>();
	private int originalLayer;



	public void SetItemLayer( int layer ){
		gameObject.layer = layer;
		foreach( var attachment in attachments ){
			attachment.SetItemLayer( layer );
		}
	}

	public void RestoreItemLayer(){
		SetItemLayer( originalLayer );
	}
																					
	public static T AddAndAwakeItemComponent<T>( GameObject target, ItemSettings newSettings, Character newOwner ) where T: Component{
		bool oldState = target.activeSelf;
		target.SetActive( false );
		T obj = target.AddComponent<T>();

		Item item = obj as Item;
		item.SetSettings( newSettings );
		item.m_owners.Add( newOwner );
		item.m_rigidBody = target.GetComponent<Rigidbody>();
		
        target.SetActive( oldState );	//call Awake if enabled
        return obj;
	}

	protected virtual void OnDrawGizmosSelected(){
		Gizmos.color = new Color( 1.0f, 0.5f, 0.0f, 1.0f );
		Vector3 indicatorPos = transform.TransformPoint( indicatorOffset );
		Gizmos.DrawWireSphere( indicatorPos, 0.01f );
		if( settings != null && settings.buoyancySphereRadius != 0.0f ){
			if( rigidBody ){
				Gizmos.color = new Color( 0.0f, 0.0f, 1.0f, 0.5f );
				Gizmos.DrawWireSphere( rigidBody.worldCenterOfMass, settings.buoyancySphereRadius );
			}
		}
	}

    protected override sealed void OnAwake(){
		
		if( settings == null ){
			Debug.LogError("ERROR! Item has no settings! "+name);
			return;
		}
		m_pickupReasons = settings.pickupReasons;
		this.enabled = true;
		if( settings.permanentlyEnabled ){
			EnableItemLogic();
		}
		OnItemAwake();
	
		if( rigidBody && spawnSleepMode > SpawnSleepMode.NONE ){
			rigidBody.Sleep();
			if( spawnSleepMode == SpawnSleepMode.SLEEP_KINEMATIC ){
				// rigidBody.isKinematic = true;
			}
			rigidBody.centerOfMass = settings.centerMass;
			rigidBody.inertiaTensor = settings.inertia;
		}
		originalLayer = gameObject.layer;
	}
	public override sealed void FixedUpdate(){
	}
	public override sealed void Update(){
	}
	public override sealed void LateUpdate(){
	}
    public override sealed void OnEnable(){
		OnItemEnable();
    }
    public override sealed void OnDisable(){
		OnItemDisable();
	}
	public override sealed void OnDestroy(){
		m_isBeingDestroyed = true;
		GameDirector.items.Remove(this);
		OnItemDestroy();
		if( mainOccupyState != null ){
			mainOccupyState.AttemptDrop();
		}
	}
	public void SetEnableStatusBar( bool enable ){
		//keep on if it is being picked up
		if( !enable && mainOccupyState != null ){
			return;
		}
        if( statusBar != null ){
		    statusBar.gameObject.SetActive( enable );
			if( enable ){
				statusBar.FaceCamera();
            	OnUpdateStatusBar();
			}
        }
	}
	protected virtual void OnUnregisterItemLogic(){
	}
	protected virtual void OnUpdateStatusBar(){
	}
	public virtual bool CanBePlacedInRestParent(){
		return true;
	}
	public void SetSettings( ItemSettings newSettings ){
		m_settings = newSettings;
	}
	protected virtual void OnItemAwake(){
	}
	public virtual Player.Animation GetPreferredPlayerHeldAnimation( PlayerHandState playerHandState ){
		return settings.playerHeldAnimation;
	}
	public virtual Loli.HoldFormAnimation GetPreferredLoliHeldAnimation( LoliHandState loliHandState ){
		return settings.loliHeldAnimation;
	}
	public virtual void OnItemFixedUpdate(){
		
		if( settings.itemType == Item.Type.LID ){
			Debug.DrawLine( transform.position, transform.position+Vector3.up, Color.green, 0.05f );
		}
	}
	public virtual void OnItemLateUpdate(){
	}
	public virtual void OnItemLateUpdatePostIK(){
        if( statusBar != null ){
		    statusBar.FaceCamera();
        }
	}
	public virtual bool OnPrePickupInterrupt( HandState handState ){
		return false;
	}
	public virtual void OnPostPickup(){
	}
	public virtual void OnPreDrop(){
	}
	public virtual void OnPostDrop(){
	}
	protected virtual void OnItemEnable(){
	}
	protected virtual void OnItemDisable(){
	}
	public virtual bool CanBePickedUp( OccupyState occupyState ){
		return !HasAttribute( Attributes.DISABLE_PICKUP ) && gameObject.activeInHierarchy;
	}
	protected virtual void OnItemDestroy(){
	}

	public virtual bool ShouldPickupWithRightHand( Character source ){
		return UnityEngine.Random.value > 0.5f;
	}

	public bool IsPickedUp(){
		return mainOccupyState!=null;
	}

	public void AddOccupyState( OccupyState newOccupyState ){
		if( newOccupyState == null ){
			Debug.LogError("[Item] Cannot link with a null OccupyState");
			return;
		}
		if( m_occupyStates.Contains( newOccupyState ) ){
			return;
		}
		if( m_occupyStates.Count == 0 ){
			EnableItemLogic();
		}
		m_occupyStates.Add( newOccupyState );
		AddOwner( newOccupyState.owner );
	}

	public void RemoveOccupyState( OccupyState occupyState ){
		if( !m_occupyStates.Contains( occupyState ) ){
			Debug.LogError("[Item] Does not contain occupyState to remove!");
			return;
		}
		RemoveOwner( occupyState.owner );
		m_occupyStates.Remove( occupyState );
		if( m_occupyStates.Count == 0 ){
			DisableItemLogic();
		}
	}

	private void AddOwner( Character newOwner ){
		if( settings.allowChangeOwner ){
			m_owners.Add( newOwner );
		}
	}
	
	private void RemoveOwner( Character newOwner ){
		if( settings.allowChangeOwner ){
			m_owners.Remove( newOwner );
		}
	}

	public void AttachTo( Transform transformParent, Item item ){
		if( m_isBeingDestroyed || attachment != null || m_rigidBody == null || transformParent == null ){
			Debug.LogError("[Item] Could not attach to!");
			return;
		}
		transform.SetParent( transformParent, true );
		attachment = new Attachment( m_rigidBody, transformParent, item );
		Destroy( m_rigidBody );
		m_rigidBody = null;
		SetAttribute( Item.Attributes.DISABLE_PICKUP );
		
		if( item ){
			item.attachments.Add( this );
			SetItemLayer( item.gameObject.layer );
		}
	}
	public void Detach(){
		if( m_isBeingDestroyed || attachment == null || m_rigidBody != null ){
			Debug.LogError("[Item] Could not detach! "+attachment+" "+m_rigidBody);
			return;
		}
		m_rigidBody = gameObject.GetComponent<Rigidbody>();
		if( m_rigidBody == null ){
			m_rigidBody = gameObject.AddComponent<Rigidbody>();
		}
		m_rigidBody.mass = attachment.oldMass;
		m_rigidBody.drag = attachment.oldDrag;
		m_rigidBody.angularDrag = attachment.oldDrag;
		m_rigidBody.centerOfMass = settings.centerMass;
		m_rigidBody.inertiaTensor = settings.inertia;
		
		if( attachment.parentItem ){
			attachment.parentItem.attachments.Remove( this );
			RestoreItemLayer();
		}

		ClearPickupReason( Item.PickupReasons.BEING_PRESENTED );
		ClearAttribute( Item.Attributes.DISABLE_PICKUP );
		transform.SetParent( null, true );
		attachment = null;
	}
	public bool HasPickupReason( Item.PickupReasons testAttrib ){
		return ((int)testAttrib&(int)pickupReasons)==(int)testAttrib;
	}
	public void SetPickupReason( Item.PickupReasons newReason ){
		m_pickupReasons |= (int)newReason;
	}
	public void ClearPickupReason( Item.PickupReasons reason ){
		m_pickupReasons &= ~(int)reason;
	}
	public bool HasAttribute( Attributes testAttrib ){
		return ((int)testAttrib&(int)attributes)==(int)testAttrib;
	}
	public bool hasAnyAttributes( int testAttribs ){
		return ( (int)attributes&testAttribs ) != 0;
	}
	public void SetAttribute( Attributes newAttrib ){
		m_attributes |= (int)newAttrib;
	}
	public void ClearAttribute( Attributes attrib ){
		m_attributes &= ~(int)attrib;
	}
}

}