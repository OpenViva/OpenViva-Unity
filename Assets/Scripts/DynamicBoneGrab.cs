using UnityEngine;

namespace viva{


public class DynamicBoneGrab : DynamicBoneColliderBase{

    private Transform grabTransform;
    private DynamicBoneItem m_targetBoneItem = null;
    public DynamicBoneItem targetBoneItem { get{ return m_targetBoneItem; } }
    private float targetIndex = -1.0f;
    private float pickupTimer = 0.0f;
    private float cachedElasticity = 0.0f;

    public override void Collide( int particleIndex, ref Vector3 particlePosition, float particleRadius ){
        if( targetIndex > 0.0f ){ 
            float strength = Mathf.Clamp01( targetIndex-particleIndex );
            strength *= Mathf.Clamp01( pickupTimer/0.4f );
            
            particlePosition = Vector3.LerpUnclamped( particlePosition, grabTransform.position, strength );
        }
    }

    public void RecalculateGrabIndex(){

        if( grabTransform == null || targetBoneItem == null ){
            return;
        }
        var dynamicBone = targetBoneItem.dynamicBone;
        float oldTargetIndex = targetIndex;
        float newTargetIndex = -1;
        for( int j=0, i=1; i<dynamicBone.GetParticleCount(); j=i++ ){
            
            Vector3 iPos = dynamicBone.GetParticlePosition(i);
            Vector3 jPos = dynamicBone.GetParticlePosition(j);
            float boneLength = Vector3.SqrMagnitude( iPos-jPos );
            if( i == dynamicBone.GetParticleCount()-1 ){
                boneLength += 0.04f; //pad last bone length for grab leeway
            }
            float grabLength = Vector3.SqrMagnitude( grabTransform.position-jPos );
            if( grabLength <= boneLength ){
                newTargetIndex = i;
                break;
            }
        }
        float freezeStrength = 1.0f-( pickupTimer/0.6f );
        if( freezeStrength > 0 ){
            targetBoneItem.dynamicBone.FreezeAllParticles( freezeStrength );
        }

        pickupTimer += Time.deltaTime;
        if( newTargetIndex == -1 ){
            if( pickupTimer > 0.3f ){
                if( targetBoneItem.mainOccupyState ){
                    targetBoneItem.mainOccupyState.AttemptDrop();
                }
            }
        }else{
            targetIndex = Mathf.MoveTowards( oldTargetIndex, newTargetIndex, 0.2f );
        }
    }

    public float GetCurrentTargetIndex(){
        return targetIndex;
    }

    public void BeginGrabbing( Transform _grabTransform, DynamicBoneItem source ){

        grabTransform = _grabTransform;
        if( targetBoneItem != null ){
            targetBoneItem.dynamicBone.m_Colliders.Remove( this );
        }
        source.dynamicBone.m_Colliders.Add( this );
        cachedElasticity = source.dynamicBone.m_Elasticity;
        source.dynamicBone.m_Elasticity = 0.0f;
        m_targetBoneItem = source;
        targetIndex = source.dynamicBone.GetParticleCount()-1;
        pickupTimer = 0.0f;
    }

    public void StopGrabbing(){
        if( targetBoneItem != null ){
            targetBoneItem.dynamicBone.m_Colliders.Remove( this );
            targetBoneItem.dynamicBone.m_Elasticity = cachedElasticity;
            m_targetBoneItem = null;
            targetIndex = -1;
            GameObject.Destroy( this );
        }
    }
}

}