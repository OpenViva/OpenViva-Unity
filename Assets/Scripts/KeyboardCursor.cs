using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

public class KeyboardCursor : MonoBehaviour{

    [SerializeField]
    private Texture2D idleCursor;
    [SerializeField]
    private Texture2D grabCursor;
    [SerializeField]
    private Texture2D combineCursor;
    [SerializeField]
    private MeshRenderer cursorMeshRenderer;

    private Grabbable lastVisibleGrabbable;


    private void Awake(){
        if( UI.main ) UI.main.onUIToggled += AdjustFromUI;
        AdjustFromUI( false );
        cursorMeshRenderer.transform.parent.localScale = Vector3.one*16f/Screen.height*0.4f;

        VivaPlayer.user.onPostCharacterChange += OnCharacterChange;
        SetCursorTexture( idleCursor );
    }

    private void AdjustFromUI( bool uiActive ){
        enabled = !uiActive;
    }

    private void OnDestroy(){
        VivaPlayer.user.onPostCharacterChange -= OnCharacterChange;
        OnCharacterChange( VivaPlayer.user.character, null );
        if( UI.main ) UI.main.onUIToggled -= AdjustFromUI;
    }

    private void OnGrabbed( GrabContext grabContext ){
        var keyboardControls = VivaPlayer.user.controls as KeyboardCharacterControls;
        if( keyboardControls.FindAvailableInteraction() != null ){
            SetCursorTexture( combineCursor );
            HintMessage.AttemptHint( HintMessage.Hints.COMBINE_ITEMS, "Your cursor will indicate that both items can be combined.\nHold left or right mouse to combine.", "Some items can be combined with physical actions", GetVRHintPosition() );
        }
    }

    private Vector3 GetVRHintPosition(){
        return VivaPlayer.user.transform.position+Tools.FlatForward( VivaPlayer.user.camera.transform.forward );
    }

    private void OnReleased( GrabContext grabContext ){
        SetCursorTexture( idleCursor );
    }

    private void OnCharacterChange( Character oldCharacter, Character newCharacter ){
        if( oldCharacter ){
            oldCharacter.onReset -= Reset;
        }
        if( newCharacter ){
            newCharacter.onReset += Reset;
        }
    }

    private void Reset(){
        var character = VivaPlayer.user.character;
        if( !character ) return;
        character.biped.rightHandGrabber.onGrabbed._InternalAddListener( OnGrabbed );
        character.biped.leftHandGrabber.onGrabbed._InternalAddListener( OnGrabbed );
        character.biped.rightHandGrabber.onReleased._InternalAddListener( OnReleased );
        character.biped.leftHandGrabber.onReleased._InternalAddListener( OnReleased );
    }

    private void OnEnable(){
        cursorMeshRenderer.enabled = true;
    }
    
    private void OnDisable(){
        cursorMeshRenderer.enabled = false;
    }

    private void FixedUpdate(){
        
        Grabbable nextVisibleGrabbable = null;
        float leastSqDist = Mathf.Infinity;
        int hits = Physics.SphereCastNonAlloc( transform.position, 0.04f, transform.forward, WorldUtil.bigHitInfoResult, 1f, WorldUtil.grabbablesMask, QueryTriggerInteraction.Collide );
        for( int i=0; i<hits; i++ ){
            var hitInfo = WorldUtil.bigHitInfoResult[i];
            var candidateGrabbable = hitInfo.collider.GetComponent<Grabbable>();
            if( !candidateGrabbable ){
                var candidateSource = Util.GetCharacter( hitInfo.rigidbody );
                if( candidateSource && candidateSource.isBiped ){
                    if( candidateSource.biped.rightArm.rigidBody == hitInfo.rigidbody || candidateSource.biped.rightHand.rigidBody == hitInfo.rigidbody ){
                        candidateGrabbable = candidateSource.biped.rightHandGrabber.GetRandomGrabbable();
                    }else if( candidateSource.biped.leftArm.rigidBody == hitInfo.rigidbody || candidateSource.biped.leftHand.rigidBody == hitInfo.rigidbody ){
                        candidateGrabbable = candidateSource.biped.leftHandGrabber.GetRandomGrabbable();
                    }
                }
            }
            if( candidateGrabbable && candidateGrabbable.enabled ){
                var sqDist = Vector3.SqrMagnitude( transform.position-hitInfo.point );
                if( sqDist < leastSqDist ){
                    leastSqDist = sqDist;
                    nextVisibleGrabbable = candidateGrabbable;
                }
            }
        }
        if( nextVisibleGrabbable ){
            Debug.DrawLine( transform.position, WorldUtil.hitInfo.point, Color.green, Time.fixedDeltaTime );
        }else{
            Debug.DrawLine( transform.position, transform.position+transform.forward*1f, Color.red, Time.fixedDeltaTime );
        }
        SetNextGrabbableInVision( nextVisibleGrabbable );
    }

    private void SetNextGrabbableInVision( Grabbable grabbable ){

        VivaPlayer.user.character.biped.rightHandGrabber.RemoveNearbyGrabbable( lastVisibleGrabbable );
        VivaPlayer.user.character.biped.leftHandGrabber.RemoveNearbyGrabbable( lastVisibleGrabbable );

        if( grabbable ){
            VivaPlayer.user.character.biped.rightHandGrabber.AddNearbyGrabbable( grabbable );
            VivaPlayer.user.character.biped.leftHandGrabber.AddNearbyGrabbable( grabbable );

            if( cursorMeshRenderer.material.mainTexture == idleCursor ){
                SetCursorTexture( grabCursor );
            }
        }else{
            if( cursorMeshRenderer.material.mainTexture == grabCursor ){
                SetCursorTexture( idleCursor );
            }
        }
        lastVisibleGrabbable = grabbable;
    }

    private void SetCursorTexture( Texture2D texture ){
        if( !cursorMeshRenderer ) return;
        cursorMeshRenderer.material.mainTexture = texture;
        cursorMeshRenderer.transform.localScale = Vector3.one*( (float)texture.width/32f );
    }
}

}