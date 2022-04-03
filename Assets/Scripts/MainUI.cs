using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;


namespace viva{


public enum MainUITab{
    WELCOME,
    NEW,
    LOAD,
    SETTING,
}

public class MainUI: UI{

    [SerializeField]
    private GameObject nav;
    [SerializeField]
    private LibraryExplorer sessionExplorer;

    private VivaPlayer user;

    
    public void OpenLoadTab(){
        DisplaySessionOptions( "New Session", "save" );
    }

    public void OpenNewTab(){
        DisplaySessionOptions( "New Session", "template" );
    }

    private void DisplaySessionOptions( string title, string sessionTypeFilter ){
        sessionExplorer.SetDefaultPrepare( delegate{
            sessionExplorer.DisplaySelection(
                title,
                sessionExplorer.ExpandDialogOptions( ImportRequestType.SESSION, null ),
                delegate( DialogOption option, LibraryEntry source ){
                    SelectSession( option.value );
                },
                null,
                null,
                delegate( SpawnableImportRequest request ){
                    SceneRequest sceneRequest = request as SceneRequest;
                    return sceneRequest.sceneSettings.type == sessionTypeFilter;
                }
            );
        });
        OpenTab( "Session" );
    }

    public override void OpenTab( string newTab ){
        base.OpenTab( newTab );

        nav.SetActive( newTab != "Welcome" );
    }
}

}