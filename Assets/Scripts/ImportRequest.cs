using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.Jobs;
using Unity.Collections;


namespace viva{

public delegate void VivaObjectArrayReturnFunc( VivaEditable[] objects );

public enum ImportRequestType{
    FBX,
    TEXTURE,
    SCRIPT,
    CHARACTER,
    SOUND,
    ITEM,
    SESSION
}
public abstract class ImportRequest : VivaEditable{

    public static List<ImportRequest> CreateRequests( List<string> filepaths ){
        List<ImportRequest> requests = new List<ImportRequest>();
        foreach( var filepath in filepaths ){
                
            ImportRequest importRequest = CreateRequest( filepath );
            if( importRequest != null )requests.Add( importRequest );
        }
        return requests;
    }
    

    public static ImportRequest CreateRequest( string filepath ){
        if( string.IsNullOrEmpty( filepath ) ) return null;
        ImportRequest importRequest;
        string ext = Path.GetExtension( filepath );
        switch( ext ){
        case ".png":
        case ".jpg":
        case ".jpeg":
            importRequest = new TextureRequest( filepath );
            break;
        case ".fbx":
            importRequest = new FBXRequest( filepath, FBXContent.ANIMATON|FBXContent.MODEL );
            break;
        case ".cs":
            importRequest = new ScriptRequest( filepath );
            break;
        case ".character":
            importRequest = new CharacterRequest( filepath );
            break;
        case ".item":
            importRequest = new ItemRequest( filepath );
            break;
        case ".wav":
            importRequest = new SoundRequest( AudioType.WAV, filepath );
            break;
        case ".ogg":
            importRequest = new SoundRequest( AudioType.OGGVORBIS, filepath );
            break;
        case ".mp3":
            importRequest = new SoundRequest( AudioType.MPEG, filepath );
            break;
        case ".viva":
            importRequest = new SceneRequest( filepath );
            break;
        default:
            importRequest = null;
            break;
        }
        return importRequest;
    }

    public readonly string filepath;
    public readonly ImportRequestType type;
    public string importError { get; private set; }
    public bool imported { get; protected set; } = false;
    public GenericCallback _internalOnImported;
    private long lastModifiedTime;


    public ImportRequest( string _filepath, ImportRequestType _type ):base(null){
        filepath = Path.GetFullPath( _filepath );
        type = _type;
    }

    public bool CheckOutdatedImport(){
        if( lastModifiedTime == 0 ) return false;
        try{
            var newModifiedTime = System.IO.File.GetLastWriteTime( filepath ).ToFileTime();
            if( lastModifiedTime < newModifiedTime ){
                lastModifiedTime = newModifiedTime;
                return true;
            }
        }catch( System.Exception e ){
            Debug.LogError("Could not get last modified time for \""+filepath+"\"");
        }
        return false;
    }

    public void Import( bool userCall=true ){ //return string, null for error
        imported = false;
        importError = OnImport();
        if( importError == null ){
            imported = true;
            try{
                lastModifiedTime = System.IO.File.GetLastWriteTime( filepath ).ToFileTime();
            }catch( System.Exception e ){
                Debug.LogError("Could not get last modified time for \""+filepath+"\"");
            }
        }else{
            imported = false;
            importError = "Could not import \""+filepath+"\"";
            if( userCall ){
                Sound.main.PlayGlobalUISound( UISound.FATAL_ERROR );
                Debugger.LogError(importError);
            }
        }
        _internalOnImported?.Invoke();
    }

    protected abstract string OnImport();

    public sealed override string GetInfoHeaderTitleText(){
        
        var color = CreateMenu.InfoTypeToColor( CreateMenu.RequestTypeToInfoType( type ) );
        return color+filepath+"</color>";
    }
}

}