﻿using Dapper;
using System.Data.SQLite;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.DatabaseAccess.SQLite
{
    public class SQLiteLDBManager
    {
        private const int CurrentSchemaVersion = 1;         // Increment this when making changes to the database schema

        private static readonly Logger Logger = LogManager.CreateLogger();
        public static SQLiteLDBManager Instance { get; } = new();

        private string _dbFilePath;
        private string _connectionString;

        private SQLiteLDBManager() { }

        public bool Initialize(string configPath)
        {
            _dbFilePath = configPath; 
            _connectionString = $"Data Source={_dbFilePath}";

            if (File.Exists(_dbFilePath) == false)
            {
                // Create a new database file if it does not exist
                if (InitializeDatabaseFile() == false)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Initializes a new empty database file using the current schema.
        /// </summary>
        private bool InitializeDatabaseFile()
        {
            string initializationScript = SQLiteScripts.GetInitializationScript();
            if (initializationScript == string.Empty)
                return Logger.ErrorReturn(false, "InitializeDatabaseFile(): Failed to get database initialization script");

            SQLiteConnection.CreateFile(_dbFilePath);
            using SQLiteConnection connection = GetConnection();
            connection.Execute(initializationScript);

            Logger.Info($"Initialized a new database file at {Path.GetRelativePath(FileHelper.ServerRoot, _dbFilePath)} using schema version {CurrentSchemaVersion}");

            CreateLeaderboards();

            return true;
        }

        private bool CreateLeaderboards()
        {
            string initializationScript = SQLiteScripts.GetLeaderboardsScript();
            if (initializationScript == string.Empty)
                return Logger.ErrorReturn(false, "CreateLeaderboards(): Failed to get database initialization script");

            SQLiteConnection.CreateFile(_dbFilePath);
            using SQLiteConnection connection = GetConnection();
            connection.Execute(initializationScript);

            return Logger.InfoReturn(true, $"Initialized Leaderboards using file at {Path.GetRelativePath(FileHelper.ServerRoot, _dbFilePath)} using schema version {CurrentSchemaVersion}");
        }

        /// <summary>
        /// Creates and opens a new <see cref="SQLiteConnection"/>.
        /// </summary>
        private SQLiteConnection GetConnection()
        {
            SQLiteConnection connection = new(_connectionString);
            connection.Open();
            return connection;
        }

        public DBLeaderboard[] GetLeaderboardList()
        {
            using SQLiteConnection connection = GetConnection();
            return connection.Query<DBLeaderboard>("SELECT * FROM Leaderboards").ToArray();
        }

        public List<DBLeaderboardInstance> GetInstanceList(long leaderboardId, int maxArchivedInstances)
        {
            using SQLiteConnection connection = GetConnection();

            List<DBLeaderboardInstance> instanceList = new(
                connection.Query<DBLeaderboardInstance>(
                    "SELECT * FROM Instances WHERE LeaderboardId = @LeaderboardId AND State <= 1 ORDER BY InstanceId DESC",
                    new { LeaderboardId = leaderboardId }));

            if (maxArchivedInstances > 0)
            {
                instanceList.AddRange(
                connection.Query<DBLeaderboardInstance>(
                    "SELECT * FROM Instances WHERE LeaderboardId = @LeaderboardId AND State > 1 ORDER BY InstanceId DESC LIMIT @MaxArchivedInstances",
                    new { LeaderboardId = leaderboardId, MaxArchivedInstances = maxArchivedInstances}));
            }

            return instanceList;
        }

        public List<DBLeaderboardEntry> GetEntries(long instanceId, bool ascending)
        {
            using SQLiteConnection connection = GetConnection();

            string order = ascending ? "ASC" : "DESC";

            List<DBLeaderboardEntry> entryList = new(
                connection.Query<DBLeaderboardEntry>(
                    $"SELECT * FROM Entries WHERE InstanceId = @InstanceId ORDER BY HighScore {order}",
                    new { InstanceId = instanceId }));

            return entryList;
        }
    }
}
