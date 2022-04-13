using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.Networking;

namespace viva{


public partial class Wardrobe : UITabMenu{

	[Header("Create Card UI")]

	[SerializeField]
	private GameObject createTab;
	[SerializeField]
	private PageScroller createCardScroller;
	[SerializeField]
	private GameObject clothingPresetUIPrefab;
	[SerializeField]
	private Sprite clothingIconSelectedSprite;
	[SerializeField]
	private Sprite clothingIconNotSelectedSprite;
	[SerializeField]
	private GameObject dragFileVisualizer;
	[SerializeField]
	private Sprite dragFileImageIcon;
	[SerializeField]
	private Sprite dragFileImageFailIcon;
	[SerializeField]
	private Texture2D clothingBackground;

	private ClothingPreset selectedClothingPreset = null;
	private Button selectedClothingButton = null;
	private Coroutine activeCoroutine = null;
	private int clothingPresetPage = 0;

	private void InitializeCreateTab(){
		createCardScroller.FlipPage(0);	//reload current page
	}

	private int OnCreateCardMaxPages(){
		int presetsEnabled = 0;
		for( int i=0; i<GameDirector.instance.wardrobe.Count; i++ ){
			if( GameDirector.instance.wardrobe[i].enable ){
				presetsEnabled++;
			}
		}
		return presetsEnabled/6;
	}

	private void OnUpdateCreateCardPage( int page ){

		//Destroy old clothingPrefabs
		//ignore first 3 since they are necessary buttons
		for( int i=createCardScroller.GetPageContent().childCount; i-->3; ){
			Destroy( createCardScroller.GetPageContent().GetChild(i).gameObject );
		}

		//display 6 new ones if possible
		int clothingPresetIndex = 0;
		for( int i=0; i<GameDirector.instance.wardrobe.Count; i++ ){
			if( GameDirector.instance.wardrobe[i].enable ){
				if( clothingPresetIndex >= page*6 ){
					break;
				}
				clothingPresetIndex++;
			}
		}

		for( int i=0; i<6; i++ ){
			if( clothingPresetIndex >= GameDirector.instance.wardrobe.Count ){
				break;
			}
			if( !GameDirector.instance.wardrobe[clothingPresetIndex].enable ){
				clothingPresetIndex++;
				i--;
				continue;
			}
			Vector3 position = new Vector3( (-1.0f+i%3)*170, (0.5f-i/3)*205 );

			GameObject ClothingPresetGameObject = GameObject.Instantiate( clothingPresetUIPrefab, position , Quaternion.identity );
			Button clothingPresetButton = ClothingPresetGameObject.GetComponent<Button>();
			if( clothingPresetButton == null ){
				Debug.LogError("ERROR Clothing Preset Button must have a Button!");
				return;
			}
			clothingPresetButton.transform.SetParent( createCardScroller.GetPageContent(), false );

			SetupClothingPresetIcon( clothingPresetButton, GameDirector.instance.wardrobe[clothingPresetIndex] );
			clothingPresetIndex++;
		}
	}

	private void SetupClothingPresetIcon( Button clothingPresetButton, ClothingPreset preset ){

		if( preset == null ){
			Debug.LogError("Cannot setup clothing preset icon with null preset!");
			return;
		}
		SetClothingIconSelection( clothingPresetButton, clothingIconNotSelectedSprite );

		if( clothingPresetButton.transform.childCount < 2 ){
			Debug.LogError("ERROR UI Icon prefab must have 2 children!");
			return;
		}
		
		clothingPresetButton.onClick.AddListener( delegate{ OnClothingIconPressed( preset, clothingPresetButton ); } );

		Text title = clothingPresetButton.transform.GetChild(0).GetComponent<Text>();
		if( title == null ){
			Debug.LogError("ERROR UI Icon prefab 1st child must have a Text!");
			return;
		}
		title.text = preset.clothePieceName;

		Image image = clothingPresetButton.transform.GetChild(1).GetComponent<Image>();
		if( image == null ){
			Debug.LogError("ERROR UI Icon prefab 1st child must have an Image!");
			return;
		}
		image.sprite = preset.preview;
	}


	private void SetClothingIconSelection( Button clothingPresetButton, Sprite sprite ){
		Image image = clothingPresetButton.GetComponent<Image>();
		if( image == null ){
			Debug.LogError("ERROR Base Clothing Prefab Icon should have an Image!");
			return;
		}
		image.sprite = sprite;
		image.enabled = false;image.enabled=true;
	}

