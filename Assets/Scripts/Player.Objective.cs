namespace viva
{


    public partial class Player : Character
    {

        //checklist in order
        public enum ObjectiveType
        {
            RELAX_ONSEN,
            WASH_HAIR,
            POKE_ANGRY,
            MAKE_ANGRY_WITH_HEADPAT,
            MAKE_HAPPY_WITH_HEADPAT,
            KISS_ANGRY_WIPE,
            KISS_MAKE_HAPPY,
            THROW_DUCK,
            LOOK_UP_SKIRT,
            GIVE_2_DONUTS,
            FIND_CHARACTER_A_WATER_REED,
            WATER_REED_SMACK,
            FIND_HAT,
            HOLD_HANDS_AND_WALK,
            BAKE_A_PASTRY,
            POUR_FLOUR_ON_HEAD
        }
        private bool[] objectives = new bool[System.Enum.GetValues(typeof(ObjectiveType)).Length];

        public string GetAchievementDescription(ObjectiveType type)
        {
            switch (type)
            {
                case ObjectiveType.RELAX_ONSEN:
                    return "Have a character Relax in the Onsen";
                case ObjectiveType.WASH_HAIR:
                    return "Wash a characters hair in the bathtub";
                case ObjectiveType.POKE_ANGRY:
                    return "Poke a characters face until they get angry";
                case ObjectiveType.MAKE_ANGRY_WITH_HEADPAT:
                    return "Make a character angry with a rough headpat";
                case ObjectiveType.MAKE_HAPPY_WITH_HEADPAT:
                    return "Make a character happy with a nice headpat";
                case ObjectiveType.KISS_ANGRY_WIPE:
                    return "Make a character angrily wipe off a cheek kiss";
                case ObjectiveType.KISS_MAKE_HAPPY:
                    return "Make a character happy with a cheek kiss";
                case ObjectiveType.THROW_DUCK:
                    return "Make a character throw a duck at you";
                case ObjectiveType.HOLD_HANDS_AND_WALK:
                    return "Hold a characters hand and walk around";
                case ObjectiveType.LOOK_UP_SKIRT:
                    return "Look up characters skirt";
                case ObjectiveType.GIVE_2_DONUTS:
                    return "Make a character hold 2 donuts";
                case ObjectiveType.FIND_CHARACTER_A_WATER_REED:
                    return "Find a character a water reed";
                case ObjectiveType.WATER_REED_SMACK:
                    return "Make a character hit you with a water reed";
                case ObjectiveType.FIND_HAT:
                    return "Find and give a character, A sunhat";
                case ObjectiveType.POUR_FLOUR_ON_HEAD:
                    return "Pour Flour on your characters Head";
                case ObjectiveType.BAKE_A_PASTRY:
                    return "Bake a pastry";
            }
            return "";
        }

        public bool IsAchievementComplete(ObjectiveType type)
        {
            return objectives[(int)type];
        }
        public void CompleteAchievement(ObjectiveType objective)
        {

            if (IsAchievementComplete(objective))
            {
                return;
            }
            objectives[(int)objective] = true;
            pauseMenu.DisplayHUDMessage(GetAchievementDescription(objective), true, PauseMenu.HintType.ACHIEVEMENT);
        }
    }

}