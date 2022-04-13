using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace viva{

public class Firework: Item {

    [Header("Firework Logic")]
    [SerializeField]
    private Fuse fuse;
    [SerializeField]
    private SoundSet propelSound;
    [SerializeField]
    private float propelForce = 10.0f;
    [SerializeField]
    private GameObject propelFXContainer;
    [SerializeField]
    private MeshRenderer rocketMR;
    [SerializeField]
    private GameObject explosionPrefab;

    private float timeStart = 0.0f;
    private float timeToExplode = 0.0f;


    public override void OnItemLateUpdate(){
        if( mainOccupyState != null ){
            PlayerHandState playerHandState = mainOccupyState as PlayerHandState;
            if( playerHandState && playerHandState.actionState.isDown ){
                fuse.enabled = true;
            }
        }
        if( timeStart != 0.0f ){
            rigidBody.AddForce( transform.up*propelForce, ForceMode.Force );
            if( Time.time-timeStart > timeToExplode ){
                Explode();
                
            }
        }
    }

    private void Explode(){
        DisableItemLogic();
        Destroy( gameObject );

        GameObject.Instantiate( explosionPrefab, transform.position, transform.rotation );
    }

    public void Fire(){
        if( timeStart != 0.0f ){
            return;
        }
        if( mainOccupyState ){
            mainOccupyState.AttemptDrop();
        }
        timeStart = Time.time;
        EnableItemLogic();
        propelFXContainer.SetActive( true );

        var handle = SoundManager.main.RequestHandle( Vector3.zero, transform );
        var clip = propelSound.GetRandomAudioClip();
        handle.Play( clip );
        timeToExplode = clip.length;
    }
}

}