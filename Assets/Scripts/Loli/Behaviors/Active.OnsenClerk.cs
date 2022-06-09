using System.Collections.Generic;


namespace viva
{

    public class OnsenClerkBehavior : ActiveBehaviors.ActiveTask
    {

        public delegate void ClerkSessionCallback(AutonomyEmpty clerkSession);

        public class ClientSession : SerializedTaskData
        {
            public readonly AutonomyEmpty clerkSessionProgress;
            public readonly Character client;
            private readonly ClerkSessionCallback onStartClerkSession;

            public ClientSession(Loli self, Character _client, ClerkSessionCallback _onStartClerkSession)
            {
                clerkSessionProgress = new AutonomyEmpty(self.autonomy, "clerk session progress", delegate { return null; });
                client = _client;
                onStartClerkSession = _onStartClerkSession;
            }

            public void BeginSession()
            {
                onStartClerkSession?.Invoke(clerkSessionProgress);
                clerkSessionProgress.FlagForSuccess();
            }
        }

        public class OnsenClerkSession : SerializedTaskData
        {

            [VivaFileAttribute]
            public VivaSessionAsset onsenReceptionAsset { get; set; }
            public OnsenReception onsenReception { get { return onsenReceptionAsset as OnsenReception; } }
        }

        private List<ClientSession> clientSessions = new List<ClientSession>();
        private Towel activeTowel;
        private ClientSession currentClientSession = null;
        public OnsenClerkSession onsenClerkSession { get { return session as OnsenClerkSession; } }


        public OnsenClerkBehavior(Loli _self) : base(_self, ActiveBehaviors.Behavior.ONSEN_CLERK, new OnsenClerkSession())
        {
        }

        public override void OnActivate()
        {

            var moveTo = onsenClerkSession.onsenReception.CreateGoToEmploymentPosition(self);
            if (moveTo != null)
            {
                self.autonomy.SetAutonomy(moveTo);
                moveTo.onSuccess += AttendNextClient;
            }

            GameDirector.player.objectFingerPointer.selectedLolis.Remove(self);
            self.characterSelectionTarget.OnUnselected();
        }

        public override void OnDeactivate()
        {
            ClearAndFailAllActiveClients();
        }

        private void ClearAndFailAllActiveClients()
        {
            foreach (var client in clientSessions)
            {
                client.clerkSessionProgress.FlagForFailure();
            }
            clientSessions.Clear();
        }

        private int FindClientIndex(Character client)
        {
            for (int i = 0; i < clientSessions.Count; i++)
            {
                if (clientSessions[i].client == client)
                {
                    return i;
                }
            }
            return -1;
        }

        public bool AttemptAttendClient(Character newClient, ClerkSessionCallback onStart)
        {
            if (newClient == null || onsenClerkSession.onsenReception == null)
            {
                return false;
            }
            self.active.SetTask(this);

            var clientIndex = FindClientIndex(newClient);
            if (clientIndex == -1)
            {
                clientSessions.Add(new ClientSession(self, newClient, onStart));
            }
            AttendNextClient();
            return true;
        }

        private void AttendNextClient()
        {
            if (currentClientSession == null && clientSessions.Count > 0)
            {
                currentClientSession = clientSessions[0];

                var greetAtReception = new AutonomyPlayAnimation(self.autonomy, "greet at reception", Loli.Animation.STAND_BOW);

                var walkToReception = onsenClerkSession.onsenReception.CreateGoToEmploymentPosition(self);
                if (walkToReception != null)
                {

                    greetAtReception.AddRequirement(walkToReception);
                    greetAtReception.AddRequirement(new AutonomyFaceDirection(self.autonomy, "face reception", delegate (TaskTarget target)
                    {
                        if (currentClientSession.client != null)
                        {
                            target.SetTargetItem(currentClientSession.client.headItem);
                        }
                    }, 1.0f, 30.0f));
                    greetAtReception.onSuccess += AttemptGiveTowelToClient;
                    greetAtReception.onFail += ConfusedAndFinalizeCurrentClient;

                    self.autonomy.SetAutonomy(greetAtReception);
                }
                else
                {
                    ConfusedAndFinalizeCurrentClient();
                }
            }
        }

