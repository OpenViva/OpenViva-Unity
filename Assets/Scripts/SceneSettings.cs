using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace viva{

[System.Serializable]
public class SceneSettings : InstanceSettings{
    
    [System.Serializable]
    public class SerializedParameter{
        public string type;
        public string value;
    }
    
    [System.Serializable]
    public class SerializedFunction{
        public string funcName;
        public SerializedParameter[] parameters;
    }
    
    [System.Serializable]
    public class SerializedScript{
        public string script;
        public SerializedFunction[] functions;
    }

    [System.Serializable]
    public class TransformData{
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }
    [System.Serializable]
    public abstract class InstanceData{
        public string name;
        public int id;
        public TransformData transform;
        public SerializedScript[] serializedScripts;
    }

    [System.Serializable]
    public class ItemData:InstanceData{
        public string[] attributes;
        public bool immovable;
    }

    [System.Serializable]
    public class CharacterData:InstanceData{
        TransformData[] ragdollData = new TransformData[ BipedProfile.humanMuscles.Length ];
    }

    [System.Serializable]
    public class SceneData{
        public string scene;
        public Vector3 min;
        public Vector3 max;
        public float timeOfDay;
        public SerializedScript[] serializedScripts;
        public List<string> pastMessages = new List<string>();  //store hints to not repeat them
        public List<string> completedAchievements = new List<string>();  //store achievements to not repeat them
    }

    public static string root { get{ return Viva.contentFolder+"/Sessions"; } }
    public static readonly InstanceManager instances = new InstanceManager( SceneSettings.root, ".viva", ImportRequestType.SESSION );

    public ItemData[] itemDatas;
    public CharacterData[] characterDatas;
    public int playerDataIndex;
    public SceneData sceneData;
    public string type = "save";    //or "template"
    public int idCounterStart;
    public int hintsDisplayed;
    

    public SceneSettings( Texture2D thumbnailTexture, string _name, SceneRequest _internalSourceRequest ):base(thumbnailTexture,_name,_internalSourceRequest){
    }

    public override string GetInfoHeaderTitleText(){
        return "Session - "+name;
    }
    public override string GetInfoHeaderText(){
        return name;
    }
    public override string GetInfoBodyContentText(){
        return "";
    }
    public override void OnInstall( string subFolder=null ){
        BuiltInAssetManager.main.Install( this, SceneSettings.root, name, ".viva" );
    }
    
    public override void OnShare(){
        _internalSourceRequest.OnShare();
    }
}

}