using System.Data;
using System.Diagnostics;
using HRM.Helpers.Security;
using HRM.Model.Healthcare;
using Microsoft.Data.SqlClient;

namespace HRM.Services
{
    public class PatientSearchService
    {
        private readonly string _connectionString;
        private readonly IRealSecurityService _securityService;

        public PatientSearchService(IConfiguration configuration, IRealSecurityService securityService)
        {
            _connectionString = configuration.GetConnectionString("HealthcareConnection") 
                                ?? configuration.GetConnectionString("HRMConnection")
                                ?? throw new InvalidOperationException("Connection string 'HealthcareConnection' is missing.");
            _securityService = securityService;
        }

        /// <summary>
        /// V1 Baseline: Query trực tiếp trên cột Name (Plaintext) dùng SQL LIKE '%keyword%'.
        /// </summary>
        public async Task<AlgoSearchResult> SearchV1Async(string keyword, int page = 1, int pageSize = 20)
        {
            var sw = Stopwatch.StartNew();
            var result = new AlgoSearchResult { AlgoName = "V1_Baseline (Plaintext LIKE)" };

            if (string.IsNullOrWhiteSpace(keyword))
                return result;

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            // Dem tong so ban ghi trong database
            using (var countCmd = new SqlCommand("SELECT COUNT(*) FROM Patient", conn))
            {
                result.TotalRecords = (int)(await countCmd.ExecuteScalarAsync() ?? 0);
            }

            var query = @"
                SELECT PatientID, Name, Age, Gender, Blood_Type, CCCD, Phone, Email, BankAccount
                FROM Patient
                WHERE LOWER(Name) LIKE '%' + LOWER(@kw) + '%'
                ORDER BY PatientID
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@kw", keyword.Trim());
                cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
                cmd.Parameters.AddWithValue("@pageSize", pageSize);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Results.Add(new PatientDto
                    {
                        PatientId = reader.GetInt32(0),
                        Name = reader.IsDBNull(1) ? null : reader.GetString(1),
                        Age = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                        Gender = reader.IsDBNull(3) ? null : reader.GetString(3),
                        BloodType = reader.IsDBNull(4) ? null : reader.GetString(4),
                        CCCD = reader.IsDBNull(5) ? null : reader.GetString(5),
                        Phone = reader.IsDBNull(6) ? null : reader.GetString(6),
                        Email = reader.IsDBNull(7) ? null : reader.GetString(7),
                        BankAccount = reader.IsDBNull(8) ? null : reader.GetString(8)
                    });
                }
            }

            sw.Stop();
            result.TotalMs = sw.ElapsedMilliseconds;
            result.Step1Ms = result.TotalMs;
            result.Step2Ms = 0;
            result.ResultCount = result.Results.Count;
            result.CandidateCount = result.ResultCount;
            result.ScannedCount = result.TotalRecords;

