using System;
using System.IO;
using System.Net;
using System.Windows;

public class DownloadItem : DependencyObject
{
    private UserInfo usrInfo;
    private DownloadInfo dlInfo;
    private WebClient client;

    public string DownloadName { get; set; }

    public int Progress
    {
        get { return (int)GetValue(ProgressProperty); }
        set { SetValue(ProgressProperty, value); }
    }

    public string ProgressFeedback
    {
        get { return (string)GetValue(ProgressFeedbackProperty); }
        set { SetValue(ProgressFeedbackProperty, value); }
    }

    public DownloadItem(UserInfo usrInfo, DownloadInfo dlInfo)
    {
        this.usrInfo = usrInfo;
        this.dlInfo = dlInfo;

        DownloadName = dlInfo.FileName;
        Progress = 0;
        ProgressFeedback = "";
    }

    public void DownLoad()
    {
        client = new WebClient();
        client.Headers.Add(HttpRequestHeader.Cookie, "user__=" + usrInfo.User + ";" + "PHPSESSID=" + usrInfo.PHPSESSIONID);

        if (!File.Exists("I:/" + dlInfo.FileName))
        {
            client.DownloadFileCompleted += Client_DownloadFileCompleted;
            client.DownloadProgressChanged += Client_DownloadProgressChanged;
            client.DownloadFileAsync(new Uri(dlInfo.Link), "I:/" + dlInfo.FileName);
            Console.WriteLine("File downloaded");
        }
        else
        {
            Console.WriteLine("File already exists");
        }
    }

    public void CancelDownload()
    {
        if (client != null && client.IsBusy)
        {
            client.CancelAsync();
        }
    }

    private void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
    {
        if (e.Cancelled)
        {
            ProgressFeedback = "Download cancelled";
        }
        else
        {
            ProgressFeedback = "Download complete";
        }
    }

    private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
    {
        Progress = e.ProgressPercentage;
        ProgressFeedback = $"Downloading {e.ProgressPercentage}% ({e.BytesReceived / 1000000}MB/{e.TotalBytesToReceive / 1000000}MB))";
    }

    // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register("Progress", typeof(int), typeof(DownloadItem), new PropertyMetadata(0));
    public static readonly DependencyProperty ProgressFeedbackProperty = DependencyProperty.Register("ProgressFeedback", typeof(string), typeof(DownloadItem), new PropertyMetadata(""));

}