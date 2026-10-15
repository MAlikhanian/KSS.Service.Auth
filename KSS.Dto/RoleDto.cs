namespace KSS.Dto
{
    public class RoleDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public Guid? ModuleId { get; set; }
        public List<RoleTranslationDto> Translations { get; set; } = new();
        /// <summary>Permission Codes assigned to this role.</summary>
        public List<string> Permissions { get; set; } = new();
    }

    public class RoleTranslationDto
    {
        public Guid RoleId { get; set; }
        public short LanguageId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
