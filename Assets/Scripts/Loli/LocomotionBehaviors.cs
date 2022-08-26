using System.Collections;
using UnityEngine;
using UnityEngine.AI;


namespace viva
{


    public partial class LocomotionBehaviors : Job
    {

        public delegate void LocomotionCallback();

        public class PathCache
        {
            public int id;
            public Vector3 destination;
        }

        public Vector3[] path { get; private set; } = null; //path must always be > 0
        public int lastPathID { get; protected set; } = 0;
        public int currCorner { get; private set; } = 0;
        private Vector3 targetVelocity = Vector3.zero;
        private float currSpeed = 0.0f;
        private Tools.EaseBlend brakeBlend = new Tools.EaseBlend();
        private LocomotionCallback onPathComplete = null;
        private Loli.LocomotionInfo zeroLocomotionInfo = new Loli.LocomotionInfo(0.0f, 0.0f, 0.0f, 0.0f);
        private Loli.LocomotionInfo locomotionInfo = null;
        private LocomotionCallback onFollowPathStart;
        private Coroutine impulseCoroutine = null;

        public static float minCornerDist { get; private set; } = 0.2f;
        public static float minSqCornerDist { get { return minCornerDist * minCornerDist; } }
        private static NavMeshHit navTest = new NavMeshHit();


        public LocomotionBehaviors(Loli _self) : base(_self, JobType.LOCOMOTION)
        {
            locomotionInfo = zeroLocomotionInfo;
        }

        public void RemoveOnFollowPathStartCallback(LocomotionCallback callback)
        {
            onFollowPathStart -= callback;
        }

        public void AddOnFollowPathStartCallback(LocomotionCallback callback)
        {
            onFollowPathStart -= callback;
            onFollowPathStart += callback;
        }



        private void initBrake()
        {
            brakeBlend.reset(currSpeed);
            brakeBlend.StartBlend(0.0f, 0.2f);
            updateBrake();
        }
        private void updateBrake()
        {
            brakeBlend.Update(self.animationDelta);
            currSpeed = brakeBlend.value;
        }

        public void ApplyMovement(bool finishPath)
        {
            if (!self.groundHeight.HasValue)
            {
                return;
            }
            if (path != null && currCorner < path.Length)
            {
                Vector3 nextCorner = path[currCorner];

                //start facing targetFaceYaw if close enough
                Vector3 cornerDiff = nextCorner - self.floorPos;
                bool heightInRange = Mathf.Abs(cornerDiff.y) < 0.3f;
                cornerDiff.y = 0.0f;
                Debug.DrawLine(nextCorner, self.floorPos, Color.white, 0.025f);
                float successDist;
                if (currCorner + 1 < path.Length || finishPath)
                {
                    successDist = LocomotionBehaviors.minCornerDist;
                }
                else
                {
                    successDist = 0.0f;
                }
                float currentCornerSqDist = cornerDiff.sqrMagnitude;
                if (currentCornerSqDist < successDist * successDist && heightInRange)
                {
                    currCorner++;
                    if (currCorner >= path.Length)
                    {
                        if (onPathComplete != null)
                        {
                            onPathComplete();
                        }
                        StopMoveTo();
                    }
                }
                else
                {   //if hasn't reached corner yet

                    float currentVelAngle = Mathf.Atan2(-self.spine1RigidBody.velocity.z, self.spine1RigidBody.velocity.x);
                    Vector3 planeVel = self.spine1RigidBody.velocity;
                    planeVel.y = 0.0f;
                    float currentVelMagnitude = planeVel.magnitude;
                    Vector3 targetDiff = nextCorner - self.floorPos;
                    float targetVelAngle = Mathf.Atan2(-targetDiff.z, targetDiff.x);

                    targetVelocity.x = Mathf.Cos(targetVelAngle);
                    targetVelocity.z = -Mathf.Sin(targetVelAngle);

                    Debug.DrawLine(nextCorner, nextCorner + Vector3.up * 0.25f, Color.magenta, 0.1f);

                    currSpeed = Mathf.Min(currSpeed + locomotionInfo.acceleration * self.animationDelta, locomotionInfo.maxSpeed);

                    float taper = Mathf.Clamp01(Mathf.Sqrt(currentCornerSqDist) / LocomotionBehaviors.minCornerDist);
                    currSpeed *= 1.0f - Mathf.Pow(1.0f - taper, 2);

                }
            }
            else
            {
                updateBrake();
            }

            if (impulseCoroutine == null)
            {
                //apply forces
                Vector3 setVel = targetVelocity * currSpeed * 2.0f;
                setVel *= 0.5f * self.puppetMaster.pinWeight;
                self.spine1RigidBody.AddForce(setVel * self.lastPhysicsStepMult, ForceMode.VelocityChange);
            }
        }

        public override void OnUpdate()
        {
            if (path != null && path.Length > 0)
            {
                for (int i = 1, j = 0; i < path.Length; j = i++)
                {
                    Debug.DrawLine(path[i], path[j], Color.green, 0.01f);
                }
            }
        }

