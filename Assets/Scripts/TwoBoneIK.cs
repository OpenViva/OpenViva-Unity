using UnityEngine;

namespace viva
{

    public class TwoBoneIK
    {

        public Transform p0;
        public float r0;
        public Quaternion offset0;
        public Transform p1;
        public float r1;
        public Quaternion offset1;


        public TwoBoneIK(Transform _p0, Quaternion _offset0, Transform _p1, Quaternion _offset1, Transform p2)
        {
            p0 = _p0;
            offset0 = _offset0;
            p1 = _p1;
            r0 = Vector3.Distance(p0.position, p1.position);
            r1 = Vector3.Distance(p1.position, p2.position);
            offset1 = _offset1;
        }

        public TwoBoneIK(TwoBoneIK copy)
        {
            p0 = copy.p0;
            r0 = copy.r0;
            offset0 = copy.offset0;
            p1 = copy.p1;
            r1 = copy.r1;
            offset1 = copy.offset1;
        }

        public void Solve(Vector3 target, Vector3 pole)
        {
            if (p0 == p1 || target == pole)
            {
                return;
            }
            Vector3 diff = target - p0.position;
            float d = diff.magnitude;
            Vector3 diffNorm = diff / d;
            float c = r0 * r0 + d * d - r1 * r1;
            float root = 4.0f * r0 * r0 * d * d - c * c;
            if (root <= 0.0f)
            {
                Vector3 up = (target - pole).normalized;
                ///TODO: reuse quaternion
                p0.rotation = Quaternion.LookRotation(diff, up) * offset0;
                p1.rotation = Quaternion.LookRotation(diff, -up) * offset1;
            }
            else
            {
                float ri = Mathf.Sqrt(root) / (2.0f * d);
                float sr0 = ri / Mathf.Tan(Mathf.Acos(c / (2.0f * r0 * d)));
                Vector3 planeCenter = p0.position + diffNorm * sr0;

                Vector3 projPole = planeCenter + Vector3.ProjectOnPlane(pole - planeCenter, diffNorm);
                projPole = planeCenter + (projPole - planeCenter).normalized * ri;

                p0.rotation = Quaternion.LookRotation(projPole - p0.position, diffNorm) * offset0;
                p1.rotation = Quaternion.LookRotation(target - projPole, diffNorm) * offset1;
            }
        }
    }

}