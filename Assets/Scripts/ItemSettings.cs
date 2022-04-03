using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace viva{

[System.Serializable]
public class GrabbableSettings{
    public int uprightOnly = 0; // -1, 0, 1
    public float preferredGrabDegree = -1;  //value between 0~360, if < 0 then disabled
    public bool freelyRotate = false;

    public GrabbableSettings(){
    }
    
    public GrabbableSettings( GrabbableSettings copy ){
        uprightOnly = copy.uprightOnly;
        preferredGrabDegree = copy.preferredGrabDegree;
        freelyRotate = copy.freelyRotate;
    }
}

[System.Serializable]
public class MaterialSettings{
    public string material;
    public string shader;

    public MaterialSettings(){
    }
}

[System.Serializable]
public class ColliderSetting{
    public string name;
    public string material;

    public ColliderSetting(){
    }
}



[System.Serializable]
public class ItemSettings : InstanceSettings{

    public float mass = 5.0f;
    public List<TextureBindingGroup> textureBindingGroups = new List<TextureBindingGroup>();
    public bool immovable;
    public MaterialSettings[] materialSettings;
    public ColliderSetting[] colliderSettings;
    public string soundSetting;
    public string collisionSoundSoft;
    public string collisionSoundHard;
    public string dragSound;
    private Model liveEditModel;


    public ItemSettings( Model _model, string _name, ImportRequest _internalSourceRequest ):base(_model.thumbnail.texture,_name,_internalSourceRequest){
        fbx = Path.GetFileName( _model._internalSourceRequest.filepath );
        serializedFBXLength = _model.modelRequest.serializedFBX.serializedFBXLength;
        modelName = _model.name;
        
        textureBindingGroups = new List<TextureBindingGroup>();
        BuildModelTextureBindingGroups( _model );
        scripts = new string[0];
        liveEditModel = _model;

        materialSettings = new MaterialSettings[0];
        colliderSettings = new ColliderSetting[0];
    }

    private MaterialSettings FindMaterialSettings( string materialName ){
        if( materialSettings == null ) materialSettings = new MaterialSettings[0];
        foreach( var settings in materialSettings ){
            if( settings.material == materialName ) return settings;
        }
        return null;
    }
    private void BuildModelTextureBindingGroups( Model model ){
        textureBindingGroups.Add( model.textureBindingGroup );
        foreach( var childModel in model.children ){
            BuildModelTextureBindingGroups( childModel );
        }
    }
    public override string GetInfoHeaderTitleText(){
        return "Item - "+name;
    }
    public override string GetInfoHeaderText(){
        return name;
    }
    public override string GetInfoBodyContentText(){
        string body = "fbx: "+fbx+"\n";
        body += "model: "+modelName;
        return body;
    }
    public override void OnInstall( string subFolder=null ){
        ItemRequest itemRequest = _internalSourceRequest as ItemRequest;
        if( itemRequest == null ){
            Debugger.LogError("Cannot install without a source request");
        }

        var lastSpawnedItem = itemRequest.lastSpawnedItem;
        if( lastSpawnedItem == null ){
            Debugger.LogError("Cannot install without any items spawned!");
            return;
        }
        
        FileWatcherManager.ignoreChanges.Add( lastSpawnedItem.model._internalSourceRequest.filepath );

        lastSpawnedItem.model.OnInstall( subFolder );
        lastSpawnedItem.scriptManager.OnInstall( "items" );

        foreach( var textureBindingGroup in textureBindingGroups ){
            textureBindingGroup.OnInstall( subFolder );
        }
        
        //archive itemSettings
        var texBytes = thumbnail.texture.GetRawTextureData();
        thumbnailResolution = thumbnail.texture.width;
        thumbnailTexData = Tools.Base64ByteArrayToString( texBytes, 0, texBytes.Length );

        var filename = Path.GetFileNameWithoutExtension( itemRequest.filepath );
        BuiltInAssetManager.main.Install( this, Item.root, filename, ".item" );
    }

    public override List<CreateMenu.OptionInfo> OnCreateMenuOptionInfoDrawer(){
        var toggleOptions = new List<CreateMenu.OptionInfo>();
        toggleOptions.Add(
            new CreateMenu.OptionInfo("Immovable", immovable, delegate( bool value ){
                immovable = value;
            })
        );
        var itemReq = _internalSourceRequest as ItemRequest;
        if( itemReq != null && itemReq.lastSpawnedItem ){
            var grabbables = itemReq.lastSpawnedItem.gameObject.GetComponentsInChildren<Grabbable>();
            EnsureGrabbableSettingsLength( grabbables.Length );
            
            for( int i=0;i<grabbableSettings.Length; i++ ){
                var grabbable = grabbables[i];

                var grabbableIndex = i;
                toggleOptions.Add(
                    new CreateMenu.OptionInfo("<color=#66ccff>\""+grabbable.name+"\"</color> upright only", immovable, delegate( bool value ){
                        grabbableSettings[ grabbableIndex ].uprightOnly = value ? 1 : 0;
                    })
                );
            }
        }
        return toggleOptions;
    }
    
    public override List<CreateMenu.MultiChoiceInfo> OnCreateMultiChoiceInfoDrawer(){
        var multiChoices = new List<CreateMenu.MultiChoiceInfo>();
        var itemReq = _internalSourceRequest as ItemRequest;
        if( itemReq != null && itemReq.lastSpawnedItem ){
            AddMaterialMultiChoiceInfos( itemReq.lastSpawnedItem.model, itemReq.lastSpawnedItem.model, multiChoices );
        }
        return multiChoices;
    }

    private void AddMaterialMultiChoiceInfos( Model root, Model model, List<CreateMenu.MultiChoiceInfo> multiChoices ){
        if( model.renderer ){
            for( int i=0; i<model.renderer.materials.Length; i++ ){
                
                var material = model.renderer.materials[i];
                var choiceLabel = "<color=#66ffcc>"+material.name.PadRight(20,' ')+"</color>";

                bool skip = false;
                foreach( var other in multiChoices ){
                    if( other.label == choiceLabel ){
                        skip = true;
                        break;
                    }
                }
                if( skip ) continue;

                var currentSettings = FindMaterialSettings( material.name );

                var multiChoice = new CreateMenu.MultiChoiceInfo(
                    choiceLabel,
                    BuiltInAssetManager.main.SettingsMaterialNames(),
                    currentSettings==null ? BuiltInAssetManager.main.defaultModelMaterials[0].name : currentSettings.shader,
                    delegate( string choice ){
                        root.SetMaterial( material.name, new Material( BuiltInAssetManager.main.FindMaterialByShader( choice ) ) );
                        root.textureBindingGroup.Apply( true );
                        if( currentSettings == null ){
                            currentSettings = new MaterialSettings();
                            currentSettings.material = material.name;
                            currentSettings.shader = choice;
                            
                            //append new setting
                            var list = new List<MaterialSettings>();
                            foreach( var settings in materialSettings ) list.Add( settings );
                            list.Add( currentSettings );

                            materialSettings = list.ToArray();
                        }
                        if( liveEditModel != null ) liveEditModel._InternalOnGenerateThumbnail();
                    }
                );
                multiChoices.Add( multiChoice );
            }
        }
        foreach( var childModel in model.children ){
            AddMaterialMultiChoiceInfos( root, childModel, multiChoices );
        }
    }

    public void EnsureGrabbableSettingsLength( int length ){
        if( grabbableSettings == null ){
            grabbableSettings = new GrabbableSettings[]{};
        }
        if( grabbableSettings.Length < length ){
            var newGrabbableSettings = new GrabbableSettings[ length ];
            for( int i=0;i<grabbableSettings.Length; i++ ){
                newGrabbableSettings[i] = grabbableSettings[i];
            }
            for( int i=grabbableSettings.Length;i<newGrabbableSettings.Length; i++ ){
                newGrabbableSettings[i] = new GrabbableSettings();
            }
            grabbableSettings = newGrabbableSettings;
        }
    }

    public override CreateMenu.InputInfo[] OnCreateMenuInputInfoDrawer(){
        return new CreateMenu.InputInfo[]{
            new CreateMenu.InputInfo( "Mass (kg)", ""+mass, delegate( string val ){
                if( System.Single.TryParse( val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float result ) ){
                    mass = result;
                }
                mass = Mathf.Clamp( mass, 1, 250 );
                return ""+mass;
            } ),
            new CreateMenu.InputInfo( "Attributes", ""+string.Join(",",attributes), delegate( string val ){
                attributes = val.Split(',');
                return val;
            } ),
            new CreateMenu.InputInfo( "Sound profile", ""+soundSetting, delegate( string val ){
                soundSetting = val;
                return val;
            } ),
            new CreateMenu.InputInfo( "Physics hard sound", ""+collisionSoundHard, delegate( string val ){
                collisionSoundHard = val;
                return val;
            } ),
            new CreateMenu.InputInfo( "Physics soft sound", ""+collisionSoundSoft, delegate( string val ){
                collisionSoundSoft = val;
                return val;
            } ),
            new CreateMenu.InputInfo( "Physics drag loop sound", ""+dragSound, delegate( string val ){
                dragSound = val;
                return val;
            } )
        };
    }
    
    public override void OnCreateMenuSelected(){
        liveEditModel?.OnCreateMenuSelected();
    }
    public override void OnCreateMenuDeselected(){
        liveEditModel?.OnCreateMenuDeselected();
    }
    public override void OnShare(){
        _internalSourceRequest.OnShare();
    }
}

}