using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace viva{

public class Vision : MonoBehaviour{

    public const int characterMemoryMax = 4;
    public const int itemMemoryMax = 32;
    public const int grabbableMemoryMax = 32;

    [SerializeField]
    private List<Zone> zones = new List<Zone>();
    [SerializeField]
    private Item[] items = new Item[itemMemoryMax]; //held items should never leave this array
    [SerializeField]
    private Grabbable[] grabbables = new Grabbable[grabbableMemoryMax];
    private int[] itemColliderCounter = new int[itemMemoryMax];
    public ListenerItem onItemSeen { get; private set; }
    public ListenerGrabbable onGrabbableSeen { get; private set; }
    public ListenerCharacter onCharacterSeen { get; private set; }
    [SerializeField]
    private Character[] charactersVisible = new Character[ characterMemoryMax ];
    [SerializeField]
    private List<Grabbable> checkGrabbableSight = new List<Grabbable>();
    private List<Zone> checkZoneSight = new List<Zone>();
    private int checkIndexCounter = 0;
    private Character self;


    public static Vision CreateVision( GameObject target ){
        if( target.TryGetComponent<Vision>( out Vision vision ) ){
            return vision;
        }
        vision = target.AddComponent<Vision>();
        
        vision.onItemSeen = new ListenerItem( vision.items, "onItemSeen" );
        vision.onGrabbableSeen = new ListenerGrabbable( vision.grabbables, "onGrabbableSeen" );
        vision.onCharacterSeen = new ListenerCharacter( vision.charactersVisible, "onCharacterSeen" );
        return vision;
    }

    public void _InternalSetup( Character _self ){
        self = _self;
    }

    public void _InternalReset(){
        onItemSeen._InternalReset();
        onGrabbableSeen._InternalReset();
    }

    public Grabbable GetGrabbableAtMemorySlot( int index ){
        return grabbables[ index ];
    }

    public List<Item> FindItemsByAttributes( AttributeRequest attributeRequest ){
        var result = new List<Item>();
        for( int i=0; i<items.Length; i++ ){
            var item = items[i];
            if( item == null ) continue;
            if( item.HasAttributes( attributeRequest ) ) result.Add( item );
        }
        return result;
    }
    
    //Also searches grabbed objects because they are part of vision
    public List<Attribute> FindClosestMissingAttributes( AttributeRequest attributeRequest ){
        var leastMissing = 1000000;
        List<Attribute> leastMissingAttributes = null;
        for( int i=0; i<items.Length; i++ ){
            var item = items[i];
            if( item == null ) continue;
            var missingAttributes = item.FindMissingAttributes( attributeRequest );
            bool newLowest = false;
            if( missingAttributes.Count < leastMissing ){
                newLowest = true;
            }else if( missingAttributes.Count == leastMissing ){
                foreach( var leastMissingAttribute in leastMissingAttributes ){
                    Attribute match = null;
                    foreach( var missingAttribute in missingAttributes ){
                        if( leastMissingAttribute.name == missingAttribute.name ){
                            match = missingAttribute;
                            break;
                        }
                    }
                    if( match == null ) break;
                    if( leastMissingAttribute.Matches( match, CountMatch.GREATER ) ){
                        newLowest = true;
                        break;
                    }
                }
            }
            if( newLowest ){
                leastMissing = missingAttributes.Count;
                leastMissingAttributes = missingAttributes;
            }
        }
        return leastMissingAttributes;
    }

    public bool SeesItemWithAttribute( string attribute ){
        if( attribute == null ) return false;
        for( int i=0; i<items.Length; i++ ){
            var item = items[i];
            if( item == null ) continue;
            if( item.HasAttribute( attribute ) ) return true;
        }
        return false;
    }
    
    public bool SeesItemWithAttributes( AttributeRequest attributeRequest ){
        for( int i=0; i<items.Length; i++ ){
            var item = items[i];
            if( item == null ) continue;
            if( item.HasAttributes( attributeRequest ) ) return true;
        }
        return false;
    }

    private int FindItemIndex( Item item ){
        for( int i=0; i<items.Length; i++ ){
            if( items[i] == item ) return i;
        }
        return -1;
    }
    
    private int FindGrabbableIndex( Grabbable grabbable ){
        for( int i=0; i<grabbables.Length; i++ ){
            if( grabbables[i] == grabbable ) return i;
        }
        return -1;
    }

    public void OnTriggerEnter( Collider collider ){
        var grabbable = collider.GetComponent<Grabbable>();
        if( grabbable ){
            checkGrabbableSight.Add( grabbable );
        }else if( collider.gameObject.layer == WorldUtil.zonesLayer ){
            var zone = collider.GetComponent<Zone>();
            if( zone ) checkZoneSight.Add( zone );
        }
    }
    
    public void OnTriggerExit( Collider collider ){
        var grabbable = collider.GetComponent<Grabbable>();
        if( grabbable ) checkGrabbableSight.Remove( grabbable );
    }
    
    private void UnregisterItem( Item item ){
        if( item == null ) return;

        int itemIndex = FindItemIndex( item );
        if( itemIndex > -1 ) itemColliderCounter[ itemIndex ]--;
    }

     public Character GetCurrentCharacter(){
        if( !self.isBiped ) return null;
        return self.biped.lookTarget.target as Character;
     }

    public Item GetMostRelevantItem(){
        if( !self.isBiped ) return null;
        if( self.biped.lookTarget.type != Target.TargetType.RIGIDBODY ) return null;
        var lookBody = self.biped.lookTarget.target as Rigidbody;
        var item = Util.GetItem( lookBody );
        if( item ) return item;

        var character = Util.GetCharacter( lookBody );
        Grabber returnFromGrabber = null;
        if( character && character.isBiped ){
            if( lookBody == character.biped.rightHand.rigidBody || 
                lookBody == character.biped.rightArm.rigidBody || 
                lookBody == character.biped.rightUpperArm.rigidBody ){
                returnFromGrabber = character.biped.rightHandGrabber;
            }else if( lookBody == character.biped.leftHand.rigidBody || 
                lookBody == character.biped.leftArm.rigidBody || 
                lookBody == character.biped.leftUpperArm.rigidBody){
                returnFromGrabber = character.biped.leftHandGrabber;
            }
            if( !returnFromGrabber ){
                switch( Random.Range(0,2) ){
                case 0:
                    returnFromGrabber = character.biped.leftHandGrabber;
                    break;
                default:
                    returnFromGrabber = character.biped.rightHandGrabber;
                    break;
                }
            }
        }
        if( returnFromGrabber ){
            if( returnFromGrabber.contextCount == 0 && ( character && character.biped ) ){
                returnFromGrabber = returnFromGrabber==character.biped.rightHandGrabber ? character.biped.leftHandGrabber : character.biped.rightHandGrabber;
            }
            for( int i=0; i<returnFromGrabber.contextCount; i++ ){
                var context = returnFromGrabber.GetGrabContext(i);
                if( context.grabbable.parentItem ) return context.grabbable.parentItem;
            }
        }
        return null;
    }

    public void See( Item item ){
        if( item == null ) return;
        RegisterItem( item );
    }

    private void FixedUpdate(){
        CheckGrabbableSight();
        CheckZoneSight();
    }

    private void CheckGrabbableSight(){
        if( checkGrabbableSight.Count == 0 ) return;
        int testIndex = ++checkIndexCounter%checkGrabbableSight.Count;
        var grabbable = checkGrabbableSight[ testIndex ];
        if( grabbable == null || grabbable.parentCharacter == self ){
            checkGrabbableSight.RemoveAt( testIndex );
        }else{
            var testPos = grabbable.rigidBody ? grabbable.rigidBody.worldCenterOfMass : grabbable.transform.position;
            var delta = testPos-transform.position;
            var sqDist = Vector3.SqrMagnitude( delta );
            if( sqDist <= 0.0001f ) return;
            sqDist = Mathf.Sqrt( sqDist );

            //must be visible
            bool sightClear;
            if( Physics.Raycast( transform.position, delta/sqDist, out RaycastHit hitInfo, sqDist, WorldUtil.defaultMask|WorldUtil.itemsStaticMask, QueryTriggerInteraction.Ignore ) ){
                var parent = hitInfo.collider.GetComponentInParent<VivaInstance>();
                sightClear = parent == grabbable.parent;
            }else{
                sightClear = true;
            }
            if( sightClear ){
                RegisterGrabbable( grabbable );
                checkGrabbableSight.RemoveAt( testIndex );
                Debug.DrawLine( transform.position, testPos, Color.green, 2.0f );
            }else{
                Debug.DrawLine( transform.position, testPos, Color.red, 0.3f );
            }
        }
    }

    private void CheckZoneSight(){
        if( checkZoneSight.Count == 0 ) return;
        var zone = checkZoneSight[0];
        if( zone == null ){
            checkZoneSight.RemoveAt(0);
        }else{
            var testPos = zone.transform.TransformPoint( zone.boxCollider.center );
            var delta = testPos-transform.position;
            var sqDist = Vector3.SqrMagnitude( delta );
            if( sqDist <= 0.0001f ) return;
            sqDist = Mathf.Sqrt( sqDist );

            //must be visible
            if( Physics.Raycast( transform.position, delta/sqDist, sqDist, WorldUtil.defaultMask|WorldUtil.itemsStaticMask, QueryTriggerInteraction.Ignore ) ){
                Debug.DrawLine( transform.position, testPos, Color.red, 0.3f );
            }else{
                if( zone.parentItem ) RegisterItem( zone.parentItem );
                checkZoneSight.RemoveAt(0);
                Debug.DrawLine( transform.position, testPos, Color.green, 0.3f );
            }
        }
    }

    private void RegisterItem( Item item ){
        int itemIndex = FindItemIndex( item );
        if( itemIndex == -1 ){  //not seen yet
            itemIndex = FindItemIndex( null );
            if( itemIndex == -1 ){
                itemIndex = FindNextBestItemSlot();
            }

            items[ itemIndex ] = item;
            itemColliderCounter[ itemIndex ] = 1;
            return;
        }
        itemColliderCounter[ itemIndex ]++;
        Script.HandleScriptCall( onItemSeen, item );
    }

    private void RegisterGrabbable( Grabbable grabbable ){
        if( grabbable == null ) return;
        //on grabbable seen
        int grabbableIndex = FindGrabbableIndex( grabbable );
        if( grabbableIndex == -1 ){
            grabbableIndex = FindNextBestGrabbableSlot();
        }
        if( grabbable.parentCharacter == self ) return;
        
        grabbables[ grabbableIndex ] = grabbable;
        onGrabbableSeen.Invoke( grabbable );
        
        //on Item seen
        var item = grabbable.parentItem;
        if( item != null ){
            RegisterItem( item );
        }else{
            //on Character seen
            var character = grabbable.parentCharacter;
            if( character ){
                var characterIndex = System.Array.IndexOf( charactersVisible, character );
                if( characterIndex == -1 ){
                    for( int i=0; i<charactersVisible.Length; i++ ){
                        characterIndex = System.Array.IndexOf( charactersVisible, null );
                        if( characterIndex == -1 ) characterIndex = Random.Range( 0, charactersVisible.Length );

                        charactersVisible[ characterIndex ] = character;
                    }
                }
                Script.HandleScriptCall( onCharacterSeen, character );
            }
        }
    }

    public bool IsPointVisible( Vector3 point, VivaInstance ignore=null ){
        var delta = point-transform.position;
        if( Physics.Raycast( transform.position, delta, out WorldUtil.hitInfo, delta.magnitude, WorldUtil.defaultMask|WorldUtil.itemsMask|WorldUtil.itemsStaticMask, QueryTriggerInteraction.Ignore ) ){
            var instance = WorldUtil.hitInfo.collider.GetComponentInParent<VivaInstance>();
            if( !instance ) return false;
            return instance == ignore;
        }
        return true;
    }

    private int FindNextBestGrabbableSlot(){
        for( int i=0; i<grabbables.Length; i++ ){
            var grabbable = grabbables[i];
            if( grabbable == null ) return i;
        }
        return Random.Range( 0, grabbables.Length );
    }

    private int FindNextBestItemSlot(){ //assumes no empty slots are available
        int leastSignificantIndex = 0;
        float smallest = Mathf.Infinity;
        for( int i=0; i<items.Length; i++ ){
            var item = items[i];
            if( item == null ) return i;
            if( item.IsBeingGrabbedByCharacter( self ) ) continue;    //do not allow index if that item is being grabbed

            if( item.model.boundsRadius < smallest ){
                leastSignificantIndex = i;
                smallest = item.model.boundsRadius;
            }
        }
        return leastSignificantIndex;
    }
}

}