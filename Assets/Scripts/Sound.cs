using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;


namespace viva{

using SoundDictionary = Dictionary<string,AudioClip>;
using SoundSetDictionary = Dictionary<string,Dictionary<string,AudioClip>>;

public enum UISound{
    SAVED,
    RELOADED,
    FATAL_ERROR,
    SCRIPT_ERROR,
    SMALL_ERROR,
    UNDO,
    MENU_SELECT,
    MENU_SCROLL,
    MESSAGE_SUCCESS,
    MESSAGE_POPUP,
    BUTTON1,
    BUTTON2,
    ACHIEVEMENT
}

public class SoundHandle{
    private AudioSource source;
    public bool exists { get{ return source!=null; } }
    public float volume { get{ if( source ){ return source.volume; }else{ return 0.0f; } } set{ if( source ){ source.volume = value; } } }
    public float pitch { set{ if( source ){ source.pitch = value; } } }
    public bool loop { set{ if( source ){ source.loop = value; } } }
    public float maxDistance { set{ if( source ){ source.maxDistance = value; } } }
    public bool playing { get{ return source ? source.isPlaying : false; } }

    public SoundHandle( AudioSource _source, Vector3 localPosition, Transform parent ){
        source = _source;
        Setup( localPosition, parent );
    }

    public void Setup( Vector3 localPosition, Transform parent ){
        source.transform.SetParent( parent, false );
        source.transform.localPosition = localPosition;
        source.volume = 1.0f;
        source.pitch = 1.0f;
        source.loop = false;
        source.maxDistance = 16.0f;
    }

    public void PlayOneShot( AudioClip clip ){
        if( source && clip ){
            source.PlayOneShot( clip );
        }
    }

    public void Play( AudioClip clip ){
        if( source ){
            source.clip = clip;
            source.Play();
        }
    }

    public void Play( string set, string group, string specificSound=null ){
        if( source ){
            var clip = Sound.Load( set, group, specificSound );
            if( clip ){
                source.clip = clip;
                source.Play();
            }
        }
    }
    
    public void PlayDelayed( AudioClip clip, float delay ){
        if( source && clip ){
            source.clip = clip;
            source.PlayDelayed( delay );
        }
    }

    public void Stop(){
        if( source ){
            source.Stop();
        }
    }
}

public class Sound : MonoBehaviour{

    private static void DownloadAudioClip( string filepath, string fileName, SoundDictionary soundGroup, AudioType audioType ){
        var audioClipDownload = new AudioClipDownload();
        SoundRequest.DownloadAudioClip( audioClipDownload, filepath, audioType );

        if( audioClipDownload.audioClip != null ){
            soundGroup[ fileName ] = audioClipDownload.audioClip;
        }
    }

    private static void PreloadGroup( string set, string group, SoundDictionary soundGroup ){
        try{
            var groupPath = SoundSettings.root+"/"+set+"/"+group;
            var files = Directory.EnumerateFiles( groupPath )
                .Where(file => file.ToLower().EndsWith("mp3") || file.ToLower().EndsWith("wav") || file.ToLower().EndsWith("ogg"))
                .ToList();
            foreach( var filepath in files ){
                var fileName = Path.GetFileName( filepath );
                if( !soundGroup.TryGetValue( fileName, out AudioClip value ) ){

                    soundGroup[ fileName ] = null;
                    
                    AudioType audioType = AudioType.UNKNOWN;
                    switch( Path.GetExtension( filepath ) ){
                    case ".wav":
                        audioType = AudioType.WAV;
                        break;
                    case ".ogg":
                        audioType = AudioType.OGGVORBIS;
                        break;
                    case ".mp3":
                        audioType = AudioType.MPEG;
                        break;
                    }
                    if( audioType != AudioType.UNKNOWN ){
                        DownloadAudioClip( filepath, fileName, soundGroup, audioType );
                    }
                }
            }
        }catch{
            Debugger.LogError("Could not load sound group \""+group+"\" in sound set \""+set+"\"");
        }
    }

