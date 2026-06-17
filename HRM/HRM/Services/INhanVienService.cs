using HRM.DTOs;
using HRM.Model;
using HRM.Model.NhanVien;

namespace HRM.Services
{
    public interface INhanVienService
    {
        Task<object> GetPagedPublicAsync(
            int page,
            int pageSize,
            string? sortColumn,
            string? sortDirection
        );

        Task<object> GetPagedPrivateAsync(
            int page,
            int pageSize,
            string? sortColumn,
            string? sortDirection
        );

        Task<PagedResult<NhanVienDTO>> SearchPublicAsync(
            string keyword,
            int page,
            int pageSize,
            string? sortColumn,
            string? sortDirection
        );

        Task<PagedResult<NhanVienDTO>> SearchPrivateAsync(
            string keyword,
            int page,
            int pageSize,
            string? sortColumn,
            string? sortDirection
        );

        Task<NhanVienDTO?> GetByIdPublicAsync(decimal id);
        Task<NhanVienDTO?> GetByIdPrivateAsync(decimal id);

        Task<decimal?> CreateAsync(NhanVienModel dto);
        Task<bool> UpdateAsync(decimal id, NhanVienModel dto);
        Task<bool> DeleteAsync(decimal id);

        Task RebuildSecureIndexBulkAsync(int batchSize = 1000);

        Task MigrateOldPlaintextDataAsync();
    }
}