        public void StopMoveTo()
        {
            path = null;
            initBrake();
        }

        public bool isMoveToActive()
        {
            if (path == null)
            {
                return false;
            }
            return currCorner < path.Length;
        }

        //return final destination of current path if any
        public Vector3? GetCurrentDestination()
        {
            if (path == null)
            {
                return null;
            }
            return path[path.Length - 1];
        }

        public void PlayForce(Vector3 force, float duration)
        {

            if (impulseCoroutine != null)
            {
                GameDirector.instance.StopCoroutine(impulseCoroutine);
            }
            impulseCoroutine = GameDirector.instance.StartCoroutine(Impulse(force, duration));
        }

        private IEnumerator Impulse(Vector3 force, float duration)
        {
            float timer = 0.0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                self.spine1RigidBody.AddForce(force, ForceMode.VelocityChange);
                yield return null;
            }
            impulseCoroutine = null;
        }

        public static bool isOnWalkableFloor(Vector3 pos)
        {
            return NavMesh.SamplePosition(pos, out navTest, minCornerDist * 2.0f, NavMesh.AllAreas);
        }

        public void FollowPath(Vector3[] newPath, LocomotionCallback _onPathComplete = null, PathCache pathID = null)
        {
            if (self.passive.handhold.anyHandBeingHeld)
            {
                return;
            }
            if (self.isConstrained)
            {
                return;
            }
            if (newPath == null)
            {
                Debug.LogError("Nav path is null!");
                return;
            }
            //paths must be of size greater than 0
            if (newPath.Length == 0)
            {
                newPath = new Vector3[1] { self.floorPos };
            }
            if (self.anchorActive)
            {
                Debug.Log("Cannot follow path while frozen!");
                return;
            }
            path = newPath;
            Tools.DrawCross(path[0] + Vector3.up * 0.2f, Color.magenta, 0.1f);
            currCorner = 0;
            onPathComplete = _onPathComplete;

            lastPathID++;
            if (pathID != null)
            {
                pathID.id = lastPathID;
                pathID.destination = newPath[newPath.Length - 1];
            }
            if (onFollowPathStart != null)
            {
                onFollowPathStart();
            }
        }

        //function raycasts down to refine floor locality
        public Vector3[] GetNavMeshPath(Vector3 newTargetMoveToPos, Vector3? overrideSourcePos = null)
        {

            Vector3 sourcePos;
            if (overrideSourcePos.HasValue)
            {
                sourcePos = overrideSourcePos.Value;
            }
            else
            {
                sourcePos = self.floorPos;
            }
            newTargetMoveToPos += Vector3.up * 0.1f;    //shift upwards to pad for nav sampling
                                                        //Find closest point to floor
            Vector3? testHit = GamePhysics.getRaycastPos(newTargetMoveToPos, -Vector3.up, 3.0f, Instance.wallsMask);
            if (testHit.HasValue)
            {
                newTargetMoveToPos = testHit.Value;
            }
            else
            {
                Debug.LogError("Could not hit raycast for targetPosition");
                Debug.DrawLine(newTargetMoveToPos, newTargetMoveToPos + Vector3.up * 0.5f, Color.red, 3.0f);
                return null;
            }
            if (!isOnWalkableFloor(newTargetMoveToPos))
            {
                Debug.LogError("No nearest target sample found ");
                Tools.DrawCross(newTargetMoveToPos, Color.red, 0.2f);
                Debug.DrawLine(newTargetMoveToPos, newTargetMoveToPos + Vector3.up * 0.5f, Color.red, 3.0f);
                return null;
            }
            Vector3 targetNavPos = navTest.position;
            if (!NavMesh.SamplePosition(sourcePos, out navTest, 1.0f, NavMesh.AllAreas))
            {
                Debug.LogError("No nearest loli nav position found");
                Debug.DrawLine(newTargetMoveToPos, newTargetMoveToPos + Vector3.up * 0.5f, Color.red, 3.0f);
                return null;
            }

            NavMeshPath newPath = new NavMeshPath();
            if (NavMesh.CalculatePath(navTest.position, targetNavPos, NavMesh.AllAreas, newPath))
            {
                Debug.DrawLine(newTargetMoveToPos, newTargetMoveToPos + Vector3.up * 0.5f, Color.green, 3.0f);
                return newPath.corners;
            }

            Debug.LogError("No path found");
            Debug.DrawLine(newTargetMoveToPos, newTargetMoveToPos + Vector3.up * 0.5f, Color.red, 3.0f);
            return null;
        }

        public override void OnAnimationChange(Loli.Animation oldAnim, Loli.Animation newAnim)
        {

            locomotionInfo = Loli.GetLegSpeedInfo(newAnim);
            if (locomotionInfo == null)
            {
                locomotionInfo = zeroLocomotionInfo;
            }
        }
    }

}