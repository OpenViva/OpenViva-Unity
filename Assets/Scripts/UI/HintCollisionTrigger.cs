using System.Collections;
using UnityEngine;

namespace viva{

public class HintCollisionTrigger : MonoBehaviour
{
    [SerializeField]
    private string hintText = "";
    [SerializeField]
    private string vrHintText = "";
    [SerializeField]
    private Vector3 localSpawnPosition = Vector3.zero;
    [SerializeField]
    private float scale = 1.0f;
    [SerializeField]
    private UIMenu targetMenu = null;
    [SerializeField]
    private Sprite sprite = null;
    [SerializeField]
    private GameObject enableOnEnterTargetMenu = null;
    [SerializeField]
    private bool useHUD = false;

    private bool activated = false;
    

	private void OnTriggerEnter( Collider collider ){

        Character character = collider.GetComponent<Character>();
        if( character == null ){
            return;
        }
        if( character.characterType != Character.Type.PLAYER ){
            return;
        }
        ActivateHint( transform );
	}

    public void ActivateHint( Transform parent, Item source=null, TutorialManager.ExitCoroutineFunction exitFunction=null ){
        if( activated ){
            return;
        }
        string message;
        if( GameDirector.player.controls == Player.ControlType.KEYBOARD || vrHintText.Length < 2 ){
            message = hintText;
        }else{
            message = vrHintText;
        }
        activated = true;
        TutorialManager.main.DisplayHint(
            parent,
            localSpawnPosition,
            message,
            sprite,
            scale,
            source,
            exitFunction
        );
        OnHintTrigger();
    }

    public void OnHintTrigger(){
        if( targetMenu != null ){
            GameDirector.instance.StartCoroutine( EnableOnEnterMenu() );
        }else{
            GameDirector.Destroy( this.gameObject );
        }
    }

    private IEnumerator EnableOnEnterMenu(){
        while( true ){
            if( GameDirector.instance.IsAnyUIMenuActive() && GameDirector.instance.lastMenu == targetMenu ){
                Debug.Log("[HINT TRIGGER] Enabled "+enableOnEnterTargetMenu);
                break;
            }
            yield return null;
        }
        if( enableOnEnterTargetMenu == null ){
            Debug.LogError("ERROR target menu object is null!");
        }else{
            enableOnEnterTargetMenu.SetActive( true );
        }
        GameDirector.Destroy( this.gameObject );
    }

    private void OnDrawGizmos(){
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube( transform.TransformPoint(localSpawnPosition), Vector3.one*0.15f );
    }
}

}