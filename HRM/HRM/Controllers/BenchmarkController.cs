using HRM.Model.Healthcare;
using HRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BenchmarkController : ControllerBase
    {
        private readonly PatientSearchService _searchService;

        public BenchmarkController(PatientSearchService searchService)
        {
            _searchService = searchService;
        }

        /// <summary>
        /// Chạy song song V1 Baseline (Plaintext LIKE) vs V2 BitGram (HMAC Index + Decrypt RAM).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> RunBenchmark([FromQuery] string keyword, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return BadRequest("Keyword parameter is required.");
            }

            // Chạy song song 2 thuật toán bằng Task.WhenAll
            var v1Task = _searchService.SearchV1Async(keyword, page, pageSize);
            var v2Task = _searchService.SearchV2Async(keyword, page, pageSize);

            await Task.WhenAll(v1Task, v2Task);

            var response = new BenchmarkResult
            {
                Keyword = keyword,
                V1_Baseline = await v1Task,
                V2_BitGram = await v2Task
            };

            return Ok(response);
        }
    }
}
