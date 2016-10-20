using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;


namespace SsisFtpSource
{
    public class FtpHelper
    {
        string _userName;
        string _password;
        string _hostName;
        string _path;

        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }

        public string HostName
        {
            get { return _hostName; }
            set { _hostName = value; }
        }

        public FtpHelper()
        { 
            
        }

        public void Connect(string userName, string password, string hostName)
        {
            _userName = userName;
            _password = password;
            _hostName = hostName;
        }

        public FileStruct[] GetFiles(string path)
        {
            _path = path;

            UriBuilder builder = new UriBuilder();
            builder.Scheme = "ftp";
            builder.Host = _hostName;
            builder.Path = path;

            String url = builder.ToString();

            try
            {
                FtpWebRequest ftpclientRequest = WebRequest.Create(url) as FtpWebRequest;
                ftpclientRequest.Credentials = new NetworkCredential(_userName, _password);
                ftpclientRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                ftpclientRequest.Proxy = null;
                FtpWebResponse response = ftpclientRequest.GetResponse() as FtpWebResponse;
                StreamReader sr = new StreamReader(response.GetResponseStream(), System.Text.Encoding.ASCII);
                string Datastring = sr.ReadToEnd();
                response.Close();

                FileStruct[] list = (new ParseListDirectory()).GetList(Datastring);
                return list;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new Exception("Error in GetFiles on parsing directory listing");
            }

            
        }
    }
}