            return result;
        }

        /// <summary>
        /// V2 HMAC/BitGram: 
        /// Step 1: Lọc candidates qua BitGramIndex_Patient
        /// Step 2: Giải mã AES-256 I_Name trong RAM và verify với keyword thật.
        /// </summary>
        public async Task<AlgoSearchResult> SearchV2Async(string keyword, int page = 1, int pageSize = 20)
        {
            var totalSw = Stopwatch.StartNew();
            var result = new AlgoSearchResult { AlgoName = "V2_BitGram (HMAC Index + AES Decrypt)" };

            if (string.IsNullOrWhiteSpace(keyword))
                return result;

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            // Đếm tổng số bản ghi
            using (var countCmd = new SqlCommand("SELECT COUNT(*) FROM Patient_Secure", conn))
            {
                result.TotalRecords = Convert.ToInt32(await countCmd.ExecuteScalarAsync() ?? 0);
            }

            // --- STEP 1: BitGram Index Lookup ---
            var step1Sw = Stopwatch.StartNew();
            var bigrams = _securityService.BuildBigrams(keyword);
            var buckets = bigrams.Select(_securityService.ComputeBitgramBucket).Distinct().ToList();

            var candidateIds = new List<int>();

            if (buckets.Any())
            {
                var bucketParams = string.Join(",", buckets.Select((b, i) => $"@b{i}"));
                var step1Sql = $@"
                    SELECT PatientID
                    FROM BitGramIndex_Patient
                    WHERE GramBucket IN ({bucketParams})
                    GROUP BY PatientID
                    HAVING COUNT(DISTINCT GramBucket) >= @minMatch";

                using var cmd1 = new SqlCommand(step1Sql, conn);
                for (int i = 0; i < buckets.Count; i++)
                {
                    cmd1.Parameters.AddWithValue($"@b{i}", buckets[i]);
                }
                cmd1.Parameters.AddWithValue("@minMatch", Math.Max(1, buckets.Count));

                using var reader1 = await cmd1.ExecuteReaderAsync();
                while (await reader1.ReadAsync())
                {
                    candidateIds.Add(reader1.GetInt32(0));
                }
            }
            step1Sw.Stop();
            result.Step1Ms = step1Sw.ElapsedMilliseconds;
            result.CandidateCount = candidateIds.Count;

            if (!candidateIds.Any())
            {
                totalSw.Stop();
                result.TotalMs = totalSw.ElapsedMilliseconds;
                return result;
            }

            // --- STEP 2: Decrypt & In-RAM Match ---
            var step2Sw = Stopwatch.StartNew();

            // Query các candidate records
            var dt = new DataTable();
            dt.Columns.Add("PatientID", typeof(int));
            foreach (var id in candidateIds) dt.Rows.Add(id);

            var step2Sql = @"
                SELECT ps.PatientID, ps.I_Name, ps.I_CCCD, ps.I_Phone, ps.I_Email, ps.I_BankAccount, ps.Age, ps.Gender, ps.Blood_Type
                FROM Patient_Secure ps
                INNER JOIN @CandidateIds c ON ps.PatientID = c.PatientID";

            var matchedPatients = new List<PatientDto>();
            var normalizedKeyword = _securityService.NormalizeName(keyword);

            using (var cmd2 = new SqlCommand(step2Sql, conn))
            {
                var p = cmd2.Parameters.AddWithValue("@CandidateIds", dt);
                p.SqlDbType = SqlDbType.Structured;
                p.TypeName = "dbo.PatientIdListType"; // Trường hợp DB chưa có Type này, ta xài IN clause fallback bên dưới

                try
                {
                    using var reader2 = await cmd2.ExecuteReaderAsync();
                    while (await reader2.ReadAsync())
                    {
                        result.ScannedCount++;
                        var encName = reader2.IsDBNull(1) ? "" : reader2.GetString(1);
                        var decryptedName = _securityService.Decrypt(encName);
                        var normName = _securityService.NormalizeName(decryptedName);

                        if (normName.Contains(normalizedKeyword))
                        {
                            matchedPatients.Add(new PatientDto
                            {
                                PatientId = reader2.GetInt32(0),
                                Name = decryptedName,
                                CCCD = reader2.IsDBNull(2) ? null : _securityService.Decrypt(reader2.GetString(2)),
                                Phone = reader2.IsDBNull(3) ? null : _securityService.Decrypt(reader2.GetString(3)),
                                Email = reader2.IsDBNull(4) ? null : _securityService.Decrypt(reader2.GetString(4)),
                                BankAccount = reader2.IsDBNull(5) ? null : _securityService.Decrypt(reader2.GetString(5)),
                                Age = reader2.IsDBNull(6) ? null : reader2.GetInt32(6),
                                Gender = reader2.IsDBNull(7) ? null : reader2.GetString(7),
                                BloodType = reader2.IsDBNull(8) ? null : reader2.GetString(8)
                            });
                        }
                    }
                }
                catch (SqlException)
                {
                    // Fallback nếu chưa tạo TVP Type: dùng IN clause chunking
                    matchedPatients = await FallbackFetchAndDecrypt(conn, candidateIds, normalizedKeyword, result);
                }
            }

            step2Sw.Stop();
            result.Step2Ms = step2Sw.ElapsedMilliseconds;

            // Paging
            result.ResultCount = matchedPatients.Count;
            result.Results = matchedPatients
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            totalSw.Stop();
            result.TotalMs = totalSw.ElapsedMilliseconds;

            return result;
        }

        private async Task<List<PatientDto>> FallbackFetchAndDecrypt(SqlConnection conn, List<int> ids, string normKeyword, AlgoSearchResult result)
        {
            var list = new List<PatientDto>();
            int chunkSize = 1000;

            for (int i = 0; i < ids.Count; i += chunkSize)
            {
                var chunk = ids.Skip(i).Take(chunkSize).ToList();
                var idParams = string.Join(",", chunk);
                var sql = $@"
                    SELECT PatientID, I_Name, I_CCCD, I_Phone, I_Email, I_BankAccount, Age, Gender, Blood_Type
                    FROM Patient_Secure
                    WHERE PatientID IN ({idParams})";

                using var cmd = new SqlCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.ScannedCount++;
                    var encName = reader.IsDBNull(1) ? "" : reader.GetString(1);
                    var decryptedName = _securityService.Decrypt(encName);
                    var normName = _securityService.NormalizeName(decryptedName);

                    if (normName.Contains(normKeyword))
                    {
                        list.Add(new PatientDto
                        {
                            PatientId = reader.GetInt32(0),
                            Name = decryptedName,
                            CCCD = reader.IsDBNull(2) ? null : _securityService.Decrypt(reader.GetString(2)),
                            Phone = reader.IsDBNull(3) ? null : _securityService.Decrypt(reader.GetString(3)),
                            Email = reader.IsDBNull(4) ? null : _securityService.Decrypt(reader.GetString(4)),
                            BankAccount = reader.IsDBNull(5) ? null : _securityService.Decrypt(reader.GetString(5)),
                            Age = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                            Gender = reader.IsDBNull(7) ? null : reader.GetString(7),
                            BloodType = reader.IsDBNull(8) ? null : reader.GetString(8)
                        });
                    }
                }
            }

            return list;
        }
    }
}
