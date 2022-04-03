using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using System.IO;



namespace viva{

[System.Serializable]
public class CharacterSettings: InstanceSettings{

    public string[] boneBindings;
    public string[] blendShapeBindings;
    public string[] textures;
    public float height;
    public string voice;
    public float eyeDegrees = 15;   //default
    public bool doNotUseCommonScripts = false;
    public bool isAnimal;

    public Model liveEditModel;
    public MaterialSettings[] materialSettings;


    public CharacterSettings( Model _model, string _name, bool _isAnimal, CharacterRequest _characterRequest ):base(_model.thumbnail.texture,_name,_characterRequest){
        if( _model == null ) throw new System.Exception("Cannot create CharacterSettings with a null Model");
        if( _model.profile == null ) throw new System.Exception("Cannot create CharacterSettings with a Model without a profile!");
        
        fbx = Path.GetFileName( _model._internalSourceRequest.filepath );
        serializedFBXLength = _model.modelRequest.serializedFBX.serializedFBXLength;
        modelName = _model.name;
        isAnimal = _isAnimal;

        CopyRagdollProfileBindings( _model.profile );
        
        blendShapeBindings = new string[ _model.profile.blendShapeBindings.Length ];
        System.Array.Copy( _model.profile.blendShapeBindings, blendShapeBindings, blendShapeBindings.Length );

        scripts = new string[0];
        if( isAnimal ){
            doNotUseCommonScripts = true;
        }
        height = _model.skinnedMeshRenderer.bounds.size.y;

        textures = new string[ _model.textureBindingGroup.Count ];
        for( int i=0; i<textures.Length; i++ ){
            textures[i] = _model.textureBindingGroup[i].path;
        }
        liveEditModel = _model;
    }

    public override string GetInfoHeaderTitleText(){
        return "Character - "+name;
    }
    public override string GetInfoHeaderText(){
        return "";
    }
    public override string GetInfoBodyContentText(){
        string s = "";
        s += "Using fbx: "+fbx;
        s += "\nUsing model: "+modelName;
        return s;
    }
    
    public override void OnInstall( string subFolder=null ){
        
        var charReq = _internalSourceRequest as CharacterRequest;
        if( charReq == null ){
            Debugger.LogError("Cannot install without a CharacterRequest");
            return;
        }
        var lastSpawnedCharacter = charReq.lastSpawnedCharacter;
        if( lastSpawnedCharacter == null ){
            Debugger.LogError("Cannot install without any characters spawned!");
            return;
        }
        FileWatcherManager.ignoreChanges.Add( lastSpawnedCharacter.model._internalSourceRequest.filepath );

        lastSpawnedCharacter.model.OnInstall( subFolder );
        lastSpawnedCharacter.scriptManager.OnInstall( "character" );
        BuiltInAssetManager.main.Install( charReq.characterSettings, Character.root, charReq.characterSettings.name, ".character" );
    }

    public override CreateMenu.InputInfo[] OnCreateMenuInputInfoDrawer(){
        var inputInfos = new List<CreateMenu.InputInfo>();
        inputInfos.Add(
            new CreateMenu.InputInfo( "Height m:", ""+height, delegate( string value ){
                if( !System.Single.TryParse( value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float newValue ) ){
                    height = 0.0f;
                }else{
                    height = newValue;
                }
                height = Mathf.Clamp( height, 0.6f, 2.5f );
                if( liveEditModel != null ) liveEditModel.Resize( height );
                return ""+height;
            } )
        );
        inputInfos.Add(
            new CreateMenu.InputInfo( "Voice:", voice, delegate( string value ){
                voice = value.ToLower();
                return voice;
            } )
        );
        if( liveEditModel != null ){
            inputInfos.Add(
                new CreateMenu.InputInfo( "Eye range:", ""+eyeDegrees, delegate( string value ){
                    if( System.Single.TryParse( value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float result ) ){
                        eyeDegrees = result;
                    }else{
                        eyeDegrees = 0;
                        value = "0";
                    }
                    return value;
                } )
            );
            for( int i=0; i<liveEditModel.profile.blendShapeBindings.Length; i++ ){
                int blendShapeSlot = i;
                inputInfos.Add( new CreateMenu.InputInfo( ( (RagdollBlendShape)i).ToString(), liveEditModel.profile.blendShapeBindings[i], delegate( string value ){
                    if( value == null ) return "";
                    if( liveEditModel.skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex( value ) == -1 ){
                        value = "INVALID";
                    }else{
                        liveEditModel.profile.blendShapeBindings[ blendShapeSlot ] = value;
                        blendShapeBindings[ blendShapeSlot ] = value;
                    }
                    return value;
                } ) );
            }
        }
        return inputInfos.ToArray();
    }

    private void CopyRagdollProfileBindings( Profile profile ){
        var bipedProfile = profile as BipedProfile;
        if( bipedProfile != null ){
            boneBindings = new string[ bipedProfile.bones.Length ];
            for( int i=0; i<boneBindings.Length; i++ ){
                var boneInfo = bipedProfile.bones[i];
                if( boneInfo == null ) continue;
                boneBindings[i] = boneInfo.originalBoneName;
            }
        }else{
            var animalProfile = profile as AnimalProfile;
            boneBindings = new string[ animalProfile.bones.Count ];
            for( int i=0; i<boneBindings.Length; i++ ){
                var boneInfo = animalProfile.bones[i];
                if( boneInfo == null ) continue;
                boneBindings[i] = boneInfo.originalBoneName;
            }
        }
    }

    public override void OnCreateMenuSelected(){
        GameUI.main.createMenu.DisplayVivaObjectInfo<CharacterSettings>( this, _internalSourceRequest );
        if( liveEditModel != null ) liveEditModel.DisplayCreateMenuSettings(delegate{
            GameUI.main.createMenu.FindAndDisplay( CreateMenu.InfoColor.MODEL, this );
            CopyRagdollProfileBindings( liveEditModel.bipedProfile );
        });

        GameUI.main.createMenu.DisplayEditLogicButton();
        GameUI.main.createMenu.editLogicButton.SetCallback( delegate{
            
            var available = GameUI.main.createMenu.GenerateAllScriptNamesList();
            for( int i=available.Count; i-->0; ){
                if( System.Array.Exists( scripts, element => element==available[i] ) ){
                    available.RemoveAt(i);
                }
            }
            UI.main.messageDialog.RequestBlackWhiteList( "Edit Logic", "Available", "Active", new List<string>( scripts ), available, delegate( List<string> newScriptNames ){
                scripts = newScriptNames.ToArray();
                var sourceCharacterRequest = _internalSourceRequest as CharacterRequest;
                sourceCharacterRequest.lastSpawnedCharacter?.scriptManager.SetScripts( GameUI.main.createMenu.FindScripts( newScriptNames ) );
            } );
        } );
    }
    public override void OnCreateMenuDeselected(){
        liveEditModel?.OnCreateMenuDeselected();
    }
    public override void OnShare(){
        _internalSourceRequest.OnShare();
    }
}

}