using System.Collections;
using UnityEngine;


namespace viva{


public class ChurchBell : MonoBehaviour {

	[SerializeField]
	private AudioSource bellSource;

	private Coroutine waitCoroutine = null;
	private float bellTimer = Mathf.NegativeInfinity;


	private bool AllowedToPlayBell(){
		if( GameDirector.skyDirector.daySegment == SkyDirector.DaySegment.NIGHT ){
			return false;
		}
		return Time.time-bellTimer>0.0f;
	}

    public void OnTriggerEnter( Collider collider ){
		Camera camera = collider.GetComponent<Camera>();
		if( camera && waitCoroutine == null ){
			waitCoroutine = GameDirector.instance.StartCoroutine( WaitForBell() );
		}
    }

	private IEnumerator WaitForBell(){

		while( true ){
			if( AllowedToPlayBell() ){
				bellSource.Play();
				bellTimer = Time.time+200.0f;
			}
			yield return new WaitForSeconds(200.0f);
		}
	}

    public void OnTriggerExit( Collider collider ){
		if( waitCoroutine != null ){
			GameDirector.instance.StopCoroutine( waitCoroutine );
			waitCoroutine = null;
		}
    }
}

}