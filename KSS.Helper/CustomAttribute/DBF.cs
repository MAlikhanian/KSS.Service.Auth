namespace KSS.Helper
{
    public class DBFColumnType : Attribute
    {
        public string ColumnType { get; private set; }

        public DBFColumnType(string columnType)
        {
            ColumnType = columnType;
        }
    }
}