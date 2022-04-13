using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace viva{


public partial class Player: Character {
	
    public enum ObjectiveType{
		WASH_HAIR,
		TAKE_PANTY_SHOT,
		POKE_ANGRY,
        MAKE_ANGRY_WITH_HEADPAT,
        MAKE_HAPPY_WITH_HEADPAT,
		KISS_ANGRY_WIPE,
		KISS_MAKE_HAPPY,
		THROW_DUCK,
		LOOK_UP_SKIRT,
		GIVE_2_DONUTS,
		FIND_SHINOBU_A_WATER_REED,
		WATER_REED_SMACK,
		FIND_HAT,
		HOLD_HANDS_AND_WALK,
		BAKE_A_PASTRY,
		POUR_FLOUR_ON_HEAD
    }
	private bool[] objectives = new bool[ System.Enum.GetValues(typeof(ObjectiveType)).Length ];

	public string GetAchievementDescription( ObjectiveType type ){
		switch(type){
		case ObjectiveType.WASH_HAIR:
			return "Wash her hair in the bathtub";
		case ObjectiveType.TAKE_PANTY_SHOT:
			return "Show her a picture of her own panties";
		case ObjectiveType.POKE_ANGRY:
			return "Poke her face until she gets angry";
		case ObjectiveType.MAKE_ANGRY_WITH_HEADPAT:
			return "Make angry with a rough headpat";
		case ObjectiveType.MAKE_HAPPY_WITH_HEADPAT:
			return "Make happy with a nice headpat";
		case ObjectiveType.KISS_ANGRY_WIPE:
			return "Make her angrily wipe off a cheek kiss";
		case ObjectiveType.KISS_MAKE_HAPPY:
			return "Make Loli happy with a cheek kiss";
		case ObjectiveType.THROW_DUCK:
			return "Make her throw a duck at you";
		case ObjectiveType.HOLD_HANDS_AND_WALK:
			return "Hold her hand and walk around";
		case ObjectiveType.LOOK_UP_SKIRT:
			return "Look up Loli's skirt";
		case ObjectiveType.GIVE_2_DONUTS:
			return "Make her hold 2 donuts";
		case ObjectiveType.FIND_SHINOBU_A_WATER_REED:
			return "Find her a water reed";
		case ObjectiveType.WATER_REED_SMACK:
			return "Make her hit you with a water reed";
		case ObjectiveType.FIND_HAT:
			return "Find and give her, her sunhat";
		case ObjectiveType.POUR_FLOUR_ON_HEAD:
			return "Pour Flour on your loli's Head";
		case ObjectiveType.BAKE_A_PASTRY:
			return "Bake a pastry";
		}
		return "";
	}

	public bool IsAchievementComplete( ObjectiveType type ){
		return objectives[(int)type];
	}
    public void CompleteAchievement( ObjectiveType objective ){

        if( IsAchievementComplete( objective ) ){
            return;
        }
        objectives[(int)objective] = true;
		pauseMenu.DisplayHUDMessage( GetAchievementDescription( objective ), PauseMenu.HintType.ACHIEVEMENT );
    }
}

}