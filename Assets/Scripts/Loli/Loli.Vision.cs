using System.Collections.Generic;
using UnityEngine;


namespace viva
{


    public partial class Loli : Character
    {

        public delegate void VisibleItemCallback(Item item);

        public enum AwarenessMode
        {
            NORMAL,
            FOCUS,
            CURIOUS,
            TIRED
        }

        [SerializeField]
        private Transform m_pupil_r;
        public Transform pupil_r { get { return m_pupil_r; } }
        [SerializeField]
        private Transform m_pupil_l;
        public Transform pupil_l { get { return m_pupil_l; } }
        [SerializeField]
        private Transform eyeball_r;
        [SerializeField]
        private Transform eyeball_l;

        private Transform lastLookAtTransform = null;
        private Transform m_currentLookAtTransform = null;
        public Transform currentLookAtTransform { get { return m_currentLookAtTransform; } }
        private Item currentLookAtItem = null;
        private Vector3 lookAtTargetPos = Vector3.zero;
        private float changeLookAtTargetBlend = 1.0f;
        private float oldLookAtTargetRadius = 1.0f;
        private Vector3 oldLookAtTargetDiffNorm = Vector3.forward;
        private float changeLookAtSpeed = 1.0f;
        private AwarenessMode awarenessMode = AwarenessMode.NORMAL;
        private bool awarenessTiredViewRest = false;
        public VisibleItemCallback onEnterVisibleItem = null;
        public VisibleItemCallback onExitVisibleItem = null;


        private class ViewItem
        {

            public readonly Item item;
            public float ignoreTimer = 0.0f;
            public float outOfViewTimer = 0.0f;

            public ViewItem(Item _item)
            {
                item = _item;
            }
        }
        private float updateIgnoreListTimer = 0.0f;

        private float randomViewTimer = 0.0f;
        private Transform cachedLookAtTargetTransform = null;
        private Set<ViewItem> viewItems = new Set<ViewItem>();
        private List<Item> inViewButBlocked = new List<Item>();

        public class Eye
        {

            public LookAtBone lookAt;
            public Transform animationBone;
            public Material material;
            public bool valid;
        }

        // public Texture2D headSkin = null;
        public Eye rightEye { get; private set; }
        public Eye leftEye { get; private set; }
        private Vector3 lookAtHeadOffset = new Vector3(0.0f, -0.05f, 0.0f);
        private float lastLeftPupilNormYaw = 0.0f;
        private float lastRightPupilNormYaw = 0.0f;
        private int pupilShrinkBS;
        private int upperEyelidRightBS;
        private int upperEyelidLeftBS;
        private int lowerEyelidRightBS;
        private int lowerEyelidLeftBS;
        private int madEyelidRightBS;
        private int madEyelidLeftBS;
        private float eyelidFollowTargetBlend = 0.0f;
        private float eyelidFollowRealBlend = 0.0f;
        private float currAvgPupilDown = 0.0f;
        private float randomEyeTimer = 0.0f;
        private float randomEyeTimerWait = 0.0f;
        private Vector2 targetRandomEyeoffset = Vector2.zero;
        private Vector2 randomEyeOffset = Vector2.zero;
        private float randomEyeOffsetMult = 1.0f;
        private float blink = 0.0f;
        private int eyeFlags = 0;
        private float upperEyelidRate = 1.0f;

        public Item GetViewResult(int index)
        {
            if (index > viewItems.objects.Count)
            {
                return null;
            }
            return viewItems.objects[index].item;   //RESULT MAY BE NULL
        }

        public Item GetCurrentLookAtItem()
        {
            return currentLookAtItem;
        }

        public int GetViewResultCount()
        {
            return viewItems.objects.Count;
        }

        public void IgnoreItem(Item item, float duration)
        {
            if (item != null)
            {
                ViewItem found = null;
                for (int i = 0; i < viewItems.Count; i++)
                {
                    var viewItem = viewItems.objects[i];
                    if (viewItem.item == item)
                    {
                        found = viewItem;
                        break;
                    }
                }
                if (found == null)
                {
                    found = new ViewItem(item);
                    viewItems.Add(found);
                }
                found.ignoreTimer = duration;
            }
        }

