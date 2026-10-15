namespace KSS.Dto
{
    /// <summary>
    /// POST body for /Api/User/MapPersonsToUsers. The caller supplies a list of
    /// PersonIds and gets back a map of PersonId → UserId for every input
    /// that has a User row. Used by dashboard report endpoints in other services.
    /// </summary>
    public class PersonsToUsersRequestDto
    {
        public List<Guid> PersonIds { get; set; } = new List<Guid>();
    }
}
