using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace viva
{


    public class BagUIMenu : UIMenu
    {

        [SerializeField]
        private PageScroller itemPageScroller;
        [SerializeField]
        private Bag bag;
        [SerializeField]
        private GameObject[] itemEntries = new GameObject[4];

        private Coroutine updatePageManifestCoroutine = null;

        public void Initialize()
        {
            itemPageScroller.Initialize(OnMaxPageManifest, OnPageUpdate);
        }
        public override void OnBeginUIInput()
        {
            gameObject.SetActive(true);
            RefreshMenuContents();
        }
        public override void OnExitUIInput()
        {
            gameObject.SetActive(false);
        }

        public void RefreshMenuContents()
        {
            itemPageScroller.FlipPage(0);
        }

        public void ClickTakeOutOfBag(int index)
        {
            bag.TakeOutOfBag(itemPageScroller.page * 4 + index);
            RefreshMenuContents();
        }

        public int OnMaxPageManifest()
        {
            return Mathf.CeilToInt(bag.storedItems.Count / 4);
        }
        public void OnPageUpdate(int page)
        {

            if (updatePageManifestCoroutine != null)
            {
                return;
            }
            GameDirector.instance.StartCoroutine(UpdatePageManifest(page));
        }

        private IEnumerator UpdatePageManifest(int page)
        {

            RectTransform contentRoot = itemPageScroller.GetPageContent();
            for (int i = 0; i < contentRoot.childCount; i++)
            {
                contentRoot.GetChild(i).gameObject.SetActive(false);
            }

            RenderTexture renderTexture = null;
            int categoriesInPage = bag.storedItems.Count - page * 4;
            for (int i = 0; i < categoriesInPage; i++)
            {
                GameObject categoryRoot = contentRoot.GetChild(i).gameObject;
                categoryRoot.SetActive(true);

                Bag.BagItemCategory category = bag.storedItems[page * 4 + i];
                if (category.icon == null)
                {
                    //initialize render target
                    if (renderTexture == null)
                    {
                        renderTexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
                    }
                    category.RenderIcon(renderTexture);
                    yield return null;
                }

                Image image = categoryRoot.GetComponent<Image>();
                image.sprite = Sprite.Create(
                    category.icon,
                    new Rect(0, 0, 256, 256),
                    new Vector2(0.5f, 0.5f),
                    1.0f, 0, SpriteMeshType.FullRect,
                    new Vector4(1, 1, 1, 1), false
                );
                Text text = categoryRoot.transform.GetChild(0).GetComponent<Text>();
                text.text = "" + category.items.Count;
                yield return null;
            }

            if (renderTexture != null)
            {
                renderTexture.Release();
            }

            updatePageManifestCoroutine = null;
        }

    }

}