using UnityEngine;
using UnityEditor;
using System.Collections;
using UnityEngine.UI;

using System.Collections.Generic;
 

namespace viva{

public enum Gesture{
    FOLLOW,
    PRESENT_START,
    PRESENT_END,
    HELLO,
    MECHANISM,
    STOP
}

public partial class Gestures : MonoBehaviour { 

    [Header("Gestures")]
    [SerializeField]
    private GameObject gestureDisplay;
    [SerializeField]
    private Material gestureDisplayMat;
    [SerializeField]
    private Texture2D[] gestureTextures;
    [SerializeField]
    public AudioClip[] gestureSounds = new AudioClip[ System.Enum.GetValues(typeof(Gesture)).Length ];
    [SerializeField]
    private GestureHand[] gestureHands = new GestureHand[2];
	public GestureHand rightGestureHand { get{ return gestureHands[0]; } }
	public GestureHand leftGestureHand { get{ return gestureHands[1]; } }
    [SerializeField]
    private VivaPlayer player;
    [SerializeField]
    private AudioClip selectionIn;
    [SerializeField]
    private AudioClip selectionOut;

    private Coroutine gestureDisplayCoroutine = null;
    private Coroutine pointCoroutine = null;
    private Outline.Entry pointOutline = null;
    private List<Outline.Entry> characterSelections = new List<Outline.Entry>();
    public int characterSelectionCount { get{ return characterSelections.Count; } }


    public Character GetSelectedCharacter( int index ){
        return characterSelections[ index ].source as Character;
    }

    private void Awake(){
        rightGestureHand.gestures = this;
        leftGestureHand.gestures = this;
    }

    public void _InternalReset(){
        UnbindFromTrackedHands();
        StopPointing();
        ClearCharacterSelection();
    }

    public void BindToTrackedHands( Transform rightHand, Transform leftHand, bool enableGestureDetection ){
        rightGestureHand.target.SetParent( rightHand, false );
        leftGestureHand.target.SetParent( leftHand, false );
        enabled = enableGestureDetection;
    }

    public void UnbindFromTrackedHands(){
        rightGestureHand.target.SetParent( transform, false );
        leftGestureHand.target.SetParent( transform, false );
        gestureDisplay.transform.SetParent( transform );
    }

    public void LateUpdate(){
        rightGestureHand.CheckDetection();
        leftGestureHand.CheckDetection();
    }
    public void FireGesture( Gesture gesture, bool rightHand, bool silent=false ){
        FireGesture( gesture, rightHand ? rightGestureHand : leftGestureHand, silent );
    }

    public void FireGesture( Gesture gesture, GestureHand gestureHand, bool silent=false ){

        switch( gesture ){
        case Gesture.HELLO:
            SendGestureToVisibleCharacters( gestureHand, "hello" );
            break;
        case Gesture.FOLLOW:
            SendGestureToVisibleCharacters( gestureHand, "follow" );
            break;
        case Gesture.STOP:
            SendGestureToVisibleCharacters( gestureHand, "stop" );
            break;
        case Gesture.PRESENT_START:
            if( !SetAttributeToHeldObjects( gestureHand, Item.offerAttribute, true ) ){
                SendGestureToVisibleCharacters( gestureHand, "give" );
            }else{
                SendGestureToVisibleCharacters( gestureHand, "take" );
            }
            break;
        case Gesture.PRESENT_END:
            SetAttributeToHeldObjects( gestureHand, Item.offerAttribute, false );
            break;
        }
        if( silent ) return;
        if( player.isUsingKeyboard ){
            PlayDisplayCoroutine( new Vector3( 0, -0.175f, 1f ), Camera.main.transform, gesture, 0.6f );
        }else{
            var rigidBody = gestureHand==rightGestureHand ? player.character.biped.rightHand.rigidBody : player.character.biped.leftHand.rigidBody;
            PlayDisplayCoroutine( rigidBody.centerOfMass/rigidBody.transform.lossyScale.x, rigidBody.transform, gesture );
        }
    }

    private bool SetAttributeToHeldObjects( GestureHand gestureHand, string attributeName, bool add ){
        bool changedAttributes = false;
        var grabber = gestureHand == rightGestureHand ? player.character.biped.rightHandGrabber : player.character.biped.leftHandGrabber;
        for( int i=0; i<grabber.contextCount; i++ ){
            var context = grabber.GetGrabContext(i);
            if( !context.grabbable || !context.grabbable.parent ) continue;

            var attrib = context.grabbable.parent.FindAttribute( attributeName );
            if( add ){
                if( attrib==null ) context.grabbable.parent.AddAttribute( attributeName );
            }else{
                context.grabbable.parent.RemoveAttribute( attributeName );
            }
            if( context.grabbable.parentItem ) Outline.StartOutlining( context.grabbable.parentItem.model, null, Color.green, Outline.Flash );
            if( context.grabbable.parentCharacter ) Outline.StartOutlining( context.grabbable.parentCharacter.model, null, Color.green, Outline.Flash );
            changedAttributes = true;
        }
        return changedAttributes;
    }
    
