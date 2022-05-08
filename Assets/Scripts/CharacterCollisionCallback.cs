using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{


public class CharacterCollisionCallback : MonoBehaviour {

	public enum Type{
		RIGHT_INDEX_FINGER,
		LEFT_INDEX_FINGER,
		RIGHT_PALM,
		LEFT_PALM,
		HEAD,
		ROOT,
		RIGHT_FOOT,
		LEFT_FOOT,
		KEYBOARD_HELPER,
		PLAYER_PROXIMITY,
		TORSO,
		RIGHT_ARM,
		LEFT_ARM
	}

	[SerializeField]
	private Character m_owner;
	public Character owner { get{ return m_owner; } }
	[SerializeField]
	private Type m_collisionPart;
	public Type collisionPart { get{ return m_collisionPart; } }


	private void OnCollisionEnter( Collision collision ){
		owner.OnCharacterCollisionEnter( this, collision );
	}

	private void OnCollisionStay( Collision collision ){
		owner.OnCharacterCollisionStay( this, collision );
	}

	private void OnCollisionExit( Collision collision ){
		owner.OnCharacterCollisionExit( this, collision );
	}
}

}