using UnityEngine;

namespace viva
{

    public static class LoliUtility
    {

        public static AutonomyPlayAnimation CreateSpeechAnimation(Loli loli, AnimationSet animationSet, SpeechBubble bubble)
        {
            var playConfuseAnim = new AutonomyPlayAnimation(loli.autonomy, "confused", loli.GetAnimationFromSet(animationSet));
            playConfuseAnim.onAnimationEnter += delegate
            {
                loli.speechBubbleDisplay.DisplayBubble(GameDirector.instance.GetSpeechBubbleTexture(bubble));
            };
            return playConfuseAnim;
        }

        public static AutonomyFaceDirection SetRootFacingTarget(Loli loli, string name, float speedMultiplier, Vector3 position)
        {
            var setRootFacingTarget = new AutonomyFaceDirection(loli.autonomy, name, delegate (TaskTarget target)
            {
                target.SetTargetPosition(position);
            }, speedMultiplier);

            return setRootFacingTarget;        
        }
    }

}