namespace KSS.Dto
{
    public class CreatePersonRequestDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public byte SexId { get; set; } = 1;
        // Persian language Id (fa = 12). Id 1 is Amharic — do not use.
        // English = 10 is the other active language.
        public short PreferredLanguageId { get; set; } = 12;
        public string NationalId { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; } = new DateTime(1990, 1, 1);
        public short BirthCountryId { get; set; } = 1;
        public short BirthRegionId { get; set; } = 1;
        public int BirthCityId { get; set; } = 1;
    }
}
