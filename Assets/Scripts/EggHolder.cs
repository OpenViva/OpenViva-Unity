using System.Collections;
using UnityEngine;

namespace viva
{


    public class EggHolder : MonoBehaviour
    {

        [Range(0.005f, 0.1f)]
        [SerializeField]
        private float slotWidth = 0.0650f;
        [Range(0.0f, 0.2f)]
        [SerializeField]
        private float slotHeight = 0.125f;

        private Egg[] slots = new Egg[12];
        private int slotsUsed = 0;

        public void RemoveFromSlot(int index)
        {
            if (slots[index] != null)
            {
                slots[index] = null;
            }
        }

        public void AddToSlot(Vector3 localSlotPos, int index, Egg egg)
        {
            if (slots[index] == null)
            {
                slots[index] = egg;
                GameDirector.instance.StartCoroutine(PlaceEggAnimation(localSlotPos, egg));
            }
        }

        public Vector3 FindNearestFreeSlotLocalPos(Vector3 localPos, ref int index)
        {
            Vector3 localSlot = Vector3.zero;
            float leastSqDist = slotWidth * slotWidth;
            index = -1;
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    int candidateIndex = i * 2 + j;
                    if (slots[candidateIndex] != null)
                    {
                        continue;
                    }
                    Vector3 center = new Vector3((-2.5f + j) * slotWidth, slotHeight, (-0.5f + i) * slotWidth);
                    float sqDist = Vector3.SqrMagnitude(center - localPos);
                    if (sqDist < leastSqDist)
                    {
                        leastSqDist = sqDist;
                        localSlot = center;
                        index = candidateIndex;
                    }
                }
            }
            return localSlot;
        }

        private void OnDrawGizmosSelected()
        {

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.green;
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    Vector3 center = new Vector3((-2.5f + j) * slotWidth, slotHeight, (-0.5f + i) * slotWidth);
                    Gizmos.DrawWireSphere(center, 0.01f);
                }
            }
        }

        private IEnumerator PlaceEggAnimation(Vector3 localSlotPos, Egg egg)
        {
            const float animDuration = 0.2f;    //seconds
            TransformBlend animBlend = new TransformBlend();
            Debug.DrawLine(transform.TransformPoint(localSlotPos), transform.TransformPoint(localSlotPos) + Vector3.up, Color.green, 3.0f);
            animBlend.SetTarget(true, egg.transform, false, false, 0.0f, 1.0f, animDuration);

            egg.rigidBody.velocity = Vector3.zero;
            egg.rigidBody.angularVelocity = Vector3.zero;
            while (!animBlend.blend.finished)
            {
                animBlend.Blend(transform.TransformPoint(localSlotPos), Quaternion.Euler(-90.0f, 0.0f, 0.0f));
                if (egg == null)
                {
                    yield break;
                }
                yield return null;
            }
            egg.rigidBody.Sleep();
        }
    }

}