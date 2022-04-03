using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using System.IO;
using System.Linq;


namespace viva{

public delegate void VivaInstanceReturnFunc( VivaInstance instance );

/// <summary>
/// The class used to represent all in-game instances
/// </summary>
public abstract class VivaInstance: MonoBehaviour{

    public static int idCounter { get; private set; } = System.Int32.MinValue;

    public static void _InternalSetIDCounterStart( int id ){
        idCounter = id;
    }

    public static VivaInstance FindVivaInstanceByID( int id ){
        var found = Character.instances.FindInstanceByID( id );
        if( found == null ) found = Item.instances.FindInstanceByID( id );

        return found;
    }

    public static T CreateVivaInstance<T>( GameObject gameObject, InstanceSettings _settings, int? idOverride ) where T:VivaInstance{
        T instance = gameObject.AddComponent( typeof(T) ) as T;
        instance._internalSettings = _settings;
        if( idOverride.HasValue ){
            instance.id = Mathf.Max( idOverride.Value, instance.id );
            instance.id = idOverride.Value;
        }else{
            instance.id = ++idCounter;
        }
        instance.scriptManager = new ScriptManager( instance );
        return instance;
    }


    [SerializeField]
    protected List<Attribute> m_attributes = new List<Attribute>();
    protected AttributeCallback _internalOnAttributeChanged;
    public int id { get; private set; } = 0;
    public InstanceSettings _internalSettings { get; private set; }
    public readonly CustomVariables customVariables = new CustomVariables();
    public GenericCallback _internalOnDestroy;
    public string assetFilepath { get{ return _internalSettings._internalSourceRequest.filepath; } }
    public ScriptManager scriptManager { get; private set; }
    public bool _internalDestroyed { get; set; }
    public bool destroyed { get{ return _internalDestroyed; } }

    public Thumbnail GetThumbnail(){
        return _internalSettings.thumbnail;
    }
    public abstract float? CalculateApproximateRadius();
    public abstract float? CalculateApproximateFloorY();

    public abstract void TeleportTo( Vector3 position, Quaternion rotation );

    public abstract void _InternalInitialize();

    public abstract void _InternalReset();
    
    private void OnDestroy(){
        _internalOnDestroy?.Invoke();
    }
    public override string ToString(){
        return name+" #"+(id-System.Int32.MinValue);
    }
    
    public bool HasAttributes( AttributeRequest attributeRequest ){
        return Attribute.Matches( m_attributes.ToArray(), attributeRequest.attributes, attributeRequest.countMatch );
    }

    public string[] AttributesToArray(){
        var result = new List<string>();
        foreach( var attrib in m_attributes ){
            result.Add( attrib.count+"x "+attrib );
        }
        return result.ToArray();
    }
    
    public string FindAttributeWithPrefix( string prefix ){
        foreach( var attr in m_attributes ){
            if( attr.name.StartsWith( prefix ) ) return attr.name;
        }
        return null;
    }

    public bool HasAttribute( string name ){
        foreach( var attrib in m_attributes ){
            if( attrib.name == name ) return true;
        }
        return false;
    }

    public List<Attribute> FindMissingAttributes( AttributeRequest attributeRequest ){
        var missing = new List<Attribute>();
        foreach( var otherAttribute in attributeRequest.attributes ){
            var attribute = FindAttribute( otherAttribute.name );
            if( attribute != null ){   //ensure counts match
                int countDiff = 0;
                switch( attributeRequest.countMatch ){
                case CountMatch.EQUAL:
                    countDiff = otherAttribute.count-attribute.count;
                    break;
                case CountMatch.EQUAL_OR_GREATER:
                    countDiff = attribute.count>=otherAttribute.count ? 0 : otherAttribute.count-attribute.count;
                    break;
                }
                if( countDiff != 0 ){
                    missing.Add( new Attribute( attribute.name, countDiff ) );
                }
            }else{
                missing.Add( otherAttribute );
            }
        }
        return missing;
    }

    public Attribute FindAttribute( string name ){
        foreach( var attrib in m_attributes ){
            if( attrib.name == name ) return attrib;
        }
        return null;
    }

    public void RemoveAttribute( string attributeName ){
        if( string.IsNullOrWhiteSpace( attributeName ) ) return;
        var attrib = FindAttribute( attributeName );
        if( attrib != null ){
            m_attributes.Remove( attrib );
            attrib.count = 0;
            _internalOnAttributeChanged.Invoke( attrib );
        }
    }

