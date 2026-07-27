using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using HRM.Common;
using HRM.Helpers.Security;
using HRM.Model;
using HRM.Model.Healthcare;
using HRM.Security;

namespace HRM.Services
{
   
    /// Triển khai dịch vụ tra cứu 2 bước (2-Step Search) trên HealthcareDB.
    /// Sử dụng ADO.NET trực tiếp để đạt hiệu năng tối đa và đo đạc milliseconds chính xác.
    
    public class PatientSearchService : IPatientSearchService
    {
        private readonly string _connectionString;
        private readonly IHybridSecurityService _securityService;

        public PatientSearchService(IConfiguration configuration, IHybridSecurityService securityService)
        {
            _connectionString = configuration.GetConnectionString("HealthcareConnection")
                ?? throw new InvalidOperationException("Connection string 'HealthcareConnection' is missing in appsettings.json.");
            _securityService = securityService;
        }

        #region V2 PIPELINE (HMAC + BITGRAM BUCKET)

        /// <summary>
        /// Tra cứu chính xác V2 (HMAC-SHA256) theo CCCD, Phone, hoặc BankAccount.
        /// </summary>
        public async Task<PagedResult<PatientDto>> SearchExactAsync(string keyword, string field)
        {
            var swTotal = Stopwatch.StartNew();
            var debug = new SearchDebugInfo
            {
                SearchKeyword = keyword,
                SearchType = $"Exact_V2_HMAC_{field}"
            };

            if (string.IsNullOrWhiteSpace(keyword))
            {
                swTotal.Stop();
                debug.TotalMs = swTotal.ElapsedMilliseconds;
                return new PagedResult<PatientDto> { SearchDebug = debug };
            }

            string cleanKw = keyword.Trim();

            // 1. Generate HMAC Exact Index
            var swIndexGen = Stopwatch.StartNew();
            string hmacIndex = _securityService.GenerateExactIndex(cleanKw);
            swIndexGen.Stop();

            // Xác định cột HMAC cần query
            string hmacColumn = (field?.ToUpperInvariant()) switch
            {
                "PHONE" => "Phone_HMAC",
                "BANK" => "Bank_HMAC",
                _ => "CCCD_HMAC"
            };

            // 2. Step 1: SQL Index Query (Fetch Candidates)
            var candidates = new List<EncryptedPatientRow>();
            var swStep1 = Stopwatch.StartNew();

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string sql = $@"
                    SELECT PatientID, I_Name, I_CCCD, I_Phone, I_BankAccount
                    FROM dbo.Patient_Secure
                    WHERE {hmacColumn} = @HmacValue;";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@HmacValue", hmacIndex);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    candidates.Add(new EncryptedPatientRow
                    {
                        PatientID = reader.GetInt32(0),
                        I_Name = reader.GetString(1),
                        I_CCCD = reader.GetString(2),
                        I_Phone = reader.GetString(3),
                        I_BankAccount = reader.GetString(4)
                    });
                }
            }
            swStep1.Stop();
            debug.Step1Ms = swStep1.ElapsedMilliseconds;
            debug.CandidateCount = candidates.Count;

            // 3. Step 2: RAM AES Decrypt & Filter
            var results = new List<PatientDto>();
            var swStep2 = Stopwatch.StartNew();

            foreach (var row in candidates)
            {
                string decryptedTarget = (field?.ToUpperInvariant()) switch
                {
                    "PHONE" => _securityService.DecryptData(row.I_Phone),
                    "BANK" => _securityService.DecryptData(row.I_BankAccount),
                    _ => _securityService.DecryptData(row.I_CCCD)
                };

                if (decryptedTarget.Equals(cleanKw, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new PatientDto
                    {
                        PatientID = row.PatientID,
                        Name = _securityService.DecryptData(row.I_Name),
                        CCCD = _securityService.DecryptData(row.I_CCCD),
                        Phone = _securityService.DecryptData(row.I_Phone),
                        BankAccount = _securityService.DecryptData(row.I_BankAccount)
                    });
                }
            }
            swStep2.Stop();
            swTotal.Stop();

            debug.Step2Ms = swStep2.ElapsedMilliseconds;
            debug.TotalMs = swTotal.ElapsedMilliseconds;
            debug.ResultCount = results.Count;
            debug.CollisionCount = debug.CandidateCount - debug.ResultCount;

            return new PagedResult<PatientDto>
            {
                Items = results,
                Total = results.Count,
                SearchDebug = debug
            };
        }

        /// <summary>
        /// Tra cứu gần đúng V2 (BitGram 16-bit Bucket) theo Họ Tên.
        /// </summary>
        public async Task<PagedResult<PatientDto>> SearchFuzzyAsync(string keyword)
        {
            var swTotal = Stopwatch.StartNew();
            var debug = new SearchDebugInfo
            {
                SearchKeyword = keyword,
                SearchType = "Fuzzy_V2_BitGram"
            };

            if (string.IsNullOrWhiteSpace(keyword))
            {
                swTotal.Stop();
                debug.TotalMs = swTotal.ElapsedMilliseconds;
                return new PagedResult<PatientDto> { SearchDebug = debug };
            }

            string normalizedKw = SecurityIndexHelper.NormalizeForSearch(keyword);
            var trigrams = SecurityIndexHelper.BuildNgrams(normalizedKw, 3);

            if (trigrams.Count == 0)
            {
                swTotal.Stop();
                debug.TotalMs = swTotal.ElapsedMilliseconds;
                return new PagedResult<PatientDto> { SearchDebug = debug };
            }

            // 1. Generate BitGram Buckets
            var buckets = trigrams.Select(g => _securityService.GenerateFuzzyIndex(g))
                                  .Where(b => !string.IsNullOrEmpty(b))
                                  .Distinct()
                                  .ToList();

            int minMatch = Math.Max(1, (int)Math.Ceiling(buckets.Count * 0.7));

            // 2. Step 1: SQL Clustered Index Query (BitGram Buckets)
            var candidateIds = new List<int>();
            var swStep1 = Stopwatch.StartNew();

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                var sqlBuilder = new StringBuilder();
                sqlBuilder.Append(@"
                    SELECT PatientID, COUNT(*) AS MatchCount
                    FROM dbo.BitGramIndex_Patient
                    WHERE GramBucket IN (");

                var cmd = new SqlCommand { Connection = conn };
                for (int i = 0; i < buckets.Count; i++)
                {
                    string paramName = $"@b{i}";
                    if (i > 0) sqlBuilder.Append(", ");
                    sqlBuilder.Append(paramName);
                    cmd.Parameters.AddWithValue(paramName, buckets[i]);
                }

                sqlBuilder.Append(@")
                    GROUP BY PatientID
                    HAVING COUNT(*) >= @MinMatch
                    ORDER BY MatchCount DESC;");

                cmd.Parameters.AddWithValue("@MinMatch", minMatch);
                cmd.CommandText = sqlBuilder.ToString();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    candidateIds.Add(reader.GetInt32(0));
                }
            }
            swStep1.Stop();
            debug.Step1Ms = swStep1.ElapsedMilliseconds;
            debug.CandidateCount = candidateIds.Count;

            // 3. Step 2: Fetch Encrypted Rows & RAM Filter
            var results = new List<PatientDto>();
            var swStep2 = Stopwatch.StartNew();

            if (candidateIds.Count > 0)
            {
                var candidatesData = await FetchEncryptedRowsByIDsAsync(candidateIds);

                foreach (var row in candidatesData)
                {
                    string decryptedName = _securityService.DecryptData(row.I_Name);
                    string normalizedDecryptedName = SecurityIndexHelper.NormalizeForSearch(decryptedName);

                    if (normalizedDecryptedName.Contains(normalizedKw))
                    {
                        results.Add(new PatientDto
                        {
                            PatientID = row.PatientID,
                            Name = decryptedName,
                            CCCD = _securityService.DecryptData(row.I_CCCD),
                            Phone = _securityService.DecryptData(row.I_Phone),
                            BankAccount = _securityService.DecryptData(row.I_BankAccount)
                        });
                    }
                }
            }

            swStep2.Stop();
            swTotal.Stop();

            debug.Step2Ms = swStep2.ElapsedMilliseconds;
            debug.TotalMs = swTotal.ElapsedMilliseconds;
            debug.ResultCount = results.Count;
            debug.CollisionCount = debug.CandidateCount - debug.ResultCount;

            return new PagedResult<PatientDto>
            {
                Items = results,
                Total = results.Count,
                SearchDebug = debug
            };
        }

        #endregion

        #region V1 BASELINE PIPELINE (SHA256 FIXED-SALT)

        /// <summary>
        /// Tra cứu chính xác V1 Baseline (SHA256 fixed salt) để so sánh sòng phẳng.
        /// </summary>
        public async Task<PagedResult<PatientDto>> SearchExactBaselineAsync(string keyword, string field)
        {
            var swTotal = Stopwatch.StartNew();
            var debug = new SearchDebugInfo
            {
                SearchKeyword = keyword,
                SearchType = $"Exact_V1_Baseline_{field}"
            };

            if (string.IsNullOrWhiteSpace(keyword))
            {
                swTotal.Stop();
                debug.TotalMs = swTotal.ElapsedMilliseconds;
                return new PagedResult<PatientDto> { SearchDebug = debug };
            }

            string cleanKw = keyword.Trim();

            // V1 Baseline Compute SHA256 Fixed Salt Hash
            var swIndexGen = Stopwatch.StartNew();
            byte[] sha256Bytes = SecurityIndexHelper.ComputeHashWithSalt(cleanKw);
            string sha256Base64 = Convert.ToBase64String(sha256Bytes);
            swIndexGen.Stop();

            string hmacColumn = (field?.ToUpperInvariant()) switch
            {
                "PHONE" => "Phone_HMAC",
                "BANK" => "Bank_HMAC",
                _ => "CCCD_HMAC"
            };

            // Query Patient_Secure với SHA256 Hash
            var candidates = new List<EncryptedPatientRow>();
            var swStep1 = Stopwatch.StartNew();

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string sql = $@"
                    SELECT PatientID, I_Name, I_CCCD, I_Phone, I_BankAccount
                    FROM dbo.Patient_Secure
                    WHERE {hmacColumn} = @ShaValue;";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ShaValue", sha256Base64);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    candidates.Add(new EncryptedPatientRow
                    {
                        PatientID = reader.GetInt32(0),
                        I_Name = reader.GetString(1),
                        I_CCCD = reader.GetString(2),
                        I_Phone = reader.GetString(3),
                        I_BankAccount = reader.GetString(4)
                    });
                }
            }
            swStep1.Stop();
            debug.Step1Ms = swStep1.ElapsedMilliseconds;
            debug.CandidateCount = candidates.Count;

            var results = new List<PatientDto>();
            var swStep2 = Stopwatch.StartNew();

            foreach (var row in candidates)
            {
                string decryptedTarget = (field?.ToUpperInvariant()) switch
                {
                    "PHONE" => _securityService.DecryptData(row.I_Phone),
                    "BANK" => _securityService.DecryptData(row.I_BankAccount),
                    _ => _securityService.DecryptData(row.I_CCCD)
                };

                if (decryptedTarget.Equals(cleanKw, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new PatientDto
                    {
                        PatientID = row.PatientID,
                        Name = _securityService.DecryptData(row.I_Name),
                        CCCD = _securityService.DecryptData(row.I_CCCD),
                        Phone = _securityService.DecryptData(row.I_Phone),
                        BankAccount = _securityService.DecryptData(row.I_BankAccount)
                    });
                }
            }
            swStep2.Stop();
            swTotal.Stop();

            debug.Step2Ms = swStep2.ElapsedMilliseconds;
            debug.TotalMs = swTotal.ElapsedMilliseconds;
            debug.ResultCount = results.Count;
            debug.CollisionCount = debug.CandidateCount - debug.ResultCount;

            return new PagedResult<PatientDto>
            {
                Items = results,
                Total = results.Count,
                SearchDebug = debug
            };
        }

        /// <summary>
        /// Tra cứu gần đúng V1 Baseline (SHA256 fixed salt Trigram) để so sánh sòng phẳng.
        /// </summary>
        public async Task<PagedResult<PatientDto>> SearchFuzzyBaselineAsync(string keyword)
        {
            var swTotal = Stopwatch.StartNew();
            var debug = new SearchDebugInfo
            {
                SearchKeyword = keyword,
                SearchType = "Fuzzy_V1_Baseline"
            };

            if (string.IsNullOrWhiteSpace(keyword))
            {
                swTotal.Stop();
                debug.TotalMs = swTotal.ElapsedMilliseconds;
                return new PagedResult<PatientDto> { SearchDebug = debug };
            }

            string normalizedKw = SecurityIndexHelper.NormalizeForSearch(keyword);
            var trigrams = SecurityIndexHelper.BuildNgrams(normalizedKw, 3);

            if (trigrams.Count == 0)
            {
                swTotal.Stop();
                debug.TotalMs = swTotal.ElapsedMilliseconds;
                return new PagedResult<PatientDto> { SearchDebug = debug };
            }

            // SHA256 Fixed Salt Hashes
            var shaHashes = trigrams.Select(g => Convert.ToBase64String(SecurityIndexHelper.ComputeHashForGram(g)))
                                    .Distinct()
                                    .ToList();

            int minMatch = Math.Max(1, (int)Math.Ceiling(shaHashes.Count * 0.7));

            var candidateIds = new List<int>();
            var swStep1 = Stopwatch.StartNew();

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                var sqlBuilder = new StringBuilder();
                sqlBuilder.Append(@"
                    SELECT PatientID, COUNT(*) AS MatchCount
                    FROM dbo.BitGramIndex_Patient
                    WHERE GramBucket IN (");

                var cmd = new SqlCommand { Connection = conn };
                for (int i = 0; i < shaHashes.Count; i++)
                {
                    string paramName = $"@s{i}";
                    if (i > 0) sqlBuilder.Append(", ");
                    sqlBuilder.Append(paramName);
                    cmd.Parameters.AddWithValue(paramName, shaHashes[i]);
                }

                sqlBuilder.Append(@")
                    GROUP BY PatientID
                    HAVING COUNT(*) >= @MinMatch
                    ORDER BY MatchCount DESC;");

                cmd.Parameters.AddWithValue("@MinMatch", minMatch);
                cmd.CommandText = sqlBuilder.ToString();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    candidateIds.Add(reader.GetInt32(0));
                }
            }
            swStep1.Stop();
            debug.Step1Ms = swStep1.ElapsedMilliseconds;
            debug.CandidateCount = candidateIds.Count;

            var results = new List<PatientDto>();
            var swStep2 = Stopwatch.StartNew();

            if (candidateIds.Count > 0)
            {
                var candidatesData = await FetchEncryptedRowsByIDsAsync(candidateIds);

                foreach (var row in candidatesData)
                {
                    string decryptedName = _securityService.DecryptData(row.I_Name);
                    string normalizedDecryptedName = SecurityIndexHelper.NormalizeForSearch(decryptedName);

                    if (normalizedDecryptedName.Contains(normalizedKw))
                    {
                        results.Add(new PatientDto
                        {
                            PatientID = row.PatientID,
                            Name = decryptedName,
                            CCCD = _securityService.DecryptData(row.I_CCCD),
                            Phone = _securityService.DecryptData(row.I_Phone),
                            BankAccount = _securityService.DecryptData(row.I_BankAccount)
                        });
                    }
                }
            }

            swStep2.Stop();
            swTotal.Stop();

            debug.Step2Ms = swStep2.ElapsedMilliseconds;
            debug.TotalMs = swTotal.ElapsedMilliseconds;
            debug.ResultCount = results.Count;
            debug.CollisionCount = debug.CandidateCount - debug.ResultCount;

            return new PagedResult<PatientDto>
            {
                Items = results,
                Total = results.Count,
                SearchDebug = debug
            };
        }

        #endregion

        #region PRIVATE HELPER METHODS

        private async Task<List<EncryptedPatientRow>> FetchEncryptedRowsByIDsAsync(List<int> patientIds)
        {
            var list = new List<EncryptedPatientRow>();
            if (patientIds == null || patientIds.Count == 0) return list;

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var sqlBuilder = new StringBuilder();
            sqlBuilder.Append(@"
                SELECT PatientID, I_Name, I_CCCD, I_Phone, I_BankAccount
                FROM dbo.Patient_Secure
                WHERE PatientID IN (");

            using var cmd = new SqlCommand { Connection = conn };
            for (int i = 0; i < patientIds.Count; i++)
            {
                string paramName = $"@p{i}";
                if (i > 0) sqlBuilder.Append(", ");
                sqlBuilder.Append(paramName);
                cmd.Parameters.AddWithValue(paramName, patientIds[i]);
            }
            sqlBuilder.Append(");");

            cmd.CommandText = sqlBuilder.ToString();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new EncryptedPatientRow
                {
                    PatientID = reader.GetInt32(0),
                    I_Name = reader.GetString(1),
                    I_CCCD = reader.GetString(2),
                    I_Phone = reader.GetString(3),
                    I_BankAccount = reader.GetString(4)
                });
            }

            return list;
        }

        private class EncryptedPatientRow
        {
            public int PatientID { get; set; }
            public string I_Name { get; set; } = string.Empty;
            public string I_CCCD { get; set; } = string.Empty;
            public string I_Phone { get; set; } = string.Empty;
            public string I_BankAccount { get; set; } = string.Empty;
        }

        #endregion
    }
}