    public T FindSpherecastVivaInstance<T>( Vector3 rayStart, Vector3 rayForward, float rayLength, bool includeItems, BoolReturnVivaInstanceFunc validate=null ) where T:VivaInstance{
        var rayEnd = rayStart+rayForward*rayLength;
        var mask = WorldUtil.characterCollisionsMask|WorldUtil.defaultMask|WorldUtil.itemsStaticMask;
        if( includeItems ) mask |= WorldUtil.itemsMask;

        var rayCastHits = Physics.SphereCastAll( rayStart, 0.25f, rayEnd-rayStart, rayLength, mask, QueryTriggerInteraction.Ignore );
        float shortestDistSq = Mathf.Infinity;
        T shortest = null;
        foreach( var raycast in rayCastHits ){
            var result = raycast.collider.GetComponentInParent<T>();
            if( result && result != shortest ){
                var distSq = Vector3.SqrMagnitude( raycast.point-rayStart );
                if( distSq < shortestDistSq ){
                    if( validate == null || validate( result ) ){
                        shortestDistSq = distSq;
                        shortest = result;
                    }
                }
            }
        }
        return shortest;
    }

    private void SendGestureToVisibleCharacters( GestureHand gestureHand, string gesture ){
        
        var rayStart = gestureHand.target.position;
        //fire horizontal rays from caller
        int rayCount = 8;
        float yawAngle = 40.0f;
        float yawStep = yawAngle/8;
        var rayLength = 10.0f;
        float currentYaw = yawStep*rayCount/-2;
        var rayForward = player.camera.transform.forward;
        
        var seen = new List<Character>();

        for( int i=0; i<rayCount; i++ ){
            currentYaw += yawStep;

            var currentRayForward = Quaternion.Euler( 0, currentYaw, 0 )*rayForward;
            var character = FindSpherecastVivaInstance<Character>( rayStart, currentRayForward, rayLength, false, delegate( VivaInstance instance ){
                //ignore character being grabbed by player
                var candidate = instance as Character;
                if( candidate.isAnimal && candidate.IsGrabbedByAnyCharacter() ) return false;

                return candidate.GetGrabContexts( player.character ).Count == 0;
            } );
            if( character != null && !seen.Contains( character ) ) seen.Add( character );
        }

        foreach( var character in seen ){
            character.onGesture.Invoke( gesture, player.character );
            Outline.StartOutlining( character.model, character, Color.white, Outline.Flash );
        }
        player.character.onSendGesture.Invoke( gesture );
    }

    private void UpdateGestureDisplayRotation( float mult=1.0f ){
        Vector3 delta = Camera.main.transform.position-gestureDisplay.transform.position;
        float scale = gestureDisplay.transform.parent ? gestureDisplay.transform.parent.lossyScale.x : 1.0f;
        gestureDisplay.transform.localScale = Vector3.one*delta.magnitude*mult*0.2f/scale;
        gestureDisplay.transform.rotation = Tools.SafeLookRotation( Camera.main.transform.position, gestureDisplay.transform.position );
    }
    
    public void StartPointing( Transform pointer, Vector3 localDir ){
        StopPointing();
        pointCoroutine = Viva.main.StartCoroutine( Point( pointer, localDir ) );
    }

    public void StopPointing(){
        if( pointCoroutine != null ){
            Viva.main.StopCoroutine( pointCoroutine );
            pointCoroutine = null;

            var lastPointOutline = pointOutline;
            StopPointOutline();

            if( lastPointOutline != null && lastPointOutline.source ){
                var character = lastPointOutline.source as Character;
                if( character ){
                    ToggleCharacterSelection( character );
                }else if( characterSelectionCount > 0 ){
                    var item = lastPointOutline.source as Item;
                    if( item ){
                        item.OpenDialogForCommand( "Do what?", false, true, "GetDialogOptions" );
                        item.onSelected.Invoke();
                    }
                }
            }
        }else{
            StopPointOutline();
        }
    }

    private void ToggleCharacterSelection( Character character ){
        if( character == null ) return;

        bool found = false;
        for( int i=0; i<characterSelections.Count; i++ ){
            var characterSelection = characterSelections[i];
            if( characterSelection.source == character ){
                found = true;
                Outline.StopOutlining( characterSelection );
                characterSelections.RemoveAt(i);
                Sound.main.PlayGlobalOneShot( selectionOut );
                break;
            }
        }
        if( !found ){
            characterSelections.Add( Outline.StartOutlining( character.model, character, new Color( 0.5f, 1, 0.5f, 1 ), Outline.Constant ) );
            Sound.main.PlayGlobalOneShot( selectionIn );
            character.onSelected.Invoke();
        }
    }

