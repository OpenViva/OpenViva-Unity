using System.Collections.Generic;
using UnityEngine;

namespace viva
{

    [RequireComponent(typeof(Animator))]
    public class Chicken : Mechanism
    {

        public enum Movement
        {
            NONE,
            AVOID_TERRAIN,
            AVOID_TRANSFORM
        }

        [SerializeField]
        private Transform m_pelvis = null;
        public Transform pelvis { get { return m_pelvis; } }
        [SerializeField]
        private ChickenSettings m_chickenSettings = null;
        public ChickenSettings chickenSettings { get { return m_chickenSettings; } }
        [SerializeField]
        private AudioSource soundSource = null;
        [SerializeField]
        private GameObject untamedItemCollisionExpansion = null;
        [SerializeField]
        private ChickenItem m_chickenItem = null;
        public ChickenItem chickenItem { get { return m_chickenItem; } }

        private List<Transform> avoidTransforms = new List<Transform>();
        private Animator animator;
        private float awarenessUpdate = 0.0f;
        private float wallProx = 2.0f;
        private float speed = 0.0f;
        private float currentAccel = 0.0f;
        private float turnSpeedDecay = 0.0f;
        private RaycastHit hitInfo = new RaycastHit();
        private Vector3 lastStandPos;
        private Plane? standingPlane;
        private float targetDeg;
        private float bukTimer = 0.0f;
        private Movement movement = Movement.NONE;
        private float randomDegTime = 0.0f;
        private float randomDegree = 0.0f;

        private static int chickenPhysicsMask;
        private static int speedID = Animator.StringToHash("speed");
        private static int locomotionID = Animator.StringToHash("Locomotion");
        private static int idleID = Animator.StringToHash("Idle1");

        public void UpdateTamedStatus()
        {

            if (!chickenItem.tamed)
            {
                return;
            }
            //change LOD children
            for (int i = 0; i < 2; i++)
            {
                SkinnedMeshRenderer smr = gameObject.transform.GetChild(i).GetComponent<SkinnedMeshRenderer>();
                if (smr == null)
                {
                    continue;
                }
                smr.material = chickenSettings.tamedChickenMaterial;
            }
            //will no longer run away
            avoidTransforms.Clear();
            untamedItemCollisionExpansion.SetActive(false);
            SetAnimation(idleID, 0.4f);
        }

        public void OnPickedUp()
        {
            SetAnimation(idleID, 0.4f);
            soundSource.Stop();
            soundSource.clip = chickenSettings.bukku.GetRandomAudioClip();
            soundSource.Play();

            this.GetComponent<SphereCollider>().isTrigger = true;
        }

        public void OnDropped()
        {
            this.GetComponent<SphereCollider>().isTrigger = false;
        }


        public override void OnMechanismAwake()
        {
            animator = gameObject.GetComponent<Animator>();
            lastStandPos = transform.position;
            GameDirector.mechanisms.Add(this);

            chickenPhysicsMask = WorldUtil.wallsMask | WorldUtil.wallsStaticForCharactersMask;
            animator.speed = 0.95f + UnityEngine.Random.value * 0.1f;
            bukTimer = UnityEngine.Random.value;

            UpdateTamedStatus();
        }

        public override bool AttemptCommandUse(Loli targetLoli, Character commandSource)
        {
            if (targetLoli == null)
            {
                return false;
            }
            if (commandSource == null)
            {
                return false;
            }
            return targetLoli.active.chickenChase.AttemptChaseChicken(this);
        }

        public override void EndUse(Character targetCharacter)
        {
        }

        public override void OnMechanismTriggerEnter(MechanismCollisionCallback self, Collider collider)
        {
            if (chickenItem.tamed)
            {
                return;
            }
            Item item = Tools.SearchTransformAncestors<Item>(collider.transform);
            if (item == null)
            {
                return;
            }
            if (item.settings.itemType != Item.Type.CHARACTER)
            {
                return;
            }
            if (item.mainOwner != null)
            {
                avoidTransforms.Add(item.mainOwner.rightHandState.fingerAnimator.hand);
                avoidTransforms.Add(item.mainOwner.leftHandState.fingerAnimator.hand);
            }
        }
        public override void OnMechanismTriggerExit(MechanismCollisionCallback self, Collider collider)
        {

            if (chickenItem.tamed)
            {
                return;
            }
            Item item = collider.GetComponent<Item>();
            if (item == null)
            {
                return;
            }
            if (item.settings.itemType != Item.Type.CHARACTER)
            {
                return;
            }
            if (item.mainOwner != null)
            {
                avoidTransforms.Remove(item.mainOwner.rightHandState.fingerAnimator.hand);
                avoidTransforms.Remove(item.mainOwner.leftHandState.fingerAnimator.hand);
            }
        }

