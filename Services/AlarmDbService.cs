using JSAT_UNDAI.Models;
using Microsoft.Data.Sqlite;
using System.Globalization;
using System.IO;

namespace JSAT_UNDAI.Services;

public sealed class AlarmDbService
{
    readonly string _connectionString;
    readonly string _fileName = "AlarmDB.db";
    readonly string _filePath;

    public AlarmDbService(string appDataDirectory)
    {
        Directory.CreateDirectory(appDataDirectory);
        _filePath = Path.Combine(appDataDirectory, _fileName);

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _filePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();
        Initialize();
    }

    private void Initialize()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS Alarms (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                AlarmName   TEXT    NOT NULL,
                OccurredAt  TEXT    NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_Alarms_OccurredAt ON Alarms(OccurredAt);";

        cmd.ExecuteNonQuery();
    }

    public async Task InsertAsync(AlarmRecord alarmRecord)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            INSERT INTO Alarms (AlarmName, OccurredAt)
            VALUES ($name, $time);
            SELECT last_insert_rowid();";

        cmd.Parameters.AddWithValue("$name", alarmRecord.AlarmName);
        cmd.Parameters.AddWithValue("$time", alarmRecord.OccurredAt.ToString("o", CultureInfo.InvariantCulture));
        alarmRecord.Id = (long)(await cmd.ExecuteScalarAsync())!;
    }

    // Newest first
    public async Task<List<AlarmRecord>> LoadLatestAsync(int count = 500)
    {
        var list = new List<AlarmRecord>();

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            SELECT Id, AlarmName, OccurredAt
            FROM Alarms ORDER BY Id DESC LIMIT $n;";
        cmd.Parameters.AddWithValue("$n", count);

        await using var reader = await cmd.ExecuteReaderAsync();
        int displayNo = 1;
        while (await reader.ReadAsync())
        {
            list.Add(new AlarmRecord
            {
                Id = reader.GetInt64(0),
                AlarmName = reader.GetString(1),
                OccurredAt = DateTime.Parse(
                    reader.GetString(2),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind),
                DisplayNo = displayNo++
            });
        }
        return list;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = "DELETE FROM Alarms WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$id", id);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<int> ClearAllAsync(bool resetIds = true)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        // One transaction: either everything is cleared or nothing is
        await using var tx = (SqliteTransaction)await conn.BeginTransactionAsync();

        int deleted;
        await using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM Alarms;";
            deleted = await cmd.ExecuteNonQueryAsync();
        }

        if (resetIds)
        {
            // AUTOINCREMENT keeps its counter in sqlite_sequence
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM sqlite_sequence WHERE name = 'Alarms';";
            await cmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
        return deleted;
    }
}