        private void UpdateViewTimers()
        {
            const float waitForUpdate = 0.5f;
            if (Time.time - updateIgnoreListTimer < waitForUpdate)
            {
                return;
            }
            updateIgnoreListTimer = Time.time;

            for (int i = inViewButBlocked.Count; i-- > 0;)
            {
                var item = inViewButBlocked[i];
                if (item == null)
                {
                    inViewButBlocked.RemoveAt(i);
                    continue;
                }
                if (CanSeePoint(item.transform.position))
                {
                    inViewButBlocked.RemoveAt(i);
                    viewItems.Add(new ViewItem(item));
                    onEnterVisibleItem?.Invoke(item);
                }
            }

            for (int i = viewItems.Count; i-- > 0;)
            {
                var viewItem = viewItems.objects[i];
                if (viewItem.item == null)
                {
                    viewItems.objects.RemoveAt(i);
                    continue;
                }
                viewItem.ignoreTimer -= waitForUpdate;
                if (viewItem.outOfViewTimer >= Mathf.Epsilon)
                {
                    viewItem.outOfViewTimer += waitForUpdate;
                    if (viewItem.outOfViewTimer >= 2.0f)
                    {   //forget after 2 seconds of out of view
                        viewItems.objects.RemoveAt(i);
                        onExitVisibleItem?.Invoke(viewItem.item);
                    }
                }
            }
        }

        public bool ShouldIgnore(Item item)
        {
            foreach (ViewItem viewItem in viewItems.objects)
            {
                if (viewItem.item == item)
                {
                    return viewItem.ignoreTimer > 0.0f;
                }
            }
            return false;
        }

        public class LookAtBone
        {

            public Transform bone { get; private set; }
            private Quaternion lastQuaternion = new Quaternion();
            private Quaternion cachedQuaternion = new Quaternion();
            public Tools.EaseBlend easeBlend = new Tools.EaseBlend();
            private float lastAngle = 0.0f;

            public LookAtBone(Transform _bone)
            {
                bone = _bone;
                if (bone == null)
                {
                    Debug.LogError("[LOOKATBONE] LookAt Bone is null!");
                }
            }
            public void lookAtHead(Vector3 targetPos)
            {
                easeBlend.Update(Time.deltaTime);
                if (easeBlend.value != 0)
                {

                    Vector3 eyesToTarget = targetPos - bone.transform.position - bone.transform.up * 0.05f;

                    //rotate neck first
                    Vector3 neckEuler = bone.parent.localEulerAngles;
                    float pitchOffset = Mathf.Atan2(-eyesToTarget.y, Mathf.Sqrt(eyesToTarget.x * eyesToTarget.x + eyesToTarget.z * eyesToTarget.z)) * Mathf.Rad2Deg;
                    neckEuler.x += Mathf.Clamp(pitchOffset * 0.25f, -20.0f, 20.0f);
                    float smoothHeadBlend = Tools.EaseInOutCubic(easeBlend.value);
                    bone.parent.localEulerAngles = Vector3.LerpUnclamped(bone.parent.localEulerAngles, neckEuler, smoothHeadBlend);

                    Vector3 localEuler = bone.transform.localEulerAngles;
                    cachedQuaternion = bone.transform.rotation;
                    Quaternion lookAt = Quaternion.LookRotation(eyesToTarget);
                    bone.transform.localRotation = Quaternion.Euler(-25.0f, 0.0f, 0.0f);
                    lookAt = Quaternion.RotateTowards(bone.transform.rotation, lookAt, 40.0f);

                    localEuler.x = 0.0f;
                    lookAt *= Quaternion.Euler(localEuler);

                    lastQuaternion = Quaternion.LerpUnclamped(lastQuaternion, lookAt, Mathf.Clamp(8.0f * Time.deltaTime, 0.0f, 1.0f));
                    bone.rotation = Quaternion.LerpUnclamped(cachedQuaternion, lastQuaternion, smoothHeadBlend * 0.7f);
                }
            }
            public void lookAtSpine2(Vector3 targetPos, float maxAngle)
            {
                easeBlend.Update(Time.deltaTime);
                if (easeBlend.value != 0)
                {

                    float rootAngle = Mathf.Atan2(bone.forward.x, bone.forward.z) * Mathf.Rad2Deg;

                    cachedQuaternion = bone.rotation;
                    float angle = Mathf.Atan2(targetPos.x - bone.position.x, targetPos.z - bone.position.z) * Mathf.Rad2Deg;
                    float angleDiff = Mathf.Clamp(Mathf.DeltaAngle(rootAngle, angle), -maxAngle, maxAngle);
                    lastAngle = Mathf.LerpAngle(lastAngle, angleDiff, 2.5f * (float)Time.deltaTime);
                    Quaternion lookAt = cachedQuaternion * Quaternion.Euler(0.0f, lastAngle, 0.0f);
                    bone.rotation = Quaternion.LerpUnclamped(bone.rotation, lookAt, Tools.EaseInOutCubic(easeBlend.value));
                }
            }
            public void LookAtEyeball(Vector3 targetPos, Vector3 headUp)
            {
                easeBlend.Update(Time.deltaTime);
                if (easeBlend.value != 0)
                {
                    cachedQuaternion = Quaternion.LookRotation(bone.up, -headUp) * Quaternion.Euler(90.0f, 0.0f, 0.0f);
                    bone.rotation = Quaternion.LookRotation(targetPos - bone.position, -headUp) * Quaternion.Euler(90.0f, 0.0f, 0.0f);
                    bone.rotation = Quaternion.LerpUnclamped(cachedQuaternion, bone.rotation, easeBlend.value);
                }
            }
        }

