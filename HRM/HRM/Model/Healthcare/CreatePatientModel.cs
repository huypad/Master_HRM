namespace HRM.Model.Healthcare
{
   

   
    public class CreatePatientModel
    {
        public string Name { get; set; } = string.Empty;
        public string CCCD { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string BankAccount { get; set; } = string.Empty;
        public int Age { get; set; } = 30;
        public string Gender { get; set; } = "Male";
        public string Blood_Type { get; set; } = "O+";
        public string Email { get; set; } = string.Empty;
    }
}
