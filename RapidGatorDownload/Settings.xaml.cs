using System.Windows;

namespace RapidGatorDownload
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        public string UserName
        {
            get
            {
                return txtUserName.Text;
            }
            set
            {
                txtUserName.Text = value;
            }
        }

        public string Password
        {
            get
            {
                return txtPassword.Password;
            }
            set
            {
                txtPassword.Password = value;
            }
        }

        public Settings()
        {
            InitializeComponent();
        }
    }
}