        private bool ViewResultsContains(Item item)
        {
            foreach (ViewItem viewItem in viewItems.objects)
            {
                if (viewItem.item == item)
                {
                    return true;
                }
            }
            return false;
        }

        private Item FindClosestItemInViewResults(Item.Type itemType)
        {

            Item result = null;
            float lowestSqDist = Mathf.Infinity;
            foreach (ViewItem viewItem in viewItems.objects)
            {
                Item item = viewItem.item;
                if (item.settings.itemType != itemType)
                {
                    continue;
                }
                float sqDist = Vector3.SqrMagnitude(item.transform.position - transform.position);
                if (sqDist < lowestSqDist)
                {
                    lowestSqDist = sqDist;
                    result = item;
                }
            }
            return result;
        }

        private void OnEnterViewItem(Item item)
        {
            //ignore certain body items
            if (item.mainOwner == this)
            {
                switch (item.settings.itemType)
                {
                    case Item.Type.CHARACTER:
                    case Item.Type.CHARACTER_HAIR:
                        return;
                }
            }
            bool found = false;
            foreach (ViewItem viewItem in viewItems.objects)
            {
                if (viewItem.item == item)
                {
                    viewItem.outOfViewTimer = 0.0f;
                    found = true;
                }
            }
            if (!found)
            {
                inViewButBlocked.Add(item);
            }
        }

        private void OnExitViewItem(Item item)
        {
            bool found = false;
            foreach (ViewItem viewItem in viewItems.objects)
            {
                if (viewItem.item == item)
                {
                    viewItem.outOfViewTimer = Mathf.Epsilon;    //begin out of view timer
                    found = true;
                }
            }
            if (!found)
            {
                inViewButBlocked.Remove(item);
            }
        }

        private Item FindMostInterestingLookAtItem(float minimumInterest)
        {

            Item result = null;
            //find object with the most motion and closest to head
            for (int i = 0; i < viewItems.objects.Count; i++)
            {
                Item viewItem = viewItems.objects[i].item;
                if (viewItem.HasAttribute(Item.Attributes.DO_NOT_LOOK_AT))
                {
                    continue;
                }
                if (viewItem.mainOwner == this)
                {
                    continue;
                }
                if (viewItem.rigidBody == null)
                {
                    continue;
                }
                float interest = viewItem.rigidBody.velocity.sqrMagnitude;
                if (interest >= minimumInterest)
                {
                    minimumInterest = interest;
                    result = viewItem;
                }
            }
            return result;
        }


        private void FixedUpdateViewAwareness()
        {

            UpdateViewTimers();

            randomViewTimer -= Time.deltaTime;
            switch (awarenessMode)
            {
                case AwarenessMode.NORMAL:
                    CheckNormalViewAwareness();
                    break;
                case AwarenessMode.TIRED:
                    CheckTiredViewAwareness();
                    break;
            }
        }

