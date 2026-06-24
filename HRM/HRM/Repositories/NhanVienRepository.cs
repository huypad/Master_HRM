using HRM.Data;
using HRM.DTOs;
using HRM.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using HRM.Common;
using HRM.Helpers.Security;
using ISecurityService = HRM.Services.ISecurityService;

namespace HRM.Repositories
{
    public class NhanVienRepository : INhanVienRepository
    {
        private readonly HrmDbContext _context;
        private readonly ISecurityService _securityService;

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
            return await _context.NhanViens
                .CountAsync(x => x.Disable == false || x.Disable == null);
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
                    Mobile = "*******", 
                    Email = "*******"    
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
            var query = _context.NhanViens
                .AsNoTracking()
                .Where(x => x.Disable == false || x.Disable == null);

            query = ApplySorting(query, sortColumn, sortDirection);

            if (string.IsNullOrWhiteSpace(sortColumn))
            {
                query = query.OrderBy(x => x.Id_NV);
            }

            var totalItems = await query.CountAsync();
            var maxPage = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (maxPage <= 0)
            {
                page = 1;
            }
            else if (page > maxPage)
            {
                page = maxPage;
            }

            var pagedEntities = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new List<NhanVienDTO>();
            foreach (var entity in pagedEntities)
            {
                try
                {
                    var holot = (entity.I_Holot == null || entity.I_Holot.Length == 0)
                        ? string.Empty
                        : _securityService.DecryptData(Convert.ToBase64String(entity.I_Holot));
                    var ten = (entity.I_Ten == null || entity.I_Ten.Length == 0)
                        ? string.Empty
                        : _securityService.DecryptData(Convert.ToBase64String(entity.I_Ten));
                    var cmnd = (entity.I_CMND == null || entity.I_CMND.Length == 0)
                        ? string.Empty
                        : _securityService.DecryptData(Convert.ToBase64String(entity.I_CMND));

                    result.Add(new NhanVienDTO
                    {
                        Id_NV = entity.Id_NV,
                        MaNV = entity.MaNV,
                        HoTen = $"{holot} {ten}".Trim(),
                        NgaySinh = entity.Ngaysinh,
                        CMND = cmnd,
                        Mobile = string.IsNullOrWhiteSpace(entity.Mobile) ? " " : entity.Mobile,
                        Email = string.IsNullOrWhiteSpace(entity.Email) ? " " : entity.Email
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Decrypt lỗi tại Id_NV = {entity.Id_NV}. Lỗi: {ex.Message}");
                    continue;
                }
            }

            return result;
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

            var records = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    x.Id_NV,
                    x.MaNV,
                    x.Ngaysinh,
                    x.I_Holot,
                    x.I_Ten,
                    x.I_CMND,
                    x.Mobile,
                    x.Email
                })
                .ToListAsync();

            return records.Select(x => new NhanVienDTO
            {
                Id_NV = x.Id_NV,
                MaNV = x.MaNV,
                HoTen = $"{ToBase64Display(x.I_Holot)} {ToBase64Display(x.I_Ten)}".Trim(),
                NgaySinh = x.Ngaysinh,
                CMND = ToBase64Display(x.I_CMND),
                Mobile = "*******",
                Email = "*******"
            }).ToList();
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
            var record = await _context.NhanViens
                .AsNoTracking()
                .Where(x => x.Id_NV == id && (x.Disable == false || x.Disable == null))
                .Select(x => new
                {
                    x.Id_NV,
                    x.MaNV,
                    x.Ngaysinh,
                    x.I_Holot,
                    x.I_Ten,
                    x.I_CMND,
                    x.Mobile,
                    x.Email
                })
                .FirstOrDefaultAsync();

            if (record == null)
            {
                return null;
            }

