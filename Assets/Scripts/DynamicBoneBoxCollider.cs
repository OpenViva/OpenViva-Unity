using UnityEngine;

[AddComponentMenu("Dynamic Bone/Dynamic Bone Box Collider")]
public class DynamicBoneBoxCollider : DynamicBoneCollider
{

    public Vector3 size = Vector3.zero;



    public override void Collide( ref Vector3 particlePosition, float particleRadius ){

        var local = transform.InverseTransformPoint( particlePosition );
        local -= center;

        var scale = Mathf.Max( Mathf.Epsilon, transform.lossyScale.x );
        var realSize = size/2f;
        realSize.x /= scale;
        realSize.y /= scale;
        realSize.z /= scale;

        if( Mathf.Abs( local.x ) < realSize.x+particleRadius && Mathf.Abs( local.y ) < realSize.y+particleRadius && Mathf.Abs( local.z ) < realSize.z+particleRadius ){
            
            Vector3 edgeDist;
            edgeDist.x = realSize.x-Mathf.Abs( local.x-realSize.x );
            edgeDist.y = realSize.y-Mathf.Abs( local.y-realSize.y );
            edgeDist.z = realSize.z-Mathf.Abs( local.z-realSize.z );
            float biggest = Mathf.Max( edgeDist.x, Mathf.Max( edgeDist.y, edgeDist.z ) );
            if( biggest == edgeDist.x ){
                if( local.x > 0 ){
                    local.x = Mathf.Max( local.x, realSize.x+particleRadius );
                }else{
                    local.x = Mathf.Min( local.x, -realSize.x-particleRadius );
                }
            }else if( biggest == edgeDist.y ){
                if( local.y > 0 ){
                    local.y = Mathf.Max( local.y, realSize.y+particleRadius );
                }else{
                    local.y = Mathf.Min( local.y, -realSize.y-particleRadius );
                }
            }else{
                if( local.z > 0 ){
                    local.z = Mathf.Max( local.z, realSize.z+particleRadius );
                }else{
                    local.z = Mathf.Min( local.z, -realSize.z-particleRadius );
                }
            }
        }

        local += center;
        particlePosition = transform.TransformPoint( local );
    }

    void OnDrawGizmosSelected()
    {
        if (!enabled)
            return;

        if (bound == Bound.Outside)
            Gizmos.color = Color.yellow;
        else
            Gizmos.color = Color.magenta;

        Gizmos.matrix = transform.localToWorldMatrix;
        var scale = Mathf.Max( Mathf.Epsilon, transform.lossyScale.x );
        Gizmos.DrawWireCube( center, size/scale );

        Gizmos.matrix = Matrix4x4.identity;
    }
}
