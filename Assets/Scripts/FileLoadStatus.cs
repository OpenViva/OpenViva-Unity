using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace viva{

public class FileLoadStatus : MonoBehaviour{

    [SerializeField]
    private Transform imageLogo;
    [SerializeField]
    private Text title;
    [SerializeField]
    private Text m_description;
    public Text description { get{ return m_description; } }

    private float dots = 0;

    void LateUpdate(){
        imageLogo.rotation = Quaternion.Euler( 0.0f, 0.0f, Time.time*20.0f );
        imageLogo.transform.localScale = Vector3.one*Mathf.LerpUnclamped( 0.45f, 0.5f, Mathf.Sin( Time.time*4.0f )*0.5f+0.5f );

        dots += Time.deltaTime*5.0f;
        title.text = "Loading"+new System.String( '.', ((int)dots)%4 );

        transform.position = GameDirector.player.transform.position;
        transform.rotation = GameDirector.player.transform.rotation;
    }
}

}