        private void CheckNormalViewAwareness()
        {

            //do any switching only after a full look at blend
            if (changeLookAtTargetBlend != 1.0f)
            {
                return;
            }
            randomViewTimer -= Time.deltaTime;
            if (randomViewTimer > 0.0f)
            {
                return;
            }
            //switch to any other interesting items
            if (currentLookAtItem != null && currentLookAtItem.rigidBody)
            {

                //change look at target if something of higher motion is found
                Item mostInterestingItem = FindMostInterestingLookAtItem(currentLookAtItem.rigidBody.velocity.sqrMagnitude + 0.1f);
                if (mostInterestingItem != null)
                {
                    SetLookAtTarget(mostInterestingItem.transform);
                }
                else
                {
                    if (currentLookAtItem.rigidBody.velocity.sqrMagnitude <= 0.1f)
                    {

                        if (randomViewTimer < 0.0f)
                        {
                            if (Random.value < 0.2f)
                            {
                                randomViewTimer = 0.3f + Random.value * 0.5f;
                            }
                            else
                            {
                                randomViewTimer = 1.0f + Random.value * 1.0f;
                            }
                        }
                    }
                }

            }
            else
            {   //if not looking at anything, find most interesting in view
                Item mostInterestingItem = FindMostInterestingLookAtItem(0.0f);
                //if nothing still, choose random
                if (mostInterestingItem == null)
                {
                    mostInterestingItem = GetRandomItemInView();
                }
                if (mostInterestingItem != null)
                {
                    SetLookAtTarget(mostInterestingItem.transform);
                }
            }
        }

        private void CheckTiredViewAwareness()
        {
            //do any switching only after a full look at blend
            if (changeLookAtTargetBlend != 1.0f)
            {
                return;
            }
            randomViewTimer -= Time.deltaTime;
            if (randomViewTimer < 0.0f)
            {
                if (awarenessTiredViewRest)
                {
                    awarenessTiredViewRest = false;
                    SetLookAtTarget(null);
                    randomViewTimer = 1.5f + Random.value * 3.5f;
                }
                else
                {   //look at something
                    Item mostInterestingItem = FindMostInterestingLookAtItem(0.0f);
                    //if nothing still, choose random
                    if (mostInterestingItem == null)
                    {
                        mostInterestingItem = GetRandomItemInView();
                    }
                    if (mostInterestingItem != null)
                    {
                        SetLookAtTarget(mostInterestingItem.transform);
                        randomViewTimer = 1.0f + Random.value * 3.0f;
                        awarenessTiredViewRest = true;  //rest next time
                    }
                    else
                    {
                        randomViewTimer = 0.4f; //check for another object again after timer
                    }
                }
            }
        }

        private Item GetRandomItemInView()
        {
            if (viewItems.objects.Count == 0)
            {
                return null;
            }
            int randomIndex = (int)(viewItems.objects.Count * Random.value) % viewItems.objects.Count;
            return viewItems.objects[randomIndex].item;
        }

        public Item FindViewItemOfType(Item.Type type)
        {
            foreach (ViewItem viewItem in viewItems.objects)
            {
                if (viewItem.item.settings.itemType == type)
                {
                    return viewItem.item;
                }
            }
            return null;
        }

        public bool CanSeePoint(Vector3 position)
        {
            if (Mathf.Abs(Tools.Bearing(headLookAt.bone, position)) > 60.0f)
            {
                return false;
            }
            Vector3 dir = position - headLookAt.bone.position;
            float maxDistance = dir.magnitude;
            dir /= maxDistance;
            return !Physics.Raycast(headLookAt.bone.position, dir, maxDistance, WorldUtil.wallsMask);
        }

        public void SetLookAtTarget(Transform newLookAtTransform, float speedMult = 1.0f)
        {
            if (m_currentLookAtTransform == newLookAtTransform)
            {
                return;
            }
            lastLookAtTransform = m_currentLookAtTransform;
            if (lastLookAtTransform == null)
            {
                oldLookAtTargetDiffNorm = headLookAt.bone.forward;
                oldLookAtTargetRadius = 1.0f;
            }
            else
            {
                oldLookAtTargetDiffNorm = lastLookAtTransform.transform.position - headLookAt.bone.position;
                oldLookAtTargetRadius = oldLookAtTargetDiffNorm.magnitude;
                oldLookAtTargetDiffNorm /= oldLookAtTargetRadius;
            }

            m_currentLookAtTransform = newLookAtTransform;
            if (newLookAtTransform == null)
            {
                lookAtTargetPos = headLookAt.bone.position + transform.forward;
                currentLookAtItem = null;
            }
            else
            {
                lookAtTargetPos = m_currentLookAtTransform.position;
                currentLookAtItem = newLookAtTransform.GetComponent(typeof(Item)) as Item;
            }

            //prevent division by zero
            if (lookAtTargetPos == headLookAt.bone.position)
            {
                lookAtTargetPos = headLookAt.bone.position + headLookAt.bone.forward;
            }
            randomEyeOffsetMult = 1.0f + Random.value * 3.0f;
            Vector3 lookAtTargetDiffNorm = lookAtTargetPos - headLookAt.bone.position;
            lookAtTargetDiffNorm /= lookAtTargetDiffNorm.magnitude;
            float angleDiff = Quaternion.Angle(Quaternion.LookRotation(oldLookAtTargetDiffNorm), Quaternion.LookRotation(lookAtTargetDiffNorm));
            changeLookAtSpeed = Mathf.Min((90.0f) / angleDiff, 7.0f) * speedMult;
            changeLookAtTargetBlend = 0.0f;
            RandomChanceBlink();
        }

