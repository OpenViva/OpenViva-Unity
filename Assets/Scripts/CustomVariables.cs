using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace viva{

public class CustomVariable{
    public VivaScript source;
    public string name;
    public object value;
}

public class CustomVariables{

    private List<CustomVariable> variables = new List<CustomVariable>();

    public CustomVariables(){
    }

    private CustomVariable? FindByName( string name ){
        foreach( var variable in variables ){
            if( variable.name == name ) return variable;
        }
        return null;
    }

    public CustomVariable Get( VivaScript source, string variableName ){
        if( source == null ) throw new System.Exception("Cannot get a custom variable without providing a source registry");
        
        CustomVariable variable = FindByName( variableName );
        if( variable == null ){
            variable = new CustomVariable(){ source=source, name=variableName };
            variables.Add( variable );
            if( !source.registry.registeredCustomVariables.Contains( this ) ) source.registry.registeredCustomVariables.Add( this );
        }
        return variable;
    }

    public void _InternalRemoveAllFromSource( VivaScript source ){
        for( int i=variables.Count; i-->0; ){
            var variable = variables[i];
            if( variable.source == source ) variables.RemoveAt(i);
        }
    }
}

}