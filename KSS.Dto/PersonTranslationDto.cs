namespace KSS.Dto
{
    public class PersonTranslationDto
    {
        public Guid PersonId { get; set; }
        public short LanguageId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? FatherName { get; set; }
        public string? DisplayName { get; set; }
    }
}
