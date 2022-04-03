#if UNITY_2019_1_OR_NEWER
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

[assembly: AlwaysLinkAssembly]
namespace VisualDesignCafe.Rendering.Nature
{
    public static class NatureRendererSrp
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            RenderPipelineManager.beginFrameRendering -= OnBeginFrameRendering;
            RenderPipelineManager.beginFrameRendering += OnBeginFrameRendering;
        }

        private static void OnBeginFrameRendering( ScriptableRenderContext context, Camera[] cameras )
        {
            try
            {
                foreach( var renderer in NatureRenderer.Renderers )
                    foreach( var camera in cameras )
                        if( renderer.isActiveAndEnabled )
                            renderer.Render( camera );
            }
            catch( System.InvalidOperationException )
            {
                // Exception gets throws when forcing a restart of Nature Renderer
                // when a setting changed in the editor. This happens because
                // the component is removed from the Renderers list and is
                // then added again. This exception can be ignored.
            }
        }
    }
}
#endif