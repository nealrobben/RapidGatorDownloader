using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Configuration;
using System.Collections.ObjectModel;

namespace RapidGatorDownload
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WebClient client;
        private UserInfo userInfo;

        private string userName = string.Empty;
        private string password = string.Empty;

        public ObservableCollection<DownloadItem> downloadsList = new ObservableCollection<DownloadItem>();

        public MainWindow()
        {
            InitializeComponent();

            userName = new SecureString().Unprotect(ReadSetting("UserName"));
            password = new SecureString().Unprotect(ReadSetting("Password"));

            var test = new DownloadItem() { DownloadName = "Test", Progress = 20 };
            var test2 = new DownloadItem() { DownloadName = "Test2", Progress = 30 };
            var test3 = new DownloadItem() { DownloadName = "Test3", Progress = 35 };

            DownloadsGrid.ItemsSource = downloadsList;

            downloadsList.Add(test);
            downloadsList.Add(test2);
            downloadsList.Add(test3);
            
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            DownloadFile();
        }

        private string ReadSetting(string key)
        {
            var appSettings = ConfigurationManager.AppSettings;
            return appSettings[key] ?? "Not Found";
        }

        private void SaveSetting(string key, string value)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                if (settings[key] == null)
                {
                    settings.Add(key, value);
                }
                else
                {
                    settings[key].Value = value;
                }
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error writing app settings");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (client != null && client.IsBusy)
            {
                client.CancelAsync();
            }
        }

        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                SendButton_Click(null, null);
            }
        }

        /// <summary>
        /// Log into rapidgator. Send an HTTPS Post request with the login information. Use the userid (and sessionID?) returned to make
        /// a normal HTTP Get request to verify that you are logged in.
        /// </summary>
        private void DownloadFile()
        {
            // http://rapidgator.net/file/831b3e77beb3071abe71c9d50225cc6d/The_Flash_Annual_01_(1959).cbr.html
            string fileUrl = @"http://rapidgator.net/file/2167549b9f220c67307580f9acfc44d5/18630NN.rar.html";

            if (!string.IsNullOrWhiteSpace(input.Text))
            {
                fileUrl = input.Text;
            }

            if (userInfo == null)
            {
                userInfo = GetUserInfo();
            }

            DownloadInfo info = GetDownloadInfoFromURL(fileUrl, userInfo);

            client = new WebClient();
            client.Headers.Add(HttpRequestHeader.Cookie, "user__=" + userInfo.User + ";" + "PHPSESSID=" + userInfo.PHPSESSIONID);

            try
            {
                //Forward slash in path
                if (!File.Exists("I:/" + info.FileName))
                {
                    client.DownloadFileCompleted += Client_DownloadFileCompleted;
                    client.DownloadProgressChanged += Client_DownloadProgressChanged;
                    client.DownloadFileAsync(new Uri(info.Link), "I:/" + info.FileName);
                    Console.WriteLine("File downloaded");
                }
                else
                {
                    Console.WriteLine("File already exists");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                MessageBox.Show("Download cancelled");
                feedbackLabel.Content = "Download canceled";
            }
            else
            {
                feedbackLabel.Content = $"Download completed";
            }
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progress.Value = e.ProgressPercentage;
            feedbackLabel.Content = $"{e.ProgressPercentage}% ({e.BytesReceived / 1000000}MB/{e.TotalBytesToReceive / 1000000}MB) ";
        }

        private UserInfo GetUserInfo()
        {
            HttpWebRequest loginRequest = (HttpWebRequest)WebRequest.Create("https://rapidgator.net/auth/login");
            loginRequest.UserAgent = @"Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";
            loginRequest.Method = "Post";
            loginRequest.Accept = @"text/html, application/xhtml+xml, */*";
            loginRequest.ContentType = @"application/x-www-form-urlencoded";
            loginRequest.Referer = @"https://rapidgator.net/auth/login";
            loginRequest.AllowAutoRedirect = false;

            //Write Post-info to body of request
            StreamWriter requestWriter = new StreamWriter(loginRequest.GetRequestStream());
            requestWriter.Write($"LoginForm%5Bemail%5D={userName}&LoginForm%5Bpassword%5D={password}&LoginForm%5BrememberMe%5D=0&LoginForm%5BverifyCode%5D=");
            requestWriter.Close();

            var loginResponse = (HttpWebResponse)loginRequest.GetResponse();

            //PHPSESSID=61f3sh50tf9uqh1queu4s46tb5; path=/,user__=2.0%7CaFzVaLpT6%2BHAC0A2TjEF3jeRiAAUu9bhLO9R9ZRHD5yfwU%2FX21gCkClIdEdLEp1t27YOCQRdhZ%2BTKGBHgoXyGYS%2B9%2FJuSRNSn81z9xFP186W2VUgYt%2FVBk8PegsF4hOT%7C1439061732%7Cef1262242f8d187feb0cdd9510a3e0c1f68d4f70; path=/; domain=.rapidgator.net
            //PHPSESSID, user__
            var cookieHeader = loginResponse.Headers["Set-Cookie"];

            string phpSessionID = cookieHeader.Substring(cookieHeader.IndexOf("PHPSESSID"), 36);
            string phpSessionIDValue = phpSessionID.Substring(phpSessionID.IndexOf("=") + 1);

            string userID = cookieHeader.Substring(cookieHeader.IndexOf("user__"), 209);
            string userIDValue = userID.Substring(userID.IndexOf("=") + 1);

            return new UserInfo() { PHPSESSIONID = phpSessionIDValue, User = userIDValue };
        }

        /// <summary>
        /// Perform a request to get the download page for a certain url and then extract the exact downloadlink from the HTML
        /// </summary>
        private DownloadInfo GetDownloadInfoFromURL(string url, UserInfo userInfo)
        {
            //http://rapidgator.net/file/2167549b9f220c67307580f9acfc44d5/18630NN.rar.html

            HttpWebRequest normalRequest = (HttpWebRequest)WebRequest.Create(url.Trim());
            normalRequest.UserAgent = @"Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";
            normalRequest.Method = "Get";
            normalRequest.Accept = @"text/html, application/xhtml+xml, */*";
            normalRequest.ContentType = @"application/x-www-form-urlencoded";
            normalRequest.AllowAutoRedirect = false;
            normalRequest.CookieContainer = new CookieContainer();

            //SessionID not necessary? (Not to get the download link, but necessary when doing the actual download I think)
            //normalRequest.CookieContainer.Add(new Cookie("PHPSESSID", phpSessionIDValue,"/", ".rapidgator.net"));
            normalRequest.CookieContainer.Add(new Cookie("user__", userInfo.User, "/", ".rapidgator.net"));

            var httpresp2 = (HttpWebResponse)normalRequest.GetResponse();
            StreamReader reader2 = new StreamReader(httpresp2.GetResponseStream());
            string responseContent = reader2.ReadToEnd();

            return ExtractDownloadInfoFromHTML(responseContent);
        }

        /// <summary>
        /// Extract the exact downloadlink from the HTML code
        /// </summary>
        /// <param name="htmlPage"></param>
        /// <returns></returns>
        private DownloadInfo ExtractDownloadInfoFromHTML(string htmlPage)
        {
            string fileUrlRegEx = string.Empty;
            string fileNameRegEx = string.Empty;

            MatchCollection m1 = Regex.Matches(htmlPage, @"(<a.*?>.*?</a>)", RegexOptions.Singleline);

            foreach (Match m in m1)
            {
                string value = m.Groups[1].Value;
                Match m2 = Regex.Match(value, @"href=\""(.*?)\""", RegexOptions.Singleline);

                if (m2.Success && m2.Groups[1].Value.Contains("session_id"))
                {
                    fileUrlRegEx = m2.Groups[1].Value;
                    Console.WriteLine("HRef=" + fileUrlRegEx);

                    string t = Regex.Replace(value, @"\s*<.*?>\s*", "", RegexOptions.Singleline);
                    fileNameRegEx = t;
                    Console.WriteLine("Text: " + t);
                }
            }

            return new DownloadInfo() { Link = fileUrlRegEx, FileName = fileNameRegEx };
        }

        private class DownloadInfo
        {
            public string Link { get; set; }
            public string FileName { get; set; }
        }

        private class UserInfo
        {
            public string PHPSESSIONID { get; set; }
            public string User { get; set; }
        }

        public class DownloadItem
        {
            public string DownloadName { get; set; }
            public int Progress { get; set; }
        }

        private void mnuSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new Settings();
            settingsWindow.Owner = this;

            settingsWindow.UserName = userName;
            settingsWindow.Password = password;

            var result = settingsWindow.ShowDialog();

            if(result == true)
            {
            userName = settingsWindow.UserName;
            password = settingsWindow.Password;

            string secureUserName = new SecureString().Protect(userName);
            string securePassword = new SecureString().Protect(password);

            SaveSetting("UserName", secureUserName);
            SaveSetting("Password", securePassword);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(client != null)
            {
                client.Dispose();
            }
        }

        private void mnuExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
