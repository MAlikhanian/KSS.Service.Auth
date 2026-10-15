using System.Text;

namespace KSS.Helper
{
    public class Notepad<D>
    {
        //public List<D> Read(string filePath, int worksheetIndex, Func<D> createInstance)
        //{
        //    List<D> dataModels = new();

        //    using (var package = new ExcelPackage(new FileInfo(filePath)))
        //    {
        //        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        //        ExcelWorksheet worksheet = package.Workbook.Worksheets[worksheetIndex];

        //        if (worksheet.Dimension != null)
        //        {
        //            int rowCount = worksheet.Dimension.Rows;
        //            int colCount = worksheet.Dimension.Columns;

        //            for (int row = 1; row <= rowCount; row++)
        //            {
        //                D dataModel = createInstance();

        //                for (int col = 1; col <= colCount; col++)
        //                {
        //                    var cellValue = worksheet.Cells[row, col].Value;

        //                    var property = typeof(D).GetProperties()[col - 1];
        //                    var convertedValue = Convert.ChangeType(cellValue, property.PropertyType);
        //                    property.SetValue(dataModel, convertedValue);
        //                }

        //                dataModels.Add(dataModel);
        //            }
        //        }
        //        else
        //            throw new Exception("Worksheet does not have any data.");
        //    }

        //    return dataModels;
        //}

        public void Write(string directory, string filePath, D item, char separator)
        {
            List<D> items = new() { item };

            Write(directory, filePath, items, separator);
        }

        public void Write(string directory, string filePath, List<D> items, char separator)
        {
            var utf8NoBom = new UTF8Encoding(false);

            Directory.CreateDirectory(directory);

            var lines = new List<string>();

            foreach (var item in items)
            {
                var values = typeof(D).GetProperties().Select(prop => prop.GetValue(item)?.ToString() ?? string.Empty);

                lines.Add(string.Join(separator, values));
            }

            File.WriteAllLines(filePath, lines, utf8NoBom);
        }
    }
}
