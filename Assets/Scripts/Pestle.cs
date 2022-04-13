using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace viva{


public class Pestle : Item{

	[Range(0.0f,0.1f)]
	[SerializeField]
	private float bottomOffset = 0.01f;
	[SerializeField]
	private AudioClip grindSound;
	[Range(0.0f,1.0f)]
	[SerializeField]
	private float grindMinimum;

	private Mortar targetMortar;
	private Vector3? lastGrindBottom = null;
	private float lastGrindSoundTime = 0;
	private Coroutine grindSoundCoroutine;

	public override void OnItemFixedUpdate(){
		base.OnItemFixedUpdate();

		//find Mortar
		Vector3 bottom = transform.position+transform.up*bottomOffset;
		Collider[] results = Physics.OverlapSphere( bottom, 0.025f, Instance.itemsMask );
		foreach( Collider collider in results ){
			Item item = collider.gameObject.GetComponent<Item>();
			if( item == null ){
				continue;
			}
			targetMortar = item as Mortar;
			if( targetMortar != null ){
				break;
			}
		}

		Vector3? currGrindBottom = null;
		if( targetMortar != null ){
			//check if twisting inside mortar
			if( !targetMortar.IsPointInsideMixingHalfSphere( bottom ) ){
				return;
			}
			//if pointing against each other
			if( Vector3.Dot( transform.up, targetMortar.transform.up ) > -0.7f ){
				return;
			}
			currGrindBottom = targetMortar.transform.InverseTransformPoint( bottom );
			if( lastGrindBottom.HasValue ){
				//calculate grind speed (distance/time)
				float grindSpeed = Vector3.Distance( lastGrindBottom.Value, currGrindBottom.Value )/Time.deltaTime;
				if( grindSpeed > grindMinimum ){
					targetMortar.Grind( grindSpeed*0.01f );
					lastGrindSoundTime = Time.time;
					if( grindSoundCoroutine == null ){
						grindSoundCoroutine = GameDirector.instance.StartCoroutine( PlayGrindSoundCoroutine() );
					}
				}
			}
		}
		lastGrindBottom = currGrindBottom;
	}

	protected override void OnDrawGizmosSelected(){
		base.OnDrawGizmosSelected();
		Gizmos.color = new Color( 0.0f, 0.5f, 1.0f, 0.5f );
		Gizmos.DrawSphere( transform.position+transform.up*bottomOffset, 0.025f );
	}

	private IEnumerator PlayGrindSoundCoroutine(){ 

		var handle = SoundManager.main.RequestHandle( transform.position );
		handle.Play( grindSound );
		while( Time.time-lastGrindSoundTime <= 0.1f ){
			yield return new WaitForSeconds(0.1f);
		}
		handle.Stop();
		grindSoundCoroutine = null;
	}
}

}