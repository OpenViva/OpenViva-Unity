using UnityEngine;
using System.Collections;

using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
 
namespace viva{

 
public partial class PauseMenu : UIMenu {
    
    [Header("Manual")]
    [SerializeField]
    private Text leftPageText;
    [SerializeField]
    private Text rightPageText;
    [SerializeField]
    private GameObject manualButtonPrefab;
    [SerializeField]
    private Transform manualButtonsParent;
    
    private int manualPageSubIndex = 0;

    
    public void clickManual(){
        SetPauseMenu( Menu.MANUAL );
    }

    public void clickGoToManualPage( Transform source ){
        SetShowManualRoot( false );

        int manualIndex = source.GetSiblingIndex();
        if( GameDirector.instance.language == null ){
            return;
        }
        Language.ManualEntry[] entries = GameDirector.instance.language.manualEntries;
        if( entries == null || manualIndex >= entries.Length ){
            return;
        }
        Language.ManualEntry entry = entries[ manualIndex ];
        manualPageSubIndex = Mathf.Clamp( entry.pages.Length/2, 0, manualPageSubIndex );

        if( manualPageSubIndex*2 < entry.pages.Length ){
            leftPageText.text = entry.pages[ manualPageSubIndex*2 ];
        }else{
            leftPageText.text = "";
        }
        if( manualPageSubIndex*2+1 < entry.pages.Length ){
            rightPageText.text = entry.pages[ manualPageSubIndex*2+1 ];
        }else{
            rightPageText.text = "";
        }
    }

    private void SetShowManualRoot( bool show ){
        Transform leftPage = GetLeftPageUIByMenu( Menu.MANUAL ).transform;
        Transform rightPage = GetRightPageUIByMenu( Menu.MANUAL ).transform;
        int target = System.Convert.ToInt32( show );

        leftPage.GetChild( target ).gameObject.SetActive( false );
        rightPage.GetChild( target ).gameObject.SetActive( false );

        leftPage.GetChild( 1-target ).gameObject.SetActive( true );
        rightPage.GetChild( 1-target ).gameObject.SetActive( true );

        if( show ){
            //build manual entries
            for( int i=0; i<manualButtonsParent.childCount; i++ ){
                manualButtonsParent.GetChild(i).gameObject.SetActive( false );
            }
            if( GameDirector.instance.language != null && GameDirector.instance.language.manualEntries != null ){
                var entries = GameDirector.instance.language.manualEntries;
                for( int i=0; i<entries.Length; i++ ){
                    Transform buttonEntry = manualButtonsParent.GetChild(i);
                    manualButtonsParent.GetChild(i).gameObject.SetActive( true );

                    Text text = buttonEntry.GetChild(0).GetComponent<Text>();
                    text.text = entries[i].manualTitle;
                }
            }
        }
    }
}

}