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

             
                string countSql = "SELECT COUNT(*) FROM dbo.Patient;";
                using (var countCmd = new SqlCommand(countSql, conn))
                {
                    total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                }

              
                string pageSql = @"
                    SELECT PatientID, FullName, CCCD, Phone, BankAccount, Age, Gender, BloodType, Email
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
                            Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1).Trim(),
                            CCCD = reader.IsDBNull(2) ? string.Empty : reader.GetString(2).Trim(),
                            Phone = reader.IsDBNull(3) ? string.Empty : reader.GetString(3).Trim(),
                            BankAccount = reader.IsDBNull(4) ? string.Empty : reader.GetString(4).Trim(),
                            Age = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                            Gender = reader.IsDBNull(6) ? null : reader.GetString(6).Trim(),
                            Blood_Type = reader.IsDBNull(7) ? null : reader.GetString(7).Trim(),
                            Email = reader.IsDBNull(8) ? null : reader.GetString(8).Trim()
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
                SELECT PatientID, FullName, CCCD, Phone, BankAccount, Age, Gender, BloodType, Email
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
                    Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1).Trim(),
                    CCCD = reader.IsDBNull(2) ? string.Empty : reader.GetString(2).Trim(),
                    Phone = reader.IsDBNull(3) ? string.Empty : reader.GetString(3).Trim(),
                    BankAccount = reader.IsDBNull(4) ? string.Empty : reader.GetString(4).Trim(),
                    Age = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    Gender = reader.IsDBNull(6) ? null : reader.GetString(6).Trim(),
                    Blood_Type = reader.IsDBNull(7) ? null : reader.GetString(7).Trim(),
                    Email = reader.IsDBNull(8) ? null : reader.GetString(8).Trim()
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

              
                string insertPatientSql = @"
                    INSERT INTO dbo.Patient (FullName, CCCD, Phone, BankAccount, Age, Gender, BloodType, Email)
                    VALUES (@FullName, @CCCD, @Phone, @BankAccount, @Age, @Gender, @BloodType, @Email);
                    SELECT SCOPE_IDENTITY();";

                int newId;
                using (var cmd = new SqlCommand(insertPatientSql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@FullName", nameStr);
                    cmd.Parameters.AddWithValue("@CCCD", cccdStr);
                    cmd.Parameters.AddWithValue("@Phone", phoneStr);
                    cmd.Parameters.AddWithValue("@BankAccount", bankStr);
                    cmd.Parameters.AddWithValue("@Age", (object?)model.Age ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Gender", (object?)model.Gender ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BloodType", (object?)model.Blood_Type ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object?)model.Email ?? DBNull.Value);

                    newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

               
                string encNameBase64 = _securityService.EncryptData(nameStr);
                string encCCCDBase64 = _securityService.EncryptData(cccdStr);
                string encPhoneBase64 = _securityService.EncryptData(phoneStr);
                string encBankBase64 = _securityService.EncryptData(bankStr);

                byte[] encNameBytes = !string.IsNullOrEmpty(encNameBase64) ? Convert.FromBase64String(encNameBase64) : Array.Empty<byte>();
                byte[] encCCCDBytes = !string.IsNullOrEmpty(encCCCDBase64) ? Convert.FromBase64String(encCCCDBase64) : Array.Empty<byte>();
                byte[] encPhoneBytes = !string.IsNullOrEmpty(encPhoneBase64) ? Convert.FromBase64String(encPhoneBase64) : Array.Empty<byte>();
                byte[] encBankBytes = !string.IsNullOrEmpty(encBankBase64) ? Convert.FromBase64String(encBankBase64) : Array.Empty<byte>();

                string hmacCCCD = _securityService.GenerateExactIndex(cccdStr);
                string hmacPhone = _securityService.GenerateExactIndex(phoneStr);
                string hmacBank = _securityService.GenerateExactIndex(bankStr);

                string insertSecureSql = @"
                    INSERT INTO dbo.Patient_Secure 
                    (PatientID, EncryptName, EncryptCCCD, EncryptPhone, EncryptBankAccount, CCCD_HMAC, Phone_HMAC, Bank_HMAC, CreatedAt)
                    VALUES 
                    (@PatientID, @EncryptName, @EncryptCCCD, @EncryptPhone, @EncryptBankAccount, @CCCD_HMAC, @Phone_HMAC, @Bank_HMAC, GETDATE());";

                using (var cmd = new SqlCommand(insertSecureSql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@PatientID", newId);
                    cmd.Parameters.Add("@EncryptName", SqlDbType.VarBinary, -1).Value = (object)encNameBytes ?? DBNull.Value;
                    cmd.Parameters.Add("@EncryptCCCD", SqlDbType.VarBinary, -1).Value = (object)encCCCDBytes ?? DBNull.Value;
                    cmd.Parameters.Add("@EncryptPhone", SqlDbType.VarBinary, -1).Value = (object)encPhoneBytes ?? DBNull.Value;
                    cmd.Parameters.Add("@EncryptBankAccount", SqlDbType.VarBinary, -1).Value = (object)encBankBytes ?? DBNull.Value;
                    cmd.Parameters.AddWithValue("@CCCD_HMAC", hmacCCCD);
                    cmd.Parameters.AddWithValue("@Phone_HMAC", hmacPhone);
                    cmd.Parameters.AddWithValue("@Bank_HMAC", hmacBank);

                    await cmd.ExecuteNonQueryAsync();
                }

             
                string fuzzyBucketStr = _securityService.GenerateFuzzyIndex(nameStr);
                if (!string.IsNullOrEmpty(fuzzyBucketStr))
                {
                    int[] buckets = ParseMinHashBuckets(fuzzyBucketStr);

                    string insertBitGramSql = @"
                        INSERT INTO dbo.BitGramIndex_Patient (PatientID, GramBucket, GramPosition)
                        VALUES (@PatientID, @GramBucket, @GramPosition);";

                    for (int pos = 0; pos < buckets.Length; pos++)
                    {
                        using var cmd = new SqlCommand(insertBitGramSql, conn, tx);
                        cmd.Parameters.AddWithValue("@PatientID", newId);
                        cmd.Parameters.AddWithValue("@GramBucket", buckets[pos]);
                        cmd.Parameters.AddWithValue("@GramPosition", pos);
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

                
                string updatePatientSql = @"
                    UPDATE dbo.Patient
                    SET FullName = @FullName, CCCD = @CCCD, Phone = @Phone, BankAccount = @BankAccount,
                        Age = @Age, Gender = @Gender, BloodType = @BloodType, Email = @Email
                    WHERE PatientID = @Id;";

                int rows;
                using (var cmd = new SqlCommand(updatePatientSql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@FullName", nameStr);
                    cmd.Parameters.AddWithValue("@CCCD", cccdStr);
                    cmd.Parameters.AddWithValue("@Phone", phoneStr);
                    cmd.Parameters.AddWithValue("@BankAccount", bankStr);
                    cmd.Parameters.AddWithValue("@Age", (object?)model.Age ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Gender", (object?)model.Gender ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BloodType", (object?)model.Blood_Type ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object?)model.Email ?? DBNull.Value);

                    rows = await cmd.ExecuteNonQueryAsync();
                }

                if (rows == 0)
                {
                    await tx.RollbackAsync();
                    return false;
                }

              
                string encNameBase64 = _securityService.EncryptData(nameStr);
                string encCCCDBase64 = _securityService.EncryptData(cccdStr);
                string encPhoneBase64 = _securityService.EncryptData(phoneStr);
                string encBankBase64 = _securityService.EncryptData(bankStr);

                byte[] encNameBytes = !string.IsNullOrEmpty(encNameBase64) ? Convert.FromBase64String(encNameBase64) : Array.Empty<byte>();
                byte[] encCCCDBytes = !string.IsNullOrEmpty(encCCCDBase64) ? Convert.FromBase64String(encCCCDBase64) : Array.Empty<byte>();
                byte[] encPhoneBytes = !string.IsNullOrEmpty(encPhoneBase64) ? Convert.FromBase64String(encPhoneBase64) : Array.Empty<byte>();
                byte[] encBankBytes = !string.IsNullOrEmpty(encBankBase64) ? Convert.FromBase64String(encBankBase64) : Array.Empty<byte>();

                string hmacCCCD = _securityService.GenerateExactIndex(cccdStr);
                string hmacPhone = _securityService.GenerateExactIndex(phoneStr);
                string hmacBank = _securityService.GenerateExactIndex(bankStr);

                string updateSecureSql = @"
                    UPDATE dbo.Patient_Secure
                    SET EncryptName = @EncryptName, EncryptCCCD = @EncryptCCCD, EncryptPhone = @EncryptPhone, EncryptBankAccount = @EncryptBankAccount,
                        CCCD_HMAC = @CCCD_HMAC, Phone_HMAC = @Phone_HMAC, Bank_HMAC = @Bank_HMAC
                    WHERE PatientID = @Id;";

                using (var cmd = new SqlCommand(updateSecureSql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.Add("@EncryptName", SqlDbType.VarBinary, -1).Value = (object)encNameBytes ?? DBNull.Value;
                    cmd.Parameters.Add("@EncryptCCCD", SqlDbType.VarBinary, -1).Value = (object)encCCCDBytes ?? DBNull.Value;
                    cmd.Parameters.Add("@EncryptPhone", SqlDbType.VarBinary, -1).Value = (object)encPhoneBytes ?? DBNull.Value;
                    cmd.Parameters.Add("@EncryptBankAccount", SqlDbType.VarBinary, -1).Value = (object)encBankBytes ?? DBNull.Value;
                    cmd.Parameters.AddWithValue("@CCCD_HMAC", hmacCCCD);
                    cmd.Parameters.AddWithValue("@Phone_HMAC", hmacPhone);
                    cmd.Parameters.AddWithValue("@Bank_HMAC", hmacBank);

                    await cmd.ExecuteNonQueryAsync();
                }

                string delBitGramSql = "DELETE FROM dbo.BitGramIndex_Patient WHERE PatientID = @Id;";
                using (var cmd = new SqlCommand(delBitGramSql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    await cmd.ExecuteNonQueryAsync();
                }

             
                string fuzzyBucketStr = _securityService.GenerateFuzzyIndex(nameStr);
                if (!string.IsNullOrEmpty(fuzzyBucketStr))
                {
                    int[] buckets = ParseMinHashBuckets(fuzzyBucketStr);

                    string insertBitGramSql = @"
                        INSERT INTO dbo.BitGramIndex_Patient (PatientID, GramBucket, GramPosition)
                        VALUES (@Id, @GramBucket, @GramPosition);";

                    for (int pos = 0; pos < buckets.Length; pos++)
                    {
                        using var cmd = new SqlCommand(insertBitGramSql, conn, tx);
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@GramBucket", buckets[pos]);
                        cmd.Parameters.AddWithValue("@GramPosition", pos);
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
               
                string delBitGram = "DELETE FROM dbo.BitGramIndex_Patient WHERE PatientID = @Id;";
                using (var cmd = new SqlCommand(delBitGram, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    await cmd.ExecuteNonQueryAsync();
                }

              
                string delSecure = "DELETE FROM dbo.Patient_Secure WHERE PatientID = @Id;";
                using (var cmd = new SqlCommand(delSecure, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    await cmd.ExecuteNonQueryAsync();
                }

              
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

      
      
        private static int[] ParseMinHashBuckets(string fuzzyBucketStr)
        {
            const string prefix = "BKT_V2_";
            if (string.IsNullOrEmpty(fuzzyBucketStr) || !fuzzyBucketStr.StartsWith(prefix))
                return Array.Empty<int>();

            string rest = fuzzyBucketStr.Substring(prefix.Length);
            if (string.IsNullOrEmpty(rest)) return Array.Empty<int>();

         
            var parts = rest.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<int>();
            foreach (var part in parts)
            {
                if (int.TryParse(part, out int val))
                    result.Add(val);
            }
            return result.ToArray();
        }
    }
}
