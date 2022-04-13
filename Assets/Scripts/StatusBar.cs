using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace viva{


public class StatusBar : MonoBehaviour{
    
	[SerializeField]
	private GameObject meterContainer;
	[SerializeField]
	private MeshRenderer meterMR;
	[SerializeField]
	private Text infoText;

	public void SetInfoText( string info, float meterPercent=0.0f ){
		
		if( meterPercent == 0.0f && info == null ){
			meterContainer.SetActive( false );
			return;
		}
		infoText.text = info;
		meterContainer.SetActive( true );
		if( meterPercent == 0.0f ){
			meterMR.enabled = false;
		}else{
			meterMR.enabled = true;
			meterMR.material.SetFloat( "_Fill", meterPercent );
			if( meterPercent == 1.0f ){
				meterMR.material.SetColor( "_FillColor", Color.green );
			}else{
				meterMR.material.SetColor( "_FillColor", Color.red );
			}
		}
	}

    public void FaceCamera(){
		infoText.transform.rotation = Quaternion.LookRotation( infoText.transform.position-GameDirector.instance.mainCamera.transform.position, GameDirector.instance.mainCamera.transform.up );
    }
}

}