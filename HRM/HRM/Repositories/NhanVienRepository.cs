using HRM.Data;
using HRM.DTOs;
using HRM.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using HRM.Common;
using HRM.Helpers.Security;

namespace HRM.Repositories
{
    public class NhanVienRepository : INhanVienRepository
    {
        private readonly HrmDbContext _context;

        public NhanVienRepository(HrmDbContext context)
        {
            _context = context;
        }

        public static class SortDirectionConst
        {
            public const string Asc = "asc";
            public const string Desc = "desc";
        }

        private IQueryable<NhanVien> ApplySorting(
            IQueryable<NhanVien> query,
            string? sortColumn,
            string? sortDirection
        )
        {
            if (string.IsNullOrWhiteSpace(sortColumn))
            {
                return query;
            }

            var isDesc = sortDirection?.ToLower() == "desc";

            return sortColumn switch
            {
                "ngaySinh" =>
                    isDesc ? query.OrderByDescending(x => x.Ngaysinh)
                           : query.OrderBy(x => x.Ngaysinh),

                _ => query
            };
        }

        public async Task<int> CountPublicAsync()
        {
            return await _context.NhanViens
                .CountAsync(x => x.Disable == false || x.Disable == null);
        }

        public async Task<int> CountPrivateAsync()
        {
            var allItems = await GetAllPrivateAsync();
            return allItems.Count;
        }

        public async Task<List<NhanVienDTO>> GetPagedPublicAsync(
            int page,
            int pageSize,
            string? sortColumn,
            string? sortDirection
        )
        {
            var query = _context.NhanViens
                .AsNoTracking()
                .Where(x => x.Disable == false || x.Disable == null);

            query = ApplySorting(query, sortColumn, sortDirection);

            if (string.IsNullOrWhiteSpace(sortColumn))
            {
                query = query.OrderBy(x => x.Id_NV);
            }

            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new NhanVienDTO
                {
                    Id_NV = x.Id_NV,
                    MaNV = x.MaNV,
                    HoTen = "*******",     
                    NgaySinh = x.Ngaysinh,
                    CMND = "*******",     
                    Mobile = x.Mobile, 
                    Email = x.Email    
                })
                .ToListAsync();
        }

        public async Task<List<NhanVienDTO>> GetPagedPrivateAsync(
            int page,
            int pageSize,
            string? sortColumn,
            string? sortDirection
        )
        {
            var allItems = await GetAllPrivateAsync();

            IEnumerable<NhanVienDTO> query = allItems
                .Where(x => x != null);

            query = ApplySortingPrivate(query, sortColumn, sortDirection);

            if (string.IsNullOrWhiteSpace(sortColumn))
            {
                query = query.OrderBy(x => x.Id_NV);
            }

            var totalItems = query.Count();
            var maxPage = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (maxPage <= 0)
            {
                page = 1;
            }
            else if (page > maxPage)
            {
                page = maxPage;
            }

            return query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public async Task<int> CountSearchPublicAsync(string keyword)
        {
            keyword = keyword.Trim().ToLower();

            return await _context.NhanViens.CountAsync(x =>
                (x.Disable == false || x.Disable == null) &&
                (
                    (x.MaNV ?? "").ToLower().Contains(keyword) ||
                    (x.Mobile ?? "").Contains(keyword) ||
                    (x.Email ?? "").ToLower().Contains(keyword)
                )
            );
        }

        public async Task<int> CountSearchPrivateAsync(string keyword)
        {
            keyword = keyword.Trim();

            var candidateIds = await SearchCandidateIdsBySecureIndexAsync(keyword);

            if (!string.IsNullOrWhiteSpace(keyword) && keyword.All(char.IsDigit))
            {
                var exactIds = await FindIdsByCMNDHashAsync(keyword);
                candidateIds = candidateIds
                    .Union(exactIds)
                    .Distinct()
                    .ToList();
            }

            var candidateItems = await GetPrivateByIdsAsync(candidateIds);
            var normalizedKeyword = keyword.ToLowerInvariant();

            return candidateItems.Count(x =>
                ((x.MaNV ?? "").ToLower().Contains(normalizedKeyword)) ||
                ((x.HoTen ?? "").ToLower().Contains(normalizedKeyword)) ||
                ((x.CMND ?? "").Contains(keyword)) ||
                ((x.Mobile ?? "").Contains(keyword)) ||
                ((x.Email ?? "").ToLower().Contains(normalizedKeyword))
            );
        }

        public async Task<List<NhanVienDTO>> SearchPublicAsync(
            string keyword,
            int page,
            int pageSize,
            string? sortColumn,
            string? sortDirection
        )
        {
            keyword = keyword.Trim().ToLower();

            var query = _context.NhanViens
                .AsNoTracking()
                .Where(x =>
                    (x.Disable == false || x.Disable == null) &&
                    (
                        (x.MaNV ?? "").ToLower().Contains(keyword) ||
                        (x.Mobile ?? "").Contains(keyword) ||
                        (x.Email ?? "").ToLower().Contains(keyword)
                    )
                );

            query = ApplySorting(query, sortColumn, sortDirection);

            if (string.IsNullOrWhiteSpace(sortColumn))
            {
                query = query.OrderBy(x => x.Id_NV);
            }

            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new NhanVienDTO
                {
                    Id_NV = x.Id_NV,
                    MaNV = x.MaNV,
                    HoTen = null,
                    NgaySinh = x.Ngaysinh,
                    CMND = "****",
                    Mobile = x.Mobile,
                    Email = x.Email
                })
                .ToListAsync();
        }

