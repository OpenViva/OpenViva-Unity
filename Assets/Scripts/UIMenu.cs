using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace viva{


public abstract class UIMenu : MonoBehaviour {

	[SerializeField]
	private GraphicRaycaster graphicRaycaster;
	[SerializeField]
	public Transform keyboardHeadTransform;

	private Coroutine persistUICoroutine = null;
	private bool waitForExitVolume = false;

	public abstract void OnBeginUIInput();
	public abstract void OnExitUIInput();
	

	public GraphicRaycaster GetGraphicRaycaster(){
		return graphicRaycaster;
	}
	
	private void OnTriggerEnter( Collider collider ){
		
		//open if collider is the player
		Character character = collider.gameObject.GetComponent<Character>();
		if( !character ){
			return;
		}
		if( character.characterType == Character.Type.PLAYER && persistUICoroutine == null ){
			persistUICoroutine = GameDirector.instance.StartCoroutine( CheckIfPlayerClickedOnUI( character as Player ) );
		}
	}
	
	private void OnTriggerExit( Collider collider ){
		//close if collider is the player
		Character character = collider.gameObject.GetComponent<Character>();
		if( !character ){
			return;
		}
		if( character.characterType == Character.Type.PLAYER ){
			ClickExitMenu();
		}
	}

	public void ClickExitMenu(){
		if( persistUICoroutine != null ){
			GameDirector.instance.StopCoroutine( persistUICoroutine );
			persistUICoroutine = null;
		}
		GameDirector.instance.StopUIInput();
	}

	private IEnumerator CheckIfPlayerClickedOnUI( Player player ){

		while( true ){	//keep checking if UI is inactive

			if( !GameDirector.instance.IsAnyUIMenuActive() ){
				if( player.controls == Player.ControlType.VR ){
					GameDirector.instance.BeginUIInput( this, player );
					break;
				}else{
					if( player.leftPlayerHandState.gripState.isUp ){
						if( GamePhysics.GetRaycastInfo( player.head.position, player.head.forward, 1.5f, Instance.uiMask ) ){
							GraphicRaycaster gRaycaster = GamePhysics.result().transform.GetComponent<GraphicRaycaster>();
							if( gRaycaster == graphicRaycaster ){
								GameDirector.instance.BeginUIInput( this, player );
								break;
							}
						}
					}
				}
			}
			persistUICoroutine = null;
			yield return null;
		}
	}

}

}