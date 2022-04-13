using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using B83.Win32;


public static class FileDragAndDrop{

    // important to keep the instance alive while the hook is active.
    private static UnityDragAndDropHook hook = null;
    public static bool EnableDragAndDrop( UnityDragAndDropHook.DroppedFilesEvent callback ){
        // must be created on the main thread to get the right thread id.
        try{
            if( hook == null ){
                hook = new UnityDragAndDropHook();
            }else{
                return false;   //already installed!
            }
            hook.InstallHook();
            hook.OnDroppedFiles += callback;
        }catch{
            return false;
        }
        return true;
    }
    
    public static void DisableDragAndDrop(){
        if( hook != null ){
            hook.UninstallHook();
            hook = null;
        }
    }
}
