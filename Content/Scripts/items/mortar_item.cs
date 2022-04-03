using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using viva;


public class MortarItem: VivaScript{

    private Item item;
    private ItemUserListener userListener;
    private Item pestle;
    private float grindPercent;
    private float lastGrindTime;
    private SoundHandle grindSound;


    public MortarItem( Item _item ){
        item = _item;    
        item.onAttributeChanged.AddListener( this, OnAttributeChanged );

        userListener = new ItemUserListener( item, this, BindCharacter, UnbindCharacter  );
 
        Sound.PreloadSet("mortar");

        item.onCollision.AddListener( this, OnCollision );
        item.onTriggerEnterItem.AddListener( this, OnTriggerEnterItem );
        item.onTriggerExitItem.AddListener( this, OnTriggerExitItem );
        item.onReceiveSpill.AddListener( this, OnReceiveSpill );
    }

    private void OnReceiveSpill( Attribute[] attributes ){

        foreach( var attribute in attributes ){
            if( attribute.name == "flour" ){
                item.AddAttribute("flour");
                Sound.Create( item.rigidBody.centerOfMass, item.transform ).Play( "flour", "intoContainer" );
            }else if( attribute.name.StartsWith("flavor:") ){
                SetFlavor( attribute.name );
            }
        }
    }

    private void OnTriggerEnterItem( Item otherItem ){
        if( otherItem.HasAttribute("pestle") ){
            pestle = otherItem;
        }
    }

    private void OnTriggerExitItem( Item otherItem ){
        if( otherItem == pestle ){
            pestle = null;
        }
    }

    private bool ValidForAction( Item otherItem ){
        if( Vector3.Dot( item.rigidBody.transform.up, Vector3.up ) < 0.3f ) return false;
        if( item.rigidBody.transform.InverseTransformPoint( otherItem.rigidBody.worldCenterOfMass ).y < 0f ) return false;
        return true;

    }

    private void OnCollision( Collision collision ){
        var collisionItem = Util.GetItem( collision.rigidbody );
        if( !collisionItem || collisionItem.destroyed || collisionItem.isBeingGrabbed ) return;

        if( !ValidForAction( collisionItem ) ) return;
        //add wheat seeds if it touches wheat
        if( collisionItem.HasAttribute("wheat") ){
            Viva.Destroy( collisionItem );
            item.AddAttribute( "wheat seeds" );
            Sound.Create( item.transform.position ).Play( "mortar", "grainPrepare" );
        }else if( collisionItem.HasAttribute("strawberry") ){
            Viva.Destroy( collisionItem );
            SetFlavor("strawberry");
            Sound.Create( item.transform.position ).Play( "mortar", "jellyScoop" );
        }else if( collisionItem.HasAttribute("cantaloupe") ){
            Viva.Destroy( collisionItem );
            SetFlavor("cantaloupe");
            Sound.Create( item.transform.position ).Play( "mortar", "jellyScoop" );
        }else if( collisionItem.HasAttribute("peach") ){
            Viva.Destroy( collisionItem );
            SetFlavor("peach");
            Sound.Create( item.transform.position ).Play( "mortar", "jellyScoop" );
        }
    }

    private void SetFlavor( string flavor ){
        item.RemoveAttributeWithPrefix("flavor:");
        item.AddAttribute( "flavor:"+flavor );
    }

    private void BindCharacter( Character character, GrabContext context ){
		SetupAnimations( character );
        if( character.possessor && character.possessor.isUsingKeyboard ){
            character.GetInput( Input.LeftAction ).onDown.AddListener( this, OnActionDown );
            character.GetInput( Input.RightAction ).onDown.AddListener( this, OnActionDown );
            character.GetInput( Input.LeftAction ).onUp.AddListener( this, OnActionUp );
            character.GetInput( Input.RightAction ).onUp.AddListener( this, OnActionUp );
        }
        var pestleListenTask = new Task( character.autonomy );
        pestleListenTask.onFixedUpdate += SpillCheck;
        pestleListenTask.StartConstant( this, "mortar pestle action" );
	}

    private void UnbindCharacter( Character character, GrabContext context ){
        if( character.possessor && character.possessor.isUsingKeyboard ){
            character.GetInput( Input.LeftAction ).onDown.RemoveListener( this, OnActionDown );
            character.GetInput( Input.RightAction ).onDown.RemoveListener( this, OnActionDown );
            character.GetInput( Input.LeftAction ).onUp.RemoveListener( this, OnActionUp );
            character.GetInput( Input.RightAction ).onUp.RemoveListener( this, OnActionUp );
        }
        character.autonomy.RemoveTask("mortar pestle action");
    }

