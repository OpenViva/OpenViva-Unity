using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace viva{


public class MarketContainerLocator : MonoBehaviour {

    [SerializeField]
    private MerchantSpot merchantSpot;

    public void OnTriggerEnter( Collider collider ){
        if( merchantSpot == null ){
            return;
        }
        var mc = collider.GetComponent<MarketContainer>();
        if( mc ){
            mc.SetParentLocator( merchantSpot );
            mc.SetMerchantGoodRegistry( merchantSpot,
                merchantSpot.GetNextMerchantGoodIndex(
                    !mc.ContainsMerchangGoodRegistry( merchantSpot )
                )
            );
        }
    }
    
    public void OnTriggerExit( Collider collider ){
        if( merchantSpot == null ){
            return;
        }
        var mc = collider.GetComponent<MarketContainer>();
        if( mc ){
            mc.SetParentLocator( null );
        }
    }

}

}