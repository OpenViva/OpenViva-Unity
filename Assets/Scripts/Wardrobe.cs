using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

namespace viva{


public partial class Wardrobe : UITabMenu{

	public static Wardrobe main { get; private set;}

	private enum Tab{
		BROWSE,
		CREATE,
		RESULT,
		NONE
	}

	[SerializeField]
	private Button createTabButton;
	[SerializeField]
	private Text lolisSelected;


	public void Awake() {
        InitializeTabs( new GameObject[]{ browseTab, createTab, resultTab } );
		main = this;
		createCardScroller.Initialize( OnCreateCardMaxPages, OnUpdateCreateCardPage );
		this.enabled = false;
	}

	public void OnApplicationQuit(){
		FileDragAndDrop.DisableDragAndDrop();
	}

	public override void OnBeginUIInput(){
		
		this.enabled = true;
		Text text = createTabButton.transform.GetChild(0).GetComponent<Text>();
		
		if( GameDirector.player.controls == Player.ControlType.OPEN_VR ){
			createTabButton.interactable = false;
			text.text = "Cannot Create in VR";
		}else{
			createTabButton.interactable = true;
			text.text = "Create";
		}
		FileDragAndDrop.EnableDragAndDrop( OnDropFile );

		lolisSelected.text = GameDirector.player.objectFingerPointer.selectedLolis.Count+" lolis selected";
	}

    public override void OnExitUIInput(){
		this.enabled = false;
		FileDragAndDrop.DisableDragAndDrop();
		SetTab( (int)Tab.NONE );

		lolisSelected.text = "";
    }

	protected override void OnValidTabChange( int tab ){

		switch( (Tab)tab ){
		case Tab.BROWSE:
			InitializeBrowseTab();
			break;
		case Tab.CREATE:
			InitializeCreateTab();
			break;
		}
 	}

// #if UNITY_EDITOR
// 	private void Update(){
		
// 		if( InputOLD.GetKey(KeyCode.T)){
// 			List<string> files = new List<string>();
// 			files.Add( "C:/Users/Master-Donut/TEST.png" );
// 			OnDropFile( files, new B83.Win32.POINT(500,500));
// 		}
// 	}
// #endif
	
	public void ShowExplorer( string subFolder ){
		GameDirector.instance.DisableNextClick();
		subFolder = Path.GetDirectoryName(Application.dataPath)+"/"+subFolder;
        subFolder = subFolder.Replace(@"/", @"\");   // explorer doesn't like front slashes
        // System.Diagnostics.Process.Start("explorer.exe", "/select,"+subFolder);
        System.Diagnostics.Process.Start("explorer.exe", "/select,"+subFolder);
    }

	public void clickShowClothingCardFolder(){
		ShowExplorer( clothingCardBrowser.cardFolder );
	}
	
	public void clickShowClothingUVsFolder(){
		ShowExplorer( "UVs" );
	}

	public void clickResetClothing(){
		ResetClothing();
	}

	public void clickBrowseTab(){
		SetTab( (int)Tab.BROWSE );
	}
	
	public void clickCreateTab(){
		SetTab( (int)Tab.CREATE );
	}

	private void ResetClothing(){
		
		foreach( var loli in GameDirector.player.objectFingerPointer.selectedLolis ){
			Outfit resetOutfit = Outfit.Create(
				new string[]{
				},
				false
			);
			loli.SetOutfit( resetOutfit );
		}
	}
}

}