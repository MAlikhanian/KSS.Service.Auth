using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Drawing;
using System.Reflection;

namespace KSS.Helper
{
    public class Excel<D>
    {
        public Excel()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }
        public List<D> Read(string filePath, int worksheetIndex, Func<D> createInstance)
        {
            List<D> dataModels = new();

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                ExcelWorksheet worksheet = package.Workbook.Worksheets[worksheetIndex];

                if (worksheet.Dimension != null)
                {
                    int rowCount = worksheet.Dimension.Rows;
                    int colCount = worksheet.Dimension.Columns;

                    for (int row = 1; row <= rowCount; row++)
                    {
                        D dataModel = createInstance();

                        for (int col = 1; col <= colCount; col++)
                        {
                            var cellValue = worksheet.Cells[row, col].Value;

                            var property = typeof(D).GetProperties()[col - 1];
                            var convertedValue = Convert.ChangeType(cellValue, property.PropertyType);
                            property.SetValue(dataModel, convertedValue);
                        }

                        dataModels.Add(dataModel);
                    }
                }
                else
                    throw new Exception("Worksheet does not have any data.");
            }

            return dataModels;
        }

        public void Write(string directory, string filePath, string worksheetName, D item, bool columnName = true, bool persianColumnName = false)
        {
            List<D> items = new() { item };

            Write(directory, filePath, worksheetName, items, columnName, persianColumnName);
        }
        public void Write(string directory, string filePath, string worksheetName, List<D> items, bool columnName = true, bool persianColumnName = false,
            byte startColumn = 1, bool showZero = false,
            bool customView = true, bool printSetting = false, byte? pageSettingType = null, string title = null, bool addRowNumber = false)
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            if (File.Exists(filePath))
                File.Delete(filePath);

            using var package = new ExcelPackage(new FileInfo(filePath));

            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add(worksheetName);

            var properties = typeof(D).GetProperties()
                          .Skip(startColumn - 1)
                          .ToArray();

            int extraCol = addRowNumber ? 1 : 0;

            if (columnName)
            {
                int colIndex = 1;

                if (addRowNumber)
                {
                    worksheet.Cells[1, colIndex].Value = "ردیف";
                    colIndex++;
                }

                for (int col = 0; col < properties.Length; col++)
                {
                    var property = properties[col];

                    var attribute = property.GetCustomAttribute<ColumnPersianName>();

                    if (attribute != null && persianColumnName)
                        worksheet.Cells[1, colIndex + col].Value = attribute.PersianName;
                    else
                        worksheet.Cells[1, colIndex + col].Value = property.Name;
                }
            }

            int excelRow = 1;

            for (int row = 0; row < items.Count; row++)
            {
                if (columnName && excelRow == 1)
                    excelRow++;

                D dataModel = items[row];

                int colIndex = 1;

                if (addRowNumber)
                {
                    worksheet.Cells[excelRow, colIndex].Value = row + 1;
                    colIndex++;
                }

                for (int col = 0; col < properties.Length; col++)
                {
                    var property = properties[col];

                    var attribute = property.GetCustomAttribute<ColumnType>();

                    if (property.GetValue(dataModel) != null)
                    {
                        if (!showZero && property.GetValue(dataModel).ToString() == "0")
                            worksheet.Cells[excelRow, colIndex + col].Value = string.Empty;
                        else
                            worksheet.Cells[excelRow, colIndex + col].Value = property.GetValue(dataModel);
                    }

                    bool hasType = property.IsDefined(typeof(ColumnType), false);

                    if (hasType && attribute.Type == "HyperLink")
                    {
                        var url = property.GetValue(dataModel)?.ToString();
                        if (!string.IsNullOrEmpty(url))
                        {
                            worksheet.Cells[excelRow, colIndex + col].Hyperlink = new ExcelHyperLink(url) { Display = "نمایش" };
                            worksheet.Cells[excelRow, colIndex + col].Style.Font.Color.SetColor(Color.Blue);
                        }
                    }
                }

                excelRow++;
            }

            int totalCols = properties.Length;

            if (addRowNumber) totalCols += 1;

            if (customView)
                ApplyCustomView(package,worksheet, excelRow - 1, totalCols, columnName, pageSettingType, title);
            if (printSetting)
                ApplyPrintSettings(package, worksheet, totalCols, items.Count, pageSettingType, title);

            package.Save();
        }

        private void ApplyCustomView(ExcelPackage package, ExcelWorksheet worksheet, int totalRows, int totalCols, bool hasHeader = true, byte? pageSettingType = null, string title = null)
        {
            switch (pageSettingType)
            {
                case null:

                    #region General Styling

                    worksheet.Cells.AutoFitColumns();
                    worksheet.Cells.Style.Numberformat.Format = "#,##0.00";
                    worksheet.View.RightToLeft = true;

                    for (int col = 1; col <= totalCols; col++)
                    {
                        worksheet.Cells[1, col].Style.Font.Bold = true;
                        worksheet.Cells[1, col].Style.Font.Size = 12;
                        worksheet.Cells[1, col].Style.Font.Color.SetColor(Color.Black);
                        worksheet.Cells[1, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[1, col].Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                    }

                    var range = worksheet.Cells[1, 1, totalRows, totalCols];

                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;

                    #endregion

                    break;

                case 8:

                    #region Salary print checklist

                    #region Fill statics cells

                    //first shift data to row: 4
                    var source = worksheet.Cells[1, 1, totalRows, totalCols];
                    var destination = worksheet.Cells[4, 1, 4 + totalRows - 1, totalCols];
                    source.Copy(destination);

                    worksheet.Cells[1, 1, 3, totalCols].Clear();

                    var parts = title.Split(',');

                    #region First row

                    worksheet.Cells["A1:B1"].Merge = true;
                    worksheet.Cells["A1"].Value = $"از تاریخ: {parts[0]}";

                    worksheet.Cells["C1"].Value = "";

                    worksheet.Cells["D1:E1"].Merge = true;
                    worksheet.Cells["D1"].Value = $"تا تاریخ: {parts[1]}";

                    worksheet.Cells["F1:Q1"].Merge = true;
                    worksheet.Cells["F1"].Value = "";

                    #endregion

                    #region Second row

                    worksheet.Cells["A2:B2"].Merge = true;
                    worksheet.Cells["A2"].Value = $"استخدام: {parts[2]}";

                    worksheet.Cells["C2"].Value = "";

                    worksheet.Cells["D2:F2"].Merge = true;
                    worksheet.Cells["D2"].Value = $"واحد سازمانی: {parts[3]}";

                    worksheet.Cells["G2:L2"].Merge = true;
                    worksheet.Cells["G2"].Value = $"{parts[4]}";

                    worksheet.Cells["M2:P2"].Merge = true;
                    worksheet.Cells["M2"].Value = $"شرکت: {parts[5]}";

                    worksheet.Cells["Q2:R2"].Merge = true;
                    worksheet.Cells["Q2"].Value = $"مرکز هزینه: {parts[6]}";

                    #endregion

                    #region Third  row

                    worksheet.Cells["A3:B3"].Merge = true;
                    worksheet.Cells["A3"].Value = "";

                    worksheet.Cells["C3:E3"].Merge = true;
                    worksheet.Cells["C3"].Value = "کارکرد";

                    worksheet.Cells["F3:K3"].Merge = true;
                    worksheet.Cells["F3"].Value = "مزایا";

                    worksheet.Cells["L3"].Value = "تعهدات کافرما";

                    worksheet.Cells["M3:P3"].Merge = true;
                    worksheet.Cells["M3"].Value = "کسور";

                    worksheet.Cells["Q3:R3"].Merge = true;
                    worksheet.Cells["Q3"].Value = "";

                    #endregion

                    #region Total row

                    worksheet.Cells[totalRows + 3 - 2, 1, totalRows + 3, 2].Merge = true;
                    worksheet.Cells[totalRows + 3 - 2, 1, totalRows + 3, 2].Value = "جمع";

                    #endregion

                    #endregion

                    #region Styling

                    worksheet.View.RightToLeft = true;

                    //1
                    worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    worksheet.Cells["A1"].Style.Border.Left.Style = ExcelBorderStyle.Thin;

                    worksheet.Cells["D1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;


                    worksheet.Cells["R1"].Style.Border.Right.Style = ExcelBorderStyle.Thin;

                    worksheet.Cells["A1:R1"].Style.Border.Top.Style = ExcelBorderStyle.Thin;

                    //2
                    worksheet.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    worksheet.Cells["A2"].Style.Border.Left.Style = ExcelBorderStyle.Thin;

                    worksheet.Cells["D2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                    worksheet.Cells["G2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells["G2"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                    worksheet.Cells["M2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                    worksheet.Cells["Q2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    worksheet.Cells["R2"].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    
                    //3
                    worksheet.Cells["A3:B3"].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    worksheet.Cells["A3"].Style.Border.Left.Style = ExcelBorderStyle.Thin;

                    worksheet.Cells["C3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells["C3"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells["C3:E3"].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    worksheet.Cells["C3:E3"].Style.Border.Left.Style = ExcelBorderStyle.Thin;

                    worksheet.Cells["F3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells["F3"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                    worksheet.Cells["F3:K3"].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    worksheet.Cells["F3:K3"].Style.Border.Left.Style = ExcelBorderStyle.Thin;

                    worksheet.Cells["L3"].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    worksheet.Cells["L3"].Style.Border.Left.Style = ExcelBorderStyle.Thin;

                    worksheet.Cells["M3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells["M3"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                    worksheet.Cells["M3:P3"].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    worksheet.Cells["M3:P3"].Style.Border.Left.Style = ExcelBorderStyle.Thin;

                    worksheet.Cells["Q3:R3"].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    worksheet.Cells["Q3:R3"].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    worksheet.Cells["Q3:R3"].Style.Border.Right.Style = ExcelBorderStyle.Thin;

                    //T
                    worksheet.Cells[totalRows + 3 - 2, 1, totalRows + 3, 2].Style.Font.Bold = true;

                    //3 top row general setting
                    using (range = worksheet.Cells[1, 1, 4, totalCols])
                    {
                        range.Style.Font.Size = 10;
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.White);
                    }

                    //content general setting
                    using (range = worksheet.Cells[4, 1, totalRows + 3, totalCols])
                    {
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Numberformat.Format = "#,##0";
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.White);
                    }

                    //Header setting
                    if (hasHeader)
                    {
                        for (int col = 1; col <= totalCols; col++)
                        {
                            var cell = worksheet.Cells[4, col];

                            if (cell.Value != null)
                            {
                                cell.Value = cell.Value.ToString().Replace("/", Environment.NewLine);
                                cell.Style.WrapText = true;
                            }

                            cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                            cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                            cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                            cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        }
                    }

                    //Draw border for every 3 rows after forth row
                    int startRow = hasHeader ? 5 : 2;
                    for (int row = startRow; row <= totalRows + 3; row += 3)
                    {
                        int endRow = Math.Min(row + 2, totalRows + 3);

                        for (int col = 1; col <= totalCols; col++)
                        {
                            // top border of block
                            worksheet.Cells[row, col].Style.Border.Top.Style = ExcelBorderStyle.Thin;

                            // bottom border of block
                            worksheet.Cells[endRow, col].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                            // left border only on first column
                            if (col == 1)
                            {
                                for (int r = row; r <= endRow; r++)
                                    worksheet.Cells[r, col].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                            }

                            // right border only on last column
                            if (col == totalCols)
                            {
                                for (int r = row; r <= endRow; r++)
                                    worksheet.Cells[r, col].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                            }
                        }
                    }

                    //Draw border for every column
                    for (int col = 1; col <= totalCols; col++)
                    {
                        worksheet.Cells[4, col, 4 + totalRows - 1, col].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        worksheet.Cells[4, col, 4 + totalRows - 1, col].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        worksheet.Column(col).AutoFit();
                    }

                    //Wrap text for first column
                    using (var col1 = worksheet.Cells[5, 2, totalRows, 2])
                    {
                        col1.Style.WrapText = true;
                    }

                    //Merg and set row number
                    for (int row = 5; row <= totalRows; row += 3)
                    {
                        worksheet.Cells[row, 1, row + 2, 1].Merge = true;
                        worksheet.Cells[row, 1, row + 2, 1].Value = (row - 5) / 3 + 1;
                    }

                    #endregion

                    #region Protect Sheet

                    worksheet.Protection.IsProtected = true;
                    worksheet.Protection.SetPassword("$P!2009#$%");

                    worksheet.Protection.AllowSelectLockedCells = true;
                    worksheet.Protection.AllowSelectUnlockedCells = true;
                    worksheet.Protection.AllowFormatCells = true;
                    worksheet.Protection.AllowEditObject = false;
                    worksheet.Protection.AllowEditScenarios = false; 

                    #endregion

                    #endregion

                    break;

                case 9:

                    #region insurance print format

                    #region Worksheet 1

                    #region Fill statics cells

                    //first shift data to row: 7
                    source = worksheet.Cells[1, 1, totalRows, totalCols];
                    destination = worksheet.Cells[7, 1, 7 + totalRows - 1, totalCols];
                    source.Copy(destination);

                    worksheet.Cells[1, 1, 6, totalCols].Clear();

                    parts = title.Split(',');

                    #region build first top row

                    worksheet.Cells["A2:S2"].Merge = true;
                    worksheet.Cells["A2"].Value = parts[0];

                    worksheet.Cells["A4:S4"].Merge = true;
                    worksheet.Cells["A4"].Value = $"لیست بیمه {parts[3]} سال {parts[4]}";

                    worksheet.Cells["A6:C6"].Merge = true;
                    worksheet.Cells["A6"].Value = $"شماره کارگاه: {parts[1]}";

                    worksheet.Cells["F6:I6"].Merge = true;
                    worksheet.Cells["F6"].Value = $"نام کارگاه: {parts[0]}";

                    worksheet.Cells["M6:R6"].Merge = true;
                    worksheet.Cells["M6"].Value = $"نشانی کارگاه: {parts[2]}";
                    
                    #endregion

                    #region build total cells

                    worksheet.Cells[totalRows + 7 - 1, 1, totalRows + 7 - 1, 10].Merge = true;
                    worksheet.Cells[totalRows + 7 - 1, 1, totalRows + 7 - 1, 10].Value = "مجموع";

                    worksheet.Cells[totalRows + 10, 13].Value = "بیمه بیکاری:";
                    worksheet.Cells[totalRows + 11, 13].Value = "بیمه سهم کارفرما:";
                    worksheet.Cells[totalRows + 12, 13].Value = "جمع کل:";

                    var a = worksheet.Cells[totalRows + 6, 18];
                    var b = worksheet.Cells[totalRows + 6, 17];

                    worksheet.Cells[totalRows + 10, 15].Value = a.Value;
                    worksheet.Cells[totalRows + 11, 15].Value = b.Value;
                    worksheet.Cells[totalRows + 12, 15].Formula = $"{a.Address}+{b.Address}";

                    #endregion

                    #endregion

                    #region Styling

                    worksheet.View.RightToLeft = true;

                    worksheet.Cells["A2"].Style.Font.Size = 14;
                    worksheet.Cells["A2"].Style.Font.Bold = true;
                    worksheet.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    worksheet.Cells["A4"].Style.Font.Size = 14;
                    worksheet.Cells["A4"].Style.Font.Bold = true;

                    worksheet.Cells["A4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells["A6"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    worksheet.Cells["F6"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    worksheet.Cells["M6"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    worksheet.Cells["A6:S6"].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    worksheet.Cells["A6:S6"].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    worksheet.Cells["A6"].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    worksheet.Cells["S6"].Style.Border.Right.Style = ExcelBorderStyle.Thin;

                    using (range = worksheet.Cells[6, 1, 6, totalCols])
                    {
                        range.Style.Font.Size = 10;
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.White);
                    }

                    worksheet.Cells[totalRows + 7 - 1, 1, totalRows + 7 - 1, 10].Style.Font.Bold = true;

                    worksheet.Cells[totalRows + 10, 13, totalRows + 12, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[totalRows + 10, 15, totalRows + 12, 15].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[totalRows + 10, 15, totalRows + 12, 15].Style.Numberformat.Format = "#,##0";

                    //Draw border and general setting for all content
                    using (range = worksheet.Cells[7, 1, totalRows + 6, totalCols])
                    {
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Size = 10;
                        range.Style.Numberformat.Format = "#,##0";
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.White);

                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    }

                    //Wrap text header
                    range = worksheet.Cells[7, 1, 7, totalCols];
                    range.Style.WrapText = true;
                    range.Style.Font.Bold = true;

                    //column autofit
                    for (int col = 1; col <= totalCols; col++)
                    {
                        worksheet.Column(col).AutoFit();
                    }

                    #endregion

                    #region Protect Sheet

                    worksheet.Protection.IsProtected = true;
                    worksheet.Protection.SetPassword("$P!2009#$%");

                    worksheet.Protection.AllowSelectLockedCells = true;
                    worksheet.Protection.AllowSelectUnlockedCells = true;
                    worksheet.Protection.AllowFormatCells = true;
                    worksheet.Protection.AllowEditObject = false;
                    worksheet.Protection.AllowEditScenarios = false;

                    #endregion

                    #endregion

                    #region Worksheet 2

                    // If sheet already exists, delete and recreate
                    var existing = package.Workbook.Worksheets["خلاصه وضعیت"];
                    if (existing != null)
                        package.Workbook.Worksheets.Delete(existing);

                    var worksheet2 = package.Workbook.Worksheets.Add("خلاصه وضعیت");

                    #region Fill statics cells

                    worksheet2.Cells["A1:B1"].Merge = true;
                    worksheet2.Cells["A1"].Value = parts[0];

                    worksheet2.Cells["A3:B3"].Merge = true;
                    worksheet2.Cells["A3"].Value = $"خلاصه وضعیت {parts[0]} به شماره کارگاه {parts[1]}";

                    worksheet2.Cells["A5:B5"].Merge = true;
                    worksheet2.Cells["A5"].Value = "سازمان تامین اجتماعی";

                    worksheet2.Cells["A7:B7"].Merge = true;
                    worksheet2.Cells["A7"].Value = $"به پیوست یک حلقه دیسکت مربوط به صورت دستمزد حقوق و مزایای ماهانه کارکنان این کارگاه در ماه {parts[3]} سال {parts[4]} که خلاصه وضعیت آن به شرح زیر است؛";

                    worksheet2.Cells["A8:B8"].Merge = true;
                    worksheet2.Cells["A8"].Value = $"به جهت منظور نمودن در سیستم مکانیزه و دریافت رسید تحویل می‌گردد.";

                    worksheet2.Cells["A9:B9"].Merge = true;
                    worksheet2.Cells["A9"].Value = $"تعداد نفرات:{totalRows - 2}";

                    worksheet2.Cells["A10"].Value = "جمع دستمزد و مزایای مشمول و غیر مشمول کسر حق بیمه";
                    worksheet2.Cells["B10"].Value = worksheet.Cells[totalRows + 6, 16].Value;

                    worksheet2.Cells["A11"].Value = "جمع دستمزد و مزایای مشمول کسر حق بیمه";
                    worksheet2.Cells["B11"].Value = worksheet.Cells[totalRows + 6, 15].Value;

                    worksheet2.Cells["A12"].Value = "جمع حق بیمه سهم کارکنان";
                    worksheet2.Cells["B12"].Value = worksheet.Cells[totalRows + 6, 19].Value;

                    worksheet2.Cells["A13"].Value = "جمع حق بیمه سهم کارفرما";
                    worksheet2.Cells["B13"].Value = worksheet.Cells[totalRows + 6, 17].Value;

                    worksheet2.Cells["A14"].Value = "جمع حق بیکاری";
                    worksheet2.Cells["B14"].Value = worksheet.Cells[totalRows + 6, 18].Value;

                    a = worksheet2.Cells["B12"];
                    b = worksheet2.Cells["B13"];
                    var c = worksheet2.Cells["B14"];

                    worksheet2.Cells["A15"].Value = "جمع کل حقوق بیمه";
                    worksheet2.Cells["B15"].Formula = $"{a.Address}+{b.Address}+{c.Address}";

                    worksheet2.Cells["A17:B17"].Merge = true;
                    worksheet2.Cells["A17"].Value = "ضمنا متعهد می‌گردد اطلاعات ذخیره شده در دیسکت‌های فوق عینا مربوط به مندرجات به شرح فوق می‌باشد.";

                    #endregion

                    #region Styling

                    worksheet2.View.RightToLeft = true;

                    worksheet2.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet2.Cells["A1"].Style.Font.Bold = true;
                    worksheet2.Cells["A1"].Style.Font.Size = 13;

                    worksheet2.Cells["A3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    worksheet2.Cells["A3"].Style.Font.Size = 12;

                    worksheet2.Cells["A5"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet2.Cells["A5"].Style.Font.Bold = true;
                    worksheet2.Cells["A5"].Style.Font.Size = 12;

                    worksheet2.Cells["A7"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    worksheet2.Cells["A7"].Style.Font.Size = 12;

                    worksheet2.Cells["A8"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    worksheet2.Cells["A8"].Style.Font.Size = 12;

                    worksheet2.Cells["A9"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    worksheet2.Cells["A9"].Style.Font.Size = 12;

                    worksheet2.Cells["A9:A14"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    worksheet2.Cells["A9:A14"].Style.Font.Size = 12;

                    worksheet2.Cells["B10:B15"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet2.Cells["B10:B15"].Style.Font.Size = 12;
                    worksheet2.Cells["B10:B15"].Style.Numberformat.Format = "#,##0";

                    worksheet2.Cells["A17"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    worksheet2.Cells["A17"].Style.Font.Size = 12;

                    worksheet2.Cells["A10:B15"].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    worksheet2.Cells["A10:B15"].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    worksheet2.Cells["A10:B15"].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    worksheet2.Cells["A10:B15"].Style.Border.Right.Style = ExcelBorderStyle.Thin;

                    worksheet2.Cells["A10:B10"].Style.Border.Top.Style = ExcelBorderStyle.Medium;
                    worksheet2.Cells["A15:B15"].Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                    worksheet2.Cells["A10:A15"].Style.Border.Left.Style = ExcelBorderStyle.Medium;
                    worksheet2.Cells["B10:B15"].Style.Border.Right.Style = ExcelBorderStyle.Medium;

                    worksheet2.Column(1).Width = 83;
                    worksheet2.Column(2).Width = 28;

                    #endregion

                    #region Protect Sheet

                    worksheet2.Protection.IsProtected = true;
                    worksheet2.Protection.SetPassword("$P!2009#$%");

                    worksheet2.Protection.AllowSelectLockedCells = true;
                    worksheet2.Protection.AllowSelectUnlockedCells = true;
                    worksheet2.Protection.AllowFormatCells = true;
                    worksheet2.Protection.AllowEditObject = false;
                    worksheet2.Protection.AllowEditScenarios = false;

                    #endregion

                    #endregion

                    #endregion

                    break;
            }
        }
        private void ApplyPrintSettings(ExcelPackage package, ExcelWorksheet worksheet, int totalCols, int totalRows, byte? pageSettingType = null, string headerTitle = null)
        {
            switch (pageSettingType)
            {
                case null:

                    #region General

                    #endregion

                    break;

                case 8:

                    #region Salary print checklist

                    //Repeat first 4 rows
                    worksheet.PrinterSettings.RepeatRows = new ExcelAddress("1:4");

                    //Landscape
                    worksheet.PrinterSettings.Orientation = eOrientation.Landscape;

                    //Fit all columns in one page width
                    worksheet.PrinterSettings.FitToWidth = 1;
                    worksheet.PrinterSettings.FitToHeight = 0;

                    //Scale to fit all columns
                    worksheet.PrinterSettings.FitToPage = true;

                    //Remove all headers/footers
                    worksheet.HeaderFooter.OddHeader.LeftAlignedText = "";
                    worksheet.HeaderFooter.OddHeader.CenteredText = "";
                    worksheet.HeaderFooter.OddHeader.RightAlignedText = "";

                    //Simple footer: "Page X of Y"
                    worksheet.HeaderFooter.OddFooter.LeftAlignedText = "";
                    worksheet.HeaderFooter.OddFooter.CenteredText = "Page &P of &N";
                    worksheet.HeaderFooter.OddFooter.RightAlignedText = "";

                    //Set rows count in every page
                    int startRow = 4;
                    for (int row = startRow + 45; row <= totalRows; row += 45)
                    {
                        worksheet.Row(row).PageBreak = true;
                    }

                    #endregion

                    break;

                case 9:

                    #region insurance print 

                    #region Worksheet 1

                    //Repeat first 4 rows
                    worksheet.PrinterSettings.RepeatRows = new ExcelAddress("1:7");

                    //Landscape
                    worksheet.PrinterSettings.Orientation = eOrientation.Landscape;

                    //Fit all columns in one page width
                    worksheet.PrinterSettings.FitToWidth = 1;
                    worksheet.PrinterSettings.FitToHeight = 0;

                    //Scale to fit all columns
                    worksheet.PrinterSettings.FitToPage = true;

                    //Remove all headers/footers
                    worksheet.HeaderFooter.OddHeader.LeftAlignedText = "";
                    worksheet.HeaderFooter.OddHeader.CenteredText = "";
                    worksheet.HeaderFooter.OddHeader.RightAlignedText = "";

                    //Simple footer: "Page X of Y"
                    worksheet.HeaderFooter.OddFooter.LeftAlignedText = "";
                    worksheet.HeaderFooter.OddFooter.CenteredText = "Page &P of &N";
                    worksheet.HeaderFooter.OddFooter.RightAlignedText = "";

                    //Set rows count in every page
                    startRow = 2;
                    for (int row = startRow + 15; row <= totalRows; row += 15)
                    {
                        worksheet.Row(row).PageBreak = true;
                    }

                    #endregion

                    #region Worksheet 2

                    var worksheet2 = package.Workbook.Worksheets["خلاصه وضعیت"];

                    //Portrait
                    worksheet2.PrinterSettings.Orientation = eOrientation.Portrait;

                    //Fit all columns in one page width
                    worksheet2.PrinterSettings.FitToWidth = 1;
                    worksheet2.PrinterSettings.FitToHeight = 0;

                    //Scale to fit all columns
                    worksheet2.PrinterSettings.FitToPage = true;

                    //Remove all headers/footers
                    worksheet2.HeaderFooter.OddHeader.LeftAlignedText = "";
                    worksheet2.HeaderFooter.OddHeader.CenteredText = "";
                    worksheet2.HeaderFooter.OddHeader.RightAlignedText = "";

                    //Simple footer: "Page X of Y"
                    worksheet2.HeaderFooter.OddFooter.LeftAlignedText = "";
                    worksheet2.HeaderFooter.OddFooter.CenteredText = "";
                    worksheet2.HeaderFooter.OddFooter.RightAlignedText = "";

                    #endregion

                    #endregion

                    break;
            }
        }

        public static void WriteMatrixToExcel(List<List<D>> matrix, string directory, string filePath, string worksheetName)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            if (File.Exists(filePath))
                File.Delete(filePath);

            using var package = new ExcelPackage(new FileInfo(filePath));
            var worksheet = package.Workbook.Worksheets.Add(worksheetName);

            worksheet.View.RightToLeft = true;

            for (int row = 0; row < matrix.Count; row++)
            {
                var currentRow = matrix[row];
                for (int col = 0; col < currentRow.Count; col++)
                {
                    worksheet.Cells[row + 1, col + 1].Value = currentRow[col];

                    if (row == 0)
                    {
                        worksheet.Cells[row + 1, col + 1].Style.Font.Bold = true;
                        worksheet.Cells[row + 1, col + 1].Style.Font.Size = 12;
                        worksheet.Cells[row + 1, col + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[row + 1, col + 1].Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                    }
                }
            }

            worksheet.Cells.AutoFitColumns();

            package.Save();
        }
    }
}
