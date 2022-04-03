using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using viva;


public class PickupTest: VivaScript{

	private readonly Character character;
	private VivaInstance[] oldListenInstances;
	private VivaInstance cached;
	private float lastGiddyTime;


	public PickupTest( Character _character ){
		character = _character;
		
		character.biped.vision.onItemSeen.AddListener( this, OnNewItemSeen );
		character.biped.vision.onCharacterSeen.AddListener( this, OnNewCharacterSeen );
		character.biped.lookTarget.onChanged.AddListener( this, OnLookTargetChange );
		character.onGesture.AddListener( this, ListenForGesture );
    }

    private void ListenForGesture( string gesture, Character source ){
        if( gesture == "stop" ){
			StopPickupTask( "pickup beingOffered right" );
			StopPickupTask( "pickup beingOffered left" );
        }else if( gesture == "take" ){
			if( source.isBiped ){
				var characters = source.biped.rightHandGrabber.IsGrabbingCharacters();
				foreach( var newCharacter in characters ){
					OnNewCharacterSeen( newCharacter );
				}
				characters = source.biped.leftHandGrabber.IsGrabbingCharacters();
				foreach( var newCharacter in characters ){
					OnNewCharacterSeen( newCharacter );
				}
			}
		}
	}

	private void StopPickupTask( string offerTaskName ){
		var oldTask = character.autonomy.FindTask( offerTaskName ) as Pickup;
		if( oldTask != null ){
			oldTask.Fail("Stopped task");
			character.autonomy.RemoveTask( oldTask );
		}
	}

	private void OnNewItemSeen( Item item ){
		if( !PickupGrabbable( item.GetRandomGrabbable() ) ){
			//else just listen to the item
			SetListenForAttributeChanged( new Item[]{ item } );
		}
	}

	private void OnNewCharacterSeen( Character newCharacter ){
		if( newCharacter.biped ){
			var context = newCharacter.biped.rightHandGrabber.IsGrabbing( Item.offerAttribute );
			if( context && PickupGrabbable( context.grabbable ) ){
				return;
			}
			context = newCharacter.biped.leftHandGrabber.IsGrabbing( Item.offerAttribute );
			if( context && PickupGrabbable( context.grabbable ) ){
				return;
			}

			var heldItems = new List<Item>();
			heldItems.AddRange( newCharacter.biped.rightHandGrabber.GetAllItems() );
			heldItems.AddRange( newCharacter.biped.leftHandGrabber.GetAllItems() );
			SetListenForAttributeChanged( heldItems.ToArray() );
		}else{
			SetListenForAttributeChanged( new Character[]{ newCharacter } );
		}
	}

	private void OnLookTargetChange(){
		var rigidBody = character.biped.lookTarget.target as Rigidbody;

		//if failed to pick up, listen for the lookCharacter's held items
		var lookCharacter = Util.GetCharacter( rigidBody );
		if( lookCharacter ){
			OnNewCharacterSeen( lookCharacter );
		}else{
			var lookItem = Util.GetItem( rigidBody );
			if( lookItem ){
				OnNewItemSeen( lookItem );
			}
		}
	}

	private void SetListenForAttributeChanged( VivaInstance[] instances ){
		if( oldListenInstances != null ){
			foreach( var oldInstance in oldListenInstances ){
				var oldItem = oldInstance as Item;
				if( oldItem ){
					oldItem.onAttributeChanged.RemoveListener( this, OnItemAttributeAdded );
				}else{
					var oldCharacter = oldInstance as Character;
					if( oldCharacter ){
						oldCharacter.onAttributeChanged.RemoveListener( this, OnAnimalAttributeAdded );
					}
				}
			}
			oldListenInstances = null;
		}
		oldListenInstances = instances;
		if( instances != null ){
			foreach( var newInstance in instances ){
				var newItem = newInstance as Item;
				if( newItem ){
					newItem.onAttributeChanged.AddListener( this, OnItemAttributeAdded );
				}else{
					var newCharacter = newInstance as Character;
					if( newCharacter ){
						newCharacter.onAttributeChanged.AddListener( this, OnAnimalAttributeAdded );
					}
				}
			}
		}
	}

