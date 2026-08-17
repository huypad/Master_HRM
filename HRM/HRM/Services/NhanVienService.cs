using HRM.DTOs;
using HRM.Entities;
using HRM.Model;
using HRM.Model.NhanVien;
using HRM.Repositories;
using Microsoft.Extensions.Logging; 

namespace HRM.Services
{
    public class NhanVienService : INhanVienService
    {
        private readonly INhanVienRepository _repo;
        private readonly ILogger<NhanVienService> _logger;

        public NhanVienService(INhanVienRepository repo, ILogger<NhanVienService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<object> GetPagedPublicAsync(
            int page,
            int pageSize,
            string? sortColumn,
            string? sortDirection
        )
        {
            var items = await _repo.GetPagedPublicAsync(page, pageSize, sortColumn, sortDirection);
            var total = await _repo.CountPublicAsync();

            return new
            {
                items,
                total,
                searchDebug = (SearchDebugInfo?)null
            };
        }

        public async Task<object> GetPagedPrivateAsync(
            int page,
            int pageSize,
            string? sortColumn,
            string? sortDirection
        )
        {
            var items = await _repo.GetPagedPrivateAsync(page, pageSize, sortColumn, sortDirection);
            var total = await _repo.CountPrivateAsync();

            return new
            {
                items,
                total,
                searchDebug = (SearchDebugInfo?)null
            };
        }

        public async Task<PagedResult<NhanVienDTO>> SearchPublicAsync(
            string keyword,
            int page,
            int pageSize,
            string? sortColumn,
            string? sortDirection
        )
        {
            var items = await _repo.SearchPublicAsync(keyword, page, pageSize, sortColumn, sortDirection);
            var debug = _repo.LastSearchDebug;

            if (debug != null)
            {
                _logger.LogInformation(
                    "[PUBLIC SEARCH] Keyword={Keyword} | Step1={P1}ms | Step2={P2}ms | Total={Total}ms | Candidates={Candidates} | Collisions={Collisions} | Results={Results}",
                    keyword, debug.Step1Ms, debug.Step2Ms, debug.TotalMs, debug.CandidateCount, debug.CollisionCount, items.Count);
            }

            return new PagedResult<NhanVienDTO>
            {
                Items = items,
                Total = _repo.LastSearchTotal,
                SearchDebug = debug
            };
        }

        public async Task<PagedResult<NhanVienDTO>> SearchPrivateAsync(
            string keyword,
            int page,
            int pageSize,
            string? sortColumn,
            string? sortDirection
        )
        {
            var items = await _repo.SearchPrivateAsync(keyword, page, pageSize, sortColumn, sortDirection);
            var debug = _repo.LastSearchDebug;

            if (debug != null)
            {
                _logger.LogInformation(
                    "[PRIVATE SEARCH] Keyword={Keyword} | Step1={P1}ms | Step2={P2}ms | Total={Total}ms | Candidates={Candidates} | Collisions={Collisions} | Results={Results}",
                    keyword, debug.Step1Ms, debug.Step2Ms, debug.TotalMs, debug.CandidateCount, debug.CollisionCount, items.Count);
            }

            return new PagedResult<NhanVienDTO>
            {
                Items = items,
                Total = _repo.LastSearchTotal,
                SearchDebug = debug
            };
        }

        public async Task<NhanVienDTO?> GetByIdPublicAsync(decimal id)
        {
            return await _repo.GetByIdPublicAsync(id);
        }

        public async Task<NhanVienDTO?> GetByIdPrivateAsync(decimal id)
        {
            return await _repo.GetByIdPrivateAsync(id);
        }

        public async Task<decimal?> CreateAsync(NhanVienModel dto)
        {
            var nv = new NhanVien
            {
                MaNV = dto.MaNV,
                Holot = dto.Holot,
                Ten = dto.Ten,
                Ngaysinh = dto.Ngaysinh,
                CMND = dto.CMND,
                Mobile = dto.Mobile,
                Email = dto.Email,
                Disable = false
            };

            return await _repo.AddAsync(nv);
        }

        public async Task<bool> UpdateAsync(decimal id, NhanVienModel dto)
        {
            var existing = await _repo.GetByIdPrivateAsync(id);
            if (existing == null)
                return false;

            var nv = new NhanVien
            {
                Id_NV = id,
                MaNV = dto.MaNV,
                Holot = dto.Holot,
                Ten = dto.Ten,
                Ngaysinh = dto.Ngaysinh,
                CMND = dto.CMND,
                Mobile = dto.Mobile,
                Email = dto.Email,
                Disable = false
            };

            await _repo.UpdateAsync(nv);
            return true;
        }

        public async Task<bool> DeleteAsync(decimal id)
        {
            var existing = await _repo.GetByIdPrivateAsync(id);
            if (existing == null)
                return false;

            await _repo.SoftDeleteAsync(id);
            return true;
        }

        public async Task RebuildSecureIndexBulkAsync(int batchSize = 1000)
        {
            await _repo.RebuildAllSecureIndexBulkAsync(batchSize);
        }

        public async Task MigrateOldPlaintextDataAsync()
        {
            await _repo.MigrateOldPlaintextDataAsync();
        }

        private IEnumerable<NhanVienDTO> ApplySortingPrivate(
            IEnumerable<NhanVienDTO> query,
            string? sortColumn,
            string? sortDirection)
        {
            if (string.IsNullOrWhiteSpace(sortColumn))
                return query;

            bool isDesc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

            return sortColumn.ToLower() switch
            {
                "manv"      => isDesc ? query.OrderByDescending(x => x.MaNV)      : query.OrderBy(x => x.MaNV),
                "hoten"     => isDesc ? query.OrderByDescending(x => x.HoTen)     : query.OrderBy(x => x.HoTen),
                "cmnd"      => isDesc ? query.OrderByDescending(x => x.CMND)      : query.OrderBy(x => x.CMND),
                "mobile"    => isDesc ? query.OrderByDescending(x => x.Mobile)    : query.OrderBy(x => x.Mobile),
                "email"     => isDesc ? query.OrderByDescending(x => x.Email)     : query.OrderBy(x => x.Email),
                "ngaysinh"  => isDesc ? query.OrderByDescending(x => x.NgaySinh)  : query.OrderBy(x => x.NgaySinh),
                _           => query
            };
        }
    }
}
