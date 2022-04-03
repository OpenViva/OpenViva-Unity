using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.Management;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;



namespace viva{

public partial class KeyboardGhostControls : CameraControls{

    protected override void BindCharacter( Character character ){
    }
    protected override void UnbindCharacter( Character character ){
    }

    public override void InitializeInstanceTransform( VivaInstance instance ){
    }
}

}