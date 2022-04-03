using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace viva{

public class TaskButton : MonoBehaviour{

    [SerializeField]
    private Text m_text;
    public Text text { get{ return m_text; } }
    [SerializeField]
    private Image m_buttonImage;
    public Image buttonImage { get{ return m_buttonImage; } }


    private void SetColor( Color color ){
        text.color = color;
        buttonImage.color = color;
    }
    
}

}