using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;


namespace viva{


public class RagdollEditor : MonoBehaviour{

    [SerializeField]
    private GameObject scrollButtonEntry;
    [SerializeField]
    private Text boneInfoText;
    [SerializeField]
    private RectTransform boneButtonsList;
    [SerializeField]
    private RectTransform boneSelectionMenu;
    [SerializeField]
    private Text boneSelectionMenuTitle;
    [SerializeField]
    private RectTransform boneSelectionScrollContent;
    [SerializeField]
    private Button accept;
    [SerializeField]
    private Button discard;

    private Button[] boneButtons;
    private BipedProfile activeProfile;
    private BipedBone selectedBindingEnum = BipedBone.HEAD;
    private int lastHighlightedBoneIndex = -1;
    private List<Tuple<Button,Text>> boneSelectionEntries = new List<Tuple<Button, Text>>();
    private string lastButtonNameBinding = null;
    private string lastUnbindText;
    private int lastUnbindPairIndex = -1;


    private void OnBoneHover( string boneButtonName ){
        if( System.Enum.TryParse<BipedBone>( boneButtonName, true, out BipedBone boneEnum ) ){
            boneInfoText.transform.parent.gameObject.SetActive( true );
            boneInfoText.rectTransform.parent.localPosition = UITools.GetScreenFitWindowPos( Viva.input.mousePosition, boneInfoText.rectTransform.parent as RectTransform, out bool farX );
            
            var bone = activeProfile[ boneEnum ];
            ValidateBoneEnumBinding( bone, out bool isBinded, out bool hasColliders );
            if( isBinded ){
                if( hasColliders || !System.Array.Exists( BipedProfile.humanMuscles, element => element==boneEnum ) ){ //check if needsColliders
                    boneInfoText.text = bone.transform.name;
                    boneInfoText.color = Color.green;
                }else{
                    boneInfoText.text = bone.transform.name+" (default collider will be used)";
                    boneInfoText.color = Color.white;
                }
            }else{
                bool optional = BipedProfile.IsOptional( boneEnum );
                boneInfoText.text = optional ? "(Optional)" : "(Required)";
                boneInfoText.color = optional ? Color.white : Color.red;
            }
        }
    }

    private void OnBoneHoverExit(){
        boneInfoText.transform.parent.gameObject.SetActive( false );
    }

    private void RestoreLastUnbindButton(){
        if( lastUnbindPairIndex == -1 ) return;
        var pair = boneSelectionEntries[ lastUnbindPairIndex ];
        pair._2.text = lastUnbindText;
        pair._1.transform.SetSiblingIndex( lastUnbindPairIndex );
        lastUnbindPairIndex = -1;
    }

    private void SetupBoneButtons(){
        
        if( boneButtons == null ){
            boneButtons = boneButtonsList.GetComponentsInChildren<Button>();
            for( int i=0; i<boneButtons.Length; i++ ){
                var button = boneButtons[i];
                button.onClick.AddListener( delegate{
                    RestoreLastUnbindButton();
                    OnBoneBindingSelected( button.name );
                } );

                var callbacks = button.gameObject.AddComponent<MouseHoverCallbacks>();
                callbacks.whileHovering += delegate{
                    OnBoneHover( button.name );
                };
                callbacks.onExit += OnBoneHoverExit;
            }
        }
        foreach( var button in boneButtons ){
            UpdateBoneBindingColor( button );
        }
    }

    private void SetupSelectableBones( Model model ){
         if( model.bones == null ){
            Debug.LogError("Cannot setup selectable bones to model with null bones");
            return;
        }
        var selectableBones = new List<Transform>();
        foreach( var bone in model.bones ){
            //display only those that are not of mirror variants
            if( !bone.name.ToLower().EndsWith("_r") && !bone.name.ToLower().EndsWith("_R") ) selectableBones.Add( bone );
        }
        selectableBones.Sort( (a,b) => a.name.CompareTo(b.name) );
        foreach( var bone in selectableBones ){
            GameObject entry = GameObject.Instantiate( scrollButtonEntry, boneSelectionScrollContent );
            entry.transform.localRotation = Quaternion.identity;
            Button button = entry.GetComponent<Button>();

            Text title = null;
            if( entry.transform.childCount >= 1 ){
                title = entry.transform.GetChild(0).GetComponent<Text>();
            }
            if( button && title ){
                boneSelectionEntries.Add( new Tuple<Button,Text>( button, title ) );
                title.text = bone.name;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener( delegate{
                    bool isNull = title.text == "<unbind>";
                    OnBoneSelected( isNull ? null : bone, bone.name, model );
                } );

                //disable if activeProfile has it selected
                foreach( var boneInfo in activeProfile.bones ){
                    if( boneInfo != null && boneInfo.transform.name == bone.name ){
                        button.gameObject.SetActive( false );
                        break;
                    }
                }
            }
        }
    }

