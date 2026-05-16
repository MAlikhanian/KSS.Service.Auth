namespace KSS.Dto
{
    /// <summary>
    /// Admin-only request to lock a user for a number of minutes.
    /// </summary>
    public class LockUserDto
    {
        public int LockMinutes { get; set; } = 30;
    }
}
