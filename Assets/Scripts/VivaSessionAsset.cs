using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.ComponentModel;
using UnityEngine.Rendering;
using System.Reflection;



namespace viva{

//is en extension of a monobehavior
public abstract class VivaSessionAsset : SealedMonoBehavior
{
    [HideInInspector]
    [SerializeField]
    private bool disablePersistance = false;
    [HideInInspector]
    [SerializeField]
    public bool targetsSceneAsset = false;
    [HideInInspector]
    [SerializeField]
    public string assetName;

    public string sessionReferenceName { get; protected set; } = null;
    protected static int sessionReferenceCounter = 1;
    private static bool autoAwake = true;


    public virtual bool IgnorePersistance(){
        return disablePersistance;
    }

    public static IEnumerator LoadFileSessionData( GameDirector.VivaFile file ){
        if( file == null ){
            Debug.LogError("[PERSISTANCE] ERROR Cannot load null VivaFile!");
            yield break;
        }

    }

    public static IEnumerator LoadFileSessionAssets( GameDirector.VivaFile file, CoroutineDeserializeManager cdm ){
        if( file == null ){
            Debug.LogError("[PERSISTANCE] ERROR Cannot load null VivaFile!");
            yield break;
        }

        //Load all serialized assets
        VivaSessionAsset[] toBeAwakened = new VivaSessionAsset[ file.serializedAssets.Count ];
        for( int i=0; i<file.serializedAssets.Count; i++ ){
            
            GameDirector.VivaFile.SerializedAsset serializedAsset = file.serializedAssets[i];
            try{
                toBeAwakened[i] = InitializeVivaSessionAsset( serializedAsset );
            }catch( Exception e ){
                Debug.LogError("[PERSISTANCE] Error calling load invoke handler for "+serializedAsset.assetName);
                Debug.LogError( e.ToString() );
            }
        }

        //awaken after all prefabs are instantiated to ensure existing references upon awaken
        for( int i=0; i<toBeAwakened.Length; i++ ){
            var asset = toBeAwakened[i];
            if( asset != null ){
                var serializedAsset = file.serializedAssets[i];
                GameDirector.instance.StartCoroutine( SerializedVivaProperty.Deserialize( serializedAsset.properties, asset, cdm ) );
                asset.transform.position = serializedAsset.transform.position;
                asset.transform.rotation = serializedAsset.transform.rotation;

                asset.OnAwake();
            }
        }
        while( !cdm.finished ){
            if( cdm.failed ){
                yield break;
            }
            yield return null;
        }
    }

    protected static VivaSessionAsset InitializeVivaSessionAsset( GameDirector.VivaFile.SerializedAsset serializedAsset ){
        
        GameObject targetAsset = null;
        if( serializedAsset.targetsSceneAsset ){
            targetAsset = GameObject.Find( serializedAsset.assetName );
        }else{
            if( serializedAsset.assetName == "" ){
                //ignore silently assets not setup
                return null;
            }
            autoAwake = false;
            GameObject prefab = GameDirector.instance.FindItemPrefabByName( serializedAsset.assetName );
            if( prefab != null ){
                targetAsset = GameObject.Instantiate( prefab, serializedAsset.transform.position, serializedAsset.transform.rotation );
                targetAsset.name = serializedAsset.sessionReferenceName;
            }
            autoAwake = true;
        }
        if( targetAsset == null ){
            Debug.LogError("[ITEM] Could not find prefab named "+serializedAsset.assetName);
            return null;
        }
        
        return targetAsset.GetComponent<VivaSessionAsset>();
    }

    protected override sealed void Awake(){
        if( sessionReferenceName == null ){
            
            if( targetsSceneAsset || disablePersistance ){
                sessionReferenceName = name+"_"+sessionReferenceCounter++;
                name = sessionReferenceName;
                assetName = sessionReferenceName;
            }else{
                sessionReferenceName = assetName+"_"+sessionReferenceCounter++;
            }
        }else{
            Debug.LogError(sessionReferenceName);
        }
        if( autoAwake ){
            OnAwake();
        }
    }

    protected override sealed void Start(){
        //disable
    }

    protected virtual void OnAwake(){}
    public virtual void Save( GameDirector.VivaFile vivaFile ){

        var serializedAsset = new GameDirector.VivaFile.SerializedAsset( this );
        if( serializedAsset == null ){
            Debug.LogError("[PERSISTANCE] Could not save "+name);
            return;
        }
        vivaFile.serializedAssets.Add( serializedAsset );
    }
}

}