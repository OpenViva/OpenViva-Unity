using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

public class Sink: Mechanism {

	public static readonly int WATER_LAYERS = 4;

	[SerializeField]
	private ParticleSystem flowPsys;
	[SerializeField]
	private AudioSource spigotAudioSource;

	private float hotWaterFill = 0.0f;
	private float coldWaterFill = 0.0f;

	
    public override bool AttemptCommandUse( Loli targetLoli, Character commandSource ){
		return false;
	}

	public override void EndUse( Character targetCharacter ){
	}

	public override void OnItemRotationChange( Item item, float newPercentRotated ){
		if( item.name.Contains("hot") ){
			hotWaterFill = newPercentRotated;
		}else{
			coldWaterFill = newPercentRotated;
		}
		UpdateSpigotFlowSize();
	}

	private void UpdateSpigotFlowSize(){
		float flowScale = hotWaterFill+coldWaterFill;
		if( flowScale == 0.0f ){
			spigotAudioSource.Stop();
			SetFlowEmission( false );
			GameDirector.mechanisms.Remove( this );
			return;
		}
		SetFlowEmission( true );
		GameDirector.mechanisms.Add( this );
		flowScale = Mathf.Clamp( hotWaterFill+coldWaterFill, 0.1f, 1.0f );
		flowPsys.transform.localScale = new Vector3( flowScale, flowScale, 1.0f );
		if( !spigotAudioSource.isPlaying ){
			spigotAudioSource.Play();
		}
		spigotAudioSource.pitch = 0.8f+flowScale*0.4f;
	}

	public override void OnMechanismFixedUpdate(){
		if( !GamePhysics.GetRaycastInfo( transform.position, Vector3.down, 0.5f, Instance.itemsMask, QueryTriggerInteraction.Collide ) ){
			return;
		}
		Container container = Tools.SearchTransformAncestors<Container>( GamePhysics.result().collider.transform );
		if( container != null ){
			container.AttemptReceiveSubstanceSpill( SubstanceSpill.Substance.WATER, Time.fixedDeltaTime );
		}
	}

	private void SetFlowEmission( bool enable ){
		var emissionModule = flowPsys.emission;
		emissionModule.enabled = enable;
	}
}

}