    public void ClearCharacterSelection(){
        foreach( var characterSelection in characterSelections ){
            Outline.StopOutlining( characterSelection );
        }
        characterSelections.Clear();
    }

    private void StopPointOutline(){
        if( pointOutline != null ){
            Outline.StopOutlining( pointOutline );
            pointOutline = null;
        }
    }

    private void SetPointOutline( Model model, VivaInstance source ){
        if( pointOutline != null && pointOutline.model == model ) return;
        StopPointOutline();
        pointOutline = Outline.StartOutlining( model, source, Color.green, null );
    }

    private IEnumerator Point( Transform pointer, Vector3 localDir ){

        yield return new WaitForSeconds( 0.25f );

        VivaInstance firstChoice = null;
        float timer = 0;
        while( true ){
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();  //wait twice for performance?
            if( !pointer ) yield break;
            
            if( firstChoice == null ){
                firstChoice = FindSpherecastVivaInstance<VivaInstance>( pointer.position, pointer.TransformDirection( localDir ), 4.0f, true );
                timer = 0;
                if( firstChoice ){
                    var item = firstChoice as Item;
                    if( item ){
                        if( characterSelectionCount == 0 ){
                            firstChoice = null;
                            continue;
                        }
                        SetPointOutline( item.model, item );
                    }else{
                        var character = firstChoice as Character;
                        if( character && character != VivaPlayer.user.character ){
                            SetPointOutline( character.model, character );
                        }else{
                            continue;
                        }
                    }
                }else{
                    continue;
                }
            }

            var nextChoice = FindSpherecastVivaInstance<VivaInstance>( pointer.position, pointer.TransformDirection( localDir ), 2.5f, true );
            if( nextChoice != firstChoice ){
                StopPointOutline();
                firstChoice = null;
                continue;
            }

            timer += Time.deltaTime;
            float ratio = 1.0f-Mathf.Clamp01( timer/0.2f );
            ratio = 1.0f-Mathf.Pow(ratio,4);
            pointOutline?.SetOutline( new Color( 0.4f*ratio, 1, 0.4f*ratio, 1.0f ), ratio*10.0f );
        }
    }

    public void SetupDisplayTexture( Gesture gesture ){
        gestureDisplayMat.mainTexture = gestureTextures[ (int)gesture ];
    }
    
    public void PlayDisplayCoroutine( Vector3 localPos, Transform source, Gesture gesture, float scale=1f ){
        SetupDisplayTexture( gesture );
        if( gestureDisplayMat.mainTexture == null ){
            return;
        }
        
        if( gestureDisplayCoroutine != null ){
            Viva.main.StopCoroutine( gestureDisplayCoroutine );
        }
        Sound.main.PlayGlobalOneShot( gestureSounds[ (int)gesture ] );

        gestureDisplay.transform.SetParent( source, true );
        gestureDisplay.transform.localPosition = localPos;

        gestureDisplayCoroutine = Viva.main.StartCoroutine( DisplayGesture( scale ) );
    }

    private IEnumerator DisplayGesture( float scale=1f ){
        gestureDisplay.SetActive(true);
        float maxTime = 0.3f;
        float timer = 0.0f;
        while( timer < maxTime ){
            timer += Time.deltaTime;
            float ratio = Mathf.Clamp01( timer/maxTime );
            gestureDisplayMat.color = new Color( 1, 1, 1, Tools.EaseInOutCubic(ratio) );
            UpdateGestureDisplayRotation( ( Tools.EaseInQuad(1.0f-ratio)*0.5f+1.0f )*scale );
            yield return new WaitForEndOfFrame();
        }
        timer = 0.5f;
        while( timer > 0.0f ){
            timer -= Time.deltaTime;
            UpdateGestureDisplayRotation( scale );
            yield return new WaitForEndOfFrame();
        }
        maxTime = 0.3f;
        timer = 0.0f;
        while( timer < maxTime ){
            timer += Time.deltaTime;
            float ratio = timer/maxTime;
            gestureDisplayMat.color = new Color( 1, 1, 1, 1.0f-ratio );
            UpdateGestureDisplayRotation(scale );
            yield return new WaitForEndOfFrame();
        }
        gestureDisplay.SetActive(false);
        yield return null;
    }

    public void StopDisplayCoroutine(){
        
        if( gestureDisplayCoroutine != null ){
            Viva.main.StopCoroutine(gestureDisplayCoroutine);
            gestureDisplayCoroutine = null;
        }
        gestureDisplay.SetActive( false );
    }	
}

}