        public void SetViewAwarenessTimeout(float timer)
        {
            randomViewTimer = timer;
        }

        private void RebindLookAtLogic()
        {
            headLookAt = new LookAtBone(head);
            spine2LookAt = new LookAtBone(spine2);

            rightEye = new Eye();
            rightEye.lookAt = new LookAtBone(pupil_r);
            rightEye.animationBone = eyeball_r;

            leftEye = new Eye();
            leftEye.lookAt = new LookAtBone(pupil_l);
            leftEye.animationBone = eyeball_l;

            pupilShrinkBS = headSMR.sharedMesh.GetBlendShapeIndex("pupilShrink");
            upperEyelidRightBS = headSMR.sharedMesh.GetBlendShapeIndex("upperEyelid_r");
            upperEyelidLeftBS = headSMR.sharedMesh.GetBlendShapeIndex("upperEyelid_l");
            lowerEyelidRightBS = headSMR.sharedMesh.GetBlendShapeIndex("lowerEyelid_r");
            lowerEyelidLeftBS = headSMR.sharedMesh.GetBlendShapeIndex("lowerEyelid_l");
            madEyelidRightBS = headSMR.sharedMesh.GetBlendShapeIndex("madEye_r");
            madEyelidLeftBS = headSMR.sharedMesh.GetBlendShapeIndex("madEye_l");

            for (int i = 0; i < headSMR.materials.Length; i++)
            {
                string name = headSMR.materials[i].name;
                if (name.StartsWith("pupil_r"))
                {
                    rightEye.material = headSMR.materials[i];
                }
                else if (name.StartsWith("pupil_l"))
                {
                    leftEye.material = headSMR.materials[i];
                }
            }
            ValidateEyes();
            if (!rightEye.valid || !leftEye.valid)
            {
                Debug.Log("ERROR One or more eyes are not valid!");
            }

            //make upper Eyelid animations moved less if lower Eyelids dont exist
            if (lowerEyelidRightBS != -1 && lowerEyelidLeftBS != -1)
            {
                upperEyelidRate = 1.0f;
            }
            else
            {
                upperEyelidRate = 0.7f;
            }
        }

        public void ValidateEyes()
        {
            ValidateEye(rightEye, -1.0f);
            ValidateEye(leftEye, 1.0f);
        }

        private void ValidateEye(Eye eye, float sideMultiplier)
        {
            bool hasBone = eye.lookAt.bone != null;
            bool hasMaterial = eye.material != null;
            bool hasTexture;
            if (hasMaterial)
            {
                hasTexture = eye.material.mainTexture != null;
                eye.material.SetFloat(WorldUtil.sideMultiplierID, sideMultiplier);
            }
            else
            {
                hasTexture = false;
            }
            bool hasAnimationBone = eye.animationBone != null;
            if (hasBone && hasMaterial && hasTexture && hasAnimationBone)
            {
                eye.valid = true;
            }
            else
            {
                eye.valid = false;
            }
        }

        Vector3 CalculateRealTargetLookAtPos(Vector3 targetLookAtPos)
        {
            changeLookAtTargetBlend = Mathf.Min(changeLookAtTargetBlend + Time.deltaTime * changeLookAtSpeed, 1.0f);

            Vector3 lookAtTargetDiffNorm = targetLookAtPos - headLookAt.bone.position;
            float lookAtTargetRadius = lookAtTargetDiffNorm.magnitude;
            lookAtTargetDiffNorm /= lookAtTargetRadius;

            float realBlend = Tools.EaseInOutCubic(changeLookAtTargetBlend);
            Vector3 changeDiffNorm = Vector3.LerpUnclamped(oldLookAtTargetDiffNorm, lookAtTargetDiffNorm, realBlend);
            changeDiffNorm /= changeDiffNorm.magnitude;

            return headLookAt.bone.position + changeDiffNorm * Mathf.LerpUnclamped(oldLookAtTargetRadius, lookAtTargetRadius, realBlend);
        }

