using HRM.Model;
using HRM.Model.Healthcare;

namespace HRM.Services
{
    
    /// Contract dịch vụ Quản lý Bệnh nhân (CRUD) trên HealthcareDB.
    /// Tự động mã hóa AES-256, sinh HMAC Index và BitGram Bucket Index cho Patient_Secure & BitGramIndex_Patient.
    
    public interface IPatientService
    {
        Task<PagedResult<PatientDto>> GetPagedAsync(int page = 1, int pageSize = 10);
        Task<PatientDto?> GetByIdAsync(int id);
        Task<PatientDto> CreateAsync(CreatePatientModel model);
        Task<bool> UpdateAsync(int id, CreatePatientModel model);
        Task<bool> DeleteAsync(int id);
    }
}
