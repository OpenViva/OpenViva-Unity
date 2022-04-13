using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plant : MonoBehaviour{

    [SerializeField]
    private Transform[] fruitSpawnTransforms;

    [SerializeField]
    private GameObject fruitPrefab;

    private int spikesActive = 0;
    private float growthPercent = 0.0f;

    public void Grow( float percent ){
 
        growthPercent = Mathf.Min( 1.0f, growthPercent+percent );
        if( growthPercent < 1.0f ){
            return;
        }

        if( spikesActive == fruitSpawnTransforms.Length ){
            return;
        }
        int randomGrowths = Random.Range( 0, fruitSpawnTransforms.Length-1-spikesActive );

        for( int i=0; i<randomGrowths; i++ ){
            Transform targetSpikeTransform = fruitSpawnTransforms[ spikesActive+i ];

            GameObject wheatSpike = GameObject.Instantiate( fruitPrefab, targetSpikeTransform.position, targetSpikeTransform.rotation );
        }

        spikesActive += randomGrowths;
    }
}