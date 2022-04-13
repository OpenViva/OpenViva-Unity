using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


namespace viva{


public partial class SleepingBehavior : ActiveBehaviors.ActiveTask {
    
    // public Loli.Animation GetLaySidePillowHeadpatStartAnimation(){
		
	// 	if( !layingOnRightSide.HasValue ){
	// 		return Loli.Animation.NONE;
	// 	}
	// 	if( phase == SleepingPhase.SLEEPING ){
	// 		if( layingOnRightSide.Value ){
	// 			return Loli.Animation.SLEEP_PILLOW_SIDE_IDLE_RIGHT;
	// 		}else{
	// 			return Loli.Animation.SLEEP_PILLOW_SIDE_IDLE_LEFT;
	// 		}
	// 	}else{
	// 		if( layingOnRightSide.Value ){
	// 			return Loli.Animation.LAY_PILLOW_SIDE_HAPPY_IDLE_RIGHT;
	// 		}else{
	// 			return Loli.Animation.LAY_PILLOW_SIDE_HAPPY_IDLE_LEFT;
	// 		}
	// 	}
    // }

	// public Loli.Animation GetSleepSidePillowHeadpatStartAnimation(){

	// 	if( !layingOnRightSide.HasValue ){
	// 		return Loli.Animation.NONE;
	// 	}
	// 	if( layingOnRightSide.Value ){
	// 		return Loli.Animation.SLEEP_PILLOW_SIDE_HEADPAT_START_RIGHT;
	// 	}else{
	// 		return Loli.Animation.SLEEP_PILLOW_SIDE_HEADPAT_START_LEFT;
	// 	}
	// }


	// public Loli.Animation GetSleepPillowUpHeadpatStartAnimation(){
	// 	return Loli.Animation.SLEEP_PILLOW_UP_IDLE;
	// }

	// public Loli.Animation GetSleepPillowUpHeadpatIdleAnimation(){
	// 	return GetSleepPillowUpHeadpatStartAnimation();
	// }

	// public Loli.Animation GetAwakePillowUpHeadpatStartAnimation(){
	// 	return self.GetLastReturnableIdleAnimation();
	// }
	
	// public Loli.Animation GetAwakePillowUpHeadpatIdleAnimation(){
	// 	if( self.IsHappy() ){
	// 		return Loli.Animation.AWAKE_HAPPY_PILLOW_UP_HEADPAT_LOOP;
	// 	}else{
	// 		return self.GetLastReturnableIdleAnimation();
	// 	}
	// }

	// public Loli.Animation GetSleepSidePillowHeadpatIdleAnimation(){
	// 	if( !layingOnRightSide.HasValue ){
	// 		return Loli.Animation.NONE;
	// 	}
	// 	if( layingOnRightSide.Value ){
	// 		return Loli.Animation.SLEEP_PILLOW_SIDE_IDLE_RIGHT;
	// 	}else{
	// 		return Loli.Animation.SLEEP_PILLOW_SIDE_IDLE_LEFT;
	// 	}
	// }

    // public Loli.Animation GetLaySidePillowHeadpatIdleAnimation(){
    //     return GetLaySidePillowHeadpatStartAnimation();	//same animation
    // }

	// public Loli.Animation GetSleepSidePillowFacePokeAnimation( int pokeSideIsLeft ){
	// 	if( !layingOnRightSide.HasValue ){
	// 		return Loli.Animation.NONE;
	// 	}
	// 	if( phase == SleepingPhase.SLEEPING ){
	// 		if( layingOnRightSide.Value ){
	// 			return Loli.Animation.SLEEP_PILLOW_SIDE_BOTHER_RIGHT;
	// 		}else{
	// 			return Loli.Animation.SLEEP_PILLOW_SIDE_BOTHER_LEFT;
	// 		}
	// 	}else{
	// 		//TODO: sleep side pillow awake poke animations?
	// 	}
	// 	return Loli.Animation.NONE;
	// }

	// public Loli.Animation GetSleepSidePillowPostFacePokeAnimation(){
		
	// 	if( !layingOnRightSide.HasValue ){
	// 		return Loli.Animation.NONE;
	// 	}
		
	// 	if( CheckIfShouldWakeUpFromBother() ){
	// 		return GetWakeUpAnimation( false );
	// 	}
	// 	if( self.passive.poke.pokeCount >= 2 ){
	// 		if( layingOnRightSide.Value ){
	// 			return Loli.Animation.SLEEP_PILLOW_SIDE_TO_SLEEP_PILLOW_UP_RIGHT;
	// 		}else{
	// 			return Loli.Animation.SLEEP_PILLOW_SIDE_TO_SLEEP_PILLOW_UP_LEFT;
	// 		}
	// 	}
	// 	return Loli.Animation.NONE;
	// }

	// public Loli.Animation GetSleepPillowUpPostFacePokeAnimation(){
		
	// 	if( CheckIfShouldWakeUpFromBother() ){
	// 		return GetWakeUpAnimation( false );
	// 	}
	// 	if( self.passive.poke.pokeCount >= 2 ){
	// 		if( Random.value > 0.5f ){
	// 			return Loli.Animation.SLEEP_PILLOW_UP_TO_SLEEP_PILLOW_SIDE_LEFT;
	// 		}else{
	// 			return Loli.Animation.SLEEP_PILLOW_UP_TO_SLEEP_PILLOW_SIDE_RIGHT;
	// 		}
	// 	}
	// 	return Loli.Animation.NONE;
	// }
}

}