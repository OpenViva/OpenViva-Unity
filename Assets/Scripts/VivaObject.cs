using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.Jobs;



namespace viva{

public abstract class VivaObject: VivaDisposable{

    public static T LoadFromFile<T>( string filepath, ImportRequest _internalSourceRequest, T overwriteTarget ) where T:VivaObject{
        if( _internalSourceRequest == null ){
            Debugger.LogError("Cannot load file with null source request");
            return null;
        }
        var obj = Tools.LoadJson<T>( filepath, overwriteTarget );
        if( obj != null ){
            obj.usage = new Usage();
            obj.thumbnail = new Thumbnail();
            obj._internalSourceRequest = _internalSourceRequest;
            return obj;
        }else{
            return null;
        }
    }

    public Thumbnail thumbnail { get; private set; } = new Thumbnail();
    public ImportRequest _internalSourceRequest { get; private set; }

    public VivaObject( ImportRequest __internalSourceRequest ){
        _internalSourceRequest = __internalSourceRequest==null ? this as ImportRequest : __internalSourceRequest;
    }

    public abstract void _InternalOnGenerateThumbnail();
    public void GenerateThumbnail( bool skipIfAlreadyGenerated=true ){
        if( thumbnail.texture && skipIfAlreadyGenerated ) return;
        _InternalOnGenerateThumbnail();
    }
}


public class Thumbnail{
    public Texture2D texture;
    public int animatedFrameWidth = 1;
    public float animatedDuration = 1;
    public readonly ListenerVivaObject onThumbnailChange = new ListenerVivaObject("onThumbnailChange");

    public Thumbnail( Texture2D _texture=null ){
        texture = _texture;
    }

    public void Copy( Thumbnail _copy ){
        texture = _copy.texture;
        animatedFrameWidth = _copy.animatedFrameWidth;
        animatedDuration = _copy.animatedDuration;
    }
}

}