	private void OnClothingIconPressed( ClothingPreset preset, Button clothingPresetButton ){
		
		if( selectedClothingButton != null ){
			SetClothingIconSelection( selectedClothingButton, clothingIconNotSelectedSprite );
		}
		SetClothingIconSelection( clothingPresetButton, clothingIconSelectedSprite );

		selectedClothingButton = clothingPresetButton;
		selectedClothingPreset = preset;
	}

	private bool IsRecognizedImageResolution( Texture2D image, ref bool isCard ){
		
		if( image.width != Steganography.PACK_SIZE ){
			return false;
		}
		if( image.height == Steganography.CARD_HEIGHT ){
			isCard = true;
			return true;
		}else if( image.width == Steganography.PACK_SIZE ){
			isCard = false;
			return true;
		}
		return false;
	}

	private IEnumerator AnimationScale( RectTransform target, float start, float end, float time ){

		float timer = 0.0f;
        while( timer < time ){
            float ratio = 1.0f-timer/time;
            target.localScale = Vector3.one*Mathf.LerpUnclamped(start,end,1.0f-ratio*ratio);
            timer += Time.deltaTime;
            yield return null;
        }
	}

	//load dropped image and decide if it's a card or a custom texture
	private IEnumerator OnDropTextureFile( Loli loli, string filepath, Image dragFileImage ){

		StartLoadingCycle();
		
		Text dragFileText = dragFileVisualizer.transform.GetChild(0).GetComponent<Text>();
		if( dragFileText == null ){
			Debug.LogError("ERROR drag file visualizer needs a Text object as a 1st child!");
			yield break;
		}

		dragFileVisualizer.SetActive( true );
		
		//ensure current UI state cannot be changed
		GameDirector.instance.PauseUIInput();
		string filename = System.IO.Path.GetFileName( filepath );

		//set new filename on visualizer text
		dragFileText.text = filename;
		dragFileImage.sprite = dragFileImageIcon;

		yield return GameDirector.instance.StartCoroutine( AnimationScale( dragFileVisualizer.transform as RectTransform, 0.5f, 2.0f, 0.15f ) );
		//attempt to load image
		Tools.FileTextureRequest request = new Tools.FileTextureRequest(
			filepath,
			new Vector2Int[]{ new Vector2Int( Steganography.PACK_SIZE, Steganography.PACK_SIZE ) },
			"Image PNG must be 1024x1024"
		);

		yield return GameDirector.instance.StartCoroutine( Tools.LoadFileTexture( request ) );
		if( request.result == null ){
			dragFileImage.sprite = dragFileImageFailIcon;
			activeCoroutine = GameDirector.instance.StartCoroutine( EndDropTextureFile( dragFileText, request.error ) );
			Debug.LogError( request.error );
			yield break;
		}
		
		//TODO: Need to destroy sprite when done using it!
		dragFileImage.sprite = Sprite.Create(
			request.result,
			new Rect( 0.0f, 0.0f, request.result.width, request.result.height ),
			Vector2.zero
		);
		
		Vector3 endPos = Vector3.zero;	//shrink end target pos
		if( selectedClothingButton == null ){
			Destroy( request.result );
			activeCoroutine = GameDirector.instance.StartCoroutine( EndDropTextureFile( dragFileText, "Select a preset in the create tab first!" ) );
			yield break;

		}else{	//zoom towards selected clothing preset button
			endPos = selectedClothingButton.transform.position;
			//Preload photoshoot pose if it's not a card
			ClothingPreset targetClothing = GameDirector.instance.FindClothing( selectedClothingPreset.clothePieceName );
			if( targetClothing == null ){
				Destroy( request.result );
				activeCoroutine = GameDirector.instance.StartCoroutine( EndDropTextureFile(dragFileText,"Could not find internal clothing by name "+selectedClothingPreset.clothePieceName) );
				yield break;
			}
			//set final card name
			request.result.name += " "+targetClothing.name.Split(' ')[0];
			Outfit.ClothingOverride clothingOverride = new Outfit.ClothingOverride( request.result, request.result.name );
			Outfit outfit = CreatePhotoshootOutfit(
				loli,
				targetClothing,
				clothingOverride
			);
			if( outfit == null ){
				Destroy( request.result );
				activeCoroutine = GameDirector.instance.StartCoroutine( EndDropTextureFile(dragFileText,"Could not create photoshoot outfit!") );
				yield break;
			}
			loli.SetOutfit( outfit );
	    
			GameDirector.PhotoshootRequest photoshoot = new GameDirector.PhotoshootRequest(
				new Vector2Int( Steganography.PACK_SIZE, Steganography.CARD_HEIGHT ),
				FindClothingPresetByClothingName( targetClothing.name ).photoshootPose,
				clothingBackground,
				Loli.Animation.PHOTOSHOOT_1
			);
			yield return GameDirector.instance.StartCoroutine( GameDirector.instance.RenderPhotoshoot( loli, photoshoot ) );

			resultCardImage.gameObject.SetActive( false );
			Steganography.PackLossyTextureRequest packRequest = new Steganography.PackLossyTextureRequest(
				clothingOverride.texture,
				photoshoot.texture,
				targetClothing.name,
				clothingOverride.texture.name
			);	
			yield return GameDirector.instance.StartCoroutine( Steganography.main.ExecutePackClothingCard( packRequest ) );

			//handle finished request
			Debug.Log("Finished Packing Card "+packRequest.error);
			if( packRequest.result == null ){
				resultProgressText.text = "Could not create card."+packRequest.error;
			}else{
				resultProgressText.text = "Created "+packRequest.result.name;
				resultCardImage.gameObject.SetActive( true );
				Texture2D thumbnail = Steganography.AttemptSaveCardThumbnail( packRequest.result, "Cards/Clothing" );
				resultCardImage.sprite = Sprite.Create(
					thumbnail,
					new Rect( 0.0f, 0.0f, thumbnail.width, thumbnail.height ),
					Vector2.zero
				);
				Steganography.SaveTexture( packRequest.result, "Cards/Clothing" );
				GameDirector.Destroy( packRequest.result );	//full res card texture no longer used
			}

			//Finally apply new outfit to
			loli.outfit.WearClothingPiece( loli, targetClothing, clothingOverride );
			loli.SetOutfit( loli.outfit );
		}

		//ease towards endPos
		Vector3 startPos = dragFileVisualizer.transform.position;
		float timer = 0.0f;
		while( timer < 0.5f ){
			timer += Time.deltaTime;
			float ratio = Tools.EaseOutQuad(timer/0.5f);
			dragFileVisualizer.transform.position = Vector3.LerpUnclamped( startPos, endPos, ratio );
			dragFileVisualizer.transform.localScale = Vector3.one*(1.0f-ratio*0.5f);
			yield return null;
		}
		resultCardImage.gameObject.SetActive( true );
		SetTab( (int)Tab.RESULT );
		GameDirector.instance.StartCoroutine( EndDropTextureFile( dragFileText, null ) );
	}
	
	
	private IEnumerator EndDropTextureFile( Text dragFileText, string error ){

		StopLoadingCycle();

		if( error != null ){
			dragFileText.text = error;
			yield return new WaitForSeconds(3.0f);
	
			yield return GameDirector.instance.StartCoroutine( AnimationScale( dragFileVisualizer.transform as RectTransform, 2.0f, 0.0f, 0.15f ) );
		}

		dragFileVisualizer.SetActive( false );
		GameDirector.instance.ResumeUIInput();
		activeCoroutine = null;	//end active coroutine
	}