        public async Task<List<NhanVienDTO>> SearchPrivateAsync(
    string keyword,
    int page,
    int pageSize,
    string? sortColumn,
    string? sortDirection
)
        {
            keyword = keyword.Trim();

            // Phase 1: lọc ứng viên bằng SecureIndex
            var candidateIds = await SearchCandidateIdsBySecureIndexAsync(keyword);

            // Exact lookup bổ sung theo CMNDHash nếu keyword là số
            if (!string.IsNullOrWhiteSpace(keyword) && keyword.All(char.IsDigit))
            {
                var exactIds = await FindIdsByCMNDHashAsync(keyword);
                candidateIds = candidateIds
                    .Union(exactIds)
                    .Distinct()
                    .ToList();
            }

            // Phase 2: chỉ giải mã tập ứng viên
            var candidateItems = await GetPrivateByIdsAsync(candidateIds);

            // Verification: áp lại query gốc trên tập đã giải mã
            var normalizedKeyword = keyword.ToLowerInvariant();

            IEnumerable<NhanVienDTO> query = candidateItems.Where(x =>
                ((x.MaNV ?? "").ToLower().Contains(normalizedKeyword)) ||
                ((x.HoTen ?? "").ToLower().Contains(normalizedKeyword)) ||
                ((x.CMND ?? "").Contains(keyword)) ||
                ((x.Mobile ?? "").Contains(keyword)) ||
                ((x.Email ?? "").ToLower().Contains(normalizedKeyword))
            );

            query = ApplySortingPrivate(query, sortColumn, sortDirection);

            if (string.IsNullOrWhiteSpace(sortColumn))
            {
                query = query.OrderBy(x => x.Id_NV);
            }

            return query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public async Task<NhanVienDTO?> GetByIdPublicAsync(decimal id)
        {
            return await _context.NhanViens
                .AsNoTracking()
                .Where(x => x.Id_NV == id && (x.Disable == false || x.Disable == null))
                .Select(x => new NhanVienDTO
                {
                    Id_NV = x.Id_NV,
                    MaNV = x.MaNV,
                    HoTen = null,
                    NgaySinh = x.Ngaysinh,
                    CMND = "****",
                    Mobile = x.Mobile,
                    Email = x.Email
                })
                .FirstOrDefaultAsync();
        }

        public async Task<NhanVienDTO?> GetByIdPrivateAsync(decimal id)
        {
            var items = await GetPrivateByIdsAsync(new List<decimal> { id });
            return items.FirstOrDefault();
        }

        public async Task<decimal?> AddAsync(NhanVien entity)
        {
            var conn = _context.Database.GetDbConnection();

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            using var cmd = conn.CreateCommand();

            cmd.CommandText = "sp_Tbl_Nhanvien_Insert";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@MaNV", SqlDbType.NVarChar, 50)
            {
                Value = (object?)entity.MaNV ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@I_Holot", SqlDbType.VarBinary)
            {
                Value = (object?)EncryptionHelper.EncryptString(entity.Holot) ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@I_Ten", SqlDbType.VarBinary)
            {
                Value = (object?)EncryptionHelper.EncryptString(entity.Ten) ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@I_CMND", SqlDbType.VarBinary, 512)
            {
                Value = (object?)EncryptionHelper.EncryptString(entity.CMND) ?? DBNull.Value
            });

            var cmndHash = string.IsNullOrWhiteSpace(entity.CMND)
                ? (object)DBNull.Value
                : SecurityIndexHelper.ComputeHashWithSalt(entity.CMND);

            cmd.Parameters.Add(new SqlParameter("@CMNDHash", SqlDbType.VarBinary, 32)
            {
                Value = cmndHash
            });

            cmd.Parameters.Add(new SqlParameter("@Ngaysinh", SqlDbType.DateTime)
            {
                Value = entity.Ngaysinh ?? (object)DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@Mobile", SqlDbType.VarChar, 20)
            {
                Value = (object?)entity.Mobile ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 255)
            {
                Value = (object?)entity.Email ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@I_Sotaikhoan", SqlDbType.VarBinary, 512)
            {
                Value = (object?)EncryptionHelper.EncryptString(entity.Sotaikhoan) ?? DBNull.Value
            });

            var sotaikhoanHash = string.IsNullOrWhiteSpace(entity.Sotaikhoan)
                ? (object)DBNull.Value
                : SecurityIndexHelper.ComputeHashWithSalt(entity.Sotaikhoan);

            cmd.Parameters.Add(new SqlParameter("@SotaikhoanHash", SqlDbType.VarBinary, 32)
            {
                Value = sotaikhoanHash
            });

            cmd.Parameters.Add(new SqlParameter("@CreatedUser", SqlDbType.Decimal)
            {
                Value = 1,
                Precision = 18,
                Scale = 0
            });

            var result = await cmd.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
                return null;

            var newId = Convert.ToDecimal(result);

            await RebuildSecureIndexForNhanVienAsync(
                (int)newId,
                entity.Holot,
                entity.Ten
            );

            return newId;
        }

        public async Task UpdateAsync(NhanVien entity)
        {
            var conn = _context.Database.GetDbConnection();

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            using var cmd = conn.CreateCommand();

            cmd.CommandText = "sp_Tbl_Nhanvien_Update";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@Id_NV", SqlDbType.Decimal)
            {
                Value = entity.Id_NV,
                Precision = 18,
                Scale = 0
            });

            cmd.Parameters.Add(new SqlParameter("@MaNV", SqlDbType.NVarChar, 50)
            {
                Value = (object?)entity.MaNV ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@I_Holot", SqlDbType.VarBinary)
            {
                Value = (object?)EncryptionHelper.EncryptString(entity.Holot) ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@I_Ten", SqlDbType.VarBinary)
            {
                Value = (object?)EncryptionHelper.EncryptString(entity.Ten) ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@I_CMND", SqlDbType.VarBinary, 512)
            {
                Value = (object?)EncryptionHelper.EncryptString(entity.CMND) ?? DBNull.Value
            });

            var cmndHash = string.IsNullOrWhiteSpace(entity.CMND)
                ? (object)DBNull.Value
                : SecurityIndexHelper.ComputeHashWithSalt(entity.CMND);

            cmd.Parameters.Add(new SqlParameter("@CMNDHash", SqlDbType.VarBinary, 32)
            {
                Value = cmndHash
            });

            cmd.Parameters.Add(new SqlParameter("@Ngaysinh", SqlDbType.DateTime)
            {
                Value = entity.Ngaysinh ?? (object)DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@Mobile", SqlDbType.VarChar, 20)
            {
                Value = (object?)entity.Mobile ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 255)
            {
                Value = (object?)entity.Email ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@I_Sotaikhoan", SqlDbType.VarBinary, 512)
            {
                Value = (object?)EncryptionHelper.EncryptString(entity.Sotaikhoan) ?? DBNull.Value
            });

            var sotaikhoanHash = string.IsNullOrWhiteSpace(entity.Sotaikhoan)
                ? (object)DBNull.Value
                : SecurityIndexHelper.ComputeHashWithSalt(entity.Sotaikhoan);

            cmd.Parameters.Add(new SqlParameter("@SotaikhoanHash", SqlDbType.VarBinary, 32)
            {
                Value = sotaikhoanHash
            });

            cmd.Parameters.Add(new SqlParameter("@LastModifiedUser", SqlDbType.Decimal)
            {
                Value = 1,
                Precision = 18,
                Scale = 0
            });

            await cmd.ExecuteNonQueryAsync();

            await RebuildSecureIndexForNhanVienAsync(
                (int)entity.Id_NV,
                entity.Holot,
                entity.Ten
            );
        }

        public async Task SoftDeleteAsync(decimal id)
        {
            var nv = await _context.NhanViens.FindAsync(id);
            if (nv == null) return;

            nv.Disable = true;
            await _context.SaveChangesAsync();
        }

        private IEnumerable<NhanVienDTO> ApplySortingPrivate(
            IEnumerable<NhanVienDTO> query,
            string? sortColumn,
            string? sortDirection
        )
        {
            if (string.IsNullOrWhiteSpace(sortColumn))
            {
                return query;
            }

            var isDesc = sortDirection?.ToLower() == "desc";

            return sortColumn switch
            {
                "ngaySinh" => isDesc
                    ? query.OrderByDescending(x => x.NgaySinh)
                    : query.OrderBy(x => x.NgaySinh),

                _ => query
            };
        }

        private async Task<List<NhanVienDTO>> GetAllPrivateAsync()
        {
            var result = new List<NhanVienDTO>();

            var conn = _context.Database.GetDbConnection();

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            using var cmd = conn.CreateCommand();

            cmd.CommandText = "sp_Tbl_Nhanvien_GetAllEncrypted";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 0;

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var id = reader.GetDecimal(reader.GetOrdinal("Id_NV"));

                try
                {
                    var holot = EncryptionHelper.DecryptString(reader["I_Holot"]);
                    var ten = EncryptionHelper.DecryptString(reader["I_Ten"]);
                    var cmnd = EncryptionHelper.DecryptString(reader["I_CMND"]);

                    result.Add(new NhanVienDTO
                    {
                        Id_NV = id,
                        MaNV = reader["MaNV"] as string,
                        HoTen = $"{holot} {ten}".Trim(),
                        NgaySinh = reader["Ngaysinh"] == DBNull.Value
                            ? null
                            : (DateTime?)reader["Ngaysinh"],
                        CMND = cmnd,
                        Mobile = reader["Mobile"] as string,
                        Email = reader["Email"] as string
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Decrypt lỗi tại Id_NV = {id}. Lỗi: {ex.Message}");
                    continue;
                }
            }

            return result;
        }
        private async Task<List<NhanVienDTO>> GetPrivateByIdsAsync(IEnumerable<decimal> ids)
        {
            var idList = ids?.Distinct().ToList() ?? new List<decimal>();

            if (!idList.Any())
                return new List<NhanVienDTO>();

            var conn = _context.Database.GetDbConnection();

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            using var cmd = conn.CreateCommand();

            cmd.CommandText = "sp_Tbl_Nhanvien_GetByIds";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 0;

            cmd.Parameters.Add(new SqlParameter("@Ids", string.Join(",", idList)));

            var result = new List<NhanVienDTO>();

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var id = reader.GetDecimal(reader.GetOrdinal("Id_NV"));

                try
                {
                    var holot = EncryptionHelper.DecryptString(reader["I_Holot"]);
                    var ten = EncryptionHelper.DecryptString(reader["I_Ten"]);
                    var cmnd = EncryptionHelper.DecryptString(reader["I_CMND"]);

                    result.Add(new NhanVienDTO
                    {
                        Id_NV = id,
                        MaNV = reader["MaNV"] as string,
                        HoTen = $"{holot} {ten}".Trim(),
                        NgaySinh = reader["Ngaysinh"] == DBNull.Value
                            ? null
                            : (DateTime?)reader["Ngaysinh"],
                        CMND = cmnd,
                        Mobile = reader["Mobile"] as string,
                        Email = reader["Email"] as string
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Decrypt lỗi tại Id_NV = {id}. Lỗi: {ex.Message}");

                    // Bỏ qua record lỗi để search không chết toàn bộ.
                    // Sau đó dùng Id này để migrate/fix lại dữ liệu.
                    continue;
                }
            }

            return result;
        }
        private async Task<List<decimal>> FindIdsByCMNDHashAsync(string cmnd)
        {
            var result = new List<decimal>();
            var hash = SecurityIndexHelper.ComputeHashWithSalt(cmnd);

            var conn = _context.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "sp_Tbl_Nhanvien_FindIdsByCMNDHash";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@CMNDHash", SqlDbType.VarBinary, 32)
            {
                Value = hash
            });

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(Convert.ToDecimal(reader["Id_NV"]));
            }

            return result;
        }


        private async Task<List<decimal>> SearchCandidateIdsBySecureIndexAsync(string keyword)
        {
            var hashes = SecurityIndexHelper.BuildNgramHashes(keyword, 3);

            if (hashes.Count == 0)
                return new List<decimal>();

            var table = new DataTable();
            table.Columns.Add("HashValue", typeof(byte[]));

            foreach (var hash in hashes)
            {
                table.Rows.Add(hash);
            }

            var result = new List<decimal>();

            var conn = _context.Database.GetDbConnection();

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            using var cmd = conn.CreateCommand();

            cmd.CommandText = "sp_SecureIndex_SearchCandidates";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@TableName", SqlDbType.NVarChar, 100)
            {
                Value = "Tbl_Nhanvien"
            });

            cmd.Parameters.Add(new SqlParameter("@ColumnName", SqlDbType.NVarChar, 100)
            {
                Value = "HoTen"
            });

            var tvpParam = new SqlParameter("@GramHashes", SqlDbType.Structured)
            {
                TypeName = "dbo.VarbinaryHashList",
                Value = table
            };

            cmd.Parameters.Add(tvpParam);

            var minMatch = Math.Max(1, (int)Math.Ceiling(hashes.Count * 0.7));

            cmd.Parameters.Add(new SqlParameter("@MinMatch", SqlDbType.Int)
            {
                Value = minMatch
            });

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(Convert.ToDecimal(reader["RecordId"]));
            }

            return result.Distinct().ToList();
        }

        private async Task RebuildSecureIndexForNhanVienAsync(int recordId, string? holot, string? ten)
        {
            var fullName = $"{holot} {ten}".Trim();

            var conn = _context.Database.GetDbConnection();

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            using (var deleteCmd = conn.CreateCommand())
            {
                deleteCmd.CommandText = "sp_SecureIndex_DeleteByRecord";
                deleteCmd.CommandType = CommandType.StoredProcedure;

                deleteCmd.Parameters.Add(new SqlParameter("@RecordId", recordId));
                deleteCmd.Parameters.Add(new SqlParameter("@TableName", "Tbl_Nhanvien"));
                deleteCmd.Parameters.Add(new SqlParameter("@ColumnName", "HoTen"));

                await deleteCmd.ExecuteNonQueryAsync();
            }

            var grams = SecurityIndexHelper.BuildNgrams(fullName, 3);

            for (int i = 0; i < grams.Count; i++)
            {
                using var insertCmd = conn.CreateCommand();

                insertCmd.CommandText = "sp_SecureIndex_Insert";
                insertCmd.CommandType = CommandType.StoredProcedure;

                insertCmd.Parameters.Add(new SqlParameter("@RecordId", recordId));
                insertCmd.Parameters.Add(new SqlParameter("@TableName", "Tbl_Nhanvien"));
                insertCmd.Parameters.Add(new SqlParameter("@ColumnName", "HoTen"));

                insertCmd.Parameters.Add(new SqlParameter("@GramHash", SqlDbType.VarBinary, 32)
                {
                    Value = SecurityIndexHelper.ComputeHashForGram(grams[i])
                });

                insertCmd.Parameters.Add(new SqlParameter("@Position", SqlDbType.Int)
                {
                    Value = i
                });

                await insertCmd.ExecuteNonQueryAsync();
            }
        }

        private async Task<List<NhanVienDTO>> GetAllForRebuildIndexAsync()
        {
            return await _context.NhanViens
                .AsNoTracking()
                .Where(x => x.Disable == false || x.Disable == null)
                .Select(x => new NhanVienDTO
                {
                    Id_NV = x.Id_NV,
                    MaNV = x.MaNV,
                    HoTen = ((x.Holot ?? "") + " " + (x.Ten ?? "")).Trim(),
                    NgaySinh = x.Ngaysinh,
                    CMND = x.CMND,
                    Mobile = x.Mobile,
                    Email = x.Email
                })
                .ToListAsync();
        }

        public async Task RebuildAllSecureIndexBulkAsync(int batchSize = 1000)
        {
            var allItems = await GetAllForRebuildIndexAsync();

            var batches = allItems
                .Where(x => x != null)
                .OrderBy(x => x.Id_NV)
                .Select((item, index) => new { item, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.item).ToList())
                .ToList();

            var conn = (SqlConnection)_context.Database.GetDbConnection();

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            int batchNo = 0;

            foreach (var batch in batches)
            {
                batchNo++;

                using var transaction = conn.BeginTransaction();

                try
                {
                    var idList = batch.Select(x => Convert.ToInt32(x.Id_NV)).ToList();

                    // 1) Xóa SecureIndex cũ của batch hiện tại
                    using (var deleteCmd = conn.CreateCommand())
                    {
                        deleteCmd.Transaction = transaction;
                        deleteCmd.CommandText = "dbo.sp_SecureIndex_DeleteByRecords";
                        deleteCmd.CommandType = CommandType.StoredProcedure;
                        deleteCmd.CommandTimeout = 0;

                        deleteCmd.Parameters.Add(new SqlParameter("@TableName", SqlDbType.NVarChar, 100)
                        {
                            Value = "Tbl_Nhanvien"
                        });

                        deleteCmd.Parameters.Add(new SqlParameter("@ColumnName", SqlDbType.NVarChar, 100)
                        {
                            Value = "HoTen"
                        });

                        deleteCmd.Parameters.Add(new SqlParameter("@Ids", SqlDbType.NVarChar)
                        {
                            Value = string.Join(",", idList)
                        });

                        await deleteCmd.ExecuteNonQueryAsync();
                    }

                    // 2) Tạo DataTable chứa n-gram hash để bulk insert
                    var table = new DataTable();
                    table.Columns.Add("RecordId", typeof(int));
                    table.Columns.Add("TableName", typeof(string));
                    table.Columns.Add("ColumnName", typeof(string));
                    table.Columns.Add("GramHash", typeof(byte[]));
                    table.Columns.Add("Position", typeof(int));

                    foreach (var item in batch)
                    {
                        var fullName = (item.HoTen ?? string.Empty).Trim();

                        if (string.IsNullOrWhiteSpace(fullName))
                            continue;

                        var grams = SecurityIndexHelper.BuildNgrams(fullName, 3);

                        for (int i = 0; i < grams.Count; i++)
                        {
                            table.Rows.Add(
                                Convert.ToInt32(item.Id_NV),
                                "Tbl_Nhanvien",
                                "HoTen",
                                SecurityIndexHelper.ComputeHashForGram(grams[i]),
                                i
                            );
                        }
                    }

                    // 3) Bulk insert SecureIndex mới của batch hiện tại
                    if (table.Rows.Count > 0)
                    {
                        using var bulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, transaction);
                        bulk.DestinationTableName = "dbo.SecureIndex";
                        bulk.BatchSize = 5000;
                        bulk.BulkCopyTimeout = 0;

                        bulk.ColumnMappings.Add("RecordId", "RecordId");
                        bulk.ColumnMappings.Add("TableName", "TableName");
                        bulk.ColumnMappings.Add("ColumnName", "ColumnName");
                        bulk.ColumnMappings.Add("GramHash", "GramHash");
                        bulk.ColumnMappings.Add("Position", "Position");

                        await bulk.WriteToServerAsync(table);
                    }

                    transaction.Commit();

                    Console.WriteLine(
                        $"Batch {batchNo}/{batches.Count} done. Records: {batch.Count}, SecureIndex rows: {table.Rows.Count}"
                    );
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }


        public async Task MigrateOldPlaintextDataAsync()
        {
            var rows = await _context.NhanViens
                .AsNoTracking()
                .Where(x =>
                    (x.Disable == false || x.Disable == null) &&
                    (
                        x.Holot != null ||
                        x.Ten != null ||
                        x.CMND != null
                    )
                )
                .Select(x => new
                {
                    x.Id_NV,
                    x.Holot,
                    x.Ten,
                    x.CMND,
                    x.Sotaikhoan
                })
                .ToListAsync();

            var conn = _context.Database.GetDbConnection();

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            foreach (var row in rows)
            {
                using var cmd = conn.CreateCommand();

                cmd.CommandText = "sp_Tbl_Nhanvien_MigrateEncrypted";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 0;

                cmd.Parameters.Add(new SqlParameter("@Id_NV", SqlDbType.Decimal)
                {
                    Value = row.Id_NV,
                    Precision = 18,
                    Scale = 0
                });

                cmd.Parameters.Add(new SqlParameter("@I_Holot", SqlDbType.VarBinary)
                {
                    Value = (object?)EncryptionHelper.EncryptString(row.Holot) ?? DBNull.Value
                });

                cmd.Parameters.Add(new SqlParameter("@I_Ten", SqlDbType.VarBinary)
                {
                    Value = (object?)EncryptionHelper.EncryptString(row.Ten) ?? DBNull.Value
                });

                cmd.Parameters.Add(new SqlParameter("@I_CMND", SqlDbType.VarBinary, 512)
                {
                    Value = (object?)EncryptionHelper.EncryptString(row.CMND) ?? DBNull.Value
                });

                var cmndHash = string.IsNullOrWhiteSpace(row.CMND)
                    ? (object)DBNull.Value
                    : SecurityIndexHelper.ComputeHashWithSalt(row.CMND);

                cmd.Parameters.Add(new SqlParameter("@CMNDHash", SqlDbType.VarBinary, 32)
                {
                    Value = cmndHash
                });

                cmd.Parameters.Add(new SqlParameter("@I_Sotaikhoan", SqlDbType.VarBinary, 512)
                {
                    Value = (object?)EncryptionHelper.EncryptString(row.Sotaikhoan) ?? DBNull.Value
                });

                var sotaikhoanHash = string.IsNullOrWhiteSpace(row.Sotaikhoan)
                    ? (object)DBNull.Value
                    : SecurityIndexHelper.ComputeHashWithSalt(row.Sotaikhoan);

                cmd.Parameters.Add(new SqlParameter("@SotaikhoanHash", SqlDbType.VarBinary, 32)
                {
                    Value = sotaikhoanHash
                });

                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}