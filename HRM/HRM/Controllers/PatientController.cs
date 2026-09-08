using System.Threading.Tasks;
using HRM.Model;
using HRM.Model.Healthcare;
using HRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Controllers
{
    [ApiController]
    [Route("api/patient")]
    public class PatientController : ControllerBase
    {
        private readonly IPatientService _patientService;

        public PatientController(IPatientService patientService)
        {
            _patientService = patientService;
        }

       

      
        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _patientService.GetPagedAsync(page, pageSize);
            return Ok(result);
        }

   
        /// Lấy chi tiết thông tin bệnh nhân theo ID.
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var patient = await _patientService.GetByIdAsync(id);
            if (patient == null)
            {
                return NotFound(new { message = $"Không tìm thấy bệnh nhân có ID = {id}" });
            }
            return Ok(patient);
        }

     
        /// Thêm mới bệnh nhân.
       

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePatientModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
            {
                return BadRequest(new { message = "Họ tên bệnh nhân không được để trống." });
            }

            var created = await _patientService.CreateAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = created.PatientID }, created);
        }

        /// Cập nhật thông tin bệnh nhân theo ID.
      
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreatePatientModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
            {
                return BadRequest(new { message = "Họ tên bệnh nhân không được để trống." });
            }

            var success = await _patientService.UpdateAsync(id, model);
            if (!success)
            {
                return NotFound(new { message = $"Không tìm thấy bệnh nhân có ID = {id} để cập nhật." });
            }

            return Ok(new { message = "Cập nhật thông tin bệnh nhân thành công." });
        }

        /// Xóa bệnh nhân theo ID.
       
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _patientService.DeleteAsync(id);
            if (!success)
            {
                return NotFound(new { message = $"Không tìm thấy bệnh nhân có ID = {id} để xóa." });
            }

            return Ok(new { message = "Xóa bệnh nhân thành công." });
        }
    }
}
