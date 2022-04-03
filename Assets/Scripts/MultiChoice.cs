using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace viva{

public class MultiChoice : MonoBehaviour{

    [SerializeField]
    private Text title;
    [SerializeField]
    private Button choicePrefab;

    private List<Button> buttons = new List<Button>();

    public StringCallback onChoice;


    public void SetChoices( string label, string[] choices, string current ){
        title.text = label;

        for( int i=0; i<choices.Length; i++ ){
            var button = GameObject.Instantiate( choicePrefab, transform );
            var buttonText = button.transform.GetChild(0).GetComponent<Text>();
            buttonText.text = choices[i];

            buttons.Add( button );
            
            button.name = buttonText.text;
            var currentButton = button;

            button.onClick.AddListener( delegate(){
                Choose( currentButton.name );
            } );
        }
        Choose( current, false );
    }

    private void Choose( string choice, bool sendEvent=true ){
        foreach( var other in buttons ){
            other.targetGraphic.color = new Color( 1, 1, 1, System.Convert.ToInt32( other.name == choice ) );
            var otherText = other.transform.GetChild(0).GetComponent<Text>();
            otherText.color =  other.name==choice ? new Color( 1, .96f, .73f, 1 ) : new Color( .7f, .7f, .7f, .7f );
        }
        if( sendEvent ) onChoice.Invoke( choice );
    }
}

}