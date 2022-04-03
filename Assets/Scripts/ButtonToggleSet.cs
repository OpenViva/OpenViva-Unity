using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace viva{

public class ButtonToggleSet : MonoBehaviour{
    
    [SerializeField]
    private Color unselectedColor;
    [SerializeField]
    private Color unselectedTextColor;
    [SerializeField]
    private Color selectedColor;
    [SerializeField]
    private Color selectedTextColor;
    [SerializeField]
    private Button[] buttons = new Button[0];


    public void Select( int index ){
        for( int i=0; i<buttons.Length; i++ ){
            var button = buttons[i];
            var colors = button.colors;
            var text = button.transform.GetChild(0).GetComponent<Text>();
            if( i == index ){
                colors.normalColor = selectedColor;
                text.color = selectedTextColor;
            }else{
                colors.normalColor = unselectedColor;
                text.color = unselectedTextColor;
            }
            button.colors = colors;
        }
    }
}


}