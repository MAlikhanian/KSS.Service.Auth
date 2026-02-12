namespace KSS.Dto
{
    public class PersonDto
    {
        public Guid Id { get; set; }
        public byte SexId { get; set; }
        public short PreferredLanguageId { get; set; }
        public string NationalId { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public short BirthCountryId { get; set; }
        public short BirthRegionId { get; set; }
        public int BirthCityId { get; set; }
        public short NationalityCountryId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
