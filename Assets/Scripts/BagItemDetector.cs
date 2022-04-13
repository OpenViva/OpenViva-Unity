using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace viva{

public class BagItemDetector : MonoBehaviour {

    [SerializeField]
    private MeshRenderer validStoreItemIndicator;
    [SerializeField]
    private Bag sourceBag;
    private Collider[] results = new Collider[7];
    [SerializeField]
    private Texture2D validIcon;
    [SerializeField]
    private Texture2D invalidIcon;

    private float lastDetectTime = 0.0f;

    public void HideIndicator(){
        validStoreItemIndicator.enabled = false;
    }

    public void UpdateDetectItem(){
        if( sourceBag.mainOccupyState == null ){
            HideIndicator();
            return;
        }
        //Detect 5 times per second
        if( Time.time-lastDetectTime < 0.25f ){
            return;
        }
        
        int collisions = Physics.OverlapSphereNonAlloc( transform.position, 0.17f, results, Instance.itemDetectorMask, QueryTriggerInteraction.Collide );
        for( int i=0; i<collisions; i++ ){
            Collider collider = results[i];
            Item item = collider.GetComponent<Item>();
            if( item == null || item == sourceBag || !item.mainOwner ){
                continue;
            }
            if( item.settings.itemType == Item.Type.CHARACTER ){
                continue;
            }
            validStoreItemIndicator.enabled = true;
            if( sourceBag.CanStoreItem( item ) ){
                validStoreItemIndicator.material.mainTexture = validIcon;
            }else{
                validStoreItemIndicator.material.mainTexture = invalidIcon;
            }
            return;
        }
        validStoreItemIndicator.enabled = false;
        ///TODO: STANDARDIZE IN A MORE ELEGANT WAY USING COLLISION EVENTS
    }

}

}