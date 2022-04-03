using RoslynCSharp;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;



namespace viva{

public class Script: VivaEditable{

    public static readonly Script _internalDefault = new Script( "[Autonomy Listener]", null );

    //list of archived scripts in the game folders (does not include Create Menu scripts)
    private static Dictionary<string,Script> scripts = new Dictionary<string, Script>();
    public static string root { get{ return Viva.contentFolder+"/Scripts"; } }
    private static ScriptDomain domain = ScriptDomain.CreateDomain( "viva", true );
    public static Script _internalNextScript;    //fix in case people use "source" in VivaScript constructor (before the actual _internalSource is assigned)
    
    public static Script GetScript( string name ){
        Script script;
        if( scripts.TryGetValue( name, out script ) ){
            return script;
        }else{
            var scriptRequest = new ScriptRequest( root+"/"+name+".cs" );
            scriptRequest.Import();
            script = scriptRequest.script;
            
            if( script != null ){
                scripts[ name ] = script;

                scriptRequest._internalOnImported += script.OnCodeChanged;
                scriptRequest._internalOnImported += ScriptManager.RecompileAllScriptManagers;
                return script;
            }else{
                return null;
            }
        }
    }

    //refresh game script by filepath
    public static void OnScriptAssetChange( string filepath ){
        foreach( var script in scripts.Values ){
            if( script._internalSourceRequest.filepath == filepath ){ 
                script._internalSourceRequest.Import();
            }
        }
        Sound.main.PlayGlobalUISound( UISound.RELOADED );
    }


    public readonly string name;
    public ScriptRequest scriptRequest { get{ return _internalSourceRequest as ScriptRequest; } }
    public string text;
    public ScriptAssembly assembly { get; private set; }
    public CustomVariables customVariables = new CustomVariables();
    

    public Script( string _name, ScriptRequest __internalSourceRequest ):base(__internalSourceRequest){
        name = _name;
    }

    public void OnCodeChanged(){
        assembly = null;
    }
    public override string GetInfoHeaderTitleText(){
        return name;
    }
    public override string GetInfoHeaderText(){
        return "Length: "+text.Length;
    }
    public override string GetInfoBodyContentText(){
        return text;
    }
    public override void _InternalOnGenerateThumbnail(){
        thumbnail.texture = GameUI.main.createMenu.defaultScriptThumbnail;
    }
    
    public override void OnInstall( string subFolder=null ){
        if( subFolder != null ) subFolder += "/";
        Tools.ArchiveFile( _internalSourceRequest.filepath, root+"/"+subFolder+System.IO.Path.GetFileName( _internalSourceRequest.filepath ) );
        UnityEngine.Debug.Log("Installed "+_internalSourceRequest.filepath);
    }

    private string GetRecommendation( string member ){
        switch( member ){
        case "Destroy":
        case "DestroyImmediate":
            return " use \"Viva.Destroy( obj )\"";
        case "isKinematic":
            return " use \"Item.SetImmovable and WorldUtil.IsImmovable\"";
        }
        return "";
    }

    private string Compile(){  //returns errors
        
        if( assembly != null ) return null; //dont recompile if already compiled

        UnityEngine.Debug.Log("Compiling scripts for "+name);
        //adjust text
        // text = text.Replace( "Debugger.Log(", "Debugger.Log(\"["+name+"]: \"+" );
        // text = text.Replace( "Debugger.LogWarning(", "Debugger.LogWarning(\"["+name+"]: \"+" );
        // text = text.Replace( "Debugger.LogError(", "Debugger.LogError(\"["+name+"]: \"+" );

        string errors = "";
        if( text.Contains("_Internal") ) errors += "\n\"_Internal\" prefixed members are illegal";

        assembly = domain.CompileAndLoadSource( text, ScriptSecurityMode.UseSettings );
        if( domain.SecurityResult != null && !domain.SecurityResult.IsSecurityVerified ){
            errors += "\nIllegal members used:";
            foreach( var illegal in domain.SecurityResult.IllegalMemberReferences ){
                errors += "\n\""+illegal.ReferencedMember.Name+"\" "+GetRecommendation(illegal.ReferencedMember.Name);
            }
            UnityEngine.Debug.LogError(errors);
        }
        if( errors != "" ){
            foreach( var error in domain.CompileResult.Errors ){
                errors += error.Message+"\n";
            }
            return errors;
        }
        
        return null;
    }

