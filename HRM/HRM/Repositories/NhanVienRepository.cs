using HRM.Data;
using HRM.DTOs;
using HRM.Entities;
using HRM.Helpers.Security;
using HRM.Model;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HRM.Common;
using ISecurityService = HRM.Services.ISecurityService;

namespace HRM.Repositories
{
    public class NhanVienRepository : INhanVienRepository
    {
        private readonly HrmDbContext _context;
        private readonly ISecurityService _securityService;

        public SearchDebugInfo? LastSearchDebug { get; private set; }
        public int LastSearchTotal { get; private set; }

        public NhanVienRepository(HrmDbContext context, ISecurityService securityService)
        {
            _context = context;
            _securityService = securityService;
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
                return query;

            var isDesc = sortDirection?.ToLower() == SortDirectionConst.Desc;

            return sortColumn switch
            {
                "ngaySinh" => isDesc ? query.OrderByDescending(x => x.Ngaysinh) : query.OrderBy(x => x.Ngaysinh),
                _ => query
            };
        }

        private IEnumerable<NhanVienDTO> ApplySortingPrivate(
            IEnumerable<NhanVienDTO> query,
            string? sortColumn,
            string? sortDirection
        )
        {
            if (string.IsNullOrWhiteSpace(sortColumn))
                return query;

            var isDesc = sortDirection?.ToLower() == SortDirectionConst.Desc;

            return sortColumn switch
            {
                "ngaySinh" => isDesc ? query.OrderByDescending(x => x.NgaySinh) : query.OrderBy(x => x.NgaySinh),
                _ => query
            };
        }

        public async Task<int> CountPublicAsync()
        {
            return await _context.NhanViens.CountAsync(x => x.Disable == false || x.Disable == null);
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
            LastSearchDebug = null;
            LastSearchTotal = 0;

            var query = _context.NhanViens
                .AsNoTracking()
                .Where(x => x.Disable == false || x.Disable == null);

            query = ApplySorting(query, sortColumn, sortDirection);

            if (string.IsNullOrWhiteSpace(sortColumn))
                query = query.OrderBy(x => x.Id_NV);

            var rows = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return rows.Select(ToPublicDto).ToList();
        }

        public async Task<List<NhanVienDTO>> GetPagedPrivateAsync(
            int page,
            int pageSize,
            string? sortColumn,
            string? sortDirection
        )
        {
            LastSearchDebug = null;
            LastSearchTotal = 0;

            var allItems = await GetAllPrivateAsync();

            IEnumerable<NhanVienDTO> query = allItems
                .Where(x => x != null);

            query = ApplySortingPrivate(query, sortColumn, sortDirection);

            if (string.IsNullOrWhiteSpace(sortColumn))
                query = query.OrderBy(x => x.Id_NV);

            return query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public Task<int> CountSearchPublicAsync(string keyword)
        {
            return Task.FromResult(LastSearchTotal);
        }

        public Task<int> CountSearchPrivateAsync(string keyword)
        {
            return Task.FromResult(LastSearchTotal);
        }

        /// <summary>
        /// Search ở màn hình Public/Admin vẫn trả kết quả đã giải mã theo yêu cầu:
        /// không nhập keyword thì xem dữ liệu mã hóa; nhập keyword thì search và hiện thông tin đầy đủ.
        /// </summary>
        public Task<List<NhanVienDTO>> SearchPublicAsync(
            string keyword,
            int page,
            int pageSize,
            string? sortColumn,
            string? sortDirection
        )
        {
            return SearchPrivateAsync(keyword, page, pageSize, sortColumn, sortDirection);
        }

        public async Task<List<NhanVienDTO>> SearchPrivateAsync(
            string keyword,
            int page,
            int pageSize,
            string? sortColumn,
            string? sortDirection
        )
        {
            var totalSw = Stopwatch.StartNew();
            keyword = (keyword ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(keyword))
            {
                LastSearchDebug = null;
                LastSearchTotal = 0;
                return await GetPagedPrivateAsync(page, pageSize, sortColumn, sortDirection);
            }

            var isCmndSearch = IsCmndKeyword(keyword);
            var searchType = isCmndSearch ? "CMND/CCCD" : "Họ tên";

           
            var step1Sw = Stopwatch.StartNew();
            var candidateIds = isCmndSearch
                ? await FindIdsByCMNDHashAsync(keyword)
                : await SearchCandidateIdsBySecureIndexAsync(keyword);
            step1Sw.Stop();

            var candidateCount = candidateIds.Distinct().Count();

           
            var step2Sw = Stopwatch.StartNew();

            var candidateItems = candidateIds.Any()
                ? await GetPrivateByIdsAsync(candidateIds)
                : new List<NhanVienDTO>();

            var matchedItems = candidateItems
                .Where(x => MatchesSearchKeyword(x, keyword))
                .ToList();

           
            if (!matchedItems.Any())
            {
                var fallbackItems = await GetAllPrivateAsync();
                candidateItems = fallbackItems;
                matchedItems = fallbackItems
                    .Where(x => MatchesSearchKeyword(x, keyword))
                    .ToList();
            }

            step2Sw.Stop();
            totalSw.Stop();

            LastSearchTotal = matchedItems.Count;

            var firstMatched = matchedItems.FirstOrDefault();
            LastSearchDebug = new SearchDebugInfo
            {
                SearchKeyword = keyword,
                SearchType = searchType,
                DecryptedValue = firstMatched == null
                    ? null
                    : isCmndSearch ? firstMatched.CMND : firstMatched.HoTen,
                CompareResult = firstMatched == null ? "Không khớp" : "Khớp",
                TotalMs = totalSw.ElapsedMilliseconds,
                Step1Ms = step1Sw.ElapsedMilliseconds,
                Step2Ms = step2Sw.ElapsedMilliseconds,
                CandidateCount = candidateCount,
                ScannedRecordCount = candidateItems.Count,
                CollisionCount = Math.Max(0, candidateItems.Count - matchedItems.Count),
                ResultCount = matchedItems.Count
            };

            IEnumerable<NhanVienDTO> query = matchedItems;
            query = ApplySortingPrivate(query, sortColumn, sortDirection);

            if (string.IsNullOrWhiteSpace(sortColumn))
                query = query.OrderBy(x => x.Id_NV);

            return query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        }

        public async Task<NhanVienDTO?> GetByIdPublicAsync(decimal id)
        {
            var row = await _context.NhanViens
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id_NV == id && (x.Disable == false || x.Disable == null));

            return row == null ? null : ToPublicDto(row);
        }

