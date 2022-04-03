using UnityEngine;



namespace viva{


public class HintMessage: MonoBehaviour{

    [System.Flags]
    public enum Hints{
        COMBINE_ITEMS=1
    }

    public static Hints hintsDisplayed;

    public static void AttemptHint( Hints hint, string messageText, string vrText, Vector3 vrPosition ){
        if( hintsDisplayed.HasFlag( hint ) ) return;
        hintsDisplayed &= hint;

        if( !VivaPlayer.user ){
            Debug.LogError("Player was not instanced during hint");
            return;
        }

        string text = "";
        if( string.IsNullOrEmpty( vrText ) ){
            text = messageText;
        }else{
            text = VivaPlayer.user.isUsingKeyboard ? messageText : vrText;
        }
        MessageManager.main.DisplayMessage( vrPosition, text, null );
    }


    public Hints hint;
    public string messageText;
    public string vrText;



    public void OnTriggerEnter( Collider collider ){
        var rigidBody = collider.transform.GetComponentInParent<Rigidbody>();
        if( rigidBody ){
            var character = Util.GetCharacter( rigidBody );
            if( character && character.isPossessed ){
                gameObject.SetActive( false );
                AttemptHint( hint, messageText, vrText, transform.position );
            }
        }
    }
}

}