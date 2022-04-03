using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.Management;
using UnityEngine.XR;
using UnityEngine.InputSystem.XR;



namespace viva{

public abstract class CameraControls : MonoBehaviour{

    protected VivaPlayer player;
    [SerializeField]
    private Camera m_camera;
    public new Camera camera { get{ return m_camera; } }
    private Outline.Entry grabOutlineEntry;

    
    public void Initialize( VivaPlayer _player ){
        player = _player;
        
        player.onPreCharacterChange += OnCharacterChange;
        BindCharacter( player.character );
    }

    protected abstract void BindCharacter( Character character );
    protected abstract void UnbindCharacter( Character character );

    private void OnDestroy(){
        if( player ){
            player.onPreCharacterChange -= OnCharacterChange;
            UnbindCharacter( player.character );
        }
        player = null;
    }
    
    private void OnCharacterChange( Character oldCharacter, Character newCharacter ){
        UnbindCharacter( oldCharacter );
        BindCharacter( newCharacter );
    }

    public abstract void InitializeInstanceTransform( VivaInstance instance );

    public virtual bool Allowed(){ return true; }

    public void BindGrabUtilities( Character character ){
        if( character == null ) return;
        character.biped.rightHandGrabber.onGrabbableNearby._InternalAddListener( OnCheckStartItemOutline );
        character.biped.rightHandGrabber.onGrabbableTooFar._InternalAddListener( OnCheckStopItemOutline );
    }

    public void UnbindGrabUtilities( Character character ){
        if( character == null ) return;
        character.biped.rightHandGrabber.onGrabbableNearby._InternalRemoveListener( OnCheckStartItemOutline );
        character.biped.rightHandGrabber.onGrabbableTooFar._InternalRemoveListener( OnCheckStopItemOutline );
    }

    private void OnCheckStartItemOutline( Grabbable grabbable ){
        if( grabbable == null ) return;
        if( !grabbable.parentItem ) return;

        Outline.StopOutlining( grabOutlineEntry );
        grabOutlineEntry = Outline.StartOutlining( grabbable.parentItem.model, grabbable.parentItem, Color.cyan, Outline.Constant );
    }

    private void OnCheckStopItemOutline( Grabbable grabbable ){
        if( grabbable == null ) return;
        if( !grabbable.parentItem ) return;

        Outline.StopOutlining( grabOutlineEntry );
    }
}

}