using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
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

    /// Triển khai dịch vụ tra cứu 2 bước (2-Step Search) trên HealthcareDB1.

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

        #region V2 PIPELINE (HMAC + BITGRAM / LSH BUCKET)


        /// Tra cứu chính xác V2 (HMAC-SHA256) theo CCCD, Phone, hoặc BankAccount.

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
            bool isIdSearch = (field?.ToUpperInvariant()) is "ID" or "PATIENTID";

            // 1. Generate HMAC & SHA256 Exact Indexes (Bao phủ cả HMAC tiêu chuẩn và SHA256 không khóa của Bạn 1)
            var swIndexGen = Stopwatch.StartNew();
            string hmacIndex = _securityService.GenerateExactIndex(cleanKw);
            string sha256Index1 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cleanKw)));
            string sha256Index2 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cleanKw + "  ")));
            swIndexGen.Stop();

            // Xác định cột HMAC cần query
            string hmacColumn = (field?.ToUpperInvariant()) switch
            {
                "PHONE" => "Phone_HMAC",
                "BANK" => "Bank_HMAC",
                _ => "CCCD_HMAC"
            };

            // 2. Step 1: SQL Index Query (Fetch Candidates bang Index Seek)
            var candidates = new List<EncryptedPatientRow>();
            var swStep1 = Stopwatch.StartNew();

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string sql = $@"
                    SELECT PatientID, EncryptName, EncryptCCCD, EncryptPhone, EncryptBankAccount
                    FROM dbo.Patient_Secure
                    WHERE {hmacColumn} IN (@HmacValue, @Sha1, @Sha2);";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@HmacValue", hmacIndex);
                cmd.Parameters.AddWithValue("@Sha1", sha256Index1);
                cmd.Parameters.AddWithValue("@Sha2", sha256Index2);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    candidates.Add(ReadEncryptedRow(reader));
                }
            }

            // Fallback neu hmacIndex tren DB cua Ban 1 dung hash khac -> doc theo value match
            if (candidates.Count == 0)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    string sql = @"
                        SELECT PatientID, EncryptName, EncryptCCCD, EncryptPhone, EncryptBankAccount
                        FROM dbo.Patient_Secure;";

                    using var cmd = new SqlCommand(sql, conn);
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var row = ReadEncryptedRow(reader);
                        string val = (field?.ToUpperInvariant()) switch
                        {
                            "PHONE" => _securityService.DecryptData(row.I_Phone),
                            "BANK" => _securityService.DecryptData(row.I_BankAccount),
                            _ => _securityService.DecryptData(row.I_CCCD)
                        };
                        if (val.Trim().Equals(cleanKw, StringComparison.OrdinalIgnoreCase))
                        {
                            candidates.Add(row);
                        }
                    }
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

                if (decryptedTarget.Trim().Equals(cleanKw, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new PatientDto
                    {
                        PatientID = row.PatientID,
                        Name = _securityService.DecryptData(row.I_Name).Replace("\0", "").Trim(),
                        CCCD = _securityService.DecryptData(row.I_CCCD).Replace("\0", "").Trim(),
                        Phone = _securityService.DecryptData(row.I_Phone).Replace("\0", "").Trim(),
                        BankAccount = _securityService.DecryptData(row.I_BankAccount).Replace("\0", "").Trim()
                    });
                }
                else
                {
                    decryptedTarget = (field?.ToUpperInvariant()) switch
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
            }
            swStep2.Stop();
            swTotal.Stop();

            debug.Step2Ms = swStep2.ElapsedMilliseconds;
            debug.TotalMs = swTotal.ElapsedMilliseconds;
            debug.ResultCount = results.Count;
            debug.CollisionCount = debug.CandidateCount - debug.ResultCount;

            await EnrichPatientDetailsAsync(results);

            return new PagedResult<PatientDto>
            {
                Items = results,
                Total = results.Count,
                SearchDebug = debug
            };
        }

        /// Tra cứu gần đúng V2 (BitGram / MinHash LSH Bucket) theo Họ Tên.

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

            string cleanKw = keyword.Trim();
            string normalizedKw = SecurityIndexHelper.NormalizeForSearch(cleanKw).Replace("\0", "").Trim();

            // 1. TÍNH BUCKET THEO THUẬT TOÁN MỚI: GenerateFuzzyIndex → 5 MinHash values
            // Cùng quy ước với phía INSERT (PatientService.CreateAsync / UpdateAsync)
            string fuzzyStr = _securityService.GenerateFuzzyIndex(cleanKw);
            int[] searchBuckets = ParseMinHashBuckets(fuzzyStr);

            // 2. Step 1: SQL Index Seek Query (GramBucket IN (@Buckets))
            var candidateIds = new List<int>();
            var swStep1 = Stopwatch.StartNew();

            if (searchBuckets.Length > 0)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var sqlBuilder = new StringBuilder();
                    sqlBuilder.Append(@"
                        SELECT DISTINCT PatientID
                        FROM dbo.BitGramIndex_Patient
                        WHERE GramBucket IN (");

                    for (int i = 0; i < searchBuckets.Length; i++)
                    {
                        if (i > 0) sqlBuilder.Append(", ");
                        sqlBuilder.Append($"@b{i}");
                    }
                    sqlBuilder.Append(");");

                    using var cmd = new SqlCommand(sqlBuilder.ToString(), conn);
                    for (int i = 0; i < searchBuckets.Length; i++)
                    {
                        cmd.Parameters.AddWithValue($"@b{i}", searchBuckets[i]);
                    }

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        candidateIds.Add(reader.GetInt32(0));
                    }
                }
            }

            swStep1.Stop();
            debug.Step1Ms = swStep1.ElapsedMilliseconds;
            debug.CandidateCount = candidateIds.Count;

            // 3. Step 2: Fetch Candidates & RAM Filter
            var results = new List<PatientDto>();
            var swStep2 = Stopwatch.StartNew();

            if (candidateIds.Count > 0)
            {
                var candidatesData = await FetchEncryptedRowsByIDsAsync(candidateIds);

                foreach (var row in candidatesData)
                {
                    string decName = _securityService.DecryptData(row.I_Name).Replace("\0", "").Trim();
                    if (string.IsNullOrEmpty(decName)) continue;

                    string normalizedDecryptedName = SecurityIndexHelper.NormalizeForSearch(decName).Replace("\0", "").Trim();

                    if (normalizedDecryptedName.Contains(normalizedKw, StringComparison.OrdinalIgnoreCase) ||
                        normalizedDecryptedName.Replace(" ", "").Contains(normalizedKw.Replace(" ", ""), StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(new PatientDto
                        {
                            PatientID = row.PatientID,
                            Name = decName,
                            CCCD = _securityService.DecryptData(row.I_CCCD).Replace("\0", "").Trim(),
                            Phone = _securityService.DecryptData(row.I_Phone).Replace("\0", "").Trim(),
                            BankAccount = _securityService.DecryptData(row.I_BankAccount).Replace("\0", "").Trim()
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

            await EnrichPatientDetailsAsync(results);

            return new PagedResult<PatientDto>
            {
                Items = results,
                Total = results.Count,
                SearchDebug = debug
            };
        }

        private static int[] ParseMinHashBuckets(string fuzzyBucketStr)
        {
            const string prefix = "BKT_V2_";
            if (string.IsNullOrEmpty(fuzzyBucketStr) || !fuzzyBucketStr.StartsWith(prefix))
                return Array.Empty<int>();

            string rest = fuzzyBucketStr.Substring(prefix.Length);
            if (string.IsNullOrEmpty(rest)) return Array.Empty<int>();

            // Số âm có dạng "-123456", không chứa "_", nên Split("_") vẫn đúng
            var parts = rest.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<int>();
            foreach (var part in parts)
            {
                if (int.TryParse(part, out int val))
                    result.Add(val);
            }
            return result.ToArray();
        }

        #endregion

        #region V1 BASELINE PIPELINE (UNINDEXED FULL SCAN & RAM AES DECRYPT)


        /// Tra cứu chính xác V1 Baseline (Mô phỏng hệ thống cũ chưa có chỉ mục băm).
        /// Kéo toàn bộ bảng Patient_Secure lên RAM -> Đếm CandidateCount thực tế -> Giải mã AES-256 từng dòng -> .Equals().

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

            // Step 1: Full Table Scan từ SQL (Đọc toàn bộ Patient_Secure)
            var candidates = new List<EncryptedPatientRow>();
            var swStep1 = Stopwatch.StartNew();

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string sql = @"
                    SELECT PatientID, EncryptName, EncryptCCCD, EncryptPhone, EncryptBankAccount
                    FROM dbo.Patient_Secure;";

                using var cmd = new SqlCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    candidates.Add(ReadEncryptedRow(reader));
                }
            }
            swStep1.Stop();
            debug.Step1Ms = swStep1.ElapsedMilliseconds;
            debug.CandidateCount = candidates.Count; // CandidateCount thực tế đọc được từ SQL

            // Step 2: Giải mã AES-256 từng dòng trên RAM và so sánh Equals
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

                if (decryptedTarget.Trim().Equals(cleanKw, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new PatientDto
                    {
                        PatientID = row.PatientID,
                        Name = _securityService.DecryptData(row.I_Name).Replace("\0", "").Trim(),
                        CCCD = _securityService.DecryptData(row.I_CCCD).Replace("\0", "").Trim(),
                        Phone = _securityService.DecryptData(row.I_Phone).Replace("\0", "").Trim(),
                        BankAccount = _securityService.DecryptData(row.I_BankAccount).Replace("\0", "").Trim()
                    });
                }
            }
            swStep2.Stop();
            swTotal.Stop();

            debug.Step2Ms = swStep2.ElapsedMilliseconds;
            debug.TotalMs = swTotal.ElapsedMilliseconds;
            debug.ResultCount = results.Count;
            debug.CollisionCount = debug.CandidateCount - debug.ResultCount;

            await EnrichPatientDetailsAsync(results);

            return new PagedResult<PatientDto>
            {
                Items = results,
                Total = results.Count,
                SearchDebug = debug
            };
        }


        /// Tra cứu gần đúng V1 Baseline (Mô phỏng hệ thống cũ chưa có chỉ mục băm).
        /// Kéo toàn bộ bảng Patient_Secure lên RAM -> Đếm CandidateCount thực tế -> Giải mã AES-256 từng dòng -> .Contains().

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

            string cleanKw = keyword.Trim();
            string normalizedKw = SecurityIndexHelper.NormalizeForSearch(cleanKw).Replace("\0", "").Trim();

            // Step 1: Full Table Scan từ SQL (Đọc toàn bộ Patient_Secure)
            var results = new List<PatientDto>();
            int candidateCount = 0;

            var swStep1 = Stopwatch.StartNew();
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string sql = @"
                    SELECT PatientID, EncryptName, EncryptCCCD, EncryptPhone, EncryptBankAccount
                    FROM dbo.Patient_Secure;";

                using var cmd = new SqlCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    candidateCount++;
                    var row = ReadEncryptedRow(reader);
                    string decName = _securityService.DecryptData(row.I_Name).Replace("\0", "").Trim();

                    if (!string.IsNullOrEmpty(decName))
                    {
                        string normDec = SecurityIndexHelper.NormalizeForSearch(decName).Replace("\0", "").Trim();
                        if (normDec.Contains(normalizedKw, StringComparison.OrdinalIgnoreCase) ||
                            normDec.Replace(" ", "").Contains(normalizedKw.Replace(" ", ""), StringComparison.OrdinalIgnoreCase))
                        {
                            results.Add(new PatientDto
                            {
                                PatientID = row.PatientID,
                                Name = decName,
                                CCCD = _securityService.DecryptData(row.I_CCCD).Replace("\0", "").Trim(),
                                Phone = _securityService.DecryptData(row.I_Phone).Replace("\0", "").Trim(),
                                BankAccount = _securityService.DecryptData(row.I_BankAccount).Replace("\0", "").Trim()
                            });
                        }
                    }
                }
            }
            swStep1.Stop();
            swTotal.Stop();

            debug.Step1Ms = swStep1.ElapsedMilliseconds;
            debug.Step2Ms = 0;
            debug.TotalMs = swTotal.ElapsedMilliseconds;
            debug.CandidateCount = candidateCount; // Thuc te 300.000 bản ghi
            debug.ResultCount = results.Count;
            debug.CollisionCount = debug.CandidateCount - debug.ResultCount;

            await EnrichPatientDetailsAsync(results);

            return new PagedResult<PatientDto>
            {
                Items = results,
                Total = results.Count,
                SearchDebug = debug
            };
        }

        #endregion

        #region PRIVATE HELPER METHODS

        private static EncryptedPatientRow ReadEncryptedRow(SqlDataReader reader)
        {
            byte[] nameBytes = reader.IsDBNull(1) ? Array.Empty<byte>() : (byte[])reader.GetValue(1);
            byte[] cccdBytes = reader.IsDBNull(2) ? Array.Empty<byte>() : (byte[])reader.GetValue(2);
            byte[] phoneBytes = reader.IsDBNull(3) ? Array.Empty<byte>() : (byte[])reader.GetValue(3);
            byte[] bankBytes = reader.IsDBNull(4) ? Array.Empty<byte>() : (byte[])reader.GetValue(4);


            return new EncryptedPatientRow
            {
                PatientID = reader.GetInt32(0),
                I_Name = nameBytes.Length > 0 ? Convert.ToBase64String(nameBytes) : string.Empty,
                I_CCCD = cccdBytes.Length > 0 ? Convert.ToBase64String(cccdBytes) : string.Empty,
                I_Phone = phoneBytes.Length > 0 ? Convert.ToBase64String(phoneBytes) : string.Empty,
                I_BankAccount = bankBytes.Length > 0 ? Convert.ToBase64String(bankBytes) : string.Empty
            };
        }

        private async Task<List<EncryptedPatientRow>> FetchEncryptedRowsByIDsAsync(List<int> patientIds)
        {
            var list = new List<EncryptedPatientRow>();
            if (patientIds == null || patientIds.Count == 0) return list;

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            int batchSize = 1000;
            for (int offset = 0; offset < patientIds.Count; offset += batchSize)
            {
                var batch = patientIds.Skip(offset).Take(batchSize).ToList();
                var sqlBuilder = new StringBuilder();
                sqlBuilder.Append(@"
                    SELECT PatientID, EncryptName, EncryptCCCD, EncryptPhone, EncryptBankAccount
                    FROM dbo.Patient_Secure
                    WHERE PatientID IN (");

                for (int i = 0; i < batch.Count; i++)
                {
                    if (i > 0) sqlBuilder.Append(", ");
                    sqlBuilder.Append($"@p{i}");
                }
                sqlBuilder.Append(");");

                using var cmd = new SqlCommand(sqlBuilder.ToString(), conn);
                for (int i = 0; i < batch.Count; i++)
                {
                    cmd.Parameters.AddWithValue($"@p{i}", batch[i]);
                }

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(ReadEncryptedRow(reader));
                }
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

        private async Task EnrichPatientDetailsAsync(List<PatientDto> results)
        {
            if (results == null || results.Count == 0) return;

            var idGroup = results.GroupBy(p => p.PatientID).ToDictionary(g => g.Key, g => g.ToList());
            var ids = idGroup.Keys.ToList();

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            int batchSize = 1000;
            for (int offset = 0; offset < ids.Count; offset += batchSize)
            {
                var batch = ids.Skip(offset).Take(batchSize).ToList();
                var sqlBuilder = new StringBuilder();
                sqlBuilder.Append(@"
                    SELECT PatientID, Age, Gender, BloodType, Email
                    FROM dbo.Patient
                    WHERE PatientID IN (");

                for (int i = 0; i < batch.Count; i++)
                {
                    if (i > 0) sqlBuilder.Append(", ");
                    sqlBuilder.Append($"@p{i}");
                }
                sqlBuilder.Append(");");

                using var cmd = new SqlCommand(sqlBuilder.ToString(), conn);
                for (int i = 0; i < batch.Count; i++)
                {
                    cmd.Parameters.AddWithValue($"@p{i}", batch[i]);
                }

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    int pId = reader.GetInt32(0);
                    if (idGroup.TryGetValue(pId, out var dtoList))
                    {
                        int? age = reader.IsDBNull(1) ? null : reader.GetInt32(1);
                        string? gender = reader.IsDBNull(2) ? null : reader.GetString(2).Trim();
                        string? bloodType = reader.IsDBNull(3) ? null : reader.GetString(3).Trim();
                        string? email = reader.IsDBNull(4) ? null : reader.GetString(4).Trim();

                        foreach (var dto in dtoList)
                        {
                            dto.Age = age;
                            dto.Gender = gender;
                            dto.Blood_Type = bloodType;
                            dto.Email = email;
                        }
                    }
                }
            }
        }

        #endregion
    }
}
