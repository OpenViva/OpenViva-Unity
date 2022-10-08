using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static viva.GameDirector;

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

            private float present_lastPalmResetTime = 0.0f;
            private float present_waitForReset = 0.0f;

            private float stop_lastPalmResetTime = 0.0f;
            private float stop_waitForReset = 0.0f;

            private int hello_step = 0;
            private float hello_lastRelativeUpX;
            private int hello_currWaveSign;
            private int hello_lastWaveSign;
            private float hello_lastWaveTime;
            private float hello_oldWaveTime;

            private bool presented = false;

            public bool AttemptHello(Transform head)
            {
                Vector3 relativeUp = head.InverseTransformDirection(gestureTarget.up);
                Vector3 relativeForward = head.InverseTransformDirection(gestureTarget.forward);
                if( relativeUp.y > 0.6f && relativeForward.z > 0.6f ){
                
                const float minWaveDeltaX = 0.2f;
                if( relativeUp.x > hello_lastRelativeUpX+minWaveDeltaX ){
                    hello_currWaveSign = 1;
                    hello_lastRelativeUpX = relativeUp.x;
                }else if( relativeUp.x < hello_lastRelativeUpX-minWaveDeltaX ){
                    hello_currWaveSign = -1;
                    hello_lastRelativeUpX = relativeUp.x;
                }
                if( hello_currWaveSign != hello_lastWaveSign ){
                    float waveTime = Mathf.Abs( Time.time-hello_lastWaveTime );
                    if( Mathf.Abs( waveTime-hello_oldWaveTime ) < 0.125f ){
                        if( ++hello_step > 3 ){
                            hello_step = -4;
                            return true;
                        }
                    }else{
                        hello_step = 0;
                    }
                    hello_lastWaveTime = Time.time;
                    hello_oldWaveTime = waveTime;
                }
                hello_lastWaveSign = hello_currWaveSign;
            }else{
                if( hello_step != 0 ){
                    //Debug.LogError("!"+Mathf.Abs( 1.0f-relativeForward.y ));
                }
                hello_step = 0;
            }
            return false;
            }

            public bool? AttemptPresent(Transform head)
            {
                Vector3 relativeUp = head.InverseTransformDirection(gestureTarget.up);
                Vector3 relativeForward = head.InverseTransformDirection(gestureTarget.forward);
                if ( relativeForward.y > 0.75f && relativeUp.z > 0.75f ){   //hand out
                Vector3 localHand = head.InverseTransformPoint( gestureTarget.position );
                if( Mathf.Abs( localHand.x ) < 0.3f &&    //hands x near head center
                    Mathf.Abs( localHand.y+0.2f ) < 0.2f &&    //hands y near head center
                    localHand.z > 0.2f                  //hands z in front of head
                    ){               
                    if( Time.time-present_lastPalmResetTime > 0.5f && present_waitForReset <= 0.0f ){
                        present_waitForReset = 0.3f;
                        presented = true;
                        return true;
                    }
                }else{
                    present_lastPalmResetTime = Time.time;
                }
            }else{
                present_lastPalmResetTime = Time.time;
                if( present_waitForReset > 0.0f ){
                    present_waitForReset -= Time.deltaTime;
                    if( present_waitForReset <= 0.0f && presented ){
                        presented = false;
                        return false;
                    }                
                }else{
                    present_waitForReset -= Time.deltaTime;
                }
            }
            return null;
            }

            public bool AttemptStop(Transform head)
            {
                Vector3 relativeUp = head.InverseTransformDirection(gestureTarget.up);
                Vector3 relativeForward = head.InverseTransformDirection(gestureTarget.forward);
                if (relativeUp.y > 0.6f && Mathf.Abs(relativeForward.x + signFlip * 0.2f) < 0.2f)
                {   //hand out
                    Vector3 localHand = head.InverseTransformPoint(gestureTarget.position);
                    if (Mathf.Abs(localHand.x) < 0.3f &&    //hands x near head center
                        Mathf.Abs(localHand.y + 0.2f) < 0.2f &&    //hands y near head center
                        localHand.z > 0.3f                  //hands z in front of head
                        )
                    {
                        if (Time.time - stop_lastPalmResetTime > 0.5f && stop_waitForReset <= 0.0f)
                        {
                            stop_waitForReset = 0.3f;
                            return true;
                        }
                    }
                    else
                    {
                        stop_lastPalmResetTime = Time.time;
                    }
                }
                else
                {
                    stop_lastPalmResetTime = Time.time;
                    if (stop_waitForReset > 0.0f)
                    {
                        stop_waitForReset -= Time.deltaTime;
                        if (stop_waitForReset <= 0.0f)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        stop_waitForReset -= Time.deltaTime;
                    }
                }
                return false;
            }

            public bool AttemptFollow(Transform head)
            {
                Vector3 relativeUp = head.InverseTransformDirection(gestureTarget.up);
                if (follow_step < 5)
                {
                    if (Mathf.Abs(-0.4f * signFlip - relativeUp.x) < 0.8f)
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
                                    else if (Time.time - follow_lastSwingTime > 0.1f && Time.time - follow_lastSwingTime < 1f && swingDistance > 0.2f)
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
                    present_waitForReset = 1.0f; //cancel present
                    follow_step = -4;
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
            WAIT,
            STOP
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
            else if (GameDirector.player.controls != Player.ControlType.KEYBOARD)
            {
                if (sourceHand.AttemptStop(head))
                {
                    FireGesture(sourceHand, Gesture.STOP);
                }
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
                        loli.active.OnGesture(sourceHand.playerHandState.selfItem, gesture);
                    }
                    GameDirector.player.pauseMenu.ContinueTutorial(PauseMenu.MenuTutorial.WAIT_TO_COME_HERE);
                    SendGestureToVisibleCharacters(sourceHand, gesture);
                    break;

                case Gesture.HELLO:                    
                    SendGestureToVisibleCharacters(sourceHand, gesture);
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
            Debug.Log("Gestured " + gesture);
        }

        public T FindSpherecastCharacter<T>( Vector3 rayStart, Vector3 rayForward, float rayLength, BoolReturnCharacterFunc validate =null) where T: Character
        {
            var rayEnd = rayStart+rayForward*rayLength;
            var mask = WorldUtil.characterMovementMask | WorldUtil.wallsMask | WorldUtil.itemsMask;

            var rayCastHits = Physics.SphereCastAll(rayStart, 0.25f, rayEnd-rayStart, rayLength, mask, QueryTriggerInteraction.Ignore);
            float shortestDistSq = Mathf.Infinity;
            T shortest = null;
            foreach(var raycast in rayCastHits){
                var result = raycast.collider.GetComponentInParent<T>();
                if(result && result != shortest)
                {
                    var distSq = Vector3.SqrMagnitude(raycast.point-rayStart);
                    if(distSq < shortestDistSq)
                    {
                        if (validate == null || validate(result))
                        {
                            shortestDistSq = distSq;
                            shortest = result;
                        }
                    }
                }
            }
            return shortest;
        }

        private void SendGestureToVisibleCharacters(GestureHand gestureHand, Gesture gesture)
        {

            var rayStart = gestureHand.gestureTarget.position;
            //fire horizontal rays from caller
            int rayCount = 8;
            float yawAngle = 40.0f;
            float yawStep = yawAngle / 8;
            var rayLength = 10.0f;
            float currentYaw = yawStep * rayCount / -2;
            var rayForward = player.head.transform.forward;

            var seen = new List<Character>();

            //Call if selected
            foreach (Loli loli in selectedLolis)
            {
                loli.active.OnGesture(gestureHand.playerHandState.selfItem, gesture);
            }

            for (int i = 0; i < rayCount; i++)
            {
                currentYaw += yawStep;

                var currentRayForward = Quaternion.Euler(0, currentYaw, 0) * rayForward;
                var character = FindSpherecastCharacter<Character>(rayStart, currentRayForward, rayLength, delegate (Character character) {
                    //ignore character being grabbed by player
                    var candidate = character as Loli;
                    //if (candidate.isConstrained) return false;

                    return candidate;
                });
                if (character != null && !seen.Contains(character)) seen.Add(character);
            }

            foreach (var character in seen)
            {
                var loli = character as Loli;
                //loli.onGesture.Invoke(gesture, player.character);
                if (!selectedLolis.Contains(loli))
                {
                    Outline.StartOutlining(loli, Color.white, Outline.Flash);
                    loli.active.OnGesture(gestureHand.playerHandState.selfItem, gesture);
                }
            }
            //player.character.onSendGesture.Invoke(gesture);

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
                gestureDisplayMat.SetFloat(WorldUtil.alphaID, Tools.EaseInOutCubic(ratio));
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
                gestureDisplayMat.SetFloat(WorldUtil.alphaID, 1.0f - ratio);
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
                gestureDisplayMat.SetFloat(WorldUtil.alphaID, 0.75f);
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
            if (!GamePhysics.GetRaycastInfo(point, dir, 20.0f, WorldUtil.wallsMask | WorldUtil.visionMask, QueryTriggerInteraction.Collide))
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
            gestureDisplayMat.SetFloat(WorldUtil.alphaID, alpha);
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