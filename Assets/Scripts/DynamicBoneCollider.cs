using UnityEngine;

public class DynamicBoneCollider : MonoBehaviour
{

#if UNITY_5
    [Tooltip("The center of the sphere or capsule, in the object's local space.")]
#endif
    public Vector3 center = Vector3.zero;

    public enum Bound
    {
        Outside,
        Inside
    }

#if UNITY_5
    [Tooltip("Constrain bones to outside bound or inside bound.")]
#endif
    public Bound bound = Bound.Outside;

    public virtual void Collide(ref Vector3 particlePosition, float particleRadius)
    {
    }
}