        private void ConfusedAndFinalizeCurrentClient()
        {
            var playAnim = LoliUtility.CreateSpeechAnimation(self, AnimationSet.CONFUSED, SpeechBubble.INTERROGATION);
            playAnim.onSuccess += delegate { FinalizeCurrentClient(false); };
            self.autonomy.SetAutonomy(playAnim);
        }

        private void GoToEmploymentPositionAndFinalizeCurrentClient(bool success)
        {
            var moveTo = onsenClerkSession.onsenReception.CreateGoToEmploymentPosition(self);
            if (moveTo != null)
            {
                self.autonomy.SetAutonomy(moveTo);
                moveTo.onSuccess += delegate { FinalizeCurrentClient(success); };
            }
            else
            {
                ConfusedAndFinalizeCurrentClient();
            }
        }

        private void AttemptGiveTowelToClient()
        {
            if (onsenClerkSession.onsenReception == null || currentClientSession == null)
            {
                self.active.SetTask(self.active.idle);
                return;
            }

            activeTowel = onsenClerkSession.onsenReception.SpawnNextStorageTowel();
            if (activeTowel == null)
            {
                ConfusedAndFinalizeCurrentClient();
                return;
            }
            var pickUpTowel = new AutonomyPickup(self.autonomy, "pick up towel", activeTowel, self.GetPreferredHandState(activeTowel));
            pickUpTowel.onSuccess += AttemptDeliverTowelToClient;
            pickUpTowel.onFail += delegate { GoToEmploymentPositionAndFinalizeCurrentClient(false); };

            self.autonomy.SetAutonomy(pickUpTowel);
        }

        private void AttemptDeliverTowelToClient()
        {

            if (onsenClerkSession.onsenReception == null || currentClientSession == null)
            {
                self.active.SetTask(self.active.idle);
                return;
            }
            var employeeInfo = onsenClerkSession.onsenReception.FindActiveEmployeeInfo(self);
            if (employeeInfo == null)
            {
                self.active.SetTask(self.active.idle);
                return;
            }
            currentClientSession.BeginSession();    //begin attending
            currentClientSession.clerkSessionProgress.onFail += AttemptReturnTowel;

            var receptionPos = onsenClerkSession.onsenReception.transform.TransformPoint(employeeInfo.localPos);
            var giveToClient = new AutonomyGive(self.autonomy, "give to client", currentClientSession.client, activeTowel);
            giveToClient.onFail += AttemptReturnTowel;

            self.autonomy.SetAutonomy(giveToClient);

            if (currentClientSession.client as Player != null)
            {
                //lead main Player
                giveToClient.onSuccess += AttemptSignalFollowClient;
            }
            else
            {
                //ignore other lolis
                giveToClient.onSuccess += delegate { GoToEmploymentPositionAndFinalizeCurrentClient(true); };
            }
        }

        private void FinalizeCurrentClient(bool success)
        {
            if (currentClientSession != null)
            {
                int clientIndex = FindClientIndex(currentClientSession.client);
                if (clientIndex != -1)
                {
                    if (!success)
                    {
                        currentClientSession.clerkSessionProgress.FlagForFailure();
                    }
                    clientSessions.RemoveAt(clientIndex);
                }
                currentClientSession = null;
                activeTowel = null;
            }

            AttendNextClient();
        }

