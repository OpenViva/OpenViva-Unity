using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace viva
{


    public partial class PauseMenu : UIMenu
    {


        public enum Menu
        {
            NONE,
            ROOT,
            KB_CONTROLS,
            VR_CONTROLS,
            MAP,
            OPTIONS,
            GRAPHICS,
            ERROR,
            CHECKLIST,
            CALIBRATE_HANDS,
            MANUAL,
            QUIT
        }

        private List<Button> cycleButtons = new List<Button>();
        private int cycleIndex = 0;
        private Menu lastMenu = Menu.NONE;

        [SerializeField]
        private RectTransform rightPageRootPanel;
        [SerializeField]
        private RectTransform leftPageRootPanel;
        [SerializeField]
        private PageScroller checklistPageScroller;
        [SerializeField]
        private GameObject checklistPrefab;
        [SerializeField]
        public GameObject fadePanel;
        [SerializeField]
        public Text mouseSensitivityText;
        [SerializeField]
        private Text musicVolumeText;
        [SerializeField]
        private Text dayNightCycleSpeedText;

        [SerializeField]
        private GameObject beginnerFriendlyKeyboard;
        [SerializeField]
        private GameObject beginnerFriendlyVR;
        [SerializeField]
        private Text respawnLoliText;
        [SerializeField]


        private Coroutine errorCoroutine = null;
        private Coroutine calibrationCoroutine = null;
        private Vector3 lastPositionBeforeTutorial = Vector3.zero;
        public bool IsPauseMenuOpen = false;

        [SerializeField]
        private GameObject calibrateHandGhostPrefab = null;


        public void ShowFirstLoadInstructions()
        {
            if (GameDirector.player.controls == Player.ControlType.KEYBOARD)
            {
                beginnerFriendlyKeyboard.SetActive(true);
            }
            else
            {
                beginnerFriendlyVR.SetActive(true);
            }
        }

        private void Start()
        {
            FindAutomaticResolution();

            if (tutorialCircle == null)
            {
                Debug.LogError("ERROR Tutorial Circle is null!");
            }          

            checklistPageScroller.Initialize(OnChecklistManifest, OnChecklistPageUpdate);

            //parent to page
            rightPageRootPanel.SetParent(UI_pageL);
            rightPageRootPanel.localEulerAngles = new Vector3(180.0f, 90.0f, 90.0f);
            rightPageRootPanel.localPosition = Vector3.zero;

            //parent to page
            leftPageRootPanel.SetParent(UI_pageR);
            leftPageRootPanel.localEulerAngles = new Vector3(0.0f, 90.0f, 90.0f);
            leftPageRootPanel.localPosition = Vector3.zero;
        }

        public void clickStartTutorial()
        {
            GameDirector.instance.StopUIInput();

            if (tutorialCoroutine != null)
            {
                return;
            }
            lastPositionBeforeTutorial = GameDirector.player.transform.position;
            GameDirector.player.transform.position = tutorialCircle.transform.position;
            menuTutorialPhase = MenuTutorial.NONE;
            tutorialCoroutine = GameDirector.instance.StartCoroutine(Tutorial());
        }

        public override void OnBeginUIInput()
        {
            Debug.Log("[PAUSEMENU] Open");
            gameObject.SetActive(true);
            PlayBookAnimation("open", OnOpenBookFinished);
            GameDirector.instance.PlayGlobalSound(openSound);
            OrientPauseBookToPlayer();
        }

        public override void OnExitUIInput()
        {
            Debug.Log("[PAUSEMENU] Closed");
            PlayBookAnimation("close", OnCloseBookFinished);
            GameDirector.instance.PlayGlobalSound(closeSound);

            SetMenuActive(Menu.NONE, false);
            lastMenu = Menu.NONE;
            StopCalibration();
        }

        private void UpdateMusicVolumeText()
        {
            musicVolumeText.text = "" + (int)(GameSettings.main.musicVolume * 100) + "%";
        }

        public void clickShiftMusicVolume(float amount)
        {
            GameSettings.main.AdjustMusicVolume(amount);
            UpdateMusicVolumeText();
        }

        public void clickShiftDayNightCycleSpeedIndex(int indexAmount)
        {
            dayNightCycleSpeedText.text = GameSettings.main.AdjustDayTimeSpeedIndex(indexAmount);
        }

        
        private void UpdateMouseSensitivityText()
        {
            mouseSensitivityText.text = "" + (int)(GameSettings.main.mouseSensitivity) + "%";
        }

        public void IncreaseMouseSensitivity()
        {
            GameSettings.main.AdjustMouseSensitivity(10.0f);
            UpdateMouseSensitivityText();
        }

        public void DecreaseMouseSensitivity()
        {
            GameSettings.main.AdjustMouseSensitivity(-10.0f);
            UpdateMouseSensitivityText();
        }

        public void DisplayError(string message)
        {

            if (errorCoroutine != null)
            {
                GameDirector.instance.StopCoroutine(errorCoroutine);
            }
            GameDirector.instance.StartCoroutine(ErrorMessage(message));
        }

        private IEnumerator ErrorMessage(string message)
        {
            float timer = 3.0f;
            GameObject errorPanel = GetRightPageUIByMenu(Menu.ERROR);
            errorPanel.SetActive(true);
            Text text = errorPanel.transform.GetChild(1).GetComponent(typeof(Text)) as Text;
            text.text = message;
            while (timer > 0.0f)
            {
                timer -= Time.deltaTime;
                yield return null;
            }
            errorPanel.SetActive(false);
        }



        private GameObject GetRightPageUIByMenu(Menu menu)
        {
            if (menu == Menu.NONE)
            {
                return null;
            }
            return rightPageRootPanel.transform.GetChild((int)menu - 1).gameObject;
        }

        private GameObject GetLeftPageUIByMenu(Menu menu)
        {
            if (menu == Menu.NONE)
            {
                return null;
            }
            return leftPageRootPanel.transform.GetChild((int)menu - 1).gameObject;
        }

        private void SetMenuActive(Menu menu, bool active)
        {

            GameObject lastMenuPanel = GetRightPageUIByMenu(lastMenu);
            if (lastMenuPanel)
            {
                lastMenuPanel.SetActive(false);
                GetLeftPageUIByMenu(lastMenu).SetActive(false);
            }
            GameObject menuPanel = GetRightPageUIByMenu(menu);
            if (menuPanel)
            {
                menuPanel.SetActive(active);
                GetLeftPageUIByMenu(menu).SetActive(active);
                if (active)
                {
                    SetCycleButtonsFrom(menuPanel);
                    lastMenu = menu;
                }
            }
        }

        public void SetPauseMenu(Menu menu)
        {
            switch (menu)
            {
                case Menu.NONE:
                    SetMenuActive(menu, false);
                    break;
                case Menu.CHECKLIST:
                    GameDirector.instance.PlayGlobalSound(nextSound);
                    SetMenuActive(menu, true);
                    ContinueTutorial(MenuTutorial.WAIT_TO_ENTER_CHECKLIST);
                    break;
                case Menu.MAP:
                    GameDirector.instance.PlayGlobalSound(nextSound);
                    SetMenuActive(menu, true);
                    break;
                case Menu.VR_CONTROLS:
                    GameDirector.instance.PlayGlobalSound(nextSound);
                    SetMenuActive(menu, true);
                    UpdateVRMovementPrefText();
                    UpdateDisableGrabToggleText();
                    UpdatePressToTurnText();
                    break;
                case Menu.KB_CONTROLS:
                case Menu.OPTIONS:
                    GameDirector.instance.PlayGlobalSound(nextSound);
                    SetMenuActive(menu, true);
                    UpdateMusicVolumeText();
                    UpdateMouseSensitivityText();
                    clickShiftDayNightCycleSpeedIndex(0);                   
                    break;
                case Menu.GRAPHICS:
                    GameDirector.instance.PlayGlobalSound(nextSound);
                    SetMenuActive(menu, true);
                    UpdateToggleQualityText();
                    UpdateAntiAliasingText();
                    UpdateShadowLevelText();
                    UpdateReflectionDistanceText();
                    UpdateResolutionText();
                    UpdateVsyncText();
                    UpdateFpsLimitText();
                    UpdateLodDistanceText();
                    break;                               
                case Menu.CALIBRATE_HANDS:
                case Menu.MANUAL:
                    GameDirector.instance.PlayGlobalSound(nextSound);
                    SetMenuActive(menu, true);
                    SetShowManualRoot(true);
                    UpdateRespawnShinobuText();
                    break;
                case Menu.QUIT:
                    GameDirector.instance.PlayGlobalSound(nextSound);
                    SetMenuActive(menu, true);
                    break;
                
                case Menu.ROOT:
                    if (lastMenu == Menu.ROOT)
                    {    //treat as a toggle
                        GameDirector.instance.StopUIInput();
                        return;
                    }
                    GameDirector.instance.PlayGlobalSound(prevSound);
                    SetMenuActive(menu, true);
                    ContinueTutorial(MenuTutorial.WAIT_TO_OPEN_PAUSE_MENU);
                    break;
            }
        }

        public void OrientPauseBookToPlayer()
        {
            Player player = GameDirector.player;
            if (player.controls == Player.ControlType.KEYBOARD)
            {
                transform.localScale = Vector3.one * 0.045f;   //make it tiny so it doesn't get occluded by other objects
                transform.position = player.head.transform.TransformPoint(keyboardOrientPosition);
                transform.rotation = player.head.rotation * Quaternion.Euler(keyboardOrientRotation);
            }
            else
            {
                Vector3 forward = player.head.forward;
                forward.y = 0.0f;
                forward = forward.normalized;
                transform.localScale = Vector3.one;
                transform.position = player.floorPos + forward * 0.8f - Vector3.up * 0.2f;
                transform.rotation = Quaternion.LookRotation(-forward, Vector3.up);
            }
        }

        private void SetCycleButtonsFrom(GameObject parent)
        {
            cycleButtons.Clear();
            for (int i = 0; i < parent.transform.childCount; i++)
            {
                Button button = parent.transform.GetChild(i).GetComponent(typeof(Button)) as Button;
                if (button != null)
                {
                    cycleButtons.Add(button);
                }
            }
            cycleIndex = 0;
        }

        public void cycleButton(int dir)
        {
            if (cycleButtons.Count == 0)
            {
                return;
            }
            cycleIndex = Mathf.Max(0, cycleIndex + dir);
            if (cycleIndex >= cycleButtons.Count)
            {
                cycleIndex = 0;
            }
            cycleButtons[cycleIndex].Select();
        }

        public void clickCurrentCycledButton()
        {
            if (cycleButtons.Count == 0)
            {
                return;
            }
            cycleButtons[cycleIndex].onClick.Invoke();
        }

        

        private void UpdateRespawnShinobuText()
        {
            Button button = respawnLoliText.transform.parent.GetComponent<Button>();
            if (GameDirector.characters.Count > 1)
            {
                respawnLoliText.text = "Respawn Loli";
                button.enabled = true;
            }
            else
            {
                respawnLoliText.text = "You need to spawn Loli in the Mirror first";
                button.enabled = false;
            }
        }


        //TODO Make this work with selected Lolis
        public void clickRespawnShinobu()
        {
            // Loli loli = GameDirector.instance.loli;
            // if( loli != null ){
            //     loli.active.SetTask( loli.active.idle, null );
            //     loli.OverrideBodyState( BodyState.STAND );
            //     loli.OverrideClearAnimationPriority();
            //     loli.SetTargetAnimation( loli.GetLastReturnableIdleAnimation() );

            //     Player player = GameDirector.player;
            //     loli.transform.position = player.head.position+player.head.forward*0.8f;
            //     loli.rigidBody.velocity = Vector3.zero;
            // }
        }

        public void clickDiscord()
        {
            Application.OpenURL("https://discord.gg/openviva");
        }

        public void clickSaveAndQuitGame(bool save)
        {
            if (save)
            {
                GameDirector.instance.Save();
            }           
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
         Application.Quit();
         System.Diagnostics.Process.GetCurrentProcess().Kill();
#endif
        }

        public void clickDeleteSave()
        {
            string path = "Saves/save.viva";
            if (File.Exists(path))
            {
                File.Delete(path);
            }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
         Application.Quit();
         System.Diagnostics.Process.GetCurrentProcess().Kill();
#endif
        }

        public void clickControls()
        {
            if (GameDirector.player.controls == Player.ControlType.KEYBOARD)
            {
                SetPauseMenu(Menu.KB_CONTROLS);
            }
            else
            {
                SetPauseMenu(Menu.VR_CONTROLS);
            }
        }
        public void clickRoot()
        {
            SetPauseMenu(Menu.ROOT);
        }
        public void clickOptions()
        {
            SetPauseMenu(Menu.OPTIONS);
        }
        public void clickGraphics()
        {
            SetPauseMenu(Menu.GRAPHICS);
        }
        public void toggleMuteMusic()
        {
            GameDirector.instance.SetMuteMusic(GameDirector.instance.IsMusicMuted());
        }
        public void clickSwitchVRControlScheme()
        {
            Player player = GameDirector.player;
            Text buttonText = GetRightPageUIByMenu(Menu.VR_CONTROLS).transform.Find("Trackpad").GetChild(0).GetComponent(typeof(Text)) as Text;
            if (GameSettings.main.vrControls == Player.VRControlType.TRACKPAD)
            {
                GameSettings.main.SetVRControls(Player.VRControlType.TELEPORT);
                buttonText.text = "Using Teleport";
                OrientPauseBookToPlayer();
            }
            else
            {
                GameSettings.main.SetVRControls(Player.VRControlType.TRACKPAD);
                buttonText.text = "Using Trackpad";
            }
            UpdateVRMovementPrefText();
        }
        public void clickToggleTrackpadPreferences()
        {

            GameSettings.main.ToggleTrackpadMovementUseRight();
            UpdateVRMovementPrefText();
        }
        private void UpdateVRMovementPrefText()
        {
            Text buttonText = GetRightPageUIByMenu(Menu.VR_CONTROLS).transform.Find("TrackpadPref").GetChild(0).GetComponent(typeof(Text)) as Text;
            buttonText.text = GameSettings.main.trackpadMovementUseRight ? "Using Right Handed" : "Using Left Handed";
        }
        public void clickSwitchControlScheme()
        {
            Player player = GameDirector.player;
            if (player.controls == Player.ControlType.KEYBOARD)
            {
                player.SetControls(Player.ControlType.VR);
            }
            else
            {
                player.SetControls(Player.ControlType.KEYBOARD);
            }
        }
        

        public int OnChecklistManifest()
        {
            return System.Enum.GetValues(typeof(Player.ObjectiveType)).Length / 10;
        }

        void OnChecklistPageUpdate(int page)
        {

            //clear old page items
            for (int i = 3; i < checklistPageScroller.GetPageContent().childCount; i++)
            {
                Destroy(checklistPageScroller.GetPageContent().GetChild(i).gameObject);
            }
            int checklistIndex = page * 10;
            for (int i = 0; i < 10; i++)
            {
                if (checklistIndex >= System.Enum.GetValues(typeof(Player.ObjectiveType)).Length)
                {
                    break;
                }
                GameObject newEntry = Instantiate(checklistPrefab, new Vector3(0.0f, -i * 32.0f - 15.0f, 0.0f), Quaternion.identity);
                RectTransform entryRect = newEntry.transform as RectTransform;
                newEntry.transform.SetParent(checklistPageScroller.GetPageContent(), false);

                Text textComponent = entryRect.GetChild(0).GetComponent(typeof(Text)) as Text;
                Player.ObjectiveType type = (Player.ObjectiveType)checklistIndex;
                textComponent.text = GameDirector.player.GetAchievementDescription(type);

                Button button = newEntry.GetComponent(typeof(Button)) as Button;
                button.interactable = GameDirector.player.IsAchievementComplete(type);

                checklistIndex++;
            }
        }

        public void clickChecklist()
        {
            SetPauseMenu(Menu.CHECKLIST);
            //setup checklist panel
            checklistPageScroller.FlipPage(0);  //refresh current page
        }

        public void BETA_increaseDaylight()
        {
            GameSettings.main.ShiftWorldTime(0.3f);
        }

        public void clickToggleDisableGrab()
        {
            GameSettings.main.ToggleDisableGrabToggle();
            UpdateDisableGrabToggleText();
        }

        private void UpdateDisableGrabToggleText()
        {
            Text buttonText = GetRightPageUIByMenu(Menu.VR_CONTROLS).transform.Find("Disable Grab Toggle").GetChild(0).GetComponent(typeof(Text)) as Text;
            buttonText.text = GameSettings.main.disableGrabToggle ? "Grab Toggle is off" : "Grab Toggle is on";
        }

        private void UpdatePressToTurnText()
        {
            Text buttonText = GetRightPageUIByMenu(Menu.VR_CONTROLS).transform.Find("Turning requires press").GetChild(0).GetComponent(typeof(Text)) as Text;
            buttonText.text = GameSettings.main.pressToTurn ? "Using Press to Turn" : "Using Touch to Turn";
        }

        public void clickTogglePressToTurn()
        {
            GameSettings.main.TogglePresstoTurn();
            UpdatePressToTurnText();
        }

        public void clickCalibrateHands()
        {            
            if (calibrationCoroutine != null)
            {
                SetPauseMenu(Menu.ROOT);
                StopCalibration();
            }
            else
            {
                SetPauseMenu(Menu.CALIBRATE_HANDS);
                StartCalibration();
            }
            
        }

        public void clickMap()
        {
            SetPauseMenu(Menu.MAP);
        }

        public void clickQuit()
        {
            SetPauseMenu(Menu.QUIT);
        }

        //TODO: add a smooth transition for this
        public void clickWaypoint(Transform pos){
            GameDirector.player.transform.position = pos.position;
        }

        private void StartCalibration()
        {
            if (calibrationCoroutine != null)
            {
                return;
            }
            calibrationCoroutine = GameDirector.instance.StartCoroutine(CalibrateHands());
        }


        private void StopCalibration()
        {
            if (calibrationCoroutine == null)
            {
                return;
            }
            GameDirector.instance.StopCoroutine(calibrationCoroutine);
            calibrationCoroutine = null;
        }

        private void SetPlayerAbsoluteVROffsets(Vector3 position, Vector3 euler, bool relativeToRightHand)
        {
            Player player = GameDirector.player;
            if (player == null)
            {
                return;
            }
            player.rightPlayerHandState.SetAbsoluteVROffsets(position, euler, relativeToRightHand);
            player.leftPlayerHandState.SetAbsoluteVROffsets(position, euler, relativeToRightHand);
        }

        private IEnumerator CalibrateHands()
        {

            PlayerHandState targetHoldState = null;
            GameObject ghost = null;
            while (true)
            {
                if (ghost == null)
                {

                    if (GameDirector.player.rightPlayerHandState.gripState.isDown)
                    {
                        targetHoldState = GameDirector.player.rightPlayerHandState;
                    }
                    if (GameDirector.player.leftPlayerHandState.gripState.isDown)
                    {
                        targetHoldState = GameDirector.player.leftPlayerHandState;
                    }
                    if (targetHoldState != null)
                    {
                        GameDirector.player.ApplyVRHandsToAnimation();
                        GameDirector.player.SetFreezeVRHandTransforms(true);
                        SetPlayerAbsoluteVROffsets(Vector3.zero, Vector3.zero, false);
                        Transform absolute = targetHoldState.absoluteHandTransform;
                        ghost = GameObject.Instantiate(calibrateHandGhostPrefab, Vector3.zero, Quaternion.identity);
                    }
                }
                else
                {  //calibrate

                    GameDirector.player.ApplyVRHandsToAnimation();
                    Transform absolute = targetHoldState.absoluteHandTransform;
                    Transform wrist = targetHoldState.fingerAnimator.wrist;
                    ghost.transform.position = absolute.position;
                    ghost.transform.rotation = absolute.rotation;
                    if (targetHoldState.gripState.isUp)
                    {
                        Transform oldParent = wrist.parent;
                        wrist.SetParent(absolute, true);
                        Vector3 posOffset = wrist.localPosition;
                        //safety position check
                        if (posOffset.sqrMagnitude > 0.4f)
                        {
                            posOffset = Vector3.zero;
                        }
                        Vector3 rotOffset = wrist.localEulerAngles;
                        wrist.SetParent(oldParent, true);
                        SetPlayerAbsoluteVROffsets(posOffset, rotOffset, targetHoldState == GameDirector.player.rightHandState);
                        Destroy(ghost);
                        ghost = null;
                        targetHoldState = null;
                        GameDirector.player.SetFreezeVRHandTransforms(false);
                    }
                }
                yield return null;
            }
        }

    }

}