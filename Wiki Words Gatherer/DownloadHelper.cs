using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Diagnostics;
using System.IO;

namespace Wiki_Words_Gatherer
{
    public class DownloadHelper
    {
        public static async Task<string> DownloadHtmlAsync(string uri)//Encoding encoding
        {
            HttpWebRequest request = WebRequest.Create(uri) as HttpWebRequest;
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; rv:36.0) Gecko/20100101 Firefox/36.0";
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            request.Headers.Set("Accept-Language", "en-US,ru;q=0.8,en-US;q=0.5,en;q=0.3");
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;         //добавляет заголовок для gzip, переключает режим декомпрессии
            request.KeepAlive = true;
            try
            {
                Debug.WriteLine("Downloading " + uri);
                var response = await request.GetResponseAsync() as HttpWebResponse;
                Debug.WriteLine("Finished " + uri);

                StreamReader sr = new StreamReader(response.GetResponseStream());
                string html = sr.ReadToEnd();
                return html;
            }
            catch
            {
                return "";
            }            
        }



        
    }
}
