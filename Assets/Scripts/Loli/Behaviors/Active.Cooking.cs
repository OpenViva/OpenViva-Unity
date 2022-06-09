using System.Collections.Generic;
using UnityEngine;


namespace viva
{


    public partial class CookingBehavior : ActiveBehaviors.ActiveTask
    {

        public class Recipe
        {
            //minimum mixing bowl properties
            public int minimumEggs = 4;
            public float minimumFlour = 6.0f;
        }

        private enum Phase
        {
            GO_TO_KITCHEN,
            SEARCH_COOKING_INGREDIENTS,
            WAIT_TO_DROP_ITEM,
            WAIT_TO_PICKUP_ITEM,
            GRIND_WHEAT,
            DROP_WHEAT_INTO_MORTAR,
            MIX_BATTER,
            DROP_EGG_INTO_BATTER,
            DROP_FLOUR_INTO_BATTER
        }



        private KitchenFacilities facilities;
        private Phase phase = Phase.GRIND_WHEAT;
        private List<Item.Type> priorityPickup = new List<Item.Type>();
        private float searchTimer = 0.0f;
        private int searches = 0;
        private Recipe currentRecipe = new Recipe();
        private MixingBowl targetMixingBowl = null;

        public CookingBehavior(Loli _self) : base(_self, ActiveBehaviors.Behavior.COOKING, null)
        {
        }

        public bool AttemptBeginCooking(KitchenFacilities newFacilities)
        {

            if (newFacilities == null)
            {
                return false;
            }
            Vector3 targetStartPos = newFacilities.transform.TransformPoint(newFacilities.centerLocalPos);
            if (Vector3.Distance(targetStartPos, self.floorPos) > 10.0f)
            {
                return false;
            }
            self.autonomy.SetAutonomy(new AutonomyFaceDirection(self.autonomy, "look kitchen", delegate (TaskTarget target)
            {
                target.SetTargetPosition(newFacilities.transform.position);
            }, 2.0f));


            switch (self.active.currentTask.type)
            {
                case ActiveBehaviors.Behavior.IDLE:
                case ActiveBehaviors.Behavior.FOLLOW:
                    break;
                default:    //not allowed with other behaviors
                    return false;
            }
            if (!self.IsHappy() || self.IsTired())
            {
                self.active.idle.PlayAvailableRefuseAnimation();
                return false;
            }
            facilities = newFacilities;
            self.active.SetTask(this, null);
            SetCookingPhase(Phase.GO_TO_KITCHEN);
            targetMixingBowl = null;
            return true;
        }

        public override void OnDeactivate()
        {
            if (phase == Phase.GRIND_WHEAT)
            {
                self.SetTargetAnimation(self.GetLastReturnableIdleAnimation());
            }
        }

        private void SetCookingPhase(Phase newPhase)
        {
            phase = newPhase;
            searchTimer = 0;
        }

        public override bool OnReturnPollTaskResult(ActiveBehaviors.ActiveTask returnSource, bool succeeded)
        {

            if (phase == Phase.WAIT_TO_DROP_ITEM /*&& returnSource==self.active.drop*/ )
            {
                SetCookingPhase(Phase.SEARCH_COOKING_INGREDIENTS);
                return true;
            }
            else if (phase == Phase.WAIT_TO_PICKUP_ITEM /*&& returnSource==self.active.pickup*/ )
            {
                SetCookingPhase(Phase.SEARCH_COOKING_INGREDIENTS);
                return true;
            }
            return false;
        }

        public override void OnActivate()
        {
            GameDirector.player.objectFingerPointer.selectedLolis.Remove(self);
            self.characterSelectionTarget.OnUnselected();
            searchTimer = 0.0f;
            searches = 0;
        }

        public override void OnUpdate()
        {
            if (facilities == null)
            {
                self.active.SetTask(self.active.idle, false);
                return;
            }
            switch (phase)
            {
                case Phase.GO_TO_KITCHEN:
                    UpdateGoToKitchen();
                    break;
                case Phase.SEARCH_COOKING_INGREDIENTS:
                    if (targetMixingBowl == null)
                    {
                        UpdateSearchForTargetMixingBowl();
                    }
                    else
                    {
                        UpdateSearchCookingIngredients();
                    }
                    break;
                case Phase.WAIT_TO_DROP_ITEM:
                    break;
                case Phase.GRIND_WHEAT:
                    UpdateGrindWheat();
                    break;
                case Phase.MIX_BATTER:
                    UpdateMixBatter();
                    break;
                case Phase.DROP_FLOUR_INTO_BATTER:
                    UpdateDropFlourIntoMixingBowl();
                    break;
                case Phase.DROP_WHEAT_INTO_MORTAR:
                    UpdateDropWheatIntoMortar();
                    break;
            }
        }

