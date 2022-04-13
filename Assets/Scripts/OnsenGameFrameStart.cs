using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace viva{


public class OnsenGameFrameStart : MonoBehaviour{
	
	[SerializeField]
	private Rigidbody targetBody;
	[SerializeField]
	private OnsenGhostMiniGame targetGame;
	[SerializeField]
	private AudioClip suspenseEnterSound;
	[SerializeField]
	private AudioClip framePlacedSound;


	private void OnTriggerEnter( Collider collider ){
		if( collider.transform == targetBody.transform && !targetGame.gameObject.activeSelf ){
			
			SoundManager.main.RequestHandle( Vector3.zero, transform ).PlayOneShot( framePlacedSound );
			GameDirector.instance.PlayGlobalSound( suspenseEnterSound );

			targetBody.isKinematic = true;
			targetBody.transform.position = transform.position;
			targetBody.transform.rotation = transform.rotation*Quaternion.Euler( 0.0f, -90.0f, 0.0f );
			targetGame.gameObject.SetActive( true );
		}
	}
}

}