    public void RemoveAttributeWithPrefix( string prefix ){
        var attribute = FindAttributeWithPrefix( prefix );
        if( attribute != null ){
            RemoveAttribute( attribute );
        }
    }

    public void AddAttribute( string newAttribute ){
        if( string.IsNullOrWhiteSpace( newAttribute ) ) return;

        if( !Attribute.ExtractInfo( newAttribute, out int count, out string attribName ) ){
            Debugger.LogError("Could not parse attribute \""+newAttribute+"\"");
            return;
        }
        if( count == 0 ) return;

        var attrib = FindAttribute( attribName );
        if( attrib == null ){
            attrib = new Attribute( attribName, count );
            m_attributes.Add( attrib );
        }else{
            attrib.count += count;
            if( attrib.count == 0 ) m_attributes.Remove( attrib );
        }
        _internalOnAttributeChanged.Invoke( attrib );
    }
}

public abstract class InstanceCallbackWrapper{
    public abstract void Invoke( VivaInstance instance );
}
public class InstanceManager{

    private List<VivaInstance> instances = new List<VivaInstance>();
    public Dictionary<string,SpawnableImportRequest> cachedRequests = new Dictionary<string, SpawnableImportRequest>();
    public VivaInstanceReturnFunc onNewInstance;
    private string root;
    private string extension;
    private ImportRequestType requestType;


    public InstanceManager( string _root, string _extension, ImportRequestType _requestType ){
        root = _root;
        extension = _extension;
        requestType = _requestType;
    }

    public void _InternalReset(){

        foreach( var instance in instances ){
            GameObject.DestroyImmediate( instance.gameObject );
        }
        instances.Clear();
    }

    private void _InternalUnlink( VivaInstance instance ){
        if( instance == null ){
            Debug.LogError("Cannot unlink null item");
            return;
        }
        int index = instances.IndexOf( instance );
        if( index != -1 ){
            instances.RemoveAt(index);
        }else{
            Debug.LogError("Could not unlink VivaInstance");
        }
    }

    public void _InternalLink( VivaInstance instance ){
        if( instance == null ) return;
        if( instances.Contains( instance ) ){
            Debug.LogError("VivaInstance already exists");
            return;
        }

        instances.Add( instance );
        onNewInstance?.Invoke( instance );

        instance._internalOnDestroy += delegate{
            _InternalUnlink( instance );
        };

        var item = instance as Item;
        if( item && item.immovable ) Scene.main.BakeNavigation();
    }

    public void _InternalSpawnAndLink( string assetName, bool instant, Vector3 position, Quaternion rotation, InstanceCallbackWrapper instanceCallback, InstanceCallbackWrapper preInitializeCallback ){
        var request = _InternalGetRequest( assetName );
        if( request == null ){
            Debugger.LogError("Cannot spawn using a null request");
            instanceCallback.Invoke( null );
            return;
        }

        var requestExtension = System.IO.Path.GetExtension( request.filepath );
        if( requestType != request.type ){
            Debug.LogError("Mismatching import types \""+requestExtension+"\" & \""+extension+"\"");
            instanceCallback.Invoke( null );
            return;
        }
        request._InternalSpawnUnlinked( instant, new SpawnProgress(
            delegate( object obj ){
                var instance = obj as VivaInstance;
                if( instance ){
                    instance.transform.position = position;
                    instance.transform.rotation = rotation;
                    preInitializeCallback?.Invoke( instance );
                    instance._InternalInitialize();
                    instanceCallback?.Invoke( instance );
                    _InternalLink( instance );
                }else{
                    instanceCallback?.Invoke( instance );   //pass in null
                }
            }
        ) );
    }

    public SpawnableImportRequest _InternalGetRequest( string name, bool userCall=true ){
        if( !cachedRequests.TryGetValue( name, out SpawnableImportRequest request ) ){
            request = ImportRequest.CreateRequest( Path.GetFullPath( root+"/"+name+extension ) ) as SpawnableImportRequest;
            if( request == null ){
                return null;
            }
            request.Import( userCall );
            cachedRequests[ name ] = request;
        }
        return request;
    }

    public VivaInstance FindInstanceByID( int id ){
        foreach( var instance in instances ){
            if( instance.id == id ) return instance;
        }
        return null;
    }
}

}