        private void UpdateGoToKitchen()
        {
            Vector3 targetStartPos = facilities.transform.TransformPoint(facilities.centerLocalPos);
            if (!self.locomotion.isMoveToActive() || Vector3.Distance(targetStartPos, self.floorPos) > 2.0f)
            {
                var goToKitchen = new AutonomyMoveTo(self.autonomy, "go to kitchen", delegate (TaskTarget target)
                {
                    target.SetTargetPosition(targetStartPos);
                },
                1.0f,
                BodyState.STAND);
                goToKitchen.onSuccess += OnReachKitchen;
                goToKitchen.onFail += delegate { self.active.SetTask(self.active.idle, false); };
                self.autonomy.SetAutonomy(goToKitchen);
                //var path = self.locomotion.GetNavMeshPath( targetStartPos );
                //if( path == null ){
                //	self.active.SetTask( self.active.idle, false );
                //}else{
                //	self.locomotion.FollowPath( path, OnReachKitchen );
                //}
            }


        }

        private void OnReachKitchen()
        {
            phase = Phase.SEARCH_COOKING_INGREDIENTS;
        }

        public override void OnLateUpdatePostIK()
        {
            if (facilities == null)
            {
                self.active.SetTask(self.active.idle, false);
                return;
            }
            switch (phase)
            {
                case Phase.DROP_EGG_INTO_BATTER:
                    LateUpdatePostIKDropEggIntoBatter();
                    break;
            }
        }

        private void LateUpdatePostIKDropEggIntoBatter()
        {
            MixingBowl bowl = FindHeldItem(Item.Type.MIXING_BOWL) as MixingBowl;
            if (!bowl)
            {
                self.active.SetTask(self.active.idle, false);
                return;
            }
            Egg egg = FindHeldItem(Item.Type.EGG) as Egg;
            if (!egg || bowl.eggCount >= currentRecipe.minimumEggs)
            {
                SetCookingPhase(Phase.SEARCH_COOKING_INGREDIENTS);
                return;
            }
            Loli.Animation eggCrackAnim;
            if (bowl.mainOccupyState.rightSide)
            {
                eggCrackAnim = Loli.Animation.STAND_EGG_INTO_MIXING_BOWL_LEFT;
            }
            else
            {
                eggCrackAnim = Loli.Animation.STAND_EGG_INTO_MIXING_BOWL_RIGHT;
            }
            if (self.currentAnim != eggCrackAnim)
            {
                if (self.currentAnim == self.GetLastReturnableIdleAnimation())
                {
                    self.SetTargetAnimation(eggCrackAnim);
                }
            }
            else if (self.GetLayerAnimNormTime(1) > 0.6f)
            {
                egg.Crack();
            }
            self.SetLookAtTarget(bowl.transform);
        }

        private void UpdateMixBatter()
        {
            MixingBowl bowl = FindHeldItem(Item.Type.MIXING_BOWL) as MixingBowl;
            if (!bowl || !FindHeldItem(Item.Type.MIXING_SPOON))
            {
                self.active.SetTask(self.active.idle, false);
                return;
            }
            if (bowl.eggCount >= currentRecipe.minimumEggs && bowl.substanceAmount >= currentRecipe.minimumFlour)
            {
                if (self.currentAnim == self.GetLastReturnableIdleAnimation())
                {
                    if (!bowl.mainOccupyState.rightSide)
                    {
                        self.SetTargetAnimation(Loli.Animation.STAND_MIXING_BOWL_MIX_LOOP_LEFT);
                    }
                    else
                    {
                        self.SetTargetAnimation(Loli.Animation.STAND_MIXING_BOWL_MIX_LOOP_RIGHT);
                    }
                    self.SetLookAtTarget(bowl.transform);
                }
                if (!self.IsSpeakingAtAll())
                {
                    self.SpeakAtRandomIntervals(Loli.VoiceLine.HUMMING, 3.0f, 3.0f);
                }
            }
            else
            {
                //finished making recipe!
                self.active.SetTask(self.active.idle, true);
                self.SetTargetAnimation(self.GetLastReturnableIdleAnimation());
            }
        }

