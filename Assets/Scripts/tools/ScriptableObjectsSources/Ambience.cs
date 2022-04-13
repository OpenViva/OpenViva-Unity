using System.Collections.Generic;
using UnityEngine;

namespace viva{


[System.Serializable]
[CreateAssetMenu(fileName = "Ambience", menuName = "Logic/Ambience", order = 1)]
public class Ambience: ScriptableObject{
	public AudioClip morningIndoor;
	public AudioClip morningOutdoor;
	public AudioClip eveningIndoor;
	public AudioClip eveningOutdoor;
	public AudioClip nightIndoor;
	public AudioClip nightOutdoor;

	public SoundSet randomSounds;

	
	public AudioClip GetAudio( SkyDirector.DaySegment daySegment, bool indoor ){
		if( indoor ){
			switch( daySegment ){
			case SkyDirector.DaySegment.MORNING:
				return morningIndoor;
			case SkyDirector.DaySegment.DAY:
				return eveningIndoor;
			case SkyDirector.DaySegment.NIGHT:
				return nightIndoor;
			}
		}else{
			switch( daySegment ){
			case SkyDirector.DaySegment.MORNING:
				return morningOutdoor;
			case SkyDirector.DaySegment.DAY:
				return eveningOutdoor;
			case SkyDirector.DaySegment.NIGHT:
				return nightOutdoor;
			}
		}
		return null;
	}
}

}