using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Office.Interop.Excel;
using Microsoft.Practices.Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Action = System.Action;
using VBIDE = Microsoft.Vbe.Interop;
using Excel = Microsoft.Office.Interop.Excel;

namespace TDV.Office
{
    public class Core
    {

        #region ----- Fields

        private const string SoftwareMicrosoftOfficeExcelSecurity = @"Software\Microsoft\Office\{0}\Excel\Security";
        private const string AccessVBOM = "AccessVBOM";
        private readonly Object _missing = Missing.Value;

        #endregion

        #region ----- Properties

        private Workbooks Books { get; set; }
        private _Workbook Book { get; set; }
        private Microsoft.Office.Interop.Excel.Application Excel { get; set; }
        private VBIDE.VBComponent Module { get; set; }

        #endregion

        public static void SetExcelSecuritySettings(string keyExcel)
        {
            var subKey = String.Format(SoftwareMicrosoftOfficeExcelSecurity, keyExcel);
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(subKey, true))
            {
                if (key != null)
                {
                    if ((int)key.GetValue(AccessVBOM, 0) != 1)
                    {
                        //WriteLog(Properties.Resources.MainWindowsVM_SetExcelSecuritySettings_MacroSecurityON);
                        //LogMessageEvent.Instance.Publish("testé");

                        key.SetValue(AccessVBOM, 1);
                    }
                    //WriteLog(Properties.Resources.MainWindowsVM_SetExcelSecuritySettings_MacroSecurityOFF);
                    key.Close();
                }
                else
                {
                    //WriteLog(string.Format(Properties.Resources.MainWindowsVM_SetExcelSecuritySettings_MacroSecurityNotDetected, subKey));
                }
            }
        }

        public static KeyValuePair<string, string> GetOfficeVersion()
        {
            KeyValuePair<string, string> result;

            switch (new Excel.Application { Visible = false }.Version)
            {
                case "11.0":
                    result = new KeyValuePair<string, string>("11.0", "Office 2003");
                    break;
                case "12.0":
                    result = new KeyValuePair<string, string>("12.0", "Office 2007");
                    break;
                case "14.0":
                    result = new KeyValuePair<string, string>("14.0", "Office 2010");
                    break;
                case "15.0":
                    result = new KeyValuePair<string, string>("15.0", "Office 2013");
                    break;
                default:
                    result = new KeyValuePair<string, string>("??.?", "Office ????");
                    break;
            }
            return result;
        }
        private void LaunchOpenSDKExcel(string fullPath, string name)
        {
            using (SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Open(fullPath, true))
            {
                WorkbookPart workbookPart = spreadsheetDocument.WorkbookPart;
                //WorksheetPart worksheetPart =  workbookPart.Workbook.First();
                WorksheetPart worksheetPart = GetWorksheetPart(workbookPart, name);
                SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();
                string text;
                foreach (Cell c in sheetData.Elements<Row>().SelectMany(r => r.Elements<Cell>()))
                {
                    text = c.CellValue.Text;
                    Console.Write(text + " ");
                }
                Console.WriteLine();
                Console.ReadKey();
            }

        }

        public WorksheetPart GetWorksheetPart(WorkbookPart workbookPart, string sheetName)
        {
            string relId = workbookPart.Workbook.Descendants<Sheet>().First(s => sheetName.Equals(s.Name)).Id;
            return (WorksheetPart)workbookPart.GetPartById(relId);
        }

        public bool IsFileLocked(string filePath)
        {
            try
            {
                using (File.Open(filePath, FileMode.Open)) { }
            }
            catch (IOException e)
            {
                var errorCode = Marshal.GetHRForException(e) & ((1 << 16) - 1);

                return errorCode == 32 || errorCode == 33;
            }

            return false;
        }

