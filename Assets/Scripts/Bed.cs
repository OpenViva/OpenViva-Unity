using UnityEngine;

namespace viva
{


    public class Bed : Mechanism
    {

        [SerializeField]
        private bool boundaryNavSearchPosX = true;
        [SerializeField]
        private bool boundaryNavSearchNegX = true;
        [SerializeField]
        private bool boundaryNavSearchPosZ = true;
        [SerializeField]
        private bool boundaryNavSearchNegZ = true;

        [SerializeField]
        private BoxCollider m_shape;
        public BoxCollider shape { get { return m_shape; } }
        [SerializeField]
        private Vector3 sleepingSpaceCenter;
        [SerializeField]
        private Vector2 sleepingSpaceRange;
        [SerializeField]
        private float sleepingSpaceYaw;
        [SerializeField]
        private DynamicBonePlaneCollider dynamicBonePlane;
        [SerializeField]
        private AudioClip m_jumpOnSound;
        public AudioClip jumpOnSound { get { return m_jumpOnSound; } }
        [SerializeField]
        private AudioClip m_rollOnSound;
        public AudioClip rollOnSound { get { return m_rollOnSound; } }
        [SerializeField]
        private Bed[] bedGroup;

        public FilterUse filterUse { get; private set; } = new FilterUse();


        public override void OnMechanismAwake()
        {
            // if( transform.parent.name == "futon (5)" ){
            // GameDirector.player.vivaControls.Keyboard.wave.performed += delegate{
            //     for( int i=1; i<GameDirector.characters.objects.Count; i++ ){
            //         var loli = GameDirector.characters.objects[i] as Loli;
            //         if( loli ){
            //             AttemptCommandUse( loli, null );
            //         }
            //     }
            // };
            // }
        }

        public override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.0f, 1.0f, 0.0f, 0.8f);
            Gizmos.DrawCube(sleepingSpaceCenter, new Vector3(sleepingSpaceRange.x, 0.0f, sleepingSpaceRange.y));

            Gizmos.color = new Color(0.0f, 1.0f, 0.0f, 0.8f);
            Vector3 min = shape.bounds.min;
            min.y += 0.05f;
            Vector3 max = shape.bounds.max;
            max.y += 0.05f;
            if (boundaryNavSearchPosX)
            {
                Gizmos.DrawCube((new Vector3(max.x, min.y, max.z) + new Vector3(max.x, min.y, min.z)) / 2.0f,
                    new Vector3(0.05f, 0.05f, shape.bounds.size.z));
            }
            if (boundaryNavSearchNegX)
            {

                Gizmos.DrawCube((new Vector3(min.x, min.y, max.z) + new Vector3(min.x, min.y, min.z)) / 2.0f,
                    new Vector3(0.05f, 0.05f, shape.bounds.size.z));
            }
            if (boundaryNavSearchPosZ)
            {
                Gizmos.DrawCube((new Vector3(min.x, min.y, max.z) + new Vector3(max.x, min.y, max.z)) / 2.0f,
                    new Vector3(shape.bounds.size.x, 0.05f, 0.05f));
            }
            if (boundaryNavSearchNegZ)
            {
                Gizmos.DrawCube((new Vector3(min.x, min.y, min.z) + new Vector3(max.x, min.y, min.z)) / 2.0f,
                    new Vector3(shape.bounds.size.x, 0.05f, 0.05f));
            }

