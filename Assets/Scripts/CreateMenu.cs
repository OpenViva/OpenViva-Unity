using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Security.Permissions;
using System.IO.Compression;


namespace viva{
    
public partial class CreateMenu : MonoBehaviour{

    public delegate void ToggleCallback( bool value );
    public delegate string StringReturnCallback( string value );
    public delegate void StringCallback( string value );

    public class OptionInfo{
        public string title;
        public bool on;
        public ToggleCallback onToggle;

        public OptionInfo( string _title, bool _on, ToggleCallback _onToggle ){
            title = _title;
            on = _on;
            onToggle = _onToggle;
        }
    }

    public class MultiChoiceInfo{
        public string label;
        public string[] choices;
        public string current;
        public StringCallback onChoice;

        public MultiChoiceInfo( string _label, string[] _choices, string _current, StringCallback _onChoice ){
            label = _label;
            choices = _choices;
            current = _current;
            onChoice = _onChoice;
        }
    }
    
    public class InputInfo{
        public string title;
        public string initValue;
        public StringReturnCallback onChange;

        public InputInfo( string _title, string _initValue, StringReturnCallback _onChange ){
            title = _title;
            initValue = _initValue;
            onChange = _onChange;
        }
    }

    [System.Serializable]
    public class InputFieldEntry{
        public GameObject container;
        public Text label;
        public InputField field;
    }

    [SerializeField]
    private ScrollViewManager objectHierarchyScroll;
    [SerializeField]
    public Texture2D defaultScriptThumbnail;
    [SerializeField]
    public Texture2D defaultSoundFileThumbnail;
    [SerializeField]
    private Image headerIcon;
    [SerializeField]
    private Text headerTitle;
    [SerializeField]
    private Text headerContent;
    [SerializeField]
    private Text bodyContent;
    [SerializeField]
    public VivaMenuButton editRagdollButton;
    [SerializeField]
    private VivaMenuButton createButton;
    [SerializeField]
    public VivaMenuButton editLogicButton;
    [SerializeField]
    public Button playSoundButton;
    [SerializeField]
    private VivaMenuButton installButton;
    [SerializeField]
    private Button closeButton;
    [SerializeField]
    private GameObject optionTogglePrefab;
    [SerializeField]
    private RectTransform drawerContainer;
    [SerializeField]
    private GameObject inputFieldPrefab;
    [SerializeField]
    private MultiChoice multiChoicePrefab;
    [SerializeField]
    private Button showFileButton;
    [SerializeField]
    private VivaMenuButton shareButton;
    [SerializeField]
    private GameObject shareHint;


    private static readonly int animationWidthID = Shader.PropertyToID("animationWidth");
    private static readonly int timeSpeedID = Shader.PropertyToID("timeSpeed");

    private List<RequestInfo> requestInfos = new List<RequestInfo>();
    private List<Info> infos = new List<Info>();
    private object lastPressedObj = null;
    private VivaEditable lastSelectedVivaObject = null;
    private bool showedShareHint;


    private void OnEnable(){
        if( !FileDragAndDrop.EnableDragAndDrop( delegate( List<string> files, B83.Win32.POINT dropPoint ){
                var assetProcessor = CreateAssetProcessor( ImportRequest.CreateRequests( files ) );
                assetProcessor.ProcessAll();
            } ) ){
            UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, "Drag and Drop error", "COULD NOT INITIALIZE DRAG & DROP HOOKS" );
        }
        foreach( var requestInfo in requestInfos ){
            CreateOnAssetChangeListener( requestInfo.request );
        }
        ClearHeaderBodyTooling();
        DisplayInfoHierarchy();
    }

    private void OnDisable(){
        lastSelectedVivaObject?.OnCreateMenuDeselected();
        FileDragAndDrop.DisableDragAndDrop();
        FileWatcherManager.main.StopWatching();

        lastSelectedVivaObject?.OnCreateMenuDeselected();
        DisplayVivaObjectInfo<VivaObject>( null );
    }

    public AssetProcessor CreateAssetProcessor( List<ImportRequest> requests ){

        for( int i=requests.Count; i-->0; ){
            bool alreadyExists = false;
            foreach( var info in requestInfos ){
                if( info.request.filepath == requests[i].filepath ){
                    alreadyExists = true;
                    break;
                }
            }
            if( alreadyExists ){
                requests.RemoveAt(i);
            }
        }
        
        return new AssetProcessor( requests );
    }

