using HRM.Model;
using HRM.Model.Healthcare;

namespace HRM.Services
{
  
    /// Contract dịch vụ tra cứu 2 bước (2-Step Search) cho HealthcareDB.
    /// Cung cấp các phương thức tra cứu Pipeline V2 (HMAC + BitGram Bucket)
    /// và Pipeline V1 Baseline (SHA256 fixed-salt) để phục vụ đo đạc Benchmark.

    public interface IPatientSearchService
    {

        /// Tra cứu chính xác (Exact Search) theo CCCD, Phone, hoặc BankAccount bằng Pipeline V2 (HMAC-SHA256).
        
        /// <param name="keyword">Từ khóa tra cứu (CCCD, SĐT, Số tài khoản).</param>
        /// <param name="field">Tên cột cần tra ("CCCD", "Phone", "Bank").</param>
        /// <returns>PagedResult chứa danh sách bệnh nhân và chỉ số đo đạc SearchDebugInfo.</returns>
        Task<PagedResult<PatientDto>> SearchExactAsync(string keyword, string field);

        /// Tra cứu gần đúng (Fuzzy Search) theo Họ tên bằng Pipeline V2 (BitGram 16-bit Bucket).
        
        /// <param name="keyword">Từ khóa Họ tên cần tìm.</param>
        /// <returns>PagedResult chứa danh sách bệnh nhân và chỉ số đo đạc SearchDebugInfo.</returns>
        Task<PagedResult<PatientDto>> SearchFuzzyAsync(string keyword);

        
        /// Tra cứu chính xác Baseline bằng Pipeline V1 (SHA256 fixed-salt) trên HealthcareDB để so sánh sòng phẳng.
        
        /// <param name="keyword">Từ khóa tra cứu.</param>
        /// <param name="field">Tên cột cần tra.</param>
        /// <returns>PagedResult chứa danh sách bệnh nhân và chỉ số đo đạc SearchDebugInfo.</returns>
        Task<PagedResult<PatientDto>> SearchExactBaselineAsync(string keyword, string field);

        
        /// Tra cứu gần đúng Baseline bằng Pipeline V1 (SHA256 fixed-salt Trigram) trên HealthcareDB để so sánh sòng phẳng.
       
        /// <param name="keyword">Từ khóa Họ tên cần tìm.</param>
        /// <returns>PagedResult chứa danh sách bệnh nhân và chỉ số đo đạc SearchDebugInfo.</returns>
        Task<PagedResult<PatientDto>> SearchFuzzyBaselineAsync(string keyword);
    }
}
