using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.UI.Widgets;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateLimitPlayerDeaths : MetaState
    {
        public static readonly Logger Logger = LogManager.CreateLogger();
        private MetaStateLimitPlayerDeathsPrototype _proto;
        private Action<PlayerDeathRecordedEvent> _playerDeathRecordedAction;

        public MetaStateLimitPlayerDeaths(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateLimitPlayerDeathsPrototype;
            _playerDeathRecordedAction = OnPlayerDeathRecorded;
        }

        public override void OnApply()
        {
            var region = Region;
            if (region == null) return;

            if (GetPlayerDeathLimit() > 0 || _proto.FailOnAllPlayersDead)
            {
                PlayerDeathLimitReset();
                region.PlayerDeathRecordedEvent.AddActionBack(_playerDeathRecordedAction);
            }
        }

        public override void OnRemove()
        {
            var region = Region;
            if (region == null) return;

            if (GetPlayerDeathLimit() > 0 || _proto.FailOnAllPlayersDead)
            {
                region.PlayerDeathRecordedEvent.RemoveAction(_playerDeathRecordedAction);
                var windgetRef = _proto.UIWidget;
                if (windgetRef != PrototypeId.Invalid)
                    region.UIDataProvider?.DeleteWidget(windgetRef);
            }

            base.OnRemove();
        }

        private void PlayerDeathLimitReset()
        {
            var region = Region;
            if (region == null) return;

            int deathLimit = GetPlayerDeathLimit();
            int regionDeath = _proto.UseRegionDeathCount ? region.Settings.PlayerDeaths : 0;
            
            if (region.PlayerDeaths != regionDeath)
            {
                // region.PlayerDeathLimitResetEvent ???
                region.PlayerDeaths = regionDeath;
                UpdateWidgetCount(deathLimit - regionDeath, deathLimit);
            }
        }

        private void UpdateWidgetCount(int current, int total)
        {
            var region = Region;
            if (region == null) return;

            var mode = MetaGame.CurrentMode;
            if (mode == null) return;

            if (current < 0) current = 0;
            mode.SendPvEInstanceDeathUpdate(current);

            var widgetRef = _proto.UIWidget;
            if (widgetRef == PrototypeId.Invalid) return;

            if (MetaGame.Debug) Logger.Info($"UpdateWidgetCount {widgetRef.GetNameFormatted()}");

            var widget = region.UIDataProvider?.GetWidget<UIWidgetGenericFraction>(widgetRef);
            widget?.SetCount(current, total);
        }

        private void OnPlayerDeathRecorded(PlayerDeathRecordedEvent evt)
        {
            var region = Region;
            if (region == null) return;

            var mode = MetaGame.CurrentMode;
            if (mode == null) return;

            if (MetaGame.Properties.HasProperty(PropertyEnum.MetaGameIgnoreDeathLimits)
                && MetaGame.Properties[PropertyEnum.MetaGameIgnoreDeathLimits] == true) return;

            int deathLimit = GetPlayerDeathLimit();
            int current = deathLimit - region.PlayerDeaths;
            if (current >= 0 && deathLimit > 0) 
            {
                OnDeathNotification(deathLimit == region.PlayerDeaths);
                UpdateWidgetCount(current, deathLimit);
            }

            if (_proto.FailOnAllPlayersDead)
                FailOnAllPlayersDead();

            // TODO _proto.BlacklistDeadPlayers
        }

        private void OnDeathNotification(bool limitHit)
        {
            var region = Region;
            if (region == null) return;

            var mode = MetaGame.CurrentMode;
            if (mode == null) return;

            if (limitHit)
            {
                if (_proto.StayInModeOnFail == false)
                {
                    region.PlayerDeathLimitHitEvent.Invoke(new(null, PrototypeDataRef));
                    MetaGame.ScheduleActivateGameMode(_proto.FailMode);
                }

                mode.SendUINotification(_proto.DeathLimitUINotification);
            }
            else
            {
                mode.SendUINotification(_proto.DeathUINotification);
            }
        }

        private void FailOnAllPlayersDead()
        {
            if (IsAllPlayersDead()) MetaGame.ScheduleActivateGameMode(_proto.FailMode);
        }

        private bool IsAllPlayersDead()
        {
            if (_proto.FailOnAllPlayersDead == false) return false;

            var region = Region;
            if (region == null) return false;

            bool isPlayers = false;
            bool allDead = true;

            foreach(var player in new PlayerIterator(region))
            {
                var avatar = player.CurrentAvatar;
                if (avatar == null) continue;

                isPlayers = true;
                if (avatar.IsDead == false)
                {
                    allDead = false;
                    break;
                }
            }

            return isPlayers && allDead;
        }

        private int GetPlayerDeathLimit()
        {
            if (Region == null) return 0;
            int deathLimitOverride = Region.Properties[PropertyEnum.DeathLimitOverride];
            if (deathLimitOverride < 0) return _proto.PlayerDeathLimit;
            return deathLimitOverride;
        }

        public override void OnAddPlayer(Player player)
        {
            if (player != null) OnUpdatePlayerNotification(player);
        }

        public override void OnUpdatePlayerNotification(Player player)
        {
            var region = Region;
            if (region == null) return;

            var mode = MetaGame.CurrentMode;
            if (mode == null) return;

            int deathLimit = GetPlayerDeathLimit();
            if (deathLimit <= 0) return;
            UpdateWidgetCount(deathLimit - region.PlayerDeaths, deathLimit);
        }

        public override void OnRemovePlayer(Player player)
        {
            if (_proto.FailOnAllPlayersDead || DeathLimit())
                FailOnAllPlayersDead();
        }

        public bool DeathLimit()
        {
            var region = Region;
            if (region == null) return false;

            if (region.PlayerDeaths >= GetPlayerDeathLimit()) return true;

            return false;
        }
    }
}