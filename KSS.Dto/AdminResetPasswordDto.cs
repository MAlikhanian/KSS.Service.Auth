namespace KSS.Dto
{
    /// <summary>
    /// Admin-only password reset for another user. No current password required.
    /// </summary>
    public class AdminResetPasswordDto
    {
        public Guid UserId { get; set; }
        public string NewPassword { get; set; } = string.Empty;
    }
}
