namespace KSS.Dto
{
    public class CreatePersonRequestDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public byte SexId { get; set; } = 1; // Default sex ID
        public short PreferredLanguageId { get; set; } = 1; // Persian language ID (default)
        public string NationalId { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; } = new DateTime(1990, 1, 1); // Default date of birth
        public short BirthCountryId { get; set; } = 1; // Default country ID
        public short BirthRegionId { get; set; } = 1; // Default region ID
        public int BirthCityId { get; set; } = 1; // Default city ID
        public short NationalityCountryId { get; set; } = 1; // Default nationality country ID
    }
}
