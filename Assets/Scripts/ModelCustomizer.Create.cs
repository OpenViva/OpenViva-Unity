using System.Collections;
using UnityEngine;
using UnityEngine.UI;


namespace viva
{

    public partial class ModelCustomizer : UITabMenu
    {

        [Header("Create Tab")]
        [SerializeField]
        private GameObject createTab = null;
        [SerializeField]
        private AudioClip modelSpawnSound = null;
        [SerializeField]
        private ModelPreviewViewport modelPreviewer = null;
        [SerializeField]
        private GameObject step1DragFiles = null;
        [SerializeField]
        private GameObject verticalInfos = null;
        [SerializeField]
        private Text modelInfo = null;
        [SerializeField]
        private Image skinContainer = null;
        [System.Serializable]
        private class EyeballValidation
        {
            public Image eyeballX;
            public Image container;
            public Image textureContainer;
            public Image materialContainer;
            public Image boneContainer;
        }
        [SerializeField]
        private EyeballValidation rightPupilInfo = null;
        [SerializeField]
        private EyeballValidation leftPupilInfo = null;
        [SerializeField]
        private Image animationCompatibilityContainer = null;
        [SerializeField]
        private Image headpatSphereContainer = null;
        [SerializeField]
        private Image headCollisionSphereContainer = null;
        [SerializeField]
        private Image hatLocalPosContainer = null;

        [Header("Card Result Tab")]
        [SerializeField]
        private CameraPose characterPhotoshootPose = null;
        [SerializeField]
        private GameObject cardResultTab = null;
        [SerializeField]
        private Image characterCardResultImage = null;
        [SerializeField]
        private Image skinCardResultImage = null;
        [SerializeField]
        private Text cardsResultInfo = null;
        [SerializeField]
        private Button acceptButton = null;
        [SerializeField]
        private AudioClip cardCreateSound = null;
        [SerializeField]
        private Texture2D characterBackground = null;
        [SerializeField]
        private Texture2D skinBackground = null;
        [SerializeField]
        private GameObject hatPreviewPrefab = null;
        [SerializeField]
        private Button hatPreviewButtonContainer = null;

        private GameObject activeHat;


        private void InitializeCreateTab()
        {
            ValidateAllInfoProperties();
        }

        private string TrimToFit(string s, int length)
        {
            if (s.Length > length)
            {
                return "..." + s.Substring(s.Length - length - 3);
            }
            return s;
        }

        private class TextureLoadRequest
        {
            public readonly string filepath;
            public Texture2D result;

            public TextureLoadRequest(string _filepath)
            {
                filepath = _filepath;
            }
        }

        public void clickPreviewHat()
        {
            if (modelPreviewer.modelDefault == null)
            {
                return;
            }
            if (activeHat != null)
            {
                Debug.LogError("Hat already exists");
                GameDirector.Destroy(activeHat);
                return;
            }
            activeHat = GameObject.Instantiate(hatPreviewPrefab);
            activeHat.transform.SetParent(modelPreviewer.modelDefault.head);
            activeHat.transform.localPosition = modelPreviewer.modelDefault.headModel.hatLocalPosAndPitch;
            activeHat.transform.localRotation = Quaternion.identity;
            activeHat.transform.localScale = Vector3.one;
            activeHat.gameObject.layer = WorldUtil.offscreenSpecialLayer;
            Vector3 localEuler = activeHat.transform.localEulerAngles;
            localEuler.x = modelPreviewer.modelDefault.headModel.hatLocalPosAndPitch.w;
            activeHat.transform.localEulerAngles = localEuler;
        }

        private void ValidateProperty(Image propertyContainer, bool valid, string title, bool required = false, string failText = null)
        {
            Text childText = propertyContainer.transform.GetChild(0).GetComponent<Text>();
            if (valid)
            {
                childText.color = Color.green;
                propertyContainer.color = new Color(0.0f, 0.5f, 0.0f);
                childText.text = title;
            }
            else
            {
                if (required)
                {
                    childText.color = Color.red;
                    propertyContainer.color = new Color(0.5f, 0.0f, 0.0f);
                }
                else
                {
                    childText.color = new Color(0.2f, 0.2f, 0.2f);
                    propertyContainer.color = new Color(0.4f, 0.4f, 0.4f);
                }
                if (failText != null)
                {
                    childText.text = failText;
                }
                else
                {
                    childText.text = "No " + title;
                }
            }
        }

