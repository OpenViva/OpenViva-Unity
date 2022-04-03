using UnityEditor;

#if UNITY_2020_2_OR_NEWER
using SCRIPTED_IMPORTER_EDITOR = UnityEditor.AssetImporters.ScriptedImporterEditor;
#else
using SCRIPTED_IMPORTER_EDITOR = UnityEditor.Experimental.AssetImporters.ScriptedImporterEditor;
#endif

namespace VisualDesignCafe.ShaderX.Editor
{
    /// <summary>
    /// The base class for the Scripted Importer Editor was changed in Unity 2020.2
    /// This causes an error during import because Unity's API Updater can
    /// not correctly change the base class in an assembly.
    /// The updated assembly runs correctly, but errors are shown in the console
    /// and the shaders do not import during the first import pass.
    /// So, this wrapper class is used for the Scripted Importer Editor and then
    /// the ShaderXImporterEditor is created to actually draw the editor.
    /// </summary>
    [CustomEditor( typeof( ShaderXImporterWrapper ) )]
    [CanEditMultipleObjects]
    public class ShaderXImporterEditorWrapper : SCRIPTED_IMPORTER_EDITOR
    {

#if UNITY_2019_2_OR_NEWER
        protected override bool needsApplyRevert => true;
#endif

        private ShaderXImporterEditor _editor;

        public override void OnInspectorGUI()
        {
            var importer = target as ShaderXImporterWrapper;

            if( _editor == null )
                _editor = new ShaderXImporterEditor( this, serializedObject, targets, importer.assetPath );

            _editor.OnInspectorGUI();

            if( _editor.ApplyAndImport )
                ApplyAndImport();

            ApplyRevertGUI();
        }

        protected override void Apply()
        {
            base.Apply();

            if( _editor != null )
                _editor.OnApply();
        }
    }
}