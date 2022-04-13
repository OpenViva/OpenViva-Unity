using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.ComponentModel;
using UnityEngine.Rendering;
using System.Reflection;



namespace viva{
    

//Reserved for non monobehavior data
[System.Serializable]
public abstract class SerializedTaskData
{
}

public class CoroutineDeserializeManager{
    public int waiting = 0;
    public bool failed { get; private set; } = false;
    public bool finished { get{ return waiting == 0; } }

    public void Fail(){
        failed = true;
    }
}

[System.Serializable]
public class SerializedVivaProperty{
    public string name;
    public int storageIndex;    //values greater than AssetStorage enum length are DataStorage (compact)
    public string value;

    public bool usesAssetStorage { get{ return storageIndex < System.Enum.GetValues(typeof( VivaFileAttribute.AssetStorage )).Length; } }

    public SerializedVivaProperty( PropertyInfo propInfo, object source ){

        name = propInfo.Name;
        var propertyString = propInfo.PropertyType.ToString();
        VivaFileAttribute.AssetStorage assetStorage = VivaFileAttribute.GetAssetStorage( propertyString );
        if( assetStorage == VivaFileAttribute.AssetStorage.UNKNOWN ){
            VivaFileAttribute.DataStorage dataStorage = VivaFileAttribute.GetDataStorage( propertyString );
            if( dataStorage == VivaFileAttribute.DataStorage.UNKNOWN ){
                Debug.LogError("[VivaFileAttribute] Unknown storage type for "+propertyString);
                return;
            }else{
                storageIndex = System.Enum.GetValues(typeof( VivaFileAttribute.AssetStorage )).Length + (int)dataStorage;   //compact
                value = VivaFileAttribute.Serialize( propInfo.GetValue( source ), dataStorage );
            }
        }else{
            storageIndex = (int)assetStorage;
            value = VivaFileAttribute.Serialize( propInfo.GetValue( source ), assetStorage );
        }
        if( value == null ){
            Debug.LogError("[Property Value] Could not serialize "+assetStorage+" with value: "+value);
        }
    }
    
    public static List<SerializedVivaProperty> Serialize( object target ){
        var properties = new List<SerializedVivaProperty>();
        PropertyInfo[] props = target.GetType().GetProperties( BindingFlags.Public | BindingFlags.Instance );

        foreach( var prop in props ){
            var attrib = prop.GetCustomAttribute( typeof(VivaFileAttribute) ) as VivaFileAttribute;
            if( attrib == null ){
                continue;
            }
            properties.Add( new SerializedVivaProperty( prop, target ) );
        }
        return properties;
    }

    public static IEnumerator Deserialize( List<SerializedVivaProperty> properties, object target, CoroutineDeserializeManager cdm ){
        if( properties == null ){
            // Debug.LogError("[SerializedVivaProperty] properties is null for "+target);
            yield break;
        }
        if( target == null ){
            // Debug.LogError("[SerializedVivaProperty] target is null!");
            yield break;
        }
        
        PropertyInfo[] props = target.GetType().GetProperties();
        foreach( var prop in props ){
            var attrib = prop.GetCustomAttribute( typeof(VivaFileAttribute) );
            if( attrib == null ){
                continue;
            }
            foreach( var savedProp in properties ){
                if( savedProp.name == prop.Name ){
                    try{
                        if( savedProp.usesAssetStorage ){
                            prop.SetValue( target, savedProp.UnpackAsAssetStorage() );
                        }else{
                            cdm.waiting++;
                            if( !savedProp.UnpackAsDataStorage(
                                delegate( object result ){
                                    prop.SetValue( target, result );
                                    cdm.waiting--;
                                })
                            ){ cdm.Fail(); }
                        }
                    }catch{
                        Debug.LogError("[PERSISTANCE] Could not apply property value. Mismatching type? ["+savedProp.name+"] val:"+savedProp.value);
                    }
                    break;
                }
            }
        }
    }

    private void OnFinishUnpacking( VivaFileAttribute.OnFinishDeserialize onFinishDeserialize, PropertyInfo prop, object target, object result ){
        prop.SetValue( target, result );

        onFinishDeserialize( result );
    }

    private object UnpackAsAssetStorage(){
        VivaFileAttribute.AssetStorage? assetStorage = (VivaFileAttribute.AssetStorage)storageIndex;
        if( assetStorage == null ){
            Debug.LogError("[Property Value] invalid storage type index "+storageIndex);
            return null;
        }
        try{
            return VivaFileAttribute.Deserialize( value, assetStorage.Value );
        }catch( Exception e ){
            Debug.LogError("[Property Value] Could not deserialize "+name+" with value: "+value);
            return null;
        }
    }
    
    private bool UnpackAsDataStorage( VivaFileAttribute.OnFinishDeserialize onFinishDeserialize ){
        VivaFileAttribute.DataStorage? dataStorage = (VivaFileAttribute.DataStorage)( storageIndex-System.Enum.GetValues(typeof( VivaFileAttribute.AssetStorage )).Length );
        if( dataStorage == null ){
            Debug.LogError("[Property Value] invalid storage type index "+storageIndex);
            return false;
        }else{
            return VivaFileAttribute.Deserialize( value, dataStorage.Value, onFinishDeserialize );
        }
    }
}

}