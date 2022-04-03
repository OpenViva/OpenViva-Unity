using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;


namespace viva{

public delegate bool SIRBoolReturnFunc( SpawnableImportRequest request );
public delegate void DialogOptionCallback( DialogOption option, LibraryEntry source );

public enum DialogOptionType{
    ITEM,
    CHARACTER,
    ITEM_ATTRIBUTE_FILTER,
    GENERIC,
    SCENE
}

public struct DialogOption{
    public string value;
    public DialogOptionType type;
    public string textureOverride;

    public DialogOption( string _name, DialogOptionType _type, string _textureOverride=null ){
        value = _name;
        type = _type;
        textureOverride = _textureOverride;
    }
    public override bool Equals( object obj ){
        if (!(obj is DialogOption)) return false;
        var candidate = (DialogOption)obj;
        return ( value == candidate.value && type == candidate.type );
    }
}

public class LibraryExplorer : MonoBehaviour{
    
    private class LibraryInfo{
        public string folder;
        public string ext;
        public ImportRequestType type;
        public DialogOptionType optionType;
    }

    [SerializeField]
    private ScrollViewManager m_scroll;
    public ScrollViewManager scroll { get{ return m_scroll; } }
    [SerializeField]
    public Image libraryScrollImage;
    [SerializeField]
    private Text title;
    [SerializeField]
    private LayoutElement contentLayoutElement;
    [SerializeField]
    private Button helpButton;
    [SerializeField]
    private Sprite defaultSprite;
    [SerializeField]
    public Scrollbar scrollbar;

    private List<DialogOption> options;
    private List<DialogOption> filesLoading = new List<DialogOption>();
    private int fileIndex = 0;
    private InstanceManager instanceManager;
    private GenericCallback onDisable;
    private DialogOptionCallback onSelect;
    private GenericCallback onDefaultPrepare;
    private GenericCallback onOverridePrepare;
    public GenericCallback onEnabled;
    private LibraryInfo info;
    private SIRBoolReturnFunc validateRequestFunc;


    public void SetDefaultPrepare( GenericCallback _onDefaultPrepare ){
        onDefaultPrepare = _onDefaultPrepare;
    }

    public void SetOverridePrepare( GenericCallback _onPrepareNextSelection ){
        onOverridePrepare = _onPrepareNextSelection;
        if( gameObject.activeSelf ) onOverridePrepare?.Invoke();
    }
    
    public void OnEnable(){
        if( onOverridePrepare == null ){
            onDefaultPrepare?.Invoke();
        }else{
            onOverridePrepare();
        }
    }

    private void OnDisable(){
        onOverridePrepare = null;
        onSelect = null;
        onDisable?.Invoke();
        onDisable = null;
    }

    private LibraryInfo CreateInfo( ImportRequestType requestType ){
        switch( requestType ){
        case ImportRequestType.ITEM:
            return new LibraryInfo(){ folder=Item.root, ext=".item", type=requestType, optionType=DialogOptionType.ITEM };
        case ImportRequestType.CHARACTER:
            return new LibraryInfo(){ folder=Character.root, ext=".character", type=requestType, optionType=DialogOptionType.CHARACTER };
        case ImportRequestType.SESSION:
            return new LibraryInfo(){ folder=SceneSettings.root, ext=".viva", type=requestType, optionType=DialogOptionType.SCENE };
        }
        return null;
    }

    public List<DialogOption> ExpandDialogOptions( ImportRequestType mainType, DialogOption[] options ){
        
        info = CreateInfo( mainType );
        if( info == null ){
            throw new System.Exception("Cannot build file dialog options with\""+mainType+"\"");
        }
        var expanded = new List<DialogOption>();
        DirectoryInfo directory = new DirectoryInfo( info.folder );
        var allAssets = directory.GetFiles("*"+info.ext);

        if( options != null ){
            foreach( var option in options ){
                switch( option.type ){
                case DialogOptionType.ITEM:
                case DialogOptionType.CHARACTER:
                case DialogOptionType.GENERIC:
                    if( !expanded.Contains( option ) ) expanded.Add( option );
                    break;
                case DialogOptionType.ITEM_ATTRIBUTE_FILTER:
                    foreach( var asset in allAssets ){
                        var itemName = System.IO.Path.GetFileNameWithoutExtension( asset.FullName );
                        var candidate = new DialogOption( itemName, DialogOptionType.ITEM );
                        if( expanded.Contains( candidate ) ) continue;

                        var itemRequest = GetOptionRequest( ImportRequestType.ITEM, itemName ) as ItemRequest;
                        if( itemRequest != null && System.Array.Exists( itemRequest.itemSettings.attributes, entry=> entry==option.value) ){
                            expanded.Add( candidate );
                        }
                    }
                    break;
                }
            }
        }else{
            foreach( var asset in allAssets ){
                var itemName = System.IO.Path.GetFileNameWithoutExtension( asset.FullName );
                expanded.Add( new DialogOption( itemName, info.optionType ) );
            }
        }
        return expanded;
    }