        private void UpdateGrindWheat()
        {
            if (!FindHeldItem(Item.Type.MORTAR) || !FindHeldItem(Item.Type.PESTLE))
            {
                self.active.SetTask(self.active.idle, false);
                return;
            }
            Mortar mortar = self.rightHandState.heldItem as Mortar;
            if (mortar == null)
            {
                mortar = self.leftHandState.heldItem as Mortar;
            }
            if (mortar.hasGrain)
            {
                if (self.currentAnim == self.GetLastReturnableIdleAnimation())
                {
                    if (self.rightHandState.heldItem.settings.itemType == Item.Type.PESTLE)
                    {
                        self.SetTargetAnimation(Loli.Animation.STAND_MORTAR_AND_PESTLE_GRIND_LOOP_LEFT);
                    }
                    else
                    {
                        self.SetTargetAnimation(Loli.Animation.STAND_MORTAR_AND_PESTLE_GRIND_LOOP_RIGHT);
                    }
                    self.SetLookAtTarget(mortar.transform);
                }
                if (!self.IsSpeakingAtAll())
                {
                    self.SpeakAtRandomIntervals(Loli.VoiceLine.HUMMING, 3.0f, 3.0f);
                }
            }
            else
            {
                //Find more grain to add to Mortar
                SetCookingPhase(Phase.SEARCH_COOKING_INGREDIENTS);
                self.SetTargetAnimation(self.GetLastReturnableIdleAnimation());
                searchTimer = 3.4f; //make search animation start at a smoother time
            }
        }

        private void UpdateDropWheatIntoMortar()
        {
            Item mortar = FindHeldItem(Item.Type.MORTAR);
            if (mortar == null)
            {
                self.active.SetTask(self.active.idle, false);
                return;
            }
            //wait until idle anim and no longer holding item
            if (!FindHeldItem(Item.Type.WHEAT_SPIKE) && self.currentAnim == self.GetLastReturnableIdleAnimation())
            {
                SetCookingPhase(Phase.SEARCH_COOKING_INGREDIENTS);
                return;
            }
            if (self.currentAnim == self.GetLastReturnableIdleAnimation())
            {
                if (mortar.mainOccupyState.rightSide)
                {
                    self.SetTargetAnimation(Loli.Animation.STAND_WHEAT_INTO_MORTAR_RIGHT);
                }
                else
                {
                    self.SetTargetAnimation(Loli.Animation.STAND_WHEAT_INTO_MORTAR_LEFT);
                }
            }
        }

        private void UpdateDropFlourIntoMixingBowl()
        {
            MixingBowl mixingBowl = FindHeldItem(Item.Type.MIXING_BOWL) as MixingBowl;
            if (mixingBowl == null)
            {
                self.active.SetTask(self.active.idle, false);
                return;
            }
            Mortar mortar = FindHeldItem(Item.Type.MORTAR) as Mortar;
            if (mortar == null)
            {
                self.active.SetTask(self.active.idle, false);
                return;
            }
            else if (mortar.substanceAmount == 0.0f)
            {
                SetCookingPhase(Phase.SEARCH_COOKING_INGREDIENTS);
                return;
            }
            if (self.currentAnim == self.GetLastReturnableIdleAnimation())
            {
                if (mixingBowl.mainOccupyState.rightSide)
                {
                    self.SetTargetAnimation(Loli.Animation.STAND_POUR_MORTAR_INTO_MIXING_BOWL_LEFT);
                }
                else
                {
                    self.SetTargetAnimation(Loli.Animation.STAND_POUR_MORTAR_INTO_MIXING_BOWL_RIGHT);
                }
            }
        }

        private Item FindHeldItem(Item.Type type)
        {
            if (self.rightLoliHandState.heldItem != null && self.rightLoliHandState.heldItem.settings.itemType == type)
            {
                return self.rightLoliHandState.heldItem;
            }
            if (self.leftLoliHandState.heldItem != null && self.leftLoliHandState.heldItem.settings.itemType == type)
            {
                return self.leftLoliHandState.heldItem;
            }
            return null;
        }

