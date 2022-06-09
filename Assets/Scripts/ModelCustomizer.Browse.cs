using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace viva
{

    public partial class ModelCustomizer : UITabMenu
    {

        [Header("Browse")]
        [SerializeField]
        private CardBrowser m_characterCardBrowser;
        public CardBrowser characterCardBrowser { get { return m_characterCardBrowser; } }
        [SerializeField]
        private CardBrowser skinCardBrowser;
        [SerializeField]
        private GameObject errorWindow;
        [SerializeField]
        private Text errorText;
        [SerializeField]
        private Loli modelDefault;

        private ModelBuildSettings lastMBS = null;

        public class LoadLoliFromCardRequest
        {

            public readonly string filename;
            public readonly Loli target;
            public string error;

            public LoadLoliFromCardRequest(string _filename, Loli _target)
            {
                filename = _filename;
                target = _target;
            }
        }


        private void InitializeBrowseTab()
        {
            characterCardBrowser.Initialize(true, PreviewCharacterCard);
            skinCardBrowser.Initialize(false, null);
        }

        public void clickCloseErrorWindow()
        {
            errorWindow.SetActive(false);
            EndActiveCoroutineAction(null);
        }

        public void DisplayErrorWindow(string error)
        {
            SetTab((int)Tab.NONE);
            errorWindow.SetActive(true);
            errorText.text = error;
        }

        public void PreviewCharacterCard(string name, Button sourceButton)
        {
            StartLoadingCycle();
            characterCardBrowser.SeAllCardsInteractible(false);
            SetAllTabButtonsInteractible(false);
            GameDirector.instance.StartCoroutine(HandlePreviewVivaModel(name));
        }

        private IEnumerator HandlePreviewVivaModel(string name)
        {

            LoadLoliFromCardRequest cardRequest = new LoadLoliFromCardRequest(name, modelDefault);
            yield return GameDirector.instance.StartCoroutine(LoadVivaModelCard(cardRequest));
            if (cardRequest.error != null)
            {
                EndActiveCoroutineAction(cardRequest.error);
                yield break;
            }
            var serializedLoli = new GameDirector.VivaFile.SerializedLoli(name, new GameDirector.VivaFile.SerializedAsset(name));

            bool finished = false;
            GameDirector.instance.StartCoroutine(modelDefault.InitializeLoli(serializedLoli, delegate
            {
                finished = true;
            }));
            while (!finished)
            {
                yield return null;
            }

            modelDefault.spine1RigidBody.isKinematic = true;
            modelDefault.SetOutfit(Outfit.Create(new string[0], false));

            modelPreviewer.SetPreviewLoli(cardRequest.target);
            ValidateAllInfoProperties();
        }

        public IEnumerator LoadVivaModelCard(LoadLoliFromCardRequest cardRequest)
        {
            if (cardRequest.target == null)
            {
                cardRequest.error = "Target loli is null";
                EndActiveCoroutineAction(cardRequest.error);
                yield break;
            }
            //Find matching model texture if it exists
            CardBrowser.LoadCardTextureRequest modelTextureRequest = new CardBrowser.LoadCardTextureRequest(cardRequest.filename);
            yield return GameDirector.instance.StartCoroutine(characterCardBrowser.LoadCardTexture(modelTextureRequest));
            if (modelTextureRequest.result == null)
            {
                cardRequest.error = modelTextureRequest.error;
                EndActiveCoroutineAction(modelTextureRequest.error);
                yield break;
            }

            Steganography.UnpackLosslessDataRequest modelCardDataRequest = new Steganography.UnpackLosslessDataRequest(modelTextureRequest.result, true);
            yield return GameDirector.instance.StartCoroutine(Steganography.main.ExecuteUnpackCharacter(modelCardDataRequest));

            if (modelCardDataRequest.result == null)
            {
                cardRequest.error = modelCardDataRequest.error;
                EndActiveCoroutineAction(modelCardDataRequest.error);
                yield break;
            }

            //Find matching skin texture if it exists
            CardBrowser.LoadCardTextureRequest skinRequest = new CardBrowser.LoadCardTextureRequest(modelTextureRequest.result.name);
            yield return GameDirector.instance.StartCoroutine(skinCardBrowser.LoadCardTexture(skinRequest));

            if (skinRequest.result == null)
            {
                cardRequest.error = skinRequest.error;
                EndActiveCoroutineAction(skinRequest.error);
                yield break;
            }

            Steganography.UnpackLosslessDataRequest skinDataRequest = new Steganography.UnpackLosslessDataRequest(skinRequest.result, false);
            yield return GameDirector.instance.StartCoroutine(Steganography.main.ExecuteUnpackCharacter(skinDataRequest));

            if (skinDataRequest.result == null)
            {
                cardRequest.error = skinDataRequest.error;
                EndActiveCoroutineAction(skinDataRequest.error);
                yield break;
            }

            //unpack textures from data
            CardTextureSerializer serializer = new CardTextureSerializer(VivaModel.skinFormat);
            Texture2D[] modelTextures = serializer.Deserialize(skinDataRequest.result);
            if (modelTextures == null)
            {
                cardRequest.error = serializer.error;
                EndActiveCoroutineAction(serializer.error);
                yield break;
            }

            //Decode card data into viva model data (model and settings)
            ModelBuildSettings mbs = new ModelBuildSettings(
                skinShader,
                clothingShader,
                pupilShader,
                new string[] { "skin", "pupil_r", "pupil_l" }
            );
            mbs.modelTexture = modelTextures[0];
            mbs.rightEyeTexture = modelTextures[1];
            mbs.leftEyeTexture = modelTextures[2];

            VivaModel.CreateLoliRequest createLoliRequest = new VivaModel.CreateLoliRequest(cardRequest.filename, cardRequest.target, modelCardDataRequest.result, mbs);
            yield return GameDirector.instance.StartCoroutine(VivaModel.DeserializeVivaModel(createLoliRequest));
            if (createLoliRequest.error != null)
            {
                cardRequest.error = createLoliRequest.error;
                EndActiveCoroutineAction("Could not create Viva model!");
                yield break;
            }
            lastMBS = createLoliRequest.mbs;
            EndActiveCoroutineAction(null);
        }
    }

}