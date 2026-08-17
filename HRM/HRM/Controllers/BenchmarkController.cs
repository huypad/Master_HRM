using HRM.Model;
using HRM.Model.Healthcare;
using HRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Controllers
{
    [ApiController]
    [Route("api/benchmark")]
    public class BenchmarkController : ControllerBase
    {
        private readonly IPatientSearchService _searchService;

        public BenchmarkController(IPatientSearchService searchService)
        {
            _searchService = searchService;
        }

        
        /// Endpoint tra cứu dữ liệu Bệnh nhân trên HealthcareDB theo Pipeline chỉ định (V1 hoặc V2).
        /// Trả về PagedResult<PatientDto> chứa chỉ số đo đạc SearchDebugInfo.
       
        /// <param name="keyword">Từ khóa tra cứu.</param>
        /// <param name="field">Cột cần tra: "Name", "CCCD", "Phone", hoặc "Bank". Mặc định "Name".</param>
        /// <param name="pipeline">Phiên bản pipeline: "V1" (SHA256 Baseline) hoặc "V2" (HMAC + BitGram). Mặc định "V2".</param>
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string keyword,
            [FromQuery] string field = "Name",
            [FromQuery] string pipeline = "V2")
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return BadRequest(new { message = "Từ khóa tìm kiếm (keyword) không được để trống." });
            }

            var isV1 = pipeline.Equals("V1", StringComparison.OrdinalIgnoreCase);
            var isName = field.Equals("Name", StringComparison.OrdinalIgnoreCase);

            PagedResult<PatientDto> result;

            if (isV1)
            {
                result = isName
                    ? await _searchService.SearchFuzzyBaselineAsync(keyword)
                    : await _searchService.SearchExactBaselineAsync(keyword, field);
            }
            else
            {
                result = isName
                    ? await _searchService.SearchFuzzyAsync(keyword)
                    : await _searchService.SearchExactAsync(keyword, field);
            }

            return Ok(result);
        }

        
        /// Endpoint chạy song song V1 Baseline và V2 HMAC/BitGram trên cùng từ khóa để so sánh hiệu năng.
        /// Trả về Anonymous Object JSON kết hợp số liệu so sánh trực tiếp.
    
        /// <param name="keyword">Từ khóa tra cứu.</param>
        /// <param name="field">Cột cần tra: "Name", "CCCD", "Phone", hoặc "Bank". Mặc định "Name".</param>
        [HttpGet("compare")]
        public async Task<IActionResult> Compare(
            [FromQuery] string keyword,
            [FromQuery] string field = "Name")
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return BadRequest(new { message = "Từ khóa tìm kiếm (keyword) không được để trống." });
            }

            var isName = field.Equals("Name", StringComparison.OrdinalIgnoreCase);

            // Chạy bất đồng bộ đồng thời V1 Baseline và V2 HMAC/BitGram qua Task.WhenAll
            var v1Task = isName
                ? _searchService.SearchFuzzyBaselineAsync(keyword)
                : _searchService.SearchExactBaselineAsync(keyword, field);

            var v2Task = isName
                ? _searchService.SearchFuzzyAsync(keyword)
                : _searchService.SearchExactAsync(keyword, field);

            await Task.WhenAll(v1Task, v2Task);

            var v1Result = await v1Task;
            var v2Result = await v2Task;

            // Tính toán tỷ lệ tăng tốc (Speedup Ratios)
            double v1Step1 = v1Result.SearchDebug?.Step1Ms ?? 0;
            double v2Step1 = v2Result.SearchDebug?.Step1Ms ?? 0;
            double step1Speedup = v2Step1 > 0 ? Math.Round(v1Step1 / v2Step1, 2) : 1.0;

            double v1Total = v1Result.SearchDebug?.TotalMs ?? 0;
            double v2Total = v2Result.SearchDebug?.TotalMs ?? 0;
            double totalSpeedup = v2Total > 0 ? Math.Round(v1Total / v2Total, 2) : 1.0;

            string winner = v2Total <= v1Total ? "V2" : "V1";

            return Ok(new
            {
                keyword,
                field,
                v1 = v1Result,
                v2 = v2Result,
                comparison = new
                {
                    step1SpeedupRatio = step1Speedup,
                    totalSpeedupRatio = totalSpeedup,
                    winner
                }
            });
        }
    }
}
