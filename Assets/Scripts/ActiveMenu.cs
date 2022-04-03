using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;


namespace viva{

public class ActiveMenu : MonoBehaviour{

    [SerializeField]
    private ScrollViewManager characterScroll;
    private List<VivaInstance> activeInstances = new List<VivaInstance>();
    [SerializeField]
    private RectTransform hoverContainer;
    [SerializeField]
    private Image hoverImage;
    [SerializeField]
    private Text hoverDescText;

    private bool needsRefresh = false;


    public void OnEnable(){
        needsRefresh = true;
        CheckRefresh();
    }

    public void OnDisable(){
        hoverContainer.gameObject.SetActive( false );
    }

    public void Add( VivaInstance instance ){
        if( instance == null ) return;
        if( activeInstances.Contains( instance ) ) return;
        activeInstances.Add( instance );
        needsRefresh = true;
        CheckRefresh();

        instance._internalOnDestroy += delegate{
            Remove( instance );
        };
    }

    public void Remove( VivaInstance instance ){
        activeInstances.Remove( instance );
        needsRefresh = true;
        CheckRefresh();
    }

    private void CheckRefresh(){
        if( !needsRefresh ) return;
        needsRefresh = false;

        int i=0;
        characterScroll.SetContentCount( activeInstances.Count, delegate( Transform prefabEntry ){
            Button button = prefabEntry.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            Text title = prefabEntry.GetChild(0).GetComponent<Text>();

            var instance = activeInstances[i++];
            if( instance._internalSettings != null ){
                if( !GameUI.main.isInEditMode && instance._internalSettings.hide ){
                    prefabEntry.gameObject.SetActive( false );
                    return;
                }
                prefabEntry.gameObject.SetActive( true );
            }
            title.text = instance.name;

            var character = instance as Character;
            if( character && character.isPossessed ){
                button.interactable = false;
            }else{
                button.interactable = true;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener( delegate{
                hoverContainer.gameObject.SetActive( false );
                Sound.main.PlayGlobalUISound( UISound.UNDO );
                var cachedInstance = instance;
                GameObject.DestroyImmediate( cachedInstance.gameObject );
            });

            var mhc = button.gameObject.AddComponent<MouseHoverCallbacks>();
            mhc.onEnter = delegate{
                if( instance == null ) return;
                hoverContainer.gameObject.SetActive( true );
                hoverDescText.text = instance.name;
                var thumbnail = instance.GetThumbnail();
                if( thumbnail.texture ) hoverImage.sprite = Sprite.Create( thumbnail.texture, new Rect( 0, 0, thumbnail.texture.width, thumbnail.texture.height ), Vector2.zero, 1, 0, SpriteMeshType.FullRect );
            };
            mhc.onExit = delegate{
                hoverContainer.gameObject.SetActive( false );
            };
            mhc.whileHovering += delegate{
                hoverContainer.localPosition = UITools.GetScreenFitWindowPos( Viva.input.mousePosition, hoverContainer, out bool farX );
                if( !farX ){
                    hoverDescText.alignment = TextAnchor.MiddleLeft;
                }else{
                    hoverDescText.alignment = TextAnchor.MiddleRight;
                }
            };

            var editButton = button.transform.GetChild(1).GetComponent<Button>();
            editButton.gameObject.SetActive( GameUI.main.isInEditMode );
            editButton.interactable = button.interactable;
            editButton.onClick.RemoveAllListeners();
            editButton.onClick.AddListener( delegate{
                DestroyImmediate( instance.gameObject );
                
                var request = new List<ImportRequest>(){ ImportRequest.CreateRequest( instance.assetFilepath )};
                var assetProcessor = GameUI.main.createMenu.CreateAssetProcessor( request );
                GameUI.main.OpenTab( "Create");
                assetProcessor.ProcessAll();
            } );
        });
    }
}

}