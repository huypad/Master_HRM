using HRM.Helpers.Security;
using HRM.Model;
using HRM.Model.Healthcare;
using Microsoft.Data.SqlClient;

namespace HRM.Services
{
    public class PatientService
    {
        private readonly string _connectionString;
        private readonly IRealSecurityService _securityService;

        public PatientService(IConfiguration configuration, IRealSecurityService securityService)
        {
            _connectionString = configuration.GetConnectionString("HealthcareConnection") 
                                ?? configuration.GetConnectionString("HRMConnection")
                                ?? throw new InvalidOperationException("Connection string 'HealthcareConnection' is missing.");
            _securityService = securityService;
        }

        public async Task<PagedResult<PatientDto>> GetPagedAsync(int page = 1, int pageSize = 20)
        {
            var result = new PagedResult<PatientDto>();

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using (var countCmd = new SqlCommand("SELECT COUNT(*) FROM Patient_Secure", conn))
            {
                result.Total = Convert.ToInt32(await countCmd.ExecuteScalarAsync() ?? 0);
            }

            var query = @"
                SELECT PatientID, I_Name, I_CCCD, I_Phone, I_Email, I_BankAccount, Age, Gender, Blood_Type
                FROM Patient_Secure
                ORDER BY PatientID
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
                cmd.Parameters.AddWithValue("@pageSize", pageSize);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Items.Add(new PatientDto
                    {
                        PatientId = reader.GetInt32(0),
                        Name = reader.IsDBNull(1) ? null : _securityService.Decrypt(reader.GetString(1)),
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

            return result;
        }

        public async Task<PatientDto?> GetByIdAsync(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = @"
                SELECT PatientID, I_Name, I_CCCD, I_Phone, I_Email, I_BankAccount, Age, Gender, Blood_Type
                FROM Patient_Secure
                WHERE PatientID = @id";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new PatientDto
                {
                    PatientId = reader.GetInt32(0),
                    Name = reader.IsDBNull(1) ? null : _securityService.Decrypt(reader.GetString(1)),
                    CCCD = reader.IsDBNull(2) ? null : _securityService.Decrypt(reader.GetString(2)),
                    Phone = reader.IsDBNull(3) ? null : _securityService.Decrypt(reader.GetString(3)),
                    Email = reader.IsDBNull(4) ? null : _securityService.Decrypt(reader.GetString(4)),
                    BankAccount = reader.IsDBNull(5) ? null : _securityService.Decrypt(reader.GetString(5)),
                    Age = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    Gender = reader.IsDBNull(7) ? null : reader.GetString(7),
                    BloodType = reader.IsDBNull(8) ? null : reader.GetString(8)
                };
            }

            return null;
        }

        public async Task<int> CreateAsync(CreatePatientModel model)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var trans = conn.BeginTransaction();

            try
            {
                // 1. Insert Patient (Plaintext)
                var sqlPatient = @"
                    INSERT INTO Patient (Name, Age, Gender, Blood_Type, CCCD, Phone, Email, BankAccount, Insurance_ID)
                    OUTPUT INSERTED.PatientID
                    VALUES (@Name, @Age, @Gender, @BloodType, @CCCD, @Phone, @Email, @BankAccount, @InsuranceId)";

                int newId;
                using (var cmd = new SqlCommand(sqlPatient, conn, trans))
                {
                    cmd.Parameters.AddWithValue("@Name", (object?)model.Name ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Age", (object?)model.Age ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Gender", (object?)model.Gender ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BloodType", (object?)model.BloodType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CCCD", (object?)model.CCCD ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Phone", (object?)model.Phone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object?)model.Email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BankAccount", (object?)model.BankAccount ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@InsuranceId", (object?)model.InsuranceId ?? DBNull.Value);
                    var idObj = await cmd.ExecuteScalarAsync();
                    newId = idObj != null ? Convert.ToInt32(idObj) : 0;
                }

                // 2. Insert Patient_Secure (Encrypted + HMAC)
                var sqlSecure = @"
                    INSERT INTO Patient_Secure (PatientID, I_Name, Age, Gender, Blood_Type, I_CCCD, CCCD_HMAC, I_Phone, Phone_HMAC, I_Email, Email_HMAC, I_BankAccount, BankAccount_HMAC, Insurance_ID)
                    VALUES (@Id, @IName, @Age, @Gender, @BloodType, @ICCCD, @CccdHmac, @IPhone, @PhoneHmac, @IEmail, @EmailHmac, @IBankAccount, @BankHmac, @InsuranceId)";

                using (var cmd = new SqlCommand(sqlSecure, conn, trans))
                {
                    cmd.Parameters.AddWithValue("@Id", newId);
                    cmd.Parameters.AddWithValue("@IName", _securityService.Encrypt(model.Name ?? ""));
                    cmd.Parameters.AddWithValue("@Age", (object?)model.Age ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Gender", (object?)model.Gender ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BloodType", (object?)model.BloodType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ICCCD", _securityService.Encrypt(model.CCCD ?? ""));
                    cmd.Parameters.AddWithValue("@CccdHmac", _securityService.ComputeHmac(model.CCCD ?? ""));
                    cmd.Parameters.AddWithValue("@IPhone", _securityService.Encrypt(model.Phone ?? ""));
                    cmd.Parameters.AddWithValue("@PhoneHmac", _securityService.ComputeHmac(model.Phone ?? ""));
                    cmd.Parameters.AddWithValue("@IEmail", _securityService.Encrypt(model.Email ?? ""));
                    cmd.Parameters.AddWithValue("@EmailHmac", _securityService.ComputeHmac(model.Email ?? ""));
                    cmd.Parameters.AddWithValue("@IBankAccount", _securityService.Encrypt(model.BankAccount ?? ""));
                    cmd.Parameters.AddWithValue("@BankHmac", _securityService.ComputeHmac(model.BankAccount ?? ""));
                    cmd.Parameters.AddWithValue("@InsuranceId", (object?)model.InsuranceId ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 3. Insert BitGramIndex_Patient
                var bigrams = _securityService.BuildBigrams(model.Name);
                if (bigrams.Any())
                {
                    var sqlIndex = "INSERT INTO BitGramIndex_Patient (PatientID, GramBucket, Position) VALUES (@Id, @Bucket, @Pos)";
                    int pos = 0;
                    foreach (var bg in bigrams)
                    {
                        var bucket = _securityService.ComputeBitgramBucket(bg);
                        using var cmd = new SqlCommand(sqlIndex, conn, trans);
                        cmd.Parameters.AddWithValue("@Id", newId);
                        cmd.Parameters.AddWithValue("@Bucket", bucket);
                        cmd.Parameters.AddWithValue("@Pos", pos++);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                await trans.CommitAsync();
                return newId;
            }
            catch
            {
                await trans.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var trans = conn.BeginTransaction();

            try
            {
                using (var cmd = new SqlCommand("DELETE FROM BitGramIndex_Patient WHERE PatientID = @id", conn, trans))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    await cmd.ExecuteNonQueryAsync();
                }

                using (var cmd = new SqlCommand("DELETE FROM Patient_Secure WHERE PatientID = @id", conn, trans))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    await cmd.ExecuteNonQueryAsync();
                }

                int rows;
                using (var cmd = new SqlCommand("DELETE FROM Patient WHERE PatientID = @id", conn, trans))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    rows = await cmd.ExecuteNonQueryAsync();
                }

                await trans.CommitAsync();
                return rows > 0;
            }
            catch
            {
                await trans.RollbackAsync();
                throw;
            }
        }

        public async Task RebuildBitgramIndexAsync()
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            // Xóa index cũ
            using (var cmd = new SqlCommand("TRUNCATE TABLE BitGramIndex_Patient", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Đọc toàn bộ Patient_Secure -> decrypt I_Name -> build bigrams & insert
            var patients = new List<(int Id, string Name)>();
            using (var cmd = new SqlCommand("SELECT PatientID, I_Name FROM Patient_Secure", conn))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    int id = reader.GetInt32(0);
                    string encName = reader.IsDBNull(1) ? "" : reader.GetString(1);
                    string decName = _securityService.Decrypt(encName);
                    patients.Add((id, decName));
                }
            }

            // Insert lại theo batch
            int chunkSize = 1000;
            for (int i = 0; i < patients.Count; i += chunkSize)
            {
                var chunk = patients.Skip(i).Take(chunkSize);
                using var trans = conn.BeginTransaction();

                foreach (var (id, name) in chunk)
                {
                    var bigrams = _securityService.BuildBigrams(name);
                    int pos = 0;
                    foreach (var bg in bigrams)
                    {
                        var bucket = _securityService.ComputeBitgramBucket(bg);
                        var sql = "INSERT INTO BitGramIndex_Patient (PatientID, GramBucket, Position) VALUES (@Id, @Bucket, @Pos)";
                        using var cmd = new SqlCommand(sql, conn, trans);
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@Bucket", bucket);
                        cmd.Parameters.AddWithValue("@Pos", pos++);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                await trans.CommitAsync();
            }
        }
    }
}
