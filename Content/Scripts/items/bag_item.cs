using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using viva;


public class BagItem: VivaScript{

    public class BagEntry{
        public string itemName;
        public int count=0;
    }

    private Item bag;
    private ItemUserListener itemUser;
    private readonly string wearVariable = "wearSide";
    private List<BagEntry> inventory = new List<BagEntry>();
    private BagEntry nextTakeOut;


    public BagItem( Item _item ){
        bag = _item;

        itemUser = new ItemUserListener( bag, this, BindCharacter, UnbindCharacter );
    }

    private void PlayerActionRightHand(){
        if( itemUser.character.possessor.isUsingKeyboard ){
            PlayerWearBagOnLeft();
        }else{
            
        }
    }

    private void PlayerActionLeftHand(){
        if( itemUser.character.possessor.isUsingKeyboard ){
            PlayerWearBagOnRight();
        }else{
            
        }
    }

    private void BindForSpine( Character character ){
        var wearSide = (bool?)bag.customVariables.Get( this, wearVariable ).value as bool?;
        if( wearSide.HasValue && wearSide.Value ){
            character.GetInput( Input.RightAction ).onUp.AddListener( this, PlayerCheckRemoveBagRight );
            character.GetInput( Input.LeftAction ).onUp.AddListener( this, PlayerPutInBagRight );
        }else{
            character.GetInput( Input.LeftAction ).onUp.AddListener( this, PlayerCheckRemoveBagLeft );
            character.GetInput( Input.RightAction ).onUp.AddListener( this, PlayerPutInBagLeft );
        }
        character.GetInput( Input.I ).onUp.AddListener( this, OpenInventory );
    }

    private void UnbindForSpine( Character character ){
        var wearSide = (bool?)bag.customVariables.Get( this, wearVariable ).value;
        if( wearSide.HasValue && wearSide.Value ){
            character.GetInput( Input.RightAction ).onUp.RemoveListener( this, PlayerCheckRemoveBagRight );
            character.GetInput( Input.LeftAction ).onUp.RemoveListener( this, PlayerPutInBagRight );
        }else{
            character.GetInput( Input.LeftAction ).onUp.RemoveListener( this, PlayerCheckRemoveBagLeft );
            character.GetInput( Input.RightAction ).onUp.RemoveListener( this, PlayerPutInBagLeft );
        }
        character.GetInput( Input.I ).onUp.RemoveListener( this, OpenInventory );
    }

    private void BindCharacter( Character character, GrabContext context ){
        SetupAnimations( character );
        
        if( character.isPossessed ){
            if( context.grabber == character.biped.rightHandGrabber ){
                character.GetInput( Input.RightAction ).onDown.AddListener( this, PlayerActionRightHand );
            }else if( context.grabber == character.biped.leftHandGrabber ){
                character.GetInput( Input.LeftAction ).onDown.AddListener( this, PlayerActionLeftHand );
            }else if( context.grabber == character.GetGrabber( RagdollMuscle.UPPER_SPINE ) ){
                BindForSpine( character );
            }
        }else{
            if( context.grabber == character.biped.rightHandGrabber ){
                if( character.autonomy.FindTask("remove bag left") == null ) PlayerWearBagOnLeft();
            }else if( context.grabber == character.biped.leftHandGrabber ){
                if( character.autonomy.FindTask("remove bag right") == null ) PlayerWearBagOnRight();
            }else if( context.grabber == character.GetGrabber( RagdollMuscle.UPPER_SPINE ) ){
                character.biped.rightHandGrabber.onGrabbed.AddListener( this, NPCPutInBagLeft );
                character.biped.leftHandGrabber.onGrabbed.AddListener( this, NPCPutInBagRight );
                character.onGesture.AddListener( this, ListenForGiveGesture );
            }
        }
    }

    private void UnbindCharacter( Character character, GrabContext context ){
        if( character.isPossessed ){
            if( context.grabber == character.biped.rightHandGrabber ){
                character.GetInput( Input.RightAction ).onDown.RemoveListener( this, PlayerActionRightHand );
            }else if( context.grabber == character.biped.leftHandGrabber ){
                character.GetInput( Input.LeftAction ).onDown.RemoveListener( this, PlayerActionLeftHand );
            }else if( context.grabber == character.GetGrabber( RagdollMuscle.UPPER_SPINE ) ){
                UnbindForSpine( character );
            }
        }else{
            if( context.grabber == character.GetGrabber( RagdollMuscle.UPPER_SPINE ) ){
                character.biped.rightHandGrabber.onGrabbed.RemoveListener( this, NPCPutInBagLeft );
                character.biped.leftHandGrabber.onGrabbed.RemoveListener( this, NPCPutInBagRight );
                character.onGesture.RemoveListener( this, ListenForGiveGesture );
            }
        }
        bag.customVariables.Get( this, wearVariable ).value = null;
    }

