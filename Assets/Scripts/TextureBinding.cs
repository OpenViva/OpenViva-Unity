using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace viva{


[System.Serializable]
public class TextureBinding{

    public static string root { get{ return Viva.contentFolder+"/Textures"; } }

    public string path = null;
    public string binding = null;
    public int materialIndex = 0;

    private TextureHandle m_handle;
    public TextureHandle handle { get{ return m_handle; } }
    [System.NonSerialized] public Renderer m_renderer;
    public Renderer renderer { get{ return m_renderer; } }


    public TextureBinding( TextureHandle _handle, Renderer _renderer, string _subPath, string _binding, int _materialIndex ){
        m_handle = _handle;

        m_renderer = _renderer;
        path = _subPath;
        binding = _binding;
        materialIndex = _materialIndex;
    }

    public void Preload(){
        if( m_handle == null || m_handle._internalTexture == null ){
            m_handle = TextureHandle.Load( path );
        }
    }

    public void Apply(){
        if( renderer != null && materialIndex < renderer.materials.Length ){
            renderer.materials[ materialIndex ].SetTexture( binding, handle._internalTexture );
        }
    }
}

}