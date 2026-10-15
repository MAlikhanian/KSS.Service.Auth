namespace KSS.Dto
{
    public class PermissionDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public Guid ModuleId { get; set; }
        public Guid ResourceId { get; set; }
        public List<PermissionTranslationDto> Translations { get; set; } = new();
    }

    public class PermissionTranslationDto
    {
        public Guid PermissionId { get; set; }
        public short LanguageId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