    public ScriptProxy Instantiate<T>( T instanceSource ) where T:VivaInstance{
        var compileErrors = Compile();
        if( compileErrors != null ){
            UI.main.messageDialog.DisplayError( MessageDialog.Type.WARNING, "Compile error in "+name+".cs", compileErrors );
            Sound.main.PlayGlobalUISound( UISound.SMALL_ERROR );
            return null;
        }

        try{
            var scriptEntry = assembly.FindSubTypeOf( typeof(VivaScript), true, false );
            if( scriptEntry == null ) throw new System.Exception("Script must inherit from VivaScript [e.g. class MyClass: VivaScript]");
            _internalNextScript = this;
            var proxy = scriptEntry.CreateInstance( null, new object[]{ instanceSource } );
            
            var vivaScript = proxy.Instance as VivaScript;
            vivaScript._internalSource = instanceSource;
            vivaScript._internalScript = this;

            return proxy;
        }catch( System.Exception e ){
            HandleScriptException( e, " in "+name+".cs", this );
        }
        return null;
    }
    
    public static void HandleScriptCall( ListenerGeneric listener ){
        try{
            listener.Invoke();
        }catch( System.Exception e ){
            Script.HandleScriptException( e, " in Listener "+listener.name+"()" );
        }
    }

    public static void HandleScriptCall( ListenerGrabbable listener, Grabbable grabbable ){
        try{
            listener.Invoke( grabbable );
        }catch( System.Exception e ){
            Script.HandleScriptException( e, " in Listener "+listener.name+"()" );
        }
    }

    public static void HandleScriptCall( ListenerItem listener, Item item ){
        try{
            listener.Invoke( item );
        }catch( System.Exception e ){
            Script.HandleScriptException( e, " in Listener "+listener.name+"()" );
        }
    }
    
    public static void HandleScriptCall( ListenerCharacter listener, Character character ){
        try{
            listener.Invoke( character );
        }catch( System.Exception e ){
            Script.HandleScriptException( e, " in Listener "+listener.name+"()" );
        }
    }

    public static void HandleScriptCall( VivaScript source, GestureCallback func, string payload, Character caller, string funcName ){
        try{
            func?.Invoke( payload, caller );
        }catch( System.Exception e ){
            if( source == null ) return;
            DisplayScriptError( e, source, funcName );
        }
    }

    public static void HandleScriptCall( VivaScript source, GenericCallback func, string funcName ){
        try{
            func?.Invoke();
        }catch( System.Exception e ){
            DisplayScriptError( e, source, funcName );
        }
    }

    public static void HandleScriptCall( VivaScript source, IntReturnFunc func, int param, string funcName ){
        try{
            func?.Invoke( param );
        }catch( System.Exception e ){
            DisplayScriptError( e, source, funcName );
        }
    }

    public static void HandleScriptCall( VivaScript source, ItemCallback func, Item item, string funcName ){
        try{
            func?.Invoke( item );
        }catch( System.Exception e ){
            DisplayScriptError( e, source, funcName );
        }
    }

    public static void HandleRagdollCollisionScriptCall( VivaScript source, BipedCollisionCallback func, BipedBone ragdoll, Collision collision, string funcName ){
        try{
            func?.Invoke( ragdoll, collision );
        }catch( System.Exception e ){
            DisplayScriptError( e, source, funcName );
        }
    }

    private static void DisplayScriptError( System.Exception e, VivaScript source, string funcName ){
        Script.HandleScriptException( e, "In Script \""+source._InternalGetScript().name+"\" task callback \""+funcName+"\"" );
    }

    public static void HandleScriptException( System.Exception e, string titleSuffix, Script script=null ){
        e = e.GetBaseException();
        string errors;
        try{  
            errors = e.ToString();
            errors = errors.Substring( 0, errors.IndexOf( " at ") );
            errors = errors.Replace("System.","");
        }catch{
            errors = "ERROR";
        }

        var stackTrace = new StackTrace( e, true );
        //trim last 2 to ignore script invocation source
        for( int i=0; i<stackTrace.FrameCount-2; i++ ){
            var frame = stackTrace.GetFrame(i);
            // errors += "\nat "+frame.GetFileName();
            if( frame.GetMethod() != null ){
                string methodString = frame.GetMethod().ToString();
                methodString = methodString.Replace( "Void .ctor(Character)", "the constructor");
                errors += " in "+methodString;
            }else{
                errors += " in a delegate";
            }
            errors += "\n";
        }
        if( script == null ){
            UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, "Runtime error "+titleSuffix, errors );
        }else{
            UnityEngine.Debug.LogError( script.name );
            UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, "Runtime error "+titleSuffix, errors, delegate{
                Tools.ExploreFile( script._internalSourceRequest.filepath );
            }, "Show file", "ok" );
        }
        Sound.main.PlayGlobalUISound( UISound.SCRIPT_ERROR );
    }
}

}