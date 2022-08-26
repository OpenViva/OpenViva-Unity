using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva
{


    public partial class ObjectFingerPointer : MonoBehaviour
    {

        [System.Serializable]
        public class GestureHand
        {

            public Transform gestureTarget;
            public PlayerHandState playerHandState;
            public float signFlip = 1.0f;

            private int follow_step = 0;
            private float follow_lastSwingTime = 0.0f;
            private int follow_targetSignForwardY = 0;
            private float follow_startForwardY = 0.0f;
            private float follow_lastRelativeForwardY;

            private float present_lastPalmUpTime = 0.0f;
            private float present_waitForReset = 0.0f;

            private int hello_step = 0;
            private float hello_lastWaveTime = 0.0f;
            private int hello_targetSignForwardX = 0;
            private float hello_startForwardX = 0.0f;
            private float hello_lastForwardX = 0.0f;


            public bool AttemptHello(Transform head)
            {
                Vector3 relativeUp = head.InverseTransformDirection(gestureTarget.up);
                Vector3 relativeRight = head.InverseTransformDirection(gestureTarget.right);
                //hand point up and forward
                if (relativeUp.y > 0.7f && relativeRight.x > 0.7f)
                {

                    int signUpX = (int)Mathf.Sign(relativeUp.x - hello_lastForwardX);
                    if (signUpX == 0)
                    {
                        signUpX = -1;
                    }
                    if (hello_step == 0)
                    {
                        hello_targetSignForwardX = signUpX;
                        hello_step++;
                        hello_lastWaveTime = Time.time;
                        hello_startForwardX = relativeUp.x;

                    }
                    else if (signUpX != hello_targetSignForwardX)
                    { //wave dir change detected
                        hello_targetSignForwardX = signUpX;
                        float waveDistance = Mathf.Abs(relativeUp.x - hello_startForwardX);
                        if (Mathf.Abs(1.2f - waveDistance) < 0.8f && Mathf.Abs(0.5f - (Time.time - hello_lastWaveTime)) < 0.44f)
                        {
                            hello_step++;
                            if (hello_step > 4)
                            {
                                hello_step = 0;
                                return true;
                            }
                        }
                        else
                        {
                            hello_step = 0;
                        }
                        hello_lastWaveTime = Time.time;
                        hello_startForwardX = relativeUp.x;
                    }
                    hello_lastForwardX = relativeUp.x;
                }
                else
                {
                    if (hello_step != 0)
                    {
                        //Debug.LogError("!"+Mathf.Abs( 1.0f-relativeForward.y ));
                    }
                    hello_step = 0;
                }
                return false;
            }

            public bool? AttemptPresent(Transform head)
            {

                Vector3 handForward = head.InverseTransformDirection(gestureTarget.forward);
                if (handForward.y > 0.7f)
                {   //hand facing up

                    Vector3 localHand = head.InverseTransformPoint(gestureTarget.position);
                    Vector3 handUp = head.InverseTransformDirection(gestureTarget.up);
                    if (Mathf.Abs(head.forward.y) < 0.75f && //head facing straight
                        Mathf.Abs(localHand.x) < 0.3f &&    //hands x near head center
                        Mathf.Abs(localHand.y) < 0.4f &&    //hands y near head center
                        localHand.z > 0.1f &&               //hands z in front of head
                        handUp.z > 0.8f                     //hand pointing out
                        )
                    {
                        if (Time.time - present_lastPalmUpTime > 0.5f && present_waitForReset <= 0.0f)
                        {
                            present_waitForReset = 0.3f;
                            return true;
                        }
                    }
                    else
                    {
                        present_lastPalmUpTime = Time.time;
                    }
                }
                else
                {
                    present_lastPalmUpTime = Time.time;
                    if (present_waitForReset > 0.0f)
                    {
                        present_waitForReset -= Time.deltaTime;
                        if (present_waitForReset <= 0.0f)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        present_waitForReset -= Time.deltaTime;
                    }
                }
                return null;
            }

            public bool AttemptFollow(Transform head)
            {
                Vector3 relativeUp = head.InverseTransformDirection(gestureTarget.up);
                if (follow_step < 4)
                {
                    if (Mathf.Abs(-0.4f - relativeUp.x) < 0.8f)
                    { //must twist hand to face up ~90 degrees

                        Vector3 relativeForward = head.InverseTransformDirection(gestureTarget.forward);
                        if (relativeForward.z > 0.0f)
                        {
                            follow_lastSwingTime = Time.time;
                            follow_step = 0;
                            return false;
                        }
                        int signForwardY = (int)Mathf.Sign(relativeForward.y - follow_lastRelativeForwardY);
                        if (signForwardY == 0)
                        {
                            signForwardY = -1;
                        }
                        if (follow_step == 0)
                        {
                            follow_targetSignForwardY = signForwardY;
                            follow_step++;
                            follow_lastSwingTime = Time.time - 0.1f;
                            follow_startForwardY = relativeForward.y;

                        }
                        else
                        {
                            float swingDistance = Mathf.Abs(follow_startForwardY - relativeForward.y);
                            if (signForwardY != follow_targetSignForwardY)
                            {
                                // Debug.Log("~X "+(Time.time-follow_lastSwingTime)+","+swingDistance+" @ "+relativeForward.y);
                                if (swingDistance > 0.15f)
                                {
                                    follow_targetSignForwardY = signForwardY;
                                    if (Time.time - follow_lastSwingTime > 0.8f)
                                    {    //break conditions
                                        follow_step = 0;
                                    }
                                    else if (Time.time - follow_lastSwingTime > 0.1f && swingDistance > 0.2f)
                                    {
                                        follow_step++;
                                        //Debug.Log("+"+follow_step);
                                        follow_lastSwingTime = Time.time;
                                        follow_startForwardY = relativeForward.y;
                                    }
                                }
                            }
                        }
                        follow_lastRelativeForwardY = relativeForward.y;

                    }
                    else
                    {
                        follow_lastSwingTime = Time.time;
                        follow_step = 0;
                    }
                }
                else
                {
                    follow_step = 0;
                    present_waitForReset = 1.0f; //cancel present
                    return true;
                }
                return false;
            }
            public void ResetAll()
            {
                follow_step = 0;
                hello_step = 0;
            }
        }

        public enum Gesture
        {
            FOLLOW,
            PRESENT_START,
            PRESENT_END,
            HELLO,
            MECHANISM,
            WAIT
        }

        [Header("Gestures")]
        [SerializeField]
        private GameObject gestureDisplay;
        [SerializeField]
        private Material gestureDisplayMat;
        [SerializeField]
        private Texture2D[] gestureTextures;
        [SerializeField]
        private AudioClip newSelectionSound;
        [SerializeField]
        private AudioClip[] gestureSounds = new AudioClip[System.Enum.GetValues(typeof(Gesture)).Length];
        [SerializeField]
        private GestureHand[] gestureHands = new GestureHand[2];
        public GestureHand rightGestureHand { get { return gestureHands[0]; } }
        public GestureHand leftGestureHand { get { return gestureHands[1]; } }

        public List<Loli> selectedLolis { get; private set; } = new List<Loli>();
        private Coroutine pointingCoroutine = null;
        private List<VivaSessionAsset> pointedAssets = new List<VivaSessionAsset>();
        private Coroutine gestureDisplayCoroutine = null;
        private static readonly int clockProgressID = Shader.PropertyToID("_ClockProgress");
        private bool pointedShort;
        private Loli pointedLoli = null;
        private Mechanism pointedMechanism = null;
        private Vector3 pointedPos;


        public void UpdateGestureDetection(GestureHand sourceHand, Transform head)
        {            
            if (sourceHand.AttemptHello(head))
            {
                FireGesture(sourceHand, Gesture.HELLO);
            }
            else if (sourceHand.AttemptFollow(head))
            {
                FireGesture(sourceHand, Gesture.FOLLOW);
            }
            bool? presenting = sourceHand.AttemptPresent(head);
            if (presenting.HasValue)
            {
                if (presenting.Value)
                {
                    FireGesture(sourceHand, Gesture.PRESENT_START);
                }
                else
                {
                    FireGesture(sourceHand, Gesture.PRESENT_END);
                }
            }
        }

        public void FireGesture(GestureHand sourceHand, Gesture gesture)
        {

            PlayDisplayCoroutine(Vector3.zero, sourceHand.gestureTarget, gesture);

            switch (gesture)
            {
                case Gesture.FOLLOW:
                    foreach (Loli loli in selectedLolis)
                    {
                        loli.active.OnGesture(sourceHand.playerHandState.selfItem, Gesture.FOLLOW);
                    }
                    GameDirector.player.pauseMenu.ContinueTutorial(PauseMenu.MenuTutorial.WAIT_TO_COME_HERE);
                    break;

                case Gesture.HELLO:
                    viva.DevTools.LogExtended("Gesture HELLO", true, true);
                    foreach (Loli loli in selectedLolis)
                    {
                        loli.active.OnGesture(sourceHand.playerHandState.selfItem, Gesture.HELLO);
                    }
                    GameDirector.player.pauseMenu.ContinueTutorial(PauseMenu.MenuTutorial.WAIT_TO_WAVE);
                    break;

                case Gesture.MECHANISM:
                    break;

                case Gesture.PRESENT_START:
                    if (sourceHand.playerHandState.heldItem != null)
                    {
                        int index = selectedLolis.Count;
                        while (--index >= 0)
                        {
                            if (index >= selectedLolis.Count)
                            {
                                continue;
                            }
                            Loli loli = selectedLolis[index];
                            viva.DevTools.LogExtended("Presenting " + sourceHand.playerHandState.heldItem + " to loli " + loli, true, true);
                            loli.onGiftItemCallstack.Call(sourceHand.playerHandState.heldItem);
                        }
                    }
                    GameDirector.player.pauseMenu.ContinueTutorial(PauseMenu.MenuTutorial.WAIT_TO_PRESENT);
                    break;

                case Gesture.PRESENT_END:
                    if (sourceHand.playerHandState.heldItem != null)
                    {
                    }
                    break;
            }
        }

        public void ClearSelection()
        {
            foreach (Loli loli in selectedLolis)
            {
                loli.characterSelectionTarget.OnUnselected();
            }
            selectedLolis.Clear();
        }

        private void UpdateGestureDisplayRotation(float bias = 2.0f)
        {
            Vector3 diff = GameDirector.player.head.position - gestureDisplay.transform.position;
            gestureDisplay.transform.rotation = Quaternion.LookRotation(diff, Vector3.up) * Quaternion.Euler(0.0f, -90.0f, 0.0f);
            gestureDisplay.transform.localScale = Vector3.one * diff.magnitude * bias;
        }

        private IEnumerator DisplayGesture()
        {
            gestureDisplay.SetActive(true);
            float maxTime = 0.3f;
            float timer = 0.0f;
            while (timer < maxTime)
            {
                timer += Time.deltaTime;
                float ratio = timer / maxTime;
                gestureDisplay.transform.localScale = Vector3.one * (Tools.EaseInQuad(1.0f - ratio) * 0.5f + 1.0f);
                gestureDisplayMat.SetFloat(Instance.alphaID, Tools.EaseInOutCubic(ratio));
                UpdateGestureDisplayRotation();
                yield return new WaitForEndOfFrame();
            }
            timer = 0.5f;
            while (timer > 0.0f)
            {
                timer -= Time.deltaTime;
                UpdateGestureDisplayRotation();
                yield return new WaitForEndOfFrame();
            }
            maxTime = 0.3f;
            timer = 0.0f;
            while (timer < maxTime)
            {
                timer += Time.deltaTime;
                float ratio = timer / maxTime;
                gestureDisplayMat.SetFloat(Instance.alphaID, 1.0f - ratio);
                UpdateGestureDisplayRotation();
                yield return new WaitForEndOfFrame();
            }
            gestureDisplay.SetActive(false);
            yield return null;
        }

        public void PointDown(PlayerHandState sourceHandState)
        {
            if (pointingCoroutine != null)
            {
                GameDirector.instance.StopCoroutine(pointingCoroutine);
                pointingCoroutine = null;
            }
            if (sourceHandState.occupied)
            {
                return;
            }
            if (pointedShort)
            {
                pointingCoroutine = GameDirector.instance.StartCoroutine(ValidateWaitAt());
            }
            else
            {
                OnPoint(sourceHandState);
            }
        }

        public void PointUp(PlayerHandState sourceHandState)
        {
            if (pointedShort || sourceHandState.occupied)
            {
                if (pointingCoroutine != null)
                {
                    GameDirector.instance.StopCoroutine(pointingCoroutine);
                    pointingCoroutine = null;
                    StopDisplayCoroutine();
                }
                pointedShort = false;
            }
            else
            {
                float upTimeout = GameDirector.player.controls == Player.ControlType.KEYBOARD ? 0.3f : 1.0f;
                if (Time.time - sourceHandState.gripState.lastDownTime < upTimeout)
                {
                    pointedShort = true;
                    pointingCoroutine = GameDirector.instance.StartCoroutine(ValidateLastPointing());
                }
                else
                {
                    pointedShort = false;
                }
            }
        }

        private IEnumerator ValidateWaitAt()
        {
            if (LocomotionBehaviors.isOnWalkableFloor(pointedPos))
            {
                gestureDisplay.transform.SetParent(null);
                gestureDisplayMat.SetFloat(Instance.alphaID, 0.75f);
            }
            else
            {
                yield break;
            }
            float timer = 0.0f;
            float duration = 1.0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;

                float clockProgress = timer / duration;
                float distance = Vector3.Magnitude(GameDirector.instance.mainCamera.transform.position - pointedPos);
                float size = Mathf.Clamp(2.5f - distance * 0.2f, 0.4f, 1.8f);

                DisplayGestureStatic(pointedPos, clockProgress, 0.7f, Gesture.WAIT, size);
                yield return null;
            }
            foreach (var loli in selectedLolis)
            {
                if (!loli.IsHappy())
                {
                    loli.active.idle.PlayAvailableRefuseAnimation();
                    yield break;
                }
                var waitAtPosition = new AutonomyMoveTo(loli.autonomy, "wait at command", delegate (TaskTarget target)
                {
                    target.SetTargetPosition(pointedPos);
                }, selectedLolis.Count * 0.2f, BodyState.STAND, delegate (TaskTarget target)
                {
                    target.SetTargetPosition(GameDirector.instance.mainCamera.transform.position);
                });
                loli.autonomy.SetAutonomy(waitAtPosition);
            }
            PlayDisplayCoroutine(pointedPos, null, Gesture.WAIT);

            pointedShort = false;
            pointingCoroutine = null;
        }

        private IEnumerator ValidateLastPointing()
        {
            yield return new WaitForSeconds(0.3f);
            if (pointedLoli)
            {
                if (!selectedLolis.Contains(pointedLoli))
                {
                    pointedLoli.characterSelectionTarget.OnSelected();
                    selectedLolis.Add(pointedLoli);
                    GameDirector.instance.PlayGlobalSound(newSelectionSound);
                }
                else
                {
                    pointedLoli.characterSelectionTarget.OnUnselected();
                    selectedLolis.Remove(pointedLoli);
                }
                pointedLoli = null;
            }
            else if (pointedMechanism)
            {
                PlayDisplayCoroutine(pointedPos, null, Gesture.MECHANISM);
                bool succeededAtLeastOnce = false;
                int index = 0;
                while (index < selectedLolis.Count)
                {
                    var loli = selectedLolis[index];
                    succeededAtLeastOnce |= pointedMechanism.AttemptCommandUse(loli, GameDirector.player);
                    index++;
                }
                if (succeededAtLeastOnce)
                {
                    GameDirector.instance.HighlightMechanism(pointedMechanism);
                }
                pointedMechanism = null;
            }

            pointingCoroutine = null;
            pointedShort = false;
        }

        private void OnPoint(PlayerHandState playerHandState)
        {

            Vector3 point;
            Vector3 dir;
            if (GameDirector.player.controls == Player.ControlType.KEYBOARD)
            {
                point = GameDirector.player.head.position;
                dir = GameDirector.player.head.forward;
            }
            else
            {
                point = playerHandState.directionPoint;
                dir = playerHandState.directionPointing;
            }

            //sample mechanism triggers
            if (!GamePhysics.GetRaycastInfo(point, dir, 20.0f, Instance.wallsMask | Instance.visionMask, QueryTriggerInteraction.Collide))
            {
                Debug.DrawLine(point, point + dir * 20.0f, Color.red, 3.0f);
                return;
            }
            Transform newRoot = GamePhysics.result().collider.transform;
            pointedPos = GamePhysics.result().point;
            pointedLoli = Tools.SearchTransformAncestors<Loli>(newRoot);
            if (!pointedLoli)
            {
                pointedMechanism = Tools.SearchTransformAncestors<Mechanism>(newRoot);
                if (pointedMechanism)
                {
                    Debug.DrawLine(point, point + dir * 20.0f, Color.blue, 3.0f);
                }
                else
                {
                    Debug.DrawLine(point, point + dir * 20.0f, Color.black, 3.0f);
                }
            }
            else
            {
                Debug.DrawLine(point, point + dir * 20.0f, Color.green, 3.0f);
                pointedMechanism = null;
            }
            if (GameDirector.player.controls != Player.ControlType.KEYBOARD)
            {
                GameDirector.instance.StartCoroutine(PointMesh(playerHandState));
            }
        }

        private IEnumerator PointMesh(PlayerHandState playerHandState)
        {
            playerHandState.directionPointerContainer.SetActive(true);
            yield return new WaitForSeconds(0.15f);
            playerHandState.directionPointerContainer.SetActive(false);
        }

        public void SetupDisplayTexture(Gesture gesture)
        {
            gestureDisplayMat.mainTexture = gestureTextures[(int)gesture];
        }

        private void DisplayGestureStatic(Vector3 pos, float clockProgress, float alpha, Gesture gesture, float size)
        {
            gestureDisplay.SetActive(true);
            gestureDisplayMat.SetFloat(clockProgressID, clockProgress);
            gestureDisplayMat.SetFloat(Instance.alphaID, alpha);
            gestureDisplay.transform.position = pos;
            SetupDisplayTexture(gesture);
            UpdateGestureDisplayRotation(size);
        }

        public void PlayDisplayCoroutine(Vector3 localPos, Transform source, Gesture gesture)
        {
            gestureDisplayMat.SetFloat(clockProgressID, 1.0f);
            SetupDisplayTexture(gesture);
            if (gestureDisplayMat.mainTexture == null)
            {
                return;
            }

            if (gestureDisplayCoroutine != null)
            {
                GameDirector.instance.StopCoroutine(gestureDisplayCoroutine);
            }
            GameDirector.instance.PlayGlobalSound(gestureSounds[(int)gesture]);

            gestureDisplay.transform.SetParent(source);
            gestureDisplay.transform.localPosition = localPos;

            gestureDisplayCoroutine = GameDirector.instance.StartCoroutine(DisplayGesture());
        }

        public void StopDisplayCoroutine()
        {

            if (gestureDisplayCoroutine != null)
            {
                GameDirector.instance.StopCoroutine(gestureDisplayCoroutine);
                gestureDisplayCoroutine = null;
            }
            gestureDisplay.SetActive(false);
        }
    }

}