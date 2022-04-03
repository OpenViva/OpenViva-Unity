using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace viva{

public delegate bool BoolReturnFunc();

public class MessageManager : MonoBehaviour{

    private static List<string> pastMessages = new List<string>();
    public static MessageManager main;


    [SerializeField]
    private StandaloneMessage[] pool = new StandaloneMessage[3];
    [SerializeField]
    private StandaloneMessage messagePrefab;

    private int nextMessageIndex = 0;


    private void Awake(){
        main = this;
    }

    public void _InternalReset( List<string> _pastMessages ){
        if( _pastMessages == null ) _pastMessages = new List<string>();
        pastMessages = _pastMessages;
    }

    private bool CheckAndAddMessage( string text ){
        if( pastMessages.Contains( text ) ){
            return false;
        }
        pastMessages.Add( text );
        return true;
    }

    public void DisplayMessage( Vector3 position, string text, BoolReturnFunc onEnd, bool checkIfAlreadyDisplayed=true, bool playSound=true ){
        if( checkIfAlreadyDisplayed && !CheckAndAddMessage( text ) ) return;
        var msg = NextFromPool();
        if( playSound ) Sound.main.PlayGlobalUISound( UISound.MESSAGE_POPUP );
        msg.canvas.transform.position = position;
        msg.text.text = text;
        msg.onEnd = onEnd;
        msg.faceCamera = true;
        msg.canvas.gameObject.SetActive( true );
    }

    private StandaloneMessage NextFromPool(){
        var msg = pool[ nextMessageIndex ];
        if( msg == null ){
            msg = GameObject.Instantiate( messagePrefab );
            pool[ nextMessageIndex ] = msg;
        }
        nextMessageIndex = ( nextMessageIndex+1)%pool.Length;
        return msg;
    }
    
}

}