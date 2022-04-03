using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using System.IO;
using RoslynCSharp;


namespace viva{

public struct ScriptInstance{
    public Script script;
    public ScriptProxy proxy;

    public void Call( string funcName, object[] parameters, bool optional=false ){
        var info = script.assembly.MainType.FindCachedMethod( funcName, false );
        if( info == null ){
            if( !optional ) Debugger.LogError("Script \""+script.name+"\" does not have the function \""+funcName+"\"");
            return;
        }
        try{
            info.Invoke( proxy.Instance, parameters );
        }catch( System.Exception e ){
            if( !optional ) Debugger.LogError( "Could not call \""+funcName+"\" on script \""+script.name+"\" "+e );
            return;
        }
    }

    public void Kill(){
        if( proxy != null ){
            var vivaScript = proxy.Instance as VivaScript;
            vivaScript.registry._InternalReset( vivaScript );
            if( !proxy.IsDisposed ) proxy.Dispose();
        }
    }
}

public class ScriptManager{

    private readonly VivaInstance instance;
    private List<ScriptInstance> scriptInstances = new List<ScriptInstance>();
    public int Count { get{ return scriptInstances.Count; } }
    private readonly GenericCallback compileFunc;
    public GenericCallback onScriptFailure;
    public GenericCallback onScriptSuccess;
    

    public ScriptManager( VivaInstance _instance ){
        instance = _instance;
    }

    public Script GetScript( int index ){
        return scriptInstances[ index ].script;
    }

    public ScriptInstance? FindScriptInstance( string name ){
        foreach( var instance in scriptInstances ){
            if( instance.script.name == name ) return instance;
        }
        return null;
    }
    
    public void AddScript( Script script ){
        if( script == null ){
            Debugger.LogError("Cannot add null script");
            return;
        }

        bool contains = false;
        foreach( var scriptInstance in scriptInstances ){
            if( scriptInstance.script == script ){
                contains = true;
                break;
            }
        }
        if( contains ) return;
        scriptInstances.Add( new ScriptInstance(){ script=script } );
    }

    public void RemoveAllScripts(){
        foreach( var scriptInstance in scriptInstances ){
            scriptInstance.Kill();
        }
        scriptInstances.Clear();
    }

    public void SetScripts( List<Script> newScripts ){
        if( newScripts == null ){
            Debug.LogError("Cannot set scripts from null array");
            return;
        }
        RemoveAllScripts();
        foreach( var newScript in newScripts ) AddScript( newScript );
    }

    public void LoadAllScripts( string folder, string[] scriptNames ){
        if( scriptNames == null ){
            Debug.LogError("Cannot load scripts from null array");
            return;
        }
        RemoveAllScripts();
        foreach( var scriptName in scriptNames ) AddScript( Script.GetScript( folder+"/"+scriptName ) );
    }
    
    public void OnInstall( string subFolder=null ){
        foreach( var scriptInstance in scriptInstances ) scriptInstance.script.OnInstall( subFolder );
    }

    public DialogOption[] HandleRequestDialogOptionsAllScripts( string functionName ){
        var result = new List<DialogOption>();
        foreach( var scriptInstance in scriptInstances ){
            var info = scriptInstance.script.assembly.MainType.FindCachedMethod( functionName, false );
            if( info == null ){
                Debugger.LogError("Script \""+scriptInstance.script.name+"\" does not have the function \""+functionName+"\"");
                continue;
            }
            if( info.ReturnParameter.ParameterType.Name != "DialogOption[]" ){
                Debugger.LogError("Function \""+functionName+"\" must return DialogOption[]");
                continue;
            }
            DialogOption[] array;
            try{
                array = info.Invoke( scriptInstance.proxy.Instance, new object[0] ) as DialogOption[];
            }catch( System.Exception e ){
                Debugger.LogError( "Could not call \""+functionName+"()\" on script \""+scriptInstance.script.name+"\"" );
                Debug.LogError( e );
                continue;
            }
            if( array != null ){
                result.AddRange( array );
            }
        }
        return result.ToArray();
    }

    public void CallOnAllScripts( string funcName, object[] parameters, bool optional=false ){
        foreach( var scriptInstance in scriptInstances ){
            scriptInstance.Call( funcName, parameters, optional );
        }
    }
    
    public void CallOnScript( string scriptName, string funcName, object[] parameters, bool optional=false ){
        foreach( var scriptInstance in scriptInstances ){
            if( scriptInstance.script.name == scriptName ) scriptInstance.Call( funcName, parameters, optional );
        }
    }

    //expensive
    public static void RecompileAllScriptManagers(){
        var instances = Resources.FindObjectsOfTypeAll<VivaInstance>();
        foreach( var instance in instances ){
            if( instance.scriptManager == null ) continue;
            instance.scriptManager.Recompile();
        }
    }
    
    public void Recompile(){
        instance._InternalReset();
        bool success = true;
        for( int i=0; i<scriptInstances.Count; i++ ){
            var scriptInstance = scriptInstances[i];
            scriptInstance.Kill();

            var newProxy = scriptInstance.script.Instantiate( instance );
            success &= newProxy!=null;
            scriptInstance.proxy = newProxy;
            scriptInstances[i] = scriptInstance;
        }
        if( !success ){
            onScriptFailure?.Invoke();
        }else{
            onScriptSuccess?.Invoke();
        }
    }

    public List<string> GetScriptList(){
        List<string> list = new List<string>();
        foreach( var scriptInstance in scriptInstances ){
            list.Add( scriptInstance.script.name );
        }
        return list;
    }

    public SceneSettings.SerializedScript[] SerializeScriptInstances(){
        var serializedScripts = new List<SceneSettings.SerializedScript>();
        for( int i=0; i<scriptInstances.Count; i++ ){
            var scriptInstance = scriptInstances[i];
            var serializedScript = ( scriptInstance.proxy.Instance as VivaScript )._InternalSerialize();
            serializedScript.script = scriptInstance.script.name;

            serializedScripts.Add( serializedScript );
        }
        return  serializedScripts.ToArray();
    }
}

}