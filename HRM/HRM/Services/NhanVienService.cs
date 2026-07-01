using HRM.DTOs;
using HRM.Entities;
using HRM.Model;
using HRM.Model.NhanVien;
using HRM.Repositories;

namespace HRM.Services
{
    public class NhanVienService : INhanVienService
    {
        private readonly INhanVienRepository _repo;

        public NhanVienService(INhanVienRepository repo)
        {
            _repo = repo;
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
                Console.WriteLine($"\n========================================");
                Console.WriteLine($"[LOG JMETER] API: SearchPublicAsync (200k data)");
                Console.WriteLine($"- Thời gian lọc thô (B1): {debug.Step1Ms} ms");
                Console.WriteLine($"- Thời gian giải mã & lọc tinh (B2): {debug.Step2Ms} ms");
                Console.WriteLine($"- Tổng thời gian: {debug.TotalMs} ms");
                Console.WriteLine($"- Số lượng ứng viên: {debug.CandidateCount} dòng");
                Console.WriteLine($"- Đụng độ: {debug.CollisionCount} bản ghi");
                Console.WriteLine($"========================================\n");
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
                Console.WriteLine($"\n========================================");
                Console.WriteLine($"[LOG JMETER] API: SearchPrivateAsync (200k data)");
                Console.WriteLine($"- Thời gian lọc thô (B1): {debug.Step1Ms} ms");
                Console.WriteLine($"- Thời gian giải mã & lọc tinh (B2): {debug.Step2Ms} ms");
                Console.WriteLine($"- Tổng thời gian: {debug.TotalMs} ms");
                Console.WriteLine($"- Số lượng ứng viên: {debug.CandidateCount} dòng");
                Console.WriteLine($"- Đụng độ: {debug.CollisionCount} bản ghi");
                Console.WriteLine($"========================================\n");
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
    }
}
