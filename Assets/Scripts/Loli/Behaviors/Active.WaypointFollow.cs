using System.Collections.Generic;
using UnityEngine;


namespace viva
{


    public partial class WaypointFollowBehavior : ActiveBehaviors.ActiveTask
    {

        private Waypoints waypoints = null;
        private int currentWaypointIndex = 0;
        private List<int> visitedNodes = new List<int>();
        private bool hasReachedNextWaypoint = false;
        private LocomotionBehaviors.PathCache employmentPathID = new LocomotionBehaviors.PathCache();

        public WaypointFollowBehavior(Loli _self) : base(_self, ActiveBehaviors.Behavior.WAYPOINT_FOLLOW, null)
        {

        }

        public override void OnUpdate()
        {
            GoToWaypointDestination();
        }

        public bool AttemptFollowWaypoints(Waypoints newWaypoints)
        {
            if (newWaypoints == null)
            {
                Debug.LogError("[WaypointFollow] Cannot begin with a null waypoints!");
                return false;
            }
            waypoints = newWaypoints;
            currentWaypointIndex = waypoints.FindNearestWaypoint(self.floorPos);
            MoveToNextWaypointNode();
            visitedNodes.Clear();

            self.active.SetTask(this, null);
            return true;
        }

        private void OnReachWaypoint()
        {
            hasReachedNextWaypoint = true;
            MoveToNextWaypointNode();
        }

        private void MoveToNextWaypointNode()
        {
            if (waypoints == null)
            {
                Debug.LogError("[Employment] Could not move to next waypoint");
                self.active.SetTask(self.active.idle, false);
                return;
            }
            var currentNode = waypoints.nodes[currentWaypointIndex];
            //select random index
            var randomized = new List<int>(currentNode.links);
            for (int i = 0; i < randomized.Count; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, randomized.Count - 1);
                int old = randomized[i];
                randomized[i] = randomized[randomIndex];
                randomized[randomIndex] = old;
            }
            int acceptedLink = -1;
            for (int i = 0; i < randomized.Count; i++)
            {
                int link = randomized[i];
                if (!visitedNodes.Contains(link))
                {
                    acceptedLink = link;
                    visitedNodes.Add(link);
                    break;
                }
            }
            if (acceptedLink >= 0)
            {
                currentWaypointIndex = acceptedLink;
                hasReachedNextWaypoint = false;
            }
        }

        private bool IsFullyIdle()
        {
            return self.IsCurrentAnimationIdle() && !self.locomotion.isMoveToActive();
        }

        private void GoToWaypointDestination()
        {
            if (waypoints == null)
            {
                self.active.SetTask(self.active.idle, false);
                return;
            }
            if (!hasReachedNextWaypoint)
            {
                if (self.hasBalance && IsFullyIdle())
                {
                    Vector3 waypointPos = waypoints.transform.TransformPoint(
                        waypoints.nodes[currentWaypointIndex].position
                    );
                    Vector2 randomCircle = Random.insideUnitCircle;
                    waypointPos += new Vector3(randomCircle.x, 0.0f, randomCircle.y);
                    var path = self.locomotion.GetNavMeshPath(waypointPos);
                    if (path == null)
                    {
                        currentWaypointIndex = waypoints.FindNearestWaypoint(self.floorPos);
                        visitedNodes.Clear();
                    }
                    else
                    {
                        self.locomotion.FollowPath(path, OnReachWaypoint, employmentPathID);
                    }
                }
            }
        }
    }

}