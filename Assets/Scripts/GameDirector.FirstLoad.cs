using System.Collections;
using System.IO;
using UnityEngine;


namespace viva{


public partial class GameDirector : MonoBehaviour {

	[SerializeField]
	private GameObject firstLoadHints;
	[SerializeField]
	private GameObject afterFirstLoadHints;
	[SerializeField]
	private GameObject firstLoadPrefab;

	
	private IEnumerator FirstLoadTutorial(){
		
		GameObject.Instantiate( firstLoadPrefab );
		yield return new WaitForSeconds(0.5f );
		settings.SelectBestVRControllerSetup();
		if( player ){
			player.OpenPauseMenu();
			player.pauseMenu.ShowFirstLoadInstructions();
		}
		while( IsAnyUIMenuActive() ){
			yield return null;
		}
		firstLoadHints.SetActive(true);
	}

	private void LoadLanguage(){
		
        string path = "Languages/"+languageName+".lang";
        if( !File.Exists( path ) ){
			Debug.Log("[Language] could not read ["+path+"]");
            return;
        }
		Debug.Log("[Language] Loading "+languageName);

        string data = File.ReadAllText( path );
		try{
	        m_language = JsonUtility.FromJson( data, typeof(Language) ) as Language;
		}catch{
			Debug.LogError("[Language] Could not parse!");
			m_language = null;
		}
	}
}

}