using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace viva{

[System.Serializable]
public class SoundSettings : VivaEditable{

    public static string root { get{ return Viva.contentFolder+"/Sounds"; } }

    public string name;
    public string set = null;
    public string group = null;
    public AudioClip audioClip { get; set; }
    

    public SoundSettings( AudioClip _audioClip, string _name, ImportRequest __internalSourceRequest ):base(__internalSourceRequest){
        name = _name;
        audioClip = _audioClip;

        usage.onDiscarded += delegate{
            AudioClip.Destroy( audioClip );
            audioClip = null;
        };
    }

    public override string GetInfoHeaderTitleText(){
        return "Sound - "+name;
    }
    public override string GetInfoHeaderText(){
        return name;
    }
    public override string GetInfoBodyContentText(){
        return "";
    }
    public override void _InternalOnGenerateThumbnail(){
        thumbnail.Copy( _internalSourceRequest.thumbnail );
    }
    public override void OnInstall( string subFolder=null ){
        _internalSourceRequest.OnInstall();
    }

    public override CreateMenu.InputInfo[] OnCreateMenuInputInfoDrawer(){
        return new CreateMenu.InputInfo[]{
            new CreateMenu.InputInfo( "Set", ""+set, delegate( string val ){
                set = val.ToLower();
                return val.ToLower();
            } ),
            new CreateMenu.InputInfo( "Group", ""+group, delegate( string val ){
                group = val.ToLower();
                return val.ToLower();
            } )
        };
    }
    
    public override void OnShare(){
        _internalSourceRequest.OnShare();
    }
}

}