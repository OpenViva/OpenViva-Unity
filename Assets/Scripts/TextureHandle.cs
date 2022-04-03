using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;



namespace viva{


public class TextureHandle: VivaDisposable{
    
    private static Dictionary<string,TextureHandle> textures = new Dictionary<string, TextureHandle>();

    public readonly Texture2D _internalTexture;
    public readonly string path;


    public TextureHandle( Texture2D _texture, string _path ):base(true,_texture.name){
        if( _texture == null ) throw new System.Exception("Cannot create a null texture handle");
        _internalTexture = _texture;
        path = _path;
    }

    public static TextureHandle Load( string path ){
        if( string.IsNullOrEmpty( path ) ){
            Debug.LogError("Cannot load texture from a null filepath");
            return null;
        }
        if( System.IO.Path.GetExtension( path ) != ".tex" ) path += ".tex";

        path = Path.GetFullPath( TextureBinding.root+"/"+path );    //reformat 
        TextureHandle handle = null;
        if( textures.TryGetValue( path, out handle ) ){
            return handle;
        }else{
            //load raw texture from harddrive
            //TODO: .zip compression? Saw great improvements in filesize
            try{
                using( var stream = File.Open( path, FileMode.Open ) ){
                    var bw = new BinaryReader( stream );
                    var transparent = bw.ReadBoolean();
                    int resolution = bw.ReadInt32();
                    int byteCount = bw.ReadInt32();
                    byte[] bytes = bw.ReadBytes( byteCount );
                    var textureName = bw.ReadString();
                    Debug.Log("+Texture "+textureName+" size:"+byteCount+" transparent:"+transparent);
                    TextureFormat format = transparent ? TextureFormat.DXT5 : TextureFormat.DXT1;
                    Texture2D texture = new Texture2D( resolution, resolution, format, true, textureName.EndsWith("_NormalMap") );
                    texture.LoadRawTextureData( bytes );
                    texture.Apply( false, false );
                    texture.name = textureName;

                    handle = new TextureHandle( texture, path );

                    stream.Close();
                }
            }catch( System.Exception e ){
                Debugger.LogError("Could not load texture \""+path+"\"");
                return null;
            }
            if( handle != null ){
                textures[ path ] = handle;
                return handle;
            }else{
                return null;
            }
        }
    }

    public static void Destroy( TextureHandle handle ){
        if( handle == null ) return;
        Debug.Log("~Texture "+handle._internalTexture.name);
        // GameObject.Destroy( handle._internalTexture );
        // textures.Remove( handle.path );
    }

    public static void DestroyUnused(){
        var textureHandleList = new List<TextureHandle>( textures.Values );
        for( int i=textureHandleList.Count; i-->0 ; ){
            var handle = textureHandleList[i];
            if( handle.usage.usage == 0 ){
                Destroy( handle );
            }
        }
    }

    public void Save( string subfolder ){
        _internalTexture.Compress( false );
        _internalTexture.Apply( true, false );

        try{
            var targetFolder = TextureBinding.root+"/"+subfolder;
            if( !System.IO.Directory.Exists( targetFolder ) ){
                System.IO.Directory.CreateDirectory( targetFolder );
            }

            var path = targetFolder+"/"+Path.GetFileName( _internalTexture.name )+".tex";
            using( var stream = new FileStream( path, FileMode.Create ) ){
                var bw = new BinaryWriter( stream );
                var isTransparent = _internalTexture.format == TextureFormat.DXT5;
                bw.Write( isTransparent );
                bw.Write( _internalTexture.width );

                var textureData = _internalTexture.GetRawTextureData();
                bw.Write( textureData.Length );
                bw.Write( textureData );
                bw.Write( _internalTexture.name );

                stream.Close();
            }
            Debug.Log("Saved "+path);
            Sound.main.PlayGlobalUISound( UISound.SAVED );
        
        }catch( System.Exception e ){
            UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, "Could not save animation", e.ToString() );
            Sound.main.PlayGlobalUISound( UISound.FATAL_ERROR );
        }
    }
}

}