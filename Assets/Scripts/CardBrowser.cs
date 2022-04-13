using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEngine.UI;


namespace viva{

[System.Serializable]
public class CardBrowser{

	public delegate void ExecuteLoadCard( string name, Button sourceCard );

	public class Card{

		public static int THUMBNAIL_SCALE = 8;
		public readonly Button button;
		public readonly Image image;
		public readonly Text text;

		private string name;
		private Texture2D thumbnail;
		private Sprite thumbnailSprite;
		private GameObject progressCycle;
		private Coroutine progressCycleCoroutine = null;

		public Card( GameObject container ){
			button = container.GetComponent<Button>();
			image = container.GetComponent<Image>();
			text = container.transform.GetChild(0).GetComponent<Text>();
		}

		public void Initialize( CardBrowser cardBrowser, string _name, ExecuteLoadCard executeLoadCard ){
			
			name = _name;
			SetThumbnail( null );
			button.onClick.RemoveAllListeners();
			button.onClick.AddListener( delegate{ executeLoadCard( name, button ); } );

			if( cardBrowser.loadThumbnails ){
				if( !File.Exists( cardBrowser.cardFolder+"/.thumbs/"+name) ){
					GameDirector.instance.StartCoroutine( GenerateThumbnailThenLoadThumbnail( cardBrowser.cardFolder ) );
				}else{
					GameDirector.instance.StartCoroutine( LoadThumbnail( cardBrowser.cardFolder ) );
				}
			}
		}

		private IEnumerator GenerateThumbnailThenLoadThumbnail( string cardFolder ){
			
			Texture2D cardTexture = new Texture2D( Steganography.PACK_SIZE, Steganography.CARD_HEIGHT, TextureFormat.RGB24, false, false );
			yield return new WaitForSeconds(0.05f);
			byte[] rawFileData;
			try{
				rawFileData = File.ReadAllBytes( cardFolder+"/"+name );
			}catch( System.Exception e ){
				Debug.LogError("ERROR Could not load image! "+e);
				yield break;
			}
			yield return new WaitForSeconds( 0.05f );
			ImageConversion.LoadImage( cardTexture, rawFileData, false );
			yield return new WaitForSeconds(0.05f);
			cardTexture.Apply();
			cardTexture.name = name.Split('.')[0];

			Texture2D newThumbnail = Steganography.AttemptSaveCardThumbnail( cardTexture, cardFolder+"/.thumbs" );
			if( newThumbnail != null ){
				Debug.Log("Imported "+name );
				SetThumbnail( newThumbnail );
			}else{
				Debug.LogError("Could not import card "+name );
			}
			GameDirector.Destroy( cardTexture );
		}
		
		private IEnumerator LoadThumbnail( string cardpath ){

			//load preview thumbnail
			byte[] thumbnailData;
			try{
				thumbnailData = File.ReadAllBytes( cardpath+"/.thumbs/"+name );
			}catch{
				Debug.LogError("ERROR Could not load thumbnail image at "+cardpath+"/.thumbs/"+name);
				yield break;
			}
			int thumbnailWidth = Steganography.PACK_SIZE/THUMBNAIL_SCALE;
			int thumbnailHeight = Steganography.CARD_HEIGHT/THUMBNAIL_SCALE;
			Texture2D newThumbnail = new Texture2D( thumbnailWidth, thumbnailHeight, TextureFormat.RGB24, false, false );
			ImageConversion.LoadImage( newThumbnail, thumbnailData, false );
			newThumbnail.Apply();

			SetThumbnail( newThumbnail );
		}

		public void SetThumbnail( Texture2D newThumbnail ){
			if( thumbnail != null ){
				GameDirector.Destroy( thumbnail );
				GameDirector.Destroy( thumbnailSprite );
			}
			thumbnail = newThumbnail;
			if( newThumbnail == null ){
				return;
			}
			thumbnailSprite = Sprite.Create( thumbnail, new Rect(0, 0,thumbnail.width, thumbnail.height), new Vector2( 0.5f, 0.5f ) );
			image.sprite = thumbnailSprite;
		}

