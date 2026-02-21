namespace KSS.Dto
{
    public class CreateRoleRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<Guid> PermissionIds { get; set; } = new();
    }
}
