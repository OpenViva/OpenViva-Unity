using UnityEngine;


namespace viva
{


    public class CharacterTriggerCallback : MonoBehaviour
    {

        public enum Type
        {
            VIEW,
            RIGHT_INDEX_FINGER,
            LEFT_INDEX_FINGER,
            RIGHT_PALM,
            LEFT_PALM,
            ROOT,
            HEAD,
            PLAYER_PROXIMITY
        }

        [SerializeField]
        private Character m_owner;
        public Character owner { get { return m_owner; } }
        [SerializeField]
        private Type m_collisionPart;
        public Type collisionPart { get { return m_collisionPart; } }


        private void OnTriggerEnter(Collider collider)
        {
            owner.OnCharacterTriggerEnter(this, collider);
        }

        private void OnTriggerStay(Collider collider)
        {
            owner.OnCharacterTriggerStay(this, collider);
        }

        private void OnTriggerExit(Collider collider)
        {
            owner.OnCharacterTriggerExit(this, collider);
        }
    }

}