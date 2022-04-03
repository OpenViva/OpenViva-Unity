using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;



namespace viva{

public class PauseBook : MonoBehaviour{

    [SerializeField]
    private Animator animator;
    [SerializeField]
    private GameObject book;
    [SerializeField]
    private AudioClip openSound;
    [SerializeField]
    private AudioClip closeSound;
    [SerializeField]
    private AudioClip turnPageSound;
    [SerializeField]
    private Transform rightPage;
    [SerializeField]
    private Transform leftPage;
    [SerializeField]
    private Canvas rightCanvas;
    [SerializeField]
    private Canvas leftCanvas;
    [SerializeField]
    private MessageDialog messageDialog;
    [SerializeField]
    private SessionMenu sessionMenu;

    private Coroutine checkCoroutine;
    public bool isOpen { get; private set; } = false;
    private GameObject lastRightPageUI;


    private void Awake(){
        book.SetActive( false );
        animator.enabled = false;

        DisplayUI( "Menu" );
    }

    public void SetSessionMenuLoadMode( bool loadMode ){
        sessionMenu.loadMode = loadMode;
    }

    private void OnDestroy(){
        InputManager.ToggleForCursorLockCounter( this, false );
    }

    public void DisplayUI( string containerName ){
        lastRightPageUI?.SetActive( false );

        lastRightPageUI = rightCanvas.transform.Find( containerName )?.gameObject;
        lastRightPageUI?.SetActive( true );

        if( isOpen ) Sound.main.PlayGlobalOneShot( turnPageSound );
    }

    public void SetOpen( bool open ){
        if( open == isOpen ) return;
        InputManager.ToggleForCursorLockCounter( this, !isOpen );
        rightCanvas.worldCamera = Camera.main;
        leftCanvas.worldCamera = Camera.main;

        if( checkCoroutine != null ) StopCoroutine( checkCoroutine );
        animator.enabled = true;
        animator.Play( open?"open":"close", 0 );
        isOpen = open;
        checkCoroutine = StartCoroutine( CheckFinished() );
    }

    public void OpenDiscord(){
        Application.OpenURL("https://discord.com/invite/w7rFnssghW");
    }

    private IEnumerator CheckFinished(){
        if( isOpen ){
            book.SetActive( true );
            var flat = Tools.FlatForward( -Camera.main.transform.forward );
            transform.rotation = Quaternion.LookRotation( flat, Vector3.up );
            transform.position = Camera.main.transform.position-flat*0.4f+Vector3.down*0.8f;

            Sound.main.PlayGlobalOneShot( openSound );
            rightCanvas.gameObject.SetActive( true );
            leftCanvas.gameObject.SetActive( true );
        }else{
            Sound.main.PlayGlobalOneShot( closeSound );
        }
        while( true ){
            yield return null;
            float norm = animator.IsInTransition(0) ? animator.GetNextAnimatorStateInfo(0).normalizedTime : animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            if( norm >= 1f ) break;
        }
        if( !isOpen ){
            book.SetActive( false );
            rightCanvas.gameObject.SetActive( false );
            leftCanvas.gameObject.SetActive( false );
        }
    }

    public void CloseBook(){
        SetOpen( false );
    }
    
    public void LoadTutorial(){
        sessionMenu.LoadSession( "tutorial", Profile.root );
    }
    
    public void SaveSession(){
        ThumbnailGenerator.main.GenerateCameraTexture( ExportAndCreateSessionTexture );
    }

    private void ExportAndCreateSessionTexture( Texture2D previewTex ){
        var defaultSessionName = System.DateTime.Now.ToString("MM-dd-yyyy hh-mm-ss");

        if( VivaPlayer.user.isUsingKeyboard ){
            messageDialog.RequestInput( "Save & Quit", "Enter session name", delegate( string name ){
                    var session = ExportSession( previewTex, name );
                    session.OnInstall();

                    sessionMenu.LoadSession( "main", Profile.root );
                },
                delegate{
                    Texture2D.Destroy( previewTex );
                },
                defaultSessionName
            );
        }else{
            var session = ExportSession( previewTex, defaultSessionName );
            session.OnInstall();

            Application.Quit();
        }
    }

    private SceneSettings ExportSession( Texture2D previewTex, string name ){
        var sceneSettings = new SceneSettings( previewTex, name, null );
        sceneSettings.idCounterStart = VivaInstance.idCounter;
        var sceneData = new SceneSettings.SceneData();
        sceneSettings.sceneData = sceneData;
        sceneData.timeOfDay = AmbienceManager.main.onDayTimePassed.timeOfDay;
        sceneData.scene = SceneManager.GetActiveScene().name;
        sceneData.min = Scene.main.min;
        sceneData.max = Scene.main.max;

        foreach( var completedAchievement in Achievement.active ){
            if( completedAchievement.complete ) sceneData.completedAchievements.Add( completedAchievement.name );
        }

        //save hints
        sceneSettings.hintsDisplayed = (int)HintMessage.hintsDisplayed;

        sceneSettings.characterDatas = SerializeCharacters( Resources.FindObjectsOfTypeAll( typeof(Character) ) as Character[], sceneSettings );
        sceneSettings.itemDatas = SerializeItems( Resources.FindObjectsOfTypeAll( typeof(Item) ) as Item[], sceneSettings );

        return sceneSettings;
    }

    private SceneSettings.CharacterData[] SerializeCharacters( Character[] characters, SceneSettings session ){
        var characterDatas = new SceneSettings.CharacterData[ characters.Length ];
        for( int i=0; i<characters.Length; i++ ){
            var charData = new SceneSettings.CharacterData();
            var character = characters[i];

            if( character == VivaPlayer.user.character ){
                session.playerDataIndex = i;
            }
            charData.id = character.id;
            charData.name = System.IO.Path.GetFileNameWithoutExtension( character.assetFilepath );
            charData.transform = new SceneSettings.TransformData();
            charData.transform.position = character.model.armature.transform.position;
            charData.transform.rotation = character.model.armature.transform.rotation;
            charData.serializedScripts = character.scriptManager.SerializeScriptInstances();

            characterDatas[i] = charData;
        }
        return characterDatas;
    }

    private SceneSettings.ItemData[] SerializeItems( Item[] items, SceneSettings session ){
        var itemDatas = new SceneSettings.ItemData[ items.Length ];
        for( int i=0; i<items.Length; i++ ){
            var itemData = new SceneSettings.ItemData();
            var item = items[i];
            if( !item ) continue;

            if( item == VivaPlayer.user.character ){
                session.playerDataIndex = i;
            }
            itemData.id = item.id;
            itemData.name = System.IO.Path.GetFileNameWithoutExtension( item.assetFilepath );
            itemData.transform = new SceneSettings.TransformData();
            itemData.transform.position = item.model.rootTransform.transform.position;
            itemData.transform.rotation = item.model.rootTransform.transform.rotation;
            itemData.serializedScripts = item.scriptManager.SerializeScriptInstances();
            itemData.attributes = item.AttributesToArray();
            itemData.immovable = item.immovable;
            itemDatas[i] = itemData;
        }
        return itemDatas;
    }
}

}