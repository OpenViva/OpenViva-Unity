using UnityEngine;


namespace viva
{

    public class AnimationEvent<T>
    {

        public class Context
        {

            public delegate void AnimationEventHandler(AnimationEvent<T> animEvent);

            private int currentAnimAccumID = 1;
            private int loopCounter = 0;
            private readonly AnimationEventHandler animEventHandler;

            public Context(AnimationEventHandler _animEventHandler)
            {
                animEventHandler = _animEventHandler;
            }

            public void ResetCounters()
            {
                currentAnimAccumID += loopCounter + 1;
                loopCounter = 0;
            }

            public void UpdateAnimationEvents(AnimationEvent<T>[] animationEvents, bool animationLoops, float normalizedTime)
            {
                if (animationEvents == null)
                {
                    return;
                }
                //animation will loop if no transition present
                if (animationLoops)
                {
                    if (loopCounter != (int)normalizedTime)
                    {
                        loopCounter = (int)normalizedTime;
                        FireAnimationEventsFor(animationEvents, 1.0f);
                        currentAnimAccumID++;
                    }
                    normalizedTime = normalizedTime % 1.0f;
                }
                FireAnimationEventsFor(animationEvents, normalizedTime);
            }

            private void FireAnimationEventsFor(AnimationEvent<T>[] animationEvents, float normalizedTime)
            {

                if (animationEvents != null)
                {

                    for (int i = 0; i < animationEvents.Length; i++)
                    {
                        AnimationEvent<T> animEvent = animationEvents[i];
#if UNITY_EDITOR
                        if (animEvent.fireTimeNormalized > 1.0f)
                        {
                            Debug.LogError("Invalid fireTimeNormalized for " + animEvent.nameID);
                        }
#endif
                        if (animEvent.fireTimeNormalized <= normalizedTime && animEvent.accumID < currentAnimAccumID)
                        {
                            animEvent.accumID = currentAnimAccumID;
                            animEventHandler(animEvent);
                        }
                    }
                }
            }
        }

        public readonly float fireTimeNormalized;
        public readonly int nameID;
        public int accumID;
        public T parameter;

        public AnimationEvent(float _fireTime, int _nameID, T _parameter)
        {
            fireTimeNormalized = _fireTime;
            nameID = _nameID;
            accumID = 0;
            parameter = _parameter;
        }
    }
}