        private bool HasItemCombo(Item.Type typeA, Item.Type typeB, ref Item mainItem, ref Item.Type missingMainItemCompliment)
        {
            Item typeAItem = FindHeldItem(typeA);
            Item typeBItem = FindHeldItem(typeB);
            if (typeAItem != null || typeBItem != null)
            {
                if (typeAItem != null)
                {
                    if (typeBItem != null)
                    {
                        return true;
                    }
                    else
                    {
                        if (mainItem == null)
                        {
                            mainItem = typeAItem;
                            missingMainItemCompliment = typeB;
                        }
                        priorityPickup.Add(typeB);
                        return false;
                    }
                }
                else
                {
                    if (mainItem == null)
                    {
                        mainItem = typeBItem;
                        missingMainItemCompliment = typeA;
                    }
                    priorityPickup.Add(typeA);
                    return false;
                }
            }
            else
            {
                priorityPickup.Add(typeA);
                priorityPickup.Add(typeB);
                return false;
            }
        }

        private MixingBowl FindMixingBowl()
        {
            MixingBowl found = FindHeldItem(Item.Type.MIXING_BOWL) as MixingBowl;
            if (found != null)
            {
                return found;
            }
            float leastSqDist = Mathf.Infinity;
            MixingBowl newTargetMixingBowl = null;
            for (int i = 0; i < self.GetViewResultCount(); i++)
            {
                MixingBowl candidate = self.GetViewResult(i) as MixingBowl;
                if (candidate == null)
                {
                    continue;
                }
                if (candidate.mainOwner != null)
                {   //ignore picked up items
                    continue;
                }
                float sqDist = Vector3.SqrMagnitude(candidate.transform.position - self.head.position);
                if (sqDist < leastSqDist)
                {
                    leastSqDist = sqDist;
                    newTargetMixingBowl = candidate;
                }
            }
            return newTargetMixingBowl;
        }

        private void UpdateSearchAroundTimer(float timer)
        {
            searchTimer += Time.deltaTime;
            if (searchTimer > timer)
            {
                searchTimer = 0.0f;
                self.autonomy.Interrupt(new AutonomyFaceDirection(self.autonomy, "face direction search cooking", delegate (TaskTarget target)
                {
                    target.SetTargetPosition(self.floorPos - self.transform.forward);
                }, 2.0f));
                // self.SetRootFacingTarget( self.floorPos-self.transform.forward, 200.0f, 15.0f, 15.0f );
                self.SetTargetAnimation(Loli.Animation.STAND_SEARCH_RIGHT);
                searches++;
            }
        }

        private void UpdateSearchForTargetMixingBowl()
        {
            targetMixingBowl = FindMixingBowl();
            if (targetMixingBowl == null)
            {
                UpdateSearchAroundTimer(1.0f);
                if (searchTimer > 2)
                {
                    self.active.SetTask(self.active.idle, false);
                    Debug.Log("[COOKING] Could not find target mixing bowl");
                }
            }
        }

