using UnityEngine;

namespace viva
{


    public class NavSearchSquare : MonoBehaviour
    {

        public class NearestEdge
        {
            public float distance;
            public Vector3 edgeA;
            public Vector3 edgeB;
            public Vector3 edgeNormal;
        }

        public enum Side
        {
            POS_X = 1,
            NEG_X = 2,
            POS_Z = 4,
            NEG_Z = 8
        }

        [SerializeField]
        private BoxCollider targetBounds;
        [HideInInspector]
        [SerializeField]
        private int sideMask = 1;
        [SerializeField]
        private float offset = 0.2f;
        [Range(-2.0f, 2.0f)]
        [SerializeField]
        private float shrinkPercent = 0.2f;


        public void OnDrawGizmosSelected()
        {
            if (targetBounds == null)
            {
                return;
            }
            Gizmos.color = Color.green;

            Vector3 min = targetBounds.center - targetBounds.size * 0.5f;
            min.y += 0.05f;
            Vector3 max = targetBounds.center + targetBounds.size * 0.5f;
            max.y += 0.05f;
            Gizmos.DrawLine(min, max);
            if ((sideMask & (int)Side.POS_X) != 0)
            {
                Tools.GizmoArrow(
                    transform.TransformPoint(new Vector3(max.x + offset, min.y, max.z)),
                    transform.TransformPoint(new Vector3(max.x + offset, min.y, min.z)), shrinkPercent
                );
            }
            if ((sideMask & (int)Side.NEG_X) != 0)
            {
                Tools.GizmoArrow(
                    transform.TransformPoint(new Vector3(min.x - offset, min.y, min.z)),
                    transform.TransformPoint(new Vector3(min.x - offset, min.y, max.z)), shrinkPercent
                );
            }
            if ((sideMask & (int)Side.POS_Z) != 0)
            {
                Tools.GizmoArrow(
                    transform.TransformPoint(new Vector3(min.x, min.y, max.z + offset)),
                    transform.TransformPoint(new Vector3(max.x, min.y, max.z + offset)), shrinkPercent
                );
            }
            if ((sideMask & (int)Side.NEG_Z) != 0)
            {
                Tools.GizmoArrow(
                    transform.TransformPoint(new Vector3(max.x, min.y, min.z - offset)),
                    transform.TransformPoint(new Vector3(min.x, min.y, min.z - offset)), shrinkPercent
                );
            }
        }

        private Vector2 GetXZ(Vector3 v)
        {
            return new Vector2(v.x, v.z);
        }

        public NearestEdge GetNearestXZEdge(Vector3 from)
        {

            Vector3 min = targetBounds.center - targetBounds.size * 0.5f;
            Vector3 max = targetBounds.center + targetBounds.size * 0.5f;

            Vector3 minXminZ = transform.TransformPoint(new Vector3(min.x, max.y, min.z));
            Vector3 minXmaxZ = transform.TransformPoint(new Vector3(min.x, max.y, max.z));
            Vector3 maxXminZ = transform.TransformPoint(new Vector3(max.x, max.y, min.z));
            Vector3 maxXmaxZ = transform.TransformPoint(new Vector3(max.x, max.y, max.z));

            from.y = minXmaxZ.y;

            NearestEdge result = new NearestEdge();

            float shortestSq = Mathf.Infinity;
            int mask = 1;
            for (int i = 0; i < 4; i++)
            {
                if ((sideMask & mask) == 0)
                {
                    mask <<= 1;
                    continue;
                }
                Vector3 testEdgeA;
                Vector3 testEdgeB;
                switch (mask)
                {
                    case (int)Side.POS_X:
                        testEdgeA = maxXmaxZ;
                        testEdgeB = maxXminZ;
                        break;
                    case (int)Side.NEG_X:
                        testEdgeA = minXminZ;
                        testEdgeB = minXmaxZ;
                        break;
                    case (int)Side.POS_Z:
                        testEdgeA = maxXmaxZ;
                        testEdgeB = minXmaxZ;
                        break;
                    default:    //Side.NEG_Z:
                        testEdgeA = minXminZ;
                        testEdgeB = maxXminZ;
                        break;
                }
                Vector3 normLineDir = (testEdgeB - testEdgeA).normalized;

                float sqDist = Tools.SqDistanceToLine(testEdgeA, normLineDir, from);
                if (sqDist < shortestSq)
                {
                    shortestSq = sqDist;
                    //shrink edge
                    Vector3 mid = (testEdgeA + testEdgeB) / 2.0f;
                    testEdgeA = mid + (testEdgeA - mid) * (1.0f - shrinkPercent);
                    testEdgeB = mid + (testEdgeB - mid) * (1.0f - shrinkPercent);

                    result.edgeA = testEdgeA;
                    result.edgeB = testEdgeB;
                    result.edgeNormal = Vector3.Cross(Vector3.up, normLineDir);
                }
                mask <<= 1;
            }
            result.distance = Mathf.Sqrt(shortestSq);
            return result;
        }