        private void UpdateRandomEyeMovements()
        {
            randomEyeTimer += Time.deltaTime;
            if (randomEyeTimer > randomEyeTimerWait)
            {
                randomEyeTimer = 0.0f;
                randomEyeTimerWait = 0.25f + (0.15f + Random.value * 1.25f) / randomEyeOffsetMult;

                randomEyeOffsetMult += (1.0f - randomEyeOffsetMult) * Time.deltaTime * 15.0f;
                targetRandomEyeoffset.x = (-0.5f + Random.value) * 0.125f * randomEyeOffsetMult;
                targetRandomEyeoffset.y = (-0.5f + Random.value) * 0.125f * randomEyeOffsetMult;

                RandomChanceBlink();
            }
            randomEyeOffset.x += (targetRandomEyeoffset.x - randomEyeOffset.x) * Time.deltaTime * 15.0f;
            randomEyeOffset.y += (targetRandomEyeoffset.y - randomEyeOffset.y) * Time.deltaTime * 15.0f;
        }
        private void RandomChanceBlink()
        {
            if (blink < 0.1f && Random.value < 0.15f)
            {
                blink = 1.0f;
            }
        }
        private void UpdateFollowEyelids()
        {
            eyelidFollowRealBlend += (eyelidFollowTargetBlend - eyelidFollowRealBlend) * Time.deltaTime * 3.0f;
            if (eyelidFollowRealBlend == 0)
            {
                return;
            }
            float madEyeMax = (1.0f - Mathf.Clamp(currAvgPupilDown * eyelidFollowRealBlend, 0.0f, 0.6f)) * 100.0f;
            SafeSetBlendShapeWeight(madEyelidRightBS, Mathf.Min(SafeGetBlendShapeWeight(madEyelidRightBS), madEyeMax));
            SafeSetBlendShapeWeight(madEyelidLeftBS, Mathf.Min(SafeGetBlendShapeWeight(madEyelidLeftBS), madEyeMax));
            float madEyeRatioFix = 1.0f - Mathf.Max(SafeGetBlendShapeWeight(madEyelidRightBS), SafeGetBlendShapeWeight(madEyelidLeftBS)) / 100.0f;
            float upperEyelidAdd = (-200.0f * currAvgPupilDown) * madEyeRatioFix * eyelidFollowRealBlend * upperEyelidRate;
            float lowerEyelidAdd = (200.0f * currAvgPupilDown) * madEyeRatioFix * eyelidFollowRealBlend;

            SafeSetBlendShapeWeight(upperEyelidRightBS, Mathf.Clamp(SafeGetBlendShapeWeight(upperEyelidRightBS) + upperEyelidAdd, 0.0f, 100.0f));
            SafeSetBlendShapeWeight(upperEyelidLeftBS, Mathf.Clamp(SafeGetBlendShapeWeight(upperEyelidLeftBS) + upperEyelidAdd, 0.0f, 100.0f));
            SafeSetBlendShapeWeight(lowerEyelidRightBS, Mathf.Clamp(SafeGetBlendShapeWeight(lowerEyelidRightBS) + lowerEyelidAdd, 0.0f, 100.0f));
            SafeSetBlendShapeWeight(lowerEyelidLeftBS, Mathf.Clamp(SafeGetBlendShapeWeight(lowerEyelidLeftBS) + lowerEyelidAdd, 0.0f, 100.0f));
        }

        private void UpdateBlinkEyes()
        {
            SafeSetBlendShapeWeight(upperEyelidRightBS, Mathf.LerpUnclamped(SafeGetBlendShapeWeight(upperEyelidRightBS), 100.0f, blink));
            SafeSetBlendShapeWeight(upperEyelidLeftBS, Mathf.LerpUnclamped(SafeGetBlendShapeWeight(upperEyelidLeftBS), 100.0f, blink));
            SafeSetBlendShapeWeight(lowerEyelidRightBS, Mathf.LerpUnclamped(SafeGetBlendShapeWeight(lowerEyelidRightBS), 100.0f, blink));
            SafeSetBlendShapeWeight(lowerEyelidLeftBS, Mathf.LerpUnclamped(SafeGetBlendShapeWeight(lowerEyelidLeftBS), 100.0f, blink));
            SafeSetBlendShapeWeight(madEyelidRightBS, SafeGetBlendShapeWeight(madEyelidRightBS) * (1.0f - blink));
            SafeSetBlendShapeWeight(madEyelidLeftBS, SafeGetBlendShapeWeight(madEyelidLeftBS) * (1.0f - blink));
            blink = Mathf.Max(0.0f, blink * (1.0f - Time.deltaTime * 8.0f));
        }