        private void RecalculateStandPosition()
        {

            Vector3 normalSum = Vector3.zero;
            Vector3 pointSum = Vector3.zero;
            Vector3 pos = transform.position + transform.up * 0.2f;
            int hits = 0;
            for (int i = 0; i < 2; i++)
            {
                Vector3 dir = -transform.up;
                dir += transform.right * (UnityEngine.Random.value - 0.5f) * chickenSettings.standCheckRandomRadius;
                dir += transform.forward * (UnityEngine.Random.value - 0.5f) * chickenSettings.standCheckRandomRadius;
                if (GamePhysics.GetRaycastInfo(pos, dir, chickenSettings.standCheckDownDistance, chickenPhysicsMask))
                {
                    Debug.DrawLine(pos, pos + dir.normalized * chickenSettings.standCheckDownDistance, Color.green, 0.05f);
                    hits++;
                    normalSum += GamePhysics.result().normal;
                    pointSum += GamePhysics.result().point;
                }
                else
                {
                    Debug.DrawLine(pos, pos + dir.normalized * chickenSettings.standCheckDownDistance, Color.red, 0.05f);
                }
            }
            if (hits > 0)
            {
                Vector3 avgNormal = (normalSum / hits).normalized;
                if (avgNormal.y < 0.6f)
                {
                    standingPlane = null;
                }
                else
                {
                    standingPlane = new Plane(avgNormal, pointSum / hits);
                }
            }
            else
            {
                standingPlane = null;
            }
        }

        private void StandOnPlane(float yawDeg)
        {
            Quaternion yawRotation = Quaternion.Euler(0.0f, yawDeg, 0.0f);
            Vector3 closest = standingPlane.Value.ClosestPointOnPlane(transform.position);
            Vector3 standingTarget = closest + standingPlane.Value.normal * chickenSettings.standingDistance;
            float posLerp = Mathf.Clamp01(Time.deltaTime * chickenSettings.standPositionLerpStrength);
            float rotLerp = Mathf.Clamp01(Time.deltaTime * chickenSettings.standRotationLerpStrength);
            transform.position = Vector3.LerpUnclamped(transform.position, standingTarget, posLerp);
            Vector3 planeForward = Vector3.ProjectOnPlane(yawRotation * Vector3.forward, standingPlane.Value.normal);
            transform.rotation = Quaternion.LerpUnclamped(transform.rotation, Quaternion.LookRotation(planeForward, standingPlane.Value.normal), rotLerp);
        }

        public override void OnMechanismFixedUpdate()
        {

            Rigidbody rigidBody = chickenItem.rigidBody;

            if (Vector3.SqrMagnitude(transform.position - lastStandPos) > chickenSettings.standRecalculateThreshold)
            {
                RecalculateStandPosition();
                lastStandPos = transform.position;
            }
            if (standingPlane != null)
            {
                rigidBody.useGravity = false;
                FixedUpdatePhysicsMovement(rigidBody);
                if (speed <= 0.001f)
                {
                    rigidBody.velocity = Vector3.zero;
                }
            }
            else
            {
                if (!rigidBody.useGravity && !soundSource.isPlaying)
                {
                    soundSource.Stop();
                    soundSource.clip = chickenSettings.bukku.GetRandomAudioClip();
                    soundSource.Play();
                }
                rigidBody.useGravity = true;
            }
        }

        private void FixedUpdatePhysicsMovement(Rigidbody rigidBody)
        {

            FixedUpdateAwareness();

            if (speed > 0.15f)
            {

                rigidBody.velocity = transform.forward * speed;
                if (!IsCurrentAnimation(locomotionID) && !IsNextAnimation(locomotionID))
                {
                    SetAnimation(locomotionID, 0.2f);
                }
                bukTimer += Time.deltaTime * speed;
                rigidBody.angularVelocity = Vector3.zero;
            }
            else
            {
                rigidBody.velocity = Vector3.zero;

                if (!IsCurrentAnimation(idleID) && !IsNextAnimation(idleID))
                {
                    SetAnimation(idleID, 0.2f);
                }
                bukTimer += Time.deltaTime * 0.2f;
            }
            if (bukTimer > 1.0f)
            {
                bukTimer %= 1.0f;
                if (!soundSource.isPlaying)
                {
                    soundSource.PlayOneShot(chickenSettings.buk.GetRandomAudioClip());
                }
            }
        }

