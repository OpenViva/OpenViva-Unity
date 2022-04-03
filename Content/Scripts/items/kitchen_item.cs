using System.Collections;
using UnityEngine;
using viva;


public class Kitchen: VivaScript{

    private Item item;
    private GameObject ovenLightPos;
    private SoundHandle fireLoop;
    private float timeLit = 0;

    public Kitchen( Item _item ){
        item = _item;

        ovenLightPos = item.model.FindChildModel("oven_light_pos")?.rootTransform.gameObject;
        foreach( var zone in item.zones ){

            zone.onTriggerEnterItem.AddListener( this, OnOvenItemEnter );
            zone.onTriggerExitItem.AddListener( this, OnOvenItemExit );
        }
    }

    private void OnOvenItemEnter( Item newItem ){
        newItem.scriptManager.CallOnAllScripts( "Baked", null, true );
        
        if( ovenLightPos && fireLoop==null ){
            var light = Util.SetupLight( ovenLightPos );
            light.enabled = true;
		    light.color = new Color( 1.0f, 0.4f, 0.1f )*8.0f;
            timeLit = Time.time;
            
            var oven = item.zones[1];
            fireLoop = Sound.Create( oven.transform.TransformPoint( oven.boxCollider.center ) );
            fireLoop.loop = true;
            fireLoop.Play( "generic", "oven", "fireLoop.wav" );
        }
    }

    private void OnOvenItemExit( Item newItem ){

        if( Time.time-timeLit < 1f ) return;
        foreach( var zone in item.zones ){
            if( zone.items.Count > 0 ) return;
        }
        //turn off
        if( ovenLightPos ){
            var light = Util.SetupLight( ovenLightPos );
            light.enabled = false;
            
            if( fireLoop != null ){
                fireLoop.Stop();
                fireLoop = null;
            }
        }
    }

    public DialogOption[] GetDialogOptions(){
        return new DialogOption[]{
            new DialogOption("strawberry pastry",DialogOptionType.GENERIC),
            new DialogOption("peach pastry",DialogOptionType.GENERIC),
            new DialogOption("cantaloupe pastry",DialogOptionType.GENERIC),
        };
    }

    public void OnDialogOption( Character character, DialogOption option ){
        MakeFood( character, option.value );
    }

    private void MakeFood( Character character, string food ){
        string[] attributes = null;
        if( food == "strawberry pastry" ) attributes = new string[]{"flavor:strawberry", "baked pastry"};
        if( food == "peach pastry" ) attributes = new string[]{"flavor:peach", "baked pastry"};
        if( food == "cantaloupe pastry" ) attributes = new string[]{"flavor:cantaloupe", "baked pastry"};
		new AssembleItem( character.autonomy, new AttributeRequest( attributes, false, CountMatch.EQUAL ) ).Start( this, "assemble food" );
    }
} 