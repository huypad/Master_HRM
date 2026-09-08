using HRM.Model;
using HRM.Model.Healthcare;

namespace HRM.Services
{
  
   

    public interface IPatientSearchService
    {

        
        Task<PagedResult<PatientDto>> SearchExactAsync(string keyword, string field);

      
        Task<PagedResult<PatientDto>> SearchFuzzyAsync(string keyword);

        
       
        Task<PagedResult<PatientDto>> SearchExactBaselineAsync(string keyword, string field);

        
       
        Task<PagedResult<PatientDto>> SearchFuzzyBaselineAsync(string keyword);
    }
}
