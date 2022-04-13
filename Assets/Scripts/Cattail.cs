using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace viva{


public class Cattail : Item {

	[SerializeField]
	private SoundSet smackFX;

	public void PlaySmackSound(){
		SoundManager.main.RequestHandle( transform.position ).PlayOneShot( smackFX.GetRandomAudioClip() );
	}
}

}