using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



namespace viva{

public class MessageDialog : MonoBehaviour{

    public class ButtonListEntry{
        public string label;
        public GenericCallback callback;

        public ButtonListEntry( string _label, GenericCallback _callback ){
            label = _label;
            callback = _callback;
        }
    }

    public enum Type: int{
        ERROR       =0xff0000,
        WARNING     =0xffff00,
        SUCCESS     =0x00ff00
    }

    [SerializeField]
    private Text titleText;
    [SerializeField]
    private Text contentText;
    [SerializeField]
    private Button accept;
    [SerializeField]
    private Text acceptText;
    [SerializeField]
    private Button cancel;
    [SerializeField]
    private Text cancelText;
    [SerializeField]
    private InputField inputField;
    [SerializeField]
    private Text inputFieldText;
    [SerializeField]
    private BlackWhiteList blackWhiteList;
    [SerializeField]
    private Transform buttonListContainer;
    [SerializeField]
    private GameObject buttonListEntryPrefab;

    public StringCallback onInputFieldChange;


    public void HandleInputFieldChange(){
        onInputFieldChange?.Invoke( inputField.text );
    }

    public void SetInputFieldColor( Color color ){
        inputFieldText.color = color;
    }

    public void DisplayError( Type type, string title, string error, GenericCallback onAccept=null, string acceptTitle=null, string cancelTitle=null ){

        if( type == Type.ERROR ){
            Debugger.LogError( error );
        }else if( type == Type.WARNING ){
            Debugger.LogWarning( error );
        }else{
            Debugger.Log( error );
        }
        
        if( Time.time < 5f ){
            return;
        }

        UI.main.OpenLastTab();
        
        SetupUIForMessage();

        int hex = (int)type;

        titleText.text = "<color=#"+hex.ToString("X6")+">"+title+"</color>";
        contentText.text = error;

        acceptText.text = acceptTitle==null ? "ok" : acceptTitle;
        accept.gameObject.SetActive( true );
        accept.onClick.RemoveAllListeners();
        accept.onClick.AddListener( delegate{
            CancelRestoreTab();
            onAccept?.Invoke();
        } );

        cancelText.text = cancelTitle==null ? "cancel" : cancelTitle;
        cancel.gameObject.SetActive( cancelTitle!=null );
        cancel.onClick.RemoveAllListeners();
        cancel.onClick.AddListener( CancelRestoreTab );

        gameObject.SetActive( true );
    }

    public void RequestBlackWhiteList( string title, string instruction1, string instruction2, List<string> _whitelist, List<string> _blacklist, StringListReturnFunc onAccept=null, GenericCallback onCancel=null ){

        SetupUIForMessage();
        blackWhiteList.gameObject.SetActive( true );

        blackWhiteList.SetupBlackWhiteList( instruction1, instruction2, _whitelist, _blacklist );
        
        titleText.text = title;
        contentText.text = "";
        
        accept.gameObject.SetActive( true );
        accept.onClick.RemoveAllListeners();
        accept.onClick.AddListener( CancelRestoreTab );
        accept.onClick.AddListener( delegate{ gameObject.SetActive( false ); onAccept?.Invoke( blackWhiteList.whitelist ); } );

        cancel.gameObject.SetActive( true );
        cancel.onClick.RemoveAllListeners();
        cancel.onClick.AddListener( delegate{
            CancelRestoreTab();
            UI.main.OpenLastTab();
        } );
        cancel.onClick.AddListener( delegate{ gameObject.SetActive( false ); onCancel?.Invoke(); } );

        gameObject.SetActive( true );
    }
    
    public void RequestInput( string title, string message, StringCallback onAccept=null, GenericCallback onCancel=null, string startingText=null ){

        SetupUIForMessage();
        inputField.gameObject.SetActive( true );

        titleText.text = "<color=#ffff00>"+title+"</color>";
        contentText.text = message;

        accept.gameObject.SetActive( true );
        accept.onClick.RemoveAllListeners();
        accept.onClick.AddListener( delegate{
            CancelRestoreTab();
            onAccept?.Invoke( inputField.text );
        } );
        
        cancel.gameObject.SetActive( true );
        cancel.onClick.RemoveAllListeners();
        cancel.onClick.AddListener( delegate{
            CancelRestoreTab();
            onCancel?.Invoke();
        } );

        gameObject.SetActive( true );

        inputField.text = startingText;
        inputFieldText.color = Color.white;
        inputField.Select();
    }

    public void RequestButtonSelection( string title, string message, ButtonListEntry[] list, GenericCallback onCancel=null ){
        SetupUIForMessage();
        buttonListContainer.gameObject.SetActive( true );

        titleText.text = "<color=#ffff00>"+title+"</color>";
        contentText.text = message;

        cancel.gameObject.SetActive( true );
        cancel.onClick.RemoveAllListeners();
        cancel.onClick.AddListener( delegate{
            CancelRestoreTab();
            onCancel?.Invoke();
        } );

        gameObject.SetActive( true );

        inputField.text = "";
        inputFieldText.color = Color.white;
        inputField.Select();

        int i=buttonListContainer.childCount;
        for( ; i-->list.Length; ){
            GameObject.Destroy( buttonListContainer.GetChild(i).gameObject );
        }

        foreach( var entry in list ){
            Transform entryTransform;
            if( i >= 0 ){
                entryTransform = buttonListContainer.GetChild(i--);
            }else{
                entryTransform = GameObject.Instantiate( buttonListEntryPrefab, buttonListContainer ).transform;
            }

            var func = entry.callback;
            var button = entryTransform.GetComponent<Button>();
            if( func == null ){
                button.interactable = false;
            }else{
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener( delegate{ func(); } );
            }

            var text = entryTransform.GetChild(0).GetComponent<Text>();
            text.text = entry.label;
        }
    }

    private void CancelRestoreTab(){
        Cancel();
        UI.main.OpenLastTab();
    }

    public void Cancel(){
        gameObject.SetActive( false );
        UI.main.blockRaycast.SetActive( false );
    }

    private void SetupUIForMessage(){
        inputField.gameObject.SetActive( false );
        blackWhiteList.gameObject.SetActive( false );
        buttonListContainer.gameObject.SetActive( false );
        accept.gameObject.SetActive( false );
        cancel.gameObject.SetActive( false );
    }
}


}