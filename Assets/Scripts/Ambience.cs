using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

public class Ambience : MonoBehaviour{

    private AudioSource localSource;
    private WeatherSound weatherSound;

    private int counter = 0;

    private void Awake(){

        weatherSound = BuiltInAssetManager.main.FindWeatherSound( name );
        if( weatherSound == null ){
            Debugger.LogWarning("Could not find weather sound with the name \""+name+"\"");
            gameObject.SetActive( false );
        }
    }

    private void FadeToNewDayEventSound( DayEvent dayEvent ){
        AmbienceManager.main.PlayManualGlobalAmbience( weatherSound.LoadDayEventSound( dayEvent ) );
    }
    
    private void OnTriggerEnter( Collider collider ){
        var camera = collider.GetComponent<Camera>();
        if( weatherSound == null || !camera ) return;

        if( counter == 0 ){
            AmbienceManager.main.onDayEvent._InternalAddListener( FadeToNewDayEventSound );
            AmbienceManager.main.AddToIndoorCounter( weatherSound.indoors ? 1 : 0 );
        }
        counter++;
    }

    private void OnTriggerExit( Collider collider ){
        var camera = collider.GetComponent<Camera>();
        if( weatherSound == null || !camera ) return;

        counter--;
        if( counter == 0 ){
            AmbienceManager.main.onDayEvent._InternalRemoveListener( FadeToNewDayEventSound );
            AmbienceManager.main.PlayDefaultGlobalAmbience();
            AmbienceManager.main.AddToIndoorCounter( weatherSound.indoors ? -1 : 0 );
        }
    }
}

}