using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.UI;


namespace viva{

public enum GameUITab{
    CREATE,
    MOVE,
    CHARACTERS,
    ITEMS,
    SETTINGS
}

public class GameUI : UI{

    public delegate void BoolCallback( bool enabled );

    [SerializeField]
    private RagdollEditor m_ragdollEditor;
    public RagdollEditor ragdollEditor { get{ return m_ragdollEditor; } }
    [SerializeField]
    private CreateMenu m_createMenu;
    public CreateMenu createMenu { get{ return m_createMenu; } }
    [SerializeField]
    private LibraryExplorer m_libraryExplorer;
    public LibraryExplorer libraryExplorer { get{ return m_libraryExplorer; } }
    [SerializeField]
    private ActiveMenu m_activeMenu;
    public ActiveMenu activeMenu { get{ return m_activeMenu; } }
    [SerializeField]
    private VRSettings m_vrSettings;
    public VRSettings vrSettings { get{ return m_vrSettings; } }
    [SerializeField]
    private Debugger m_debugger;
    public Debugger debugger { get{ return m_debugger; } }
    [SerializeField]
    private Image tabFadeImage;
    [SerializeField]
    private GameObject canvasHighlightContainer;
    [SerializeField]
    private Collider m_canvasCollider;
    public Collider canvasCollider { get{ return m_canvasCollider; } }
    [SerializeField]
    private GameObject[] playModeOnly;

    public new static GameUI main { get; private set; }

    private Coroutine trackCoroutine = null;
    public bool isInEditMode { get{ return true; } }
    private bool userIsUsingKeyboard { get{ return VivaPlayer.user ? VivaPlayer.user.isUsingKeyboard : true; } }


    protected override void Awake(){
        main = this;
        base.Awake();

        TogglePlayMode();

        Character.instances.onNewInstance += activeMenu.Add;
        Item.instances.onNewInstance += activeMenu.Add;
    }

    protected void OnDestroy(){
        
        Character.instances.onNewInstance -= activeMenu.Add;
        Item.instances.onNewInstance -= activeMenu.Add;
    }

    public void TrackTarget( NullableVec3ReturnFunc targetFunc ){
        StopTrackingTarget();
        SetCanvasMode( false );

        trackCoroutine = Viva.main.StartCoroutine( ExecuteTrackTarget ( targetFunc ) );
    }

    private IEnumerator ExecuteTrackTarget( NullableVec3ReturnFunc targetFunc ){

        while( true ){
            var readPos = targetFunc();
            if( !readPos.HasValue ){
                StopTrackingTarget();
                yield break;
            }
            var lookQuat = Quaternion.LookRotation( Camera.main.transform.forward, Vector3.up );
            transform.rotation = lookQuat;

            transform.position = readPos.Value;
            yield return null;
        }
    }

    public void StopTrackingTarget(){
        if( trackCoroutine != null ){
            Viva.main.StopCoroutine( trackCoroutine );
            trackCoroutine = null;
        }
        SetCanvasMode( userIsUsingKeyboard );
    }

    public void ToggleDebugger(){
        debugger.gameObject.SetActive( !debugger.gameObject.activeSelf );
    }

    public void SetHideDecorations( bool hide ){
        tabFadeImage.enabled = !hide;
        libraryExplorer.libraryScrollImage.enabled = !hide;
        canvasHighlightContainer.SetActive( !hide );
    }

    public void TogglePlayMode(){
        var isOn = !playModeOnly[0].activeSelf;
        foreach( var target in playModeOnly ) target.SetActive( isOn );
    }

    public void ToggleVR(){
        VivaPlayer.user?.ToggleVR();
    }

    public override void OpenTab( string tab ){
        if( tab == "Items" ){
            tab = "Library";
            libraryExplorer.SetDefaultPrepare( delegate{ libraryExplorer.DisplaySelection(
                "Item Library",
                libraryExplorer.ExpandDialogOptions( ImportRequestType.ITEM, null ),
                libraryExplorer.LoadAndSpawnDialogOption,
                null,
                null,
                delegate( SpawnableImportRequest request ){
                    var itemRequest = request as ItemRequest;
                    return itemRequest.itemSettings != null && ( isInEditMode || !itemRequest.itemSettings.hide );
                }
            ); } );
        }else if( tab == "Characters" ){
            tab = "Library";
            libraryExplorer.SetDefaultPrepare( delegate{ libraryExplorer.DisplaySelection(
                "Character Library",
                libraryExplorer.ExpandDialogOptions( ImportRequestType.CHARACTER, null ),
                libraryExplorer.LoadAndSpawnDialogOption,
                null,
                null,
                delegate( SpawnableImportRequest request ){
                    var charRequest = request as CharacterRequest;
                    return isInEditMode || !charRequest.characterSettings.hide;
                }
            ); } );
        }

        base.OpenTab( tab );
    }
}

}