        private void LaunchExcel(string fullPath, string name, IEnumerable<KeyValuePair<string, string>> macroRangeList, string destinationFolderPath)
        {

            try
            {
                //if (IsFileLocked(fullPath))
                //    return;

                //CurrentFilename = name;

                //WriteLog("Launch Excel in background");

                // Create an instance of Microsoft Excel
                Excel = new Excel.Application
                {
                    Visible = false,
                    DisplayAlerts = false,
                    ScreenUpdating = true,
                };

                // Define Workbooks
                Books = Excel.Workbooks;
                Book = null;

                //WriteLog(string.Format("Open Excel workbook {0}", name));
                Book = Books.Open(fullPath, _missing, _missing, _missing, _missing, _missing, _missing, _missing, _missing,
                    _missing, _missing, _missing, _missing, _missing, _missing);

                // Create a new VBA code module.
                Module = Book.VBProject.VBComponents.Add(VBIDE.vbext_ComponentType.vbext_ct_StdModule);

                var i = 0;
                foreach (var macroRange in macroRangeList)
                {
                    i++;
                    // VBA code for the dynamic macro that calls 
                    KeyValuePair<string, string> range = macroRange;
                    //WriteLog(string.Format("Add dynamically macro {0}", range.Key.Replace(" ", string.Empty)));

                    // Add the VBA macro to the new code module.
                    var macro = GetMacroCode(range.Key, range.Value, i, destinationFolderPath);
                    Module.CodeModule.AddFromString(macro);
                    //WriteLog("Run macro");
                    RunMacro(Excel, new Object[] { range.Key.Replace(" ", string.Empty) + i });
                }

                // Quit Excel and clean up.
                QuitExcel();

                //CurrentFilename = string.Empty;
            }
            catch (Exception)
            {
                //WriteLog("LaunchExcel Error");
            }

        }

        private void QuitExcel()
        {
            //WriteLog("Quit and clean up");

            if (Book != null)
            {
                Book.Close(false, _missing, _missing);
                Marshal.ReleaseComObject(Book);
                Book = null;
            }

            if (Books != null)
            {
                Marshal.ReleaseComObject(Books);
                Books = null;
            }

            if (Excel != null)
            {
                Excel.Quit();
                Marshal.ReleaseComObject(Excel);
                Excel = null;
            }

            //Garbage collection
            GC.Collect();
        }

        public string GetMacroCode(string macroName, string range, int i, string destinationFolderPath)
        {
            var moduleCode = new StringBuilder();
            var macroNameTrim = macroName.Replace(" ", string.Empty).Trim();
            moduleCode.AppendLine(string.Format("Public Sub {0}()", macroNameTrim + i));
            moduleCode.AppendLine();
            moduleCode.AppendLine(@"Dim strPath As String");
            moduleCode.AppendLine(@"Dim rng As Excel.Range");
            moduleCode.AppendLine(@"Dim cht As Excel.ChartObject");
            moduleCode.AppendLine();
            moduleCode.AppendLine(@"On Error GoTo ExitProc");
            moduleCode.AppendLine();
            moduleCode.AppendLine(@"Application.ScreenUpdating = True");
            moduleCode.AppendLine();
            moduleCode.AppendLine(string.Format(@"Sheets(""{0}"").Select", macroName));
            moduleCode.AppendLine(@"ActiveWindow.DisplayGridlines = False");
            moduleCode.AppendLine();
            moduleCode.AppendLine(string.Format("Range(\"{0}\").Select", range));
            moduleCode.AppendLine(@"Hi = Selection.Height");
            moduleCode.AppendLine(@"Wi = Selection.Width");
            moduleCode.AppendLine();
            moduleCode.AppendLine(@"Selection.CopyPicture xlScreen, xlPicture");
            moduleCode.AppendLine(@"Set cht = ActiveSheet.ChartObjects.Add(0, 0, Wi, Hi)");
            moduleCode.AppendLine();
            moduleCode.AppendLine(@"cht.Chart.Paste");
            moduleCode.AppendLine(string.Format("cht.Chart.Export \"{0}\" & \"{1}.bmp\"", string.Concat(destinationFolderPath, @"\"), string.Concat(macroNameTrim, i)));
            moduleCode.AppendLine();
            moduleCode.AppendLine(@"cht.Delete");
            moduleCode.AppendLine();
            moduleCode.AppendLine(@"ExitProc:");
            moduleCode.AppendLine(@"Application.ScreenUpdating = True");
            moduleCode.AppendLine(@"Set cht = Nothing");
            moduleCode.AppendLine(@"Set rng = Nothing");
            moduleCode.AppendLine();
            moduleCode.AppendLine(@"End Sub");
            return moduleCode.ToString();
        }

        private void RunMacro(object oApp, object[] oRunArgs)
        {
            try
            {
                oApp.GetType().InvokeMember("Run", BindingFlags.Default | BindingFlags.InvokeMethod, null, oApp, oRunArgs);
            }
            catch (Exception)
            {
                //WriteLog("Run macro failed");
            }
        }
    }
}