        public LocomotionBehaviors.NavSearchLine[] CreateSquareNavSearchLines(bool fromOutside)
        {

            float InOrOut = System.Convert.ToInt32(fromOutside) * 2 - 1;

            Vector3 min = targetBounds.center - targetBounds.size * 0.5f;
            min.y += 0.05f;
            Vector3 max = targetBounds.center + targetBounds.size * 0.5f;
            max.y += 0.05f;

            int sides = 0;
            sides += System.Convert.ToInt32((sideMask & (int)Side.POS_X) != 0);
            sides += System.Convert.ToInt32((sideMask & (int)Side.NEG_X) != 0);
            sides += System.Convert.ToInt32((sideMask & (int)Side.POS_Z) != 0);
            sides += System.Convert.ToInt32((sideMask & (int)Side.NEG_Z) != 0);

            LocomotionBehaviors.NavSearchLine[] navSearchLines = new LocomotionBehaviors.NavSearchLine[sides];
            sides = 0;
            if ((sideMask & (int)Side.POS_X) != 0)
            {
                navSearchLines[sides++] = new LocomotionBehaviors.NavSearchLine(
                    transform.TransformPoint(new Vector3(max.x + offset, min.y, max.z)),
                    transform.TransformPoint(new Vector3(max.x + offset, min.y, min.z)),
                    targetBounds.size.y + 0.2f,
                    4,
                    shrinkPercent
                );
            }
            if ((sideMask & (int)Side.NEG_X) != 0)
            {
                navSearchLines[sides++] = new LocomotionBehaviors.NavSearchLine(
                    transform.TransformPoint(new Vector3(min.x - offset, min.y, min.z)),
                    transform.TransformPoint(new Vector3(min.x - offset, min.y, max.z)),
                    targetBounds.size.y + 0.2f,
                    4,
                    shrinkPercent
                );
            }
            if ((sideMask & (int)Side.POS_Z) != 0)
            {
                navSearchLines[sides++] = new LocomotionBehaviors.NavSearchLine(
                    transform.TransformPoint(new Vector3(min.x, min.y, max.z + offset)),
                    transform.TransformPoint(new Vector3(max.x, min.y, max.z + offset)),
                    targetBounds.size.y + 0.2f,
                    4,
                    shrinkPercent
                );
            }
            if ((sideMask & (int)Side.NEG_Z) != 0)
            {
                navSearchLines[sides++] = new LocomotionBehaviors.NavSearchLine(
                    transform.TransformPoint(new Vector3(max.x, min.y, min.z - offset)),
                    transform.TransformPoint(new Vector3(min.x, min.y, min.z - offset)),
                    targetBounds.size.y + 0.2f,
                    4,
                    shrinkPercent
                );
            }
            return navSearchLines;
        }
    }

}