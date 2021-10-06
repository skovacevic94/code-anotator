using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace code_anotator
{
    public class RepositoryViewModel : INotifyPropertyChanged
    {
        public RepositoryManager RepositoryManager { get; set; }

        private Repository selectedRepository;
        public Repository SelectedRepository
        {
            get => selectedRepository;
            set
            {
                if(selectedRepository != value)
                {
                    selectedRepository = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CommentsAnnotationView"));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedRepository"));
                }
            }
        }

        private CommentAnnotation selectedAnnotation;
        public CommentAnnotation SelectedAnnotation 
        {
            get => selectedAnnotation; 
            set
            {
                if(selectedAnnotation != value)
                {
                    selectedAnnotation = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedAnnotation"));
                }
            }
        }

        public int LabeledCount
        {
            get
            {
                if (SelectedRepository != null)
                    return SelectedRepository.CommentsAnnotation.Where(c => !string.IsNullOrEmpty(c.Class)).Count();
                return 0;
            }
        }

        public ICollectionView CommentsAnnotationView
        {
            get
            {
                if (SelectedRepository != null)
                {
                    return CollectionViewSource.GetDefaultView(SelectedRepository.CommentsAnnotation);
                }
                else return CollectionViewSource.GetDefaultView(new ObservableCollection<CommentAnnotation>());
            }
        }

        public RepositoryViewModel(RepositoryManager repositoryManager)
        {
            RepositoryManager = repositoryManager;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