    public RequestInfo AddImportRequest( ImportRequest importRequest ){
        if( importRequest == null ){
            Debugger.LogError("Cannot add null importRequest");
            return null;
        }
        bool alreadyExists = false;
        foreach( var info in requestInfos ){
            if( info.request == importRequest ){
                alreadyExists = true;
                break;
            }
        }
        if( alreadyExists ) return null;
            
        var newRequestInfo = new RequestInfo( importRequest );
        //add in order
        int insertIndex;
        for( insertIndex=0; insertIndex<requestInfos.Count; insertIndex++ ){
            if( requestInfos[insertIndex].request.type >= importRequest.type ){
                break;
            }
        }
        requestInfos.Insert( insertIndex, newRequestInfo );
        var spawnableImportRequest = importRequest as SpawnableImportRequest;
        if( spawnableImportRequest == null ){
            importRequest._internalOnImported += delegate{ OnRequestInfoUpdated( newRequestInfo ); };
            if( importRequest.imported ) OnRequestInfoUpdated( newRequestInfo );
        }else{
            spawnableImportRequest.onAnyFinishedSpawning += delegate{ OnRequestInfoUpdated( newRequestInfo ); };
            spawnableImportRequest.EnableThumbnailGenerationOnEdit();
        }
        importRequest.usage.onDiscarded += delegate{ RemoveImportRequest( importRequest ); };
        CreateOnAssetChangeListener( importRequest );
        DisplayInfoHierarchy();

        return newRequestInfo;
    }

    private void CreateOnAssetChangeListener( ImportRequest importRequest ){
        var folder = Path.GetDirectoryName( importRequest.filepath );
        var fw = FileWatcherManager.main.GetFileWatcher( folder );
        if( fw == null ) return;
        fw.onFileChange -= OnAssetChange;   //remove first to prevent duplicate calls
        fw.onFileChange += OnAssetChange;
    }

    private void OnAssetChange( string filepath ){
        for( int i=requestInfos.Count; i-->0; ){
            var requestInfo = requestInfos[i];
            if( requestInfo.request.filepath == filepath ){
                Debugger.Log("Reimporting "+requestInfo.request.filepath );
                requestInfo.request.Import();
            }
        }
    }

    private void ClearHeaderBodyTooling(){
        headerIcon.sprite = null;
        headerTitle.text = "";
        headerContent.text = "";
        bodyContent.text = "";
    }

    public void DisplayInfoHierarchy(){
        if( requestInfos == null ) return;
        infos.Clear();
        foreach( var requestInfo in requestInfos ){
            infos.AddRange( requestInfo.infoLabels );
        }
        int counter = 0;
        objectHierarchyScroll.SetContentCount( infos.Count, delegate( Transform prefabEntry ){
            Button button = prefabEntry.GetComponent<Button>();
            Text title = prefabEntry.GetChild(0).GetComponent<Text>();

            var info = infos[ counter++ ];
            title.text = info.label;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener( delegate{ OnInfoPressed( info ); } );
        } );
    }

    private void OnRequestInfoUpdated( RequestInfo requestInfo ){
        requestInfo.RefreshInfoLabels();
        var request = requestInfo.request;
        request._InternalOnGenerateThumbnail();
        DisplayInfoHierarchy();

        //find and update info if it exists
        Info info = null;
        foreach( var candidate in infos ){
            if( candidate.source == requestInfo ){
                info = candidate;
                break;
            }
        }
        if( info != null && lastPressedObj == info.source ){
            DisplayImportRequestInfo( request );
        }
    }

    public void Create(){
        UI.main.messageDialog.RequestButtonSelection( "Create...", "Select type", new MessageDialog.ButtonListEntry[]{
            new MessageDialog.ButtonListEntry(
                "Character",
                delegate{ CreateBiped( lastPressedObj as Model ); }
            ),
            new MessageDialog.ButtonListEntry(
                "Animal",
                delegate{ CreateAnimal( lastPressedObj as Model ); }
            ),
            new MessageDialog.ButtonListEntry(
                "Prop",
                delegate{ CreateItem( lastPressedObj as Model ); }
            )
        });
    }

    private void ShowShareHint(){
        if( showedShareHint ) return;
        showedShareHint = true;
        shareHint.SetActive( true );
    }

    public void PlaySound(){
        var soundSettings = lastSelectedVivaObject as SoundSettings;    
        if( soundSettings == null ) return;

        Sound.main.PlayGlobalOneShot( soundSettings.audioClip );
    }

