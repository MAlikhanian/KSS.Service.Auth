namespace KSS.Helper
{
    public class ColumnPersianName : Attribute
    {
        public string PersianName { get; private set; }

        public ColumnPersianName(string persianName)
        {
            PersianName = persianName;
        }
    }
}
