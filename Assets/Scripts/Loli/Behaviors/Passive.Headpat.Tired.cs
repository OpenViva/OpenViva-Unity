using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{


public partial class HeadpatBehavior: PassiveBehaviors.PassiveTask {

    private Loli.Animation GetTiredHeadpatStartAnimation(){
        return Loli.Animation.STAND_TIRED_HEADPAT_IDLE;
    }
    private Loli.Animation GetTiredHeadpatIdleAnimation(){
        return GetTiredHeadpatStartAnimation();	//same animation
    }
}

}