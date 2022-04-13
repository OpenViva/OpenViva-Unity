using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace viva{

public class NatureDirector : MonoBehaviour{

    public static NatureDirector main;
    public static readonly int PLANT_BUCKETS = 4;
    private static readonly float GROWTH_STEP_TIME = 60.0f;

    [SerializeField]
    private Plant[] plants = new Plant[0];

    private float growthTimer = GROWTH_STEP_TIME-1.0f;
    private int plantBucketIndexActive = 0;

    private void Awake(){
        main = this;
    }

    private void Update(){
        UpdateGrowth();
    }

    public void UpdateGrowth(){
        growthTimer += Time.deltaTime;
        if( growthTimer < GROWTH_STEP_TIME ){
            return;
        }
        growthTimer = 0.0f;

        GrowAllPlantsInBucket( plantBucketIndexActive );
        plantBucketIndexActive = ( plantBucketIndexActive+1 )%PLANT_BUCKETS;
    }

    private void GrowAllPlantsInBucket( int index ){
        int bucketSize = Mathf.CeilToInt( (float)plants.Length/PLANT_BUCKETS );
        int bucketStart = index*bucketSize;
        int bucketEnd = Mathf.Min( bucketStart+bucketSize, plants.Length );
        for( int i=bucketStart; i<bucketEnd; i++ ){
            plants[i].Grow( 1.0f );
        }
    }
}

}