    private void OnInfoPressed( Info info ){
        if( info == null ) return;

        objectHierarchyScroll.GetButtonAt( infos.IndexOf( info ) )?.Select();

        lastPressedObj = info.source;

        VivaEditable targetEditable = null;
        var reqInfo = info.source as RequestInfo;
        if( reqInfo != null ){
            targetEditable = reqInfo.request;
        }else{
            targetEditable = info.source as VivaEditable;
        }

        lastSelectedVivaObject?.OnCreateMenuDeselected();
        targetEditable?.OnCreateMenuSelected();
    }

    public List<Script> FindScripts( List<string> scriptNames ){
        List<Script> scripts = new List<Script>();
        foreach( var requestInfo in requestInfos ){
            if( requestInfo.request.imported && requestInfo.request.type == ImportRequestType.SCRIPT ){
                var script = ( requestInfo.request as ScriptRequest ).script;
                if( scriptNames.Contains( script.name ) ) scripts.Add( script );
            }
        }
        return scripts;
    }

    public List<string> GenerateAllScriptNamesList(){
        List<string> scripts = new List<string>();
        foreach( var requestInfo in requestInfos ){
            if( requestInfo.request.imported && requestInfo.request.type == ImportRequestType.SCRIPT ){
                scripts.Add( ( requestInfo.request as ScriptRequest ).script.name );
            }
        }
        return scripts;
    }

    private void HandleInputFieldFileExists( string fileName ){
        if( System.IO.File.Exists( Viva.contentFolder+"/"+fileName ) ){
            UI.main.messageDialog.SetInputFieldColor( Color.red );
        }else{
            UI.main.messageDialog.SetInputFieldColor( Color.white );
        }
    }
    
