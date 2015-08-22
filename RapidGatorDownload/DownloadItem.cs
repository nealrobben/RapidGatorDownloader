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
    //public int Progress { get; set; }

    public DownloadItem(UserInfo usrInfo, DownloadInfo dlInfo)
    {
        this.usrInfo = usrInfo;
        this.dlInfo = dlInfo;

        DownloadName = dlInfo.FileName;
        Progress = 0;
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

    }

    private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
    {
        Progress = e.ProgressPercentage;
        //feedbackLabel.Content = $"{e.ProgressPercentage}% ({e.BytesReceived / 1000000}MB/{e.TotalBytesToReceive / 1000000}MB) ";
    }

    public int Progress
    {
        get { return (int)GetValue(ProgressProperty); }
        set { SetValue(ProgressProperty, value); }
    }

    // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ProgressProperty =
        DependencyProperty.Register("Progress", typeof(int), typeof(DownloadItem), new PropertyMetadata(0));

}