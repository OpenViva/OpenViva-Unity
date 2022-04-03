using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;


namespace viva{

public delegate void GenericParameterCallback( object parameter );
    
public class Event{

    public enum EventType{
        VOICE=0,
        FUNCTION=1,
        INTERNAL_CHARACTER_FUNC=2,
    }
    
    public static Event Voice( float position, string sound, bool ignoreIfAlreadyTalking=false ){
        return new Event( position, Event.EventType.VOICE, sound, ignoreIfAlreadyTalking );
    }

    public static Event Function( float position, VivaScript source, GenericParameterCallback func, object parameters=null ){
        var funcEvent = new Event( position, Event.EventType.FUNCTION, func, parameters );
        funcEvent.source = source;
        return funcEvent;
    }

    public static Event Function( float position, VivaScript source, GenericCallback func, object parameters=null ){
        var funcEvent = new Event( position, Event.EventType.FUNCTION, func, parameters );
        funcEvent.source = source;
        return funcEvent;
    }
    
    public static Event Footstep( float position, bool rightFoot ){
        return new Event( position, Event.EventType.INTERNAL_CHARACTER_FUNC, rightFoot );
    }

    public readonly EventType type;
    public float position;
    public readonly object param1;
    public readonly object param2;
    public VivaScript source;

    private Event( float _position, EventType _type, object _param1=null, object _param2=null ){
        position = Mathf.Clamp( _position, 0.0005f, 0.9995f ); //fix %1 returning 0 and edge case when 0 repeats as 1
        type = _type;
        param1 = _param1;
        param2 = _param2;
    }

    public override bool Equals( object obj ){
        if (!(obj is Event)) return false;
        var candidate = (Event)obj;
        return ( type == candidate.type && position == candidate.position && param1 == candidate.param1 && param2 == candidate.param2 );
    }

    public void Fire( Character character ){
        switch( type ){
        case EventType.VOICE:
            if( !character ) return;
            bool ignoreIfAlreadyTalking = (bool)param2;
            if( ignoreIfAlreadyTalking && character.lastVoiceHandle != null && character.lastVoiceHandle.playing ) return;
            character.PlayVoiceGroup( param1 as string );
            break;
        case EventType.FUNCTION:
            {
                bool execute = false;
                if( source._internalSource as Character ){
                    execute = true;
                }else if( source._internalSource as Item ){
                    if( character.isBiped ){
                        //fire only if item is being held in any character grabber
                        foreach( var grabber in character.allGrabbers ){
                            var items = grabber.GetAllItems();
                            foreach( var item in items ){
                                if( item == source._internalSource ){
                                    execute = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                if( execute ){
                    try{
                        if( param1.GetType() != typeof(GenericParameterCallback) ){
                            var func = (GenericCallback)param1;
                            func.Invoke();
                        }else{
                            var func = (GenericParameterCallback)param1;
                            func.Invoke( param2 );
                        }
                    }catch( System.Exception e ){
                        Script.HandleScriptException( e, "In animation event function at "+position );
                    }
                }
            }
            break;
        case EventType.INTERNAL_CHARACTER_FUNC:
            if( !character ) return;
            character.Footstep( (bool)param1 );
            break;
        }
    }
}

public struct CurveKey{
    public float position;
    public float value;

    public CurveKey( float _position, float _value ){
        position = _position;
        value = _value;
    }
}

public class Curve{
    private readonly CurveKey[] keys;

    public Curve( CurveKey[] _keys ){
        if( _keys == null ) throw new System.Exception("Cannot create Curve with null keys");
        if( _keys.Length == 0 ) throw new System.Exception("Cannot create Curve with 0 keys");
        keys = _keys;
        System.Array.Sort( keys, (a,b)=>a.position.CompareTo(b.position) );
    }

    public Curve( float singleKeyValue, float midpointSmooth=0 ){
        midpointSmooth = Mathf.Clamp01( midpointSmooth );
        if( midpointSmooth > 0.5f || midpointSmooth == 0 ){
            keys = new CurveKey[]{ new CurveKey( 0, singleKeyValue ) };
        }else{
            keys = new CurveKey[]{
                new CurveKey( 0, 1 ),
                new CurveKey( midpointSmooth, singleKeyValue ),
                new CurveKey( 1-midpointSmooth, singleKeyValue ),
                new CurveKey( 1, 1 )
            };
        }
    }

    public float Sample( float position ){
        CurveKey? lowerKey = null;
        CurveKey? upperKey = null;
        for( int i=0; i<keys.Length; i++ ){
            var key = keys[i];
            if( key.position >= position ){
                upperKey = key;
                break;
            }
            lowerKey = key;
        }
        float lowerPos;
        float upperPos;
        float lowerVal;
        float upperVal;
        if( lowerKey.HasValue ){
            lowerPos = lowerKey.Value.position;
            lowerVal = lowerKey.Value.value;
        }else{
            lowerPos = upperKey.Value.position;
            lowerVal = upperKey.Value.value;
        }
        if( upperKey.HasValue ){
            upperPos = upperKey.Value.position;
            upperVal = upperKey.Value.value;
        }else{
            upperPos = lowerKey.Value.position;
            upperVal = lowerKey.Value.value;
        }
        float ratio = Tools.GetClampedRatio( lowerPos, upperPos, position );
        return Mathf.LerpUnclamped( lowerVal, upperVal, ratio );
    }
}

public class Weight{    //TODO: convert to struct?
    public float value = 0.0f;
    
    public void AnimateTowardsValue( float target, float speed ){
        value = Mathf.MoveTowards( value, value+( target-value )*speed, speed );
    }
}

public class WeightManager1D{
    
    public readonly Weight[] weights;
    public readonly float[] positions;
    public float position { get; private set; }

    public WeightManager1D( Weight[] _weights, float[] _positions ){
        if( _weights == null ) throw new System.Exception("Cannot manage weights of a null Weight array");
        if( _positions == null ) throw new System.Exception("Cannot manage weights of a null positions array");
        if( _weights.Length != _positions.Length ) throw new System.Exception("Weights array length must match positions array length");
        if( _positions.Length < 2 ) throw new System.Exception("Positions array length must be at least 2");

        //check order of weights
        for( int j=0,i=1; i<_positions.Length; j=i++ ){
            if( _positions[j] >= _positions[i] ) throw new System.Exception("Positions array must be in increasing order");
        }

        weights = _weights;
        positions = _positions;
        SetPosition( positions[0] );    //default set to lowest
    }

    public void SetPosition( float newPosition ){
        newPosition = Mathf.Clamp( newPosition, positions[0], positions[ positions.Length-1 ] );
        position = newPosition;
        
        float p_old = positions[0];
        int i;
        for( i=1; i<positions.Length; i++ ){
            float p_i = positions[i];
            if( newPosition > p_i ){
                weights[ i-1 ].value = 0;
                p_old = p_i;
            }else{
                float inBetween = Mathf.Clamp01( (newPosition-p_old)/(p_i-p_old) );
                weights[i].value = inBetween;
                weights[i-1].value = 1.0f-inBetween;
                
                while( ++i < weights.Length ) weights[i].value = 0;
                return;
            }
        }
    }
}

}