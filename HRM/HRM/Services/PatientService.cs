using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
    
    /// Triển khai Quản lý Bệnh nhân (CRUD) trực tiếp qua ADO.NET trên HealthcareDB.
    /// Tự động đồng bộ mã hóa AES-256, HMAC exact index và BitGram 16-bit bucket index.
   
    public class PatientService : IPatientService
    {
        private readonly string _connectionString;
        private readonly IHybridSecurityService _securityService;

        public PatientService(IConfiguration configuration, IHybridSecurityService securityService)
        {
            _connectionString = configuration.GetConnectionString("HealthcareConnection")
                ?? throw new InvalidOperationException("Connection string 'HealthcareConnection' is missing in appsettings.json.");
            _securityService = securityService;
        }

        public async Task<PagedResult<PatientDto>> GetPagedAsync(int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            int offset = (page - 1) * pageSize;

            var items = new List<PatientDto>();
            int total = 0;

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                // 1. Dem tong so phan tu
                string countSql = "SELECT COUNT(*) FROM dbo.Patient;";
                using (var countCmd = new SqlCommand(countSql, conn))
                {
                    total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                }

                // 2. Lay du lieu phan trang
                string pageSql = @"
                    SELECT PatientID, Name, CCCD, Phone, BankAccount, Age, Gender, Blood_Type, Email
                    FROM dbo.Patient
                    ORDER BY PatientID DESC
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                using (var pageCmd = new SqlCommand(pageSql, conn))
                {
                    pageCmd.Parameters.AddWithValue("@Offset", offset);
                    pageCmd.Parameters.AddWithValue("@PageSize", pageSize);

                    using var reader = await pageCmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        items.Add(new PatientDto
                        {
                            PatientID = reader.GetInt32(0),
                            Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            CCCD = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            Phone = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            BankAccount = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                            Age = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                            Gender = reader.IsDBNull(6) ? null : reader.GetString(6),
                            Blood_Type = reader.IsDBNull(7) ? null : reader.GetString(7),
                            Email = reader.IsDBNull(8) ? null : reader.GetString(8)
                        });
                    }
                }
            }

            return new PagedResult<PatientDto>
            {
                Items = items,
                Total = total
            };
        }

        public async Task<PatientDto?> GetByIdAsync(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
                SELECT PatientID, Name, CCCD, Phone, BankAccount, Age, Gender, Blood_Type, Email
                FROM dbo.Patient
                WHERE PatientID = @Id;";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new PatientDto
                {
                    PatientID = reader.GetInt32(0),
                    Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    CCCD = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Phone = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    BankAccount = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    Age = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    Gender = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Blood_Type = reader.IsDBNull(7) ? null : reader.GetString(7),
                    Email = reader.IsDBNull(8) ? null : reader.GetString(8)
                };
            }

            return null;
        }

        public async Task<PatientDto> CreateAsync(CreatePatientModel model)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();

            try
            {
                string nameStr = model.Name ?? string.Empty;
                string cccdStr = model.CCCD ?? string.Empty;
                string phoneStr = model.Phone ?? string.Empty;
                string bankStr = model.BankAccount ?? string.Empty;

                // 1. Insert vao bang Patient (Plaintext)
                string insertPatientSql = @"
                    INSERT INTO dbo.Patient (Name, CCCD, Phone, BankAccount, Age, Gender, Blood_Type, Email)
                    VALUES (@Name, @CCCD, @Phone, @BankAccount, @Age, @Gender, @Blood_Type, @Email);
                    SELECT SCOPE_IDENTITY();";

                int newId;
                using (var cmd = new SqlCommand(insertPatientSql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Name", nameStr);
                    cmd.Parameters.AddWithValue("@CCCD", cccdStr);
                    cmd.Parameters.AddWithValue("@Phone", phoneStr);
                    cmd.Parameters.AddWithValue("@BankAccount", bankStr);
                    cmd.Parameters.AddWithValue("@Age", model.Age);
                    cmd.Parameters.AddWithValue("@Gender", (object?)model.Gender ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Blood_Type", (object?)model.Blood_Type ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object?)model.Email ?? DBNull.Value);

                    newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // 2. Ma hoa AES va sinh HMAC
                string encName = _securityService.EncryptData(nameStr);
                string encCCCD = _securityService.EncryptData(cccdStr);
                string encPhone = _securityService.EncryptData(phoneStr);
                string encBank = _securityService.EncryptData(bankStr);

                string hmacCCCD = _securityService.GenerateExactIndex(cccdStr);
                string hmacPhone = _securityService.GenerateExactIndex(phoneStr);
                string hmacBank = _securityService.GenerateExactIndex(bankStr);

                string insertSecureSql = @"
                    INSERT INTO dbo.Patient_Secure 
                    (PatientID, I_Name, I_CCCD, I_Phone, I_BankAccount, CCCD_HMAC, Phone_HMAC, Bank_HMAC, SeededAt)
                    VALUES 
                    (@PatientID, @I_Name, @I_CCCD, @I_Phone, @I_BankAccount, @CCCD_HMAC, @Phone_HMAC, @Bank_HMAC, GETDATE());";

                using (var cmd = new SqlCommand(insertSecureSql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@PatientID", newId);
                    cmd.Parameters.AddWithValue("@I_Name", encName);
                    cmd.Parameters.AddWithValue("@I_CCCD", encCCCD);
                    cmd.Parameters.AddWithValue("@I_Phone", encPhone);
                    cmd.Parameters.AddWithValue("@I_BankAccount", encBank);
                    cmd.Parameters.AddWithValue("@CCCD_HMAC", hmacCCCD);
                    cmd.Parameters.AddWithValue("@Phone_HMAC", hmacPhone);
                    cmd.Parameters.AddWithValue("@Bank_HMAC", hmacBank);

                    await cmd.ExecuteNonQueryAsync();
                }

                // 3. Sinh BitGram Bucket Index cho Name va Insert vao BitGramIndex_Patient
                string normalizedName = SecurityIndexHelper.NormalizeForSearch(nameStr);
                var trigrams = SecurityIndexHelper.BuildNgrams(normalizedName, 3);

                if (trigrams.Count > 0)
                {
                    string insertBitGramSql = @"
                        INSERT INTO dbo.BitGramIndex_Patient (PatientID, GramBucket, Position)
                        VALUES (@PatientID, @GramBucket, @Position);";

                    for (int pos = 0; pos < trigrams.Count; pos++)
                    {
                        string gram = trigrams[pos];
                        string bucket = _securityService.GenerateFuzzyIndex(gram);

                        using var cmd = new SqlCommand(insertBitGramSql, conn, tx);
                        cmd.Parameters.AddWithValue("@PatientID", newId);
                        cmd.Parameters.AddWithValue("@GramBucket", bucket);
                        cmd.Parameters.AddWithValue("@Position", pos);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();

                return new PatientDto
                {
                    PatientID = newId,
                    Name = nameStr,
                    CCCD = cccdStr,
                    Phone = phoneStr,
                    BankAccount = bankStr,
                    Age = model.Age,
                    Gender = model.Gender,
                    Blood_Type = model.Blood_Type,
                    Email = model.Email
                };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateAsync(int id, CreatePatientModel model)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();

            try
            {
                string nameStr = model.Name ?? string.Empty;
                string cccdStr = model.CCCD ?? string.Empty;
                string phoneStr = model.Phone ?? string.Empty;
                string bankStr = model.BankAccount ?? string.Empty;

                // 1. Cap nhat Patient (Plaintext)
                string updatePatientSql = @"
                    UPDATE dbo.Patient
                    SET Name = @Name, CCCD = @CCCD, Phone = @Phone, BankAccount = @BankAccount,
                        Age = @Age, Gender = @Gender, Blood_Type = @Blood_Type, Email = @Email
                    WHERE PatientID = @Id;";

                int rows;
                using (var cmd = new SqlCommand(updatePatientSql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Name", nameStr);
                    cmd.Parameters.AddWithValue("@CCCD", cccdStr);
                    cmd.Parameters.AddWithValue("@Phone", phoneStr);
                    cmd.Parameters.AddWithValue("@BankAccount", bankStr);
                    cmd.Parameters.AddWithValue("@Age", model.Age);
                    cmd.Parameters.AddWithValue("@Gender", (object?)model.Gender ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Blood_Type", (object?)model.Blood_Type ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object?)model.Email ?? DBNull.Value);

                    rows = await cmd.ExecuteNonQueryAsync();
                }

                if (rows == 0)
                {
                    await tx.RollbackAsync();
                    return false;
                }

                // 2. Re-encrypt & Re-index Patient_Secure
                string encName = _securityService.EncryptData(nameStr);
                string encCCCD = _securityService.EncryptData(cccdStr);
                string encPhone = _securityService.EncryptData(phoneStr);
                string encBank = _securityService.EncryptData(bankStr);

                string hmacCCCD = _securityService.GenerateExactIndex(cccdStr);
                string hmacPhone = _securityService.GenerateExactIndex(phoneStr);
                string hmacBank = _securityService.GenerateExactIndex(bankStr);

                string updateSecureSql = @"
                    UPDATE dbo.Patient_Secure
                    SET I_Name = @I_Name, I_CCCD = @I_CCCD, I_Phone = @I_Phone, I_BankAccount = @I_BankAccount,
                        CCCD_HMAC = @CCCD_HMAC, Phone_HMAC = @Phone_HMAC, Bank_HMAC = @Bank_HMAC
                    WHERE PatientID = @Id;";

                using (var cmd = new SqlCommand(updateSecureSql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@I_Name", encName);
                    cmd.Parameters.AddWithValue("@I_CCCD", encCCCD);
                    cmd.Parameters.AddWithValue("@I_Phone", encPhone);
                    cmd.Parameters.AddWithValue("@I_BankAccount", encBank);
                    cmd.Parameters.AddWithValue("@CCCD_HMAC", hmacCCCD);
                    cmd.Parameters.AddWithValue("@Phone_HMAC", hmacPhone);
                    cmd.Parameters.AddWithValue("@Bank_HMAC", hmacBank);

                    await cmd.ExecuteNonQueryAsync();
                }

                // 3. Rebuild BitGram Index
                string delBitGramSql = "DELETE FROM dbo.BitGramIndex_Patient WHERE PatientID = @Id;";
                using (var cmd = new SqlCommand(delBitGramSql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    await cmd.ExecuteNonQueryAsync();
                }

                string normalizedName = SecurityIndexHelper.NormalizeForSearch(nameStr);
                var trigrams = SecurityIndexHelper.BuildNgrams(normalizedName, 3);

                if (trigrams.Count > 0)
                {
                    string insertBitGramSql = @"
                        INSERT INTO dbo.BitGramIndex_Patient (PatientID, GramBucket, Position)
                        VALUES (@Id, @GramBucket, @Position);";

                    for (int pos = 0; pos < trigrams.Count; pos++)
                    {
                        string gram = trigrams[pos];
                        string bucket = _securityService.GenerateFuzzyIndex(gram);

                        using var cmd = new SqlCommand(insertBitGramSql, conn, tx);
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@GramBucket", bucket);
                        cmd.Parameters.AddWithValue("@Position", pos);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();
                return true;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();

            try
            {
                // 1. Delete BitGramIndex_Patient
                string delBitGram = "DELETE FROM dbo.BitGramIndex_Patient WHERE PatientID = @Id;";
                using (var cmd = new SqlCommand(delBitGram, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 2. Delete Patient_Secure
                string delSecure = "DELETE FROM dbo.Patient_Secure WHERE PatientID = @Id;";
                using (var cmd = new SqlCommand(delSecure, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 3. Delete Patient
                string delPatient = "DELETE FROM dbo.Patient WHERE PatientID = @Id;";
                int rows;
                using (var cmd = new SqlCommand(delPatient, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    rows = await cmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return rows > 0;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
