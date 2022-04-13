using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{


public partial class PokeBehavior : PassiveBehaviors.PassiveTask {

    private Loli.Animation GetBathtubIdleFacePokedAnimation( int pokeSideIsLeft ){
		if( self.IsHappy() ){
			int baseEnumVal = (int)Loli.Animation.BATHTUB_IDLE_FACE_POKE_1_RIGHT;
			int pokeIntensity = (int)( Random.value*1.9f ); //0 or 1
			Loli.Animation pokeAnimation = (Loli.Animation)( baseEnumVal+pokeIntensity*2+pokeSideIsLeft );
			if( self.currentAnim == pokeAnimation ){
				//alternate animation so it always replay a new poke animation
				pokeIntensity = ++pokeIntensity%2;
				pokeAnimation = (Loli.Animation)( baseEnumVal+pokeIntensity*2+pokeSideIsLeft );
			}
			return pokeAnimation;
		}else{
			if( self.currentAnim != Loli.Animation.BATHTUB_SINK_ANGRY ){
				return Loli.Animation.BATHTUB_SINK_ANGRY;
			}else{
				return Loli.Animation.BATHTUB_ANGRY_IDLE_LOOP;
			}
		}
	}

	private Loli.Animation GetBathtubRelaxPostFacePokedAnimation( int pokeBearingSign ){
		if( self.IsHappy() ){
			return Loli.Animation.BATHTUB_RELAX_TO_HAPPY_IDLE;
		}else{
			return Loli.Animation.BATHTUB_RELAX_TO_ANGRY_IDLE;
		}
	}

	private Loli.Animation GetBathtubIdlePostFacePokedAnimation( int pokeBearingSign ){
		if( self.IsHappy() ){
			return Loli.Animation.BATHTUB_HAPPY_IDLE_LOOP;
		}else{
			return Loli.Animation.BATHTUB_ANGRY_IDLE_LOOP;
		}
	}
}

}