        private void ValidateEyeInfo(EyeballValidation editor, string suffix, Loli.Eye eye)
        {

            bool hasBone = eye.lookAt.bone != null;
            bool hasMaterial = eye.material != null;
            bool hasTexture;
            if (hasMaterial)
            {
                hasTexture = eye.material.mainTexture != null;
            }
            else
            {
                hasTexture = false;
            }
            bool partiallyComplete = hasBone || hasMaterial || hasTexture;
            if (partiallyComplete)
            {
                //Must complete pupil if it is partially complete
                if (hasBone && hasMaterial && hasTexture)
                {
                    editor.eyeballX.enabled = false;
                    editor.container.color = new Color(0.0f, 0.4f, 0.0f);
                }
                else
                {
                    editor.eyeballX.enabled = true;
                    editor.container.color = new Color(0.4f, 0.0f, 0.0f);
                }
            }
            else
            {
                //set as disable and optional
                editor.eyeballX.enabled = true;
                editor.container.color = new Color(0.3f, 0.3f, 0.3f);
            }
            string textureTitle;
            if (hasTexture)
            {
                textureTitle = TrimToFit(eye.material.mainTexture.name, 24);
            }
            else
            {
                textureTitle = "pupil" + suffix + "Texture";
            }
            ValidateProperty(editor.textureContainer, hasTexture, "pupil" + suffix + " texture", partiallyComplete);
            ValidateProperty(editor.boneContainer, hasBone, "Bone", partiallyComplete);
            ValidateProperty(editor.materialContainer, hasMaterial, "Material", partiallyComplete);
        }

        private void ValidateAllInfoProperties()
        {

            if (modelPreviewer.modelDefault != null)
            {

                VivaModel head = modelPreviewer.modelDefault.headModel;
                verticalInfos.SetActive(true);
                modelInfo.text = modelPreviewer.modelDefault.name;

                string skinTitle;
                if (head.texture != null)
                {
                    skinTitle = "skin: " + TrimToFit(head.texture.name, 26);
                }
                else
                {
                    skinTitle = "skin";
                }
                ValidateProperty(skinContainer, head.texture != null, skinTitle, true, "Drag and drop 1024x1024 PNG");
                ValidateEyeInfo(rightPupilInfo, "_r", modelPreviewer.modelDefault.rightEye);
                ValidateEyeInfo(leftPupilInfo, "_l", modelPreviewer.modelDefault.leftEye);

                if (head.mesh != null)
                {
                    //add animation compatibility
                    int animationCompatibility = Mathf.RoundToInt((float)(head.mesh.blendShapeCount - 1) / 27.0f * 100);
                    ValidateProperty(animationCompatibilityContainer, animationCompatibility > 0, "Facial Animations: %" + animationCompatibility, false, "No Facial Animations");
                    ValidateProperty(headpatSphereContainer, head.headpatWorldSphere.w != 0.0f, "\"" + head.name + " headpat\"", true, "Must specify headpat sphere!");
                    ValidateProperty(headCollisionSphereContainer, head.headCollisionWorldSphere.w != 0.0f, "\"" + head.name + " head collision\"", false);
                    ValidateProperty(hatLocalPosContainer, head.hatLocalPosAndPitch != Vector4.zero, "\"" + head.name + " hat\"", false);
                }
                else
                {
                    modelInfo.text = "<color=#ff0000ff>Could not read mesh</color>";
                }
                modelPreviewer.modelDefault.ValidateEyes();
            }
            else
            {
                verticalInfos.SetActive(false);
            }
            ValidateAllowTweakTab();
            ValidateAllowHatPreview();

            step1DragFiles.SetActive(modelPreviewer.modelDefault == null);
        }

        private void ValidateAllowHatPreview()
        {
            if (modelPreviewer.modelDefault != null)
            {
                hatPreviewButtonContainer.gameObject.SetActive(true);
            }
            else
            {
                hatPreviewButtonContainer.gameObject.SetActive(false);
            }
        }

        public void clickCreateCard()
        {

            if (activeCoroutine != null)
            {
                Debug.LogError("ERROR Already making a card!");
                return;
            }
            if (activeHat != null)
            {
                clickPreviewHat();  //turn off hat
            }
            StopHairMotionTest();
            modelPreviewer.SetPreviewMode(ModelPreviewViewport.PreviewMode.NONE);
            SetActiveCoroutineAction(CreateCharacterCard(modelPreviewer.modelDefault), true);
        }