        private void FixedUpdateAwareness()
        {
            float currDeg = Mathf.Atan2(transform.forward.x, transform.forward.z) * Mathf.Rad2Deg;

            awarenessUpdate += Time.deltaTime;

            if (awarenessUpdate > 0.3f)
            {
                awarenessUpdate %= 0.3f;

                //only check for walls if moving
                if (speed > 0.15f)
                {
                    targetDeg = FindAvoidTerrainDirection(currDeg);
                }
                else
                {
                    targetDeg = currDeg;
                }
                if (targetDeg == currDeg)
                {
                    targetDeg = FindAvoidSphereTransformsDirection(currDeg);
                    if (targetDeg != currDeg)
                    {
                        movement = Movement.AVOID_TRANSFORM;
                        if (Time.time - randomDegTime > 1.0f)
                        {
                            randomDegTime = Time.time;
                            randomDegTime = 0.0f;
                            randomDegree = (-1.0f + UnityEngine.Random.value * 2.0f) * chickenSettings.randomChaseDirectionChange;
                        }
                        targetDeg += randomDegree;
                    }
                    else
                    {
                        movement = Movement.NONE;
                    }
                }
                else
                {
                    randomDegree = 0.0f;
                    movement = Movement.AVOID_TERRAIN;
                }
                if (chickenItem.tamed)
                {
                    chickenItem.UpdateEggTimer();
                }
            }
            UpdateMovement(currDeg);
        }

        private void UpdateMovement(float currDeg)
        {

            switch (movement)
            {
                case Movement.AVOID_TERRAIN:
                    turnSpeedDecay = Mathf.Min(turnSpeedDecay + Time.deltaTime * 2.0f, chickenSettings.maxTurnSpeedDecay);
                    currDeg = Mathf.LerpAngle(currDeg, targetDeg, turnSpeedDecay);
                    break;
                case Movement.AVOID_TRANSFORM:
                    currentAccel = Mathf.Min(currentAccel + Time.deltaTime * chickenSettings.accel, chickenSettings.maxAccel);
                    currDeg = Mathf.LerpAngle(currDeg, targetDeg, Time.deltaTime * chickenSettings.turnSpeed);
                    break;
            }
            turnSpeedDecay = Mathf.Max(turnSpeedDecay - Time.deltaTime, 0.2f);
            currentAccel *= 1.0f - turnSpeedDecay;

            speed = Mathf.Min(chickenSettings.maxSpeed, speed + currentAccel);
            animator.SetFloat(speedID, speed / chickenSettings.maxSpeed);
            speed *= chickenSettings.friction;
            StandOnPlane(currDeg);
        }

        private float FindAvoidSphereTransformsDirection(float currTargetDeg)
        {
            //no wall detected
            wallProx = Mathf.Min(wallProx + 0.05f, chickenSettings.proxMax);

            //avoid spheres around avoidTransforms
            float sphereSumWeight = 0.0f;
            Vector3 sphereAvoidSum = Vector3.zero;
            foreach (Transform sphere in avoidTransforms)
            {
                Vector3 diff = sphere.position - transform.position;
                float dist = diff.magnitude;
                if (dist < 3.0f)
                {

                    diff.y = 0.0f;
                    sphereAvoidSum += -diff.normalized;
                    sphereSumWeight += 3.0f - dist;
                }
            }
            if (sphereSumWeight > 0.0f)
            {
                Vector3 avgSphereAvoidNormal = sphereAvoidSum / sphereSumWeight;
                Debug.DrawLine(transform.position + Vector3.up * 0.5f, transform.position + Vector3.up * 0.5f + avgSphereAvoidNormal, Color.white, 2.5f);
                return Mathf.Atan2(avgSphereAvoidNormal.x, avgSphereAvoidNormal.z) * Mathf.Rad2Deg;
            }
            return currTargetDeg;
        }

