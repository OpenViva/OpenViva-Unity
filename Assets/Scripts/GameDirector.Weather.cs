using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace viva
{


    public partial class GameDirector : MonoBehaviour
    {

        [Header("Weather")]

        [SerializeField]
        private GameObject cloudsPrefab;
        [SerializeField]
        private CloudRenderSettings cloudRenderSettings;
        [SerializeField]
        private Material weatherSimGenerate;
        [SerializeField]
        private Texture weatherNoises;
        [SerializeField]
        public Material raymarchingCloudsMat;
        private bool Clouds = true;


        private GameObject clouds;
        private MeshRenderer cloudsMR;
        [SerializeField]
        private RenderTexture m_cloudRT;
        public RenderTexture cloudRT { get { return m_cloudRT; } }
        private CommandBuffer cloudCommandBuffer;
        private const float cloudPostRenderDistance = 150.0f;

        private const int weatherRTSize = 256;
        private RenderTexture weatherGenerateRT;
        private const float weatherRenderWaitTime = 1.0f / 20.0f; //20fps
        private float weatherRenderWaitTimer = 0.0f;

        public bool UseClouds()
        {
            return Clouds;
        }

        public void SetCloud(bool on)
        {
            Clouds = on;
            RebuildCloudRendering();
        }

        public RenderTexture GetCloudRT()
        {
            return cloudRT;
        }

        private void LateUpdateWeatherRendering()
        {

            if (clouds == null)
            {
                return;
            }

            weatherRenderWaitTimer -= Time.deltaTime;
            if (weatherRenderWaitTimer <= 0.0f)
            {
                weatherRenderWaitTimer = weatherRenderWaitTime - (-weatherRenderWaitTimer % weatherRenderWaitTime);

                RenderTexture old = RenderTexture.active;
                RenderTexture.active = weatherGenerateRT;
                Graphics.DrawTexture(new Rect(0, 0, 1.0f, 1.0f), weatherNoises, weatherSimGenerate);
                RenderTexture.active = old;
                clouds.transform.position = mainCamera.transform.position;
            }
        }

        private void RebuildCloudRendering()
        {
            if (cloudsPrefab == null)
            {
                Debug.LogError("Missing cloud prefab!");
                return;
            }
            if (cloudRenderSettings == null)
            {
                Debug.LogError("Missing cloud render settings!");
                return;
            }
            if (clouds != null)
            {
                Destroy(clouds);
                cloudRT.Release();
                GameDirector.instance.mainCamera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, cloudCommandBuffer);
            }

            clouds = GameObject.Instantiate(cloudsPrefab);

            //create raymarched cloud variables
            RenderTextureDescriptor desc;
            if (XRSettings.enabled) {
                desc = XRSettings.eyeTextureDesc;
                // Initial values in XRSettings.eyeTextureDesc are weird, very low res, no vrUsage??? I will just set them myself
                desc.width = Screen.width / 3;
                desc.height = Screen.height / 3;
                desc.vrUsage = VRTextureUsage.TwoEyes;
                desc.volumeDepth = 2;
            } else {
                desc = new RenderTextureDescriptor(Screen.width, Screen.height);
                desc.width /= 4;
                desc.height /= 4;
                desc.volumeDepth = 1;
            }
            Debug.Log("Reacreating Clouds RT " + desc.vrUsage + " " + XRSettings.enabled + " " + desc.width + " " + desc.height + " " + desc.volumeDepth);
            desc.dimension = TextureDimension.Tex2DArray;
            desc.colorFormat = RenderTextureFormat.ARGB32;
            m_cloudRT = new RenderTexture(desc);
            cloudRT.wrapMode = TextureWrapMode.Mirror;

            cloudsMR = clouds.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
            cloudsMR.enabled = false; //dont render in the scene

            //render in the buffer
            cloudCommandBuffer = new CommandBuffer();
            cloudCommandBuffer.SetRenderTarget(cloudRT, 0, CubemapFace.Unknown, -1);
            if (Clouds)
            {
                cloudCommandBuffer.DrawRenderer(cloudsMR, raymarchingCloudsMat, 0, -1);
            }
            cloudCommandBuffer.SetRenderTarget(null as RenderTexture);

            //initialize cloud rendering
            GameDirector.instance.mainCamera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, cloudCommandBuffer);
            cloudRenderSettings.Apply(cloudsMR);

            //create weather variables
            weatherGenerateRT = new RenderTexture(weatherRTSize, weatherRTSize, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
            weatherGenerateRT.wrapMode = TextureWrapMode.Repeat;
            cloudsMR.sharedMaterial.SetTexture("_CloudMap", weatherGenerateRT);
        }
    }

}