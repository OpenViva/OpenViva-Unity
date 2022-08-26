using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace viva
{


    public partial class GameDirector : MonoBehaviour
    {

        public enum ControlsAllowed
        {
            ALL,
            HAND_INPUT_ONLY,
            NONE
        }

        public delegate void ControlOverrideCallback();

        private GameObject lastUIObjectHovered = null;
        private GameObject lastUIObjectDowned = null;
        private ControlOverrideCallback lastControlChange = null;
        public PointerEventData eventData;

        private Coroutine UICoroutine = null;
        private bool pauseUIInput = false;
        public UIMenu lastMenu { get; private set; }

        [Header("UI")]
        [SerializeField]
        private EventSystem eventSystem;    //there should be 1 in the entire scene

        [SerializeField]
        private MeshFilter raycastLaserMF;

        private Mesh laserMesh;
        private bool disableNextClick = false;
        private List<Vector3> laserVertices = new List<Vector3>();
        private List<Vector2> laserUVs = new List<Vector2>();
        private int[] laserIndices = new int[12];   //4 triangles
        private bool aimingAtUI = false;
        private Slider currentDrag = null;
        private ControlsAllowed m_controlsAllowed = ControlsAllowed.ALL;
        public ControlsAllowed controlsAllowed { get { return m_controlsAllowed; } }
        private Player sourcePlayer = null;
        private bool useRightHandForVRPointer = true;


        public void DisableNextClick()
        {
            disableNextClick = true;
        }

        private void StopUICoroutine()
        {
            if (UICoroutine != null)
            {
                GameDirector.instance.StopCoroutine(UICoroutine);
                UICoroutine = null;
            }
        }

        public void UpdateSourcePlayerUIControlType()
        {
            StopUICoroutine();

            Transform pointer;
            PlayerHandState.ButtonState button;
            if (sourcePlayer.controls == Player.ControlType.KEYBOARD)
            {

                pointer = null;
                button = player.leftPlayerHandState.actionState;
                SetEnableCursor(true);
                m_controlsAllowed = ControlsAllowed.NONE;

                if (lastMenu.KeyboardModeBringsHandDown(true))
                {
                    sourcePlayer.rightPlayerHandState.animSys.SetTargetAndIdleOverrideAnimation(Player.Animation.KEYBOARD_HANDS_DOWN);
                }
                if (lastMenu.KeyboardModeBringsHandDown(false))
                {
                    sourcePlayer.leftPlayerHandState.animSys.SetTargetAndIdleOverrideAnimation(Player.Animation.KEYBOARD_HANDS_DOWN);
                }

                raycastLaserMF.gameObject.SetActive(false);
            }
            else
            {
                PlayerHandState hand;
                if (useRightHandForVRPointer)
                {
                    hand = sourcePlayer.rightPlayerHandState;
                }
                else
                {
                    hand = sourcePlayer.leftPlayerHandState;
                }
                pointer = hand.behaviourPose.transform;
                button = hand.actionState;
                m_controlsAllowed = ControlsAllowed.NONE;
                raycastLaserMF.gameObject.SetActive(true);
            }
            InitializeLaserPointer();
            UICoroutine = GameDirector.instance.StartCoroutine(UpdatePointer(pointer, button, lastMenu));
        }

        private void SetLastNewMenu(UIMenu newMenu)
        {
            //hide hand models for keyboard mode
            if (lastMenu && lastMenu.keyboardHeadTransform != null && player.controls == Player.ControlType.KEYBOARD)
            {
                GameDirector.player.SetShowModel(true);
            }
            //restore hand models for keyboard mode
            if (newMenu && newMenu.keyboardHeadTransform != null && player.controls == Player.ControlType.KEYBOARD)
            {
                GameDirector.player.SetShowModel(false);
            }
            if (lastMenu)
            {
                lastMenu.OnExitUIInput();
            }
            lastMenu = newMenu;
        }

        public void BeginUIInput(UIMenu newMenu, Player newSourcePlayer, bool _useRightHandForVRPointer = true)
        {
            useRightHandForVRPointer = _useRightHandForVRPointer;
            if (newMenu == null)
            {
                Debug.LogError("ERROR Cannot BeginUIInput to null menu!");
                return;
            }
            if (lastMenu == newMenu)
            {
                return;
            }
            if (lastMenu != null)
            {
                lastMenu.OnExitUIInput();
            }
            SetLastNewMenu(newMenu);
            player.SetCrosshair(false); //disable crosshair
            sourcePlayer = newSourcePlayer;
            UpdateSourcePlayerUIControlType();
            lastMenu.OnBeginUIInput();
        }

        private void InitializeLaserPointer()
        {
            if (laserMesh == null)
            {
                laserMesh = new Mesh();
                raycastLaserMF.mesh = laserMesh;

                for (int i = 0; i < 8; i++)
                {
                    laserVertices.Add(Vector3.zero);
                }
                int index = 0;
                for (int i = 0; i < 2; i++)
                {
                    laserUVs.Add(Vector2.zero);
                    laserUVs.Add(new Vector2(1.0f, 0.0f));
                    laserUVs.Add(new Vector2(0.0f, 1.0f));
                    laserUVs.Add(Vector2.one);

                    laserIndices[index++] = i * 4;
                    laserIndices[index++] = i * 4 + 1;
                    laserIndices[index++] = i * 4 + 2;
                    laserIndices[index++] = i * 4 + 2;
                    laserIndices[index++] = i * 4 + 1;
                    laserIndices[index++] = i * 4 + 3;
                }
            }
        }

        private void UpdateRaycastLaserWithPointer(Vector3 laserStart, Vector3 laserEnd, Vector3 up, Vector3 right)
        {
            laserVertices[0] = laserStart + right;
            laserVertices[1] = laserStart - right;
            laserVertices[2] = laserEnd + right;
            laserVertices[3] = laserEnd - right;
            laserVertices[4] = laserStart + up;
            laserVertices[5] = laserStart - up;
            laserVertices[6] = laserEnd + up;
            laserVertices[7] = laserEnd - up;

            laserMesh.SetVertices(laserVertices);
            laserMesh.SetUVs(0, laserUVs);
            laserMesh.SetIndices(laserIndices, MeshTopology.Triangles, 0);

            laserMesh.RecalculateBounds();
        }

        public void SetEnableCursor(bool enable)
        {
            if (enable)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public bool IsAnyUIMenuActive()
        {
            return UICoroutine != null;
        }

        public bool IsAimingAtUI()
        {
            return aimingAtUI;
        }

        public bool isUIMenuActive(UIMenu menu)
        {
            return lastMenu == menu;
        }

        public void StopUIInput()
        {
            if (UICoroutine == null)
            {
                return;
            }
            SetLastNewMenu(null);

            StopUICoroutine();

            aimingAtUI = false;
            raycastLaserMF.gameObject.SetActive(false);
            SetEnableControls(ControlsAllowed.ALL);
            player.SetCrosshair(true); //enable crosshair

            ///TODO: RETURN TO CURRENT ITEMS ANIMATION NOT IDLE!
            sourcePlayer.rightPlayerHandState.animSys.SetTargetAndIdleOverrideAnimation(Player.Animation.NONE);
            sourcePlayer.leftPlayerHandState.animSys.SetTargetAndIdleOverrideAnimation(Player.Animation.NONE);

            sourcePlayer = null;
        }

        public void PauseUIInput()
        {
            pauseUIInput = true;
        }

        public void ResumeUIInput()
        {
            pauseUIInput = false;
        }

        public void SetEnableControls(ControlsAllowed allowed, ControlOverrideCallback onChanged = null)
        {
            m_controlsAllowed = allowed;
            SetEnableCursor(allowed > ControlsAllowed.ALL);
            if (lastControlChange != null)
            {
                lastControlChange();
            }
            lastControlChange = onChanged;
        }

        public void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && m_controlsAllowed == ControlsAllowed.NONE && !IsAnyUIMenuActive())
            {
                SetEnableControls(ControlsAllowed.ALL);
            }
        }

        private void UpdateLastHovered()
        {
            if (lastUIObjectHovered)
            {
                ExecuteEvents.Execute(lastUIObjectHovered, eventData, ExecuteEvents.pointerExitHandler);
                lastUIObjectHovered = null;
            }
        }

        private IEnumerator UpdatePointer(Transform pointer, PlayerHandState.ButtonState button, UIMenu activeMenu)
        {

            Vector3 cachedHeadPos = sourcePlayer.head.position;
            Quaternion cachedHeadRot = sourcePlayer.head.rotation;
            float ease = 0.0f;

            while (true)
            {

                if (activeMenu.keyboardHeadTransform != null)
                {
                    ease = Mathf.Clamp01(ease + Time.deltaTime * 2.0f);
                    sourcePlayer.head.position = Vector3.LerpUnclamped(cachedHeadPos, activeMenu.keyboardHeadTransform.position, ease);
                    sourcePlayer.head.rotation = Quaternion.LerpUnclamped(cachedHeadRot, activeMenu.keyboardHeadTransform.rotation, ease);
                }
                if (pauseUIInput)
                {
                    yield return null;
                    continue;
                }

                eventData = new PointerEventData(eventSystem);

                if (pointer != null)
                {
                    RaycastHit hitInfo = new RaycastHit();
                    bool hit = Physics.Raycast(pointer.position, pointer.forward, out hitInfo, 4.0f, Instance.uiMask | Instance.wallsMask);

                    if (hit)
                    {  //make sure it hit the UI not the wall mask
                        hit = hitInfo.collider.gameObject.layer == Instance.uiLayer;
                    }
                    if (!hit)
                    {
                        UpdateRaycastLaserWithPointer(
                            pointer.position + pointer.up * 0.1f,
                            pointer.position + pointer.forward * 100.0f,
                            pointer.right * 0.01f,
                            pointer.forward * 0.005f
                        );
                        aimingAtUI = false;

                        UpdateLastHovered();
                        yield return null;
                        continue;
                    }
                    aimingAtUI = true;
                    UpdateRaycastLaserWithPointer(
                        pointer.position + pointer.up * 0.1f,
                        hitInfo.point,
                        pointer.right * 0.005f,
                        pointer.forward * 0.0025f
                    );
                    eventData.position = GameDirector.instance.mainCamera.WorldToScreenPoint(hitInfo.point);
                }
                else
                {
                    eventData.position = player.mousePosition;
                }

                if (button.isUp)
                {
                    if (disableNextClick)
                    {
                        button.Consume();
                        disableNextClick = false;
                    }
                }

                //raycast to 2D UI components
                List<RaycastResult> results = new List<RaycastResult>();
                activeMenu.GetGraphicRaycaster().Raycast(eventData, results);
                if (results.Count > 0)
                {

                    foreach (RaycastResult raycastResult in results)
                    {
                        GameObject newUIObject = raycastResult.gameObject;

                        if (newUIObject != lastUIObjectHovered)
                        {
                            if (lastUIObjectHovered != null)
                            {
                                ExecuteEvents.Execute(lastUIObjectHovered, eventData, ExecuteEvents.pointerExitHandler);
                            }
                            ExecuteEvents.Execute(newUIObject, eventData, ExecuteEvents.pointerEnterHandler);
                        }
                        lastUIObjectHovered = newUIObject;
                        if (button.isDown)
                        {
                            button.Consume();
                            lastUIObjectDowned = newUIObject;
                            ExecuteEvents.Execute(newUIObject, eventData, ExecuteEvents.pointerDownHandler);

                            eventData.pointerDrag = newUIObject;
                        }
                        else if (button.isUp)
                        {
                            button.Consume();
                            ExecuteEvents.Execute(newUIObject, eventData, ExecuteEvents.pointerUpHandler);
                            if (lastUIObjectDowned == newUIObject)
                            {
                                ExecuteEvents.Execute(newUIObject, eventData, ExecuteEvents.pointerClickHandler);
                            }
                        }
                        break;
                    }
                }
                else
                {
                    UpdateLastHovered();
                }
                yield return null;
            }
        }
    }

}