namespace KSS.Helper
{
    public class ColumnType : Attribute
    {
        public string Type { get; private set; }

        public ColumnType(string type)
        {
            Type = type;
        }
    }
}
