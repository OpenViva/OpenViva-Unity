using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using viva;


public class MixingBowl: VivaScript{

    private Item item;
    private ItemUserListener playerListener;
    private Item mixingSpoon;
    private float mixPercent;
    private float lastMixTime;
    private SoundHandle mixSound;

    public MixingBowl( Item _item ){
        item = _item;
        item.onAttributeChanged.AddListener( this, OnAttributeChanged );

        Sound.PreloadSet("mortar");

        playerListener = new ItemUserListener( item, this, BindPlayer, UnbindPlayer  );
 
        Sound.PreloadSet("mortar");
        item.onTriggerEnterItem.AddListener( this, OnTriggerEnterItem );
        item.onTriggerExitItem.AddListener( this, OnTriggerExitItem );
        item.onReceiveSpill.AddListener( this, OnReceiveSpill );
    }

    private void OnReceiveSpill( Attribute[] attributes ){
        foreach( var attribute in attributes ){
            if( attribute.name == "flour" ){
                item.AddAttribute("mixing flour");
                Sound.Create( item.rigidBody.centerOfMass, item.transform ).Play( "flour", "intoContainer" );
            }else if( attribute.name.StartsWith("flavor:") ){
                item.RemoveAttributeWithPrefix("flavor:");
                item.AddAttribute( attribute.name );
            }else if( attribute.name == "mixed egg" ){
                item.AddAttribute( "mixed egg" );
            }
        }
    }

    private Attribute[] OnSpill(){
        var attributesToSpill = new List<Attribute>();

        if( item.HasAttribute("mixing flour") ){
            attributesToSpill.Add( new Attribute("mixing flour",1) );
            item.RemoveAttribute("mixing flour");
            Sound.Create( item.rigidBody.centerOfMass, item.transform ).Play( "flour", "spill" );
        }

        var flavor = item.FindAttributeWithPrefix("flavor:");
        if( flavor != null ){
            attributesToSpill.Add( new Attribute(flavor,1) );
            item.RemoveAttribute( flavor );
        }
        return attributesToSpill.ToArray();
    }

    private void OnTriggerEnterItem( Item otherItem ){
        if( otherItem.HasAttribute("mixingSpoon") ){
            mixingSpoon = otherItem;
        }
    }

    private void OnTriggerExitItem( Item otherItem ){
        if( otherItem == mixingSpoon ){
            mixingSpoon = null;
        }
    }

    private void BindPlayer( Character character, GrabContext context ){
        if( !character.isBiped ) return;
		SetupAnimations( character );
        if( character.possessor && character.possessor.isUsingKeyboard ){
            character.GetInput( Input.LeftAction ).onDown.AddListener( this, OnActionDown );
            character.GetInput( Input.RightAction ).onDown.AddListener( this, OnActionDown );
            character.GetInput( Input.LeftAction ).onUp.AddListener( this, OnActionUp );
            character.GetInput( Input.RightAction ).onUp.AddListener( this, OnActionUp );
        }
        var mixingSpoonListenTask = new Task( character.autonomy );
        mixingSpoonListenTask.onFixedUpdate += OnMixingSpoonCheck;
        mixingSpoonListenTask.StartConstant( this, "mortar mixingSpoon action" );
	}

    private void OnMixingSpoonCheck(){
        if( mixingSpoon && mixingSpoon.isBeingGrabbed ){
            var relativeVel = item.rigidBody.transform.InverseTransformVector( mixingSpoon.rigidBody.velocity);
            if( relativeVel.sqrMagnitude > 0.02f ){
                lastMixTime = Time.time;
                mixPercent += Time.deltaTime*0.25f;
                if( mixPercent > 1f ){
                    mixPercent = 0;
                    item.RemoveAttribute( "mixing flour" );
                    item.RemoveAttribute( "mixing egg" );
                    var flavor = item.FindAttributeWithPrefix("flavor:");
                    if( flavor != null ){
                        item.RemoveAttribute( flavor );
                    }
                    
                    var pastry = Item.Spawn( "raw pastry", item.transform.position+item.transform.up*0.1f, Quaternion.identity );
                    if( pastry ){
                        pastry.rigidBody.velocity = Vector3.up*0.5f;
                        if( playerListener.character ){
                            playerListener.character.biped.vision.See( pastry );
                        }
                        pastry.AddAttribute( flavor );
                    }
                    
                }
            }
            if( Time.time-lastMixTime < 0.25f ){
                if( mixSound == null ){
                    mixSound = Sound.Create( Vector3.zero, mixingSpoon.rigidBody.transform );
                    mixSound.loop = true;
                    mixSound.Play( "generic", "flour", "mixing batter.wav" );
                }
            }else{
                StopMixSound();
            }
        }else{
            StopMixSound();
        }
    }

