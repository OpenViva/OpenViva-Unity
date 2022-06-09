using UnityEditor;
using UnityEngine;


namespace viva
{


    public class Waypoints : MonoBehaviour
    {

        public enum Action
        {
            NONE = 0,
            SIT = 1,
            SWEEP = 2
        }

        [System.Serializable]
        public class Node
        {
            public Vector3 position;
            public Action action = Action.NONE;
            public GameObject target;
            public float radius = 1.5f;
            public int[] links = new int[0];
        }

        [SerializeField]
        private Node[] m_nodes = new Node[0];
        public Node[] nodes { get { return m_nodes; } }


        public int FindNearestWaypoint(Vector3 pos)
        {
            float radius = Mathf.Infinity;
            int result = 0;
            for (int i = 0; i < nodes.Length; i++)
            {
                float sqDist = Vector3.SqrMagnitude(transform.TransformPoint(nodes[i].position) - pos);
                if (sqDist < radius)
                {
                    radius = sqDist;
                    result = i;
                }
            }
            return result;
        }

        public void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1.0f, 1.0f, 0.0f, 0.4f);
            GUIStyle style = new GUIStyle();
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.UpperCenter;
            int circleResolution = 10;
            Quaternion twist = Quaternion.Euler(0.0f, 360.0f / circleResolution, 0.0f);
            for (int j = 0, i = 0; i < nodes.Length; j = i++)
            {
                var node = nodes[i];

                Vector3 iPos = transform.TransformPoint(node.position);
                if (node.links.Length == 0)
                {
                    style.normal.textColor = Color.red;
                }
                else
                {
                    style.normal.textColor = Color.yellow;
                }
#if UNITY_EDITOR
                Handles.Label(transform.TransformPoint(node.position), "" + i, style);
#endif
                Vector3 dir = Vector3.forward * node.radius * 0.63662f;
                Vector3 lastPos = iPos + Vector3.forward * node.radius * 0.31831f - Vector3.right * node.radius;
                for (int k = 0; k < circleResolution; k++)
                {
                    dir = twist * dir;
                    Vector3 newPos = lastPos + dir;
                    Gizmos.DrawLine(lastPos, newPos);
                    lastPos = newPos;
                }

                for (int k = 0; k < node.links.Length; k++)
                {
                    var kNode = node.links[k];
                    int index = kNode;
                    if (index < 0 || index >= nodes.Length || index == i)
                    {
                        continue;
                    }
                    Vector3 kPos = transform.TransformPoint(nodes[kNode].position);
                    Vector3 ik = (kPos - iPos).normalized;
                    Vector3 adjIPos = iPos + ik * node.radius;
                    Vector3 adjKPos = kPos - ik * nodes[kNode].radius;

                    Vector3 right = Vector3.Cross(adjKPos - adjIPos, Vector3.up).normalized * 0.2f;
                    Gizmos.DrawLine(adjIPos, adjIPos + right);
                    Gizmos.DrawLine(adjIPos + right, adjKPos + right);
                    Gizmos.DrawLine(adjKPos + right, adjKPos);
                }
            }
        }
    }

}