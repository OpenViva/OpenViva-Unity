using UnityEngine;

namespace viva
{


    public partial class Loli : Character
    {

        public enum BodyPart
        {
            NONE,       //keep first index as NULL
            HEAD_SC,
            TUMMY_CC,
            RIGHT_FOOT_CC,
            LEFT_FOOT_CC,
        }

        [SerializeField]
        public Collider[] colliderBodyParts = new Collider[System.Enum.GetValues(typeof(BodyPart)).Length];
        private float lastCollisionSoundTime = 1.0f;


        public BodyPart IdentifyCollider(Collider test)
        {

            for (int i = 0; i < colliderBodyParts.Length; i++)
            {
                if (colliderBodyParts[i] == test)
                {
                    return (BodyPart)i;
                }
            }
            return BodyPart.NONE;
        }

        public Collider GetColliderBodyPart(BodyPart part)
        {
            return colliderBodyParts[(int)part];
        }

        private void PlayPhysicsCollisionSound(AudioClip clip, bool playStartleSound)
        {
            if (Time.time - lastCollisionSoundTime > 0.1f)
            {
                if (playStartleSound)
                {
                    if (!IsSpeaking(VoiceLine.STARTLE_SHORT))
                    {
                        Speak(VoiceLine.STARTLE_SHORT);
                    }
                }
                lastCollisionSoundTime = Time.time;
                SoundManager.main.RequestHandle(floorPos).PlayOneShot(clip);
            }
        }

        public override void OnCharacterTriggerEnter(CharacterTriggerCallback ccc, Collider collider)
        {
            Item item = collider.GetComponent<Item>();
            if (item == null)
            {
                return;
            }
            onCharacterTriggerEnter?.Invoke(ccc, collider);
            switch (ccc.collisionPart)
            {
                case CharacterTriggerCallback.Type.VIEW:
                    OnEnterViewItem(item);
                    break;
                case CharacterTriggerCallback.Type.PLAYER_PROXIMITY:
                    passive.OnItemTriggerEnter(item);
                    break;
            }
        }

        public override void OnCharacterTriggerExit(CharacterTriggerCallback ccc, Collider collider)
        {
            Item item = collider.GetComponent<Item>();
            if (item == null)
            {
                return;
            }
            switch (ccc.collisionPart)
            {
                case CharacterTriggerCallback.Type.VIEW:
                    OnExitViewItem(item);
                    break;
                case CharacterTriggerCallback.Type.PLAYER_PROXIMITY:
                    passive.OnItemTriggerExit(item);
                    break;
            }
        }

        public override void OnCharacterCollisionEnter(CharacterCollisionCallback ccc, Collision collision)
        {

            //HACK FOR v0.7a (prevents null collision checks before model has been initialized)
            //TODO: Convert ModelRebuilder to function to create loli ONLY when it's ready
            if (active == null)
            {
                return;
            }
            onCharacterCollisionEnter?.Invoke(ccc, collision);

            if (collision.gameObject.layer == WorldUtil.outofbounds)
            {
                TeleportToSpawn(GameDirector.instance.loliRespawnPoint.transform.position, transform.rotation);
            }

            bool isNotFoot = ccc.collisionPart != CharacterCollisionCallback.Type.LEFT_FOOT && ccc.collisionPart != CharacterCollisionCallback.Type.RIGHT_FOOT;
            if (!hasBalance || isNotFoot)
            {
                if (!active.horseback.isOnHorse)
                {
                    if (collision.relativeVelocity.sqrMagnitude > 36.0f)
                    {
                        OnHitHard(ccc, collision);
                        PlayPhysicsCollisionSound(GameDirector.instance.loliSettings.bodyImpactHardSound.GetRandomAudioClip(), true);
                    }
                    else if (collision.relativeVelocity.sqrMagnitude > 12.0f)
                    {
                        PlayPhysicsCollisionSound(GameDirector.instance.loliSettings.bodyImpactSoftSound.GetRandomAudioClip(), false);
                    }
                }
            }

            //pass trigger event to actives
            var item = collision.gameObject.GetComponent<Item>();
            if (item != null && item.mainOwner != this)
            {
                switch (ccc.collisionPart)
                {
                    case CharacterCollisionCallback.Type.HEAD:
                        //ignore other head collisions since they can be kisses not pokes
                        //var item = collision.gameObject.GetComponent<Item>();
                        if (!passive.headpat.AttemptBeginHeadpat(item))
                        {
                            passive.poke.AttemptFacePoke(item);
                        }
                        break;
                    case CharacterCollisionCallback.Type.TORSO:
                        //viva.DevTools.LogExtended("Torso Poke!", true, true);
                        passive.poke.AttemptTummyPoke(item);
                        break;
                    // just a test, redirected to face poke
                    case CharacterCollisionCallback.Type.LEFT_FOOT:
                        //viva.DevTools.LogExtended("Left Foot Poke from " + item.mainOwner + ", Source is player: " + sourceIsPlayer, true, true);
                        passive.poke.AttemptFootPoke(item);
                        break;
                    // just a test, redirected to face poke
                    case CharacterCollisionCallback.Type.RIGHT_FOOT:
                        //viva.DevTools.LogExtended("Right Foot Poke from " + item.mainOwner + ", Source is player: " + sourceIsPlayer, true, true);
                        passive.poke.AttemptFootPoke(item);
                        break;
                }
            }
        }

        public delegate void CollisionCallback(CharacterCollisionCallback ccc, Collision collision);

        public override void OnCharacterCollisionExit(CharacterCollisionCallback ccc, Collision collision)
        {
        }

        private void ApplyHeadModelCollisions()
        {

            headRigidBody.transform.position = head.position;
            headRigidBody.transform.rotation = head.rotation;

            Transform headContainer = headRigidBody.transform;
            Vector3 headpatLocalSphere = headContainer.InverseTransformPoint(anchor.TransformPoint(new Vector3(
                headModel.headpatWorldSphere.x,
                headModel.headpatWorldSphere.y,
                headModel.headpatWorldSphere.z
            )));
            float headpatLocalSphereRadius = headModel.headpatWorldSphere.w;
            if (headpatLocalSphereRadius == 0.0f)
            {   //provide default headpat sphere
                headpatLocalSphereRadius = 0.121875f;
                headpatLocalSphere = Vector3.up * 0.1575f;
            }

            //top of head
            SphereCollider topOfHeadSC = colliderBodyParts[(int)BodyPart.HEAD_SC] as SphereCollider;
            topOfHeadSC.radius = headpatLocalSphereRadius;
            topOfHeadSC.center = headpatLocalSphere;
        }
    }

}