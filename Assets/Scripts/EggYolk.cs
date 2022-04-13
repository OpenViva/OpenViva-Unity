using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

public class EggYolk : SubstanceSpill{
    
    [SerializeField]
    private Rigidbody[] rigidBodies;

    public Transform spawnParent = null;

    private Vector3 spawnPos;
    private int yolkBoneDrop = 0;
    private float yolkBoneDropTimer = 0.0f;
    private Transform consumeTarget = null;


    private void Awake(){
        spawnParent = transform.parent;
        spawnPos = transform.localPosition;
        transform.SetParent( null, true );
        BeginInstantSpill(1,false);
    }

    void LateUpdate(){
        if( yolkBoneDrop < rigidBodies.Length ){

            yolkBoneDropTimer += Time.deltaTime;
            if( yolkBoneDropTimer > 0.1f ){
                yolkBoneDropTimer %= 0.1f;

                Rigidbody rb = rigidBodies[ yolkBoneDrop ];
                rb.isKinematic = false;

                yolkBoneDrop++;
                if( yolkBoneDrop >= rigidBodies.Length ){
                    Destroy( gameObject, 2.0f );
                }
            }
        }
        if( consumeTarget != null ){
            for( int i=0; i<yolkBoneDrop; i++ ){
                var rb = rigidBodies[i];
                Vector3 sphereCenter = consumeTarget.TransformPoint( Vector3.up*0.3f );
                
                //clamp in sphere
                Vector3 diff = sphereCenter-rb.transform.position;
                rb.transform.position = sphereCenter-Vector3.ClampMagnitude( diff, 0.3f );
            }
        }
        if( spawnParent != null ){
            for( int i=yolkBoneDrop; i<rigidBodies.Length; i++ ){
                rigidBodies[i].transform.position = spawnParent.TransformPoint( spawnPos );
            }
        }
    }

    protected override void OnConsumeSpill( Container container ){
        consumeTarget = container.transform;
    }
}

}