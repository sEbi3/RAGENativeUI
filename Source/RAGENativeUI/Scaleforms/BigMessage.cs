namespace RAGENativeUI.Scaleforms
{
#if RPH1
    extern alias rph1;
    using WeaponHash = rph1::Rage.WeaponHash;
    using Vector2 = rph1::Rage.Vector2;
    using Vector3 = rph1::Rage.Vector3;
    using Rotator = rph1::Rage.Rotator;
#else
    /** REDACTED **/
#endif

    using System.Drawing;

    public class BigMessage : Scaleform
    {
        private uint endTime;
        private bool performedOutTransition;
        private uint outTransitionTimeInMilliseconds = 400;
        private float outTransitionTimeInSeconds = 0.4f;

        public OutTransitionType OutTransition { get; set; } = OutTransitionType.MoveUp;
        public float OutTransitionTime
        {
            get { return outTransitionTimeInSeconds; }
            set
            {
                Throw.IfNegative(value, nameof(value));

                outTransitionTimeInSeconds = value;
                outTransitionTimeInMilliseconds = (uint)(value * 1000);
            }
        }

        public BigMessage() : base("mp_big_message_freemode")
        {
        }

        public void SetVerticlePositionOverride(float newY)
        {
            CallMethod("OVERRIDE_Y_POSITION", newY);
        }

        private void SetUpTimer(uint time)
        {
            endTime = RPH.Game.GameTime + time;
            performedOutTransition = false;
        }

        public void CallMethodAndShow(uint time, string methodName, params object[] arguments)
        {
            CallMethod(methodName, arguments);
            SetUpTimer(time);
        }

        public void Hide()
        {
            if (endTime != 0 && !performedOutTransition)
            {
                PerformOutTransition();
                performedOutTransition = true;
                endTime = RPH.Game.GameTime + outTransitionTimeInMilliseconds;
            }
        }

        public void ShowMissionPassedMessage(string message, uint time = 5000)
        {
            Throw.IfNull(message, nameof(message));

            // the subtitle is only shown if TRANSITION_UP is called, so we set it to "placeholder" because
            // if it's undefined the main message isn't centered vertically
            CallMethodAndShow(time, "SHOW_MISSION_PASSED_MESSAGE", message, "placeholder", 100, true, 0, true);
        }

        public void ShowMissionPassedOldMessage(string message, string subtitle, uint time = 5000)
        {
            Throw.IfNull(message, nameof(message));
            Throw.IfNull(subtitle, nameof(subtitle));

            CallMethodAndShow(time, "SHOW_MISSION_PASSED_MESSAGE", message, subtitle);
        }

        public void ShowMpMessageLarge(string message, string subtitle, byte textAlpha = 100, uint time = 5000)
        {
            Throw.IfNull(message, nameof(message));
            Throw.IfNull(subtitle, nameof(subtitle));

            CallMethodAndShow(time, "SHOW_CENTERED_MP_MESSAGE_LARGE", message, subtitle, 100, true, (int)textAlpha);
        }

        public void ShowColoredShard(string message, string subtitle, HudColor textColor, HudColor backgroundColor, uint time = 5000)
        {
            Throw.IfNull(message, nameof(message));
            Throw.IfNull(subtitle, nameof(subtitle));

            CallMethodAndShow(time, "SHOW_SHARD_CENTERED_MP_MESSAGE", message, subtitle, (int)textColor, (int)backgroundColor);
        }

        public void ShowSimpleShard(string message, string subtitle, uint time = 5000)
        {
            Throw.IfNull(message, nameof(message));
            Throw.IfNull(subtitle, nameof(subtitle));

            CallMethodAndShow(time, "SHOW_SHARD_CREW_RANKUP_MP_MESSAGE", message, subtitle);
        }

        public void ShowRankupMessage(string message, string subtitle, int rank, uint time = 5000)
        {
            Throw.IfNull(message, nameof(message));
            Throw.IfNull(subtitle, nameof(subtitle));

            CallMethodAndShow(time, "SHOW_BIG_MP_MESSAGE", message, subtitle, rank);
        }

        public void ShowWeaponPurchasedMessage(string message, string weaponName, WeaponHash weapon, byte alpha = 100, uint time = 5000)
        {
            Throw.IfNull(message, nameof(message));
            Throw.IfNull(weaponName, nameof(weaponName));

            CallMethodAndShow(time, "SHOW_WEAPON_PURCHASED", message, weaponName, (uint)weapon, 0, (int)alpha);
        }

        // returns whether the scaleform should be drawn
        private bool Update()
        {
            if (endTime != 0)
            {
                uint gameTime = RPH.Game.GameTime;

                if (gameTime > endTime + outTransitionTimeInMilliseconds)
                {
                    endTime = 0;
                    performedOutTransition = false;
                }
                else if (!performedOutTransition && gameTime > endTime)
                {
                    PerformOutTransition();
                    performedOutTransition = true;
                }

                return true;
            }

            return false;
        }

        private void PerformOutTransition()
        {
            switch (OutTransition)
            {
                case OutTransitionType.FadeAway:
                    CallMethod("SHARD_ANIM_OUT", 2, outTransitionTimeInSeconds, 6);
                    break;
                case OutTransitionType.MoveUp:
                    CallMethod("TRANSITION_OUT", outTransitionTimeInSeconds);
                    break;
            }
        }

        public override void Draw(Color color)
        {
            if (Update())
            {
                base.Draw(color);
            }
        }

        public override void Draw(Vector2 position, Vector2 size, Color color)
        {
            if (Update())
            {
                base.Draw(position, size, color);
            }
        }

        public override void Draw3D(Vector3 position, Rotator rotation, Vector3 scale)
        {
            if (Update())
            {
                base.Draw3D(position, rotation, scale);
            }
        }


        public enum OutTransitionType
        {
            None,
            FadeAway,
            MoveUp,
        }
    }
}

