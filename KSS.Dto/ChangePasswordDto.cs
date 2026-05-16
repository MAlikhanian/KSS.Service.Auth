namespace KSS.Dto
{
    /// <summary>
    /// Self-service password change. UserId is taken from the JWT — not the body.
    /// </summary>
    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
