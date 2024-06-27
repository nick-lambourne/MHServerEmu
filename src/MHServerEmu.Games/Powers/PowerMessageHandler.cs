﻿using System.Text;
using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.LegacyImplementations;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Powers
{
    // NewPowerMessageHandler is the shiny new work-in-progress implementation of the power system
    // OldPowerMessageHandler is a collection of old hacks we made to force some things to happen early on
    // Eventually we are going to merge NewPowerMessageHandler with PlayerConnection and remove OldPowerMessageHandler completely

    public interface IPowerMessageHandler
    {
        public void ReceiveMessage(PlayerConnection playerConnection, MailboxMessage message);
    }

    public class NewPowerMessageHandler : IPowerMessageHandler
    {
        private const bool OutputToLog = false;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly PlayerConnection _playerConnection;

        public NewPowerMessageHandler(PlayerConnection playerConnection)
        {
            _playerConnection = playerConnection;
        }

        public void ReceiveMessage(PlayerConnection playerConnection, MailboxMessage message)
        {
            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageTryActivatePower:              OnTryActivatePower(message); break;
                case ClientToGameServerMessage.NetMessagePowerRelease:                  OnPowerRelease(message); break;
                case ClientToGameServerMessage.NetMessageTryCancelPower:                OnTryCancelPower(message); break;
                case ClientToGameServerMessage.NetMessageTryCancelActivePower:          OnTryCancelActivePower(message); break;
                case ClientToGameServerMessage.NetMessageContinuousPowerUpdateToServer: OnContinuousPowerUpdate(message); break;
                case ClientToGameServerMessage.NetMessageCancelPendingAction:           OnCancelPendingAction(message); break;

                default: Logger.Warn($"ReceiveMessage(): Unhandled {(ClientToGameServerMessage)message.Id} [{message.Id}]"); break;
            }
        }

        private bool OnTryActivatePower(MailboxMessage message)
        {
            var tryActivatePower = message.As<NetMessageTryActivatePower>();
            if (tryActivatePower == null) return Logger.WarnReturn(false, $"OnTryActivatePower(): Failed to retrieve message");

            if (OutputToLog)
            {
                StringBuilder sb = new();
                sb.AppendLine($"idUserEntity: {tryActivatePower.IdUserEntity}");
                sb.AppendLine($"powerPrototypeId: {GameDatabase.GetPrototypeName((PrototypeId)tryActivatePower.PowerPrototypeId)}");

                if (tryActivatePower.HasIdTargetEntity)
                    sb.AppendLine($"idTargetEntity: {tryActivatePower.IdTargetEntity}");

                if (tryActivatePower.HasTargetPosition)
                    sb.AppendLine($"targetPosition: {new Vector3(tryActivatePower.TargetPosition)}");

                if (tryActivatePower.HasMovementSpeed)
                    sb.AppendLine($"movementSpeed: {tryActivatePower.MovementSpeed}f");

                if (tryActivatePower.HasMovementTimeMS)
                    sb.AppendLine($"movementTimeMS: {tryActivatePower.MovementTimeMS}");

                if (tryActivatePower.HasPowerRandomSeed)
                    sb.AppendLine($"powerRandomSeed: {tryActivatePower.PowerRandomSeed}");

                if (tryActivatePower.HasItemSourceId)
                    sb.AppendLine($"itemSourceId: {tryActivatePower.ItemSourceId}");

                sb.AppendLine($"fxRandomSeed: {tryActivatePower.FxRandomSeed}");

                if (tryActivatePower.HasTriggeringPowerPrototypeId)
                    sb.AppendLine($"triggeringPowerPrototypeId: {GameDatabase.GetPrototypeName((PrototypeId)tryActivatePower.TriggeringPowerPrototypeId)}");

                Logger.Debug($"OnTryActivatePower():\n{sb}");
            }

            Avatar avatar = _playerConnection.Player.GetActiveAvatarById(tryActivatePower.IdUserEntity);

            // These checks fail due to lag, so no need to log
            if (avatar == null) return true;
            if (avatar.IsInWorld == false) return true;

            PrototypeId powerProtoRef = (PrototypeId)tryActivatePower.PowerPrototypeId;

            // Build settings from the protobuf
            PowerActivationSettings settings = new(avatar.RegionLocation.Position);
            settings.ApplyProtobuf(tryActivatePower);

            avatar.ActivatePower(powerProtoRef, in settings);

            return true;
        }

        private bool OnPowerRelease(MailboxMessage message)
        {
            var powerRelease = message.As<NetMessagePowerRelease>();
            if (powerRelease == null) return Logger.WarnReturn(false, $"OnPowerRelease(): Failed to retrieve message");

            if (OutputToLog)
            {
                StringBuilder sb = new();
                sb.AppendLine($"idUserEntity: {powerRelease.IdUserEntity}");
                sb.AppendLine($"powerPrototypeId: {GameDatabase.GetPrototypeName((PrototypeId)powerRelease.PowerPrototypeId)}");

                if (powerRelease.HasIdTargetEntity)
                    sb.AppendLine($"idTargetEntity: {powerRelease.IdUserEntity}");

                if (powerRelease.HasTargetPosition)
                    sb.AppendLine($"targetPosition: {new Vector3(powerRelease.TargetPosition)}");

                Logger.Debug($"OnPowerRelease():\n{sb}");
            }

            return true;
        }

        private bool OnTryCancelPower(MailboxMessage message)
        {
            var tryCancelPower = message.As<NetMessageTryCancelPower>();
            if (tryCancelPower == null) return Logger.WarnReturn(false, $"OnTryCancelPower(): Failed to retrieve message");

            if (OutputToLog)
            {
                StringBuilder sb = new();
                sb.AppendLine($"idUserEntity: {tryCancelPower.IdUserEntity}");
                sb.AppendLine($"powerPrototypeId: {GameDatabase.GetPrototypeName((PrototypeId)tryCancelPower.PowerPrototypeId)}");
                sb.AppendLine($"endPowerFlags: {(EndPowerFlags)tryCancelPower.EndPowerFlags}");

                Logger.Debug($"OnTryCancelPower():\n{sb}");
            }

            return true;
        }

        private bool OnTryCancelActivePower(MailboxMessage message)
        {
            var tryCancelActivePower = message.As<NetMessageTryCancelActivePower>();
            if (tryCancelActivePower == null) return Logger.WarnReturn(false, $"OnTryCancelActivePower(): Failed to retrieve message");

            if (OutputToLog)
                Logger.Debug($"OnTryCancelActivePower():\n{tryCancelActivePower}");

            return true;
        }

        private bool OnContinuousPowerUpdate(MailboxMessage message)
        {
            var continuousPowerUpdate = message.As<NetMessageContinuousPowerUpdateToServer>();
            if (continuousPowerUpdate == null) return Logger.WarnReturn(false, $"OnContinuousPowerUpdate(): Failed to retrieve message");

            if (OutputToLog)
            {
                StringBuilder sb = new();
                sb.AppendLine($"powerPrototypeId: {GameDatabase.GetPrototypeName((PrototypeId)continuousPowerUpdate.PowerPrototypeId)}");
                sb.AppendLine($"avatarIndex: {continuousPowerUpdate.AvatarIndex}");

                if (continuousPowerUpdate.HasIdTargetEntity)
                    sb.AppendLine($"idTargetEntity: {continuousPowerUpdate.IdTargetEntity}");

                if (continuousPowerUpdate.HasTargetPosition)
                    sb.AppendLine($"targetPosition: {new Vector3(continuousPowerUpdate.TargetPosition)}");

                if (continuousPowerUpdate.HasRandomSeed)
                    sb.AppendLine($"randomSeed: {continuousPowerUpdate.RandomSeed}");

                Logger.Debug($"OnContinuousPowerUpdate():\n{sb}");
            }

            return true;
        }

        private bool OnCancelPendingAction(MailboxMessage message)
        {
            var cancelPendingAction = message.As<NetMessageCancelPendingAction>();
            if (cancelPendingAction == null) return Logger.WarnReturn(false, $"OnCancelPendingAction(): Failed to retrieve message");

            if (OutputToLog)
                Logger.Debug($"OnCancelPendingAction():\n{cancelPendingAction}");

            return true;
        }
    }

    public class OldPowerMessageHandler : IPowerMessageHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly PlayerConnection _playerConnection;

        public OldPowerMessageHandler(PlayerConnection playerConnection)
        {
            _playerConnection = playerConnection;
        }

        public void ReceiveMessage(PlayerConnection playerConnection, MailboxMessage message)
        {
            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageTryActivatePower:              OnTryActivatePower(message); break;
                case ClientToGameServerMessage.NetMessagePowerRelease:                  OnPowerRelease(message); break;
                case ClientToGameServerMessage.NetMessageTryCancelPower:                OnTryCancelPower(message); break;
                case ClientToGameServerMessage.NetMessageTryCancelActivePower:          OnTryCancelActivePower(message); break;
                case ClientToGameServerMessage.NetMessageContinuousPowerUpdateToServer: OnContinuousPowerUpdate(message); break;

                default: Logger.Warn($"ReceiveMessage(): Unhandled {(ClientToGameServerMessage)message.Id} [{message.Id}]"); break;
            }
        }

        private bool PowerHasKeyword(PrototypeId powerId, PrototypeId keyword)
        {
            var power = GameDatabase.GetPrototype<PowerPrototype>(powerId);
            if (power == null) return false;

            for (int i = 0; i < power.Keywords.Length; i++)
                if (power.Keywords[i] == keyword) return true;

            return false;
        }

        private bool HandleTravelPower(PrototypeId powerProtoRef)
        {
            Avatar avatar = _playerConnection.Player.CurrentAvatar;

            Power travelPower = avatar.GetPower(powerProtoRef);
            if (travelPower == null)
                return Logger.WarnReturn(false, "HandleTravelPower(): travelPower == null");

            if (travelPower.IsTravelPower() == false)
                return Logger.WarnReturn(false, $"HandleTravelPower(): {travelPower.Prototype} is not a travel power");

            TimeSpan activationTime = travelPower.GetActivationTime();

            EventPointer<OLD_StartTravelEvent> eventPointer = new();
            avatar.Game.GameEventScheduler.ScheduleEvent(eventPointer, activationTime);
            eventPointer.Get().Initialize(_playerConnection, travelPower.PrototypeDataRef);

            return true;
        }

        #region Message Handling

        private bool OnTryActivatePower(MailboxMessage message)
        {
            var tryActivatePower = message.As<NetMessageTryActivatePower>();
            if (tryActivatePower == null) return Logger.WarnReturn(false, $"OnTryActivatePower(): Failed to retrieve message");

            var powerPrototypeId = (PrototypeId)tryActivatePower.PowerPrototypeId;
            string powerPrototypePath = GameDatabase.GetPrototypeName(powerPrototypeId);
            Logger.Trace($"Received TryActivatePower for {powerPrototypePath}");

            Avatar avatar = _playerConnection.Player.CurrentAvatar;
            Game game = avatar.Game;

            // Send this activation to other players
            ActivatePowerArchive activatePowerArchive = new();
            activatePowerArchive.Initialize(tryActivatePower, avatar.RegionLocation.Position);
            game.NetworkManager.SendMessageToInterested(activatePowerArchive.ToProtobuf(), avatar, AOINetworkPolicyValues.AOIChannelProximity, true);

            if (powerPrototypePath.Contains("ThrowablePowers/"))
            {
                Logger.Trace($"AddEvent EndThrowing for {powerPrototypePath}");

                bool isCancelling = powerPrototypePath.Contains("CancelPower");

                Power power = avatar.GetPower(powerPrototypeId);
                if (power == null) Logger.Warn("OnTryActivatePower(): power == null");

                EventPointer<OLD_EndThrowingEvent> endThrowingPointer = new();
                game.GameEventScheduler.ScheduleEvent(endThrowingPointer, power.GetAnimationTime());
                endThrowingPointer.Get().Initialize(avatar, isCancelling);

                return true;
            }
            else if (powerPrototypePath.Contains("EmmaFrost/"))
            {
                if (PowerHasKeyword(powerPrototypeId, (PrototypeId)HardcodedBlueprints.DiamondFormActivatePower))
                {
                    EventPointer<OLD_DiamondFormActivateEvent> activateEventPointer = new();
                    game.GameEventScheduler.ScheduleEvent(activateEventPointer, TimeSpan.Zero);
                    activateEventPointer.Get().PlayerConnection = _playerConnection;
                }
                else if (PowerHasKeyword(powerPrototypeId, (PrototypeId)HardcodedBlueprints.Mental))
                {
                    EventPointer<OLD_DiamondFormDeactivateEvent> deactivateEventPointer = new();
                    game.GameEventScheduler.ScheduleEvent(deactivateEventPointer, TimeSpan.Zero);
                    deactivateEventPointer.Get().PlayerConnection = _playerConnection;
                }
            }
            else if (tryActivatePower.PowerPrototypeId == (ulong)PowerPrototypes.Magik.Ultimate)
            {
                EventPointer<OLD_StartMagikUltimate> startEventPointer = new();
                game.GameEventScheduler.ScheduleEvent(startEventPointer, TimeSpan.Zero);
                startEventPointer.Get().Initialize(_playerConnection);

                EventPointer<OLD_EndMagikUltimateEvent> endEventPointer = new();
                game.GameEventScheduler.ScheduleEvent(endEventPointer, TimeSpan.FromSeconds(20));
                endEventPointer.Get().PlayerConnection = _playerConnection;
            }
            else if (tryActivatePower.PowerPrototypeId == (ulong)PowerPrototypes.Items.BowlingBallItemPower)
            {
                Inventory inventory = _playerConnection.Player.GetInventory(InventoryConvenienceLabel.General);
                
                Entity bowlingBall = inventory.GetMatchingEntity((PrototypeId)7835010736274089329); // BowlingBallItem
                if (bowlingBall == null) return false;

                if (bowlingBall.Properties[PropertyEnum.InventoryStackCount] > 1)
                    bowlingBall.Properties.AdjustProperty(-1, PropertyEnum.InventoryStackCount);
                else
                    bowlingBall.Destroy();
            }
            
            if (tryActivatePower.PowerPrototypeId == (ulong)avatar.TeamUpPowerRef)
            {
                avatar.SummonTeamUpAgent();
            }

            // if (powerPrototypePath.Contains("TravelPower/")) 
            //    TrawerPower(client, tryActivatePower.PowerPrototypeId);

            //Logger.Trace(tryActivatePower.ToString());

            PowerResults results = new();
            results.Init(tryActivatePower);
            if (results.TargetEntityId > 0)
            {
                game.NetworkManager.SendMessageToInterested(results.ToProtobuf(), avatar, AOINetworkPolicyValues.AOIChannelProximity);
                TestHit(results.TargetEntityId, (int)results.DamagePhysical);
            }

            return true;
        }

        private void TestHit(ulong entityId, int damage)
        {
            if (damage == 0) return;
            
            var entity = _playerConnection.Game.EntityManager.GetEntity<WorldEntity>(entityId);
            if (entity == null) return;

            int health = entity.Properties[PropertyEnum.Health];

            if (entity.ConditionCollection.Count > 0 && health == entity.Properties[PropertyEnum.HealthMaxOther])
            {
                // TODO: Clean this up
                _playerConnection.SendMessage(NetMessageDeleteCondition.CreateBuilder()
                    .SetIdEntity(entityId)
                    .SetKey(1)
                    .Build());
            }

            int newHealth = Math.Max(health - damage, 0);
            entity.Properties[PropertyEnum.Health] = newHealth;

            if (newHealth == 0)
            {
                entity.Kill(_playerConnection.Player.CurrentAvatar.Id);
            }
            else if (entity.WorldEntityPrototype is AgentPrototype agentProto && agentProto.Locomotion.Immobile == false)
            {
                entity.ChangeRegionPosition(null, new(Vector3.AngleYaw(entity.RegionLocation.Position, _playerConnection.LastPosition), 0f, 0f));
            }       
        }

        private bool OnPowerRelease(MailboxMessage message)
        {
            var powerRelease = message.As<NetMessagePowerRelease>();
            if (powerRelease == null) return Logger.WarnReturn(false, $"OnPowerRelease(): Failed to retrieve message");

            Logger.Trace($"Received PowerRelease for {GameDatabase.GetPrototypeName((PrototypeId)powerRelease.PowerPrototypeId)}");
            return true;
        }

        private bool OnTryCancelPower(MailboxMessage message)
        {
            var tryCancelPower = message.As<NetMessageTryCancelPower>();
            if (tryCancelPower == null) return Logger.WarnReturn(false, $"OnTryCancelPower(): Failed to retrieve message");

            string powerPrototypePath = GameDatabase.GetPrototypeName((PrototypeId)tryCancelPower.PowerPrototypeId);
            Logger.Trace($"Received TryCancelPower for {powerPrototypePath}");

            Logger.Trace(tryCancelPower.ToString());

            // Broadcast to other interested clients
            Avatar avatar = _playerConnection.Player.CurrentAvatar;
            Game game = avatar.Game;

            // NOTE: Although NetMessageCancelPower is not an archive, it uses power prototype enums
            ulong powerPrototypeEnum = (ulong)DataDirectory.Instance.GetPrototypeEnumValue<PowerPrototype>((PrototypeId)tryCancelPower.PowerPrototypeId);
            var clientMessage = NetMessageCancelPower.CreateBuilder()
                .SetIdAgent(tryCancelPower.IdUserEntity)
                .SetPowerPrototypeId(powerPrototypeEnum)
                .SetEndPowerFlags(tryCancelPower.EndPowerFlags)
                .Build();

            game.NetworkManager.SendMessageToInterested(clientMessage, avatar, AOINetworkPolicyValues.AOIChannelProximity, true);

            if (powerPrototypePath.Contains("TravelPower/"))
            {
                EventPointer<OLD_EndTravelEvent> eventPointer = new();
                game.GameEventScheduler.ScheduleEvent(eventPointer, TimeSpan.Zero);
                eventPointer.Get().Initialize(_playerConnection, (PrototypeId)tryCancelPower.PowerPrototypeId);
            }

            return true;
        }

        private bool OnTryCancelActivePower(MailboxMessage message)
        {
            Logger.Trace("Received TryCancelActivePower");
            return true;
        }

        private bool OnContinuousPowerUpdate(MailboxMessage message)
        {
            var continuousPowerUpdate = message.As<NetMessageContinuousPowerUpdateToServer>();
            if (continuousPowerUpdate == null) return Logger.WarnReturn(false, $"OnContinuousPowerUpdate(): Failed to retrieve message");

            var powerPrototypeId = (PrototypeId)continuousPowerUpdate.PowerPrototypeId;
            string powerPrototypePath = GameDatabase.GetPrototypeName(powerPrototypeId);
            Logger.Trace($"Received ContinuousPowerUpdate for {powerPrototypePath}");
            // Logger.Trace(continuousPowerUpdate.ToString());

            Avatar avatar = _playerConnection.Player.CurrentAvatar;
            Game game = avatar.Game;

            // Broadcast to other interested clients
            var clientMessageBuilder = NetMessageContinuousPowerUpdateToClient.CreateBuilder()
                .SetIdAvatar(avatar.Id)
                .SetPowerPrototypeId(continuousPowerUpdate.PowerPrototypeId);

            if (continuousPowerUpdate.HasIdTargetEntity)
                clientMessageBuilder.SetIdTargetEntity(continuousPowerUpdate.IdTargetEntity);

            if (continuousPowerUpdate.HasTargetPosition)
                clientMessageBuilder.SetTargetPosition(continuousPowerUpdate.TargetPosition);

            if (continuousPowerUpdate.HasRandomSeed)
                clientMessageBuilder.SetRandomSeed(continuousPowerUpdate.RandomSeed);

            game.NetworkManager.SendMessageToInterested(clientMessageBuilder.Build(), avatar, AOINetworkPolicyValues.AOIChannelProximity, true);

            // Handle travel
            if (powerPrototypePath.Contains("TravelPower/"))
                HandleTravelPower(powerPrototypeId);

            return true;
        }

        #endregion
    }
}
