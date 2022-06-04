using System.Collections.Generic;
using UnityEngine;


namespace viva
{


    public class MerchantBehavior : ActiveBehaviors.ActiveTask
    {

        [System.Serializable]
        public class MerchantSession : SerializedTaskData
        {
            [VivaFileAttribute]
            public VivaSessionAsset merchantSpotAsset { get; set; }

            public MerchantSpot merchantSpot { get { return merchantSpotAsset as MerchantSpot; } }
        }


        public MerchantSession merchantSession { get { return session as MerchantSession; } }
        public Set<Character> clients = new Set<Character>();
        public List<Character> alreadyAttended = new List<Character>();
        public AutonomyPlayAnimation comeCloserPlayAnim;
        private float lastBeginSellingTime;


        public MerchantBehavior(Loli _self) : base(_self, ActiveBehaviors.Behavior.MERCHANT, new MerchantSession())
        {
        }

        public override void OnActivate()
        {
            BeginSelling();
        }

        public void BeginSelling()
        {

            //must be employed to call this function
            if (merchantSession.merchantSpot == null)
            {
                return;
            }
            self.active.SetTask(this, null);

            var playSellAnim = new AutonomyPlayAnimation(
                self.autonomy, "play sell anim", Loli.Animation.STAND_MERCHANT_IDLE1
            );
            playSellAnim.loop = true;
            playSellAnim.AddRequirement(merchantSession.merchantSpot.CreateGoToEmploymentPosition(self));
            playSellAnim.onRegistered += delegate
            {
                self.onEnterVisibleItem += OnEnterVisibleItem;
                self.onExitVisibleItem += OnExitVisibleItem;
            };
            playSellAnim.onUnregistered += delegate
            {
                self.onEnterVisibleItem -= OnEnterVisibleItem;
                self.onExitVisibleItem -= OnExitVisibleItem;
            };
            var playWaitTimer = new AutonomyWait(self.autonomy, "check clients", 1.0f);
            playWaitTimer.loop = true;
            playWaitTimer.onSuccess += delegate { CheckClients(); };

            playSellAnim.AddPassive(playWaitTimer);

            self.autonomy.SetAutonomy(playSellAnim);
        }

        private void CheckClients()
        {

            if (comeCloserPlayAnim != null)
            {
                return;
            }
            if (Time.time - lastBeginSellingTime > 40.0f)
            {
                lastBeginSellingTime = Time.time;
                alreadyAttended.Clear();
            }
            foreach (var client in clients.objects)
            {
                float sqDist = (client.headItem.transform.position - self.headItem.transform.position).sqrMagnitude;
                if (sqDist < 12.0f)
                {
                    if (!alreadyAttended.Contains(client))
                    {
                        alreadyAttended.Add(client);

                        var comeAnimation = self.rightHandState.occupied ? Loli.Animation.STAND_FOLLOW_ME_LEFT : Loli.Animation.STAND_FOLLOW_ME_RIGHT;
                        comeCloserPlayAnim = new AutonomyPlayAnimation(self.autonomy, "play come closer", comeAnimation);

                        var waitForIdle = new AutonomyWaitForIdle(self.autonomy, "wait for idle");
                        comeCloserPlayAnim.AddRequirement(waitForIdle);

                        var faceClient = new AutonomyFaceDirection(self.autonomy, "face client", delegate (TaskTarget target)
                        {
                            target.SetTargetPosition(client.headItem.transform.position);
                        }, 1.0f, 20.0f);
                        comeCloserPlayAnim.AddRequirement(faceClient);
                        comeCloserPlayAnim.onFail += delegate { comeCloserPlayAnim = null; BeginSelling(); };
                        comeCloserPlayAnim.onSuccess += delegate { comeCloserPlayAnim = null; BeginSelling(); };

                        self.autonomy.SetAutonomy(comeCloserPlayAnim);
                        break;
                    }
                }
            }
        }

        private void OnEnterVisibleItem(Item item)
        {
            if (item.settings.itemType == Item.Type.CHARACTER)
            {
                clients.Add(item.mainOwner);
            }
        }
        private void OnExitVisibleItem(Item item)
        {
            if (item.settings.itemType == Item.Type.CHARACTER)
            {
                clients.Remove(item.mainOwner);
            }
        }
    }

}