    public void ListenForGiveGesture( string gestureName, Character caller ){
        //remove bag if not busy
        if( gestureName == "give" ){
            if( itemUser.character && itemUser.character.autonomy.HasTag( "idle" ) ){
                RemoveBag();
            }
        }
    }

    public void RemoveBag(){
        if( IsWearingBag( true ) ){
            PlayBagAnim( "remove bag ", true, false );
        }else if( IsWearingBag( false ) ){
            PlayBagAnim( "remove bag ", false, false );
        }
    }

    private void NPCPutInBagRight( GrabContext context ){
        if( !IsWearingBag( true ) ) return;
        PlayBagAnim( "put in bag ", true, false );
    }

    private void NPCPutInBagLeft( GrabContext context ){
        if( !IsWearingBag( false ) ) return;
        PlayBagAnim( "put in bag ", false, false );
    }

    public void OpenInventory(){
        var character = itemUser.character;
        if( !bag || !character ) return;
        bag.OpenDialogForCommand( "inventory", false, false, "GetInventoryDialogOptions" );
    }

    public void OnDialogOption( Character character, DialogOption option ){
        for( int i=0; i<inventory.Count; i++ ){
            var bagEntry = inventory[i];
            if( bagEntry.itemName == option.value ){
                nextTakeOut = bagEntry;
                
                bool? wearSide = (bool?)bag.customVariables.Get( this, wearVariable ).value;
                var side = (wearSide.HasValue&&wearSide.Value) ? "right" : "left";
                var takeFromBag = new PlayAnimation( itemUser.character.autonomy, "stand", "take from bag "+side );
                takeFromBag.Start( this, "take from bag "+side );
            }
        }
    }

    public DialogOption[] GetInventoryDialogOptions(){
        var options = new DialogOption[ inventory.Count ];
        for( int i=0; i<inventory.Count; i++ ){
            var bagEntry = inventory[i];
            options[i] = new DialogOption( bagEntry.itemName, DialogOptionType.ITEM );
        }
        return options;
    }

    private void TakeFromBag( object shoulderSideParam ){
        if( nextTakeOut == null || nextTakeOut.count == 0 ) return;
        if( --nextTakeOut.count == 0 ){
            inventory.Remove( nextTakeOut );
        }

        var newItem = Item.Spawn( nextTakeOut.itemName, bag.transform.position, Quaternion.identity );
        if( newItem && newItem.grabbableCount > 0 ){
            var grabbable = newItem.GetGrabbable(0);
            var user = itemUser.character;
            if( user ){
                bool shoulderSide = (bool)shoulderSideParam;
                if( shoulderSide ){
                    if( !user.biped.rightHandGrabber.grabbing ) user.biped.rightHandGrabber.Grab( grabbable, false );
                }else{
                    if( !user.biped.leftHandGrabber.grabbing ) user.biped.leftHandGrabber.Grab( grabbable, false );

                }
            }
        }
        nextTakeOut = null;
    }

    private bool IsPlayerShiftUp(){
        if( itemUser.character == null ) return false;
        var up = itemUser.character.possessor?.movement.leftShiftDown;
        return up.HasValue ? up.Value : false;
    }

    private bool IsWearingBag( bool shoulderSide ){
        var character = itemUser.character;
        if( character == null ) return false;

        var shoulderGrabber = character.GetGrabber( RagdollMuscle.UPPER_SPINE );
        var items = shoulderGrabber.FindItems( "bag" );
        foreach( var item in items ){
            bool? wearSide = (bool?)item.customVariables.Get( this, wearVariable ).value;
            if( wearSide.HasValue && wearSide.Value == shoulderSide ){
                return true;
            }
        }
        return false;
    }

    private void PlayBagAnim( string prefix, bool shoulderSide, bool isPlayer ){
        var side = shoulderSide?"right":"left";
        var removeBag = new PlayAnimation( itemUser.character.autonomy, "stand", prefix+side );
        removeBag.Start( this, prefix+side );
    }

    private void PlayerCheckRemoveBagLeft(){
        if( !IsPlayerShiftUp() || !IsWearingBag( false ) ) return;
        if( itemUser.character.isPossessed && Time.time-itemUser.character.biped.leftHandGrabber.timeSinceLastRelease<0.5f ) return;
        PlayBagAnim( "remove bag ", false, true );
    }
    
