using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System.IO;


namespace viva{


public class ScriptRequest: ImportRequest{

    public Script script = null;


    public ScriptRequest( string _filepath ):base( _filepath, ImportRequestType.SCRIPT ){
    }
    
    protected override string OnImport(){
        
        string text = null;
        try{
            text = System.IO.File.ReadAllText( filepath );
        }catch( System.Exception e ){
            return e.ToString();
        }
        if( script == null ) script = new Script( Path.GetFileNameWithoutExtension( filepath ) , this );
        script.text = text;

        Debug.Log("+SCRIPT "+filepath+" "+script);
        return null;
    }

    public override void _InternalOnGenerateThumbnail(){
        thumbnail.texture = GameUI.main.createMenu.defaultScriptThumbnail;
    }
    public override string GetInfoHeaderText(){
        return script != null ? script.GetInfoHeaderText() : "";
    }
    public override string GetInfoBodyContentText(){
        return script != null ? script.GetInfoBodyContentText() : "";
    }
    public override void OnCreateMenuSelected(){
        GameUI.main.createMenu.DisplayVivaObjectInfo<CharacterSettings>( script, _internalSourceRequest );
    }
}

}