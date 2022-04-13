using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;


[System.Serializable]
public class PageScroller{

    public delegate int OnMaxPageManifest();
    public delegate void OnPageUpdate( int page );
    
    [SerializeField]
    private RectTransform pageContent;

    [SerializeField]
    private Button rightPageButton;
    
    [SerializeField]
    private Button leftPageButton;

    [SerializeField]
    private Text pageText;

    private int m_page = 0;
    public int page { get{ return m_page; } }
    private OnMaxPageManifest onMaxPageManifest;
    private OnPageUpdate onPageUpdate;
    private bool pageChangeEnabled = true;

    public void FlipPage( int delta ){
        if( !pageChangeEnabled ){
            return;
        }
        int newPage = m_page+delta;
        int maxPages = onMaxPageManifest();
        rightPageButton.gameObject.SetActive( false );
        leftPageButton.gameObject.SetActive( false );
        if( newPage >= maxPages ){
            newPage = maxPages;
        }else{
            rightPageButton.gameObject.SetActive( true );
        }
        if( newPage <= 0 ){
            newPage = 0;
        }else{
            leftPageButton.gameObject.SetActive( true );
        }
        //prevent repeating same page load unless delta is zero
        if( newPage == m_page && delta != 0 ){
            return;
        }
        m_page = newPage;
        pageText.text = "Page "+(m_page+1)+"/"+(maxPages+1);
        onPageUpdate( m_page );
    }

    public void SetEnablePageChange( bool enable ){
        pageChangeEnabled = enable;
    }

    public void Initialize( OnMaxPageManifest maxPageManifest, OnPageUpdate pageUpdate ){
        onMaxPageManifest = maxPageManifest;
        onPageUpdate = pageUpdate;

        rightPageButton.onClick.AddListener( delegate {FlipPage(1);} );
        leftPageButton.onClick.AddListener( delegate {FlipPage(-1);} );
    }

    public RectTransform GetPageContent(){
        return pageContent;
    } 
}