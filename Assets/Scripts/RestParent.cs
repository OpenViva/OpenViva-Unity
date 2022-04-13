using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace viva{


public class RestParent : MonoBehaviour{

    [SerializeField]
    private Transform m_targetParent;
    public Transform targetParent { get{ return m_targetParent; } }

    private List<Item> listenToBeginRestParent = new List<Item>();

	public void OnTriggerEnter( Collider collider ){
        Item item = collider.gameObject.GetComponent<Item>();
        if( item == null || item.rigidBody == null ){
            return;
        }
        if( !item.CanBePlacedInRestParent() ){
            return;
        }
        if( item.mainOccupyState != null ){
            return;
        }
        item.rigidBody.WakeUp();
        item.rigidBody.isKinematic = false;
	}

	public void OnTriggerExit( Collider collider ){
		Item item = collider.gameObject.GetComponent<Item>();
        if( item == null ){
            return;
        }
        if( item.mainOccupyState != null ){
            return;
        }
	}

}

}