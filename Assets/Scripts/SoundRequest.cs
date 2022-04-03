using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine.Networking;


namespace viva{

public class AudioClipDownload{
    public AudioClip audioClip;
    public string error;
}


public class SoundRequest: ImportRequest{

    public SoundSettings soundSettings { get; private set; }
    private AudioType audioType;


    public SoundRequest( AudioType _audioType, string _filepath ):base( _filepath, ImportRequestType.SOUND ){
        audioType = _audioType;
    }

    public override void OnInstall( string subFolder=null ){
        if( soundSettings == null ){
            UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, "Could not install", "Cannot install SoundRequest with errors!" );
            return;
        }
        var dir = SoundSettings.root+"/"+soundSettings.set;
        if( !Tools.EnsureFolder( dir ) ){
            UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, "Could not install","Sound set is invalid as a directory" );
            return;
        }
        dir = SoundSettings.root+"/"+soundSettings.set+"/"+soundSettings.group;
        if( !Tools.EnsureFolder( dir ) ){
            UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, "Could not install","Sound group is invalid as a directory" );
            return;
        }

        Tools.ArchiveFile( _internalSourceRequest.filepath, dir+"/"+Path.GetFileName( _internalSourceRequest.filepath ) );

        Sound.main.PlayGlobalUISound( UISound.SAVED );
    }

    protected override string OnImport(){
        AudioClipDownload handle = new AudioClipDownload();
        DownloadAudioClip( handle, filepath, audioType );

        if( handle.audioClip == null ){
            return "Could not import sound \""+filepath+"\"";
        }
        soundSettings = new SoundSettings( handle.audioClip, Path.GetFileName( filepath ), this );
        return null;
    }

    public static void DownloadAudioClip( AudioClipDownload handle, string filepath, AudioType audioType ){
        UnityWebRequest soundRequest = UnityWebRequestMultimedia.GetAudioClip( filepath, audioType );
        var task = soundRequest.SendWebRequest();
        while( !task.isDone ){}

        if( soundRequest.result == UnityWebRequest.Result.ConnectionError ){
            handle.error = soundRequest.error;
        }else{
            handle.audioClip = DownloadHandlerAudioClip.GetContent( soundRequest );
            handle.audioClip.name = Path.GetFileName( filepath );
        }
    }

    public override string GetInfoHeaderText(){
        return soundSettings == null ? "" : soundSettings.name;
    }
    public override string GetInfoBodyContentText(){
        return "";
    }
    public override void _InternalOnGenerateThumbnail(){
        thumbnail.texture = GameUI.main.createMenu.defaultSoundFileThumbnail;
    }
    public override void OnCreateMenuSelected(){
        GameUI.main.createMenu.DisplayVivaObjectInfo<SoundSettings>( soundSettings, this );
        GameUI.main.createMenu.DisplayPlaySoundButton();
    }
}

}