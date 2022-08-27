using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace viva
{


    public class PolaroidCamera : Item
    {

        public delegate void PhotoCallback(PolaroidFrame frame);

        private static RenderTexture polaroidRenderTexture = null;
        private static RenderTexture dataRenderTexture = null;

        [SerializeField]
        private GamePostProcessing gamePostProcessing = null;
        [SerializeField]
        private GameObject polaroidPrefab = null;
        [SerializeField]
        private Camera camera = null;
        [SerializeField]
        private CameraRenderMaterial effectScript = null;
        [SerializeField]
        private Shader photoDataReplacementShader = null;
        [SerializeField]
        private GameObject flash = null;
        [SerializeField]
        private AudioClip polaroidSound = null;

        private Coroutine shutterCoroutine = null;
        private PolaroidFrame lastTakenPhoto = null;

        public override void OnPreDrop()
        {
            if (shutterCoroutine != null)
            {
                DropLastTakenPhoto();
            }
        }

        public override void OnItemLateUpdatePostIK()
        {

            if (mainOwner == null)
            {
                return;
            }
            switch (mainOwner.characterType)
            {
                case Character.Type.PLAYER:
                    Player player = (Player)mainOwner;
                    UpdatePlayerPolaroidCameraInteraction(player, mainOccupyState);
                    break;
            }
        }

        private void UpdatePlayerPolaroidCameraInteraction(Player player, OccupyState mainHoldState)
        {

            //don't pose if offering object through gesture
            if (HasPickupReason(Item.PickupReasons.BEING_PRESENTED))
            {
                return;
            }

            //present camera for posing
            List<Character> lolis = GameDirector.instance.FindCharactersInSphere((int)Character.Type.LOLI, player.head.position, 5.0f);
            if (lolis.Count == 0)
            {
                return;
            }
            Transform camera = gameObject.transform;
            for (int i = 0; i < lolis.Count; i++)
            {
                Loli loli = lolis[i] as Loli;
                //if camera is visible for Shinobu and bearing is small
                if (!loli.CanSeePoint(camera.position) ||
                    Mathf.Abs(Tools.Bearing(player.head, loli.head.position)) > 50.0f)
                {
                    return;
                }
                //if camera up is about the same as owner's head up
                if (Vector3.Angle(transform.forward, player.head.up) > 30.0f)
                {
                    return;
                }

                //cancel if too close to her head
                if (Vector3.SqrMagnitude(camera.position - loli.head.position) < 0.36f)
                {
                    return;
                }
                float rayDist = Tools.PointToSegmentDistance(camera.position, camera.position - camera.up * 20.0f, loli.head.position);
                if (rayDist < 0.35f)
                {
                    loli.active.cameraPose.AttemptPoseForCamera(transform);
                }
            }
        }

        private bool IsLookingAtPanty()
        {

            Collider[] results = Physics.OverlapSphere(transform.position - transform.up, 1.0f, WorldUtil.characterMovementMask);
            for (int i = 0; i < results.Length; i++)
            {
                Loli loli = results[i].gameObject.GetComponent(typeof(Loli)) as Loli;
                if (loli != null)
                {
                    //aim at pelvic bone
                    Transform spine1 = loli.spine2.parent;
                    Vector3 screenPoint = camera.WorldToScreenPoint(spine1.position);
                    if (screenPoint.x < 0.0f || screenPoint.x > camera.pixelWidth ||
                        screenPoint.y < 0.0f || screenPoint.y > camera.pixelHeight)
                    {
                        continue;
                    }
                    float localY = loli.spine2.InverseTransformPoint(transform.position).y;
                    float dist = Vector3.Distance(spine1.position, transform.position);

                    //precomputed linear equation for near panty requirements
                    float m = (-0.25f - 0.0f) / (2.0f - 0.2f);
                    float b = -m * 0.3f;
                    float targetY = m * dist + b;
                    if (localY < targetY || dist < 0.2f)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override void OnItemLateUpdate()
        {
            if (mainOwner == null)
            {
                return;
            }
            PlayerHandState handState = mainOwner.FindOccupyStateByHeldItem(this) as PlayerHandState;
            if (handState == null)
            {
                return;
            }
            if (handState.actionState.isDown)
            {
                SnapPhoto();
            }
        }

        public void SnapPhoto(PhotoCallback onPhotoCreated = null, PhotoCallback onPhotoReleased = null)
        {
            if (shutterCoroutine == null)
            {
                SoundManager.main.RequestHandle(transform.position).PlayOneShot(polaroidSound);
                shutterCoroutine = GameDirector.instance.StartCoroutine(OpenShutterCoroutine(onPhotoCreated, onPhotoReleased));
            }
        }

        private Texture2D RenderCameraTexture(RenderTexture renderTexture, Shader replacementShader)
        {
            camera.targetTexture = renderTexture;

            RenderTexture currentRT = RenderTexture.active;
            RenderTexture.active = camera.targetTexture;
            if (replacementShader != null)
            {
                camera.RenderWithShader(replacementShader, "PhotoData");
            }
            else
            {
                camera.Render();
            }

            Texture2D newTexture = new Texture2D(camera.targetTexture.width, camera.targetTexture.height, TextureFormat.RGB24, false, true);
            newTexture.ReadPixels(new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height), 0, 0);
            newTexture.Apply();
            newTexture.Compress(false);

            RenderTexture.active = currentRT;
            return newTexture;
        }

        private PolaroidFrame.PhotoSummary AnalyzePhoto(Color32[] pixelData)
        {

            int lewdPixels = 0;
            for (int i = 0; i < pixelData.Length; i++)
            {
                Color32 color = pixelData[i];
                //lewd pixel is full red
                if (color.r == 255 && color.g + color.b == 0)
                {
                    lewdPixels++;
                }
            }
            if ((float)lewdPixels / (dataRenderTexture.width * dataRenderTexture.height) >= 0.04f)
            {   //if percent greater than
                return PolaroidFrame.PhotoSummary.PANTY;
            }
            return PolaroidFrame.PhotoSummary.GENERIC;
        }

        private IEnumerator OpenShutterCoroutine(PhotoCallback onPhotoCreated, PhotoCallback onPhotoReleased)
        {

            DropLastTakenPhoto();
            //Must be a Square texture
            polaroidRenderTexture = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            dataRenderTexture = new RenderTexture(16, 16, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

            //render regular polaroid frame
            Texture2D polaroidTexture = RenderCameraTexture(polaroidRenderTexture, null);
            polaroidRenderTexture.Release();
            yield return new WaitForEndOfFrame();

            //render for data texture
            Material cachedEffectMat = effectScript.getEffectMat();
            effectScript.setEffectMaterial(null);
            Texture2D dataTexture = RenderCameraTexture(dataRenderTexture, photoDataReplacementShader); //disable post processing for data reads
            effectScript.setEffectMaterial(cachedEffectMat);
            dataRenderTexture.Release();

            //show flash after rendering
            flash.SetActive(true);
            yield return new WaitForEndOfFrame();
            flash.SetActive(false);

            //check if ghost is in front
            var collisions = Physics.OverlapSphere(transform.position, 10.0f, WorldUtil.itemDetectorMask, QueryTriggerInteraction.Collide);
            foreach (var collision in collisions)
            {
                var ghost = collision.GetComponent<OnsenGhost>();
                if (ghost)
                {
                    float dist = Tools.SqDistanceToLine(transform.position, -transform.up, ghost.transform.position);
                    Debug.Log(dist);
                    if (dist < 1.0f)
                    {
                        ghost.Kill();
                    }
                }
            }

            //Analyze pixels for panties
            PolaroidFrame.PhotoSummary summary;
            if (dataTexture != null)
            {
                Color32[] pixelData = dataTexture.GetPixels32();
                yield return new WaitForEndOfFrame();
                summary = AnalyzePhoto(pixelData);
            }
            else
            {
                Debug.LogError("ERROR Could not analyze photo summary!");
                summary = PolaroidFrame.PhotoSummary.GENERIC;
            }

            //animate polaroid coming out of camera
            PolaroidFrame polaroidFrame = SpawnPolaroid(polaroidTexture);
            if (onPhotoCreated != null)
            {
                onPhotoCreated(polaroidFrame);
            }
            // GameObject newPolaroid = SpawnPolaroid( dataTexture, summary );
            polaroidFrame.photoSummary = summary;
            polaroidFrame.SetAttribute(Item.Attributes.DISABLE_PICKUP);

            //Animate polaroid coming out of camera
            Vector3 spawnPos = new Vector3(0.0f, 0.0f, 0.027f);
            float completion = 0.0f;
            while (completion < 1.0f)
            {
                completion = Mathf.Min(1.0f, completion + Time.deltaTime);
                spawnPos.y = -0.11f * completion;
                polaroidFrame.transform.position = transform.TransformPoint(spawnPos);
                polaroidFrame.transform.rotation = transform.rotation * Quaternion.Euler(new Vector3(0.0f, 180.0f, 0.0f));
                yield return null;
            }
            polaroidFrame.ClearAttribute(Item.Attributes.DISABLE_PICKUP);

            //finish shutter

            if (mainOwner == null)
            {
                DropLastTakenPhoto();
            }
            shutterCoroutine = null;
            if (onPhotoReleased != null)
            {
                onPhotoReleased(polaroidFrame);
            }
        }

        private void DropLastTakenPhoto()
        {
            if (lastTakenPhoto != null)
            {
                if (lastTakenPhoto.transform.parent == transform)
                {
                    lastTakenPhoto.Detach();
                }
            }
        }

        private PolaroidFrame SpawnPolaroid(Texture2D polaroidTexture)
        {

            GameObject container = GameObject.Instantiate(polaroidPrefab);
            PolaroidFrame polaroidFrame = container.GetComponent(typeof(PolaroidFrame)) as PolaroidFrame;
            //polaroidFrame.ParentToTransform( transform );
            MeshRenderer mr = container.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
            mr.materials[1].mainTexture = polaroidTexture;

            lastTakenPhoto = polaroidFrame;

            return polaroidFrame;
        }
    }

}