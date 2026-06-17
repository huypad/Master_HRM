using HRM.Common;
using HRM.Model.NhanVien;
using HRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Controllers
{
    [ApiController]
    [Route("api/nhanvien")]
    public class NhanVienController : ControllerBase
    {
        private readonly INhanVienService _service;

        public NhanVienController(INhanVienService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? sortColumn = null,
            [FromQuery] string? sortDirection = "asc",
            [FromQuery] string accessMode = AccessModeConst.Public
        )
        {
            var isPrivate = accessMode.Equals(AccessModeConst.Private, StringComparison.OrdinalIgnoreCase);

            var data = isPrivate
                ? await _service.GetPagedPrivateAsync(page, pageSize, sortColumn, sortDirection)
                : await _service.GetPagedPublicAsync(page, pageSize, sortColumn, sortDirection);

            return Ok(data);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string keyword,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? sortColumn = null,
            [FromQuery] string? sortDirection = "asc",
            [FromQuery] string accessMode = AccessModeConst.Public
        )
        {
            var isPrivate = accessMode.Equals(AccessModeConst.Private, StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(keyword))
            {
                var data = isPrivate
                    ? await _service.GetPagedPrivateAsync(page, pageSize, sortColumn, sortDirection)
                    : await _service.GetPagedPublicAsync(page, pageSize, sortColumn, sortDirection);

                return Ok(data);
            }

            var result = isPrivate
                ? await _service.SearchPrivateAsync(keyword, page, pageSize, sortColumn, sortDirection)
                : await _service.SearchPublicAsync(keyword, page, pageSize, sortColumn, sortDirection);

            return Ok(new
            {
                items = result.Items,
                total = result.Total
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(
            decimal id,
            [FromQuery] string accessMode = AccessModeConst.Public
        )
        {
            var isPrivate = accessMode.Equals(AccessModeConst.Private, StringComparison.OrdinalIgnoreCase);

            var result = isPrivate
                ? await _service.GetByIdPrivateAsync(id)
                : await _service.GetByIdPublicAsync(id);

            return result == null
                ? NotFound(new { message = "Không tìm thấy nhân viên" })
                : Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] NhanVienModel dto)
        {
            var id = await _service.CreateAsync(dto);
            if (id == null)
                return BadRequest(new { message = "Tạo nhân viên thất bại" });

            return Ok(new
            {
                message = "Tạo nhân viên thành công",
                id
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(decimal id, [FromBody] NhanVienModel dto)
        {
            var success = await _service.UpdateAsync(id, dto);
            if (!success)
                return NotFound(new { message = "Không tìm thấy nhân viên để cập nhật" });

            return Ok(new { message = "Cập nhật thành công" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(decimal id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success)
                return NotFound(new { message = "Không tìm thấy nhân viên để xóa" });

            return Ok(new { message = "Xóa thành công" });
        }

        [HttpPost("rebuild-secure-index-bulk")]
        public async Task<IActionResult> RebuildSecureIndexBulk([FromQuery] int batchSize = 1000)
        {
            await _service.RebuildSecureIndexBulkAsync(batchSize);
            return Ok(new
            {
                message = "Rebuild SecureIndex bulk completed",
                batchSize
            });
        }

        [HttpPost("migrate-old-plaintext-data")]
        public async Task<IActionResult> MigrateOldPlaintextData()
        {
            await _service.MigrateOldPlaintextDataAsync();

            return Ok(new
            {
                message = "Migrate dữ liệu cũ sang encrypted/hash hoàn tất"
            });
        }
    }
}