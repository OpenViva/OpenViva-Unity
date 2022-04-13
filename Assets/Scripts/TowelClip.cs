using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

public class TowelClip: VivaSessionAsset {

	[SerializeField]
	private VivaSessionAsset m_activeTowelAsset;
	[VivaFileAttribute]
	public VivaSessionAsset activeTowelAsset { get{ return m_activeTowelAsset; } protected set{ m_activeTowelAsset = value; } }
	public Towel activeTowel { get{ return m_activeTowelAsset as Towel; } }


	public void ClearActiveTowel(){
		activeTowelAsset = null;
	}

	public void RackTowel( Towel towel ){
		if( towel == null ){
			return;
		}
		if( towel.mainOccupyState != null ){
			towel.mainOccupyState.AttemptDrop();
		}
		towel.transform.position = transform.position;
		towel.transform.rotation = transform.rotation;

		m_activeTowelAsset = towel;
		towel.SetLastWallClip( this );
	}

	public void SpawnNewTowel(){
		if( activeTowel != null ){
			return;
		}
		GameObject towelPrefab = GameDirector.instance.FindItemPrefabByName("towel");
		if( towelPrefab != null ){
			var container = GameObject.Instantiate( towelPrefab, transform.position, transform.rotation );
			Towel towel = container.GetComponent<Towel>();
			if( towel != null ){
				RackTowel( towel );
			}else{
				Debug.LogError("[TowelClip] Towel prefab had wrong object!");
			}
		}else{
			Debug.LogError("[TowelClip] No towel prefab found!");
		}
	}
}

}