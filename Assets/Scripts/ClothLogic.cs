using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

public class ClothLogic : MonoBehaviour{

    private readonly float far = 16f;
    public Cloth cloth;
    public SkinnedMeshRenderer smr;
    private float sqDist;

    private void OnBecameVisible(){
        if( cloth && sqDist <= far*far ) cloth.enabled = true;
    }

    private void OnBecameInvisible(){
        if( cloth ) cloth.enabled = false;
    }

    private void FixedUpdate(){
        if( !Camera.main || !smr || !cloth ) return;
        sqDist = Vector3.SqrMagnitude( smr.bounds.center-Camera.main.transform.position );
        if( sqDist > far*far ){
            cloth.enabled = false;
        }else if( smr.isVisible ){
            cloth.enabled = true;
        }
        cloth.clothSolverFrequency = Tools.RemapClamped( 0, far*far, 85, 10f, sqDist );
    }
}

}