    public void EditBipedProfile( Model model, GenericCallback onSuccess, GenericCallback onDiscard ){
        if( model == null || model.bones == null ){
            Debug.LogError("Cannot BipedRagdoll edit null model or null bones");
            return;
        }
        Debug.Log("Editing model \""+model.name+"\"");

        if( model.bipedProfile != null ){
            activeProfile = new BipedProfile( model.bipedProfile );
        }else{
            activeProfile = new BipedProfile( model );
        }
        SetupBoneButtons();
        SetupSelectableBones( model );
        
        accept.onClick.RemoveAllListeners();
        accept.onClick.AddListener( delegate{ HandleSetRagdollProfile( model, onSuccess ); } );
        discard.onClick.RemoveAllListeners();
        discard.onClick.AddListener( delegate{ GameUI.main.OpenTab( "Create" ); onDiscard?.Invoke(); } );

        GameUI.main.OpenTab("Ragdoll Editor");
    }

    private void HandleSetRagdollProfile( Model model, GenericCallback onSuccess ){
        if( model.AttemptSetProfile( activeProfile, out string message ) ){
            GameUI.main.OpenTab( "Create" );
            if( message == null ){
                UI.main.messageDialog.DisplayError( MessageDialog.Type.SUCCESS, "Auto BipedRagdoll", "Auto binded to ragdoll", onSuccess);
            }else{
                UI.main.messageDialog.DisplayError( MessageDialog.Type.WARNING, "Auto BipedRagdoll", "Auto binded to ragdoll however: \n"+message, onSuccess);
            }
        }else{
            UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, "Incomplete BipedRagdoll", message, null );
        }
    }
    
    private void UpdateBoneBindingColor( BipedBone targetBoneEnum ){
        if( boneButtons == null ){
            return;
        }
        foreach( var boneButton in boneButtons ){
            if( System.Enum.TryParse<BipedBone>( boneButton.name, true, out BipedBone boneEnum ) ){
                if( boneEnum == targetBoneEnum ){
                    UpdateBoneBindingColor( boneButton );
                    return;
                }
            }
        }
        Debug.LogError("No bone binding button found for "+targetBoneEnum);
    }

    private void ValidateBoneEnumBinding( Bone<BipedBone> bone, out bool isBinded, out bool hasColliders ){
        if( bone == null ){
            isBinded = false;
            hasColliders = false;
        }else{
            isBinded = true;
            hasColliders = false;
            for( int i=0; i<bone.transform.childCount; i++ ){
                var child = bone.transform.GetChild(i).gameObject;
                if( child.layer == WorldUtil.itemsLayer ){
                    hasColliders = child.TryGetComponent<Collider>( out Collider dummy );
                    break;
                }
            }
        }
    }

    private void UpdateBoneBindingColor( Button boneButton ){
        //parse the 1 or 2 variant(s) given binding name
        BipedBone? boneEnum = null;
        bool mirrorVariant = false;
        if( System.Enum.TryParse<BipedBone>( boneButton.name, true, out BipedBone boneEnum0 ) ){
            boneEnum = boneEnum0;
            mirrorVariant = boneButton.name.EndsWith("_l");
        }
        if( !boneEnum.HasValue ){
            Debug.LogError("Could not update binding color for boneButton "+boneButton.name );
            return;
        }
        
        bool required = !BipedProfile.IsOptional( boneEnum.Value );
        Color bindingsColor;
        Color colliderColor;
        ValidateBoneEnumBinding( activeProfile[ boneEnum.Value ], out bool isBinded0, out bool hasColliders0 );
        if( mirrorVariant ){
            ValidateBoneEnumBinding( activeProfile[ boneEnum.Value-1 ], out bool isBinded1, out bool hasColliders1 );
            if( isBinded0 || isBinded1 ){
                bindingsColor = Color.green;
            }else{
                bindingsColor = required ? Color.red : Color.grey;
            }
            if( hasColliders0 || hasColliders1 ){
                colliderColor = Color.white;
            }else{
                colliderColor = Color.black;
            }
        }else{
            if( isBinded0 ){
                bindingsColor = Color.green;
            }else{
                bindingsColor = required ? Color.red : Color.grey;
            }
            if( hasColliders0 ){
                colliderColor = Color.white;
            }else{
                colliderColor = Color.black;
            }
        }
        boneButton.image.color = bindingsColor;
        boneButton.image.material = Instantiate( boneButton.image.material );
        boneButton.image.material.SetColor("_Outline", colliderColor );
    }

