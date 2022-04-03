using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace viva{


public class ServerSelectUI : MonoBehaviour{

    [SerializeField]
    private RectTransform serverListContainer;
    [SerializeField]
    private GameObject serverEntryButtonPrefab;

    public void OnEnable(){
        BuildServerEntries();
    }

    public static bool debugInit = false;
    public void Start(){ if( !debugInit){ debugInit = true; } }

    private void BuildServerEntries(){

        GameObject entry = GameObject.Instantiate( serverEntryButtonPrefab, Vector3.zero, Quaternion.identity );
        entry.transform.SetParent( serverListContainer, false );
        
        string address = null;
        if( entry.transform.childCount >= 1 ){
            Text text = entry.transform.GetChild(0).GetComponent<Text>();
            if( text ){
                address = text.text;
            }
        }
        Button button = entry.GetComponent<Button>();
        if( button ){
            button.onClick.AddListener( delegate{ OnClickedServerEntry(address); } );
        }
    }

    private void OnClickedServerEntry( string address ){
        Debug.Log("Connecting to ["+address+"]");
    }
}

}