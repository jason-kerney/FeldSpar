namespace ViewModel
{
    public class TestDetailModel : PropertyNotifyBase
    {
        private string name;
        private TestStatus status;
        private string failDetail;

        public string Name
        {
            get { return name; }
            set
            {
                if (name == value)
                {
                    return;
                }
                name = value;
                OnPropertyChanged();
            }
        }

        public TestStatus Status
        {
            get { return status; }
            set
            {
                if (status == value)
                {
                    return;
                }
                
                status = value;
                OnPropertyChanged();
            }
        }

        public string FailDetail
        {
            get { return failDetail; }
            set
            {
                if (failDetail == value)
                {
                    return;
                }

                failDetail = value;
                OnPropertyChanged();
            }
        }
    }
}