using UnityEngine;
using System.Collections;

using UnityEngine.UI;
using System.Collections.Generic;

namespace viva{


public partial class PauseMenu: UIMenu{

    private MenuTutorial menuTutorialPhase = MenuTutorial.NONE;
    private Coroutine tutorialCoroutine = null;
    private bool m_finishedDisplayHint = true;
    public bool finishedDisplayedHint { get{ return m_finishedDisplayHint; } }

    [SerializeField]
    private RectTransform HUD_canvas;

    [SerializeField]
    private GameObject tutorialCircle = null;

    [SerializeField]
    private GameObject tutorialDuckPrefab = null;

    [SerializeField]
    private GameObject tutorialFramePrefab = null;

    [SerializeField]
    private GameObject achievementPrefab;

    GameObject duck;

    Item frame;

    public enum HintType{
        ACHIEVEMENT,
        HINT,
        HINT_NO_IMAGE
    }

    public enum MenuTutorial{
        NONE,
        WAIT_TO_OPEN_PAUSE_MENU,
        WAIT_TO_ENTER_CHECKLIST,
        WAIT_TO_EXIT_CHECKLIST,

        WAIT_CROUCH,
        WAIT_TO_WAVE,
        WAIT_TO_COME_HERE,
        WAIT_TO_START_PICKUP,
        WAIT_TO_PRESENT,

        WAIT_TO_PICKUP_FRAME,
        WAIT_TO_RIP_FRAME,

        FINISHED_ALL
    }

    [SerializeField]
	private Sprite[] hintTypeFrames = new Sprite[ System.Enum.GetValues(typeof(HintType)).Length ];
    [SerializeField]
	private Sprite[] hintTypeImages = new Sprite[ System.Enum.GetValues(typeof(HintType)).Length ];

    [SerializeField]
    private AudioClip menuSound;

    [SerializeField]
    private AudioClip achievementSound;

    private int achievementPanelsActive = 0;

    private void ExitTutorial(){
        if( tutorialCoroutine == null ){
            return;
        }
        Debug.Log("[TUTORIAL] Exited");
        tutorialCircle.SetActive( false );
        menuTutorialPhase = MenuTutorial.FINISHED_ALL;
        GameDirector.instance.StopCoroutine( tutorialCoroutine );
        tutorialCoroutine = null;
        //destroy tutorial objects
        Destroy(frame);
        Destroy(duck);
        //resume game
        GameDirector.player.transform.position = lastPositionBeforeTutorial;
        //reset music
        GameDirector.instance.SetMusic( GameDirector.instance.GetDefaultMusic(), 3.0f );
        GameDirector.instance.SetUserIsExploring(false);
    }
    private void checkIfExitedTutorialCircle(){
        if( Vector3.SqrMagnitude( transform.position-tutorialCircle.transform.position ) > 90.0f ){
            ExitTutorial();
        }
    }

	public void ContinueTutorial( MenuTutorial continuePhase ){
        if(tutorialCoroutine != null){
            if( (MenuTutorial)( (int)menuTutorialPhase+1 ) == continuePhase ){
            menuTutorialPhase = continuePhase;
            Debug.Log("Continued tutorial! Now at "+menuTutorialPhase);
            }
        }
    }

    public MenuTutorial GetMenuTutorialPhase(){
        return menuTutorialPhase;
    }

