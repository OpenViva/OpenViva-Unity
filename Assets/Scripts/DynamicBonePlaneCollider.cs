using UnityEngine;

[AddComponentMenu("Dynamic Bone/Dynamic Bone Plane Collider")]
public class DynamicBonePlaneCollider : DynamicBoneCollider
{
    public enum Direction
    {
        X, Y, Z
    }

    public Direction direction = Direction.Y;

    public override void Collide(ref Vector3 particlePosition, float particleRadius)
    {
        Vector3 normal = Vector3.up;
        switch (direction)
        {
            case Direction.X:
                normal = transform.right;
                break;
            case Direction.Y:
                normal = transform.up;
                break;
            case Direction.Z:
                normal = transform.forward;
                break;
        }

        Vector3 p = transform.TransformPoint(center);
        Plane plane = new Plane(normal, p);
        float d = plane.GetDistanceToPoint(particlePosition);

        if (bound == Bound.Outside)
        {
            if (d < 0)
                particlePosition -= normal * d;
        }
        else
        {
            if (d > 0)
                particlePosition -= normal * d;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!enabled)
            return;

        if (bound == Bound.Outside)
            Gizmos.color = Color.yellow;
        else
            Gizmos.color = Color.magenta;

        Vector3 normal = Vector3.up;
        switch (direction)
        {
            case Direction.X:
                normal = transform.right;
                break;
            case Direction.Y:
                normal = transform.up;
                break;
            case Direction.Z:
                normal = transform.forward;
                break;
        }

        Vector3 p = transform.TransformPoint(center);
        Gizmos.DrawLine(p, p + normal);
    }
}
