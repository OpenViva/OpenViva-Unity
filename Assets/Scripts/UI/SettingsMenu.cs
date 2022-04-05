using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace viva{

public class SettingsMenu : MonoBehaviour
{
    public void CycleDaytime(){
        AmbienceManager.main.onDayTimePassed.timeOfDay += 0.3f;
    }

    public void ResetLogic(){
        var vivaInstances = Resources.FindObjectsOfTypeAll<VivaInstance>();
        foreach( var instance in vivaInstances ) instance?.scriptManager.Recompile();

        Sound.main.PlayGlobalUISound( UISound.RELOADED );
    }

    public void MuteMusic(){
        AmbienceManager.main.SetMuteMusic( !AmbienceManager.main.IsMusicMuted() );
    }
}

}