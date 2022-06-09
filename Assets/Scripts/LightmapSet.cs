using UnityEngine;



[System.Serializable]
public class LightmapSet : ScriptableObject
{
    [System.Serializable]
    public class SphericalHarmonics
    {
        public float[] coefficients = new float[27];
    }

    [System.Serializable]
    public class RendererInfo
    {
        public Renderer renderer;
        public int lightmapIndex;
        public Vector4 lightmapOffsetScale;
    }

    public RendererInfo[] rendererInfos;
    public Texture2D[] lightmaps;
    public Texture2D[] lightmapsDir;
    public Texture2D[] lightmapsShadow;
    public LightmapsMode lightmapsMode;
    public SphericalHarmonics[] lightProbes;
}