        public async Task<NhanVienDTO?> GetByIdPrivateAsync(decimal id)
        {
            var items = await GetPrivateByIdsAsync(new List<decimal> { id });
            return items.FirstOrDefault();
        }

        private NhanVienDTO? ReadPrivateDto(IDataRecord reader)
        {
            var id = Convert.ToDecimal(reader["Id_NV"]);

            try
            {
                var holot = DecryptDbValue(reader["I_Holot"]);
                var ten = DecryptDbValue(reader["I_Ten"]);
                var cmnd = DecryptDbValue(reader["I_CMND"]);

                return new NhanVienDTO
                {
                    Id_NV = id,
                    MaNV = ReadString(reader, "MaNV"),
                    HoTen = $"{holot} {ten}".Trim(),
                    NgaySinh = ReadDateTime(reader, "Ngaysinh"),
                    CMND = cmnd,
                    Mobile = ReadString(reader, "Mobile"),
                    Email = ReadString(reader, "Email")
                };
            }
            catch (Exception ex)
            {
               
                return null;
            }
        }

        public async Task<List<decimal>> FindIdsByCMNDHashAsync(string cmnd)
        {
            var result = new List<decimal>();
            var normalizedCmnd = NormalizeDigits(cmnd);
            var hash = SearchIndexToDbBytes(normalizedCmnd, "CMND");

            if (hash == null)
                return result;

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

            return result.Distinct().ToList();
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
                Value = (object?)EncryptToDbBytes(entity.Holot) ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@I_Ten", SqlDbType.VarBinary)
            {
                Value = (object?)EncryptToDbBytes(entity.Ten) ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@I_CMND", SqlDbType.VarBinary, 512)
            {
                Value = (object?)EncryptToDbBytes(NormalizeDigits(entity.CMND)) ?? DBNull.Value
            });

            var cmndHash = string.IsNullOrWhiteSpace(entity.CMND)
                ? (object)DBNull.Value
                : Convert.FromBase64String(_securityService.GenerateSearchIndex(entity.CMND, "CMND"));

            cmd.Parameters.Add(new SqlParameter("@CMNDHash", SqlDbType.VarBinary, 32)
            {
                Value = (object?)SearchIndexToDbBytes(NormalizeDigits(entity.CMND), "CMND") ?? DBNull.Value
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
                Value = (object?)EncryptToDbBytes(entity.Sotaikhoan) ?? DBNull.Value
            });

            var sotaikhoanHash = string.IsNullOrWhiteSpace(entity.Sotaikhoan)
                ? (object)DBNull.Value
                : Convert.FromBase64String(_securityService.GenerateSearchIndex(entity.Sotaikhoan, "Sotaikhoan"));

            cmd.Parameters.Add(new SqlParameter("@SotaikhoanHash", SqlDbType.VarBinary, 32)
            {
                Value = (object?)SearchIndexToDbBytes(entity.Sotaikhoan, "Sotaikhoan") ?? DBNull.Value
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

            await RebuildSecureIndexForNhanVienAsync((int)newId, entity.Holot, entity.Ten);

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
                Value = (object?)EncryptToDbBytes(entity.Holot) ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@I_Ten", SqlDbType.VarBinary)
            {
                Value = (object?)EncryptToDbBytes(entity.Ten) ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@I_CMND", SqlDbType.VarBinary, 512)
            {
                Value = (object?)EncryptToDbBytes(NormalizeDigits(entity.CMND)) ?? DBNull.Value
            });

            var cmndHash = string.IsNullOrWhiteSpace(entity.CMND)
                ? (object)DBNull.Value
                : Convert.FromBase64String(_securityService.GenerateSearchIndex(entity.CMND, "CMND"));

            cmd.Parameters.Add(new SqlParameter("@CMNDHash", SqlDbType.VarBinary, 32)
            {
                Value = (object?)SearchIndexToDbBytes(NormalizeDigits(entity.CMND), "CMND") ?? DBNull.Value
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
                Value = (object?)EncryptToDbBytes(entity.Sotaikhoan) ?? DBNull.Value
            });

            var sotaikhoanHash = string.IsNullOrWhiteSpace(entity.Sotaikhoan)
                ? (object)DBNull.Value
                : Convert.FromBase64String(_securityService.GenerateSearchIndex(entity.Sotaikhoan, "Sotaikhoan"));

            cmd.Parameters.Add(new SqlParameter("@SotaikhoanHash", SqlDbType.VarBinary, 32)
            {
                Value = (object?)SearchIndexToDbBytes(entity.Sotaikhoan, "Sotaikhoan") ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@LastModifiedUser", SqlDbType.Decimal)
            {
                Value = 1,
                Precision = 18,
                Scale = 0
            });

            await cmd.ExecuteNonQueryAsync();

            await RebuildSecureIndexForNhanVienAsync((int)entity.Id_NV, entity.Holot, entity.Ten);
        }

        public async Task SoftDeleteAsync(decimal id)
        {
            var nv = await _context.NhanViens.FindAsync(id);
            if (nv == null) return;

            nv.Disable = true;
            await _context.SaveChangesAsync();
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
                var dto = ReadPrivateDto(reader);
                if (dto != null)
                    result.Add(dto);
            }

            return result;
        }

        public async Task<List<NhanVienDTO>> GetPrivateByIdsAsync(IEnumerable<decimal> ids)
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
                var dto = ReadPrivateDto(reader);
                if (dto != null)
                    result.Add(dto);
            }

            return result;
        }

