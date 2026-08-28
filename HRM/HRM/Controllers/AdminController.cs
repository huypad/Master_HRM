using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using HRM.Security;

namespace HRM.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly IHybridSecurityService _securityService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IConfiguration configuration,
            IHybridSecurityService securityService,
            ILogger<AdminController> logger)
        {
            _connectionString = configuration.GetConnectionString("HealthcareConnection")
                ?? throw new InvalidOperationException("Connection string missing.");
            _securityService = securityService;
            _logger = logger;
        }

        /// Re-index BitGramIndex_Patient theo cấu hình MinHash LSH hiện hành.
        [HttpPost("reindex-patients")]
        public async Task<IActionResult> ReIndexPatients([FromQuery] int batchSize = 500)
        {
            if (batchSize < 1 || batchSize > 2000) batchSize = 500;

            var swTotal = Stopwatch.StartNew();
            int totalProcessed = 0, totalSuccess = 0, totalFailed = 0;
            var errors = new List<string>();

            _logger.LogInformation("[ReIndex] Bat dau. BatchSize={B}", batchSize);

            int totalCount = 0;
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Patient WITH(NOLOCK);", conn);
                cmd.CommandTimeout = 300;
                totalCount = (int)(await cmd.ExecuteScalarAsync() ?? 0);
            }
            _logger.LogInformation("[ReIndex] Tong so benh nhan: {T}", totalCount);

            int lastId = 0;
            while (true)
            {
                var batch = new List<(int PId, string Name)>(batchSize);
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    using var cmd = new SqlCommand(@"
                        SELECT TOP (@Sz) PatientID, ISNULL(FullName,'') FROM dbo.Patient
                        WHERE PatientID > @LastId
                        ORDER BY PatientID;", conn);
                    cmd.CommandTimeout = 120;
                    cmd.Parameters.AddWithValue("@LastId", lastId);
                    cmd.Parameters.AddWithValue("@Sz", batchSize);
                    using var rdr = await cmd.ExecuteReaderAsync();
                    while (await rdr.ReadAsync())
                    {
                        int id = rdr.GetInt32(0);
                        batch.Add((id, rdr.GetString(1).Trim()));
                        lastId = id;
                    }
                }

                if (batch.Count == 0) break;

                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    using var tx = conn.BeginTransaction();
                    int batchSuccess = 0;
                    int? currentPatientId = null;
                    try
                    {
                        foreach (var (pid, name) in batch)
                        {
                            currentPatientId = pid;
                            string fuzzyStr = _securityService.GenerateFuzzyIndex(name);
                            int[] buckets = ParseMinHashBuckets(fuzzyStr);

                            if (buckets.Length == 0)
                            {
                                throw new InvalidOperationException($"PID={pid}: không tạo được bucket LSH.");
                            }

                            using (var del = new SqlCommand(
                                "DELETE FROM dbo.BitGramIndex_Patient WHERE PatientID=@Id;", conn, tx))
                            {
                                del.CommandTimeout = 180;
                                del.Parameters.AddWithValue("@Id", pid);
                                await del.ExecuteNonQueryAsync();
                            }

                            for (int pos = 0; pos < buckets.Length; pos++)
                            {
                                using var ins = new SqlCommand(@"
                                    INSERT INTO dbo.BitGramIndex_Patient (PatientID,GramBucket,GramPosition)
                                    VALUES (@P,@B,@Pos);", conn, tx);
                                ins.Parameters.AddWithValue("@P", pid);
                                ins.Parameters.AddWithValue("@B", buckets[pos]);
                                ins.Parameters.AddWithValue("@Pos", pos);
                                await ins.ExecuteNonQueryAsync();
                            }

                            batchSuccess++;
                        }
                        await tx.CommitAsync();
                        totalProcessed += batch.Count;
                        totalSuccess += batchSuccess;
                    }
                    catch (Exception ex)
                    {
                        try { await tx.RollbackAsync(); } catch { }
                        totalProcessed += batch.Count;
                        totalFailed += batch.Count;
                        errors.Add($"Batch có PID={currentPatientId} đã rollback: {ex.Message}");
                        _logger.LogError(ex, "[ReIndex] Batch containing PID={PatientId} ROLLBACK", currentPatientId);
                    }
                }

                int processedInBatches = totalProcessed;
                if (processedInBatches % 5000 < batchSize || processedInBatches >= totalCount)
                {
                    double pct = totalCount > 0 ? (double)processedInBatches / totalCount * 100 : 100;
                    _logger.LogInformation("[ReIndex] {O}/{T} ({P:F1}%) | OK={S} | Fail={F} | {E:F0}s",
                        processedInBatches, totalCount, pct, totalSuccess, totalFailed, swTotal.Elapsed.TotalSeconds);
                }

                if (batch.Count < batchSize) break;
            }

            swTotal.Stop();
            return Ok(new
            {
                status    = totalFailed == 0 ? "DONE" : "DONE_WITH_ERRORS",
                totalCount, totalProcessed, totalSuccess, totalFailed, batchSize,
                elapsedSeconds = Math.Round(swTotal.Elapsed.TotalSeconds, 1),
                errors = errors.Count > 0 ? errors : null
            });
        }


        private static int[] ParseMinHashBuckets(string s)
        {
            const string pfx = "BKT_V2_";
            if (string.IsNullOrEmpty(s) || !s.StartsWith(pfx)) return Array.Empty<int>();
            string rest = s.Substring(pfx.Length);
            if (string.IsNullOrEmpty(rest)) return Array.Empty<int>();
            var parts = rest.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            var r = new List<int>();
            foreach (var p in parts)
                if (int.TryParse(p, out int v)) r.Add(v);
            return r.ToArray();
        }
    }
}
