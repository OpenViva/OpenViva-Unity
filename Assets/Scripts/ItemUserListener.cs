using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace viva{

public delegate void ItemCharacterCallback( Character character, GrabContext context );

public class ItemUserListener{

    private readonly Item item;
    private readonly VivaScript source;
    public Character character { get; private set; }
    private readonly ItemCharacterCallback onBind;
    private readonly ItemCharacterCallback onUnbind;


    public ItemUserListener( Item _item, VivaScript _source, ItemCharacterCallback _onBind, ItemCharacterCallback _onUnbind ){
        if( _item == null ) throw new System.Exception("Cannot have a null _item for ItemUserListener");
        if( _source == null ) throw new System.Exception("Cannot have a null _registry for ItemUserListener");
        item = _item;
        source = _source;
        onBind = _onBind;
        onUnbind = _onUnbind;

        foreach( var grabbable in item.grabbables ){
            grabbable.onGrabbed.AddListener( source, OnGrabbed );
            grabbable.onReleased.AddListener( source, OnReleased );
        }
    }

    private void OnGrabbed( GrabContext context ){
        if( character != null ) return;

        var newTarget = context.grabber.character;
        if( newTarget == null ) return;

        //setup isteners for character
        character = newTarget;
        try{
            onBind( character, context );
        }catch( System.Exception e ){
            Script.HandleScriptException( e, "In Script \""+source._InternalGetScript().name+"\" task callback \"onBind\"" );
        }
    }
    private void OnReleased( GrabContext context ){
        if( character == null ) return;
        //if no longer grabbed by target, replace with next grabber
        if( !item.IsBeingGrabbedByCharacter( character ) ){
            onUnbind( character, context );
            character = null;

            foreach( var grabbable in item.grabbables ){
                //pick next user
                if( grabbable.grabContextCount > 0 ){
                    OnGrabbed( grabbable.GetGrabContext(0) );
                    break;
                }
            }
        }
    }
}

}