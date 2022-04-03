using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using viva;


public class Lantern: VivaScript{

	private Material glassMaterial;
	private Light light;
	private ItemUserListener playerUser;


	public Lantern( Item item ){
		var lightPos = item.model.FindChildModel("lightPos");
		if( lightPos == null ) return;

		light = Util.SetupLight( lightPos.rootTransform.gameObject );
		light.enabled = false;
		light.range = 5f;
		light.color = new Color( 1.0f, 0.6f, 0.4f )*8.0f;

		foreach( var material in item.model.renderer.materials ){
			if( material.name.ToLower().Contains("glass") ){
				glassMaterial = material;
				break;
			}
		}

		playerUser = new ItemUserListener( item, this, BindCharacter, UnbindCharacter );

		SetLightOn( false );
		SetLightOn( true );
	}

	private void BindCharacter( Character character, GrabContext context ){
		if( character.possessor ){
			if( character.isBiped && context.grabber == character.biped.rightHandGrabber ){
				character.GetInput( Input.RightAction ).onDown.AddListener( this, PlayerToggles );
			}else{
				character.GetInput( Input.LeftAction ).onDown.AddListener( this, PlayerToggles );
			}
		}else{
			SetLightOn( true );

		}
	}

	private void PlayerToggles(){
		if( !playerUser.character ) return;
		if( !playerUser.character.possessor ) return;
		if( playerUser.character.possessor.movement.leftShiftDown ) return;
		
		ToggleLight();
	}

	private void UnbindCharacter( Character character, GrabContext context ){
		if( character.possessor ){
			if( character.isBiped && context.grabber == character.biped.rightHandGrabber ){
				character.GetInput( Input.RightAction ).onDown.RemoveListener( this, ToggleLight );
			}else{
				character.GetInput( Input.LeftAction ).onDown.RemoveListener( this, ToggleLight );
			}
		}
	}

	private void ToggleLight(){
		SetLightOn( !light.enabled );
	}

	private void SetLightOn( bool on ){
		light.enabled = on;

		float multiplier = System.Convert.ToSingle( light.enabled )*10000.0f;
		glassMaterial.SetColor("_EmissionMult", Color.white*multiplier );
	}
}  