    private void SpillCheck(){
        CheckPestleGrind();
    }

    private Attribute[] OnSpill(){
        var attributesToSpill = new List<Attribute>();

        if( item.HasAttribute("flour") ){
            attributesToSpill.Add( new Attribute("flour",1) );
            item.RemoveAttribute("flour");
            Sound.Create( item.rigidBody.centerOfMass, item.transform ).Play( "flour", "spill" );
        }

        var flavor = item.FindAttributeWithPrefix("flavor:");
        if( flavor != null ){
            attributesToSpill.Add( new Attribute(flavor,1) );
            item.RemoveAttribute( flavor );
        }
        return attributesToSpill.ToArray();
    }

    private void CheckPestleGrind(){
        if( pestle && pestle.isBeingGrabbed && ValidForAction( pestle ) ){
            if( item.HasAttribute("wheat seeds") ){
                var relativeVel = item.rigidBody.transform.InverseTransformVector( pestle.rigidBody.velocity);
                if( relativeVel.sqrMagnitude > 0.02f ){
                    lastGrindTime = Time.time;
                    grindPercent += Time.deltaTime*0.25f;
                    if( grindPercent > 1f ){
                        grindPercent = 0;
                        item.RemoveAttribute( "wheat seeds" );
                        item.AddAttribute( "flour" );
                    }
                }
            }
            if( Time.time-lastGrindTime < 0.25f ){
                if( grindSound == null ){
                    grindSound = Sound.Create( Vector3.zero, pestle.rigidBody.transform );
                    grindSound.loop = true;
                    grindSound.Play( "mortar","grainCrush" );
                }
            }else{
                StopGrindSound();
            }
        }else{
            StopGrindSound();
        }
    }

    private void StopGrindSound(){
        if( grindSound != null ){
            grindSound.Stop();
            grindSound = null;
        }
    }

    private void OnActionDown(){
		if( !userListener.character || !userListener.character.isPossessed ) return;
        var kbControls = userListener.character.possessor.controls as KeyboardCharacterControls;
        kbControls.PlayAvailableInteraction();
    }

    private void OnActionUp(){
		if( !userListener.character || !userListener.character.isPossessed ) return;
        var kbControls = userListener.character.possessor.controls as KeyboardCharacterControls;
        kbControls.StopAvailableInteraction();
    }

    private void OnAttributeChanged( Item item, Attribute attribute ){
        if( attribute.name == "wheat seeds"){
            item.AnimateBlendShape( "wheatCrushed", "grains1", System.Convert.ToUInt32( attribute.count>=1 ), 0.3f );
            item.AnimateBlendShape( "wheatCrushed", "grains2", System.Convert.ToUInt32( attribute.count>=2 ), 0.3f );
            item.AnimateBlendShape( "wheatCrushed", "grains3", System.Convert.ToUInt32( attribute.count>=3 ), 0.3f );
            if( attribute.count > 0 ){
                Sound.Create( Vector3.zero, item.transform ).Play( "mortar","grainPrepare");
                grindPercent = 0;
            }
        }
        if( attribute.name == "flour"){
            item.AnimateBlendShape( "wheatCrushed", "flourGrow", Mathf.Clamp01( attribute.count ), 0.4f );
            item.EnableSpilling( OnSpill );
        }
        if( attribute.name == "flavor:strawberry" ){
            if( attribute.count > 0 ){
                item.SetModelColor( "wheatCrushed", new Color(1f,0.2f,0.2f) );
                Sound.Create( Vector3.zero, item.transform ).Play( "mortar","jellyScoop");
            }else{
                item.SetModelColor( "wheatCrushed", Color.white );
            }
        }
        if( attribute.name == "flavor:cantaloupe" ){
            if( attribute.count > 0 ){
                item.SetModelColor( "wheatCrushed", new Color(0.4f,0.8f,0.2f) );
                Sound.Create( Vector3.zero, item.transform ).Play( "mortar","jellyScoop");
            }else{
                item.SetModelColor( "wheatCrushed", Color.white );
            }
        }
        if( attribute.name == "flavor:peach" ){
            if( attribute.count > 0 ){
                item.SetModelColor( "wheatCrushed", new Color(1f,0.7f,0.5f) );
                Sound.Create( Vector3.zero, item.transform ).Play( "mortar","jellyScoop");
            }else{
                item.SetModelColor( "wheatCrushed", Color.white );
            }
        }
    }

