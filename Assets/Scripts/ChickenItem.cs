using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace viva{

public class ChickenItem : Item
{
    [SerializeField]
    private Chicken parentMechanism;
    [SerializeField]
    private bool m_tamed = false;
	[VivaFileAttribute]
    public bool tamed { get{ return m_tamed; } protected set{ m_tamed = value; } }
    [SerializeField]
    private GameObject tameFX;
    private int activeEggCount = 0;
    private float lastEggSpawnTime;

    private static bool firstLoadHint = false;
    
    protected override void OnItemAwake(){
        base.OnItemAwake();
        parentMechanism.OnMechanismAwake();

        //TODO: Merge Item and mechanism together. Currently item must be top of the list of components else VivaSessionAsset wont find this component to awake it
    }

    public override void OnPreDrop(){
        parentMechanism.OnDropped();
    }

    public override void OnPostPickup(){
        if( !m_tamed ){
            Player player = mainOwner as Player;
            if( player && !firstLoadHint ){
                firstLoadHint = true;
                player.pauseMenu.DisplayHUDMessage("Place the chicken on the ground for eggs", true, PauseMenu.HintType.HINT_NO_IMAGE);
            }
            m_tamed = true;
            tameFX.SetActive( true );
            SoundManager.main.RequestHandle( transform.position ).PlayOneShot( parentMechanism.chickenSettings.tameSound );
            parentMechanism.UpdateTamedStatus();
            lastEggSpawnTime = Time.time-28.0f;
        }
        parentMechanism.OnPickedUp();
    }

    public void OnEggDestroyed( Vector3 eggSplatPos ){
        activeEggCount--;
    }
    public void OnEggSourceSet(){
        activeEggCount++;
    }

    public void UpdateEggTimer(){
        if( activeEggCount < 3 ){
            if( Time.time-lastEggSpawnTime > 2.0f ){
                lastEggSpawnTime = Time.time;

                GameObject egg = GameObject.Instantiate( parentMechanism.chickenSettings.eggPrefab, transform.position, transform.rotation );
                Egg eggScript = egg.GetComponent<Egg>();
                if( eggScript != null ){
                    eggScript.sourceChickenItem = this;
                }
                SoundManager.main.RequestHandle( transform.position ).PlayOneShot( parentMechanism.chickenSettings.eggSpawnSound );
            }
        }
    }
}

}