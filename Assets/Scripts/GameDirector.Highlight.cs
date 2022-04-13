using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace viva{


public partial class GameDirector : MonoBehaviour {

	[SerializeField]
	private GameObject highlightObj = null;
	[SerializeField]
	private AudioClip highlightSound;

	private Coroutine highlightCoroutine = null;
	private static int highlightColorID = Shader.PropertyToID("_Color");
	private static int highlightOutlineID = Shader.PropertyToID("_Outline");

	private IEnumerator FadeHighlight( Mechanism targetMechanism, float highlightTime ){
		
		highlightObj.SetActive( true );
		MeshFilter mf = highlightObj.GetComponent(typeof(MeshFilter)) as MeshFilter;
		Material mat = (highlightObj.GetComponent(typeof(MeshRenderer)) as MeshRenderer).material;
		mf.sharedMesh = targetMechanism.highlightMesh;
		Color color = new Color(0.0f,0.66f,0.88f,1.0f);
		
		Transform targetTransform;
		if( targetMechanism.highlightTransformOverride != null ){
			targetTransform = targetMechanism.highlightTransformOverride;
		}else{
			targetTransform = targetMechanism.transform;
		}
		highlightObj.transform.position = targetTransform.position;
		highlightObj.transform.rotation = targetTransform.rotation;
		highlightObj.transform.localScale = targetTransform.lossyScale;

		float timer = highlightTime;
		while( timer > 0.0f ){

			float animation = timer/highlightTime;
			animation = animation*animation;
			color.a = animation;
			mat.SetFloat( highlightOutlineID, animation*0.04f );
			mat.SetColor( highlightColorID, color*animation );

			timer -= Time.deltaTime;
			yield return null;
		}
		highlightCoroutine = null;
		highlightObj.SetActive( false );	
	}

	public void HighlightMechanism( Mechanism targetMechanism ){
		if( targetMechanism == null ){
			Debug.LogError("ERROR Highlight mechanism is null! ");
			return;
		}
		if( highlightCoroutine != null ){
			GameDirector.instance.StopCoroutine( highlightCoroutine );
			highlightCoroutine = null;
		}
		PlayGlobalSound( highlightSound );
		highlightCoroutine = GameDirector.instance.StartCoroutine( FadeHighlight( targetMechanism, 1.0f ) );
	}
}


}