    private void PlayerCheckRemoveBagRight(){
        if( !IsPlayerShiftUp() || !IsWearingBag( true ) ) return;
        if( itemUser.character.isPossessed && Time.time-itemUser.character.biped.rightHandGrabber.timeSinceLastRelease<0.5f ) return;
        PlayBagAnim( "remove bag ", true, true );
    }

    private void PlayerPutInBagRight(){
        if( IsPlayerShiftUp() || !IsWearingBag( true ) ) return;
        if( !CanItemsBePutInBag( itemUser.character.biped.leftHandGrabber.GetAllItems() ) ) return;
        PlayBagAnim( "put in bag ", true, true );
    }

    private void PlayerPutInBagLeft(){
        if( IsPlayerShiftUp() || !IsWearingBag( false ) ) return;
        if( !CanItemsBePutInBag( itemUser.character.biped.rightHandGrabber.GetAllItems() ) ) return;
        PlayBagAnim( "put in bag ", false, true );
    }

    private void PlayerWearBagOnLeft(){
        if( IsPlayerShiftUp() || IsWearingBag( false ) ) return;
        PlayBagAnim( "wear bag ", false, true );
    }

    private void PlayerWearBagOnRight(){
        if( IsPlayerShiftUp() || IsWearingBag( true ) ) return;
        PlayBagAnim( "wear bag ", true, true );
    }

    private bool CanItemsBePutInBag( List<Item> items ){
        if( items == null ) return false;
        if( items.Count == 0 ) return false;
        foreach( var item in items ){
            if( item == bag ) return false; //cant put self inside
            foreach( var grabbable in item.grabbables ){
                for( int i=0; i<grabbable.grabContextCount; i++ ){
                    GrabContext context = grabbable.GetGrabContext(i);
                    // item must wait a bit before being put into the bag immediately
                    if( Time.time-context.timeStarted < 0.5f && context.grabber.character == itemUser.character ){
                        return false;
                    }
                }
            }
        }
        return true;
    }

    private void ApplyWearBag( object shoulderSideParam ){
        var character = itemUser.character;
        if( !character || !bag ) return;
        bool shoulderSide = (bool)shoulderSideParam;

        var handGrabber = shoulderSide ? character.biped.leftHandGrabber : character.biped.rightHandGrabber;
        Viva.Destroy( handGrabber.IsGrabbing( bag ) );
        
        var shoulderGrabber = character.GetGrabber( RagdollMuscle.UPPER_SPINE );

        var firstGrabbable = bag.GetGrabbable(0);
        var shoulderGrabbableSettings = new GrabbableSettings( firstGrabbable.settings );
        shoulderGrabbableSettings.freelyRotate = false;
        firstGrabbable.OverrideSettingsForNextGrab( shoulderGrabbableSettings );

        bag.customVariables.Get( this, wearVariable ).value = (bool?)shoulderSide;
        var context = shoulderGrabber.Grab( firstGrabbable, true );
    
        context.SetTargetRotation( character.model.armature.rotation*Quaternion.Euler( 0, 0, (System.Convert.ToSingle(shoulderSide)*2-1)*7) );

        var upperSpine = character.biped.upperSpine.target;
        var shoulder = shoulderSide ? character.biped.rightUpperArm.target : character.biped.leftUpperArm.target;
        var bagPos = upperSpine.InverseTransformPoint( shoulder.position )*character.scale;
        bagPos += upperSpine.InverseTransformDirection( Vector3.up )*character.model.bipedProfile.armThickness;
        context.SetAnchor( -bagPos );
    }

    private void ApplyRemoveBag( object shoulderSideParam ){
        var character = itemUser.character;
        if( !character || !bag ) return;
        bool shoulderSide = (bool)shoulderSideParam;
        var shoulderGrabber = character.GetGrabber( RagdollMuscle.UPPER_SPINE );
        Viva.Destroy( shoulderGrabber.IsGrabbing( bag ) );
        
        var handGrabber = shoulderSide ? character.biped.leftHandGrabber : character.biped.rightHandGrabber;
        
        var context = handGrabber.Grab( bag.GetGrabbable(0), false );
        bag.customVariables.Get( this, wearVariable ).value = null;
    }

    private void ApplyPutInBag( object shoulderSideParam ){
        var character = itemUser.character;
        if( !character || !bag ) return;
        bool shoulderSide = (bool)shoulderSideParam;

        var otherHandGrabber = shoulderSide ? character.biped.leftHandGrabber : character.biped.rightHandGrabber;

        var items = otherHandGrabber.GetAllItems();
        if( !CanItemsBePutInBag( items ) ) return;

        foreach( var item in items ){
            var existing = FindExisting( item.name );
            if( existing == null ){
                existing = new BagEntry(){ itemName=item.name, count=0 };
                inventory.Add( existing );
            }
            existing.count++;

            Viva.Destroy( item );
        }
    }

