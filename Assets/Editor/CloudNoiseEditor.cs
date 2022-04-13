using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.IO;
using viva;


public class CloudNoiseEditor : EditorWindow{
	public static void ShowWindow(){
		GetWindow<CloudNoiseEditor>();
	}

    [SerializeField]
    private float scale = 7.0f;
    [SerializeField]
    private float darks = 1.0f;
    [SerializeField]
    private float subNoiseAlpha = 1.5f;
    [SerializeField]
    private float depth = 0.0f;
    [SerializeField]
    private float height = 0.01f;
    [SerializeField]
    private CloudRenderSettings targetRenderSettings = null;
    
    private const int SIZE = 128;

    [SerializeField]
    public Material noiseMat;
    [SerializeField]
    public GameObject cloudPlane;


	[MenuItem("Tools/Cloud Noise Editor")]
	static void Init(){
		GetWindow(typeof(CloudNoiseEditor));
	}

    public void OnGUI(){

		SerializedObject sEditor = new SerializedObject( this );
		EditorGUILayout.PropertyField( sEditor.FindProperty("noiseMat") );
		EditorGUILayout.PropertyField( sEditor.FindProperty("cloudPlane") );
		EditorGUILayout.PropertyField( sEditor.FindProperty("targetRenderSettings") );
        sEditor.ApplyModifiedProperties();

        if( noiseMat == null ){
            return;
        }
        
        EditorGUILayout.LabelField( "Scale" );
        scale = EditorGUILayout.Slider( scale, 1.0f, 32.0f );
        EditorGUILayout.LabelField( "Darks" );
        darks = EditorGUILayout.Slider( darks, 1.0f, 4.0f );
        EditorGUILayout.LabelField( "Sub Noise Alpha" );
        subNoiseAlpha = EditorGUILayout.Slider( subNoiseAlpha, 0.0f, 1.5f );
        EditorGUILayout.LabelField( "Depth" );
        depth = EditorGUILayout.Slider( depth, 0.0f, 128.0f );
        EditorGUILayout.LabelField( "Height" );
        height = EditorGUILayout.Slider( height, 0.005f, 0.02f );

        //render
        if( targetRenderSettings != null ){
            
            noiseMat.SetFloat( "_Scale", scale );
            noiseMat.SetFloat( "_Darks", darks );
            noiseMat.SetFloat( "_GreenAlpha", subNoiseAlpha );
            noiseMat.SetFloat( "_Depth", depth*0.025f );
            
            RenderTexture previewRT = new RenderTexture( SIZE, SIZE, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear );
            Texture2D preview = new Texture2D( SIZE, SIZE, TextureFormat.RGB24, false, true );

            RenderTexture.active = previewRT;
                
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, SIZE, SIZE,0);
            Graphics.DrawTexture(new Rect(0, 0, SIZE, SIZE),EditorGUIUtility.whiteTexture, noiseMat);
            GL.PopMatrix();

            preview.ReadPixels( new Rect(0,0,SIZE,SIZE), 0, 0, false );
            preview.Apply();

            RenderTexture.active = null;
        
            if( GUILayout.Button("Bake") ){
                GenerateVolumetricTexture();
            }
            EditorGUI.DrawPreviewTexture( GUILayoutUtility.GetRect( 300, 300 ), preview, null, ScaleMode.ScaleToFit );
        }
    }

    private void GenerateVolumetricTexture(){

        Texture3D volumetricTexture = new Texture3D( SIZE, SIZE, SIZE, TextureFormat.RGB24, false );
        RenderTexture volumeRT = new RenderTexture( SIZE, SIZE, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear );
        Texture2D temp = new Texture2D( SIZE, SIZE, TextureFormat.RGB24, false, true );

        RenderTexture.active = volumeRT;

        Color[] pixels = new Color[ SIZE*SIZE*SIZE ];

        for( int y=0; y<SIZE; y++ ){

            noiseMat.SetFloat( "_Depth", ((float)y)*scale*height );
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, SIZE, SIZE,0);
            Graphics.DrawTexture( new Rect(0,0,SIZE,SIZE), EditorGUIUtility.whiteTexture, noiseMat );
            GL.PopMatrix();

            temp.ReadPixels( new Rect(0,0,SIZE,SIZE), 0, 0, false );
            temp.Apply();

            Color[] depthPixels = temp.GetPixels();
            int subPixelIndex = 0;
            for( int z=0; z<SIZE; z++ ){
                for( int x=0; x<SIZE; x++ ){
                    pixels[x*SIZE*SIZE+y*SIZE+z] = depthPixels[subPixelIndex];
                    subPixelIndex++;
                }
            }
        }
        RenderTexture.active = null;
        volumeRT.Release();
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        volumetricTexture.SetPixels( pixels );
        volumetricTexture.wrapMode = TextureWrapMode.Mirror;
        volumetricTexture.Apply();

        string path = "Assets/Textures/effects/volumes/cloudNoise.asset";
        AssetDatabase.CreateAsset( volumetricTexture, path );
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        SerializedObject trs = new SerializedObject( targetRenderSettings );
        SerializedProperty volumetricTexProp = trs.FindProperty( "volumetricTexture" );

        volumetricTexProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath( path, typeof(Texture3D) ) as Texture3D;

        trs.ApplyModifiedProperties();

        if( cloudPlane != null ){
            targetRenderSettings.Apply( cloudPlane.GetComponent(typeof(MeshRenderer)) as MeshRenderer );
        }
    }
}