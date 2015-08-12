﻿using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Configuration;

namespace RapidGatorDownload
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WebClient client;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            ReadSetting("UserName");
            SaveSetting("UserName", "Neal");

            //DownloadWithHttpRequest();
        }

        private void ReadSetting(string key)
        {
            var appSettings = ConfigurationManager.AppSettings;
            string result = appSettings[key] ?? "Not Found";
            Console.WriteLine(result);
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
            if(client != null && client.IsBusy)
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
        private void DownloadWithHttpRequest()
        {
            string fileUrl = @"http://rapidgator.net/file/2167549b9f220c67307580f9acfc44d5/18630NN.rar.html";

            UserInfo userInfo = GetUserInfo();
            DownloadInfo info = GetDownloadInfoFromURL(fileUrl, userInfo);

            client = new WebClient();
            client.Headers.Add(HttpRequestHeader.Cookie, "user__=" + userInfo.User + ";" + "PHPSESSID=" + userInfo.PHPSESSIONID);

            try
            {
                //Forward slash in path
                if (!File.Exists("I:/" + info.FileName))
                {
                    client.DownloadDataCompleted += Client_DownloadDataCompleted;
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

        private void Client_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            //This event doesn't fire when the download async is cancelled for some reason
            if(e.Cancelled)
            {
                MessageBox.Show("Download cancelled");
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

        private DownloadInfo GetDownloadInfoFromURL(string url, UserInfo userInfo)
        {
            //http://rapidgator.net/file/2167549b9f220c67307580f9acfc44d5/18630NN.rar.html

            HttpWebRequest normalRequest = (HttpWebRequest)WebRequest.Create(url);
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
    }
}