        public void FixedUpdateLookAtLogic()
        {
            if (m_currentLookAtTransform == null)
            {
                lookAtTargetPos = headLookAt.bone.position + headLookAt.bone.forward;
            }
            else
            {
                if (!m_currentLookAtTransform.gameObject.activeSelf)
                {
                    SetLookAtTarget(null);
                    lookAtTargetPos = headLookAt.bone.position + headLookAt.bone.forward;
                }
                else
                {
                    lookAtTargetPos = m_currentLookAtTransform.transform.position;
                }
            }

            Vector3 realTargetLookAtPos = CalculateRealTargetLookAtPos(lookAtTargetPos);
            spine2LookAt.lookAtSpine2(realTargetLookAtPos, 20.0f);
            headLookAt.lookAtHead(realTargetLookAtPos + lookAtHeadOffset);
        }

        private void UpdateEyes()
        {
            UpdateRandomEyeMovements();

            Vector3 realTargetLookAtPos = CalculateRealTargetLookAtPos(lookAtTargetPos);
            if (rightEye.valid)
            {
                rightEye.lookAt.bone.rotation = rightEye.animationBone.rotation;
                rightEye.lookAt.LookAtEyeball(realTargetLookAtPos, -headLookAt.bone.up);
            }
            if (leftEye.valid)
            {
                leftEye.lookAt.bone.rotation = leftEye.animationBone.rotation;
                leftEye.lookAt.LookAtEyeball(realTargetLookAtPos, -headLookAt.bone.up);
            }

            UpdatePupilTargetLookAt();
            UpdateFollowEyelids();

            if ((eyeFlags & (int)EyeLogicFlags.BLINK) != 0)
            {
                UpdateBlinkEyes();
            }
            else
            {
                blink *= 0.7f;
            }

            UpdatePupilShrinkUniforms();
        }

        private void UpdatePupilShrinkUniforms()
        {
            float pupilShrink = SafeGetBlendShapeWeight(pupilShrinkBS) / 100.0f + 1.0f;
            //scale so it is NOT full strength
            pupilShrink = 1.0f + (pupilShrink - 1.0f) * 0.5f;
            rightEye.material.SetFloat(WorldUtil.pupilShrinkID, pupilShrink);
            leftEye.material.SetFloat(WorldUtil.pupilShrinkID, pupilShrink);
        }

        public void ResetEyeUniforms()
        {

            ResetEyeUniform(rightEye);
            ResetEyeUniform(leftEye);
        }

        private void ResetEyeUniform(Eye eye)
        {

            eye.material.SetFloat(WorldUtil.pupilShrinkID, 1.0f);
            eye.material.SetFloat(WorldUtil.pupilRightID, 0.0f);
            eye.material.SetFloat(WorldUtil.pupilUpID, 0.0f);
        }

        private float SafeGetBlendShapeWeight(int id)
        {
            if (id == -1)
            {
                return 0.0f;
            }
            else
            {
                return headSMR.GetBlendShapeWeight(id);
            }
        }

        private void SafeSetBlendShapeWeight(int id, float amount)
        {
            if (id != -1)
            {
                headSMR.SetBlendShapeWeight(id, amount);
            }
        }