        private void AttemptReturnTowel()
        {

            if (onsenClerkSession.onsenReception == null)
            {
                self.active.SetTask(self.active.idle);
                return;
            }
            if (activeTowel == null)
            {
                GoToEmploymentPositionAndFinalizeCurrentClient(false);
                return;
            }

            var towelClip = onsenClerkSession.onsenReception.GetNextEmptyStorageTowel();
            if (towelClip == null)
            {
                GoToEmploymentPositionAndFinalizeCurrentClient(false);
                return;
            }
            var returnTowel = new AutonomyDrop(self.autonomy, "drop towel", activeTowel, towelClip.transform.position);
            returnTowel.onSuccess += delegate { if (activeTowel && activeTowel.lastWallClip) { activeTowel.lastWallClip.RackTowel(activeTowel); } };
            returnTowel.onFail += delegate { GoToEmploymentPositionAndFinalizeCurrentClient(false); };

            var goToReception = onsenClerkSession.onsenReception.CreateGoToEmploymentPosition(self);
            if (goToReception != null)
            {
                returnTowel.onSuccess += delegate { GoToEmploymentPositionAndFinalizeCurrentClient(true); };
            }
            else
            {
                ConfusedAndFinalizeCurrentClient();
            }

            self.autonomy.SetAutonomy(returnTowel);
        }

        private void AttemptSignalFollowClient()
        {

            if (onsenClerkSession.onsenReception == null || currentClientSession == null)
            {
                return;
            }

            var playFollowMe = new AutonomyPlayAnimation(self.autonomy, "play follow me", self.rightHandState.occupied ? Loli.Animation.STAND_FOLLOW_ME_LEFT : Loli.Animation.STAND_FOLLOW_ME_RIGHT);

            var goToFollowStart = new AutonomyMoveTo(self.autonomy, "go to follow start", delegate (TaskTarget target)
            {
                target.SetTargetPosition(onsenClerkSession.onsenReception.followMeStart.position);
            },
                0.1f,
                BodyState.STAND
            );

            var faceClient = new AutonomyFaceDirection(self.autonomy, "face client", delegate (TaskTarget target)
            {
                if (currentClientSession.client)
                {
                    target.SetTargetItem(currentClientSession.client.headItem);
                }
            }, 1.0f, 20.0f);

            playFollowMe.AddRequirement(goToFollowStart);
            playFollowMe.AddRequirement(faceClient);

            self.autonomy.SetAutonomy(playFollowMe);

            playFollowMe.onSuccess += AttemptLeadClientToChangingRoom;
            playFollowMe.onFail += delegate { GoToEmploymentPositionAndFinalizeCurrentClient(false); };
        }

        private void AttemptLeadClientToChangingRoom()
        {

            if (onsenClerkSession.onsenReception == null || currentClientSession == null)
            {
                return;
            }

            var waitForClientToGoToChangingRoom = new AutonomySphereBoundary(self.autonomy,
            delegate (TaskTarget source)
            {
                if (currentClientSession.client)
                {
                    source.SetTargetPosition(currentClientSession.client.floorPos);
                }
            },
            delegate (TaskTarget target)
            {
                target.SetTargetPosition(self.head.position);
            }, 3.0f);

            waitForClientToGoToChangingRoom.onSuccess += delegate { GoToEmploymentPositionAndFinalizeCurrentClient(true); };
            waitForClientToGoToChangingRoom.onFail += delegate { GoToEmploymentPositionAndFinalizeCurrentClient(false); };

            var leadToChangingRoom = new AutonomyMoveTo(self.autonomy, "lead to changing room", delegate (TaskTarget target)
            {
                target.SetTargetPosition(onsenClerkSession.onsenReception.transform.TransformPoint(onsenClerkSession.onsenReception.localClientShowChangingRoomPos));
            },
                0.3f,
                BodyState.STAND,
                delegate (TaskTarget target)
                {
                    target.SetTargetPosition(onsenClerkSession.onsenReception.transform.position);
                }
            );

            waitForClientToGoToChangingRoom.AddRequirement(leadToChangingRoom);

            self.autonomy.SetAutonomy(waitForClientToGoToChangingRoom);
        }
    }

}