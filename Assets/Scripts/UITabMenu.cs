using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

public abstract class UITabMenu : UIMenu{

    private GameObject[] tabs;
    private int lastTabIndex;
    public int lastValidTabIndex { get; private set; }

    private Coroutine loadingCycleCoroutine = null;

    [SerializeField]
    private Transform loadingCycle;


    protected void InitializeTabs( GameObject[] _tabs ){
        tabs = _tabs;
        lastTabIndex = tabs.Length;
    }

    private GameObject GetTabContainer( int tabIndex ){
        
        if( tabIndex < tabs.Length ){
            return tabs[tabIndex];
        }
        return null;
    }

    public void SetTab( int tabIndex ){

        GameObject lastTab = GetTabContainer( lastTabIndex );
        if( lastTab != null ){
            lastTab.SetActive( false );
        }
        GameObject newtab = GetTabContainer( tabIndex );
        lastTabIndex = tabIndex;
        if( newtab != null ){
            lastValidTabIndex = tabIndex;
            newtab.SetActive( true );
            OnValidTabChange( tabIndex );
        }
    }

    protected abstract void OnValidTabChange( int newTab );
    
    public void StartLoadingCycle(){
        if( loadingCycleCoroutine != null ){
            return;
        }
        loadingCycle.gameObject.SetActive( true );
        loadingCycleCoroutine = GameDirector.instance.StartCoroutine( LoadingCycleAnimation() );
    }

    public void StopLoadingCycle(){
        if( loadingCycleCoroutine == null ){
            return;
        }
        loadingCycle.gameObject.SetActive( false );
        GameDirector.instance.StopCoroutine( loadingCycleCoroutine );
        loadingCycleCoroutine = null;
    }

    private IEnumerator LoadingCycleAnimation(){
        while( true ){
            loadingCycle.localEulerAngles = new Vector3( 0.0f, 0.0f, Mathf.Floor( Time.time*8.0f )*45.0f );
            yield return null;
        }
    }
}

}