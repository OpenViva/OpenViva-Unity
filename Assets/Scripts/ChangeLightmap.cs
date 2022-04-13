// using UnityEditor;
// using UnityEngine;
// using UnityEngine.Rendering;
// using System;
// using System.IO;
// using System.Collections.Generic;
    

// public class ChangeLightmap : MonoBehaviour{
    
//     [SerializeField] private string m_targetLightmapSet = "LightMapData_1";
//     public string targetLightmapSet {get { return m_targetLightmapSet; }}
//     [SerializeField]
//     private LightmapSet m_targetSet;
//     public LightmapSet targetSet { get{ return m_targetSet; } }
    
    
//     //TODO : enable logs only when verbose enabled
//     [SerializeField] private  bool verbose = false;
    
//     public static string GetLightmapSetFilePath(string dir){
//         return "Assets/ScriptableObjects/LightmapSets/"+dir+".asset"; // The directory where the lightmap data resides.
//     }
//     public static string GetLightmapSetFolder(string dir){
//         return "Assets/ScriptableObjects/LightmapSets/"+dir+"/"; // The directory where the lightmap data resides.
//     }

//     public void Load( LightmapSet lightmapSet ){
//         if( lightmapSet == null ){
//             Debug.LogError("[LightmapSet] Cannot load null set!");
//             return;
//         }
//         var newLightmaps = new LightmapData[lightmapSet.lightmaps.Length];
    
//         for( int i = 0; i < newLightmaps.Length; i++){
//             newLightmaps[i] = new LightmapData();
//             newLightmaps[i].lightmapColor = lightmapSet.lightmaps[i];
    
//             if( lightmapSet.lightmapsMode != LightmapsMode.NonDirectional){
//                 newLightmaps [i].lightmapDir = Resources.Load<Texture2D>(m_targetLightmapSet+"/" + lightmapSet.lightmapsDir[i].name);
//                 if( lightmapSet.lightmapsShadow [i] != null) { // If the textuer existed and was set in the data file.
//                     newLightmaps [i].shadowMask = Resources.Load<Texture2D>( m_targetLightmapSet + "/" + lightmapSet.lightmapsShadow [i].name);
//                 }
//             }
//         }
    
//         LoadLightProbes( lightmapSet );
//         ApplyRendererInfo(lightmapSet.rendererInfos);
    
//         LightmapSettings.lightmaps = newLightmaps;
//     }
    
//     private void LoadLightProbes( LightmapSet lightmapSet ){
//         var sphericalHarmonicsArray = new SphericalHarmonicsL2[lightmapSet.lightProbes.Length];
    
//         for( int i = 0; i < lightmapSet.lightProbes.Length; i++)
//         {
//             var sphericalHarmonics = new SphericalHarmonicsL2();
    
//             // j is coefficient
//             for( int j = 0; j < 3; j++)
//             {
//                 //k is channel(  r g b )
//                 for( int k = 0; k < 9; k++)
//                 {
//                     sphericalHarmonics[j, k] = lightmapSet.lightProbes[i].coefficients[j * 9 + k];
//                 }
//             }
    
//             sphericalHarmonicsArray[i] = sphericalHarmonics;
//         }
    
//         try
//         {
//             LightmapSettings.lightProbes.bakedProbes = sphericalHarmonicsArray;
//         }
//         catch { Debug.LogWarning("Warning, error when trying to load lightprobes for scenario "); }
//     }
    
//     private void ApplyRendererInfo( LightmapSet.RendererInfo[] infos){
//         for( int i = 0; i < infos.Length; i++){
//             var info = infos[i];
//             if( info.renderer == null ){
//                 continue;
//             }
//             info.renderer.lightmapIndex = infos[i].lightmapIndex;
//             if( !info.renderer.isPartOfStaticBatch){
//                 info.renderer.lightmapScaleOffset = infos[i].lightmapOffsetScale;
//             }
//             if( info.renderer.isPartOfStaticBatch && verbose == true){
//                 Debug.Log("Object " + info.renderer.gameObject.name + " is part of static batch, skipping lightmap offset and scale.");
//             }
//         }
//     }
    
    
// }