	private void OnItemAttributeAdded( Item item, Attribute attribute ){
		if( attribute.name == Item.offerAttribute ){
			PickupGrabbable( item.GetRandomGrabbable() );
		}
	}

	private void OnAnimalAttributeAdded( Character character, Attribute attribute ){
		if( attribute.name == Item.offerAttribute ){
			PickupGrabbable( character.animal.GetRandomGrabbable() );
		}
	}

	private bool PickupGrabbable( Grabbable grabbable ){
		if( !grabbable ) return false;
		VivaInstance sourceInstance = grabbable.parent;
		if( !sourceInstance ) return false;
		if( !sourceInstance.HasAttribute(Item.offerAttribute) ) return false;
		if( cached != null && sourceInstance == cached ) return false;
		if( !character.autonomy.HasTag( "idle" ) ) return false;

		//hands must be free
		if( character.biped.rightHandGrabber.grabbing && character.biped.leftHandGrabber.grabbing ){
			return false;
		}
		//dont pick up if already grabbing it
		if( character.IsGrabbing( sourceInstance ) ) return false;

		var pickup = new Pickup( character.autonomy, grabbable );
		// pickup.tags.Add("idle");
		var offerTaskName = "pickup beingOffered "+pickup.grabber?.signName;

		var oldTask = character.autonomy.FindTask( offerTaskName ) as Pickup;
		if( oldTask != null ){
			if( CalculateInterest( oldTask.grabbable ) >= CalculateInterest( grabbable ) ){
				return false;
			}
			oldTask.Fail("Saw more interesting item to pickup");
			character.autonomy.RemoveTask( oldTask );
		}

		SetupAnimations( character );
		SetListenForAttributeChanged( null );

		//create the autonomy task
		cached = sourceInstance;
		//if item is picked up
		pickup.onSuccess += delegate{
			sourceInstance?.RemoveAttribute( Item.offerAttribute );
		};
		pickup.onAutonomyExit += delegate{
			cached = null;
		};

		var item = sourceInstance as Item;
		pickup.moveTo.onRegistered += delegate{
			character.biped.lookTarget.SetTargetRigidBody( item?.rigidBody );
		};
		
		var ensureItemExists = new Condition( character.autonomy, delegate{
			return item && ( item.IsBeingGrabbedByCharacter( character ) || sourceInstance.HasAttribute( Item.offerAttribute ) );
		} );
		pickup.AddRequirement( ensureItemExists );

		if( Random.value > 0.3f && Time.time-lastGiddyTime > 10f && character.mainAnimationLayer.currentBodySet["giddy"] != null ){
			lastGiddyTime = Time.time;
			var playGiddyAnimation = new PlayAnimation( character.autonomy, null, "giddy", true, 0, false );
			playGiddyAnimation.name = "giddy before pickup";
			playGiddyAnimation.onEnterAnimation += delegate{
				character.biped.lookTarget.SetTargetRigidBody( item?.rigidBody );
			};
			playGiddyAnimation.onExitAnimation += playGiddyAnimation.Succeed;
			
			pickup.AddRequirement( playGiddyAnimation );

			var faceItem = new FaceTargetBody( character.autonomy, 1, 30, 0.3f );
			faceItem.target.SetTargetRigidBody( item?.rigidBody );
			playGiddyAnimation.AddPassive( faceItem );
		}
		
		pickup.Start( this, offerTaskName );
		return true;
	}

	private float CalculateInterest( Grabbable grabbable ){
		if( !grabbable ) return 0;
		int interest = 0;
		if( grabbable.parentItem ){
			foreach( var itemGrabbable in grabbable.parentItem.grabbables ){
				if( itemGrabbable.isBeingGrabbed ) interest++;
			}
		}
		return interest;
	}

	private void SetupAnimations( Character character ){
		var stand = character.animationSet.GetBodySet("stand");
		var standGiddy = stand.Single( "giddy", "stand_giddy_surprise", false );
		standGiddy.AddEvent( Event.Voice(0,"impressed") );
		standGiddy.nextState = stand["idle"];
	}

	private bool AmIGrabbing( Item item ){
		return character.biped.rightHandGrabber.IsGrabbing( item ) || character.biped.leftHandGrabber.IsGrabbing( item );
	}
}  