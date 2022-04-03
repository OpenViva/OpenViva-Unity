using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;

[Serializable, VolumeComponentMenu("Post-processing/Water/Underwater")]
public sealed class Underwater : CustomPostProcessVolumeComponent, IPostProcessComponent{

    [Tooltip("Controls the intensity of the effect.")]
    public BoolParameter enable = new BoolParameter( false );
    public FloatParameter waterLevel = new FloatParameter(0);
    public FloatParameter fogLength = new FloatParameter(1);
    public FloatParameter fogThickness = new FloatParameter(0.5f);
    public ColorParameter fogColor = new ColorParameter( Color.blue );

    Material m_Material;

    public bool IsActive() => m_Material != null && enable.value;

    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.BeforePostProcess;

    public override void Setup(){
        if (Shader.Find("Hidden/Shader/Underwater") != null)

            m_Material = new Material(Shader.Find("Hidden/Shader/Underwater"));

    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination){
        if (m_Material == null)  
            return;

        m_Material.SetFloat("_WaterLevel", waterLevel.value);
        m_Material.SetFloat("_FogLength", fogLength.value);
        m_Material.SetFloat("_FogThickness", fogThickness.value);
        m_Material.SetColor("_WaterFogColor", fogColor.value);

        m_Material.SetTexture("_InputTexture", source);

        HDUtils.DrawFullScreen(cmd, m_Material, destination);

    }

    public override void Cleanup() => CoreUtils.Destroy(m_Material);

}