    private void OnBoneBindingSelected( string buttonName ){
        if( lastButtonNameBinding == buttonName ){  //toggle hide bone selection menu if same bone is selected
            RestoreLastUnbindButton();
            boneSelectionMenu.gameObject.SetActive( false );
            lastButtonNameBinding = null;
            return;
        }
        lastButtonNameBinding = buttonName;

        boneSelectionMenu.gameObject.SetActive( true );
        boneSelectionMenu.localPosition = UITools.GetScreenFitWindowPos( Viva.input.mousePosition, boneSelectionMenu, out bool farX );

        boneSelectionMenuTitle.text = buttonName;
        if( System.Enum.TryParse<BipedBone>( buttonName, true, out BipedBone boneEnum ) ){
            selectedBindingEnum = boneEnum;
            
            //update allowed selection
            int counter = 0;
            bool allowOnlyMirrorVariants = BipedProfile.HasMirrorVariant( selectedBindingEnum );
            foreach( var boneEntryPair in boneSelectionEntries ){
                var lowerText = boneEntryPair._2.text.ToLower();
                counter++;
                if( lowerText == buttonName ){
                    lastUnbindPairIndex = counter;
                }
                var mirrorVariantEntry = lowerText.EndsWith("_r") || lowerText.EndsWith("_l");
                boneEntryPair._1.gameObject.SetActive( mirrorVariantEntry == allowOnlyMirrorVariants );
            }

            //setup unbind button
            if( lastUnbindPairIndex >= 0 ){
                var unbindPair = boneSelectionEntries[ lastUnbindPairIndex ];
                lastUnbindText = unbindPair._2.text;
                unbindPair._1.transform.SetAsFirstSibling();
                unbindPair._2.text = "<unbind>";
                unbindPair._1.gameObject.SetActive( true );
            }
        }else{
            Debug.LogError("Could not parse bone binding for "+buttonName);
        }
    }

    private void OnBoneSelected( Transform bone, string boneName, Model model ){
        EditAssignToRagdollProfile( activeProfile, bone, boneName, selectedBindingEnum );
        if( BipedProfile.HasMirrorVariant( selectedBindingEnum ) ){
            EditAssignToRagdollProfileMirrored( activeProfile, model, selectedBindingEnum-1, bone, boneName );
        }
        UpdateBoneBindingColor( selectedBindingEnum );
        boneSelectionMenu.gameObject.SetActive( false );
    }

    private void EditAssignToRagdollProfileMirrored( BipedProfile profile, Model model, BipedBone mirroredBoneEnum, Transform bone, string boneName ){
        if( model.bones == null ){
            Debug.LogError("Cannot assign ragdoll profile mirrored to model with null bones");
            return;
        }
        //find mirrored binding 
        boneName = Regex.Replace( boneName, $"{"_L"}$", "_R" );
        boneName = Regex.Replace( boneName, $"{"_l"}$", "_r" );

        if( bone == null ){
            EditAssignToRagdollProfile( profile, null, boneName, mirroredBoneEnum );
        }else{
            foreach( var otherBone in model.bones ){
                if( otherBone.name == boneName ){
                    EditAssignToRagdollProfile( profile, otherBone, boneName, mirroredBoneEnum );
                    break;
                }
            }
        }
    }

    private void EditAssignToRagdollProfile( BipedProfile profile, Transform boneTransform, string boneName, BipedBone boneEnum ){
        //restore old value if any
        var oldValue = profile[ boneEnum ];
        if( oldValue != null ){
            //restore button and boneInfo
            foreach( var boneEntryPair in boneSelectionEntries ){
                if( boneEntryPair._2.text == oldValue.transform.name ){
                    boneEntryPair._1.gameObject.SetActive( true ); //restore selection
                    break;
                }
            }
        }
        //reset other bone infos using bone
        for( int i=0; i<profile.bones.Length; i++ ){
            var boneInfo = profile.bones[i];
            if( boneInfo == null ) continue;
            if( boneInfo.transform == boneTransform ){
                profile.bones[i] = null;
                var mirrorVariant = BipedProfile.GetBoneMirrorVariant( (BipedBone)i );
                if( mirrorVariant.HasValue ){
                    profile[ mirrorVariant.Value ] = null;
                }
                UpdateBoneBindingColor( (BipedBone)i );
            }
        }
        profile.AssignToRagdollProfile( boneTransform, boneEnum );
        
        //update bone selection button
        foreach( var boneEntryPair in boneSelectionEntries ){
            if( boneEntryPair._2.text == boneName ){
                boneEntryPair._1.gameObject.SetActive( false ); //assign selection
                break;
            }
        }
    }
}

}