	public void OnDropFile( List<string> files, B83.Win32.POINT aDropPoint ){
		
		//avoid calling multiple drag and drop coroutines
		if( activeCoroutine != null ){
			return;
		}
		//mouse must hit UI
		RaycastHit hitInfo = new RaycastHit();
		Ray mouseray = GameDirector.instance.mainCamera.ScreenPointToRay( GameDirector.player.mousePosition );
		if( !Physics.Raycast( mouseray.origin, mouseray.direction, out hitInfo, 2.0f, Instance.uiMask ) ){
			return;
		}
		if( files.Count == 0 ){
			return;
		}

		//ensure Shinobu Character is nearby
		Loli loli = GameDirector.instance.FindClosestCharacter<Loli>( GameDirector.player.floorPos, 10.0f );
		if( loli == null ){
			Debug.LogError("No Shinobu is nearby!");
			return;
		}
		dragFileVisualizer.SetActive( true );
		Image dragFileImage = dragFileVisualizer.GetComponent<Image>();
		if( dragFileImage == null ){
			Debug.LogError("ERROR drag file visualizer needs an Image!");
			return;
		}
		activeCoroutine = GameDirector.instance.StartCoroutine( OnDropTextureFile( loli, files[0], dragFileImage ) );

		dragFileVisualizer.transform.position = hitInfo.point;
		dragFileVisualizer.transform.rotation = Quaternion.LookRotation( hitInfo.point-GameDirector.instance.mainCamera.transform.position, Vector3.up );
	}
}

}