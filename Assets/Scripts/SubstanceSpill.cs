using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace viva{


public class SubstanceSpill : MonoBehaviour {

	public enum Substance{
		FLOUR,
        RAW_GRAIN,
        EGG,
        WATER,
        MILK
    };

    [SerializeField]
    public Substance substance;
    [SerializeField]
    private SoundSet spillContactSound;
    [SerializeField]
    private Container m_sourceContainer;
    public Container sourceContainer { get{ return m_sourceContainer; } }
    [SerializeField]
    private GameObject substanceCollisionCallbackPrefab;

    public List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();
    private float lastSpillAudioSourceTime = 0.0f;
    private SoundSet.StableSoundRandomizer spillSoundRandomizer;
    private float m_activeSpillAmount;
    public float activeSpillAmount { get{ return m_activeSpillAmount; } }
    private bool instantFill = false;
    private float lastCollisionCheckTime = 0.0f;

    public void ConsumeSpill( Container container ){
        m_activeSpillAmount = 0.0f;
        OnConsumeSpill( container );
    }

    protected virtual void OnConsumeSpill( Container container ){}

    //can only spill in whole integers
    public void BeginInstantSpill( int amount, bool spawnCallbackEntities = true ){
        instantFill = true;
        if( amount <= 0 ){
            return;
        }
        m_activeSpillAmount = amount;
        if( !spawnCallbackEntities ){
            return;
        }
        for( int i=0; i<8; i++ ){
            Vector3 offset = transform.right*(-3.0f+i)*0.02f;
            GameObject callbackEntity = GameObject.Instantiate( substanceCollisionCallbackPrefab, transform.position+offset, Quaternion.identity );
            SubstanceCollisionCallback callback = callbackEntity.GetComponent<SubstanceCollisionCallback>();
            callback.rigidBody.velocity = transform.forward*1.5f*Random.value;
            callback.sourceSubstanceSpill = this;
        }
    }

    public void BeginContinuousSpill( float amount ){
        m_activeSpillAmount = amount;
        instantFill = false;
    }
    private IEnumerator DestroySoundSource( GameObject source ){
        yield return new WaitForSeconds(2.0f);
        Destroy( source );
    }

    private void CheckCharacterItemContact( Item targetItem, Vector3 particlePosition ){
        CharacterCollisionCallback callback = targetItem.GetComponent<CharacterCollisionCallback>();
        if( callback == null ){
            return;
        }
        if( callback.collisionPart == CharacterCollisionCallback.Type.HEAD ){
            Loli loli = targetItem.mainOwner as Loli;
            if( loli ){
                loli.passive.environment.AttemptReactToSubstanceSpill( substance, particlePosition );
            }
        }
    }

    public void PlaySpillContactSound( Vector3 position ){
        if( Time.time-lastSpillAudioSourceTime < 0.2f ){
            return;
        }
        
        if( spillContactSound != null ){
            if( spillSoundRandomizer == null ){
                spillSoundRandomizer = new SoundSet.StableSoundRandomizer( spillContactSound );
            }
            SoundManager.main.RequestHandle( transform.position ).Play( spillContactSound.sounds[ spillSoundRandomizer.GetNextStableRandomIndex() ] );
        }
        lastSpillAudioSourceTime = Time.time;
    }
}

}