    private BagEntry FindExisting( string itemName ){
        foreach( var bagEntry in inventory ){
            if( bagEntry.itemName == itemName ) return bagEntry;
        }
        return null;
    }

    private void SetupAnimations( Character character ){
        var stand = character.animationSet.GetBodySet("stand", character.mainAnimationLayerIndex );
        var wearBagRight = stand.Single( "wear bag right","stand_wear_bag_right", false, 0.8f );
        wearBagRight.nextState = stand["idle"];
        wearBagRight.AddEvent( Event.Function( 0.5f, this, ApplyWearBag, true ) );
        wearBagRight.curves[BipedRagdoll.leftArmID] = new Curve(0,0.1f);
        wearBagRight.curves[BipedRagdoll.rightArmID] = new Curve(0,0.1f);
        
        var wearBagLeft = stand.Single( "wear bag left", "stand_wear_bag_left", false, 0.8f );
        wearBagLeft.nextState = stand["idle"];
        wearBagLeft.AddEvent( Event.Function( 0.5f, this, ApplyWearBag, false ) );
        wearBagLeft.curves[BipedRagdoll.leftArmID] = new Curve(0,0.1f);
        wearBagLeft.curves[BipedRagdoll.rightArmID] = new Curve(0,0.1f);

        var removeBagRight = stand.Single( "remove bag right", "stand_remove_bag_right", false, 0.8f );
        removeBagRight.nextState = stand["idle"];
        removeBagRight.AddEvent( Event.Function( 0.5f, this, ApplyRemoveBag, true ) );
        removeBagRight.curves[BipedRagdoll.leftArmID] = new Curve(0,0.1f);
        removeBagRight.curves[BipedRagdoll.rightArmID] = new Curve(0,0.1f);
        
        var removeBagLeft = stand.Single( "remove bag left", "stand_remove_bag_left", false, 0.8f );
        removeBagLeft.nextState = stand["idle"];
        removeBagLeft.AddEvent( Event.Function( 0.5f, this, ApplyRemoveBag, false ) );
        removeBagLeft.curves[BipedRagdoll.leftArmID] = new Curve(0,0.1f);
        removeBagLeft.curves[BipedRagdoll.rightArmID] = new Curve(0,0.1f);

        var putInBagRight = stand.Single( "put in bag right", "stand_bag_put_in_right", false, 0.8f );
        putInBagRight.nextState = stand["idle"];
        putInBagRight.AddEvent( Event.Function( 0.5f, this, ApplyPutInBag, true ) );
        putInBagRight.curves[BipedRagdoll.leftArmID] = new Curve(0,0.1f);
        putInBagRight.curves[BipedRagdoll.rightArmID] = new Curve(0,0.1f);
        
        var putInBagLeft = stand.Single( "put in bag left", "stand_bag_put_in_left", false, 0.8f );
        putInBagLeft.nextState = stand["idle"];
        putInBagLeft.AddEvent( Event.Function( 0.5f, this, ApplyPutInBag, false ) );
        putInBagLeft.curves[BipedRagdoll.leftArmID] = new Curve(0,0.1f);
        putInBagLeft.curves[BipedRagdoll.rightArmID] = new Curve(0,0.1f);

        var takeFromBagRight = stand.Single( "take from bag right", "stand_bag_put_in_right", false, 0.8f );
        takeFromBagRight.nextState = stand["idle"];
        takeFromBagRight.AddEvent( Event.Function( 0.5f, this, TakeFromBag, false ) );
        takeFromBagRight.curves[BipedRagdoll.leftArmID] = new Curve(0,0.1f);
        takeFromBagRight.curves[BipedRagdoll.rightArmID] = new Curve(0,0.1f);
        
        var takeFromBagLeft = stand.Single( "take from bag left", "stand_bag_put_in_left", false, 0.8f );
        takeFromBagLeft.nextState = stand["idle"];
        takeFromBagLeft.AddEvent( Event.Function( 0.5f, this, TakeFromBag, true ) );
        takeFromBagLeft.curves[BipedRagdoll.leftArmID] = new Curve(0,0.1f);
        takeFromBagLeft.curves[BipedRagdoll.rightArmID] = new Curve(0,0.1f);
    }

    public DialogOption[] GetDialogOptions(){
        if( bag.isBeingGrabbed ) return null;
        return new DialogOption[]{
            new DialogOption("wear",DialogOptionType.GENERIC)
        };
    }
} 