using UnityEngine;
using UnityEngine.Events;


namespace viva
{


    public class SlidingDoor : MonoBehaviour
    {
        [Range(0.0f, 10.0f)]
        [SerializeField]
        private float unitsMax = 1.0f;
        public float maxUnitsRight { get { return unitsMax; } }
        [Range(0.0f, 10.0f)]
        [SerializeField]
        private float unitsMin = 1.0f;
        public float maxUnitsLeft { get { return unitsMin; } }
        [Header("")]
        [SerializeField]
        private float m_width = 1.0f;
        public float width { get { return m_width; } }
        [SerializeField]
        private float startUnit = 0.0f;
        [SerializeField]
        private UnityEvent onCollisionExit;

        public Vector3 startPos { get; private set; }
        public FilterUse filterUse { get; private set; } = new FilterUse();


        private void Awake()
        {
            startPos = transform.position;
        }

        private void Start()
        {
            transform.position += transform.right * width * startUnit;
        }


        public Vector3 GetUnitPosition(float unit)
        {
            return startPos + transform.right * width * unit;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.matrix = transform.localToWorldMatrix;

            Vector3 unit = Vector3.right * width;
            Vector3 anchor = Vector3.zero;

            Vector3 center = (anchor - unit * unitsMin + anchor + unit * (unitsMax + 1.0f)) / 2.0f;
            Vector3 size = new Vector3((unitsMax + unitsMin + 1) * width, 0.15f, 0.15f);
            Gizmos.DrawCube(center, size);
        }

        public void OnCollisionExit(Collision collision)
        {
            onCollisionExit.Invoke();
        }

        public float GetUnitDistanceFromStart()
        {
            return GetUnitDistanceFromStart(transform.position);
        }

        public float GetUnitDistanceFromStart(Vector3 worldPosOnDoorPlane)
        {   //point must be on sliding plane
            float diff = (worldPosOnDoorPlane - startPos).magnitude / width;
            Debug.DrawLine(worldPosOnDoorPlane, startPos, Color.white, 3.0f);
            float sign = Mathf.Sign(Vector3.Dot(worldPosOnDoorPlane - startPos, transform.right));
            return diff * sign;
        }

        public float GetUnitDistanceFromStart(SlidingDoor otherDoor)
        {
            //correct orientation
            float dot = Mathf.Sign(Vector3.Dot(transform.forward, otherDoor.transform.forward));
            if (dot < 0.0f)
            {
                return GetUnitDistanceFromStart(otherDoor.transform.position - transform.right * width);
            }
            else
            {
                return GetUnitDistanceFromStart(otherDoor.transform.position);
            }
        }
    }

}