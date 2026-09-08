using HRM.Model;
using HRM.Model.Healthcare;

namespace HRM.Services
{
    
  
    public interface IPatientService
    {
        Task<PagedResult<PatientDto>> GetPagedAsync(int page = 1, int pageSize = 10);
        Task<PatientDto?> GetByIdAsync(int id);
        Task<PatientDto> CreateAsync(CreatePatientModel model);
        Task<bool> UpdateAsync(int id, CreatePatientModel model);
        Task<bool> DeleteAsync(int id);
    }
}
