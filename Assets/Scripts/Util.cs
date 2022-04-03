using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;


namespace viva{

public static class Util{

    public static bool IsImmovable( Collider collider ){
        if( collider == null ) return false;
        if( collider.gameObject.layer == WorldUtil.defaultLayer ) return true;
        if( collider.gameObject.layer == WorldUtil.itemsStaticLayer ) return true;
        return false;
    }

    public static void IgnorePhysics( Collider[] a, Collider[] b, bool ignore ){
        foreach( var cA in a ){
            foreach( var cB in b ){
                Physics.IgnoreCollision( cA, cB, ignore );
            }
        }
    }

    public static void RemoveNulls<T>( List<T> list ){
        if( list == null ) return;
        for( int i=list.Count; i-->0; ){
            if( list[i] == null ) list.RemoveAt(i);
        }
    }

    public static void IgnorePhysics( Collider a, Collider[] b, bool ignore ){
        foreach( var cB in b ){
            Physics.IgnoreCollision( a, cB, ignore );
        }
    }

    public static Character GetCharacter( Rigidbody rigidbody ){
        if( !rigidbody ) return null;
        return rigidbody.gameObject.GetComponentInParent<Character>();
    }

    public static Character GetCharacter( Collider collider ){
        if( !collider ) return null;
        return collider.gameObject.GetComponentInParent<Character>();
    }

    public static Grabber GetGrabber( Rigidbody rigidbody ){
        if( rigidbody && rigidbody.TryGetComponent<Grabber>( out Grabber grabber ) ){
            return grabber;
        }
        return null;
    }

    public static Item GetItem( Rigidbody rigidbody ){
        if( rigidbody && rigidbody.TryGetComponent<Item>( out Item item ) ){
            return item;
        }
        return null;
    }

    public static Light SetupLight( GameObject target ){
        if( target == null ){
            Debugger.LogError("Cannot create a light with a null target");
            return null;
        }
		var light = target.GetComponent<Light>();
		if( light == null ) light = target.AddComponent<Light>();
        light.renderingLayerMask = (int)LightLayerEnum.Everything;
		light.useColorTemperature = false;

        var hdData = target.GetComponent<HDAdditionalLightData>();
        if( hdData == null ) hdData = target.AddComponent<HDAdditionalLightData>();
        hdData.intensity = 400;
        hdData.range = 4;

        return light;
    }

    public static string SerializeGameObject( object obj ){
        var type = obj.GetType().ToString();
        switch( type ){
        case "viva.Character":
        case "viva.Item":
            return ((VivaInstance)obj).id.ToString();
        case "System.Int32":
        case "System.Single":
        case "System.String":
            return obj.ToString();
        case "System.Boolean":
            return (bool)obj ? "1" : "0";
        case "UnityEngine.Vector2":
            var vec2Obj = (Vector2)obj;
            return ""+vec2Obj.x+"*"+vec2Obj.y;
        case "UnityEngine.Vector3":
            var vec3Obj = (Vector3)obj;
            return ""+vec3Obj.x+"*"+vec3Obj.y+"*"+vec3Obj.z;
        case "UnityEngine.Quaternion":
            var quatObj = (Quaternion)obj;
            return ""+quatObj.x+"*"+quatObj.y+"*"+quatObj.z+"*"+quatObj.w;
        default:
            return JsonUtility.ToJson( obj );
        }
    }

    public static object DeserializeGameObject( string type, string value ){
        object obj = null;
        switch( type ){
        case "viva.Character":
        case "viva.Item":
            if( System.Int32.TryParse( value, out int id ) ){
                obj = VivaInstance.FindVivaInstanceByID( id );
                if( obj == null ){
                    Debug.LogError("Could not find instance with id "+id+"");
                }
            }else{
                Debug.LogError("Could not parse id \""+value+"\"");
            }
            break;
        case "System.Int32":
            return System.Int32.TryParse( value, out int _int ) ? _int : 0;
        case "System.Single":
            return System.Single.TryParse( value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float _float ) ? _float : 0;
        case "System.String":
            return (string)value;
        case "System.Boolean":
            return (string)value=="1";
        case "UnityEngine.Vector2":
            var vec2Temp = value.Split ('*');
            if( vec2Temp.Length<2) return null;
            return new Vector2( ParseFloat(vec2Temp[0]), ParseFloat(vec2Temp[1]) );
        case "UnityEngine.Vector3":
            var vec3Temp = value.Split ('*');
            if( vec3Temp.Length<3) return null;
            return new Vector3( ParseFloat(vec3Temp[0]), ParseFloat(vec3Temp[1]), ParseFloat(vec3Temp[2]) );
        case "UnityEngine.Quaternion":
            var quatTemp = value.Split ('*');
            if( quatTemp.Length<4) return null;
            return new Quaternion( ParseFloat(quatTemp[0]), ParseFloat(quatTemp[1]), ParseFloat(quatTemp[2]), ParseFloat(quatTemp[3]) );
        default:
            var parseType = System.Type.GetType( type );
            if( parseType == null ){
                Debug.LogError("Could not parse type for \""+type+"\" while deserializing for script");
                return null;
            }
            obj = JsonUtility.FromJson( value, parseType );
            break;
        }
        return obj;
    }

    private static float ParseFloat( string str ){
        return float.Parse( str, System.Globalization.CultureInfo.InvariantCulture );
    }
}

}