    private void StopMixSound(){
        if( mixSound != null ){
            mixSound.Stop();
            mixSound = null;
        }
    }

    private void UnbindPlayer( Character character, GrabContext context ){
        if( character.possessor && character.possessor.isUsingKeyboard ){
            character.GetInput( Input.LeftAction ).onDown.RemoveListener( this, OnActionDown );
            character.GetInput( Input.RightAction ).onDown.RemoveListener( this, OnActionDown );
            character.GetInput( Input.LeftAction ).onUp.RemoveListener( this, OnActionUp );
            character.GetInput( Input.RightAction ).onUp.RemoveListener( this, OnActionUp );
        }
    }

    private void OnActionDown(){
		if( !playerListener.character || !playerListener.character.isPossessed ) return;
        var kbControls = playerListener.character.possessor.controls as KeyboardCharacterControls;
        kbControls.PlayAvailableInteraction();
    }

    private void OnActionUp(){
		if( !playerListener.character || !playerListener.character.isPossessed ) return;
        var kbControls = playerListener.character.possessor.controls as KeyboardCharacterControls;
        kbControls.StopAvailableInteraction();
    }


    private void BreakEgg( object eggIsInRightHand ){
        if( !playerListener.character ) return;
        bool eggSide = (bool)eggIsInRightHand;
        List<Item> eggs;
        if( eggSide ){
            eggs = playerListener.character.biped.rightHandGrabber.GetAllItems();
        }else{
            eggs = playerListener.character.biped.leftHandGrabber.GetAllItems();
        }
        foreach( var egg in eggs ){
            egg.scriptManager.CallOnScript("egg_item","BreakEgg", null);
        }
    }

    private void SetupAnimations( Character character ){

        var stand = character.animationSet.GetBodySet("stand", character.mainAnimationLayerIndex );

		var standMixingBowlMixRight = stand.Single( "mixing bowl mix right", "stand_mixing_bowl_mix_right", true, 1 );
        standMixingBowlMixRight.AddEvent( Event.Voice(0,"humming",true) );
		standMixingBowlMixRight.curves[BipedRagdoll.emotionID] = new Curve(1);
		var standMixingBowlMixLeft = stand.Single( "mixing bowl mix left", "stand_mixing_bowl_mix_left", true, 1 );
        standMixingBowlMixLeft.AddEvent( Event.Voice(0,"humming",true) );
		standMixingBowlMixLeft.curves[BipedRagdoll.emotionID] = new Curve(1);

		var eggIntoMixingBowlRight = stand.Single( "egg into mixing bowl right", "stand_egg_into_mixing_bowl_right", false, 1f );
        eggIntoMixingBowlRight.AddEvent( Event.Voice(0,"humming",true) );
		eggIntoMixingBowlRight.AddEvent( Event.Function( 0.6f, this, BreakEgg, true ) );
		eggIntoMixingBowlRight.curves[BipedRagdoll.emotionID] = new Curve(1);
        eggIntoMixingBowlRight.nextState = stand["idle"];
        var eggIntoMixingBowlLeft = stand.Single( "egg into mixing bowl left", "stand_egg_into_mixing_bowl_left", false, 1f );
        eggIntoMixingBowlLeft.AddEvent( Event.Voice(0,"humming",true) );
		eggIntoMixingBowlLeft.AddEvent( Event.Function( 0.6f, this, BreakEgg, false ) );
		eggIntoMixingBowlLeft.curves[BipedRagdoll.emotionID] = new Curve(1);
        eggIntoMixingBowlLeft.nextState = stand["idle"];
    }

    private void OnAttributeChanged( Item item, Attribute attribute ){
        if( attribute.name == "mixing flour" ){
            item.AnimateBlendShape( "batter", "flour", Mathf.Clamp01( 0.5f*attribute.count ), 0.4f );
            item.EnableSpilling( OnSpill );
        }
        if( attribute.name == "flavor:strawberry" ){
            item.SetModelColor( "batter", new Color(1f,0.2f,0.2f) );
        }
        if( attribute.name == "flavor:cantaloupe" ){
            item.SetModelColor( "batter", new Color(0.4f,0.8f,0.2f) );
        }
        if( attribute.name == "flavor:peach" ){
            item.SetModelColor( "batter", new Color(1f,0.7f,0.5f) );
        }
    }
} 