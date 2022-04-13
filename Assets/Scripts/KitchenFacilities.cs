using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace viva{

public class KitchenFacilities: Mechanism {


    [SerializeField]
    public Vector3 centerLocalPos;

    private static List<KitchenFacilities> facilities = new List<KitchenFacilities>();

    public static KitchenFacilities FindNearestFacility( Vector3 pos ){
        float least = Mathf.Infinity;
        KitchenFacilities nearest = null;
        foreach( KitchenFacilities facility in facilities ){
            float sqDst = Vector3.SqrMagnitude( pos-facility.centerLocalPos );
            if( sqDst < least ){
                least = sqDst;
                nearest = facility;
            }
        }
        return nearest;
    }

    public override void OnMechanismAwake(){
        facilities.Add( this );
    }
    
    public override bool AttemptCommandUse( Loli targetLoli, Character commandSource ){
        if( targetLoli == null ){
            return false;
        }
        return targetLoli.active.cooking.AttemptBeginCooking( this );
    }

    public override void EndUse( Character targetCharacter ){
    }

    public void OnDrawGizmosSelected(){
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere( transform.TransformPoint( centerLocalPos ), 0.2f );
    }
}

}