using UnityEngine;


namespace viva
{


    public class FingerAnimator : MonoBehaviour
    {

        [SerializeField]
        public Transform wrist;
        [SerializeField]
        public Transform targetBone;
        [SerializeField]
        public Transform[] fingers = new Transform[15];

        public Transform hand { get { return transform; } }


        public void ApplyBlendedFingerPose(Quaternion[] targetLocalRotations, float blend = 1.0f)
        {

            for (int i = 0; i < 15; i++)
            {
                fingers[i].localRotation = Quaternion.LerpUnclamped(fingers[i].localRotation, targetLocalRotations[i], blend);
            }
        }
        public void ApplyFingerTargetBlend(Quaternion[] targetLocalRotations, float[] blendArray, int blendArrayOffset)
        {

            int f = 0;
            for (int i = blendArrayOffset; i < blendArrayOffset + 5; i++)
            {
                fingers[f].localRotation = Quaternion.LerpUnclamped(fingers[f].localRotation, targetLocalRotations[f], blendArray[i]);
                f++;
                fingers[f].localRotation = Quaternion.LerpUnclamped(fingers[f].localRotation, targetLocalRotations[f], blendArray[i]);
                f++;
                fingers[f].localRotation = Quaternion.LerpUnclamped(fingers[f].localRotation, targetLocalRotations[f], blendArray[i]);
                f++;
            }
        }
    }

}