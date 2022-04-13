using UnityEngine;
using System.Collections.Generic;

namespace viva{


public static class LoliUtility{
	

	public static AutonomyPlayAnimation CreateSpeechAnimation( Loli loli, AnimationSet animationSet, SpeechBubble bubble ){
		var playConfuseAnim = new AutonomyPlayAnimation( loli.autonomy, "confused", loli.GetAnimationFromSet( animationSet ) );
		playConfuseAnim.onAnimationEnter += delegate{
			loli.speechBubbleDisplay.DisplayBubble( GameDirector.instance.GetSpeechBubbleTexture( bubble ) );
		};
		return playConfuseAnim;
	}
}


}