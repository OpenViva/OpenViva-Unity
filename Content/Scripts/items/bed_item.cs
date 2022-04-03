using System.Collections;
using UnityEngine;
using viva;


public class BedItem: VivaScript{

    private Item item;

    public BedItem( Item _item ){
        item = _item;
    }

    public DialogOption[] GetDialogOptions(){
        return new DialogOption[]{
            new DialogOption("sleep",DialogOptionType.GENERIC)
        };
    }

    public void OnDialogOption( Character character, DialogOption option ){
        if( option.Equals( new DialogOption( "sleep", DialogOptionType.GENERIC ) ) ){
            character.scriptManager.CallOnScript( "sleep", "SleepOnBed", new object[]{ item } );
        }
    }
} 