using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace viva{

public enum SpeechBubble{
	EXCLAMATION,
	INTERROGATION,
	FULL
}

public partial class GameDirector : MonoBehaviour {


	public static Player player { get; private set; }
	private static Set<Character> m_characters = new Set<Character>();
	public static Set<Character> characters { get{ return m_characters; } }

	[SerializeField]
	private Player m_player;
	[SerializeField]
	private GameObject loliPrefab;
	[SerializeField]
	private Transform loliPool;
	[SerializeField]
	public Transform loliRespawnPoint;
	[SerializeField]
	private PoseCache m_loliBasePose;
	public PoseCache loliBasePose { get{ return m_loliBasePose; } }
	[SerializeField]
	private LoliSettings m_allLoliSettings;
	public LoliSettings loliSettings { get{ return m_allLoliSettings; } }
	[SerializeField]
	private Texture2D[] speechBubbleTextures = new Texture2D[ System.Enum.GetValues(typeof(SpeechBubble)).Length ];
	
	[SerializeField]
	private PhysicMaterial m_stickyPhysicsMaterial;
	public PhysicMaterial stickyPhysicsMaterial { get{ return m_stickyPhysicsMaterial; } }
	[SerializeField]
	private PhysicMaterial m_slipperyPhysicsMaterial;
	public PhysicMaterial slipperyPhysicsMaterial { get{ return m_slipperyPhysicsMaterial; } }


	public Texture2D GetSpeechBubbleTexture( SpeechBubble bubble ){
		return speechBubbleTextures[ (int)bubble ];
	}


	public Loli GetLoliFromPool(){
		if( loliPool.childCount == 0 ){
			var container = GameObject.Instantiate( loliPrefab, Vector3.zero, Quaternion.identity );
			var newLoli = container.GetComponent<Loli>();
			return newLoli;
		}
		var result = loliPool.GetChild(0);
		result.transform.SetParent( null, true );
		return result.GetComponent<Loli>();;
	}


	public T FindClosestCharacter<T>( Vector3 point, float radius ){
		radius *= radius;
		T result = default(T);
		for( int i=0; i<m_characters.objects.Count; i++ ){
			Character candidate = m_characters.objects[i];
			float sqDist = Vector3.SqrMagnitude( candidate.head.position-point );
			if( sqDist < radius && candidate.GetType() == typeof(T) ){
				try{
					result = (T)System.Convert.ChangeType( candidate, typeof(T) );
					radius = sqDist;
				}catch{
					Debug.LogError("ERROR Object could not be cast!");
				}
			}
		}
		return result;
	}

	
	public Player FindNearbyPlayer( Vector3 position, float distance ){

		Character nearby = GameDirector.instance.FindClosestCharacter<Player>(
			position,
			distance
		);
		return nearby as Player;
	}

	public Loli FindClosestGeneralDirectionLoli( Transform source, float maxBearing=45.0f ){
		float least = Mathf.Infinity;
		Loli result = null;
		for( int i=0; i<characters.objects.Count; i++ ){
			Loli loli = characters.objects[i] as Loli;
			if( loli == null ){
				continue;
			}
			if( !loli.CanSeePoint( source.position ) ){
				continue;
			}
			Vector3 headPos = loli.head.position;
			float bearing = Tools.Bearing( source, headPos );
			if( bearing > maxBearing ){
				continue;
			}
			float sqDist = Vector3.SqrMagnitude( headPos-source.position );
			if( sqDist < least ){
				least = sqDist;
				result = loli;
			}
		}
		return result;
	}
	
	public List<Loli> FindGeneralDirectionLolis( Transform source, float maxBearing=45.0f, float maxDist=15.0f ){
		maxDist *= maxDist;
		List<Loli> result = new List<Loli>();
		for( int i=0; i<characters.objects.Count; i++ ){
			Loli loli = characters.objects[i] as Loli;
			if( loli == null ){
				continue;
			}
			if( !loli.CanSeePoint( source.position ) ){
				continue;
			}
			Vector3 headPos = loli.head.position;
			float bearing = Tools.Bearing( source, headPos );
			if( bearing > maxBearing ){
				continue;
			}
			float sqDist = Vector3.SqrMagnitude( headPos-source.position );
			if( sqDist < maxDist ){
				result.Add( loli );
			}
		}
		return result;
	}

	public List<Character> FindCharactersInSphere( int typeMask, Vector3 point, float radius ){
		radius *= radius;
		List<Character> candidates = new List<Character>();
		for( int i=0; i<m_characters.objects.Count; i++ ){
			Character candidate = m_characters.objects[i];
			float sqDist = Vector3.SqrMagnitude( candidate.head.position-point );
			if( sqDist < radius && ((int)candidate.characterType&typeMask) != 0 ){
				candidates.Add( candidate );
			}
		}
		return candidates;
	}

	public Item FindPickupItemForCharacter( Character source, Vector3 position, float radius, Item.Type preference = Item.Type.NONE ){
		
		Collider[] objects = Physics.OverlapSphere( position, radius, Instance.visionMask, QueryTriggerInteraction.Collide );
		Item result = null;
		float leastSqDist = Mathf.Infinity;
		for( int i=0; i<objects.Length; i++ ){

			Collider collider = objects[i];
			// Item item = Tools.SearchTransformFamily<Item>( collider.transform );
			Item item = collider.transform.GetComponent<Item>();
			if( item == null ){
				continue;
			}
			//ignore self body items
			if( !item.settings.allowChangeOwner && item.mainOwner == source ){
				continue;
			}
			if( item.hasAnyAttributes((int)Item.Attributes.DISABLE_PICKUP) ){
				continue;
			}
			if( item.settings.itemType == preference ){
				return item;
			}
			float sqDist = Vector3.SqrMagnitude( position-collider.bounds.center );
			if( sqDist < leastSqDist ){
				result = item;
				leastSqDist = sqDist;
			}
		}
		return result;
	}
}

}