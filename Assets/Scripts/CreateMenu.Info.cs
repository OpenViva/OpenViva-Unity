using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Security.Permissions;


namespace viva{
    
public partial class CreateMenu : MonoBehaviour{

    public enum InfoColor{
        ERROR               =0xff2f2f,
        FBX_REQUEST         =0x66efff,
        MODEL               =0x98d5ff,
        ANIMATION           =0x8bb4ff,
        TEXTURE_REQUEST     =0x6dffc3,
        TEXTURE             =0x6dffc4,
        SCRIPT              =0xffd800,
        CHARACTER_REQUEST   =0xdeff00,
        SCRIPT_REQUEST      =0xffd801,
        ITEM_REQUEST        =0x00d8ff,
        SOUND_REQUEST       =0xffd800,
    }
    
    public static string InfoTypeToColor( InfoColor type ){
        int hex = (int)type;
        return "<color=#"+hex.ToString("X6")+">";
    }
    
    public static InfoColor RequestTypeToInfoType( ImportRequestType reqType ){
        switch( reqType ){
        case ImportRequestType.FBX:
            return InfoColor.FBX_REQUEST;
        case ImportRequestType.TEXTURE:
            return InfoColor.TEXTURE_REQUEST;
        case ImportRequestType.CHARACTER:
            return InfoColor.CHARACTER_REQUEST;
        case ImportRequestType.SCRIPT:
            return InfoColor.SCRIPT_REQUEST;
        case ImportRequestType.ITEM:
            return InfoColor.ITEM_REQUEST;
        case ImportRequestType.SOUND:
            return InfoColor.SOUND_REQUEST;
        default:
            return InfoColor.ERROR;
        }
    }

    public class Info{
        public readonly InfoColor type;
        public readonly object source;
        public string label;

        public Info( InfoColor _type, object _source, string _label ){
            type = _type;
            source = _source;
            label = InfoTypeToColor(_type)+_label+"</color>";
        }
    }

    public class RequestInfo{
        public readonly ImportRequest request;
        public List<Info> infoLabels;

        public RequestInfo( ImportRequest _request ){
            request = _request;
            infoLabels = new List<Info>(){ new Info( RequestTypeToInfoType( request.type ), this, Path.GetFileName( request.filepath ) ) };

            RefreshInfoLabels();
        }
        public void RefreshInfoLabels(){
            infoLabels.RemoveRange( 1, infoLabels.Count-1 );
            if( !request.imported ){
                infoLabels.Add( new Info( InfoColor.ERROR, "...", "      Still importing..." ) );
                return;
            }
            switch( request.type ){
            case ImportRequestType.FBX:
                var fbxRequest = request as FBXRequest;
                if( fbxRequest.lastSpawnedFBX ){
                    foreach( var model in fbxRequest.lastSpawnedFBX.rootModels ){
                        if( !model._internalIsRoot ) continue;
                        infoLabels.Add( new Info( InfoColor.MODEL, model, "      "+model.name ) );
                        foreach( var anim in model.animations ){
                            infoLabels.Add( new Info( InfoColor.ANIMATION, anim, "         "+anim.name ) );
                        }
                    }
                }
                break;
            case ImportRequestType.CHARACTER:
                var charRequest = request as CharacterRequest;
                break;
            default:
                break;
            }
        }
    }
}

}