            Vector3 lineCenter = shape.center;
            lineCenter.y += shape.size.y;
            Gizmos.DrawLine(lineCenter, lineCenter + Quaternion.Euler(0.0f, sleepingSpaceYaw, 0.0f) * Vector3.forward);
        }

        public DynamicBonePlaneCollider GetDynamicBonePlane()
        {
            return dynamicBonePlane;
        }

        public LocomotionBehaviors.NavSearchLine[] CreateBoundaryNavSearchLines(bool fromOutside)
        {

            float outside = System.Convert.ToInt32(fromOutside) * 2 - 1;
            Vector3 min = shape.bounds.min;
            min.x -= 0.2f * outside;
            min.y += 0.05f;
            min.z -= 0.2f * outside;
            Vector3 max = shape.bounds.max;
            max.x += 0.2f * outside;
            max.y += 0.05f;
            max.z += 0.2f * outside;

            float localGroundHeight;
            if (fromOutside)
            {
                localGroundHeight = min.y;
            }
            else
            {
                localGroundHeight = max.y;
            }

            int sides = 0;
            sides += System.Convert.ToInt32(boundaryNavSearchPosX);
            sides += System.Convert.ToInt32(boundaryNavSearchNegX);
            sides += System.Convert.ToInt32(boundaryNavSearchPosZ);
            sides += System.Convert.ToInt32(boundaryNavSearchNegZ);

            LocomotionBehaviors.NavSearchLine[] navSearchLines = new LocomotionBehaviors.NavSearchLine[sides];
            sides = 0;
            if (boundaryNavSearchPosX)
            {
                navSearchLines[sides++] = new LocomotionBehaviors.NavSearchLine(
                    new Vector3(max.x, localGroundHeight, max.z),
                    new Vector3(max.x, localGroundHeight, min.z),
                    shape.bounds.extents.y + 0.2f,
                    4,
                    0.4f
                );
            }
            if (boundaryNavSearchNegX)
            {
                navSearchLines[sides++] = new LocomotionBehaviors.NavSearchLine(
                    new Vector3(min.x, localGroundHeight, min.z),
                    new Vector3(min.x, localGroundHeight, max.z),
                    shape.bounds.extents.y + 0.2f,
                    4,
                    0.4f
                );
            }
            if (boundaryNavSearchPosZ)
            {
                navSearchLines[sides++] = new LocomotionBehaviors.NavSearchLine(
                    new Vector3(min.x, localGroundHeight, max.z),
                    new Vector3(max.x, localGroundHeight, max.z),
                    shape.bounds.extents.y + 0.2f,
                    4,
                    0.4f
                );
            }
            if (boundaryNavSearchNegZ)
            {
                navSearchLines[sides++] = new LocomotionBehaviors.NavSearchLine(
                    new Vector3(max.x, localGroundHeight, min.z),
                    new Vector3(min.x, localGroundHeight, min.z),
                    shape.bounds.extents.y + 0.2f,
                    4,
                    0.4f
                );
            }

            return navSearchLines;
        }

        public void GetRandomSleepingTransform(out Vector3 position, out Vector3 forward)
        {

            Vector3 randomRange = Vector3.zero;
            randomRange.x = (Random.value - 0.5f) * sleepingSpaceRange.x;
            randomRange.z = (Random.value - 0.5f) * sleepingSpaceRange.y;

            sleepingSpaceCenter.y = shape.center.y + shape.size.y * 0.5f;
            position = transform.TransformPoint(sleepingSpaceCenter + randomRange);
            forward = transform.rotation * Quaternion.Euler(0.0f, sleepingSpaceYaw, 0.0f) * Vector3.forward;
        }

        public override bool AttemptCommandUse(Loli targetLoli, Character commandSource)
        {
            if (targetLoli == null)
            {
                return false;
            }
            if (!CanHost(targetLoli))
            {
                foreach (Bed bed in bedGroup)
                {
                    if (bed == this || !bed.CanHost(targetLoli))
                    {
                        continue;
                    }
                    return targetLoli.active.sleeping.AttemptBeginSleeping(bed);
                }
            }
            else
            {
                return targetLoli.active.sleeping.AttemptBeginSleeping(this);
            }
            return false;
        }

        public bool CanHost(Character character)
        {
            return filterUse.owner == null;
        }

        public override void EndUse(Character targetCharacter)
        {
        }

    }

}