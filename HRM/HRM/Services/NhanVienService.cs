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
                total
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
                total
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
        // Phase 1: DB Query (plaintext - không có decrypt)
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var items = await _repo.SearchPublicAsync(keyword, page, pageSize, sortColumn, sortDirection);
        var phase1Ms = sw.ElapsedMilliseconds;

        // Phase 2: Count (đếm log)
        sw.Restart();
        var total = await _repo.CountSearchPublicAsync(keyword);
        var phase2Ms = sw.ElapsedMilliseconds;

        int collisionCount = total - items.Count;

        _logger.LogInformation(
            "[PUBLIC SEARCH] Keyword={Keyword} | Phase1_DBQuery={P1}ms | Phase2_Count={P2}ms | Collisions={Collisions} | Results={Results}",
            keyword, phase1Ms, phase2Ms, collisionCount, items.Count);

        return new PagedResult<NhanVienDTO> { Items = items, Total = total };
        }

        public async Task<PagedResult<NhanVienDTO>> SearchPrivateAsync(
            string keyword,
            int page,
            int pageSize,
            string? sortColumn,
            string? sortDirection
        )
        {
        keyword = keyword.Trim();
        var sw = new System.Diagnostics.Stopwatch();

        // Phase 1: Querying over encrypted data (SecureIndex n-gram)
        sw.Restart();
        var candidateIds = await _repo.SearchCandidateIdsBySecureIndexAsync(keyword);
        if (!string.IsNullOrWhiteSpace(keyword) && keyword.All(char.IsDigit))
        {
            var exactIds = await _repo.FindIdsByCMNDHashAsync(keyword);
            candidateIds = candidateIds.Union(exactIds).Distinct().ToList();
        }
        var phase1Ms = sw.ElapsedMilliseconds;

        // Phase 2: Decryption
        sw.Restart();
        var candidateItems = await _repo.GetPrivateByIdsAsync(candidateIds);
        var phase2Ms = sw.ElapsedMilliseconds;

        // Phase 3: Filter results (in-memory)
        sw.Restart();
        var normalizedKeyword = keyword.ToLowerInvariant();
        IEnumerable<NhanVienDTO> query = candidateItems.Where(x =>
            ((x.MaNV  ?? "").ToLower().Contains(normalizedKeyword)) ||
            ((x.HoTen ?? "").ToLower().Contains(normalizedKeyword)) ||
            ((x.CMND  ?? "").Contains(keyword))                     ||
            ((x.Mobile?? "").Contains(keyword))                     ||
            ((x.Email ?? "").ToLower().Contains(normalizedKeyword))
        );
        query = ApplySortingPrivate(query, sortColumn, sortDirection);
        if (string.IsNullOrWhiteSpace(sortColumn))
            query = query.OrderBy(x => x.Id_NV);

        var filtered = query.ToList();
        var phase3Ms = sw.ElapsedMilliseconds;

        int collisionCount = candidateIds.Count - filtered.Count;

        _logger.LogInformation(
            "[PRIVATE SEARCH] Keyword={Keyword} | Phase1_EncryptedQuery={P1}ms | Phase2_Decryption={P2}ms | Phase3_Filter={P3}ms | Candidates={Candidates} | Collisions={Collisions} | Results={Results}",
            keyword, phase1Ms, phase2Ms, phase3Ms, candidateIds.Count, collisionCount, filtered.Count);

        var paged = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<NhanVienDTO> { Items = paged, Total = filtered.Count };
        }

        public async Task<NhanVienDTO?> GetByIdPublicAsync(decimal id)
        {
            return await _repo.GetByIdPublicAsync(id);
        }

        public async Task<NhanVienDTO?> GetByIdPrivateAsync(decimal id)
        {
            // Repository đã decrypt và trả về NhanVienDTO rồi,
            // service không cần ghép Holot/Ten hay decrypt lại.
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

            // Nếu model có số tài khoản thì bật dòng này.
            // nv.Sotaikhoan = dto.Sotaikhoan;

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

            // Nếu model có số tài khoản thì bật dòng này.
            // nv.Sotaikhoan = dto.Sotaikhoan;

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