        public async Task<List<decimal>> SearchCandidateIdsBySecureIndexAsync(string keyword)
        {
            var allIds = new List<decimal>();
            var terms = BuildNameSearchTerms(keyword);

            foreach (var term in terms)
            {
                var ids = await SearchCandidateIdsByNameTermAsync(term);
                allIds.AddRange(ids);
            }

            return allIds.Distinct().ToList();
        }

        private async Task<List<decimal>> SearchCandidateIdsByNameTermAsync(string term)
        {
            var grams = BuildNgrams(term, 3);

            if (grams.Count == 0)
                return new List<decimal>();

            var table = new DataTable();
            table.Columns.Add("HashValue", typeof(byte[]));

            foreach (var gram in grams)
            {
                var hash = SearchIndexToDbBytes(gram, "HoTenGram");
                if (hash != null)
                    table.Rows.Add(hash);
            }

            if (table.Rows.Count == 0)
                return new List<decimal>();

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

            cmd.Parameters.Add(new SqlParameter("@GramHashes", SqlDbType.Structured)
            {
                TypeName = "dbo.VarbinaryHashList",
                Value = table
            });

            var minMatch = Math.Max(1, (int)Math.Ceiling(grams.Count * 0.75));
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

            var grams = BuildNgrams(fullName, 3);

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
                    Value = SearchIndexToDbBytes(grams[i], "HoTenGram") ?? Array.Empty<byte>()
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
            // Rebuild index phải dựa trên dữ liệu đã giải mã, không dùng Holot/Ten plaintext cũ.
            return await GetAllPrivateAsync();
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

                        var grams = BuildNgrams(fullName, 3);

                        for (int i = 0; i < grams.Count; i++)
                        {
                            var hash = SearchIndexToDbBytes(grams[i], "HoTenGram");
                            if (hash == null)
                                continue;

                            table.Rows.Add(
                                Convert.ToInt32(item.Id_NV),
                                "Tbl_Nhanvien",
                                "HoTen",
                                hash,
                                i
                            );
                        }
                    }

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
                    (x.Holot != null || x.Ten != null || x.CMND != null)
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
                    Value = (object?)EncryptToDbBytes(row.Holot) ?? DBNull.Value
                });

                cmd.Parameters.Add(new SqlParameter("@I_Ten", SqlDbType.VarBinary)
                {
                    Value = (object?)EncryptToDbBytes(row.Ten) ?? DBNull.Value
                });

                cmd.Parameters.Add(new SqlParameter("@I_CMND", SqlDbType.VarBinary, 512)
                {
                    Value = (object?)EncryptToDbBytes(NormalizeDigits(row.CMND)) ?? DBNull.Value
                });

                var cmndHash = string.IsNullOrWhiteSpace(row.CMND)
                    ? (object)DBNull.Value
                    : Convert.FromBase64String(_securityService.GenerateSearchIndex(row.CMND, "CMND"));

                cmd.Parameters.Add(new SqlParameter("@CMNDHash", SqlDbType.VarBinary, 32)
                {
                    Value = (object?)SearchIndexToDbBytes(NormalizeDigits(row.CMND), "CMND") ?? DBNull.Value
                });

                cmd.Parameters.Add(new SqlParameter("@I_Sotaikhoan", SqlDbType.VarBinary, 512)
                {
                    Value = (object?)EncryptToDbBytes(row.Sotaikhoan) ?? DBNull.Value
                });

                var sotaikhoanHash = string.IsNullOrWhiteSpace(row.Sotaikhoan)
                    ? (object)DBNull.Value
                    : Convert.FromBase64String(_securityService.GenerateSearchIndex(row.Sotaikhoan, "Sotaikhoan"));

                cmd.Parameters.Add(new SqlParameter("@SotaikhoanHash", SqlDbType.VarBinary, 32)
                {
                    Value = (object?)SearchIndexToDbBytes(row.Sotaikhoan, "Sotaikhoan") ?? DBNull.Value
                });

                await cmd.ExecuteNonQueryAsync();
            }
        }

        private NhanVienDTO ToPublicDto(NhanVien x)
        {
            return new NhanVienDTO
            {
                Id_NV = x.Id_NV,
                MaNV = x.MaNV,
                HoTen = $"{ToEncryptedDisplay(x.I_Holot)} {ToEncryptedDisplay(x.I_Ten)}".Trim(),
                NgaySinh = x.Ngaysinh,
                CMND = ToEncryptedDisplay(x.I_CMND),
                Mobile = x.Mobile,
                Email = x.Email
            };
        }

        private static string? ReadString(IDataRecord reader, string columnName)
        {
            var value = reader[columnName];
            return value == DBNull.Value ? null : value?.ToString();
        }

        private static DateTime? ReadDateTime(IDataRecord reader, string columnName)
        {
            var value = reader[columnName];
            return value == DBNull.Value ? null : Convert.ToDateTime(value);
        }

        private string? DecryptDbValue(object? dbValue)
        {
            if (dbValue == null || dbValue == DBNull.Value)
                return null;

            // DB hiện tại là varbinary. Nếu sau này DB đổi sang nvarchar thì vẫn hỗ trợ string.
            if (dbValue is byte[] bytes)
            {
                if (bytes.Length == 0)
                    return null;

                return _securityService.DecryptData(Convert.ToBase64String(bytes));
            }

            return _securityService.DecryptData(dbValue.ToString() ?? string.Empty);
        }

        private byte[]? EncryptToDbBytes(string? rawData)
        {
            if (string.IsNullOrWhiteSpace(rawData))
                return null;

            var encryptedText = _securityService.EncryptData(rawData);
            return TextToBytes(encryptedText);
        }

        private byte[]? SearchIndexToDbBytes(string? rawData, string columnProfile)
        {
            if (string.IsNullOrWhiteSpace(rawData))
                return null;

            var indexText = _securityService.GenerateSearchIndex(rawData, columnProfile);
            return TextToBytes(indexText);
        }

        private static byte[]? TextToBytes(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            try
            {
                return Convert.FromBase64String(text);
            }
            catch
            {
                return Encoding.UTF8.GetBytes(text);
            }
        }

        private static string ToEncryptedDisplay(byte[]? bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return "****";

            var base64 = Convert.ToBase64String(bytes);
            return base64.Length <= 24 ? base64 : base64.Substring(0, 24) + "...";
        }

        private static bool MatchesSearchKeyword(NhanVienDTO item, string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return true;

            if (IsCmndKeyword(keyword))
                return SameDigits(item.CMND, keyword);

            return ContainsName(item.HoTen, keyword);
        }

        private static bool IsCmndKeyword(string keyword)
        {
            var digits = NormalizeDigits(keyword);

            return digits.Length >= 9 &&
                   keyword.All(c => char.IsDigit(c) || char.IsWhiteSpace(c) || c == '-' || c == '.');
        }

        private static string NormalizeDigits(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            return Regex.Replace(input, @"\D", string.Empty);
        }

        private static bool SameDigits(string? source, string keyword)
        {
            var sourceDigits = NormalizeDigits(source);
            var keywordDigits = NormalizeDigits(keyword);

            return !string.IsNullOrWhiteSpace(keywordDigits) && sourceDigits == keywordDigits;
        }

        private static bool ContainsName(string? source, string keyword)
        {
            var sourceNorm = NormalizeVietnameseText(source);
            var keywordNorm = NormalizeVietnameseText(keyword);

            if (string.IsNullOrWhiteSpace(sourceNorm) || string.IsNullOrWhiteSpace(keywordNorm))
                return false;

            return sourceNorm.Contains(keywordNorm);
        }

        private static string NormalizeVietnameseText(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var text = Regex.Replace(input.Trim().ToLowerInvariant(), @"\s+", " ");
            text = text.Normalize(NormalizationForm.FormD);

            var sb = new StringBuilder();
            foreach (var c in text)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            text = sb.ToString().Normalize(NormalizationForm.FormC);
            text = text.Replace('đ', 'd');
            text = Regex.Replace(text, @"\s+", " ");

            return text.Trim();
        }

        private static string NormalizeForIndex(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            return Regex.Replace(input.Trim().ToLowerInvariant(), @"\s+", " ").Trim();
        }

        private static List<string> BuildNameSearchTerms(string keyword)
        {
            var normalized = NormalizeForIndex(keyword);
            var terms = new List<string>();

            if (string.IsNullOrWhiteSpace(normalized))
                return terms;

            
            terms.Add(normalized);

            var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 1)
            {
                var lastName = words[^1];
                if (lastName.Length >= 2)
                {
                    terms.Add(lastName);
                }
            }

            return terms.Distinct().ToList();
        }

        private static List<string> BuildNgrams(string value, int n = 3)
        {
            var normalized = NormalizeForIndex(value);
            var grams = new List<string>();

            if (string.IsNullOrEmpty(normalized))
                return grams;

            if (normalized.Length < n)
            {
                grams.Add(normalized);
                return grams;
            }

            for (int i = 0; i <= normalized.Length - n; i++)
                grams.Add(normalized.Substring(i, n));

            return grams.Distinct().ToList();
        }

        private static string ToBase64Display(object? data)
        {
            if (data == null)
            {
                return "*******";
            }

            if (data is byte[] bytes)
            {
                if (bytes.Length == 0)
                {
                    return "*******";
                }

                return Convert.ToBase64String(bytes);
            }

            var text = data.ToString();

            if (string.IsNullOrWhiteSpace(text))
            {
                return "*******";
            }
            return text;
        }

        private byte[] SearchIndexBytes(string rawData, string columnProfile)
        {
            var b64 = _securityService.GenerateSearchIndex(rawData, columnProfile);
            return Convert.FromBase64String(b64);
        }
    }
}
