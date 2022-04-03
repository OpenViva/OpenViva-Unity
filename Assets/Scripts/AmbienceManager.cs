using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;


namespace viva{

public struct WaitEntry{
    public GenericCallback callback;
    public float timeOfDay;

    public override bool Equals( object obj ){
        if (!(obj is WaitEntry)) return false;
        var candidate = (WaitEntry)obj;
        return callback == candidate.callback;
    }

    public WaitEntry( GenericCallback _callback, float _dayTimeToWait ){
        callback = _callback;
        timeOfDay = _dayTimeToWait;
    }
}
    
public class ListenerDayCycle: Listener<WaitEntry>{
    
    public float timeOfDay = 0.0f;

    public ListenerDayCycle( string _name ):base(_name){
    }

    public override void AddListener( VivaScript source, WaitEntry waitEntry ){
        if( source == null ) throw new System.Exception("Cannot listen to \""+name+"\" with a null source");
        if( waitEntry.callback == null ) throw new System.Exception("Cannot listen with a null function");
        if( RegistryExists( source.registry, waitEntry ) ) throw new System.Exception("Already listening from \""+name+"\". Remove the listener before adding another one.");

        //add current time of day to make it a delta from now
        waitEntry.timeOfDay += timeOfDay;

        //sort least to greatest
        int insertIndex = 0;
        for( int i=listens.Count; i-->0; ){
            var candidate = listens[i];
            if( candidate.value.timeOfDay >= waitEntry.timeOfDay ){
                insertIndex = i+1;
                break;
            }
        }
        listens.Insert( insertIndex, new Listen<WaitEntry>(){ registry=source.registry, value=waitEntry } );
        source.registry.registeredTo.Add( this );
    }

    public void InvokeCurrentTimeOfDay(){
        int frontLength = 0;
        for( int i=listens.Count; i-->0; ){
            if( listens[i].value.timeOfDay <= timeOfDay ){
                frontLength++;
            }else{
                break;
            }
        }
        if( frontLength == 0 ) return;
        var safeCopy = listens.GetRange( listens.Count-frontLength, frontLength );
        listens.RemoveRange( listens.Count-frontLength, frontLength );
        foreach( var copy in safeCopy ){
            copy.registry.registeredTo.Remove( this );
            copy.value.callback();
        }
    }
}

[System.Serializable]
public class WeatherSound{
    public string name;
    public string morningSound;
    public string daySound;
    public string nightSound;
    public bool indoors;

    public AudioClip LoadDayEventSound( DayEvent dayEvent ){
        switch( dayEvent ){
        case DayEvent.MORNING:
            return Sound.Load( "nature", "ambience", morningSound );
        case DayEvent.DAY:
            return Sound.Load( "nature", "ambience", daySound );
        case DayEvent.NIGHT:
            return Sound.Load( "nature", "ambience", nightSound );
        }
        return null;
    }
}

public enum DayEvent{
    MORNING,
    DAY,
    NIGHT
}

[RequireComponent(typeof(Light))]
public class AmbienceManager : MonoBehaviour{

    public static AmbienceManager main { get; private set; }

    [Range(0,1f)]
    [SerializeField]
    private float morningCycleBegin = 0.9f;
    [Range(0,1f)]
    [SerializeField]
    private float morningCycleEnd = 0.15f;
    [Range(0,1f)]
    [SerializeField]
    private float nightCycleBegin = 0.5f;
    [Range(0,0.5f)]
    [SerializeField]
    private float moonLightUpMin = 0.1f;
    [SerializeField]
    public float cycleSeconds = 10.0f;
    [SerializeField]
    private float yaw = 0;
    [SerializeField]
    private Light m_sunLight;
    public Light sunLight { get{ return m_sunLight; } }
    [SerializeField]
    private Light moonLight;
    [SerializeField]
    private HDAdditionalLightData moonLightData;
    [SerializeField]
    private Color dayCharacterEmision;
    [SerializeField]
    private Color nightCharacterEmision;
    [SerializeField]
    private float shadowUpdateTime = 0.025f;
    [Range(0,500f)]
    [SerializeField]
    private float moonLightStrength = 500;
    [SerializeField]
    private MeshRenderer stars;
    [SerializeField]
    private AudioSource globalSourceA;
    [SerializeField]
    private AudioSource globalSourceB;
    [SerializeField]
    private string defaultWeatherSoundName;

    private const float sunYLimit = 0.1f;
    private float shadowUpdateTimer = 0;
    private Coroutine fadeCoroutine;
    private WeatherSound defaultWeatherSound;

