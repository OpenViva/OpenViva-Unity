using UnityEngine;
using System.Collections;
using System.Collections.Generic;



namespace viva{


public partial class CharacterSelectionTarget : MonoBehaviour {
	
	[SerializeField]
	private Loli target;
	[SerializeField]
	private MeshRenderer meshRenderer;

	private Coroutine coroutine = null;

    private static readonly int scaleID = Shader.PropertyToID("_Scale");
    private static readonly int alphaID = Shader.PropertyToID("_Alpha");
    private static readonly int additiveID = Shader.PropertyToID("_Additive");


	private void ClearCoroutine(){
		if( coroutine != null ){
			GameDirector.instance.StopCoroutine( coroutine );
			coroutine = null;
		}
	}

	public void ShowStatus( float startSize, float startAlpha, float endSize, float endAlpha ){
		ClearCoroutine();
		coroutine = GameDirector.instance.StartCoroutine( ShowNewTargetStatus( startSize, startAlpha, endSize, endAlpha ) );
	}
	
	public void OnSelected(){
		ShowStatus( 0.075f, 0.0f, 0.125f, 0.8f );
	}
	
	public void OnUnselected(){
		ShowStatus( 0.125f, 1.0f, 0.0f, 0.0f );
	}

	private IEnumerator ShowNewTargetStatus( float startSize, float startAlpha, float endSize, float endAlpha ){

		meshRenderer.enabled = true;
		
		float timer = 0.0f;
		float duration = 0.25f;
		while( timer < duration ){
			timer = Mathf.Min( timer+Time.deltaTime, duration );
			float ratio = timer/duration;
			float easeIn = 1.0f-Mathf.Pow( 1.0f-ratio, 2 );
			meshRenderer.material.SetFloat( scaleID, Mathf.Lerp( startSize, endSize, easeIn ) );
			meshRenderer.material.SetFloat( alphaID, Mathf.Lerp( startAlpha, endAlpha, easeIn ) );
			meshRenderer.material.SetFloat( additiveID, 1.0f-easeIn );
			yield return null;
		}
		
		if( endAlpha <= 0.0f ){
			meshRenderer.enabled = false;
		}
		coroutine = null;
	}
}

}