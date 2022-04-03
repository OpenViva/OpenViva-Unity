#if UNITY_2020_2_OR_NEWER
using SCRIPTED_IMPORTER = UnityEditor.AssetImporters.ScriptedImporter;
using SCRIPTED_IMPORTER_ATTRIBUTE = UnityEditor.AssetImporters.ScriptedImporterAttribute;
using ASSET_IMPORT_CONTEXT = UnityEditor.AssetImporters.AssetImportContext;
#else
using ASSET_IMPORT_CONTEXT = UnityEditor.Experimental.AssetImporters.AssetImportContext;
using SCRIPTED_IMPORTER = UnityEditor.Experimental.AssetImporters.ScriptedImporter;
using SCRIPTED_IMPORTER_ATTRIBUTE = UnityEditor.Experimental.AssetImporters.ScriptedImporterAttribute;
#endif

namespace VisualDesignCafe.ShaderX.Editor
{
     /// <summary>
     /// The base class for the Scripted Importer was changed in Unity 2020.2
     /// This causes an error during import because Unity's API Updater can
     /// not correctly change the base class in an assembly.
     /// The updated assembly runs correctly, but errors are shown in the console
     /// and the shaders do not import during the first import pass.
     /// So, this wrapper class is used for the Scripted Importer and then
     /// the ShaderXImporter is created to actually import the shader.
     /// </summary>
    [SCRIPTED_IMPORTER_ATTRIBUTE( 1, "shaderx" )]
    public class ShaderXImporterWrapper : SCRIPTED_IMPORTER
    {
        [UnityEngine.SerializeField]
        private ShaderXImportSettings _importSettings;

        public override void OnImportAsset( ASSET_IMPORT_CONTEXT c )
        {
            var importer = new ShaderXImporter();
            var context = new AssetImportContext( c.assetPath );
            importer.OnImportAsset( context, _importSettings );

            foreach( var obj in context.Objects )
                c.AddObjectToAsset( obj.Identifier, obj.Object, obj.Icon );

            c.SetMainObject( context.MainObject );

            foreach( var path in context.Dependencies )
                c.DependsOnSourceAsset( path );
        }
    }
}