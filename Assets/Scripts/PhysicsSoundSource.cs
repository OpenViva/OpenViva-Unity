using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{


public partial class PhysicsSoundSource : MonoBehaviour {
	
	[SerializeField]
	public _InternalSerializedSoundGroup collisionSoundsSoft;
	[SerializeField]
	public _InternalSerializedSoundGroup collisionSoundsHard;
	[SerializeField]
	public Rigidbody[] ignoreSoundsFrom = new Rigidbody[0];
	[SerializeField]
	public AudioClip dragSoundLoop;
	[SerializeField]
	public PhysicsSoundSourceSettings settings;
	[SerializeField]
	private bool ignoreTerrain = false;

	private float lastCollisionSoundTime;
	private Coroutine dragCoroutine = null;
	private bool isDragging = false;
	private Rigidbody rigidBody;
	public bool playSounds = true;
	

	private void Awake(){
		rigidBody = gameObject.GetComponent<Rigidbody>();
	}

    protected virtual void OnCollisionEnter( Collision collision ){
		
		if( !playSounds ) return;
		//ignore collisions near start of game
		if( Time.time < 1.0f ){
			return;
		}
		if( collision.collider as TerrainCollider && ignoreTerrain ){
			return;
		}
		if( Time.time-lastCollisionSoundTime < 0.15f ){
			return;
		}
		if( System.Array.Exists( ignoreSoundsFrom, elem=>elem==collision.rigidbody ) ){
			return;
		}
		var avgPos = WorldUtil.AverageContactPosition( collision, 2 );
		if( !avgPos.HasValue ){
			return;
		}
		SimulateContact( avgPos.Value, collision.relativeVelocity.sqrMagnitude );
    }

	public void SimulateContact( Vector3 position, float relativeVelocitySqMag ){
		if( settings == null ){
			Debug.LogError("[PhysicsSoundSource] Missing settings "+name);
			return;
		}

		if( relativeVelocitySqMag > settings.hardMinVel*settings.hardMinVel && collisionSoundsHard != null ){
			var handle = Sound.Create( position );
			handle.pitch = 0.8f+UnityEngine.Random.value*0.4f;
			handle.PlayOneShot( collisionSoundsHard.GetRandomAudioClip() );

			lastCollisionSoundTime = Time.time;
		}else  if( relativeVelocitySqMag > settings.softMinVel*settings.softMinVel && collisionSoundsSoft != null ){
			var handle = Sound.Create( position );
			handle.PlayOneShot( collisionSoundsSoft.GetRandomAudioClip() );

			float vel = Mathf.Sqrt( relativeVelocitySqMag );
			float t = (vel-settings.softMinVel)/( settings.hardMinVel-settings.softMinVel );
			handle.volume = Mathf.Min( 1.0f, t );
			handle.pitch = Mathf.LerpUnclamped( settings.softMinPitch, settings.softMaxPitch, t );

			lastCollisionSoundTime = Time.time;
		}
	}

	protected void OnCollisionStay( Collision collision ){
		
		if( settings == null || dragSoundLoop == null ){
			return;
		}
		//ignore collisions near start of game
		if( Time.time < 2.0f ){
			return;
		}
		if( rigidBody == null ){
			Debug.LogError("[PhysicsSoundSource] missing rigidBody reference for "+name);
			return;
		}
		var relativeVel = collision.rigidbody ? collision.rigidbody.velocity : Vector3.zero;
		if( ( rigidBody.velocity-relativeVel ).sqrMagnitude > settings.dragMinVel*settings.dragMinVel ){
			isDragging = true;
			if( dragCoroutine == null ){
				dragCoroutine = StartCoroutine( Drag() );
			}
		}
	}

	private IEnumerator Drag(){
		
		var handle = Sound.Create( Vector3.zero, transform );
		handle.Play( dragSoundLoop );
		handle.loop = true;

		while( isDragging ){
			float vel = rigidBody.velocity.magnitude;
			handle.pitch = Mathf.LerpUnclamped( settings.dragMinPitch, settings.dragMaxPitch, Tools.GetClampedRatio( settings.dragMinVel, settings.dragMaxVel, vel ) );
			handle.volume = Mathf.Clamp01( vel/( settings.dragMaxVolumeVel) );
			isDragging = false;
			yield return new WaitForFixedUpdate();
			yield return new WaitForFixedUpdate();
		}

		handle.Stop();

		dragCoroutine = null;
	}
}

}