        private IEnumerator CreateCharacterCard(Loli loli)
        {

            SetTab((int)Tab.CARD_RESULT);
            characterCardResultImage.gameObject.SetActive(false);
            skinCardResultImage.gameObject.SetActive(false);
            cardsResultInfo.text = "Creating character cards...";
            cardsResultInfo.color = Color.yellow;
            acceptButton.interactable = false;

            modelPreviewer.modelDefault.outfitInstance.DisableActiveDynamicBones();

            //create photoshoot images
            GameDirector.PhotoshootRequest photoshoot = new GameDirector.PhotoshootRequest(
                new Vector2Int(Steganography.PACK_SIZE, Steganography.CARD_HEIGHT),
                characterPhotoshootPose,
                characterBackground,
                modelPreviewer.modelDefaultPoseAnim
            );
            yield return GameDirector.instance.StartCoroutine(GameDirector.instance.RenderPhotoshoot(loli, photoshoot));

            //create character model card
            Steganography.PackLosslessDataRequest packRequest = new Steganography.PackLosslessDataRequest(
                modelPreviewer.modelDefault.headModel.name,
                modelPreviewer.modelDefault.headModel.SerializeToCardData(),
                photoshoot.texture,
                true
            );
            yield return GameDirector.instance.StartCoroutine(Steganography.main.ExecutePackLosslessData(packRequest));
            DisplayAndSaveCardResult(packRequest.result, characterCardResultImage, characterCardBrowser.cardFolder, packRequest.error);

            //create character skin card
            photoshoot = new GameDirector.PhotoshootRequest(
                new Vector2Int(Steganography.PACK_SIZE, Steganography.CARD_HEIGHT),
                characterPhotoshootPose,
                skinBackground,
                modelPreviewer.modelDefaultPoseAnim
            );
            yield return GameDirector.instance.StartCoroutine(GameDirector.instance.RenderPhotoshoot(loli, photoshoot));

            CardTextureSerializer serializer = new CardTextureSerializer(VivaModel.skinFormat);
            byte[] data = serializer.Serialize(
                new Texture2D[]{
                modelPreviewer.modelDefault.headModel.texture,
                modelPreviewer.modelDefault.headModel.rightEyeTexture,
                modelPreviewer.modelDefault.headModel.leftEyeTexture,
                }
            );
            if (data == null)
            {
                EndActiveCoroutineAction(serializer.error);
            }
            else
            {
                packRequest = new Steganography.PackLosslessDataRequest(
                    modelPreviewer.modelDefault.headModel.name,
                    data,
                    photoshoot.texture,
                    false
                );
                yield return GameDirector.instance.StartCoroutine(Steganography.main.ExecutePackLosslessData(packRequest));
                DisplayAndSaveCardResult(packRequest.result, skinCardResultImage, skinCardBrowser.cardFolder, packRequest.error);
            }

            acceptButton.interactable = true;
            modelPreviewer.SetPreviewLoli(null);

            EndActiveCoroutineAction(null);
            ValidateAllowTweakTab();
        }

        private void DisplayAndSaveCardResult(Texture2D card, Image targetDisplayImage, string cardFolder, string error)
        {
            if (card == null)
            {
                EndActiveCoroutineAction(error);
            }
            else
            {
                cardsResultInfo.text = "Created " + card.name;
                cardsResultInfo.color = Color.green;
                targetDisplayImage.gameObject.SetActive(true);
                targetDisplayImage.sprite = Sprite.Create(
                    card,
                    new Rect(0.0f, 0.0f, card.width, card.height),
                    Vector2.zero
                );
                Texture2D texture = Steganography.AttemptSaveCardThumbnail(card, characterCardBrowser.cardFolder + "/.thumbs");
                Destroy(texture);
                Steganography.SaveTexture(card, cardFolder);
                GameDirector.instance.StartCoroutine(CreateCardAnimation(targetDisplayImage));
            }
        }

        private IEnumerator CreateCardAnimation(Image target)
        {
            GameDirector.instance.PlayGlobalSound(cardCreateSound);
            target.material = new Material(target.material);

            float timer = 0.0f;
            while (timer < 1.0f)
            {
                timer += Time.deltaTime;
                float inv = 1.0f - timer;
                target.rectTransform.localScale = Vector3.one * (0.5f + (1.0f - Mathf.Pow(inv, 3.0f)) * 0.5f);
                target.material.SetColor("_AdditiveColor", new Color(1.0f, 1.0f, 1.0f, 0.0f) * (0.5f + timer * 0.5f));
                target.color = new Color(1.0f, 1.0f, 1.0f, timer);
                yield return null;
            }

            timer = 0.0f;
            while (timer < 0.3f)
            {
                timer += Time.deltaTime;
                float ratio = timer / 0.3f;
                float inv = 1.0f - ratio;
                float fastInv = 1.0f - inv * inv * inv;
                float easeOutFast = (1.0f - Mathf.Pow(inv, 16.0f));
                easeOutFast *= easeOutFast;
                easeOutFast *= easeOutFast;
                easeOutFast *= easeOutFast;
                float animation = Mathf.Max(1.0f + Mathf.Sin(fastInv * Mathf.PI) * easeOutFast, 1.0f);
                target.rectTransform.localScale = Vector3.one * animation;
                target.material.SetColor("_AdditiveColor", new Color(1.0f, 1.0f, 1.0f, 0.0f) * (1.0f - animation));
                yield return null;
            }
            target.rectTransform.localScale = Vector3.one;
        }

    }

}