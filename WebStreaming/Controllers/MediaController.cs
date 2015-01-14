using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Web.Configuration;
using System.Web.Http;
using WebStreaming.Controllers;

namespace WebStreaming.Controllers
{

    public class MediaController : ApiController
    {


        public const int bufferSize = 1024; 

        public static readonly string InitialDirectory;
        public static  List<string> MediaFilesList;

     
        static MediaController()
        {
            InitialDirectory = WebConfigurationManager.AppSettings["InitialDirectory"];
            MediaFilesList =  GetMediaFiles(WebConfigurationManager.AppSettings["InitialDirectory"]);
        }



       [HttpGet]
        public IEnumerable<string> GetFiles()
        {
            return MediaFilesList;
        }

        [HttpGet]
        public HttpResponseMessage Play(string f)
        {
            FileInfo fileInfo = new FileInfo(Path.Combine(InitialDirectory, f));

            if (!fileInfo.Exists)
                throw new HttpResponseException(HttpStatusCode.NotFound);

            long contentLength = fileInfo.Length;

            RangeHeaderValue rangeHeader = base.Request.Headers.Range;
            HttpResponseMessage response = new HttpResponseMessage();

            response.Headers.AcceptRanges.Add("bytes");

            long start = 0, end = 0;

            if (rangeHeader.Unit != "bytes" || rangeHeader.Ranges.Count > 1 ||
                !TryReadRangeItem(rangeHeader.Ranges.First(), contentLength, out start, out end))
            {
                response.StatusCode = HttpStatusCode.RequestedRangeNotSatisfiable;
                response.Content = new StreamContent(Stream.Null);  
                response.Content.Headers.ContentRange = new ContentRangeHeaderValue(contentLength);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");

                return response;
            }

            var contentRange = new ContentRangeHeaderValue(start, end, contentLength);

            
            response.StatusCode = HttpStatusCode.PartialContent;
            response.Content = new PushStreamContent((outputStream, httpContent, transpContext)
            =>
            {
                using (outputStream) 
                using (Stream inputStream = fileInfo.OpenRead())
                    StreamContent(inputStream, outputStream, start, end);

            }, new MediaTypeHeaderValue("video/mp4"));

            response.Content.Headers.ContentLength = end - start + 1;
            response.Content.Headers.ContentRange = contentRange;

            return response;
        }


        private static bool TryReadRangeItem(RangeItemHeaderValue range, long contentLength,
            out long start, out long end)
        {
            if (range.From != null)
            {
                start = range.From.Value;
                if (range.To != null)
                    end = range.To.Value;
                else
                    end = contentLength - 1;
            }
            else
            {
                end = contentLength - 1;
                if (range.To != null)
                    start = contentLength - range.To.Value;
                else
                    start = 0;
            }
            return (start < contentLength && end < contentLength);
        }

        private static void StreamContent(Stream inputStream, Stream outputStream,
            long start, long end)
        {
            int count = 0;
            long remainingBytes = end - start + 1;
            long position = start;
            byte[] buffer = new byte[bufferSize];
            
            inputStream.Position = start;
            while (position <= end) 
            {
                try
                {
                    if (remainingBytes > bufferSize)
                        count = inputStream.Read(buffer, 0, bufferSize);
                    else
                        count = inputStream.Read(buffer, 0, (int)remainingBytes); 
                    outputStream.Write(buffer, 0, count);
                }
                catch (Exception error)
                {
                    Debug.WriteLine(error);
                    break;
                }
                position = inputStream.Position;
                remainingBytes = end - position + 1;
            } 
        }

     private static List<string> GetMediaFiles(string initDir)
        {
            var files = new List<string>(Directory.GetFiles(initDir, "*.*", SearchOption.AllDirectories));
            return files;
        }
    }
}
