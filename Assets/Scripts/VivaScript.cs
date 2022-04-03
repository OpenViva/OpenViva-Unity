using RoslynCSharp;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;



namespace viva{

[System.Serializable]
public class Preserialization{
    public readonly string funcName;
    public object[] parameters;

    public Preserialization( string _funcName, object[] _parameters=null ){
        funcName = _funcName;
        parameters = _parameters;
    }
}

public class LoadState{
    public readonly object[] objs;

    public LoadState( object[] _objs ){
        objs = _objs;
    }

    public T Get<T>( int objectIndex ){
        try{
            return (T)objs[ objectIndex ];
        }catch( System.Exception e ){
            Debugger.LogError(e.ToString());
            return default(T);
        }
    }
}

public class VivaScript{

    public static readonly VivaScript _internalDefault = new VivaScript();

    public readonly ListenerRegistry registry = new ListenerRegistry();
    public readonly List<Preserialization> preserializations = new List<Preserialization>();
    public Script _internalScript;
    public VivaInstance _internalSource;


    public Script _InternalGetScript(){
        if( _internalScript == null ) return Script._internalNextScript;
        return _internalScript;
    }
    
    //parameters must be parametrized to array
    public void Call( string funcName, object[] parameters ){
        var script = _InternalGetScript();
        if( script.assembly == null ) return;
        
        var info = script.assembly.MainType.FindCachedMethod( funcName, false );
        if( info == null ){
            Debugger.LogError("Script \""+script.name+"\" does not have the function \""+funcName+"\"");
            return;
        }
        try{
            if( parameters.Length == 1 && parameters[0] == null && info.GetParameters().Length == 0 ){
                info.Invoke( this, null );
            }else{
                info.Invoke( this, parameters );
            }
        }catch( System.Exception e ){
            Debugger.LogError( "Could not call \""+funcName+"\" on script \""+script.name+"\"" );
            return;
        }
    }

    public void Save( string _funcName, object[] _parameters=null ){
        if( string.IsNullOrEmpty( _funcName ) ){
            Debugger.LogError("Cannot serialize with a null func name");
            return;
        }
        foreach( var cached in preserializations ){
            if( cached.funcName == _funcName ){
                cached.parameters = _parameters;
                return;
            }
        }
        var preserialization = new Preserialization( _funcName, _parameters );
        preserializations.Add( preserialization );
    }

    //raw unparametrized _parameters
    public void Load( string _funcName, object[] _parameters ){ 
        Save( _funcName, _parameters );
        // Call( _funcName, new LoadState( _parameters ) );             TODO: simpler load state
        Call( _funcName, new object[]{ _parameters } );
    }

    public viva.SceneSettings.SerializedScript _InternalSerialize(){
        var serializedScript = new SceneSettings.SerializedScript();
        serializedScript.functions = new SceneSettings.SerializedFunction[ preserializations.Count ];
        for( int i=0; i<serializedScript.functions.Length; i++ ){
            var preserialization = preserializations[i];
            var serializedFunction = new SceneSettings.SerializedFunction();
            serializedFunction.funcName = preserialization.funcName;

            serializedFunction.parameters = new SceneSettings.SerializedParameter[ preserialization.parameters.Length ];
            for( int j=0; j<serializedFunction.parameters.Length; j++ ){
                var obj = preserialization.parameters[j];
                var parameter = new SceneSettings.SerializedParameter();
                if( obj != null ){
                    parameter.type = obj.GetType().ToString();
                    parameter.value = Util.SerializeGameObject( obj );
                }
                serializedFunction.parameters[j] = parameter;
            }
            serializedScript.functions[i] = serializedFunction;
        }
        return serializedScript;
    }
}

}