        public void SetEyeRotations(Vector3 rightEyeDir, Vector3 leftEyeDir)
        {


            float madEyelid = (SafeGetBlendShapeWeight(madEyelidRightBS) + SafeGetBlendShapeWeight(madEyelidLeftBS)) / 200.0f;
            float rightPupilNormYaw = rightEyeDir.x;
            // rightPupilNormYaw -= madEyelid*0.15f;
            float leftPupilNormYaw = leftEyeDir.x;
            // leftPupilNormYaw += madEyelid*0.15f;

            if (leftPupilNormYaw > -2.0f && rightPupilNormYaw < 2.0f)
            {
                lastRightPupilNormYaw = rightPupilNormYaw;
                lastLeftPupilNormYaw = leftPupilNormYaw;
            }

            float pupilShrink = SafeGetBlendShapeWeight(pupilShrinkBS);
            Vector2 rightPupilInfo = GetPupilShaderLookAt(lastRightPupilNormYaw, rightEyeDir.y, 1.0f, pupilShrink);
            rightPupilInfo.y = GetClampedPupilPitch(rightPupilInfo.y, madEyelid);
            Vector2 leftPupilInfo = GetPupilShaderLookAt(lastLeftPupilNormYaw, leftEyeDir.y, -1.0f, pupilShrink);
            leftPupilInfo.y = GetClampedPupilPitch(leftPupilInfo.y, madEyelid);

            //symmetrize
            currAvgPupilDown = (rightPupilInfo.y + leftPupilInfo.y) * 0.5f + 0.05f;

            rightEye.material.SetFloat(WorldUtil.pupilShrinkID, 1.0f + pupilShrink);
            rightEye.material.SetFloat(WorldUtil.pupilRightID, -rightPupilInfo.x);
            rightEye.material.SetFloat(WorldUtil.pupilUpID, currAvgPupilDown);

            leftEye.material.SetFloat(WorldUtil.pupilShrinkID, 1.0f + pupilShrink);
            leftEye.material.SetFloat(WorldUtil.pupilRightID, leftPupilInfo.x);
            leftEye.material.SetFloat(WorldUtil.pupilUpID, currAvgPupilDown);
        }

        private void UpdatePupilTargetLookAt()
        {

            if (!rightEye.valid || !leftEye.valid)
            {
                return;
            }
            Vector3 rightEyeDir = headLookAt.bone.InverseTransformDirection(rightEye.lookAt.bone.up);
            Vector3 leftEyeDir = headLookAt.bone.InverseTransformDirection(leftEye.lookAt.bone.up);

            //add random looks
            rightEyeDir.x += randomEyeOffset.x;
            rightEyeDir.y += randomEyeOffset.y;
            leftEyeDir.x += randomEyeOffset.x;
            leftEyeDir.y += randomEyeOffset.y;

            SetEyeRotations(rightEyeDir, leftEyeDir);
        }

        public Vector2 GetPupilShaderLookAt(float normYaw, float normalizedPitch, float side, float pupilShrink)
        {

            Vector2 pupilOffset = Vector2.zero;
            pupilOffset.x = normYaw - headModel.pupilOffset.x * side;
            pupilOffset.y = normalizedPitch + headModel.pupilOffset.y;
            //clamp pupils within radius of circle
            float pupilOffsetLength = pupilOffset.magnitude;
            float eyeSocketRadius = headModel.pupilSpanRadius;//+pupilShrink*0.2f;	//eye socket radius increases with mad blendshape
            if (pupilOffsetLength > eyeSocketRadius)
            {
                Vector2 unit = pupilOffset / pupilOffsetLength;
                pupilOffset.x = unit.x * eyeSocketRadius;
                pupilOffset.y = unit.y * eyeSocketRadius;
            }
            pupilOffset.x += headModel.pupilOffset.x * side;
            pupilOffset.y -= headModel.pupilOffset.y;
            return pupilOffset;
        }

        private float PupilStretchNormalizedEuler(float val)
        {
            return (1.0f - Mathf.Pow(1.0f - Mathf.Abs(val), 3.0f)) * Mathf.Sign(val);
        }

        private float GetClampedPupilPitch(float normalizedPitch, float madEyelid)
        {   //pitch is div by 90.0f

            //mad eyelid clamps always
            normalizedPitch = Mathf.Clamp(normalizedPitch, -0.6f + madEyelid * 0.2f, 1.0f - madEyelid * 0.8f);

            float upperEyelid = (SafeGetBlendShapeWeight(upperEyelidRightBS) + SafeGetBlendShapeWeight(upperEyelidLeftBS)) / 200.0f;
            float lowerEyelid = (SafeGetBlendShapeWeight(lowerEyelidRightBS) + SafeGetBlendShapeWeight(lowerEyelidLeftBS)) / 200.0f;
            upperEyelid *= (1.0f - blink) * (1.0f - madEyelid);
            lowerEyelid *= (1.0f - blink) * lowerEyelid * (1.0f - madEyelid);
            //clamp with lower eyelid
            normalizedPitch = Mathf.Max(-0.4f + lowerEyelid * 0.25f, normalizedPitch);
            //clamp with upper eyelid
            normalizedPitch = Mathf.Min(0.6f - Mathf.Min(1.0f, upperEyelid) * 0.85f, normalizedPitch);
            return normalizedPitch;
        }

    }

}