    private IEnumerator Tutorial(){
        tutorialCircle.SetActive( true );
        Player player = GameDirector.player;
        yield return new WaitForSeconds(1.0f);

        DisplayHUDMessage( "        Welcome to the Tutorial        ", true, HintType.HINT_NO_IMAGE );
        DisplayHUDMessage( "Exit the tutorial by leaving the green circle.", true, HintType.HINT );
        
        while( !m_finishedDisplayHint ){
            checkIfExitedTutorialCircle();
            yield return null;
        }
        if( player.controls == Player.ControlType.KEYBOARD ){
            DisplayHUDMessage( "Open the options menu with ESC", true, HintType.HINT, MenuTutorial.WAIT_TO_OPEN_PAUSE_MENU );
            while( menuTutorialPhase <= MenuTutorial.NONE ){
                checkIfExitedTutorialCircle();
                yield return null;
            }
        }else{
            DisplayHUDMessage( "Press the MENU controller button", true, HintType.HINT, MenuTutorial.WAIT_TO_OPEN_PAUSE_MENU );
            while( menuTutorialPhase <= MenuTutorial.NONE ){
                checkIfExitedTutorialCircle();
                yield return null;
            }
        }
        DisplayHUDMessage( "You may switch to VR in Controls.", true, HintType.HINT_NO_IMAGE, MenuTutorial.WAIT_TO_ENTER_CHECKLIST );
        DisplayHUDMessage( "It is recommended to calibrate VR hands.", true, HintType.HINT_NO_IMAGE, MenuTutorial.WAIT_TO_ENTER_CHECKLIST );
        DisplayHUDMessage( "When you are ready to continue, click Checklist.", true, HintType.HINT, MenuTutorial.WAIT_TO_ENTER_CHECKLIST );
        while( menuTutorialPhase <= MenuTutorial.WAIT_TO_OPEN_PAUSE_MENU ){
            checkIfExitedTutorialCircle();
            yield return null;
        }
        DisplayHUDMessage( "Complete all tasks below as an objective.", true, HintType.HINT_NO_IMAGE, MenuTutorial.WAIT_TO_EXIT_CHECKLIST );
        DisplayHUDMessage( "When you are ready to continue, exit all menus.", true, HintType.HINT, MenuTutorial.WAIT_TO_EXIT_CHECKLIST );
        while( menuTutorialPhase <= MenuTutorial.WAIT_TO_ENTER_CHECKLIST ){
            checkIfExitedTutorialCircle();
            yield return null;
        }

        if( player.controls == Player.ControlType.KEYBOARD ){
            DisplayHUDMessage( "WASD to walk around. Left Shift to run.", true, HintType.HINT );
        }else{
            if( GameDirector.settings.vrControls == Player.VRControlType.TRACKPAD ){
                DisplayHUDMessage( "Use trackpad/thumbstick to walk around.", true, HintType.HINT_NO_IMAGE ); 
                DisplayHUDMessage( "Press and hold to run faster.", true, HintType.HINT );
            }else{
                DisplayHUDMessage( "Press trackpad/thumbstick to teleport.", true, HintType.HINT ); 
            }
        }
        while( !m_finishedDisplayHint ){
            checkIfExitedTutorialCircle();
            yield return null;
        }
        if( player.controls == Player.ControlType.KEYBOARD ){
            DisplayHUDMessage( "If you sit down on the floor with the loli,", true, HintType.HINT_NO_IMAGE, MenuTutorial.WAIT_CROUCH );
            DisplayHUDMessage( "you can play hand games with her.", true, HintType.HINT_NO_IMAGE, MenuTutorial.WAIT_CROUCH );
            DisplayHUDMessage( "Try toggling crouch with Ctrl.", true, HintType.HINT, MenuTutorial.WAIT_CROUCH );
            while( menuTutorialPhase <= MenuTutorial.WAIT_TO_EXIT_CHECKLIST ){
                checkIfExitedTutorialCircle();
                if( player.GetKeyboardCurrentHeight() < 1.0f ){
                    ContinueTutorial( MenuTutorial.WAIT_CROUCH );
                }
                yield return null;
            }
        }else{
            DisplayHUDMessage( "If you sit down on the floor with the loli,", true, HintType.HINT_NO_IMAGE );
            DisplayHUDMessage( "You can play hand games with her.", true, HintType.HINT );
            float time = Time.time;
            while( Time.time-time < 5.0f ){
                checkIfExitedTutorialCircle();
                yield return null;
            }
            menuTutorialPhase = MenuTutorial.WAIT_CROUCH;
        }
        
        DisplayHUDMessage( "You can make gestures to interact with the loli.", true, HintType.HINT_NO_IMAGE, MenuTutorial.WAIT_TO_WAVE );
        if( player.controls == Player.ControlType.KEYBOARD ){
            DisplayHUDMessage( "Say Hello with R", true, HintType.HINT, MenuTutorial.WAIT_TO_WAVE );
        }else{
            DisplayHUDMessage( "Wave side to side with your hand", true, HintType.HINT_NO_IMAGE, MenuTutorial.WAIT_TO_WAVE );
            DisplayHUDMessage( "until you see the wave icon appear", true, HintType.HINT, MenuTutorial.WAIT_TO_WAVE );
        }
        while( menuTutorialPhase <= MenuTutorial.WAIT_CROUCH ){
            checkIfExitedTutorialCircle();
            yield return null;
        }
        DisplayHUDMessage( "Good!", true, HintType.HINT );
        while( !m_finishedDisplayHint ){
            checkIfExitedTutorialCircle();
            yield return null;
        }
        DisplayHUDMessage( "Now gesture someone to come here.", true, HintType.HINT_NO_IMAGE, MenuTutorial.WAIT_TO_COME_HERE );
        if( player.controls == Player.ControlType.KEYBOARD ){
            DisplayHUDMessage( "Press F to Follow", true, HintType.HINT, MenuTutorial.WAIT_TO_COME_HERE );
        }else{
            DisplayHUDMessage( "Swing your hand back and forth, palm facing you.", true, HintType.HINT, MenuTutorial.WAIT_TO_COME_HERE );
        }
        while( menuTutorialPhase <= MenuTutorial.WAIT_TO_WAVE ){
            checkIfExitedTutorialCircle();
            yield return null;
        }
        DisplayHUDMessage( "Good! This will make your loli follow you.", true, HintType.HINT );
        while( !m_finishedDisplayHint ){
            checkIfExitedTutorialCircle();
            yield return null;
        }

        DisplayHUDMessage( "Pick up the rubber ducky in the center.", true, HintType.HINT_NO_IMAGE, MenuTutorial.WAIT_TO_START_PICKUP );
        duck = GameObject.Instantiate( tutorialDuckPrefab, tutorialCircle.transform.position+Vector3.up*0.1f, Quaternion.identity );
        if( player.controls == Player.ControlType.KEYBOARD ){
            DisplayHUDMessage( "Left click left hand. Right click right hand.", true, HintType.HINT_NO_IMAGE, MenuTutorial.WAIT_TO_START_PICKUP );
            DisplayHUDMessage( "Drop by holding SHIFT", true, HintType.HINT, MenuTutorial.WAIT_TO_START_PICKUP );
        }else{
            DisplayHUDMessage( "Drop/Pickup :Grip Button (VIVE), Trigger (Oculus)", true, HintType.HINT, MenuTutorial.WAIT_TO_START_PICKUP );
        }
        while( menuTutorialPhase <= MenuTutorial.WAIT_TO_COME_HERE ){
            checkIfExitedTutorialCircle();
            if( player.rightHandState.heldItem != null ){
                if( player.rightHandState.heldItem.gameObject == duck ){
                    ContinueTutorial( MenuTutorial.WAIT_TO_START_PICKUP );
                }
            }else if( player.leftHandState.heldItem != null ){
                if( player.leftHandState.heldItem.gameObject == duck ){
                    ContinueTutorial( MenuTutorial.WAIT_TO_START_PICKUP );
                }
            }
            yield return null;
        }
        DisplayHUDMessage( "Now extend the ducky out like an offering", true, HintType.HINT_NO_IMAGE, MenuTutorial.WAIT_TO_PRESENT );
        if( player.controls == Player.ControlType.KEYBOARD ){
            DisplayHUDMessage( "Drop objects by holding SHIFT and clicking.", true, HintType.HINT_NO_IMAGE, MenuTutorial.WAIT_TO_PRESENT );
            DisplayHUDMessage( "Extend left hand with Q and right with E", true, HintType.HINT, MenuTutorial.WAIT_TO_PRESENT );
        }
        while( menuTutorialPhase <= MenuTutorial.WAIT_TO_START_PICKUP ){
            checkIfExitedTutorialCircle();
            yield return null;
        }
        DisplayHUDMessage( "You can give or request items from the loli like that.", true, HintType.HINT );
        while( !m_finishedDisplayHint ){
            checkIfExitedTutorialCircle();
            yield return null;
        }

        //polaroid frame tutorial section
        frame = GameObject.Instantiate( tutorialFramePrefab, tutorialCircle.transform.position+Vector3.up*0.1f, Quaternion.identity ).GetComponent( typeof(PolaroidFrame)) as PolaroidFrame;
        DisplayHUDMessage( "Pick up the Polaroid frame in the center", true, HintType.HINT_NO_IMAGE, MenuTutorial.WAIT_TO_PICKUP_FRAME );
        DisplayHUDMessage( "and drop the rest of the objects", true, HintType.HINT_NO_IMAGE, MenuTutorial.WAIT_TO_PICKUP_FRAME );
        if( player.controls == Player.ControlType.KEYBOARD ){
            DisplayHUDMessage( "Drop by holding SHIFT", true, HintType.HINT, MenuTutorial.WAIT_TO_PICKUP_FRAME );
        }else{
            DisplayHUDMessage( "Drop/Pickup :Grip Button (VIVE), Trigger (Oculus)", true, HintType.HINT, MenuTutorial.WAIT_TO_START_PICKUP );
        }
        
        while( menuTutorialPhase <= MenuTutorial.WAIT_TO_PRESENT ){
            checkIfExitedTutorialCircle();
            int held = System.Convert.ToInt32( player.rightHandState.heldItem != null );
            held += System.Convert.ToInt32( player.leftHandState.heldItem != null );
            if( held == 1 ){
                if( player.rightHandState.heldItem == frame ||
                    player.leftHandState.heldItem == frame ){
                    ContinueTutorial( MenuTutorial.WAIT_TO_PICKUP_FRAME );
                }
            }
            yield return null;
        }

        if( player.controls == Player.ControlType.KEYBOARD ){
            DisplayHUDMessage( "To Rip the frame, click with the hand holding it", true, HintType.HINT_NO_IMAGE, MenuTutorial.WAIT_TO_RIP_FRAME );
            DisplayHUDMessage( "then once it's held with both hands,", true, HintType.HINT_NO_IMAGE, MenuTutorial.WAIT_TO_RIP_FRAME );
            DisplayHUDMessage( "click with both mouse clicks simultaneously", true, HintType.HINT, MenuTutorial.WAIT_TO_RIP_FRAME );
        }else{
            DisplayHUDMessage( "To Rip the frame, move both hands close together", true, HintType.HINT_NO_IMAGE, MenuTutorial.WAIT_TO_RIP_FRAME );
            DisplayHUDMessage( "then press both triggers and pull away", true, HintType.HINT, MenuTutorial.WAIT_TO_RIP_FRAME );
        }
        while( menuTutorialPhase <= MenuTutorial.WAIT_TO_PICKUP_FRAME ){
            checkIfExitedTutorialCircle();
            if( frame == null ){
                ContinueTutorial( MenuTutorial.WAIT_TO_RIP_FRAME );
            }
            yield return null;
        }

        DisplayHUDMessage( "Game under development expect bugs!", true, HintType.HINT_NO_IMAGE );    
        DisplayHUDMessage( "Tutorial Finished!", true, HintType.HINT );
        DisplayHUDMessage( "You will now be sent back to your previous location.", true, HintType.HINT_NO_IMAGE );
        
        ExitTutorial();
    }

