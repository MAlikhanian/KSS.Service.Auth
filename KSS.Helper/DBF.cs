using KSS.Helper.Model;
using OfficeOpenXml;
using SocialExplorer.IO.FastDBF;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace KSS.Helper
{
    public class DBF<D> where D : class, new()
    {
        private string DbfDirectory { get; set; }

        private string FilePath { get; set; }

        private readonly DbfFile dbfFile;

        public DBF(string directory, string filePath)
        {
            DbfDirectory = directory;
            FilePath = filePath;

            if (!Directory.Exists(DbfDirectory))
                Directory.CreateDirectory(DbfDirectory);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            dbfFile = new DbfFile(Encoding.GetEncoding(1256));
        }

        public List<D> Read()
        {
            List<D> items = new();

            dbfFile.Open(FilePath, FileMode.Open);

            DbfRecord dbfRecord = new(dbfFile.Header);

            while (dbfFile.ReadNext(dbfRecord))
            {
                D item = new();

                PropertyInfo[] properties = typeof(D).GetProperties();

                for (int i = 0; i < properties.Length; i++)
                {
                    PropertyInfo prop = properties[i];

                    var value = dbfRecord[i].Trim();

                    // Convert the DBF string value to the property type

                    object convertedValue = Convert.ChangeType(value, prop.PropertyType);

                    prop.SetValue(item, convertedValue);
                }

                items.Add(item);
            }

            dbfFile.Close();

            return items;
        }

        public List<Error> Write(D item)
        {
            List<D> items = new() { item };

            return Write(items);
        }

        public List<Error> Write(List<D> items)
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);

            dbfFile.Open(FilePath, FileMode.Create);

            PropertyInfo[] properties = typeof(D).GetProperties();

            foreach (PropertyInfo prop in properties)
            {
                var attribute = prop.GetCustomAttribute<DBFColumnType>();

                if (attribute != null)
                {
                    var columnType = ParseColumnType(attribute.ColumnType);

                    dbfFile.Header.AddColumn(new DbfColumn(prop.Name, columnType.Item1, columnType.Item2, 0));
                }
            }

            List<Error> errors = new();

            foreach (D item in items)
            {
                var dbfRecord = new DbfRecord(dbfFile.Header);

                StringBuilder errorTitle = new();

                try
                {
                    for (int i = 0; i < properties.Length; i++)
                    {
                        PropertyInfo prop = properties[i];

                        object value = prop.GetValue(item);

                        errorTitle.AppendFormat("{0}: {1}; ", prop.Name, value);

                        dbfRecord[i] = Convert.ToString(value); // Convert value to string; handle nulls and formatting as needed
                    }

                    dbfFile.Write(dbfRecord);
                }
                catch (Exception ex)
                {
                    errorTitle.AppendFormat("Exception: {0}", ex.Message);

                    errors.Add(new Error { ErrorTitle = errorTitle.ToString() });
                }
            }

            dbfFile.Close();

            return errors;
        }

        private static Tuple<DbfColumn.DbfColumnType, int> ParseColumnType(string type)
        {
            string[] parts = type.Split('(');
            string typeName = parts[0];
            int length = 10; // Default length

            if (parts.Length > 1)
            {
                string lengthPart = parts[1].Replace(")", "");
                int.TryParse(lengthPart, out length);
            }

            return new Tuple<DbfColumn.DbfColumnType, int>
            (
                typeName == "CHAR" ? DbfColumn.DbfColumnType.Character : DbfColumn.DbfColumnType.Number,
                length
            );
        }

        #region Old Sample

        //private void DBF_CreateTable<T>(string fileName)
        //{
        //    //Old Sample

        //    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        //    {
        //        StringBuilder command = new();

        //        List<string> columnNames = new();

        //        command.Append($"CREATE TABLE {fileName} ");
        //        command.Append('(');

        //        foreach (var property in typeof(T).GetProperties())
        //        {
        //            var columnType = property.GetCustomAttribute<DBFColumnType>().DBFType;
        //            columnNames.Add(property.Name);
        //            command.Append($"{property.Name} {columnType}, ");
        //        }

        //        command.Length -= 2;

        //        command.Append(')');

        //        using OleDbCommand cmd = new(command.ToString(), oleDbConnection);

        //        string filePath = Path.Combine(oleDbConnection.DataSource, fileName);

        //        if (File.Exists(filePath))
        //            File.Delete(filePath);

        //        cmd.ExecuteNonQuery();
        //    }
        //    else
        //        Console.WriteLine("Operation not supported on this OS.");
        //}

        //private void DBF_InsertItem<T>(string fileName, T item) where T : class
        //{
        //    if (item != null)
        //    {
        //        List<T> items = new() { item };

        //        DBF_InsertItem(fileName, items);
        //    }
        //}

        //private void DBF_InsertItem<T>(string fileName, List<T> items) where T : class
        //{
        //    if (items != null)
        //    {
        //        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        //        {
        //            List<string> columnNames = new();

        //            foreach (var property in typeof(T).GetProperties())
        //                columnNames.Add(property.Name);

        //            foreach (var item in items)
        //            {
        //                List<string> values = new();

        //                foreach (var property in typeof(T).GetProperties())
        //                    values.Add($"'{property.GetValue(item)?.ToString()?.Replace("'", "''") ?? "NULL"}'".ToIranSystem());

        //                string command = $"INSERT INTO {fileName} ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", values)})";

        //                using OleDbCommand cmd = new(command, oleDbConnection);

        //                try
        //                {
        //                    cmd.ExecuteNonQuery();
        //                }
        //                catch (Exception)
        //                {
        //                    errors.Add(new Error { ErrorTitle = command });
        //                }
        //            }
        //        }
        //        else
        //            Console.WriteLine("Operation not supported on this OS.");
        //    }
        //}

        //private DataTable DBF_Read(string fileName)
        //{
        //    DataTable dataTable = null;

        //    string filePath = Path.Combine(oleDbConnection.DataSource, $"{fileName}.dbf");

        //    if (File.Exists(filePath))
        //    {
        //        dataTable = new DataTable();

        //        string command = $"SELECT * FROM {fileName}";
        //        //string command = $"SELECT * FROM alikhani";

        //        OleDbDataAdapter adapter = new OleDbDataAdapter(command, oleDbConnection);

        //        adapter.Fill(dataTable);

        //        foreach (DataRow row in dataTable.Rows)
        //        {
        //            foreach (DataColumn column in dataTable.Columns)
        //            {
        //                Console.Write($"{column.ColumnName}: {row[column]} ");

        //                string test = ConvertTo.UnicodeFrom(TextEncoding.Arabic1256, row[column].ToString());

        //                if (test == "0082280665")
        //                {

        //                }
        //            }
        //            Console.WriteLine();
        //        }
        //    }

        //    return dataTable;
        //}

        //private string DBF_ConnectionString()
        //{
        //    if (!Directory.Exists(_fileSetting.TempDirectory.DBF))
        //        Directory.CreateDirectory(_fileSetting.TempDirectory.DBF);

        //    //return $@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={directory};Extended Properties=dBASE IV;";

        //    //https://www.microsoft.com/en-us/download/details.aspx?id=54920
        //    //accessdatabaseengine_X64.exe
        //    return $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={_fileSetting.TempDirectory.DBF};Extended Properties=dBASE IV;";
        //}

        #endregion
    }
}
