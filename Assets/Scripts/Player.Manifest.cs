
namespace viva
{


    public partial class Player : Character
    {

        public enum ControlType
        {
            KEYBOARD,
            VR
        }

        public enum UtilityHoldForms
        {
            FIST_CLOSED_RIGHT,
            FIST_CLOSED_LEFT,
            PALM_EXTEND_RIGHT,
            PALM_EXTEND_LEFT,
            HANDHOLD_GIVE_RIGHT,
            HANDHOLD_GIVE_LEFT,
            HANDHOLD_MATCH_RIGHT,
            HANDHOLD_MATCH_LEFT,
            POLAROID_RIP_RIGHT,
            POLAROID_RIP_LEFT,
        }

        public enum Animation
        {   //DO NOT CHANGE ORDER
            NONE,
            IDLE,
            GESTURE_COME,
            GESTURE_WAVE,
            GESTURE_PRESENT_RIGHT,
            CAMERA,
            POLAROID,
            POLAROID_RIP_IN_RIGHT,
            POLAROID_RIP_IN_LEFT,
            POLAROID_RIP,
            POKE,
            HEADPAT,
            HEADPAT_SCRUB,
            VALVE,
            VALVE_REPOSITION,
            POINT,
            DOORKNOB,
            DOORKNOB_2_IDLE,
            SOAP_GENERATE_BUBBLES_RIGHT,
            SOAP_GENERATE_BUBBLES_LEFT,
            VR_SOAP_GENERATE_BUBBLES,
            LANTERN,
            WALL_CANDLE,
            KEYBOARD_HANDS_DOWN,
            BAG_PLACE_INSIDE_RIGHT,
            BAG_PLACE_INSIDE_LEFT,
            BAG_TAKE_OUT_RIGHT,
            BAG_TAKE_OUT_LEFT,
            CHICKEN,
            MORTAR,
            BAG,
            MORTAR_AND_PESTLE_IN_RIGHT,
            MORTAR_AND_PESTLE_IN_LEFT,
            MORTAR_AND_PESTLE_GRIND_RIGHT_LOOP,
            MORTAR_AND_PESTLE_GRIND_LEFT_LOOP,
            MORTAR_EMPTY_CONTENTS,
            EGG_CRACK,
            MIXING_BOWL,
            JAR,
            POT,
            MIXING_SPOON,
            MIXING_BOWL_AND_SPOON_IN_RIGHT,
            MIXING_BOWL_AND_SPOON_IN_LEFT,
            MIXING_BOWL_AND_SPOON_RIGHT_LOOP,
            MIXING_BOWL_AND_SPOON_LEFT_LOOP,
            JAR_EMPTY_CONTENTS,
            MIXING_BOWL_EMPTY_CONTENTS,
            KNIFE,
            MIXING_SPOON_SCOOP,
            GESTURE_PRESENT_LEFT,
            MORTAR_EMPTY_INTO_MIXING_BOWL_RIGHT,
            MORTAR_EMPTY_INTO_MIXING_BOWL_LEFT,
            POKER_CARD,
            ADD_CARD_TO_RIGHT,
            ADD_CARD_TO_LEFT,
            SELECT_CARD_RIGHT,
            SELECT_CARD_LEFT,
            CARD_SPRING_FLOURISH_RIGHT,
            CARD_SPRING_FLOURISH_LEFT,
            PESTLE,
            PASTRY,
            PEACH,
            STRAWBERRY,
            CAMERA_VR_ONLY,
            CANTALOUPE,
            CATTAIL,
            EGG,
            HAIR,
            CONTAINER_LID,
            REINS,
            SOAP,
            TOWEL,
            WHEAT_SPIKE,
            HAND,
            HAND_ALT,
            GENERIC,
            FIREWORK,
            RUBBER_DUCKY,
            FLASHLIGHT,
            GESTURE_STOP,
        }
    }

}