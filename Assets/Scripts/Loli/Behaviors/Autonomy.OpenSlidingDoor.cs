using UnityEngine;


namespace viva
{


    public class AutonomyOpenSlidingDoor : Autonomy.Task
    {

        private SlidingDoor slidingDoor;
        private AutonomyPlayAnimation openDoor;
        private AutonomyMoveTo goToAndOpenDoor;
        private CharacterCollisionCallback.Type targetType;
        private LoliHandState targetHandState;
        private ContactPoint[] contactPoints = new ContactPoint[1];
        private float targetEndDoorUnit;
        private float targetDoorUnit;
        private float slidingComplete = 0.0f;
        private readonly float startingDoorUnit;
        private readonly float startingDoorSide;
        private readonly float targetDoorSide;


        public AutonomyOpenSlidingDoor(Autonomy _autonomy, string _name, Vector3 targetSidePos, SlidingDoor _slidingDoor, AutonomyMoveTo parentMoveTo) : base(_autonomy, _name)
        {
            slidingDoor = _slidingDoor;

            if (slidingDoor)
            {
                startingDoorUnit = CalculateStartingDoorUnit();
                startingDoorSide = Tools.GetSide(self.floorPos, slidingDoor.transform);
                targetDoorUnit = CalculateTargetDoorUnit();
                targetDoorSide = Tools.GetSide(targetSidePos, slidingDoor.transform);

                openDoor = new AutonomyPlayAnimation(autonomy, _name + " play anim ", Loli.Animation.NONE);
                openDoor.onRegistered += SetupSlidingDoorAnimation;

                goToAndOpenDoor = new AutonomyMoveTo(autonomy, _name + " move to", delegate (TaskTarget target)
                {

                    target.SetTargetPosition(GetFrontCenterDoorPos(startingDoorUnit, Mathf.Lerp(0.35f, 0.2f, slidingComplete)));
                },
                    0.0f,
                    BodyState.STAND,
                    delegate (TaskTarget target)
                    {
                        target.SetTargetPosition(GetFrontCenterDoorPos(slidingDoor.GetUnitDistanceFromStart(), -1.0f));
                    }
                );
                parentMoveTo.ignoreSlidingDoors.Add(slidingDoor);
                goToAndOpenDoor.ignoreSlidingDoors = parentMoveTo.ignoreSlidingDoors;

                onSuccess += delegate { parentMoveTo.ignoreSlidingDoors.Remove(slidingDoor); };
                onFail += delegate { parentMoveTo.ignoreSlidingDoors.Remove(slidingDoor); };
                onRemovedFromQueue += delegate { parentMoveTo.ignoreSlidingDoors.Remove(slidingDoor); };

                var openSlidingDoor = this;
                goToAndOpenDoor.onSuccess += delegate
                {
                    openSlidingDoor.AddPassive(openDoor);
                };

                //wait for owner to be blank
                var waitForOwnerEmpty = new AutonomyFilterUse(self.autonomy, _name + " filter use", slidingDoor.filterUse, 0.0f);
                //wait a minimum distance perpendicular to door
                var waitAtSafeRegion = new AutonomyMoveTo(self.autonomy, _name + " wait away from door", delegate (TaskTarget target)
                {
                    Vector3 local = slidingDoor.transform.InverseTransformPoint(self.floorPos);
                    local.y = 0.0f;
                    float perpDist = local.z;
                    local.z = 0.0f;
                    float selfUnit = slidingDoor.GetUnitDistanceFromStart(slidingDoor.transform.TransformPoint(local));

                    perpDist = Mathf.Max(1.0f, perpDist);
                    target.SetTargetPosition(GetFrontCenterDoorPos(selfUnit - 0.5f, perpDist));

                }, 0.0f, BodyState.STAND,
                    delegate (TaskTarget target)
                    {
                        if (slidingDoor)
                        {
                            target.SetTargetPosition(slidingDoor.transform.position);
                        }
                    }
                );

                waitForOwnerEmpty.AddPassive(waitAtSafeRegion);

                waitForOwnerEmpty.onSuccess += delegate
                {
                    openSlidingDoor.AddPassive(goToAndOpenDoor);
                };
                PrependRequirement(waitForOwnerEmpty);
            }
        }

        private float CalculateStartingDoorUnit()
        {
            //pick current unit or closest end
            float unit = slidingDoor.GetUnitDistanceFromStart();

            float roundUnit = Mathf.Round(unit);
            if (roundUnit == slidingDoor.maxUnitsLeft || roundUnit == slidingDoor.maxUnitsRight)
            {
                return roundUnit;
            }
            return unit;
        }

        private float CalculateTargetDoorUnit()
        {

            float slideLeft = Mathf.Max(startingDoorUnit - 1.0f, -slidingDoor.maxUnitsLeft);
            float slideRight = Mathf.Min(startingDoorUnit + 1.0f, slidingDoor.maxUnitsRight);

            float minUnit = startingDoorUnit - slideLeft;
            float maxUnit = slideRight - startingDoorUnit;
            // Debug.LogError(" startingDoorUnit: "+startingDoorUnit+" minUnit:"+minUnit+" maxUnit:"+maxUnit);
            // Debug.LogError( ( maxUnit > minUnit )+"  "+startingDoorSide+"  " );
            if (maxUnit > minUnit)
            {   //slide right
                targetEndDoorUnit = slideRight;
                return slideRight;
            }
            else
            {   //slide left
                targetEndDoorUnit = slideLeft;
                return slideLeft;
            }
        }

        private void SetupSlidingDoorAnimation()
        {
            targetHandState = self.GetPreferredHandState(null);
            if (targetHandState == null)
            {
                FlagForFailure();
                return;
            }
            if (targetHandState.rightSide)
            {
                openDoor.OverrideAnimations(Loli.Animation.STAND_SLIDINGDOOR_RIGHT, Loli.Animation.STAND_SLIDINGDOOR_IDLE_LOOP_RIGHT);
            }
            else
            {
                openDoor.OverrideAnimations(Loli.Animation.STAND_SLIDINGDOOR_LEFT, Loli.Animation.STAND_SLIDINGDOOR_IDLE_LOOP_LEFT);
            }

            int targetHandSign = System.Convert.ToInt32(!targetHandState.rightSide) * 2 - 1;    //-1 or 1  left/right

            if (targetDoorUnit > startingDoorUnit)
            {
                self.animator.SetFloat(Instance.pickupReverseID, targetHandSign * startingDoorSide);
            }
            else
            {
                self.animator.SetFloat(Instance.pickupReverseID, -targetHandSign * startingDoorSide);
            }
        }

        public override bool? Progress()
        {

            if (slidingDoor == null)
            {
                return false;
            }
            float currentSide = Tools.GetSide(self.floorPos, slidingDoor.transform);
            if (currentSide == targetDoorSide)
            {
                return true;
            }

            float unit = slidingDoor.GetUnitDistanceFromStart();
            slidingComplete = 1.0f - Mathf.Clamp01(Mathf.Abs(unit - targetDoorUnit));
            if (slidingComplete >= 0.65f)
            {   //65% required to succeed
                FlagForSuccess();
            }
            return null;
        }

        private Vector3 GetFrontCenterDoorPos(float unit, float zForward)
        {

            if (slidingDoor == null)
            {
                return Vector3.zero;
            }
            else
            {
                return slidingDoor.GetUnitPosition(unit + 0.5f) + slidingDoor.transform.forward * startingDoorSide * zForward + Vector3.up * 0.2f;
            }
        }
    }

}