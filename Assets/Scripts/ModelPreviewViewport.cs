using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace viva
{


    [RequireComponent(typeof(Camera))]
    [RequireComponent(typeof(LineRenderer))]
    public class ModelPreviewViewport : MonoBehaviour
    {
        public enum PreviewMode
        {
            EYES,
            BONES,
            POSE,
            NONE
        }

        [SerializeField]
        private MeshRenderer meshRenderer;
        [SerializeField]
        private Material modelPreviewMaterial;
        [SerializeField]
        private Button playButton;
        [SerializeField]
        private Dropdown posedropdown;

        public Camera renderCamera { get; private set; }
        private Material[] cachedMeshRendererMaterials;
        public RenderTexture renderTexture { get; private set; }
        private Vector3 currentPivot = Vector3.zero;
        private float currentHeight = 0.0f;
        private float currentRadius = 1.0f;
        public Loli modelDefault { get; private set; }
        private float panCameraEase = 1.0f;
        private Vector3 targetPivot = Vector3.zero;
        private Vector3 cachedPivot = Vector3.zero;
        private Quaternion cachedRotation;
        private Quaternion targetRotation;
        private float cachedHeight;
        private float targetHeight;
        private float cachedRadius;
        private float targetRadius;
        private PreviewMode previewMode = PreviewMode.NONE;
        private LineRenderer lineRenderer;
        private Coroutine highlightBoneChainCoroutine = null;
        public Loli.Animation modelDefaultPoseAnim = Loli.Animation.PHOTOSHOOT_2;

        private void Awake()
        {

            renderCamera = this.GetComponent<Camera>();
            lineRenderer = this.GetComponent<LineRenderer>();
            cachedMeshRendererMaterials = meshRenderer.materials;
        }

        public void SetPreviewMode(PreviewMode newPreviewMode)
        {
            if (previewMode == newPreviewMode)
            {
                return;
            }
            previewMode = newPreviewMode;
            if (previewMode == PreviewMode.EYES)
            {
                PanCamera(
                    modelDefault.head.transform.position,
                    Quaternion.LookRotation(-Vector3.forward, Vector3.up),
                    0.1f,
                    0.7f
                );
            }
            else
            {
                PanCamera(
                    modelDefault.head.transform.position,
                    Quaternion.LookRotation(-Vector3.forward, Vector3.up),
                    0.0f,
                    1.3f
                );
            }
            if (previewMode != PreviewMode.POSE && previewMode != PreviewMode.BONES)
            {
                modelDefault.ForceImmediatePose(modelDefault.GetLastReturnableIdleAnimation());

            }
            if (previewMode != PreviewMode.BONES)
            {
                StopHighlightingBoneChain();
            }
        }

        public void StartHighlightingBoneChain(Transform startBone)
        {
            if (highlightBoneChainCoroutine != null)
            {
                GameDirector.instance.StopCoroutine(highlightBoneChainCoroutine);
            }
            highlightBoneChainCoroutine = GameDirector.instance.StartCoroutine(HighlightBoneChain(startBone));
        }

        private void StopHighlightingBoneChain()
        {
            if (highlightBoneChainCoroutine == null)
            {
                return;
            }
            lineRenderer.positionCount = 0;
            GameDirector.instance.StopCoroutine(highlightBoneChainCoroutine);
            highlightBoneChainCoroutine = null;
        }

        private IEnumerator HighlightBoneChain(Transform startBone)
        {

            while (true)
            {

                List<Vector3> pointList = new List<Vector3>();
                Transform child = startBone;
                while (child != null)
                {
                    pointList.Add(child.position);
                    if (child.childCount == 0)
                    {
                        break;
                    }
                    child = child.GetChild(0);
                }
                Vector3[] points = pointList.ToArray();
                lineRenderer.positionCount = points.Length;
                lineRenderer.SetPositions(points);
                yield return null;
            }
        }

        private void OnEnable()
        {
            renderTexture = new RenderTexture(408, 1024, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            renderCamera.targetTexture = renderTexture;
            modelPreviewMaterial.mainTexture = renderTexture;

            Material[] materials = meshRenderer.materials;
            materials[1] = modelPreviewMaterial;
            meshRenderer.materials = materials;
        }

        private void OnDisable()
        {
            renderCamera.targetTexture = null;
            renderTexture.Release();
            Destroy(renderTexture);

            meshRenderer.materials = cachedMeshRendererMaterials;
        }

        private void PanCamera(Vector3 newPivot, Quaternion newRotation, float newHeight, float newRadius)
        {

            cachedPivot = currentPivot;
            cachedRotation = renderCamera.transform.rotation;
            cachedHeight = currentHeight;
            cachedRadius = currentRadius;
            panCameraEase = 0.0f;

            targetPivot = newPivot;
            targetRotation = newRotation;
            targetRadius = newRadius;
            targetHeight = newHeight;
        }

        private void LateUpdate()
        {
            renderCamera.transform.rotation = Quaternion.LookRotation(Tools.FlatForward(transform.forward), Vector3.up);
            if (panCameraEase < 1.0f)
            {
                panCameraEase = Mathf.Clamp01(panCameraEase + Time.deltaTime * 5.0f);
                float ease = Tools.EaseOutQuad(panCameraEase);
                currentPivot = Vector3.LerpUnclamped(cachedPivot, targetPivot, ease);
                renderCamera.transform.rotation = Quaternion.LerpUnclamped(cachedRotation, targetRotation, ease);
                currentHeight = Mathf.LerpUnclamped(cachedHeight, targetHeight, ease);
                currentRadius = Mathf.LerpUnclamped(cachedRadius, targetRadius, ease);
            }
            else
            {
                UpdateCameraTransformKeyboardInputs();
            }

            UpdateCameraTransform();
            modelDefault.ApplyToonAmbience(modelDefault.transform.position, Color.white);

            switch (previewMode)
            {
                case PreviewMode.EYES:
                    Vector3 spinDir = new Vector3(
                        Mathf.Cos(Time.time * 1.5f),
                        Mathf.Sin(Time.time * 1.5f),
                        0.0f
                    );
                    modelDefault.SetEyeRotations(spinDir, spinDir);
                    break;
                case PreviewMode.POSE:
                    if (posedropdown.value == 0)
                    {
                        modelDefaultPoseAnim = Loli.Animation.PHOTOSHOOT_2;
                    }
                    if (posedropdown.value == 1)
                    {
                        modelDefaultPoseAnim = Loli.Animation.PHOTOSHOOT_1;
                    }
                    // if(posedropdown.value == 2){
                    //     modelDefaultPoseAnim = Loli.Animation.PHOTOSHOOT_3;
                    // }
                    modelDefault.ForceImmediatePose(modelDefaultPoseAnim);
                    break;
            }
        }

        private void UpdateCameraTransformKeyboardInputs()
        {
            float speed;
            if (GameDirector.player.keyboardAlt)
            {
                speed = 0.05f;
            }
            else
            {
                speed = 0.01f;
            }
            // if( GameDirector.player.qButtonState.isHeldDown InputOLD.GetKey( KeyCode.Q ) ){
            //     currentRadius = Mathf.Max( currentRadius-speed, 0.1f );
            // }else if( InputOLD.GetKey( KeyCode.E) ){
            //     currentRadius = Mathf.Min( currentRadius+speed, 4.0f );
            // }
            if (GameDirector.player.movement.x < 0.0f)
            {
                transform.rotation *= Quaternion.Euler(0.0f, speed * Mathf.Rad2Deg, 0.0f);
            }
            else if (GameDirector.player.movement.x > 0.0f)
            {
                transform.rotation *= Quaternion.Euler(0.0f, -speed * Mathf.Rad2Deg, 0.0f);
            }
            if (GameDirector.player.movement.y > 0.0f)
            {
                currentHeight = Mathf.Min(currentHeight + speed * 0.7f, 4.0f);
            }
            else if (GameDirector.player.movement.y < 0.0f)
            {
                currentHeight = Mathf.Max(currentHeight - speed * 0.7f, -4.0f);
            }
        }

        private void UpdateCameraTransform()
        {
            Vector3 pivot = currentPivot + Vector3.up * currentHeight;
            transform.position = pivot - transform.forward * currentRadius;
            Vector3 lookVector = pivot - transform.position;
            if (lookVector.sqrMagnitude == 0.0f)
            {
                lookVector += Vector3.forward;
            }
            transform.rotation = Quaternion.LookRotation(currentPivot - transform.position, Vector3.up);
        }

        public void SelectPreviewLoli(Vector3 spawnPosition)
        {
            if (modelDefault == null)
            {
                return;
            }
            modelDefault.SetPreviewMode(false);
            modelDefault.Teleport(spawnPosition, modelDefault.transform.rotation);
            modelDefault = null;
            gameObject.SetActive(false);
        }

        public void SetPreviewLoli(Loli newLoli)
        {
            if (newLoli == null)
            {
                gameObject.SetActive(false);
                playButton.gameObject.SetActive(false);
                modelDefault = null;
                return;
            }
            gameObject.SetActive(true);
            playButton.gameObject.SetActive(true);

            //delete old loli if present
            if (modelDefault != null)
            {
                //hotswap if possible
                if (newLoli != null)
                {
                    newLoli.Hotswap(modelDefault.headModel);
                }
            }

            modelDefault = newLoli;
            if (modelDefault != null)
            {
                //prepare for preview
                newLoli.SetTargetAnimation(Loli.Animation.STAND_STRETCH);
                modelDefault.active.idle.enableFaceTargetTimer = false;
                modelDefault.SetPreviewMode(true);

                currentPivot = modelDefault.head.transform.parent.position; //neck
                renderCamera.transform.rotation = Quaternion.LookRotation(-Vector3.forward, Vector3.up);
                currentHeight = 0.0f;
                currentRadius = 1.0f;
                PanCamera(
                    modelDefault.head.transform.position,
                    Quaternion.LookRotation(-Vector3.forward, Vector3.up),
                    0.0f,
                    1.3f
                );

                modelDefault.SetLookAtTarget(renderCamera.transform);
            }
        }
    }

}