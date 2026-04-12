using Hospital.api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PatientController : ControllerBase
    {
        private readonly HospitalDbContext _context;

        public PatientController(HospitalDbContext context)
        {
            _context = context;
        }

        // ================= GET ALL =================
        [HttpGet]
        public IActionResult GetPatients()
        {
            var data = _context.Patients.ToList();

            var result = data.Select(p => new
            {
                p.Id,
                p.Campcode,
                p.Name,
                p.Gender,
                p.Age,
                p.Address,
                p.ContactNo,
                p.NID,
                p.MedicalRecordNumber,
                p.PreSurgeryVisualAcuity,
                p.DateOfSurgery,
                p.TypeOfSurgery,
                p.EyeOperated,
                p.PostSurgeryVisualAcuity,

                BeneficiaryPhoto = p.BeneficiaryPhoto != null
                    ? Convert.ToBase64String(p.BeneficiaryPhoto)
                    : null
            });

            return Ok(result);
        }

        // ================= GET BY ID =================
        [HttpGet("{id}")]
        public IActionResult GetPatient(int id)
        {
            var p = _context.Patients.FirstOrDefault(x => x.Id == id);

            if (p == null)
                return NotFound("Patient not found");

            return Ok(new
            {
                p.Id,
                p.Campcode,
                p.Name,
                p.Gender,
                p.Age,
                p.Address,
                p.ContactNo,
                p.NID,
                p.MedicalRecordNumber,
                p.PreSurgeryVisualAcuity,
                p.DateOfSurgery,
                p.TypeOfSurgery,
                p.EyeOperated,
                p.PostSurgeryVisualAcuity,

                BeneficiaryPhoto = p.BeneficiaryPhoto != null
                    ? Convert.ToBase64String(p.BeneficiaryPhoto)
                    : null
            });
        }

        // ================= CREATE =================
        [HttpPost]
        public IActionResult AddPatient([FromBody] PatientDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid data");

            var patient = new Patient
            {
                Campcode=dto.Campcode,
                Name = dto.Name,
                Gender = dto.Gender,
                Age = dto.Age,
                Address = dto.Address,
                ContactNo = dto.ContactNo,
                NID = dto.NID,
                MedicalRecordNumber = dto.MedicalRecordNumber,
                PreSurgeryVisualAcuity = dto.PreSurgeryVisualAcuity,
                DateOfSurgery = dto.DateOfSurgery ?? DateTime.Now,
                TypeOfSurgery = dto.TypeOfSurgery,
                EyeOperated = dto.EyeOperated,
                PostSurgeryVisualAcuity = dto.PostSurgeryVisualAcuity,

                // ✅ VARBINARY FIX
                BeneficiaryPhoto = string.IsNullOrEmpty(dto.BeneficiaryPhoto)
                    ? null
                    : Convert.FromBase64String(dto.BeneficiaryPhoto)
            };

            _context.Patients.Add(patient);
            _context.SaveChanges();

            return Ok(patient);
        }

        // ================= UPDATE =================
        [HttpPut("{id}")]
        public IActionResult UpdatePatient(int id, [FromBody] PatientDto dto)
        {
            var existing = _context.Patients.FirstOrDefault(x => x.Id == id);

            if (existing == null)
                return NotFound("Patient not found");
            existing.Campcode=dto.Campcode;
            existing.Name = dto.Name;
            existing.Gender = dto.Gender;
            existing.Age = dto.Age;
            existing.Address = dto.Address;
            existing.ContactNo = dto.ContactNo;
            existing.NID= dto.NID;
            existing.MedicalRecordNumber = dto.MedicalRecordNumber;
            existing.PreSurgeryVisualAcuity = dto.PreSurgeryVisualAcuity;
            existing.DateOfSurgery = dto.DateOfSurgery;
            existing.TypeOfSurgery = dto.TypeOfSurgery;
            existing.EyeOperated = dto.EyeOperated;
            existing.PostSurgeryVisualAcuity = dto.PostSurgeryVisualAcuity;

            // ✅ update photo only if sent
            if (!string.IsNullOrWhiteSpace(dto.BeneficiaryPhoto))
            {
                existing.BeneficiaryPhoto = Convert.FromBase64String(dto.BeneficiaryPhoto);
            }

            _context.SaveChanges();

            return Ok(existing);
        }

        // ================= DELETE =================
        [HttpDelete("{id}")]
        public IActionResult DeletePatient(int id)
        {
            var patient = _context.Patients.FirstOrDefault(x => x.Id == id);

            if (patient == null)
                return NotFound("Patient not found");

            _context.Patients.Remove(patient);
            _context.SaveChanges();

            return Ok("Deleted successfully");
        }
    }
}