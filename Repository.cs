using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using Excel = Microsoft.Office.Interop.Excel;

namespace code_anotator
{
    public class Repository
    {
        public string LocalPath { get; }
        public string Url { get; }
        public string Name { get; }

        public ObservableCollection<CommentAnnotation> CommentsAnnotation { get; set; }

        public Repository(string localPath)
        {
            LocalPath = localPath;
            Url = new LibGit2Sharp.Repository(localPath).Network.Remotes.First().Url;
            Name = Url.Split('/').Where(x => !string.IsNullOrWhiteSpace(x)).LastOrDefault();

            CommentsAnnotation = new ObservableCollection<CommentAnnotation>();
        }

        public void FindAllComments(string[] extensions)
        {
            foreach (string file in Directory.EnumerateFiles(LocalPath, "*.*", SearchOption.AllDirectories))
            {
                foreach(string ext in extensions)
                {
                    if(Path.GetExtension(file) == ext)
                    {
                        string code = File.ReadAllText(file);

                        List<Comment> parsedComments = CommentParser.Parse(code);

                        foreach(Comment comment in parsedComments)
                        {
                            string sourceId = Path.GetRelativePath(LocalPath, file);
                            Application.Current.Dispatcher.BeginInvoke(new Action(() => CommentsAnnotation.Add(new CommentAnnotation()
                            {
                                SourceFile = file,
                                SourceId = sourceId,
                                Comment = comment,
                                CommentId = $"{Name}\\{sourceId}\\{comment.LineNumber}",
                            })));
                        }
                    }
                }
            }
        }

        public void LoadFromXLSX(string path, Dictionary<CommentAnnotationProperties, int> xlsMapping)
        {
            Excel.Application xlApp;
            Excel.Workbook xlWorkBook;
            Excel.Worksheet xlWorkSheet;
            Excel.Range range;

            xlApp = new Excel.Application();
            xlWorkBook = xlApp.Workbooks.Open(path, 0, true, 5, "", "", true, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
            xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);

            range = xlWorkSheet.UsedRange;

            Array data = (Array)(range.Cells.Value2);

            for (int rowIdx = 2; rowIdx <= range.Rows.Count; rowIdx++)
            {

                int? lineNumber = null;
                int? startIndex = null;
                int? endIndex = null;

                Func<CommentAnnotationProperties, string> getValue = (CommentAnnotationProperties property) =>
                {
                    int colIdx = xlsMapping[property];
                    return Convert.ToString(data.GetValue(new int[] { rowIdx, colIdx }));
                };
                if (int.TryParse(getValue(CommentAnnotationProperties.LINE_NUMBER), out int lineNumberParsed))
                {
                    lineNumber = lineNumberParsed;
                }
                if(xlsMapping.ContainsKey(CommentAnnotationProperties.START_INDEX) && 
                    int.TryParse(getValue(CommentAnnotationProperties.START_INDEX), out int startIndexParsed))
                {
                    startIndex = startIndexParsed;
                }
                if (xlsMapping.ContainsKey(CommentAnnotationProperties.END_INDEX) && 
                    int.TryParse(getValue(CommentAnnotationProperties.END_INDEX), out int endIndexParsed))
                {
                    endIndex = endIndexParsed;
                }

                Comment comment = new Comment()
                {
                    LineNumber = lineNumber,
                    StartIndex = startIndex,
                    EndIndex = endIndex,
                    Text = getValue(CommentAnnotationProperties.TEXT)
                };

                CommentAnnotation annotation = new CommentAnnotation()
                {
                    SourceId = getValue(CommentAnnotationProperties.SOURCE_ID),
                    CommentId = getValue(CommentAnnotationProperties.COMMENT_ID),
                    Class = getValue(CommentAnnotationProperties.CLASS),
                    Comment = comment
                };
                annotation.SourceFile = Path.Combine(LocalPath, annotation.SourceId);

                CommentsAnnotation.Add(annotation);
            }

            xlWorkBook.Close(true, null, null);
            xlApp.Quit();

            Marshal.ReleaseComObject(xlWorkSheet);
            Marshal.ReleaseComObject(xlWorkBook);
            Marshal.ReleaseComObject(xlApp);
        }

        public void SaveToXLSX()
        {
            Excel.Application xlApp = new Microsoft.Office.Interop.Excel.Application();

            if (xlApp == null)
            {
                throw new InvalidOperationException("Excel is not properly installed!!");
            }

            Excel.Workbook xlWorkBook;
            Excel.Worksheet xlWorkSheet;

            xlWorkBook = xlApp.Workbooks.Add(Missing.Value);
            xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);

            object[,] data = new object[CommentsAnnotation.Count + 1, 10];

            data[0, 0] = "NaturalLanguageID";
            data[0, 1] = "ProgrammingLanguageName";
            data[0, 2] = "RepoID";
            data[0, 3] = "SourceID";
            data[0, 4] = "LineNumber";
            data[0, 5] = "CommentID";
            data[0, 6] = "Text";
            data[0, 7] = "Class";
            data[0, 8] = "StartIndex";
            data[0, 9] = "EndIndex";
            data[0, 10] = "CommentURL";
            for (int rowIdx = 1; rowIdx <= CommentsAnnotation.Count; rowIdx++)
            {
                var commentAnnotation = CommentsAnnotation[rowIdx - 1];
                data[rowIdx, 0] = Application.Current.FindResource("Language").ToString();
                data[rowIdx, 1] = Convert.ToString(Application.Current.FindResource("ProgrammingLanguage"));
                data[rowIdx, 2] = Name;
                data[rowIdx, 3] = Convert.ToString(commentAnnotation.SourceId).Replace('\\', '/');
                data[rowIdx, 4] = commentAnnotation.Comment.LineNumber;
                data[rowIdx, 5] = Convert.ToString(commentAnnotation.CommentId).Replace('\\', '/');
                data[rowIdx, 6] = Convert.ToString(commentAnnotation.Comment.Text);
                data[rowIdx, 7] = Convert.ToString(commentAnnotation.Class);
                data[rowIdx, 8] = commentAnnotation.Comment.StartIndex;
                data[rowIdx, 9] = commentAnnotation.Comment.EndIndex;
            }

            Excel.Range range = (Excel.Range) xlWorkSheet.Cells[1, 1];
            range = range.Resize[CommentsAnnotation.Count, 10];
            range.Value2 = data;
            range.EntireColumn.AutoFit();

            string path = Path.Combine(Path.GetDirectoryName(LocalPath), $"{Name.Replace(".", "_")}_{DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss")}");

            xlWorkBook.SaveAs(path, Excel.XlFileFormat.xlOpenXMLWorkbook, Missing.Value,
                Missing.Value, false, false, Excel.XlSaveAsAccessMode.xlNoChange,
                Excel.XlSaveConflictResolution.xlUserResolution, true,
                Missing.Value, Missing.Value, Missing.Value);
            xlWorkBook.Close(true, Missing.Value, Missing.Value);
            xlApp.Quit();

            Marshal.ReleaseComObject(xlWorkSheet);
            Marshal.ReleaseComObject(xlWorkBook);
            Marshal.ReleaseComObject(xlApp);
        }
    }
}