    private void CheckInputFileExists( string fileExtension, string title, string instructions, viva.StringCallback onSuccess ){
        UI.main.messageDialog.onInputFieldChange = delegate( string text ){ HandleInputFieldFileExists( text+fileExtension ); };
        UI.main.messageDialog.RequestInput( title, instructions, delegate( string value ){
            if( value.Length == 0 ){
                UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, title, "Invalid name" );
            }else{
                if( System.IO.File.Exists( Viva.contentFolder+"/"+value+fileExtension ) ){
                    UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, title, "File already exists \""+value+"\"");                    
                }else{
                    onSuccess( value );
                }
            }
        });
    }

    private void CreateBiped( Model model ){
        if( model == null ) throw new System.Exception("Cannot create character with null model");
        if( model.bipedProfile == null ){
            GameUI.main.ragdollEditor.EditBipedProfile( model, delegate{
                CreateBiped( model );
            }, null );
            UI.main.messageDialog.Cancel();
            return;
        }
        ShowShareHint(); 

        CheckInputFileExists( ".char", "Create Character", "Enter new character name", delegate( string value ){
            var infoRequest = AddImportRequest( new CharacterRequest( model, value, false ) );
            DisplayInfoHierarchy();
            FindAndDisplay( InfoColor.CHARACTER_REQUEST, infoRequest );
        });
    }

    private void CreateAnimal( Model model ){
        if( model == null ) throw new System.Exception("Cannot create animal with null model");
        if( model.profile == null ){
            AnimalProfile profile = null;
            try{
                profile = new AnimalProfile( model );
            }catch( System.Exception e ){
                Debug.LogError(e);
            }
            if( model.AttemptSetProfile( profile, out string message ) ){
                CreateAnimal( model );
            }
            return;
        }

        CheckInputFileExists( ".char", "Create Animal", "Enter new animal name", delegate( string value ){
            var infoRequest = AddImportRequest( new CharacterRequest( model, value, true ) );
            DisplayInfoHierarchy();
            FindAndDisplay( InfoColor.CHARACTER_REQUEST, infoRequest );
        });
    }

    private void CreateItem( Model model ){
        if( model == null ) throw new System.Exception("Cannot create prop with null model");
        CheckInputFileExists( ".char", "Create Prop", "Enter new prop name", delegate( string value ){
            var infoRequest = AddImportRequest( new ItemRequest( model, value ) );
            DisplayInfoHierarchy();
            FindAndDisplay( InfoColor.ITEM_REQUEST, infoRequest );
        });
    }

    public void FindAndDisplay( InfoColor type, object obj ){
        foreach( var info in infos ){
            if( info.type == type && info.source == obj ){
                OnInfoPressed( info );
                break;
            }
        }
    }

    private void DisplayImportRequestInfo( ImportRequest request ){
        if( request == null ) return;
        if( !request.imported ){
            ClearHeaderBodyTooling();
            return;
        }
        
        DisplayVivaObjectInfo<ImportRequest>( request );
    }

    private void RemoveImportRequest( ImportRequest importRequest ){
        for( int i=0; i<requestInfos.Count; i++ ){
            if( requestInfos[i].request == importRequest ){
                requestInfos.RemoveAt(i);
                break;
            }
        }
        DisplayInfoHierarchy();
        DisplayVivaObjectInfo<ImportRequest>( null );
    }

    private void DisplayThumbnail( Texture2D texture ){
        if( texture != null ){
            headerIcon.sprite = Sprite.Create( texture, new Rect( 0, 0, texture.width, texture.height ), Vector2.zero, 1, 0, SpriteMeshType.FullRect );
        }else{
            headerIcon.sprite = null;
        }
    }

    private void DisplayVivaObjectThumbnail( VivaObject vivaObject ){
        DisplayThumbnail( vivaObject.thumbnail.texture );
    }

    public void DisplayVivaObjectInfo<T>( VivaEditable obj, ImportRequest _internalSourceRequest=null ){
        if( lastSelectedVivaObject != null ){
            var lastThumbnail = lastSelectedVivaObject.thumbnail;
            lastThumbnail.onThumbnailChange._InternalRemoveListener( DisplayVivaObjectThumbnail );
        }

        lastSelectedVivaObject = obj;

        if( obj != null ){
            var thumbnail = obj.thumbnail;
            thumbnail.onThumbnailChange._InternalAddListener( DisplayVivaObjectThumbnail );
            DisplayThumbnail( thumbnail.texture );
            
            headerIcon.material.SetFloat( animationWidthID, thumbnail.animatedFrameWidth );
            headerIcon.material.SetFloat( timeSpeedID, ( thumbnail.animatedFrameWidth*thumbnail.animatedFrameWidth )/thumbnail.animatedDuration );

            headerTitle.text = obj.GetInfoHeaderTitleText();
            headerContent.text = obj.GetInfoHeaderText();
            bodyContent.text = obj.GetInfoBodyContentText();
            
            //show close button if info is a request
            if( _internalSourceRequest != null ){
                closeButton.gameObject.SetActive( true );
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener( _internalSourceRequest.usage.Decrease );
            }else{
                closeButton.gameObject.SetActive( false );
            }

            showFileButton.gameObject.SetActive( AttemptSetupShowFile( obj ) );
            shareButton.SetCallback( obj.OnShare );
            installButton.SetCallback( delegate(){ obj.OnInstall(); } );
        }else{
            headerIcon.sprite = null;
            headerTitle.text = "";
            headerContent.text = "";
            bodyContent.text = "";
            closeButton.gameObject.SetActive( false );
            showFileButton.gameObject.SetActive( false );
        }
        DisplayOptionInfoDrawer( obj );
        DisplayInputInfoDrawer( obj );
        DisplayMultiChoiceInfoDrawer( obj );

        editRagdollButton.gameObject.SetActive( false ); 
        createButton.gameObject.SetActive( false );
        editLogicButton.gameObject.SetActive( false );
        playSoundButton.gameObject.SetActive( false );

        shareButton.gameObject.SetActive( Tools.IsOverriden<T>( "OnShare" ) );
        installButton.gameObject.SetActive( Tools.IsOverriden<T>( "OnInstall" ) );
    }

    public void DisplayEditRagdollButton(){
        editRagdollButton.gameObject.SetActive( true ); 
    }
    
    public void DisplayCreateButton(){
        createButton.gameObject.SetActive( true );
    }

    public void HideCreateButton(){
        createButton.gameObject.SetActive( false );
    }

    public void DisplayEditLogicButton(){
        editLogicButton.gameObject.SetActive( true );
    }
    
    public void DisplayPlaySoundButton(){
        playSoundButton.gameObject.SetActive( true );
    }

    private void DisplayOptionInfoDrawer( VivaEditable obj ){
        
        for( int i=drawerContainer.childCount; i-->0; ){
            var child = drawerContainer.GetChild(i);
            if( child.name.Contains( optionTogglePrefab.name ) ){
                GameObject.DestroyImmediate( child.gameObject );
            }
        }
        if( obj == null ) return;

        var optionInfos = obj.OnCreateMenuOptionInfoDrawer();
        if( optionInfos != null ){
            for( int i=0; i<optionInfos.Count; i++ ){
                var info = optionInfos[i];
                var optionToggle = GameObject.Instantiate( optionTogglePrefab, drawerContainer ).GetComponent<Toggle>();

                Text text = optionToggle.transform.GetChild(0).GetComponent<Text>();
                text.text = info.title;

                optionToggle.onValueChanged.RemoveAllListeners();
                optionToggle.onValueChanged.AddListener( delegate( bool value ){
                    info.onToggle.Invoke( value );
                } );
                optionToggle.isOn = info.on;
            }
        }
    }

    private void DisplayMultiChoiceInfoDrawer( VivaEditable obj ){
        
        for( int i=drawerContainer.childCount; i-->0; ){
            var child = drawerContainer.GetChild(i);
            if( child.name.Contains( multiChoicePrefab.name ) ){
                GameObject.DestroyImmediate( child.gameObject );
            }
        }
        if( obj == null ) return;

        var multiChoices = obj.OnCreateMultiChoiceInfoDrawer();
        if( multiChoices != null ){
            for( int i=0; i<multiChoices.Count; i++ ){
                var info = multiChoices[i];
                var multiChoice = GameObject.Instantiate( multiChoicePrefab, drawerContainer );

                multiChoice.SetChoices( info.label, info.choices, info.current );

                multiChoice.onChoice += delegate( string choice ){
                    info.onChoice.Invoke( choice );
                };
            }
        }
    }

    private void DisplayInputInfoDrawer( VivaEditable obj ){
        
        //delete old inputfield entries
        for( int i=drawerContainer.childCount; i-->0; ){
            var child = drawerContainer.GetChild(i);
            if( child.name.Contains( inputFieldPrefab.name ) ){
                GameObject.DestroyImmediate( child.gameObject );
            }
        }
        if( obj == null ) return;

        var inputInfos = obj.OnCreateMenuInputInfoDrawer();
        if( inputInfos != null ){
            for( int i=0; i<inputInfos.Length; i++ ){
                var info = inputInfos[i];

                var inputField = new InputFieldEntry();
                inputField.container = GameObject.Instantiate( inputFieldPrefab, drawerContainer );
                inputField.label = inputField.container.transform.GetChild(0).GetChild(0).GetComponent<Text>();
                inputField.field = inputField.container.transform.GetChild(1).GetComponent<InputField>();
                
                inputField.container.SetActive( true );

                inputField.label.text = info.title;

                inputField.field.onEndEdit.RemoveAllListeners();
                inputField.field.text = info.initValue;

                inputField.field.onEndEdit.AddListener( delegate( string value ){
                    inputField.field.text =  info.onChange.Invoke( value );
                } );
            }
        }
    }

    private bool AttemptSetupShowFile( VivaEditable obj ){
        if( obj._internalSourceRequest == null ) return false;

        showFileButton.onClick.RemoveAllListeners();
        showFileButton.onClick.AddListener( delegate{
#if UNITY_EDITOR
            var assetProcessor = CreateAssetProcessor( ImportRequest.CreateRequests( new List<string>(){ "C:/Users/Master-Donut/Documents/v/Content/TEMP/gold ship thumbnail.png" } ) );
            assetProcessor.ProcessAll();
#else
            Tools.ExploreFile( obj._internalSourceRequest.filepath );
#endif
        });
        return true;
    }

    public void AttemptSetThumbnail(){
        if( lastSelectedVivaObject == null ) return;
        var selections = new List<MessageDialog.ButtonListEntry>();
        foreach( var requestInfo in requestInfos ){
            var texRequest = requestInfo.request as TextureRequest;
            if( texRequest != null && texRequest.handle != null ){
                selections.Add( new MessageDialog.ButtonListEntry( texRequest.handle._internalTexture?.name, delegate{
                    var setting = lastSelectedVivaObject as InstanceSettings;
                    if( setting == null ) return;
                    setting.thumbnail.texture = texRequest.handle._internalTexture;
                    setting.thumbnail.onThumbnailChange.Invoke( setting );
                    DisplayVivaObjectInfo<SpawnableImportRequest>( lastSelectedVivaObject );
                    UI.main.messageDialog.Cancel();
                } ) );
            }
        }
        
        UI.main.messageDialog.RequestButtonSelection( "Custom Thumbnail", "Select a texture", selections.ToArray() );
    }

    //returns success: true  or  error:false
    public bool ArchiveToShareFolder( string filepath, string sourceFolder ){
        var archiveFilePath = sourceFolder+"/"+filepath.Substring( Viva.contentFolder.Length, filepath.Length-Viva.contentFolder.Length  );
        Tools.EnsureFolder( System.IO.Path.GetDirectoryName( archiveFilePath ) );
        return Tools.ArchiveFile( filepath, archiveFilePath );
    }
}

}