    public HDAdditionalLightData hdLightData { get; private set; }
    public ListenerDayEvent onDayEvent { get; private set; } = new ListenerDayEvent("onDayEvent");
    public ListenerDayCycle onDayTimePassed { get; private set; } = new ListenerDayCycle("onDayTimePassed");
    private readonly int characterEmissionID = Shader.PropertyToID("_CharacterEmission");
    private readonly int alphaID = Shader.PropertyToID("_Alpha");

    public enum Music{
		NONE,	//first index music be left out NULL
		DAY_INDOOR,
		DAY_OUTDOOR,
		NIGHT,
		EXPLORING,
		EXPLORING_NIGHT,
		ONSEN
	}

    [Header("Music")]
	[SerializeField]
	private bool muteMusic = false;
	[SerializeField]
	private AudioSource musicSourceA;
	[SerializeField]
	private AudioSource musicSourceB;
	[SerializeField]
	private AudioClip[] music = new AudioClip[ System.Enum.GetValues(typeof(Music)).Length ];

	private Music currentMusic = Music.NONE;
	private Coroutine fadeMusicCoroutine = null;
	private Music queuedMusic = Music.NONE;
	private Music lastMusic = Music.DAY_OUTDOOR;

	private bool lockMusic = false;
	public bool userIsIndoors { get{ return indoorCounter>0; } }
	private bool userIsExploring = false;
    private int indoorCounter;
    private Music? overrideMusic = null;

	public bool IsMusicMuted(){
		return muteMusic;
	}

    public void SetOverrideMusic( Music? music ){
        overrideMusic = music;
        SetMusic( GetDefaultMusic() );
    }

	public void SetMuteMusic( bool enable ){
		muteMusic = enable;
		if( muteMusic ){
			SetMusic( Music.NONE, 1.0f );
		}else{
			SetMusic( GetDefaultMusic(), 0.5f );
		}
	}

	public void LockMusic( bool _lockMusic ){
		lockMusic = _lockMusic;
	}

	public void SetMusic( Music newMusic, float fadeTime = 3.0f ){
		if( lockMusic ){
			return;
		}
		if( newMusic != Music.NONE ){
			lastMusic = newMusic;
		}
		if( muteMusic ){
			newMusic = Music.NONE;
		}
		queuedMusic = newMusic;
		if( currentMusic == newMusic ){
			return;
		}
		//Wait for last fade
		if( fadeMusicCoroutine != null ){
			return;
		}
		fadeMusicCoroutine = StartCoroutine( FadeMusic( newMusic, fadeTime ) );
	}

	public void AddToIndoorCounter( int add ){
        indoorCounter += add;
        onDayEvent._InternalAddListener( PlayMusic );
	}

	public void SetUserIsExploring( bool exploring ){
		userIsExploring = exploring;
		SetMusic( GetDefaultMusic() );
	}

	public Music GetDefaultMusic(){
        if( overrideMusic.HasValue ) return overrideMusic.Value;
		switch( onDayEvent.currentEvent ){
		case DayEvent.DAY:
		
			if( userIsExploring ){
				return Music.EXPLORING;
			}
			if( userIsIndoors ){
				return Music.DAY_INDOOR;
			}else{
				return Music.DAY_OUTDOOR;
			}
		case DayEvent.MORNING:
			return Music.NONE;
		case DayEvent.NIGHT:
		
			if( userIsExploring ){
				return Music.EXPLORING_NIGHT;
			}
			return Music.NIGHT;
		}
		return Music.NONE;
	}

	public void UpdateMusicVolume(){
		musicSourceB.volume = VivaSettings.main.musicVolume;
	}

	private IEnumerator FadeMusic( Music newMusic, float fadeTime ){
		Debug.Log("[MUSIC] "+newMusic);
		float timer = fadeTime;

		//Fade music source from A to B
		musicSourceA.clip = music[ (int)currentMusic ];
		musicSourceA.time = musicSourceB.time;
		musicSourceB.volume = VivaSettings.main.musicVolume;
		musicSourceA.Play();
		
		musicSourceB.clip = music[ (int)newMusic];
		musicSourceB.time = musicSourceA.time;
		musicSourceB.volume = 0.0f;
		musicSourceB.Play();
		
		while( timer > 0.0f ){
			timer = Mathf.Max( 0.0f, timer-Time.deltaTime );
			musicSourceA.volume = (timer/fadeTime)*VivaSettings.main.musicVolume;
			musicSourceB.volume = (1.0f-timer/fadeTime)*VivaSettings.main.musicVolume;
			yield return null;
		}
		musicSourceA.volume = 0.0f;
		musicSourceA.Stop();
		musicSourceB.volume = VivaSettings.main.musicVolume;
		
		fadeMusicCoroutine = null;
		currentMusic = newMusic;

		if( queuedMusic != currentMusic ){
			SetMusic( queuedMusic );
		}
	}


