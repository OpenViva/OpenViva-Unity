using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;



namespace viva{


[System.Serializable]
public class TextureBindingGroup{

    public string rendererName;
    [SerializeField]
    private List<TextureBinding> textureBindings = new List<TextureBinding>();
    
    private readonly Model model;
    public GenericCallback onApplied;
    public int Count { get{ return textureBindings.Count; } }
    private TextureBindingGroup resetBindings;

    public TextureBindingGroup( Model _model ){
        model = _model;
        rendererName = model.renderer ? model.renderer.name : "none";
    }

    public TextureBindingGroup( TextureBindingGroup copy ){
        model = copy.model;
        rendererName = copy.rendererName;
        textureBindings.AddRange( copy.textureBindings );
        resetBindings = copy.resetBindings;
    }

    public TextureBinding this[ int index ]{
        get{ return textureBindings[ index ]; }
    }

    public void OnInstall( string subFolder=null ){
        for( int i=0; i<Count; i++ ){
            this[i].handle.Save( model.name );
        }
    }

    public TextureBinding GenerateBinding( TextureHandle handle ){

        if( handle == null ){
            Debug.LogError("Handle is null. Cannot generate binding.");
            return null;
        }
        if( handle._internalTexture == null ){
            Debug.LogError("Handle texture is null. Cannot generate binding for \""+model?.name+"\"");
            return null;
        }
        
        var renderer = model.renderer;
        if( renderer == null ){
            Debug.LogError("Model renderer is null. Cannot generate binding.");
            return null;
        }

        //split up name into MATERIAL TARGET NAME and BIND TARGET NAME
        var texName = handle._internalTexture.name;
        int hyphenIndex = texName.LastIndexOf('_');
        if( hyphenIndex == -1 ){
            Debug.LogError("Texture name is missing hyphen. Cannot generate binding.");
            return null;
        }
        var materialTargetName = texName.Substring( 0, hyphenIndex );
        var binding = texName.Substring( hyphenIndex, texName.Length-hyphenIndex );
        
        for( int i=0; i<renderer.materials.Length; i++ ){
            if( renderer.materials[i].name == materialTargetName ){
                return new TextureBinding( handle, renderer, model.name+"/"+handle._internalTexture.name+".tex", binding, i );
            }
        }
        Debug.LogError("Could not find material name \""+materialTargetName+"\" for texture. Cannot generate binding.");
        return null;
    }

    public void Add( TextureBinding binding ){
        if( binding == null ){
            Debugger.LogError("Cannot add null texture binding");
            return;
        }
        if( binding.renderer == null ){
            Debugger.LogError("Cannot add texture binding with a null renderer");
            return;
        }
        if( binding.materialIndex > binding.renderer.materials.Length ){
            Debugger.LogError("Cannot add texture binding with an out of bounds material index");
            return;
        }

        //replace other binding if it exists
        for( int i=0; i<textureBindings.Count; i++ ){
            var other = textureBindings[i];
            if( other.renderer==binding.renderer && other.binding==binding.binding && other.materialIndex==binding.materialIndex ){
                other.handle.usage.Decrease();
                textureBindings.RemoveAt(i);
                break;
            }
        }
        textureBindings.Add( binding );
        binding.handle.usage.Increase();
    }

    public void SaveForReset(){
        SetResetBindings( new TextureBindingGroup( this ) );
    }

    private void SetResetBindings( TextureBindingGroup bindings ){
        if( bindings == resetBindings ) return;
        if( resetBindings != null ){
            resetBindings.DiscardAll( true );
        }
        resetBindings = bindings;
    }

    public void Reset( bool applyToChildren ){
        DiscardAll( false );
        if( resetBindings != null ){
            for( int i=0; i<resetBindings.Count; i++ ){
                Add( GenerateBinding( resetBindings[i].handle ) );
            }
        }
        if( applyToChildren ){
            foreach( var childModel in model.children ){
                childModel.textureBindingGroup.Reset( true );
            }
        }
        Apply( false );
    }

    public void MatchAndSetForReset( List<TextureBindingGroup> textureBindingGroups ){
        if( model.renderer ){
            foreach( var textureBindingGroup in textureBindingGroups ){
                if( model.renderer.name==textureBindingGroup.rendererName ){
                    SetResetBindings( textureBindingGroup );
                    break;
                }
            }
        }
        foreach( var childModel in model.children ){
            childModel.textureBindingGroup.MatchAndSetForReset( textureBindingGroups );
        }
    }

    public void DiscardAll( bool applyToChildren ){
        foreach( var binding in textureBindings ){
            binding.handle.usage.Decrease();
        }
        textureBindings.Clear();
        if( applyToChildren && model != null ){
            foreach( var childModel in model.children ){
                childModel.textureBindingGroup.DiscardAll( true );
            }
        }
    }

    public void Apply( bool applyHierarchy ){
        foreach( var binding in textureBindings ) binding.Apply();
        if( applyHierarchy ){
            foreach( var childModel in model.children ){
                childModel.textureBindingGroup.Apply( true );
            }
        }
        onApplied?.Invoke();
    }
}

}