		private void StopProgressCycle(){
			if( progressCycleCoroutine != null ){
				GameDirector.Destroy( progressCycle );
				GameDirector.instance.StopCoroutine( progressCycleCoroutine );
				progressCycleCoroutine = null;
			}
		}
		private void PlayProgressCycle( GameObject progressCyclePrefab ){
			StopProgressCycle();
			progressCycle = GameObject.Instantiate(
				progressCyclePrefab,
				button.transform.position,
				button.transform.rotation,
				button.transform
			);
			progressCycleCoroutine = GameDirector.instance.StartCoroutine( ProgressCycleAnimation() );
		}

		private IEnumerator ProgressCycleAnimation(){
			
			float scale = 0.0f;
			while( true ){
				scale += (1.0f-scale)*Time.deltaTime;
				progressCycle.transform.localScale = Vector3.one*scale;
				progressCycle.transform.localEulerAngles = new Vector3( 0.0f, 0.0f, Time.time*180.0f );
				yield return null;
			}
		}

		private IEnumerator FlashCardUIElement( Image image ){
			float timer = 0;
			while( timer < 0.3f ){
				timer += Time.deltaTime;
				if( image == null ){	//was destroyed externally
					yield break;
				}
				float ratio = 0.5f+Mathf.Clamp01( timer/0.3f )*0.5f;
				image.transform.localScale = Vector3.LerpUnclamped( Vector3.one*3.0f, Vector3.one, Tools.EaseOutQuad(ratio) );
				image.color = Color.LerpUnclamped( Color.green, Color.white, ratio );

				yield return null;
			}
		}
	}

	public class LoadCardTextureRequest{
		public readonly string name;
		public Texture2D result;
		public string error;

		public LoadCardTextureRequest( string _name ){
			name = _name;
		}
	}

	[SerializeField]
	private string m_cardFolder;
	public string cardFolder { get{ return m_cardFolder; } }
    [SerializeField]
    private PageScroller cardScroller;
	[SerializeField]
	private RectTransform cardEntriesContainer;

	private Coroutine loadCardsCoroutine = null;
	private Card[] cardEntries;
	private string[] cardManifest = null;
	private string[] filteredCards = null;
	private ExecuteLoadCard executeLoadCard;
	public bool loadThumbnails { get; private set; }


	public void Initialize( bool _loadThumbnails, ExecuteLoadCard _executeLoadCard ){
		loadThumbnails = _loadThumbnails;
		executeLoadCard = _executeLoadCard;
		cardScroller.Initialize( OnLoadCardMaxPages, OnUpdateLoadCardPage );
		InitializeCardEntries();
		RefreshCards();
	}

	private void InitializeCardEntries(){
		if( cardEntries != null ){
			return;
		}
		cardEntries = new Card[ cardEntriesContainer.childCount ];
		for( int i=0; i<cardEntries.Length; i++ ){
			cardEntries[i] = new Card( cardEntriesContainer.GetChild(i).gameObject );
		}
	}

	public IEnumerator LoadCardTexture( LoadCardTextureRequest request ){
		
		request.result = null;
		request.error = null;

		FileStream fs = null;
		string path = cardFolder+"/"+request.name;
		try{
			fs = new FileStream(path, FileMode.Open, FileAccess.Read);
			if( !fs.CanRead ){
				fs = null;
			}
		}catch( System.Exception e ){
			request.error = e.Message;
			fs = null;
		}


		if( fs == null ){
			request.error += "\nCould not open file "+path;
			yield break;
		}

		byte[] data = new byte[ fs.Length ];
		const float targetLoadWait = 0.5f; //seconds
		const float waitLength = 0.03f;
		int waits = (int)(targetLoadWait/waitLength);
		int bytesPerRead = (int)(fs.Length/waits);
		int bytesLeft = (int)fs.Length;
		while( fs.CanRead && bytesLeft > 0 ){
			bytesPerRead = Mathf.Min( bytesPerRead, bytesLeft );
			fs.Read(data, data.Length-bytesLeft, bytesPerRead );
			bytesLeft -= bytesPerRead;
			yield return new WaitForSeconds(waitLength);
		}
		fs.Close();

		Texture2D texture = new Texture2D( Steganography.PACK_SIZE, Steganography.CARD_HEIGHT, TextureFormat.RGB24, false, false );
		if( ImageConversion.LoadImage( texture, data, false ) ){
			texture.name = request.name;
			request.result = texture;
			if( request.result.width == 8 && request.result.height == 8 ){
				GameDirector.Destroy( request.result );
				request.result = null;
				request.error = "ERROR Could not read texture!";
			}
		}else{
			request.error = "ERROR Could not read texture!";
		}
		
	}