    //TODO: Move everything to do with Hints and Achievement Messages to its own class (GameDirector.Hud)
    public void DisplayHUDMessage( string message, bool playsound, HintType hintType, MenuTutorial waitForPhase = MenuTutorial.NONE){
        GameDirector.instance.StartCoroutine( HandleHUDMessage( message, playsound, hintType, waitForPhase ) );
    }

    private void OrientHUDToPlayer(){

        if( GameDirector.player.controls == Player.ControlType.KEYBOARD ){
            HUD_canvas.transform.localScale = Vector3.one*0.001f;
            HUD_canvas.transform.position = GameDirector.instance.mainCamera.transform.position+GameDirector.instance.mainCamera.transform.forward*0.8f;
            HUD_canvas.transform.rotation = Quaternion.LookRotation( GameDirector.instance.mainCamera.transform.forward, GameDirector.instance.mainCamera.transform.up );
        }else{
            HUD_canvas.transform.localScale = Vector3.one*0.002f;
            Vector3 floorForward = GameDirector.instance.mainCamera.transform.forward;
            floorForward.y = 0.001f;
            floorForward = floorForward.normalized;

            HUD_canvas.transform.position = GameDirector.player.head.position+floorForward*1.5f-Vector3.up*0.5f;
            HUD_canvas.transform.rotation = Quaternion.LookRotation( floorForward, Vector3.up );
        }
    }
    
