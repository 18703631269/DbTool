﻿using DbTool.Core;
using DbTool.Core.Entity;
using NPOI.SS.UserModel;
using WeihanLi.Npoi;

namespace DbTool
{
    public class ExcelDbDocExporter : IDbDocExporter
    {
        static ExcelDbDocExporter()
        {
            var settings = FluentSettings.For<ColumnEntity>();
            settings.HasExcelSetting(x =>
                {
                    x.Author = "DbTool";
                })
                .HasSheetSetting(x =>
                {
                    x.StartRowIndex = 2;
                    x.AutoColumnWidthEnabled = true;
                    x.RowAction = row =>
                    {
                        // apply header row style
                        if (row.RowNum == 1)
                        {
                            var headerStyle = row.Sheet.Workbook.CreateCellStyle();
                            headerStyle.Alignment = HorizontalAlignment.Center;
                            var headerFont = row.Sheet.Workbook.CreateFont();
                            headerFont.FontHeight = 180;
                            headerFont.IsBold = true;
                            headerFont.FontName = "微软雅黑";
                            headerStyle.SetFont(headerFont);
                            row.Cells.ForEach(c => c.CellStyle = headerStyle);
                        }
                    };
                    x.SheetAction = sheet =>
                    {
                        // set merged region
                        sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(0, 0, 0, 6));
                        // apply title style
                        var titleStyle = sheet.Workbook.CreateCellStyle();
                        titleStyle.Alignment = HorizontalAlignment.Left;
                        var font = sheet.Workbook.CreateFont();
                        font.FontHeight = 200;
                        font.FontName = "微软雅黑";
                        font.IsBold = true;
                        titleStyle.SetFont(font);
                        titleStyle.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.Black.Index;
                        titleStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.SeaGreen.Index;
                        titleStyle.FillPattern = FillPattern.SolidForeground;
                        sheet.GetRow(0).GetCell(0).CellStyle = titleStyle;
                    };
                })
                ;
            settings.Property(x => x.ColumnName)
                .HasColumnIndex(0)
                .HasColumnTitle("列名称")
                ;
            settings.Property(x => x.ColumnDescription)
                .HasColumnIndex(1)
                .HasColumnTitle("列描述")
                ;
            settings.Property(x => x.IsPrimaryKey)
                .HasColumnIndex(2)
                .HasColumnTitle("是否主键")
                .HasColumnOutputFormatter(x => x ? "Y" : "N")
                .HasColumnInputFormatter(x => "Y".Equals(x))
                ;
            settings.Property(x => x.IsNullable)
                .HasColumnIndex(3)
                .HasColumnTitle("是否可以为空")
                .HasColumnOutputFormatter(x => x ? "Y" : "N")
                .HasColumnInputFormatter(x => "Y".Equals(x))
                ;
            settings.Property(x => x.DataType)
                .HasColumnIndex(4)
                .HasColumnTitle("数据类型")
                .HasColumnOutputFormatter(x => x?.ToUpper())
                ;
            settings.Property(x => x.Size)
                .HasColumnIndex(5)
                .HasColumnTitle("数据长度")
                .HasColumnOutputFormatter(x => x > 0 && x < int.MaxValue ? x.ToString() : string.Empty)
                ;
            settings.Property(x => x.DefaultValue)
                .HasColumnIndex(6)
                .HasColumnTitle("默认值")
                .HasOutputFormatter((x, _) =>
                {
                    if (x?.DefaultValue != null)
                    {
                        return x.DefaultValue.ToString();
                    }
                    if (x?.IsPrimaryKey == true && x.DataType.ToUpper().Contains("INT"))
                    {
                        return "IDENTITY(1,1)";
                    }
                    return string.Empty;
                })
                ;
            settings.Property(x => x.NotEmptyDescription).Ignored();
        }

        public string ExportType => "Excel";

        public string FileExtension => ".xls";

        public byte[] Export(TableEntity[] tableInfo, string dbType)
        {
            var workbook = ExcelHelper.PrepareWorkbook(!FileExtension.EndsWith(".xls"));
            foreach (var tableEntity in tableInfo)
            {
                //Create Sheet
                var tempSheet = workbook.CreateSheet(tableEntity.TableName);
                //create title
                var titleRow = tempSheet.CreateRow(0);
                var titleCell = titleRow.CreateCell(0);
                titleCell.SetCellValue(tableEntity.TableDescription);

                // export list data to excel
                tempSheet.ImportData(tableEntity.Columns);
            }
            return workbook.ToExcelBytes();
        }
    }
}