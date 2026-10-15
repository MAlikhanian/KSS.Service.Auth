namespace KSS.Dto
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public DateTime TokenExpires { get; set; }
        public UserDto User { get; set; } = null!;
        public List<string> Roles { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
    }
}
