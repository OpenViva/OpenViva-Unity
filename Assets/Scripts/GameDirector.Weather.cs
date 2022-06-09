using UnityEngine;
using UnityEngine.Rendering;

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
            int width;
            int height;
            if (m_player.controls == Player.ControlType.VR)
            {
                width = Screen.width / 3;
                height = Screen.height / 3;
            }
            else
            {
                width = Screen.width / 4;
                height = Screen.height / 4;
            }
            m_cloudRT = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            cloudRT.wrapMode = TextureWrapMode.Mirror;

            cloudsMR = clouds.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
            cloudsMR.enabled = false; //dont render in the scene

            //render in the buffer
            cloudCommandBuffer = new CommandBuffer();
            cloudCommandBuffer.SetRenderTarget(cloudRT);
            if (Clouds)
            {
                cloudCommandBuffer.DrawRenderer(cloudsMR, raymarchingCloudsMat, 0);
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