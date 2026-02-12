namespace KSS.Dto
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public DateTime TokenExpires { get; set; }
        public UserDto User { get; set; } = null!;
    }
}
