using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace viva{

public class LibraryEntry : MonoBehaviour{

    [SerializeField]
    public Button button;
    [SerializeField]
    public Image thumbnail;
    [SerializeField]
    public Text label;
    [SerializeField]
    public LoadingDots loadingDots;

    public object tag;
    public LibraryExplorer explorer;

    
    public void SetThumbnailSprite( Texture2D texture ){
        thumbnail.sprite = Sprite.Create( texture , new Rect( 0, 0, texture.width, texture.height ), Vector2.zero, 1, 0, SpriteMeshType.FullRect );
    }
}

}