    private void EnsureVisibleHUD(){
        
        //stick HUD close to player if he moves far
        Player player = GameDirector.player;
        if( player.controls == Player.ControlType.KEYBOARD ){
            OrientHUDToPlayer();
        }else{  //if VR
            if( (HUD_canvas.position-player.head.position).sqrMagnitude > 6.0f ){
                OrientHUDToPlayer();
            }else if( Mathf.Abs( Tools.Bearing( player.head, HUD_canvas.position ) ) > 40.0f ){
                OrientHUDToPlayer();
            }
        }
    }

    public IEnumerator HandleHUDMessage( string message, bool playsound, HintType hintType, MenuTutorial waitForPhase ){
        m_finishedDisplayHint = false;

        GameObject achievementPanel = Instantiate( achievementPrefab, Vector3.zero, HUD_canvas.rotation, HUD_canvas );

        Vector3 targetPos = new Vector3( 0.0f, achievementPanelsActive++*-40.0f+360.0f, 0.0f );
        if( playsound ){
            if( hintType == HintType.ACHIEVEMENT )
            GameDirector.instance.PlayGlobalSound( achievementSound );
            else if( hintType == HintType.HINT )
            GameDirector.instance.PlayGlobalSound( menuSound ); 
        }

        Image image = achievementPanel.GetComponent(typeof(Image)) as Image;
        image.sprite = hintTypeFrames[ (int)hintType ];
        Text text = achievementPanel.transform.GetChild(0).GetComponent(typeof(Text)) as Text;
        text.text = message;
        
        Image hintImage = achievementPanel.transform.GetChild(1).GetComponent(typeof(Image)) as Image;
        hintImage.sprite = hintTypeImages[ (int)hintType ];

        Color fade = Color.white;
        Color textFade = Color.black;

        float timer = 0.0f;
        while( timer < 0.4f ){
            timer += Time.deltaTime;
            float alpha = Mathf.Min(1.0f,timer/0.4f);
            achievementPanel.transform.localPosition = Vector3.LerpUnclamped( Vector3.zero, targetPos, Tools.EaseOutQuad(alpha) );
            achievementPanel.transform.localScale = Vector3.one*Mathf.Lerp( 1.0f, 2.0f, 1.0f-Mathf.Abs( 0.5f-alpha )*2.0f );
            
            fade.a = alpha;
            image.color = fade;
            hintImage.color = fade;
            textFade.a = alpha;
            text.color = textFade;
            EnsureVisibleHUD();
            yield return null;
        }
        
        if( waitForPhase != MenuTutorial.NONE ){
            while( true ){
                
                if( menuTutorialPhase >= waitForPhase ){
                    break;
                }else{
                    EnsureVisibleHUD();
                    yield return null;
                }
            }
        }else{
            float length = 2.0f+message.Length*0.06f;
            while( timer < length ){
                timer += Time.deltaTime;
                EnsureVisibleHUD();
                yield return null;
            }
        }
        timer = 0.0f;
        achievementPanelsActive--;
        m_finishedDisplayHint = true;
        while( timer < 0.5f ){
            timer += Time.deltaTime;
            float alpha = 1.0f-timer/0.5f;
            fade.a = alpha;
            image.color = fade;
            hintImage.color = fade;
            textFade.a = alpha;
            text.color = textFade;
            yield return null;
        }
        Destroy( achievementPanel );
    }
}

}