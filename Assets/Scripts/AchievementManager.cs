using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace viva{

public class Achievement{
    public string name;
    public string description;
    public string itemIcon;
    public bool complete;

    public Achievement( string _name, string _description, string _itemIcon=null, bool _complete=false ){
        name = _name;
        description = _description;
        itemIcon = _itemIcon;
        complete = _complete;
    }

    private static List<Achievement> m_active = new List<Achievement>();
    public static IList<Achievement> active { get{ return m_active.AsReadOnly(); } }

    public static void _InternalReset(){
        m_active.Clear();
    }

    public static void Add( string _name, string description, string _itemIcon=null, bool _complete=false ){
        foreach( var achievement in active ){
            if( achievement.name == _name ) return;
        }
        m_active.Add( new Achievement( _name, description, _itemIcon, _complete ) );
    }
}

public class AchievementManager : MonoBehaviour{

    [SerializeField]
    private ScrollViewManager list;
    [SerializeField]
    private RectTransform tooltipHint;
    [SerializeField]
    private Text tooltipText;
    [SerializeField]
    private Image tooltipImage;
    [SerializeField]
    private Text pageText;

    private int lastPage;
    public static AchievementManager main;


    public void _InternalReset(){
        Achievement._InternalReset();

        Awake();
    }

    public void CompleteAchievement( string achievementName, bool showUI=false ){
        foreach( var achievement in Achievement.active ){
            if( achievement.name == achievementName ){
                if( showUI && !achievement.complete ){
                    Sound.main.PlayGlobalUISound( UISound.ACHIEVEMENT );
                    if( MessageManager.main ) MessageManager.main.DisplayMessage( Camera.main.transform.TransformPoint( Vector3.forward*1.5f+Vector3.up*0.5f ) , "Completed: "+achievement.name, null, false, false );
                }
                achievement.complete = true;
                break;
            }
        }
    }

    private void Awake(){
        main = this;

        gameObject.SetActive( false );
    }

    private void OnEnable(){
        DisplayAchievements();
    }

    public void PrevPage(){
        DisplayAchievements( Mathf.Min( Mathf.Max( lastPage-1, 0 ), Mathf.Max( 0, Achievement.active.Count-1)/10 ) );
    }

    public void NextPage(){
        DisplayAchievements( Mathf.Max( 0, Mathf.Min( lastPage+1, Mathf.Max( 0, Achievement.active.Count-1)/10 ) ) );
    }

    public void DisplayAchievements( int page=0 ){
        lastPage = page;
        pageText.text = (page+1)+"/"+( Mathf.Max( 0, Achievement.active.Count-1)/10+1 );
        int index = page*10;
        var count = Mathf.Clamp( Achievement.active.Count-page*10, 0, 10 );

        list.SetContentCount( count, delegate( Transform buttonTransform ){
            var achievement = Achievement.active[ index++ ];

            var buttonImage = buttonTransform.GetComponent<Image>();
            buttonImage.color = achievement.complete ? Color.white : new Color( 1f, 0.6f, 0.6f );

            var text = buttonTransform.GetComponentInChildren<Text>();
            text.text = achievement.name;

            var mhc = buttonTransform.GetComponent<MouseHoverCallbacks>();
            if( !mhc ) mhc = buttonTransform.gameObject.AddComponent<MouseHoverCallbacks>();
            mhc.onEnter = delegate{
                tooltipHint.gameObject.SetActive( true );
                tooltipText.text = achievement.description;
                if( achievement.itemIcon == null ){
                    tooltipImage.gameObject.SetActive( false );
                }else{
                    tooltipImage.gameObject.SetActive( true );

                    Texture2D itemTexture;
                    var itemRequest = Item.instances._InternalGetRequest( achievement.itemIcon, false ) as ItemRequest;
                    if( itemRequest != null && itemRequest.itemSettings != null ){
                        itemRequest.itemSettings.GenerateThumbnail();
                        itemTexture = itemRequest.itemSettings.thumbnail.texture;
                    }else{
                        var charRequest = Character.instances._InternalGetRequest( achievement.itemIcon, false ) as CharacterRequest;
                        if( charRequest != null && charRequest.characterSettings != null ){
                            charRequest.characterSettings.GenerateThumbnail();
                            itemTexture = charRequest.characterSettings.thumbnail.texture;
                        }else{
                            itemTexture = BuiltInAssetManager.main.defaultFBXThumbnail;
                        }
                    }
                    tooltipImage.sprite = Sprite.Create( itemTexture, new Rect( 0, 0, itemTexture.width, itemTexture.height ), Vector2.zero, 1, 0, SpriteMeshType.FullRect );
                }
            };
            mhc.onExit = delegate{
                tooltipHint.gameObject.SetActive( false );
            };
            mhc.whileHovering += delegate{
                tooltipHint.localPosition = UITools.GetScreenFitWindowPos( Viva.input.mousePosition, tooltipHint, out bool farX );
                tooltipHint.localPosition = new Vector2( tooltipHint.localPosition.x+tooltipHint.rect.size.x, tooltipHint.localPosition.y );
            };
        } );
    }
}

}