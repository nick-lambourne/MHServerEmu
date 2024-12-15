﻿using Gazillion;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using System.Diagnostics;

namespace MHServerEmu.Games.Leaderboards
{   
    /// <summary>
    /// A singleton that contains leaderboard infomation.
    /// </summary>
    public class LeaderboardDatabase
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private const ulong UpdateTimeIntervalMS = 30 * 1000;   // 30 seconds

        private static readonly string LeaderboardsDirectory = Path.Combine(FileHelper.DataDirectory, "Game", "Leaderboards");
        private Dictionary<PrototypeGuid, Leaderboard> _leaderboards = new();
        public static LeaderboardDatabase Instance { get; } = new();
        public int LeaderboardCount { get; set; }

        private LeaderboardDatabase() { }

        /// <summary>
        /// Initializes the <see cref="LeaderboardDatabase"/> instance.
        /// </summary>
        public bool Initialize()
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Check leaderboards
            string configPath = Path.Combine(LeaderboardsDirectory, "Leaderboard.db");
            if (File.Exists(configPath) == false)
            {
                // TODO create new leaderboard.db
            }

            Logger.Info($"Initialized {_leaderboards.Count} leaderboards in {stopwatch.ElapsedMilliseconds} ms");
            return true;
        }

        public IEnumerable<LeaderboardPrototype> GetActiveLeaderboardPrototypes()
        {
            foreach (var leaderboard in _leaderboards.Values)
                foreach (var instance in leaderboard.Instances)
                    if (instance.State == LeaderboardState.eLBS_Active)
                        yield return leaderboard.Prototype;
        }

        public bool GetLeaderboardInstances(PrototypeGuid guid, out List<LeaderboardInstance> instances)
        {
            instances = new();

            if (_leaderboards.TryGetValue(guid, out var info) == false) return false;
            if (info.Prototype == null) return false;

            int maxarchived = info.Prototype.MaxArchivedInstances;

            foreach (var instance in info.Instances)
            {
                instances.Add(instance);
                if (maxarchived-- == 0) break;
            }

            return true;
        }

        public LeaderboardReport GetLeaderboardReport(NetMessageLeaderboardRequest request)
        {
            ulong leaderboardId = 0;
            ulong instanceId = 0;

            var report = LeaderboardReport.CreateBuilder()
                .SetNextUpdateTimeIntervalMS(UpdateTimeIntervalMS);

            if (request.HasPlayerScoreQuery)
            {
                var query = request.PlayerScoreQuery;
                leaderboardId = query.LeaderboardId;
                instanceId = query.InstanceId;
                ulong playerId = query.PlayerId;
                ulong avatarId = query.HasAvatarId ? query.AvatarId : 0;

                if (GetLeaderboardScoreData(leaderboardId, instanceId, playerId, avatarId, out LeaderboardScoreData scoreData))
                    report.SetScoreData(scoreData);
            }

            if (request.HasGuildScoreQuery) // Not used
            {
                var query = request.GuildScoreQuery;
                leaderboardId = query.LeaderboardId;
                instanceId = query.InstanceId;
                ulong guid = query.GuildId;

                if (GetLeaderboardScoreData(leaderboardId, instanceId, guid, 0, out LeaderboardScoreData scoreData))
                    report.SetScoreData(scoreData);
            }

            if (request.HasMetaScoreQuery) // Tournament: Civil War
            {
                var query = request.MetaScoreQuery;
                leaderboardId = query.LeaderboardId;
                instanceId = query.InstanceId;
                ulong playerId = query.PlayerId;

                if (GetLeaderboardScoreData(leaderboardId, instanceId, playerId, 0, out LeaderboardScoreData scoreData))
                    report.SetScoreData(scoreData);
            }

            if (request.HasDataQuery)
            {
                var query = request.DataQuery;
                leaderboardId = query.LeaderboardId;
                instanceId = query.InstanceId;

                if (GetLeaderboardTableData(leaderboardId, instanceId, out LeaderboardTableData tableData))
                    report.SetTableData(tableData);
            }

            report.SetLeaderboardId(leaderboardId).SetInstanceId(instanceId);

            return report.Build();
        }

        private bool GetLeaderboardTableData(ulong leaderboardId, ulong instanceId, out LeaderboardTableData tableData)
        {
            tableData = null;
            var leaderboard = GetLeaderboard((PrototypeGuid)leaderboardId);
            if (leaderboard == null) return false;

            var instance = leaderboard.GetInstance(instanceId);
            if (instance == null) return false;

            tableData = instance.GetTableData();
            return true;
        }

        private bool GetLeaderboardScoreData(ulong leaderboardId, ulong instanceId, ulong guid, ulong avatarId, 
            out LeaderboardScoreData scoreData)
        {
            scoreData = null;
            var leaderboard = GetLeaderboard((PrototypeGuid)leaderboardId);
            if (leaderboard == null) return false;

            var type = leaderboard.Prototype.Type;

            var instance = leaderboard.GetInstance(instanceId);
            if (instance == null) return false;

            LeaderboardEntry entry;
            if (type == LeaderboardType.MetaLeaderboard)
            {
                ulong leaderboardEntryId = instance.GetLeaderboardEntryId(guid);
                entry = instance.GetEntry(leaderboardEntryId, avatarId);
            }
            else
            {
                entry = instance.GetEntry(guid, avatarId);
            }

            if (entry == null) return false;

            var scoreDataBuilder = LeaderboardScoreData.CreateBuilder()
                .SetLeaderboardId(leaderboardId);

            if (instanceId != 0) scoreDataBuilder.SetInstanceId(instanceId);

            if (type == LeaderboardType.Player) 
            {
                scoreDataBuilder.SetAvatarId(avatarId);
                scoreDataBuilder.SetPlayerId(guid);
            }

            if (type == LeaderboardType.Guild)
                scoreDataBuilder.SetGuildId(guid);

            scoreDataBuilder.SetScore(entry.Score);
            scoreDataBuilder.SetPercentileBucket((uint)instance.GetPercentileBucket(entry));

            scoreData = scoreDataBuilder.Build();

            return true;
        }

        public Leaderboard GetLeaderboard(PrototypeGuid guid)
        {
            if (_leaderboards.TryGetValue(guid, out var leaderboard))
                return leaderboard;
            return null;
        }

        public void UpdateLeaderboards()
        {
            foreach (var leaderboard in _leaderboards.Values.ToArray())
                if (leaderboard.NeedsUpdate)
                    leaderboard.OnUpdate();
        }
    }
}