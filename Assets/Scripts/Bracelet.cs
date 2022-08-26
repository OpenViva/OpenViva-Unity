using UnityEngine;

namespace viva
{
    public class Bracelet : ClothingScript
    {

        public float height = 0.0f;
        public float velocity = 0.0f;
        private Transform wrist;

        public override void OnBeginWearing(Loli loli)
        {

            wrist = loli.bodyArmature.Find("spine1/spine2/spine3/shoulder_l/armControl_l/forearmControl_l/wrist_l");
            transform.SetParent(wrist, false);
            transform.localRotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
        }

        public Vector3 offset = new Vector3(0.0f, 0.0f, -0.0005f);

        public override void OnApplywearing()
        {

            height += velocity;
            if (height > 1.0f)
            {
                height = 1.0f;
                velocity = 0.0f;
            }
            else if (height < 0.0f)
            {
                height = 0.0f;
                velocity = 0.0f;
            }
            offset.y = height * 0.025f;
            transform.localPosition = offset;
            velocity += transform.forward.y * Time.deltaTime;
        }
    }

}