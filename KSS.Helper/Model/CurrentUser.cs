namespace KSS.Helper.Model
{
    public class CurrentUser
    {
        public required string AccountName { get; set; }
        public int PersonId { get; set; }
        public short PositionId { get; set; }
        public short DepartmentId { get; set; }
    }
}
