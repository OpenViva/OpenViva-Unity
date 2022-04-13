using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace viva{

public class WaterCurrent : MonoBehaviour{
    
    [SerializeField]
    private Vector3 m_force;
    public Vector3 force { get{ return m_force; } }

    public void OnDrawGizmosSelected(){

        BoxCollider bc = gameObject.GetComponent<BoxCollider>();
        if( bc == null ){
            return;
        }
        Vector3 center = transform.TransformPoint( bc.center+Vector3.up*bc.size.y );
        //draw normal direction
        Gizmos.color = Color.white;
        Vector3 flatDir = transform.up;
        flatDir.y = 0.0001f;
        flatDir = flatDir.normalized;
        Gizmos.DrawLine( center, center+transform.up*0.1f );
        Gizmos.DrawLine( center, center+flatDir*0.25f );
        Gizmos.DrawLine( center+transform.up*0.1f, center+flatDir*0.25f );

        //draw current direction
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine( center, center+transform.up*0.2f );
        Gizmos.DrawLine( center, center+force.normalized*0.5f );
        Gizmos.DrawLine( center+transform.up*0.2f, center+force.normalized*0.5f );
    }
    
    public void OnTriggerEnter( Collider collider ){
        Character character = collider.GetComponent<Character>();
        if( character == null ){
            return;
        }
        character.footstepInfo.AddtoFootstepRegion( FootstepInfo.Type.WATER );
    }
    
    public void OnTriggerExit( Collider collider ){
        Character character = collider.GetComponent<Character>();
        if( character == null ){
            return;
        }
        character.footstepInfo.RemoveFromFootstepRegion( FootstepInfo.Type.WATER );
    }
}


}