    private void Awake(){
        hdLightData = sunLight.GetComponent<HDAdditionalLightData>();
        main = this;
        defaultWeatherSound = BuiltInAssetManager.main.FindWeatherSound( defaultWeatherSoundName );
        hdLightData.SetShadowUpdateMode( ShadowUpdateMode.OnDemand );
    }

    public void _InternalReset(){
        onDayEvent._InternalReset();
        onDayTimePassed._InternalReset();
        AmbienceManager.main.PlayDefaultGlobalAmbience();
        indoorCounter = 0;
        overrideMusic = null;
    }

    public DayEvent GetDayEvent( float cycle ){
        cycle %= 1f;
        if( cycle < morningCycleEnd ){
            return DayEvent.MORNING;
        }else if( cycle < nightCycleBegin ){
            return DayEvent.DAY;
        }else if( cycle < morningCycleBegin ){
            return DayEvent.NIGHT;
        }
        return DayEvent.MORNING;
    }

    private void FixedUpdate(){

        if( cycleSeconds > 0 ){
            float dayTimePassed = Time.deltaTime/cycleSeconds;
            onDayTimePassed.timeOfDay += dayTimePassed;
            sunLight.transform.rotation = Quaternion.Euler( onDayTimePassed.timeOfDay*360.0f, yaw, 0 );

            onDayTimePassed.InvokeCurrentTimeOfDay();

            var newEvent = GetDayEvent( onDayTimePassed.timeOfDay );
            if( newEvent != onDayEvent.currentEvent ) onDayEvent.Invoke( newEvent );
        }

        if( sunLight.transform.forward.y > sunYLimit ){
            sunLight.enabled = false;
        }else{
            sunLight.enabled = true;
        }

        shadowUpdateTimer += Time.deltaTime;
        if( shadowUpdateTimer > shadowUpdateTime ){
            shadowUpdateTimer %= shadowUpdateTime;
            hdLightData.RequestShadowMapRendering();
            if (UnityEngine.Rendering.RenderPipelineManager.currentPipeline is HDRenderPipeline) {
                HDRenderPipeline hd = (HDRenderPipeline)UnityEngine.Rendering.RenderPipelineManager.currentPipeline;
                hd.RequestSkyEnvironmentUpdate();
            }
        }

        var nightFactor = Mathf.Clamp01( (sunLight.transform.forward.y+moonLightUpMin)*2.0f );
        moonLightData.intensity = moonLightStrength*nightFactor;
        moonLight.enabled = nightFactor>0;

        stars.material.SetFloat( alphaID, nightFactor );
        stars.gameObject.SetActive( nightFactor>0 );

        Shader.SetGlobalColor( characterEmissionID, Color.LerpUnclamped( nightCharacterEmision, dayCharacterEmision, nightFactor ) );
    }

    private void FadeToNewDefaultDayEventSound( DayEvent dayEvent ){
        AmbienceManager.main.PlayGlobalAmbience( defaultWeatherSound.LoadDayEventSound( dayEvent ) );
    }

    public void PlayDefaultGlobalAmbience(){
        onDayEvent._InternalAddListener( FadeToNewDefaultDayEventSound );
    }

    private void PlayMusic( DayEvent dayEvent ){
		SetMusic( GetDefaultMusic() );
    }

    public void PlayManualGlobalAmbience( AudioClip clip ){
        onDayEvent._InternalRemoveListener( FadeToNewDefaultDayEventSound );
        PlayGlobalAmbience( clip );
    }

    private void PlayGlobalAmbience( AudioClip clip ){
        if( clip == null ){
            Debugger.LogError("Cannot play null global ambience");
            return; //adding listener fires this function
        }

        if( fadeCoroutine != null ){
            StopCoroutine( fadeCoroutine );
            fadeCoroutine = null;
        }
        fadeCoroutine = StartCoroutine( FadeCoroutine( clip ) );
    }

    //mix between 2 global sound sources to fade clip
    private IEnumerator FadeCoroutine( AudioClip clip ){

        AudioSource fadeOut = globalSourceA.volume > globalSourceB.volume ? globalSourceA : globalSourceB;
        AudioSource fadeIn = fadeOut==globalSourceA ? globalSourceB : globalSourceA;

        fadeIn.clip = clip;

        float fadeInStart = fadeIn.volume;
        float duration = Mathf.Abs( fadeInStart-1f )+0.1f;
        fadeOut.enabled = true;
        fadeIn.enabled = true;

        fadeIn.Play();

        float timer = 0;
        while( timer < duration ){
            timer += Time.deltaTime;

            float alpha = Mathf.Clamp01( timer/duration );
            fadeIn.volume = Mathf.LerpUnclamped( fadeInStart, 1f, alpha );
            fadeOut.volume = 1f-fadeIn.volume;

            yield return null;
        }
        fadeOut.enabled = false;

        fadeCoroutine = null;
    }
}


}