        private void UpdateSearchCookingIngredients()
        {

            priorityPickup.Clear();
            Item mainItem = null;
            Item.Type missingMainItemCompliment = Item.Type.NONE;

            if (targetMixingBowl.substanceAmount >= currentRecipe.minimumFlour)
            {
                if (targetMixingBowl.eggCount >= currentRecipe.minimumEggs)
                {
                    if (HasItemCombo(Item.Type.MIXING_BOWL, Item.Type.MIXING_SPOON, ref mainItem, ref missingMainItemCompliment))
                    {
                        SetCookingPhase(Phase.MIX_BATTER);
                        return;
                    }
                }
                else
                {
                    //need to add eggs
                    if (HasItemCombo(Item.Type.MIXING_BOWL, Item.Type.EGG, ref mainItem, ref missingMainItemCompliment))
                    {
                        SetCookingPhase(Phase.DROP_EGG_INTO_BATTER);
                        return;
                    }
                }
            }
            else
            {
                //need to add flour
                Mortar mortar = FindHeldItem(Item.Type.MORTAR) as Mortar;
                if (mortar != null)
                {
                    if (!mortar.hasGrain)
                    {

                        if (mortar.substanceAmount > 0.0f)
                        {
                            //Add flour to target mixing bowl
                            if (HasItemCombo(Item.Type.MORTAR, Item.Type.MIXING_BOWL, ref mainItem, ref missingMainItemCompliment))
                            {
                                SetCookingPhase(Phase.DROP_FLOUR_INTO_BATTER);
                                return;
                            }
                        }
                        else
                        {
                            //Add Wheat grain to Mortar
                            if (HasItemCombo(Item.Type.MORTAR, Item.Type.WHEAT_SPIKE, ref mainItem, ref missingMainItemCompliment))
                            {
                                SetCookingPhase(Phase.DROP_WHEAT_INTO_MORTAR);
                                return;
                            }
                        }
                    }
                    else
                    {
                        if (HasItemCombo(Item.Type.MORTAR, Item.Type.PESTLE, ref mainItem, ref missingMainItemCompliment))
                        {
                            SetCookingPhase(Phase.GRIND_WHEAT);
                            return;
                        }
                    }
                }
                else
                {
                    //pick up any Mortar
                    priorityPickup.Add(Item.Type.MORTAR);
                }
            }

            //pickup based on compliment and priority
            for (int i = 0; i < self.GetViewResultCount(); i++)
            {
                Item item = self.GetViewResult(i);
                if (item.mainOwner != null)
                {   //ignore picked up items
                    continue;
                }
                if (!priorityPickup.Contains(item.settings.itemType))
                {
                    continue;
                }

                //Find HandState to pickup with
                LoliHandState pickupHand;
                if (mainItem != null && item.settings.itemType == missingMainItemCompliment)
                {
                    if (self.rightHandState.heldItem != null && self.rightHandState.heldItem.settings.itemType == mainItem.settings.itemType)
                    {
                        pickupHand = self.leftLoliHandState;
                    }
                    else if (self.leftHandState.heldItem && self.leftHandState.heldItem.settings.itemType == mainItem.settings.itemType)
                    {
                        pickupHand = self.rightLoliHandState;
                    }
                    else
                    {
                        Debug.LogError("ERROR");
                        continue;   //error
                    }
                }
                else
                {
                    if (UnityEngine.Random.value > 0.5f)
                    {
                        pickupHand = self.rightLoliHandState;
                    }
                    else
                    {
                        pickupHand = self.leftLoliHandState;
                    }
                }
                //drop item if pickupHand is curently busy
                if (pickupHand.heldItem != null)
                {
                    var itemdrop = new AutonomyDrop(self.autonomy, "item drop", pickupHand.heldItem, facilities.centerLocalPos);
                    self.autonomy.SetAutonomy(itemdrop);
                    itemdrop.onSuccess += delegate
                    {
                        self.active.PollNextTaskResult(this);
                        SetCookingPhase(Phase.WAIT_TO_DROP_ITEM);
                        return;
                    };
                    //if( self.active.drop.AttemptDropItem( pickupHand, true, 1.0f, 0.1f, true ) ){
                    //	self.active.PollNextTaskResult( this );
                    //	SetCookingPhase( Phase.WAIT_TO_DROP_ITEM );
                    //	return;
                    //}
                }
                else
                {   //else use it to pick up missingItem
                    var itempickup = new AutonomyPickup(self.autonomy, "item pickup", item, self.GetPreferredHandState(item));
                    self.autonomy.SetAutonomy(itempickup);
                    itempickup.onSuccess += delegate
                    {
                        self.active.PollNextTaskResult(this);
                        SetCookingPhase(Phase.WAIT_TO_PICKUP_ITEM);
                        return;
                    };
                    //if( self.active.pickup.AttemptGoAndPickup( item, self.active.pickup.FindPreferredHandState( item ), true, true ) ){
                    //	self.active.PollNextTaskResult( this );
                    //	SetCookingPhase( Phase.WAIT_TO_PICKUP_ITEM );
                    //	return;
                    //}
                }
                break;
            }
            //increase search timer
            UpdateSearchAroundTimer(4.0f);
            if (searches > 2)
            {
                //if didn't find anything, drop contents and end behavior
                LoliHandState dropHand;
                if (mainItem != null)
                {
                    if (self.rightHandState.heldItem == mainItem)
                    {
                        dropHand = self.rightLoliHandState;
                    }
                    else if (self.leftHandState.heldItem == mainItem)
                    {
                        dropHand = self.leftLoliHandState;
                    }
                    else
                    {
                        dropHand = null;
                    }
                }
                else
                {
                    dropHand = null;
                }
                // if( dropHand != null && self.active.drop.AttemptDropItem( dropHand, true, 1.0f, 0.1f, true ) ){
                //     self.active.PollNextTaskResult( this );
                // 	SetCookingPhase( Phase.WAIT_TO_DROP_ITEM );
                // }else{
                //     self.active.SetTask( self.active.idle, false );
                // }
            }
        }

        public override void OnAnimationChange(Loli.Animation oldAnim, Loli.Animation newAnim)
        {

        }
    }

}