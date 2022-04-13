using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{


public class Candle : Item
{
	[SerializeField]
	private GameObject lightContainer;

	[SerializeField]
	private AudioClip candleOnSound;
	
	[SerializeField]
	private AudioClip candleOffSound;

	private Coroutine toggleCoroutine = null;

	private void Toggle( OccupyState mainHoldState ){
		StopLightCoroutine();
		toggleCoroutine = GameDirector.instance.StartCoroutine( SetLightOn( !lightContainer.activeSelf, mainHoldState ) );
	}

	private void StopLightCoroutine(){
		if( toggleCoroutine != null ){
			GameDirector.instance.StopCoroutine( toggleCoroutine );
			toggleCoroutine = null;
		}
	}

	private IEnumerator SetLightOn( bool enable, OccupyState mainHoldState ){
		yield return new WaitForSeconds( 0.3f );
		lightContainer.SetActive( enable );
		yield return new WaitForSeconds( 0.2f );

		mainHoldState.AttemptDrop();
		if( enable ){
			SoundManager.main.RequestHandle( transform.position ).PlayOneShot( candleOnSound );
		}else{
			SoundManager.main.RequestHandle( transform.position ).PlayOneShot( candleOffSound );
		}
	}

	public override void OnPreDrop(){
		StopLightCoroutine();
	}

	public override void OnPostPickup(){
		Toggle( mainOccupyState );
	}

}

}