        private bool SampleTerrainDanger(Vector3 pos, Vector3 dir)
        {
            //avoid walls
            if (Physics.Raycast(pos, dir, out hitInfo, wallProx, chickenPhysicsMask))
            {
                return true;
            }
            //avoid cliffs
            Debug.DrawLine(pos + dir * wallProx, pos + dir * wallProx + Vector3.down * chickenSettings.dropHeightMin, Color.red, 2.3f);
            if (!Physics.Raycast(pos + dir * wallProx, Vector3.down, out hitInfo, chickenSettings.dropHeightMin, chickenPhysicsMask))
            {
                hitInfo.normal = -dir;
                hitInfo.distance = wallProx * 0.9f;
                Debug.DrawLine(pos + dir * wallProx, pos + dir * wallProx + Vector3.up * 0.2f, Color.blue, 0.5f);
                return true;
            }
            return false;
        }

        public bool IsNextAnimation(int hash)
        {
            return animator.GetNextAnimatorStateInfo(0).shortNameHash == hash;
        }

        public bool IsCurrentAnimation(int hash)
        {
            return animator.GetCurrentAnimatorStateInfo(0).shortNameHash == hash;
        }
        public void SetAnimation(int hash, float transitionTime)
        {
            animator.CrossFade(hash, transitionTime / GetLayerAnimLength(0), 0, 0.0f);
        }

        public float GetLayerAnimLength(int layer)
        {
            if (animator.IsInTransition(layer))
            {
                return animator.GetNextAnimatorStateInfo(layer).length;
            }
            else
            {
                return animator.GetCurrentAnimatorStateInfo(layer).length;
            }
        }

        private float FindAvoidTerrainDirection(float currTargetDeg)
        {

            //avoid walls
            Vector3? fromTerrainNormal = null;
            for (int i = 0; i < 2; i++)
            {   //0,1
                int side = i * 2 - 1;   //-1,1
                Vector3 viewPos = m_pelvis.transform.position;
                Vector3 viewDir = (transform.forward + transform.right * side * 0.5f).normalized;
                Debug.DrawLine(viewPos, viewPos + viewDir * wallProx, Color.green, 0.3f);
                if (SampleTerrainDanger(viewPos, viewDir))
                {
                    if (!fromTerrainNormal.HasValue)
                    {
                        fromTerrainNormal = -viewDir.normalized;
                    }
                    else
                    {
                        fromTerrainNormal = (fromTerrainNormal - viewDir.normalized) / 2.0f;
                    }
                }
            }

            //refine from terrain normal
            if (fromTerrainNormal.HasValue)
            {
                int subHits = 1;    //count the first raycast
                float seekVariation = 0.5f + UnityEngine.Random.value;
                float avgDistance = 0.0f;
                for (int i = 0; i < 8; i++)
                {
                    float side = (float)i / 4.0f - 1;   //-1.0f ~ 1.0f
                    Vector3 viewDir = (transform.forward + transform.right * side * seekVariation + transform.up * (UnityEngine.Random.value - 0.5f) * 0.5f).normalized;
                    Debug.DrawLine(m_pelvis.transform.position, m_pelvis.transform.position + viewDir * wallProx, Color.yellow, 1.6f);
                    if (SampleTerrainDanger(m_pelvis.transform.position, viewDir))
                    {
                        fromTerrainNormal += hitInfo.normal;
                        subHits++;
                        avgDistance += hitInfo.distance;
                    }
                }
                fromTerrainNormal /= subHits;
                avgDistance /= subHits;

                //reflect off of wall
                if (fromTerrainNormal.Value.x != 0.0f || fromTerrainNormal.Value.z != 0.0f)
                {

                    Vector3 wallReflection = Vector3.Reflect(transform.forward, fromTerrainNormal.Value);
                    wallProx = Mathf.Min(Mathf.Max(chickenSettings.proxMin, avgDistance), wallProx);

                    // Debug.DrawLine( transform.position+Vector3.up*0.6f, transform.position+Vector3.up*0.6f+wallReflection, Color.cyan, 2.5f );
                    float reflectedDeg = Mathf.Atan2(wallReflection.x, wallReflection.z) * Mathf.Rad2Deg;
                    return reflectedDeg;
                }
            }
            return currTargetDeg;
        }
    }

}