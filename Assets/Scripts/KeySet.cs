using UnityEngine;
using System.Collections.Generic;




namespace viva{

public delegate void GenericCallback();
public delegate void BoolCallbackFunc( bool value );
public delegate void StringCallback( string value );
public delegate void StringListReturnFunc( List<string> values );

public enum Input{
    A,
    B,
    C,
    D,
    E,
    F,
    G,
    H,
    I,
    J,
    K,
    L,
    M,
    N,
    O,
    P,
    Q,
    R,
    S,
    T,
    U,
    V,
    W,
    X,
    Y,
    Z,
    Tab,
    Esc,
    Space,
    LeftShift,
    RightAction,
    MiddleAction,
    LeftAction,
    LeftControl
}

/// <summary> Class for attaching functions to controllers such as keyboards and VR controllers.
public sealed class InputButton{

    /// <summary> The name of the InputButton
    public readonly string name;
    /// <summary> The listener to add callbacks to whenever this InputButton is pressed.
    public ListenerGeneric onDown;
    /// <summary> The listener to add callbacks to whenever this InputButton is released.
    public ListenerGeneric onUp;
    
    public InputButton( string _name ){
        name = _name;
        onDown = new ListenerGeneric( name+" onDown" );
        onUp = new ListenerGeneric( name+" onUp" );
    }

    /// <summary> Force fires the InputButton with the specified action
    /// <param name="down">Specifies whether to treat this fire as a press or a release.</param>
    public void Fire( bool down ){
        if( down ){
            onDown.Invoke();
        }else{
            onUp.Invoke();
        }
    }
}

public sealed class InputButtonSet{

    private readonly Dictionary<string,InputButton> keys = new Dictionary<string,InputButton>();

    public InputButton this[ string keyName ]{
        get{
            if( keys.TryGetValue( keyName, out InputButton result ) ){
                return result;
            }
            return null;
        }
    }

    public void _InternalReset(){
        foreach( var key in keys.Values ){
            key.onDown._InternalReset();
            key.onUp._InternalReset();
        }
    }

    public bool AddInputButton( string keyName ){
        if( keys.ContainsKey( keyName ) ) return false;

        keys.Add( keyName, new InputButton( keyName ) );
        return true;
    }
}

}