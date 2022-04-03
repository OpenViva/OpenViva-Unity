using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;


namespace viva{

public delegate Vector3 Vec3ReturnFunc(); 
public delegate void BoolCallback( bool enabled );

public abstract class UI: MonoBehaviour{

    [SerializeField]
    private string defaultTab;
    [SerializeField]
    private bool allowScreenSpaceUI = true;
    [SerializeField]
    protected Canvas canvas;
    [SerializeField]
    private MessageDialog m_messageDialog;
    public MessageDialog messageDialog { get{ return m_messageDialog; } }
    [SerializeField]
    private GameObject keyboardNav;
    [SerializeField]
    private GameObject vrNav;
    [SerializeField]
    private GameObject tabs;
    [SerializeField]
    private GameObject m_blockRaycast;
    public GameObject blockRaycast { get{ return m_blockRaycast; } }
    [SerializeField]
    private SceneSwitcher sceneSwitcherPrefab;

    public static UI main;
    public string lastTab { get; private set; } = null;
    public BoolCallback onUIToggled;
    private bool userIsUsingKeyboard { get{ return VivaPlayer.user ? VivaPlayer.user.isUsingKeyboard : true; } }
    public bool isUIActive { get{ return canvas.gameObject.activeSelf; } }
    private Vector3 worldScale;
    private string selectedSessionFullpath = null;
    private bool loadingScene = false;


    private void SetTabActive( string tabName, bool enable ){
        if( tabName == null ) return;
        for( int i=0; i<tabs.transform.childCount; i++ ){
            var tab = tabs.transform.GetChild(i);
            if( tab.name == tabName ){
                tab.gameObject.SetActive( enable );
                break;
            }
        }
    }

    public void CreateNewSession(){
        LoadSession( "default" );
    }

    public void ReturnToTitleScreen(){
        LoadSession( "main", Profile.root );
    }

    public void SelectSession( string session, string subfolder=null ){
        selectedSessionFullpath = System.IO.Path.GetFullPath( (subfolder==null ? SceneSettings.root : subfolder) +"/"+ session+".viva" );
        Sound.main.PlayGlobalUISound( UISound.BUTTON2 );
    }

    public void LoadSession( string session, string subfolder=null ){
        SelectSession( session, subfolder );
        LoadSelectedSession();
    }

    public void LoadSelectedSession(){
        if( loadingScene ) return;
        if( selectedSessionFullpath == null ) return;
        if( selectedSessionFullpath == null ) return;

        Debug.Log("Loading session \""+selectedSessionFullpath+"\"");
        var request = ImportRequest.CreateRequest( selectedSessionFullpath ) as SceneRequest;
        if( request == null ) return;
        loadingScene = true;

        request.Import();
        SpawnScene( request );
    }

    private void SpawnScene( SceneRequest request ){
        if( request.imported ){
            request.sceneSwitcher = GameObject.Instantiate( sceneSwitcherPrefab );
            GameObject.DontDestroyOnLoad( request.sceneSwitcher );
            request._InternalSpawnUnlinked( false, new SpawnProgress( delegate{ loadingScene = false; } ) );
        }
    }

    public void SetCanvasCamera( Camera camera ){
        canvas.worldCamera = camera;
    }

    public void SetCanvasMode( bool keyboardMode ){
        if( keyboardMode && allowScreenSpaceUI ){
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
        }else{
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.transform.localPosition = Vector3.zero;
            canvas.transform.localRotation = Quaternion.identity;
            canvas.transform.localScale = (VivaPlayer.user && VivaPlayer.user.isUsingKeyboard ) ? worldScale*0.75f : worldScale;
            ( canvas.transform as RectTransform ).sizeDelta = Vector2.one*1024;
            RepositionForVR();
        }
    }

    protected virtual void Awake(){
        main = this;
        worldScale = canvas.transform.localScale;
    }

    private void Start(){
        SetTabActive( defaultTab, true );
        lastTab = defaultTab;

        onUIToggled += delegate( bool enabled ){
            if( VivaPlayer.user ) VivaPlayer.user.movement.enabled = !enabled;
        };
    }

    public virtual void OpenTab( string newTab ){
        if( newTab == null ) return; 
        SetEnableUI( true );

        SetTabActive( lastTab, false );
        SetTabActive( newTab, true );
        lastTab = newTab;
        Sound.main.PlayGlobalUISound( UISound.BUTTON1 );
    }
    
    public void OpenLastTab(){
        main.OpenTab( main.lastTab );
    }
    public void ToggleUI(){
        SetEnableUI( !canvas.gameObject.activeSelf );
    }

    public void SetEnableUI( bool enable ){
        canvas.gameObject.SetActive( enable );
        onUIToggled?.Invoke( enable );
        SetEnableNav( enable );
        InputManager.ToggleForCursorLockCounter( this, enable );
    }

    private void OnDestroy(){
        InputManager.ToggleForCursorLockCounter( this, false );
    }

    public void SetEnableNav( bool enable ){
        if( userIsUsingKeyboard ){
            if( enable ){
                if( keyboardNav ) keyboardNav.SetActive( enable );
                if( vrNav ) vrNav.SetActive( !enable );
            }else{
                if( keyboardNav ) keyboardNav.SetActive( false );
                if( vrNav ) vrNav.SetActive( false );
            }
        }else{
            if( enable ){
                if( keyboardNav ) keyboardNav.SetActive( !enable );
                if( vrNav ) vrNav.SetActive( enable );
            }else{
                if( keyboardNav ) keyboardNav.SetActive( false );
                if( vrNav ) vrNav.SetActive( false );
            }
            RepositionForVR();
        }
    }
    
    public void RepositionForVR(){
        if( userIsUsingKeyboard || !VivaPlayer.user || !VivaPlayer.user.character ) return;

        transform.rotation = Quaternion.LookRotation( Tools.FlatForward( Camera.main.transform.forward ), Vector3.up );

        float relativeUserSize = VivaPlayer.user.character.model.bipedProfile.hipHeight*VivaPlayer.user.character.scale;

        // transform.localScale = Vector3.one*0.001f*relativeUserSize;
        var headFloorPos = Camera.main.transform.position+Vector3.down*0.25f;
        transform.position = headFloorPos+transform.forward*1.1f;
        Tools.DrawCross( headFloorPos, Color.green, 1, 4 );
        Debug.DrawLine( Vector3.zero, Camera.main.transform.forward, Color.cyan, 4 );
    }
}

}