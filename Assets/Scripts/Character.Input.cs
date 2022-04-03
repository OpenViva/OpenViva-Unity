using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

public partial class Character: VivaInstance{

    private InputButtonSet input = new InputButtonSet();
    public readonly ListenerFloat onScroll = new ListenerFloat("onScroll");
    private bool bindedInput = false;

	/// <summary> Gets the InputButton with the specified Keyboard enum.
    /// <param name="inputEnum"> keyEnum: The InputButton enum of the desired InputButton.
    public InputButton GetInput( Input inputEnum ){
        return input[ inputEnum.ToString() ];
    }

    private void UnbindInput(){
        if( !bindedInput ) return;
        bindedInput = false;
        for( int i=0; i<System.Enum.GetValues(typeof(Input)).Length; i++ ){
            Input val = (Input)i;
            string inputId = val.ToString();
            if( val != Input.RightAction && val != Input.LeftAction && val != Input.MiddleAction ){
                Viva.input.actions.desktop.Get().FindAction( inputId ).performed -= InputAction;
            }
        }
        Viva.input.actions.desktop.RightMouse.performed -= RightMouseAction;
        Viva.input.actions.desktop.MiddleMouse.performed -= MiddleMouseAction;
        Viva.input.actions.desktop.LeftMouse.performed -= LeftMouseAction;
        Viva.input.actions.vrControls.rightTriggerClick.performed -= RightMouseAction;
        Viva.input.actions.vrControls.leftTriggerClick.performed -= LeftMouseAction;
        Viva.input.actions.desktop.Scroll.performed -= ScrollAction;
    }

    private void BindInput(){
        if( bindedInput ) return;
        bindedInput = true;
        //setup custom controls
        for( int i=0; i<System.Enum.GetValues(typeof(Input)).Length; i++ ){
            Input val = (Input)i;
            string inputId = val.ToString();

            if( val != Input.RightAction && val != Input.LeftAction && val != Input.MiddleAction ){
                Viva.input.actions.desktop.Get().FindAction( inputId ).performed += InputAction;
            }
        }
        Viva.input.actions.desktop.RightMouse.performed += RightMouseAction;
        Viva.input.actions.desktop.MiddleMouse.performed += MiddleMouseAction;
        Viva.input.actions.desktop.LeftMouse.performed += LeftMouseAction;
        Viva.input.actions.vrControls.rightTriggerClick.performed += RightMouseAction;
        Viva.input.actions.vrControls.leftTriggerClick.performed += LeftMouseAction;
        Viva.input.actions.desktop.Scroll.performed += ScrollAction;
    }

    private void LeftMouseAction( UnityEngine.InputSystem.InputAction.CallbackContext ctx ){
        input[ "LeftAction" ].Fire( ctx.ReadValueAsButton() );
    }

    private void MiddleMouseAction( UnityEngine.InputSystem.InputAction.CallbackContext ctx ){
        input[ "MiddleAction" ].Fire( ctx.ReadValueAsButton() );
    }

    private void RightMouseAction( UnityEngine.InputSystem.InputAction.CallbackContext ctx ){
        input[ "RightAction" ].Fire( ctx.ReadValueAsButton() );
    }

    private void InputAction( UnityEngine.InputSystem.InputAction.CallbackContext ctx ){
        input[ ctx.action.name ].Fire( ctx.ReadValueAsButton() );
    }

    private void ScrollAction( UnityEngine.InputSystem.InputAction.CallbackContext ctx ){
        onScroll.Invoke( ctx.ReadValue<Vector2>().y );
    }
}

}