	public string[] FindAllExistingCardsInFolders(){
		var thumbsPath = Steganography.EnsureFolderExistence(m_cardFolder+"/.thumbs");

		//combine thumbs and Deck files
		FileInfo[] files = new DirectoryInfo(thumbsPath).GetFiles();
		HashSet<string> cardsInFolderDict = new HashSet<string>();
		AddFilesIfValidCardPath( files, m_cardFolder, cardsInFolderDict );

		var deckPath = Steganography.EnsureFolderExistence(m_cardFolder);
		files = new DirectoryInfo(deckPath).GetFiles();
		AddFilesIfValidCardPath( files, m_cardFolder, cardsInFolderDict );

		return cardsInFolderDict.ToArray();
	}

	public void RefreshCards(){

		cardManifest = FindAllExistingCardsInFolders();
		Debug.Log("[Card Browser] Manifested "+cardManifest.Length+" cards ");
		FilterCards("");

		cardScroller.FlipPage(0);	//refresh current page
	}

	public void SeAllCardsInteractible( bool interactible ){
		if( cardEntries == null ){
			return;
		}
		foreach( Card card in cardEntries ){
			card.button.interactable = interactible;
		}
	}


	private void FilterCards( string filter ){
		if( filter == null || filter == "" ){
			filteredCards = cardManifest;
			return;
		}
		List<string> result = new List<string>();

		for( int i=0; i<cardManifest.Length; i++ ){
			if( cardManifest[i].Contains( filter ) ){
				result.Add( cardManifest[i] );
			}
		}
		filteredCards = result.ToArray();

		Debug.Log("[CARD BROWSER] Filtered "+filteredCards.Length+" cards");
	}

	private void AddFilesIfValidCardPath( FileInfo[] files, string path, HashSet<string> cardsInFolderDict ){
		for( int i=0; i<files.Length; i++ ){
			string cardName = files[i].Name;
			if( !cardName.ToLower().EndsWith("png") ){
				continue;
			}
			if( !File.Exists(path+"/"+cardName) ){	//remove orphaned thumbnails
				File.Delete( path+"/.thumbs/"+cardName );
				Debug.Log("deleting Deck/"+cardName);
				continue;
			}
			var newEntry = cardName.Substring(0,cardName.Length-4);	//remove .png from name
			if( !cardsInFolderDict.Contains( newEntry ) ){
				cardsInFolderDict.Add( newEntry );
			}
		}
	}

    private int OnLoadCardMaxPages(){
		return Mathf.CeilToInt( filteredCards.Length/cardEntries.Length );
	}

	private void OnUpdateLoadCardPage( int page ){
		if( loadCardsCoroutine != null ){
			GameDirector.instance.StopCoroutine( loadCardsCoroutine );
		}
		loadCardsCoroutine = GameDirector.instance.StartCoroutine( LoadCardPage( page ) );
	}

	private IEnumerator LoadCardPage( int page ){

		cardScroller.SetEnablePageChange( false );
		
		//hide the rest of the cards
		int cardIndex = page*cardEntries.Length;
		int i=0;
		for( ; i<cardEntries.Length; i++ ){
			if( cardIndex >= filteredCards.Length ){
				break;
			}
			Card card = cardEntries[i];
			card.Initialize( this, filteredCards[ cardIndex++ ]+".png", executeLoadCard );
			card.button.gameObject.SetActive( true );
		}
		for( ; i<cardEntries.Length; i++ ){
			cardEntries[i].button.gameObject.SetActive( false );
		}

		yield return new WaitForSeconds(1.0f);
		cardScroller.SetEnablePageChange( true );
	}

	private IEnumerator GrowCardUIElementEffect( Transform target ){

		float timer = 0.0f;
		while( timer < 0.15f ){
			if( target == null ){	//if was destroyed outside function
				yield break;
			}
			timer += Time.deltaTime;
			float ratio = Mathf.Clamp01( timer/0.15f );
			target.localScale = Vector3.one*ratio;
			yield return null;
		}
	}
}

}