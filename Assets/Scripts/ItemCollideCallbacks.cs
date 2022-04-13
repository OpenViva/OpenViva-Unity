using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace viva{


public class ItemCollideCallbacks: MonoBehaviour {

    public UnityEvent onPlayerTrigger;


    private void OnTriggerEnter( Collider collider ){
        Item item = collider.GetComponent<Item>();
        if( item == null ){
            return;
        }
        if( item.settings.itemType == Item.Type.CHARACTER ){
            if( item.mainOwner == GameDirector.player ){
                onPlayerTrigger?.Invoke();
            }
        }
    }
}

}