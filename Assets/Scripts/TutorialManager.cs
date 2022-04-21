using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace viva{


public class TutorialManager : VivaSessionAsset {
    
    //TODO: turn into own monobehaviours
    public enum Hint{
        HINT_BED,
        HINT_BATHROOM_DOOR,
        HINT_BATHE,
        HINT_CLOTHING,
        HINT_WATER_TEMPERATURE,
        HINT_VR_GESTURES,
        HINT_LEAVE_TO_UNDRESS,
        HINT_HOW_TO_USE_SOAP,
        HINT_KEEP_TRYING_HEADPATTING,
        HINT_KEEP_HAPPY_TO_HELP,
        HINT_HANDS_MUST_BE_SOAPY,
        HINT_HOW_TO_COMPLETE_BATHING,
        HINT_GET_ON_KNEES_FOR_WASH,
        HINT_TOWEL_TO_GET_OUT,
    }

    public static TutorialManager main = null;
    public delegate IEnumerator ExitCoroutineFunction( Item source );

    private bool[] hints = new bool[ System.Enum.GetValues(typeof(Hint)).Length ];
    private Coroutine activeMessageCoroutine = null;
    private Coroutine activeExitCoroutine = null;
    private Vector3? messageContainerAverage = null;

    [SerializeField]
    private AudioClip hintSound;
    [SerializeField]
    private Text messageText;
    [SerializeField]
    private Transform canvas;
    [SerializeField]
    private Image messageFrameImage;
    [SerializeField]
    private Image messageImage;
    [SerializeField]
    private float globalHintScale = 0.002f;
    [SerializeField]
    private Transform objectHintsContainer = null;


    protected override void OnAwake(){
        main = this;
    }

    public void DisplayObjectHint( Item source, string objName, ExitCoroutineFunction exitFunction=null ){
        if( !source ){
            return;
        }
        //skip if player is not nearby or triggered by a loli
        if( source.mainOwner == null ){
            if( Vector3.SqrMagnitude( source.transform.position-GameDirector.player.floorPos ) > 4.0f ){
                return;
            }
        }else if( source.mainOwner.characterType == Character.Type.LOLI ){
            return;
        }
        
        //find HintCollisionTrigger
        Transform child = objectHintsContainer.Find(objName);
        if( child ){
            HintCollisionTrigger trigger = child.GetComponent<HintCollisionTrigger>();
            trigger.ActivateHint( source.transform, source, exitFunction );
        }
    }

    private void UpdateMessageContainerPosition( Transform parent, Vector3 localPos ){
        if( parent == null ){
            return;
        }
        Vector3 pos = parent.position;
        pos.y += localPos.y;
        if( GameDirector.player.controls == Player.ControlType.KEYBOARD ){
            pos += GameDirector.player.head.forward*localPos.z;
            float bearingSign = Mathf.Sign( Tools.Bearing( GameDirector.player.head, parent.position ) );
            pos -= GameDirector.player.head.right*bearingSign*localPos.x;
        }
        if( !messageContainerAverage.HasValue ){
            messageContainerAverage = pos;
        }else{
            messageContainerAverage = Vector3.LerpUnclamped( messageContainerAverage.Value, pos, Time.deltaTime*8.0f );
        }
        canvas.position = messageContainerAverage.Value;       
    }

    public IEnumerator MessageCoroutine( Transform parent, Vector3 localPos, string message, Sprite messageSprite ){
        Debug.Log("[Hint] "+message);
        messageContainerAverage = null;
        canvas.gameObject.SetActive( true );
        GameDirector.instance.PlayGlobalSound( hintSound );

        messageText.text = message;
        LayoutRebuilder.MarkLayoutForRebuild(messageText.rectTransform);

        messageImage.transform.parent.gameObject.SetActive( messageSprite != null );
        messageImage.sprite = messageSprite;

        Vector3 targetScale = canvas.localScale;

        float timer = 0.0f;
        while( timer < 0.4f ){
            timer += Time.deltaTime;
            float alpha = Mathf.Min(1.0f,timer/0.4f);
             
            UpdateMessageContainerPosition( parent, localPos );
            canvas.localScale = targetScale*Mathf.Lerp( 1.0f, 2.0f, 1.0f-Mathf.Abs( 0.5f-alpha )*2.0f );
            
            messageFrameImage.color = new Color( 1, 1, 1, alpha );
            messageFrameImage.transform.rotation = Quaternion.LookRotation( messageFrameImage.transform.position-GameDirector.instance.mainCamera.transform.position, Vector3.up );
            messageText.color = new Color( 0, 0, 0, alpha );
            yield return null;
        }
        //begin countdown timer when sign is in view
        while( true ){
            UpdateMessageContainerPosition( parent, localPos );
            float bearing = Tools.Bearing( GameDirector.instance.mainCamera.transform, canvas.transform.position );
            if( Mathf.Abs( bearing ) < 10.0f ){
                break;
            }
            UpdateMessageContainerPosition( parent, localPos );
            yield return null;
        }
        //hold for a readable length
        float length = 4.5f+message.Length*0.14f;
        while( timer < length ){
            timer += Time.deltaTime;
            UpdateMessageContainerPosition( parent, localPos );
            messageFrameImage.transform.rotation = Quaternion.LookRotation( messageFrameImage.transform.position-GameDirector.instance.mainCamera.transform.position, Vector3.up );
            yield return null;
        }
        StopHint();
    }

    private IEnumerator ExitHintCoroutine(){
        yield return new WaitForSeconds(0.5f);
        float timer = 0.0f;
        while( timer < 0.5f ){
            timer += Time.deltaTime;
            float alpha = 1.0f-timer/0.5f;
            messageFrameImage.color = new Color( 1, 1, 1, alpha );
            messageText.color = new Color( 0, 0, 0, alpha );
            yield return null;
        }
        canvas.gameObject.SetActive( false );
        activeMessageCoroutine = null;
    }

    public void StopHint(){
        if( activeMessageCoroutine != null ){
            GameDirector.instance.StopCoroutine( activeMessageCoroutine );
            activeMessageCoroutine = null;
        }
        if( activeExitCoroutine != null ){
            GameDirector.instance.StopCoroutine( activeExitCoroutine );
            activeExitCoroutine = null;
        }
        activeMessageCoroutine = GameDirector.instance.StartCoroutine( ExitHintCoroutine() );
    }

    private Vector3 GetFirstChildPosition( Transform transform ){
        if( transform.childCount == 0 ){
            Debug.LogError("ERROR Collider for hint needs a child for hint placement!");
            return Vector3.zero;
        }
        return transform.GetChild(0).transform.position;
    }

    private List<string> hintsPlayed = new List<string>();

    public void DisplayHint( Transform parent, Vector3 localPos, string text, Sprite sprite = null, float scale=1.0f, Item source=null, ExitCoroutineFunction exitFunction=null ){
        
        //play Hint only once
        if( hintsPlayed.Contains(text) ){
            return;
        }
        hintsPlayed.Add(text);
        
        canvas.localScale = Vector3.one*scale*globalHintScale;
        
        if( activeMessageCoroutine != null ){
            GameDirector.instance.StopCoroutine( activeMessageCoroutine );
        }
        activeMessageCoroutine = GameDirector.instance.StartCoroutine( MessageCoroutine( parent, localPos, text, sprite ) );

        if( activeExitCoroutine != null ){
            GameDirector.instance.StopCoroutine( activeExitCoroutine );
            activeExitCoroutine = null;
        }
        if( exitFunction != null ){
            activeExitCoroutine = GameDirector.instance.StartCoroutine( exitFunction( source ) );
        }
    }
}

}