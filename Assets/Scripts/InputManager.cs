using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.InputSystem;


namespace viva{

public class InputManager{

    public InputActions_viva actions { get; private set; }

    public Vector2 mousePosition { get; private set; }
    public Vector2 mouseVelocity { get{ return actions.Keyboard.mouseVelocity.ReadValue<Vector2>(); } }

    private static List<MonoBehaviour> cursorLockCounters = new List<MonoBehaviour>();
    public static bool cursorActive { get{ RefreshCursorLockCounter(); return cursorLockCounters.Count>0; } }

    public InputManager(){

        actions = new InputActions_viva();
        actions.Enable();

        BindControls();
    }

    public void BindControls(){
        actions.Keyboard.mousePosition.performed += UpdateMousePosition;
    }

    private void BindForMouseAndKeyboard(){
    }

    public static void Reset(){
        cursorLockCounters.Clear();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UpdateMousePosition( InputAction.CallbackContext ctx ){
        mousePosition = ctx.ReadValue<Vector2>();
    }

    public static void ToggleForCursorLockCounter( MonoBehaviour source, bool add ){
        if( add ){
            if( cursorLockCounters.Contains( source ) ) return;
            cursorLockCounters.Add( source );
        }else{
            cursorLockCounters.Remove( source );
        }
        RefreshCursorLockCounter();
    }
    public static void RefreshCursorLockCounter(){
        Util.RemoveNulls<MonoBehaviour>( cursorLockCounters );
        if( cursorLockCounters.Count > 0 ){
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }else{
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}

}