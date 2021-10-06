using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace code_anotator
{
    /// <summary>
    /// Interaction logic for LoadRepoAnnotationsDialog.xaml
    /// </summary>
    public partial class LoadRepoAnnotationsDialog : Window
    {
        public string SelectedPath { get; set; }
        public Dictionary<CommentAnnotationProperties, int> Mapping2XLS { get; set; }

        private TextBox[] textBoxes = new TextBox[7];
        private Dictionary<CommentAnnotationProperties, int> defaultValues = new Dictionary<CommentAnnotationProperties, int>()
        {
            { CommentAnnotationProperties.SOURCE_ID, 4 },
            { CommentAnnotationProperties.LINE_NUMBER, 5 },
            { CommentAnnotationProperties.COMMENT_ID, 6 },
            { CommentAnnotationProperties.TEXT, 7 },
            { CommentAnnotationProperties.CLASS, 8 },
            { CommentAnnotationProperties.START_INDEX, 9 },
            { CommentAnnotationProperties.END_INDEX, 10 }
        };

        public LoadRepoAnnotationsDialog()
        {
            InitializeComponent();

            string[] enums = Enum.GetNames(typeof(CommentAnnotationProperties));
            var enumValues = Enum.GetValues(typeof(CommentAnnotationProperties));
            for (int i = 0; i < enums.Length; ++i )
            {
                int row = i % 4 ;
                int col = 2 * (i / 4);

                Label label = new Label()
                {
                    Content = enums[i],
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(label, row);
                Grid.SetColumn(label, col);

                textBoxes[i] = new TextBox()
                {
                    Name = $"{enums[i]}TxtBox",
                    Margin = new Thickness(5, 0, 0, 0),
                    Width = 40,
                    Height = 22,
                    Text = defaultValues[(CommentAnnotationProperties)(enumValues.GetValue(i))].ToString()
                };
                Grid.SetRow(textBoxes[i], row);
                Grid.SetColumn(textBoxes[i], col+1);

                mappingInputGrid.Children.Add(label);
                mappingInputGrid.Children.Add(textBoxes[i]);
            }
        }

        private void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.DefaultExt = ".xlsx";
            dlg.Filter = "Excel |*.xlsx";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                selectedFilePathTextBlock.Text = dlg.FileName;
                okBtn.IsEnabled = true;
                SelectedPath = dlg.FileName;
            }
        }

        private void okBtn_Click(object sender, RoutedEventArgs e)
        {
            string[] enums = Enum.GetNames(typeof(CommentAnnotationProperties));
            var enumValues = Enum.GetValues(typeof(CommentAnnotationProperties));
            Mapping2XLS = new Dictionary<CommentAnnotationProperties, int>();
            for (int i = 0; i < textBoxes.Length; ++i)
            {
                if(string.IsNullOrEmpty(textBoxes[i].Text.Trim()))
                {
                    MessageBox.Show($"{enums[i]} can't be empty");
                    return;
                }
                if(!int.TryParse(textBoxes[i].Text.Trim(), out int value) || value < 1)
                {
                    MessageBox.Show($"{enums[i]} is not valid positive integer");
                    return;
                }

                Mapping2XLS[(CommentAnnotationProperties)(enumValues.GetValue(i))] = value;
            }
            this.DialogResult = true;
        }
    }
}