            return new NhanVienDTO
            {
                Id_NV = record.Id_NV,
                MaNV = record.MaNV,
                HoTen = $"{ToBase64Display(record.I_Holot)} {ToBase64Display(record.I_Ten)}".Trim(),
                NgaySinh = record.Ngaysinh,
                CMND = ToBase64Display(record.I_CMND),
                Mobile = "*******",
                Email = "*******"
            };
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
                Value = string.IsNullOrWhiteSpace(entity.Holot) ? DBNull.Value : (object)Convert.FromBase64String(_securityService.EncryptData(entity.Holot))
            });

            cmd.Parameters.Add(new SqlParameter("@I_Ten", SqlDbType.VarBinary)
            {
                Value = string.IsNullOrWhiteSpace(entity.Ten) ? DBNull.Value : (object)Convert.FromBase64String(_securityService.EncryptData(entity.Ten))
            });

            cmd.Parameters.Add(new SqlParameter("@I_CMND", SqlDbType.VarBinary, 512)
            {
                Value = string.IsNullOrWhiteSpace(entity.CMND) ? DBNull.Value : (object)Convert.FromBase64String(_securityService.EncryptData(entity.CMND))
            });

            var cmndHash = string.IsNullOrWhiteSpace(entity.CMND)
                ? (object)DBNull.Value
                : Convert.FromBase64String(_securityService.GenerateSearchIndex(entity.CMND, "CMND"));

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
                Value = string.IsNullOrWhiteSpace(entity.Sotaikhoan) ? DBNull.Value : (object)Convert.FromBase64String(_securityService.EncryptData(entity.Sotaikhoan))
            });

            var sotaikhoanHash = string.IsNullOrWhiteSpace(entity.Sotaikhoan)
                ? (object)DBNull.Value
                : Convert.FromBase64String(_securityService.GenerateSearchIndex(entity.Sotaikhoan, "Sotaikhoan"));

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
                Value = string.IsNullOrWhiteSpace(entity.Holot) ? DBNull.Value : (object)Convert.FromBase64String(_securityService.EncryptData(entity.Holot))
            });

            cmd.Parameters.Add(new SqlParameter("@I_Ten", SqlDbType.VarBinary)
            {
                Value = string.IsNullOrWhiteSpace(entity.Ten) ? DBNull.Value : (object)Convert.FromBase64String(_securityService.EncryptData(entity.Ten))
            });

            cmd.Parameters.Add(new SqlParameter("@I_CMND", SqlDbType.VarBinary, 512)
            {
                Value = string.IsNullOrWhiteSpace(entity.CMND) ? DBNull.Value : (object)Convert.FromBase64String(_securityService.EncryptData(entity.CMND))
            });

            var cmndHash = string.IsNullOrWhiteSpace(entity.CMND)
                ? (object)DBNull.Value
                : Convert.FromBase64String(_securityService.GenerateSearchIndex(entity.CMND, "CMND"));

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
                Value = string.IsNullOrWhiteSpace(entity.Sotaikhoan) ? DBNull.Value : (object)Convert.FromBase64String(_securityService.EncryptData(entity.Sotaikhoan))
            });

            var sotaikhoanHash = string.IsNullOrWhiteSpace(entity.Sotaikhoan)
                ? (object)DBNull.Value
                : Convert.FromBase64String(_securityService.GenerateSearchIndex(entity.Sotaikhoan, "Sotaikhoan"));

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
                    var holot = reader["I_Holot"] == DBNull.Value || reader["I_Holot"] == null
                        ? string.Empty
                        : _securityService.DecryptData(Convert.ToBase64String((byte[])reader["I_Holot"]));
                    var ten = reader["I_Ten"] == DBNull.Value || reader["I_Ten"] == null
                        ? string.Empty
                        : _securityService.DecryptData(Convert.ToBase64String((byte[])reader["I_Ten"]));
                    var cmnd = reader["I_CMND"] == DBNull.Value || reader["I_CMND"] == null
                        ? string.Empty
                        : _securityService.DecryptData(Convert.ToBase64String((byte[])reader["I_CMND"]));

                    result.Add(new NhanVienDTO
                    {
                        Id_NV = id,
                        MaNV = reader["MaNV"] as string,
                        HoTen = string.IsNullOrWhiteSpace(holot) && string.IsNullOrWhiteSpace(ten) ? " " : $"{holot} {ten}".Trim(),
                        NgaySinh = reader["Ngaysinh"] == DBNull.Value
                            ? null
                            : (DateTime?)reader["Ngaysinh"],
                        CMND = string.IsNullOrWhiteSpace(cmnd) ? " " : cmnd,
                        Mobile = (reader["Mobile"] as string) ?? " ",
                        Email = (reader["Email"] as string) ?? " "
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
                    var holot = reader["I_Holot"] == DBNull.Value || reader["I_Holot"] == null
                        ? string.Empty
                        : _securityService.DecryptData(Convert.ToBase64String((byte[])reader["I_Holot"]));
                    var ten = reader["I_Ten"] == DBNull.Value || reader["I_Ten"] == null
                        ? string.Empty
                        : _securityService.DecryptData(Convert.ToBase64String((byte[])reader["I_Ten"]));
                    var cmnd = reader["I_CMND"] == DBNull.Value || reader["I_CMND"] == null
                        ? string.Empty
                        : _securityService.DecryptData(Convert.ToBase64String((byte[])reader["I_CMND"]));

                    result.Add(new NhanVienDTO
                    {
                        Id_NV = id,
                        MaNV = reader["MaNV"] as string,
                        HoTen = string.IsNullOrWhiteSpace(holot) && string.IsNullOrWhiteSpace(ten) ? " " : $"{holot} {ten}".Trim(),
                        NgaySinh = reader["Ngaysinh"] == DBNull.Value
                            ? null
                            : (DateTime?)reader["Ngaysinh"],
                        CMND = string.IsNullOrWhiteSpace(cmnd) ? " " : cmnd,
                        Mobile = string.IsNullOrWhiteSpace(reader["Mobile"] as string) ? " " : reader["Mobile"] as string,
                        Email = string.IsNullOrWhiteSpace(reader["Email"] as string) ? " " : reader["Email"] as string
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
            var hash = Convert.FromBase64String(_securityService.GenerateSearchIndex(cmnd, "CMND"));

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
            var grams = SecurityIndexHelper.BuildNgrams(keyword, 3);

            if (grams.Count == 0)
                return new List<decimal>();

            var hashes = grams
                .Select(x => SearchIndexBytes(x, "HoTen:NGram"))
                .ToList();

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
                    Value = SearchIndexBytes(grams[i], "HoTen:NGram")
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
                                SearchIndexBytes(grams[i], "HoTen:NGram"),
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
                    Value = string.IsNullOrWhiteSpace(row.Holot) ? DBNull.Value : (object)Convert.FromBase64String(_securityService.EncryptData(row.Holot))
                });

                cmd.Parameters.Add(new SqlParameter("@I_Ten", SqlDbType.VarBinary)
                {
                    Value = string.IsNullOrWhiteSpace(row.Ten) ? DBNull.Value : (object)Convert.FromBase64String(_securityService.EncryptData(row.Ten))
                });

                cmd.Parameters.Add(new SqlParameter("@I_CMND", SqlDbType.VarBinary, 512)
                {
                    Value = string.IsNullOrWhiteSpace(row.CMND) ? DBNull.Value : (object)Convert.FromBase64String(_securityService.EncryptData(row.CMND))
                });

                var cmndHash = string.IsNullOrWhiteSpace(row.CMND)
                    ? (object)DBNull.Value
                    : Convert.FromBase64String(_securityService.GenerateSearchIndex(row.CMND, "CMND"));

                cmd.Parameters.Add(new SqlParameter("@CMNDHash", SqlDbType.VarBinary, 32)
                {
                    Value = cmndHash
                });

                cmd.Parameters.Add(new SqlParameter("@I_Sotaikhoan", SqlDbType.VarBinary, 512)
                {
                    Value = string.IsNullOrWhiteSpace(row.Sotaikhoan) ? DBNull.Value : (object)Convert.FromBase64String(_securityService.EncryptData(row.Sotaikhoan))
                });

                var sotaikhoanHash = string.IsNullOrWhiteSpace(row.Sotaikhoan)
                    ? (object)DBNull.Value
                    : Convert.FromBase64String(_securityService.GenerateSearchIndex(row.Sotaikhoan, "Sotaikhoan"));

                cmd.Parameters.Add(new SqlParameter("@SotaikhoanHash", SqlDbType.VarBinary, 32)
                {
                    Value = sotaikhoanHash
                });

                await cmd.ExecuteNonQueryAsync();
            }
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