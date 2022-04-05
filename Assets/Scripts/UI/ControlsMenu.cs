using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace viva{

public class ControlsMenu : MonoBehaviour
{
    // Start is called before the first frame update
    public void ToggleVR(){
        VivaPlayer.user?.ToggleVR();
    }
}

}
