using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;


namespace viva{

public class VivaMenuButton : Button{

    public void SetCallback( UnityAction callback, bool hideArrow=true ){
        onClick.RemoveAllListeners();
        onClick.AddListener( callback );
    }
}

}