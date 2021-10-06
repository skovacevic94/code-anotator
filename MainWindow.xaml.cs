using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace code_anotator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string lastValidSourceId;

        private readonly RepositoryViewModel RepositoryVM;

        public MainWindow(string cacheDir)
        {
            InitializeComponent();

            RepositoryVM = new RepositoryViewModel(new RepositoryManager(cacheDir));
            DataContext = RepositoryVM;

            var visibilityList = new List<string>((string[])(Application.Current.FindResource("CommentClasses")));
            visibilityList.Insert(0, "ALL");
            classVisibilityCmb.ItemsSource = visibilityList;

            commentClassCmb.ItemsSource = new List<string>((string[])(Application.Current.FindResource("CommentClasses")));
        }

        private void OnRepoLoaded()
        {
            addNewButton.IsEnabled = true;
            removeButton.IsEnabled = true;
            saveButton.IsEnabled = true;
        }

        private void FindAllRepos()
        {
            RepositoryVM.SelectedRepository.CommentsAnnotation.Clear();
            var extensionList = (string[])App.Current.Resources["Excentions"];

            RepositoryVM.SelectedRepository.FindAllComments(extensionList);

            OnRepoLoaded();
        }

        private void CloneButton_Click(object sender, RoutedEventArgs e)
        {
            RepositoryVM.RepositoryManager.CloneRepository(repoUrlTextbox.Text.Trim());
        }

        private void FindAllButton_Click(object sender, RoutedEventArgs e)
        {
            FindAllRepos();
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CommentAnnotation annotation = ((CommentAnnotation)datagrid.SelectedItem);
            if (annotation == null || annotation.SourceFile == null)
            {
                if (lastValidSourceId == null) pickSelectionButton.IsEnabled = false;
                return;
            }

            try
            {
                avEditor.Document = new ICSharpCode.AvalonEdit.Document.TextDocument(File.ReadAllText(annotation.SourceFile));
                if (annotation.Comment.StartIndex.HasValue && annotation.Comment.EndIndex.HasValue)
                {
                    int startIndex = annotation.Comment.StartIndex.Value;
                    avEditor.SelectionStart = startIndex;
                    avEditor.SelectionLength = annotation.Comment.EndIndex.Value - startIndex + 1;
                }

                if (annotation.Comment.LineNumber.HasValue)
                {
                    double vertOffset = (avEditor.TextArea.TextView.DefaultLineHeight) * annotation.Comment.LineNumber.Value;
                    avEditor.ScrollToVerticalOffset(vertOffset - avEditor.TextArea.ActualHeight / 2);
                }

                lastValidSourceId = annotation.SourceId;
                pickSelectionButton.IsEnabled = true;
            }
            catch (Exception exc) when (
                exc is FileNotFoundException ||
                exc is DirectoryNotFoundException)
            {
                MessageBox.Show($"Can't find source file at {annotation.SourceFile}");
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            LoadRepoAnnotationsDialog inputDialog = new LoadRepoAnnotationsDialog();
            if (inputDialog.ShowDialog() == true)
            {
                RepositoryVM.SelectedRepository.CommentsAnnotation.Clear();
                RepositoryVM.SelectedRepository.LoadFromXLSX(inputDialog.SelectedPath, inputDialog.Mapping2XLS);

                OnRepoLoaded();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            RepositoryVM.SelectedRepository.SaveToXLSX();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedAnnotation = datagrid.SelectedItem;
            RepositoryVM.SelectedRepository.CommentsAnnotation.Remove((CommentAnnotation)selectedAnnotation);
        }

        private void AddNewButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = datagrid.SelectedIndex;
            if (selectedIndex == -1)
            {
                MessageBox.Show("Select row above which you want to insert new annotation");
                return;
            }

            RepositoryVM.SelectedRepository.CommentsAnnotation.Insert(selectedIndex, new CommentAnnotation() { Comment = new Comment() });
        }

        private void PickSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            int startIndex = avEditor.SelectionStart;
            int endIndex = avEditor.SelectionLength + startIndex;
            RepositoryVM.SelectedAnnotation.Comment.StartIndex = startIndex;
            RepositoryVM.SelectedAnnotation.Comment.EndIndex = endIndex;

            string code = avEditor.Document.Text;
            int lineNumber = 1;
            for (int i = 0; i < startIndex; ++i)
            {
                if (code[i] == '\n') lineNumber++;
            }
            RepositoryVM.SelectedAnnotation.Comment.LineNumber = lineNumber;
            RepositoryVM.SelectedAnnotation.Comment.Text = avEditor.SelectedText;

            if (lastValidSourceId != null)
            {
                string sourceId = lastValidSourceId;
                string name = RepositoryVM.SelectedRepository.Name;
                string sourceFile = Path.Combine(RepositoryVM.SelectedRepository.LocalPath, sourceId);
                RepositoryVM.SelectedAnnotation.SourceId = sourceId;
                RepositoryVM.SelectedAnnotation.SourceFile = sourceFile;
                RepositoryVM.SelectedAnnotation.CommentId = $"{name}\\{sourceId}\\{lineNumber}";
            }

            datagrid.Items.Refresh();
            var selectedIndex = datagrid.SelectedIndex;
            datagrid.UnselectAll();
            datagrid.SelectedIndex = selectedIndex;
        }

        private void CmbRepositories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            radioButtonGroup.IsEnabled = true;
            allFilterRadioButton.IsChecked = true;
            findAllButton.IsEnabled = true;
            loadButton.IsEnabled = true;
        }

        private bool LabeledAnnotationsPredicate(string commentClass)
        {
            if (string.IsNullOrEmpty(commentClass)) return false;
            if (classVisibilityCmb.SelectedIndex == 0)
            {
                string[] validClassValues = (string[])(Application.Current.FindResource("CommentClasses"));
                foreach (var validClassValue in validClassValues)
                {
                    if (commentClass.Trim().ToLower() == (validClassValue).Trim().ToLower())
                    {
                        return true;
                    }
                }
                return false;
            }
            else return commentClass.Trim().ToLower() == ((string)classVisibilityCmb.SelectedItem).Trim().ToLower();
        }

        private void ClassVisibilityCmb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (labeledFilterRadioButton.IsChecked.GetValueOrDefault())
            {
                RepositoryVM.CommentsAnnotationView.Filter = new Predicate<object>(c =>
                {
                    CommentAnnotation annotation = c as CommentAnnotation;

                    return LabeledAnnotationsPredicate(annotation.Class);
                });
            }
        }

        private void FilterRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (allFilterRadioButton.IsChecked.GetValueOrDefault())
            {
                RepositoryVM.CommentsAnnotationView.Filter = new Predicate<object>(c => true);
                addNewButton.IsEnabled = true;
            }
            else if (unlabeledFilterRadioButton.IsChecked.GetValueOrDefault())
            {
                RepositoryVM.CommentsAnnotationView.Filter = new Predicate<object>(c =>
                {
                    CommentAnnotation annotation = c as CommentAnnotation;
                    return string.IsNullOrEmpty(annotation.Class);
                });
                addNewButton.IsEnabled = false;
            }
            else if (labeledFilterRadioButton.IsChecked.GetValueOrDefault())
            {
                RepositoryVM.CommentsAnnotationView.Filter = new Predicate<object>(c =>
                {
                    CommentAnnotation annotation = c as CommentAnnotation;

                    return LabeledAnnotationsPredicate(annotation.Class);
                });
                addNewButton.IsEnabled = false;
            }
        }

        private void FormatTextButton_Click(object sender, RoutedEventArgs e)
        {
            if(!string.IsNullOrEmpty(bigCommentTextBox.Text))
            {
                bigCommentTextBox.Text = CommentParser.ProcessComment(bigCommentTextBox.Text);
            }
        }
    }
}
