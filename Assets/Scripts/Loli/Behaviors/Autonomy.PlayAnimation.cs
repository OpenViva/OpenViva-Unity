using System.Collections.Generic;


namespace viva
{

    using AutonomyAnimationEvent = AnimationEvent<OnAnimationCallback>;

    public delegate void OnAnimationCallback();


    public class AutonomyPlayAnimation : Autonomy.Task
    {

        public Loli.Animation entryAnimation { get; private set; }
        private Loli.Animation exitAnimation;
        private bool played = false;
        public float? playing { get; private set; } = null;
        public bool loop = false;
        public float minimumReqTorsoNormTime = 0.8f;
        private AutonomyAnimationEvent.Context animationEventContext;
        private AutonomyAnimationEvent[] animationEvents;
        public OnAnimationCallback onAnimationEnter;
        public OnAnimationCallback onAnimationExit;

        public BodyState targetBodyState { get; private set; }
        private List<Loli.Animation> transferAnims = new List<Loli.Animation>();


        public AutonomyPlayAnimation(Autonomy _autonomy, string _name, Loli.Animation _entryAnimation) : base(_autonomy, _name)
        {
            OverrideAnimations(_entryAnimation, _entryAnimation);

            onAnimationChange += HandleAnimationChange;
            onFixedUpdate += OnFixedUpdate;
            onReset += delegate
            {
                playing = null;
                played = false;
            };
            onRegistered += OnRegistered;
            onSuccess += delegate { played = true; };

            onAnimationEnter += delegate
            {
                //ensure enforce body state doesn't interfere with post bodystate
                SetTargetBodyState(Loli.animationInfos[entryAnimation].newBodyState);
            };

            onUnregistered += delegate { self.OnBodyStateChanged -= OnBodyStateChanged; };
        }

        private void OnRegistered()
        {
            OverrideAnimations(entryAnimation, exitAnimation);
            HandleAnimationChange(self.lastAnim, self.currentAnim);
            self.OnBodyStateChanged += OnBodyStateChanged;
        }

        private void SetTargetBodyState(BodyState _bodyState)
        {
            if (targetBodyState == _bodyState)
            {
                return;
            }
            targetBodyState = _bodyState;
            RecalculateTransferAnims();
        }

        public void OverrideAnimations(Loli.Animation _entryAnimation)
        {
            OverrideAnimations(_entryAnimation, _entryAnimation);
        }

        public void OverrideAnimations(Loli.Animation _entryAnimation, Loli.Animation _exitAnimation)
        {
            entryAnimation = _entryAnimation;
            exitAnimation = _exitAnimation;

            SetTargetBodyState(Loli.animationInfos[entryAnimation].conditionBodyState);
        }

        public void SetupAnimationEvents(AutonomyAnimationEvent[] _animationEvents)
        {
            animationEvents = _animationEvents;
            if (animationEventContext == null)
            {
                animationEventContext = new AutonomyAnimationEvent.Context(
                    delegate (AutonomyAnimationEvent animEvent)
                    {
                        animEvent.parameter?.Invoke();
                    });
            }
        }

        private void OnBodyStateChanged(BodyState oldBodyState, BodyState newBodyState)
        {
            RecalculateTransferAnims();
        }

        private void RecalculateTransferAnims()
        {
            if (!Loli.FindBodyStatePath(self, transferAnims, self.bodyState, targetBodyState))
            {
                FlagForFailure();
            }
        }

        public override bool? Progress()
        {
            if (played)
            {
                return true;
            }
            return null;
        }

        public void OnFixedUpdate()
        {
            if (entryAnimation == Loli.Animation.NONE)
            {
                return;
            }
            if (playing.HasValue)
            {
                if (animationEventContext != null)
                {
                    animationEventContext.UpdateAnimationEvents(animationEvents, self.currentAnimationLoops, self.GetLayerAnimNormTime(1));
                }
                if (self.currentAnim == exitAnimation)
                {
                    if (self.GetLayerAnimNormTime(1) - playing.Value > minimumReqTorsoNormTime)
                    {
                        FinishedPlaying();
                        return;
                    }
                }
            }
            if (!played && self.currentAnim != entryAnimation && self.currentAnim != exitAnimation)
            {

                if (self.bodyState == targetBodyState)
                {
                    // self.OverrideClearAnimationPriority();	//necessary ?
                    self.SetTargetAnimation(entryAnimation);
                }
                else if (self.IsCurrentAnimationIdle() && transferAnims.Count > 0)
                {
                    var transferAnim = transferAnims[0];
                    if (transferAnim != Loli.Animation.NONE)
                    {
                        self.SetTargetAnimation(transferAnim);
                    }
                }
            }
        }

        private void FinishedPlaying()
        {
            if (playing.HasValue)
            {
                playing = null;
                onAnimationExit?.Invoke();
                if (!loop)
                {
                    FlagForSuccess();
                }
            }
        }

        private void HandleAnimationChange(Loli.Animation oldAnim, Loli.Animation newAnim)
        {
            if (animationEventContext != null)
            {
                animationEventContext.ResetCounters();
            }

            if (newAnim == entryAnimation)
            {
                if (!playing.HasValue)
                {
                    playing = self.GetLayerAnimNormTime(1);
                    onAnimationEnter?.Invoke();
                }
            }
            if (self.lastTorsoNormTimePlayed > minimumReqTorsoNormTime)
            {
                if (oldAnim == exitAnimation)
                {
                    FinishedPlaying();
                }
            }
        }
    }

}