    public void DisplaySelection( string titleText, List<DialogOption> _dialogOptions, DialogOptionCallback _onSelect=null, GenericCallback _onDisable=null, GameObject overrideScrollButtonEntry=null, SIRBoolReturnFunc _validateRequestFunc=null ){
        title.text = titleText;
        options = _dialogOptions;
        onSelect = _onSelect;
        onDisable = _onDisable;
        fileIndex = 0;
        validateRequestFunc = _validateRequestFunc;
        scroll.SetContentCount( options.Count, null, overrideScrollButtonEntry );

        contentLayoutElement.flexibleHeight = System.Convert.ToSingle( libraryScrollImage.enabled );

        for( int i=0; i<options.Count; i++ ){
            scroll.scrollContent.GetChild(i).gameObject.SetActive( false );
        }

        for( int i=0; i<options.Count; i++ ){
            var option = options[i];
            if( !IsDialogOptionSpawnable( option ) ) continue;
            SetLoadingButton( scroll.scrollContent.GetChild(i).GetComponent<LibraryEntry>(), filesLoading.Contains( option ) );
        }
    }

    private bool IsDialogOptionSpawnable( DialogOption option ){
        return option.type == DialogOptionType.ITEM || option.type == DialogOptionType.CHARACTER || option.type == DialogOptionType.SCENE;
    }

    private void FixedUpdate(){
        if( options == null || fileIndex >= options.Count || fileIndex >= scroll.scrollContent.childCount ){
            return;
        }
        var option = options[ fileIndex ];
        var entry = scroll.scrollContent.GetChild( fileIndex++ ).GetComponent<LibraryEntry>();

        bool allowed = true;
        entry.label.text = option.value;
        if( IsDialogOptionSpawnable( option ) ){
            SpawnableImportRequest spawnableRequest = GetOptionRequest( info.type, option.value );
            entry.tag = spawnableRequest as VivaObject;
            if( spawnableRequest == null ){
                Debugger.LogError("Could not load and spawn asset \""+option.value+"\" as a "+info.type);
            }else{
                if( option.textureOverride == null ){
                    spawnableRequest.thumbnail.onThumbnailChange._InternalAddListener( UpdateEntrySprite );
                    if( spawnableRequest.thumbnail.texture == null ) spawnableRequest._InternalOnGenerateThumbnail();
                    else UpdateEntrySprite( spawnableRequest );
                }else{
                    var handle = TextureHandle.Load( option.textureOverride );
                    entry.SetThumbnailSprite( handle._internalTexture );
                }
            }
            if( validateRequestFunc != null ){
                allowed = validateRequestFunc( spawnableRequest );
            }
        }else{
            if( option.textureOverride == null ){
                entry.thumbnail.sprite = defaultSprite;
            }else{
                var handle = TextureHandle.Load( option.textureOverride );
                entry.SetThumbnailSprite( handle._internalTexture );
            }
        }
        entry.gameObject.SetActive( allowed );
        entry.button.onClick.RemoveAllListeners();
        entry.button.onClick.AddListener( delegate{
            onSelect( option, entry );
        } );
    }

    private void UpdateEntrySprite( VivaObject vivaObject ){
        for( int i=0; i<scroll.scrollContent.childCount; i++ ){
            var entry = scroll.scrollContent.GetChild(i).GetComponent<LibraryEntry>();
            if( entry.tag == vivaObject ){
                var thumbnailTex = vivaObject.thumbnail.texture;
                if( thumbnailTex == null ) thumbnailTex = BuiltInAssetManager.main.defaultFBXThumbnail;
                entry.SetThumbnailSprite( thumbnailTex );
            }
        }
    }

    private SpawnableImportRequest GetOptionRequest( ImportRequestType requestType, string name ){
        switch( requestType ){
        case ImportRequestType.CHARACTER:
            return Character.instances._InternalGetRequest( name );
        case ImportRequestType.ITEM:
            return Item.instances._InternalGetRequest( name );
        case ImportRequestType.SESSION:
            return SceneSettings.instances._InternalGetRequest( name );
        default:
            Debug.LogError("UNSUPPORTED LIBRARY EXPLORER");
            return null;
        }
    }

    private void SetLoadingButton( LibraryEntry entry, bool isLoading ){
        if( entry == null ) return;
        entry.loadingDots?.gameObject.SetActive( isLoading );
        entry.thumbnail.color = isLoading ? Color.grey : Color.white;
        entry.button.interactable = !isLoading;
    }

    public void RemoveLoadingFromButton( DialogOption option ){
        filesLoading.Remove( option );
        for( int i=0; i<options.Count; i++ ){
            var candidate = options[i];
            if( candidate.Equals( option ) ){
                SetLoadingButton( scroll.scrollContent.GetChild(i).GetComponent<LibraryEntry>(), false );
                break;
            }
        }
    }

    public void LoadAndSpawnDialogOption( DialogOption option, LibraryEntry entry ){
        var spawnableRequest = GetOptionRequest( info.type, option.value );
        if( spawnableRequest == null ){
            Debug.LogError("Could not load and spawn asset \""+option.value+"\" as a "+info.type);
            return;
        }
        filesLoading.Add( option );
        SetLoadingButton( entry, true );

        entry.explorer = this;
        
        switch( info.type ){
        case ImportRequestType.CHARACTER: Character._InternalSpawn( option.value, false, Vector3.zero, Quaternion.identity, delegate( Character character ){
                RemoveLoadingFromButton( option );
            },
            delegate( Character character ){
                VivaPlayer.user.controls.InitializeInstanceTransform( character );
            } );
            break;
        case ImportRequestType.ITEM:  Item._InternalSpawn( option.value, false, Vector3.zero, Quaternion.identity, delegate( Item item ){
                RemoveLoadingFromButton( option );
            },
            delegate( Item item ){
                VivaPlayer.user.controls.InitializeInstanceTransform( item );
            } );
            break;
        }
    }
}

}