    public static void PreloadSet( string set ){
        try{
            if( !soundSetCache.TryGetValue( set, out SoundSetDictionary soundSet ) ){
                soundSet = new SoundSetDictionary();
                soundSetCache[ set ] = soundSet;
            }
            var groups = Directory.EnumerateDirectories( SoundSettings.root+"/"+set ).ToList();
            foreach( var group in groups ){
                var groupName = Path.GetFileName( group );
                if( !soundSet.TryGetValue( groupName, out SoundDictionary soundGroup ) ){
                    soundGroup = new SoundDictionary();
                    soundSet[ groupName ] = soundGroup;
                }
                PreloadGroup( set, groupName, soundGroup );
            }
        }catch{
            Debugger.LogError("Could not load sound set \""+set+"\"");
        }
    }

    public static AudioClip Load( string set, string group, string specificSound=null ){
        if( set == null || group == null ) return null;

        if( !soundSetCache.TryGetValue( set, out SoundSetDictionary soundSet ) ){
            PreloadSet( set );
            if( !soundSetCache.TryGetValue( set, out soundSet ) ){
                return null;
            }
        }
        if( !soundSet.TryGetValue( group, out SoundDictionary soundGroup ) ){
            Debugger.LogError( "Sound group \""+group+"\" does not exist in \""+set+"\"" );
            return null;
        }

        if( specificSound == null ){
            //return a random sound from the group
            if( soundGroup.Count == 0 ){
                Debugger.LogError( "Sound group \""+group+"\" in \""+set+"\" is empty" );
                return null;
            }
            return soundGroup.ElementAt( Random.Range( 0, soundGroup.Count ) ).Value;
        }else if( soundGroup.TryGetValue( specificSound, out AudioClip sound ) ){
            return sound;
        }else{
            Debugger.LogError( "Specific Sound \""+specificSound+"\" does not exist in group \""+group+"\" in set \""+set+"\"" );
            return null;
        }
    }


    [SerializeField]
    private AudioSource[] audioSources;
    [SerializeField]
    private GameObject audioSourcePrefab;
    [SerializeField]
    private AudioClip[] uiSounds = new AudioClip[ System.Enum.GetValues( typeof( UISound ) ).Length ];
    [SerializeField]
    private AudioSource globalSource;

    private SoundHandle[] handles;
    private int handleIndex = 0;
    private int lastUISoundTime = 0;

    public static Sound main { get; private set; }
    private static Dictionary<string,SoundSetDictionary> soundSetCache = new Dictionary<string, SoundSetDictionary>();


    public void PlayGlobalUISound( UISound sound ){
        if( Time.frameCount-lastUISoundTime < 5 ){
            return;
        }
        lastUISoundTime = Time.frameCount;
        globalSource.PlayOneShot( uiSounds[ (int)sound ] );
    }

    public void PlayGlobalOneShot( AudioClip audioClip ){
        globalSource.PlayOneShot( audioClip );
    }

    private void Awake(){
        main = this;
        handles = new SoundHandle[ audioSources.Length ];
        for( int i=0; i<handles.Length; i++ ){
            handles[i] = new SoundHandle( audioSources[i], Vector3.zero, null );
        }
    }

    public static SoundHandle Create( Vector3 localPosition, Transform parent=null ){
        return main._InternalCreate( localPosition, parent );
    }

    private SoundHandle _InternalCreate( Vector3 localPosition, Transform parent=null ){
            
        for( int i=0; i<audioSources.Length; i++ ){
            handleIndex = (handleIndex+1)%audioSources.Length;
            var candidate = handles[ handleIndex ];
            //keep iterating until finding one that isn't occupied
            if( !candidate.exists || !candidate.playing ){
                break;
            }
        }

        var handle = handles[ handleIndex ];
        if( !handle.exists ){
            var audioSourcePrefabInstance = GameObject.Instantiate( audioSourcePrefab );
            var newAudioSource = audioSourcePrefabInstance.GetComponent<AudioSource>();
            audioSources[ handleIndex ] = newAudioSource;
            handle = new SoundHandle( newAudioSource, localPosition, parent );
            handles[ handleIndex ] = handle;
        }else{
            handle.Setup( localPosition, parent );
        }
        return handle;
    }
}

}