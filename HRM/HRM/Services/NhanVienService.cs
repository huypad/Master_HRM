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
            var items = await _repo.SearchPublicAsync(keyword, page, pageSize, sortColumn, sortDirection);
            var total = await _repo.CountSearchPublicAsync(keyword);

            return new PagedResult<NhanVienDTO>
            {
                Items = items,
                Total = total
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
            var total = await _repo.CountSearchPrivateAsync(keyword);

            return new PagedResult<NhanVienDTO>
            {
                Items = items,
                Total = total
            };
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
    }
}