    private void DropHandIntoOther( object handSideParam ){
        var user = userListener.character;
        if( !user ) return;
        Item item;
        Item otherItem;
        if( (bool)handSideParam ){
            item = user.biped.rightHandGrabber.mainGrabbed?.parentItem;
            otherItem = user.biped.leftHandGrabber.mainGrabbed?.parentItem;
            user.biped.rightHandGrabber.ReleaseAll();
        }else{
            item = user.biped.leftHandGrabber.mainGrabbed?.parentItem;
            otherItem = user.biped.rightHandGrabber.mainGrabbed?.parentItem;
            user.biped.leftHandGrabber.ReleaseAll();
        }
        if( item && otherItem ){
            item.rigidBody.velocity = ( otherItem.rigidBody.worldCenterOfMass-item.rigidBody.worldCenterOfMass ).normalized*32f;
        }
    }

    private void SetupAnimations( Character character ){
        var stand = character.animationSet.GetBodySet("stand", character.mainAnimationLayerIndex);

		var mortarAndPestleGrindRight = stand.Single( "mortar and pestle grind right", "stand_mortar_and_pestle_grind_right", true, 1 );
		mortarAndPestleGrindRight.curves[BipedRagdoll.emotionID] = new Curve(1);
        
        mortarAndPestleGrindRight.AddEvent( Event.Voice(0,"humming",true) );
		var mortarAndPestleGrindInLeft = stand.Single( "mortar and pestle grind left", "stand_mortar_and_pestle_grind_left", true, 1 );
		mortarAndPestleGrindInLeft.curves[BipedRagdoll.emotionID] = new Curve(1);
        mortarAndPestleGrindInLeft.AddEvent( Event.Voice(0,"humming",true) );

		var wheatIntoMortarInRight = stand.Single( "wheat into mortar right", "stand_wheat_into_mortar_in_right", false, 0.5f );
		wheatIntoMortarInRight.AddEvent( Event.Function( 0.7f, this, DropHandIntoOther, true ) );
		wheatIntoMortarInRight.curves[BipedRagdoll.emotionID] = new Curve(1);
		var wheatIntoMortarOutRight = new AnimationSingle( viva.Animation.Load("stand_wheat_into_mortar_out_right"), character, false, 1 );
        wheatIntoMortarOutRight.AddEvent( Event.Voice(0,"humming",true) );
		wheatIntoMortarInRight.nextState = wheatIntoMortarOutRight;
		wheatIntoMortarOutRight.nextState = stand["idle"];

		var wheatIntoMortarInLeft = stand.Single( "wheat into mortar left", "stand_wheat_into_mortar_in_left", false, 0.5f );
		wheatIntoMortarInLeft.AddEvent( Event.Function( 0.7f, this, DropHandIntoOther, false ) );
		wheatIntoMortarInLeft.curves[BipedRagdoll.emotionID] = new Curve(1);
		var wheatIntoMortarOutLeft = new AnimationSingle( viva.Animation.Load("stand_wheat_into_mortar_out_left"), character, false, 1 );
        wheatIntoMortarOutLeft.AddEvent( Event.Voice(0,"humming",true) );
		wheatIntoMortarInLeft.nextState = wheatIntoMortarOutLeft;
		wheatIntoMortarOutLeft.nextState = stand["idle"];

		var mortarIntoMixingBowlRight = stand.Single( "mortar into mixing bowl right", "stand_pour_mortar_into_mixing_bowl_right", false, 1 );
        mortarIntoMixingBowlRight.AddEvent( Event.Voice(0,"humming",true) );
		mortarIntoMixingBowlRight.nextState = stand["idle"];
		mortarIntoMixingBowlRight.curves[BipedRagdoll.emotionID] = new Curve(1);

		var mortarIntoMixingBowlLeft = stand.Single( "mortar into mixing bowl left", "stand_pour_mortar_into_mixing_bowl_left", false, 1 );
        mortarIntoMixingBowlLeft.AddEvent( Event.Voice(0,"humming",true) );
		mortarIntoMixingBowlLeft.nextState = stand["idle"];
		mortarIntoMixingBowlLeft.curves[BipedRagdoll.emotionID] = new Curve(1);
    }
} 