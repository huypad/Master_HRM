using HRM.DTOs;
using HRM.Entities;

namespace HRM.Repositories
{
    public interface INhanVienRepository
    {
        Task<int> CountPublicAsync();
        Task<int> CountPrivateAsync();

        Task<List<NhanVienDTO>> GetPagedPublicAsync(
            int page,
            int pageSize,
            string? sortColumn,
            string? sortDirection
        );

        Task<List<NhanVienDTO>> GetPagedPrivateAsync(
            int page,
            int pageSize,
            string? sortColumn,
            string? sortDirection
        );

        Task<int> CountSearchPublicAsync(string keyword);
        Task<int> CountSearchPrivateAsync(string keyword);

        Task<List<NhanVienDTO>> SearchPublicAsync(
            string keyword,
            int page,
            int pageSize,
            string? sortColumn,
            string? sortDirection
        );

        Task<List<NhanVienDTO>> SearchPrivateAsync(
            string keyword,
            int page,
            int pageSize,
            string? sortColumn,
            string? sortDirection
        );

        Task<NhanVienDTO?> GetByIdPublicAsync(decimal id);
        Task<NhanVienDTO?> GetByIdPrivateAsync(decimal id);

        Task<decimal?> AddAsync(NhanVien entity);
        Task UpdateAsync(NhanVien entity);
        Task SoftDeleteAsync(decimal id);

        Task RebuildAllSecureIndexBulkAsync(int batchSize = 1000);

        Task MigrateOldPlaintextDataAsync();
    }
}