namespace KSS.Dto
{
    public class AssignRoleRequestDto
    {
        public Guid UserId { get; set; }
        public List<Guid> RoleIds { get; set; } = new();
    }
}
