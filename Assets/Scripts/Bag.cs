using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace viva
{


    public class Bag : Item
    {

        public class BagItemCategory
        {

            private Texture2D m_icon;
            public Texture2D icon { get { return m_icon; } }
            public readonly Item.Type type;
            public readonly List<Item> items = new List<Item>();

            public BagItemCategory(Item.Type _type)
            {
                type = _type;
            }

            public void OnDestroy(Bag parentBag)
            {
                foreach (Item item in items)
                {
                    item.transform.position = parentBag.transform.position;
                    item.gameObject.SetActive(true);
                }
                if (m_icon != null)
                {
                    Destroy(m_icon);
                }
            }

            public void RenderIcon(RenderTexture renderTexture)
            {

                if (m_icon != null)
                {
                    Destroy(m_icon);
                }
                Item item = items[0];
                Camera camera = GameDirector.instance.utilityCamera;
                camera.gameObject.SetActive(true);
                camera.cullingMask = WorldUtil.itemsMask;
                camera.fieldOfView = 40;

                //setup item gameobject settings
                item.gameObject.SetActive(true);
                item.transform.position = Vector3.up * 1000.0f; //place far above to avoid rendering other items
                GameDirector.skyDirector.OverrideDayNightCycleLighting(GameDirector.skyDirector.defaultDayNightPhase, Quaternion.Euler(45.0f, 45.0f, 0.0f));

                Bounds itemBounds = Tools.CalculateCenterAndBoundingHeight(item.gameObject, 0.05f);
                item.transform.rotation = Quaternion.Euler(item.settings.iconEulerAngles);
                camera.transform.position = itemBounds.center - Vector3.forward * itemBounds.extents.magnitude * 2.0f;
                camera.transform.rotation = Quaternion.LookRotation(item.transform.position - camera.transform.position, Vector3.up);

                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture old = RenderTexture.active;
                RenderTexture.active = renderTexture;

                Texture2D newIcon = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false, true);
                newIcon.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0, false);
                newIcon.Apply(false, true);

                RenderTexture.active = old;

                m_icon = newIcon;

                //restore item gameobject settings
                GameDirector.skyDirector.RestoreDayNightCycleLighting();
                camera.gameObject.SetActive(false);
                item.gameObject.SetActive(false);
            }
        }

        private bool? flagAttachAfterIK = null;
        private bool isOnShoulder = false;
        private TransformBlend handWearBlend = new TransformBlend();

        [SerializeField]
        public RootAnimationOffset shoulderWearOffset;
        [SerializeField]
        public IKAnimationTarget rightArmIKOverrideAnimation;
        [SerializeField]
        public IKAnimationTarget leftArmIKOverrideAnimation;
        [SerializeField]
        public Vector3 armRotateWhileWearing;
        [SerializeField]
        public Vector3 wearOffsetEuler = Vector3.zero;
        [SerializeField]
        private SkinnedMeshRenderer smr;
        [SerializeField]
        private int maxItems = 3;
        [SerializeField]
        private AudioClip storeItemSound;
        [SerializeField]
        private BagUIMenu bagUI;
        [HideInInspector]
        [SerializeField]
        private VivaSessionAsset[] m_storedItemsManifest = null;
        [VivaFileAttribute]
        public VivaSessionAsset[] storedItemsManifest { get { return m_storedItemsManifest; } protected set { m_storedItemsManifest = value; } }
        [SerializeField]
        private BagItemDetector bagItemDetector;

        private BagItemCategory nextItemTakeOutTarget = null;
        private float waitToWearTimer = 0.0f;

        private List<BagItemCategory> m_storedItems = new List<BagItemCategory>();
        public List<BagItemCategory> storedItems { get { return m_storedItems; } }

        public enum PhotoSummary
        {
            GENERIC,
            PANTY
        }

        public override void Save(GameDirector.VivaFile vivaFile)
        {
            //build storedItemsManifest for saving
            List<VivaSessionAsset> tempManifestList = new List<VivaSessionAsset>();
            foreach (var category in storedItems)
            {
                foreach (VivaSessionAsset asset in category.items)
                {
                    tempManifestList.Add(asset);
                }
            }
            storedItemsManifest = tempManifestList.ToArray();
            base.Save(vivaFile);
        }

        protected override void OnItemAwake()
        {
            base.OnItemAwake();

            bagUI.Initialize();
            if (storedItemsManifest != null)
            {
                foreach (VivaSessionAsset asset in storedItemsManifest)
                {
                    Item item = asset as Item;
                    if (item == null)
                    {
                        Debug.LogError("[BAG] Could not store item on load");
                        break;
                    }
                    StoreItem(item);
                }
            }
            storedItemsManifest = null; //clear startup manifest
        }

        public override void OnItemLateUpdate()
        {

            if (mainOwner == null)
            {
                return;
            }
            switch (mainOwner.characterType)
            {
                case Character.Type.LOLI:
                    LateUpdateLoliUsage(mainOwner as Loli);
                    break;
                case Character.Type.PLAYER:
                    LateUpdatePlayerUsage(mainOwner as Player);
                    break;
            }
        }

        public void SetWaitToWearTimer(float time)
        {
            waitToWearTimer = time;
        }

        private void SetUIFacingOwner()
        {
            if (mainOccupyState == null)
            {
                return;
            }
            Vector3 uiSide = new Vector3(0.125f, 0.0f, 0.0f);
            if (!mainOccupyState.rightSide)
            {
                uiSide.x *= -1.0f;
            }

            bagUI.transform.localPosition = uiSide;
            bool useVROrientation = false;
            if (mainOwner && mainOwner.characterType == Character.Type.PLAYER && (mainOwner as Player).controls == Player.ControlType.VR)
            {
                useVROrientation = true;
            }
            if (useVROrientation)
            {
                bagUI.transform.rotation = Quaternion.LookRotation(bagUI.transform.position - mainOwner.head.position, Vector3.up);
            }
            else
            {
                bagUI.transform.rotation = Quaternion.LookRotation(bagUI.transform.position - mainOwner.head.position, mainOwner.head.up);
            }
        }

        private void LateUpdatePlayerUsage(Player player)
        {

            PlayerHandState handstate = player.FindOccupyStateByHeldItem(this) as PlayerHandState;
            if (handstate == null)
            {
                return;
            }
            PlayerHandState otherHandState = handstate.otherPlayerHandState;
            Item targetItem = otherHandState.heldItem;

            //can't put bags inside bags
            if (!CanStoreItem(targetItem))
            {
                targetItem = null;
            }
            if (handstate.actionState.isDown && !GameDirector.instance.IsAnyUIMenuActive())
            {
                handstate.actionState.Consume();
                if (targetItem != null)
                {
                    //place targetItem in bag
                    if (handstate.rightSide)
                    {
                        handstate.animSys.SetTargetAnimation(Player.Animation.BAG_PLACE_INSIDE_RIGHT);
                        otherHandState.animSys.SetTargetAnimation(Player.Animation.BAG_PLACE_INSIDE_RIGHT);
                    }
                    else
                    {
                        handstate.animSys.SetTargetAnimation(Player.Animation.BAG_PLACE_INSIDE_LEFT);
                        otherHandState.animSys.SetTargetAnimation(Player.Animation.BAG_PLACE_INSIDE_LEFT);
                    }
                }
                else
                {
                    //open bag inventory UI
                    GameDirector.instance.BeginUIInput(bagUI, player, otherHandState.rightSide);
                }
            }
        }

        public bool CanStoreItem(Item targetItem)
        {
            if (targetItem == null)
            {
                return false;
            }
            switch (targetItem.settings.itemType)
            {
                case Item.Type.BAG:
                case Item.Type.MIXING_BOWL:
                case Item.Type.MORTAR:
                case Item.Type.PESTLE:
                case Item.Type.TOWEL:
                case Item.Type.POT:
                case Item.Type.KNIFE:
                case Item.Type.MIXING_SPOON:
                case Item.Type.LID:
                    return false;
            }
            //can only store items that are parentable
            if (!targetItem.settings.allowChangeOwner)
            {
                return false;
            }
            return true;
        }

        private void LateUpdateLoliUsage(Loli loli)
        {

            if (isOnShoulder)
            {
                return;
            }

            waitToWearTimer -= Time.deltaTime;
            if (waitToWearTimer > 0.0f)
            {
                return;
            }

            //ensure bag is fully parented before triggering animation so blends look right
            // if( !loli.active.IsTaskActive( loli.active.give ) ){
            // 	if( loli.leftHandState.heldItem == this && !loli.rightShoulderState.occupied ){

            // 	 	if( loli.leftHandState.finishedBlending ){
            // 			loli.SetTargetAnimation( Loli.Animation.STAND_WEAR_BAG_RIGHT );
            // 		}
            // 	}else if( !loli.leftShoulderState.occupied ){
            // 		if( loli.rightHandState.finishedBlending ){
            // 			loli.SetTargetAnimation( Loli.Animation.STAND_WEAR_BAG_LEFT );
            // 		}
            // 	}
            // }
        }

        public override bool ShouldPickupWithRightHand(Character source)
        {
            Loli loli = source as Loli;
            if (loli == null)
            {
                return base.ShouldPickupWithRightHand(source);
            }
            if (loli.rightShoulderState.occupied)
            {
                return true;
            }
            else if (loli.leftShoulderState.occupied)
            {
                return false;
            }
            else
            {
                return base.ShouldPickupWithRightHand(source);
            }
        }

        public void FlagWearOnLoliShoulder(bool side)
        {
            flagAttachAfterIK = side;
        }

        private void InitializeWearOnLoliShoulder(bool rightShoulder)
        {

            Loli loli = mainOwner as Loli;
            if (loli == null)
            {
                return;
            }

            ShoulderState targetShoulderState;
            IKAnimationTarget armIKOverrideAnimation;
            if (rightShoulder)
            {
                targetShoulderState = loli.rightShoulderState;
                armIKOverrideAnimation = rightArmIKOverrideAnimation;
            }
            else
            {
                targetShoulderState = loli.leftShoulderState;
                armIKOverrideAnimation = leftArmIKOverrideAnimation;
            }
            targetShoulderState.Pickup(
                this,
                ShoulderWearCallback,
                HoldType.OBJECT,
                shoulderWearOffset,
                armIKOverrideAnimation,
                1.0f
            );
            isOnShoulder = rightShoulder;
        }

        public override void OnItemLateUpdatePostIK()
        {
            if (flagAttachAfterIK.HasValue)
            {
                InitializeWearOnLoliShoulder(flagAttachAfterIK.Value);
                flagAttachAfterIK = null;
            }
            ApplyHangOnHand();
            SetUIFacingOwner();
            bagItemDetector.UpdateDetectItem();
        }

        private void ApplyHangOnHand()
        {
            if (mainOwner == null)
            {
                return;
            }
            HandState handState = mainOwner.FindOccupyStateByHeldItem(this) as HandState;
            if (handState == null)
            {
                return;
            }
            Quaternion restRotation = Quaternion.LookRotation(Vector3.down, handState.fingerAnimator.targetBone.up);
            // handWearBlend.Blend( transform.localPosition, restRotation*Quaternion.Euler( wearOffsetEuler ) );
        }
        public void ShoulderWearCallback(OccupyState source, Item oldItem, Item newItem)
        {
        }

        public override void OnPreDrop()
        {
            ApplyHangOnHand();  //ensures following cache includes this armIK or anim offsets
            flagAttachAfterIK = null;

            bagUI.ClickExitMenu();
            bagItemDetector.HideIndicator();

            //remove from shoulderState if applicable
            Loli loli = mainOwner as Loli;
            if (loli == null)
            {
                return;
            }
        }
        public override void OnPostPickup()
        {
            isOnShoulder = false;
            handWearBlend.SetTarget(true, transform, true, false, 0.0f, 1.0f, 0.5f);
        }

        public void StoreItem(Item targetItem)
        {

            if (!CanStoreItem(targetItem))
            {
                return;
            }
            BagItemCategory category = null;
            foreach (BagItemCategory entry in storedItems)
            {
                if (entry.type == targetItem.settings.itemType)
                {
                    category = entry;
                    break;
                }
            }
            //already in bag!
            if (category != null && category.items.Contains(targetItem))
            {
                return;
            }
            //prepare to add to bag category
            if (targetItem.mainOccupyState != null)
            {
                targetItem.mainOccupyState.AttemptDrop();
            }
            if (category == null)
            {
                category = new BagItemCategory(targetItem.settings.itemType);
                storedItems.Add(category);
            }
            SoundManager.main.RequestHandle(transform.position).PlayOneShot(storeItemSound);
            category.items.Add(targetItem);
            targetItem.gameObject.SetActive(false);
            GameDirector.instance.StartCoroutine(FullBagShapekeyAnimation((float)m_storedItems.Count / maxItems));
        }

        public void PlayOpenBagShapeKeyAnimation()
        {
            GameDirector.instance.StartCoroutine(OpenBagShapeKeyAnimation());
        }

        private IEnumerator OpenBagShapeKeyAnimation()
        {

            int openBagID = smr.sharedMesh.GetBlendShapeIndex("open");
            float timer = 0.0f;
            while (timer < 0.5f)
            {
                timer = Mathf.Min(0.5f, timer + Time.deltaTime);
                smr.SetBlendShapeWeight(openBagID, (timer / 0.5f) * 100.0f);
                yield return null;
            }
            timer = 0.5f;
            while (timer > 0.0f)
            {
                timer = Mathf.Max(0.0f, timer - Time.deltaTime);
                smr.SetBlendShapeWeight(openBagID, (timer / 0.5f) * 100.0f);
                yield return null;
            }
            smr.SetBlendShapeWeight(openBagID, 0.0f);
        }

        private IEnumerator FullBagShapekeyAnimation(float percentFull)
        {

            int fullBagID = smr.sharedMesh.GetBlendShapeIndex("full");
            float timer = 0.0f;
            float start = smr.GetBlendShapeWeight(fullBagID);
            percentFull *= 100;
            while (timer < 0.5f)
            {
                timer = Mathf.Min(0.5f, timer + Time.deltaTime);
                float ratio = timer / 0.5f;
                smr.SetBlendShapeWeight(fullBagID, Mathf.LerpUnclamped(start, percentFull, ratio));
                yield return null;
            }
            smr.SetBlendShapeWeight(fullBagID, percentFull);
        }

        public override bool CanBePickedUp(OccupyState newOccupyState)
        {
            if (newOccupyState == null)
            {
                return false;
            }
            if (mainOccupyState == null)
            {
                return true;
            }
            if (mainOwner.characterType == Character.Type.PLAYER && mainOccupyState.owner == newOccupyState.owner)
            {
                return false;
            }
            return base.CanBePickedUp(newOccupyState);
        }

        private bool CanOwnerTakeOutAnItem()
        {
            if (mainOwner == null)
            {
                return false;
            }
            HandState handstate = mainOwner.FindOccupyStateByHeldItem(this) as HandState;
            if (handstate == null)
            {
                return false;
            }
            HandState otherHandState;
            if (handstate.rightSide)
            {
                otherHandState = mainOwner.leftHandState as HandState;
            }
            else
            {
                otherHandState = mainOwner.rightHandState as HandState;
            }
            if (otherHandState.occupied)
            {
                return false;
            }
            return true;
        }

        public void TakeOutOfBag(int index)
        {

            if (index > storedItems.Count || index < 0)
            {
                return;
            }
            if (!CanOwnerTakeOutAnItem())
            {
                return;
            }
            BagItemCategory category = storedItems[index];
            if (category.items.Count == 0)
            {
                return;
            }
            nextItemTakeOutTarget = category;
            if (mainOwner.characterType == Character.Type.PLAYER)
            {
                Player player = mainOwner as Player;
                if (player.controls == Player.ControlType.KEYBOARD)
                {
                    if (mainOccupyState.rightSide)
                    {
                        player.rightPlayerHandState.animSys.SetTargetAnimation(Player.Animation.BAG_TAKE_OUT_RIGHT);
                        player.leftPlayerHandState.animSys.SetTargetAnimation(Player.Animation.BAG_TAKE_OUT_RIGHT);
                    }
                    else
                    {
                        player.rightPlayerHandState.animSys.SetTargetAnimation(Player.Animation.BAG_TAKE_OUT_LEFT);
                        player.leftPlayerHandState.animSys.SetTargetAnimation(Player.Animation.BAG_TAKE_OUT_LEFT);
                    }
                    bagUI.ClickExitMenu();
                }
                else
                {
                    TakeOutNextItem();
                }
            }
        }

        public void TakeOutNextItem()
        {

            if (!CanOwnerTakeOutAnItem())
            {
                return;
            }
            if (nextItemTakeOutTarget == null)
            {
                return;
            }
            Item targetItem = nextItemTakeOutTarget.items[0];
            nextItemTakeOutTarget.items.RemoveAt(0);
            if (nextItemTakeOutTarget.items.Count == 0)
            {
                nextItemTakeOutTarget.OnDestroy(this);
                storedItems.Remove(nextItemTakeOutTarget);
            }

            targetItem.gameObject.SetActive(true);
            HandState targetHandState;
            if (mainOccupyState.rightSide)
            {
                targetHandState = mainOwner.leftHandState;
            }
            else
            {
                targetHandState = mainOwner.rightHandState;
            }
            targetItem.transform.position = targetHandState.fingerAnimator.hand.position;
            targetHandState.GrabItemRigidBody(targetItem);
            nextItemTakeOutTarget = null;
        }
    }

}