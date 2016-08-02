using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileUploader
{
    public class FileUploader
    {
        public static void UploadFile(string url, NameValueCollection values, string filePath, Action<string, int> progress, Action<string> completed)
        {
            var fileStream = File.OpenRead(filePath);
            var fileName = Path.GetFileName(filePath);
            var ms = new MemoryStream();
            // Make a copy of the input stream in case sb uses disposable stream
            fileStream.CopyTo(ms);
            // Stream position needs to be set to zero - just to be sure.
            ms.Position = 0;

            try
            {
                const string contentType = "application/octet-stream";

                var request = WebRequest.Create(url);
                request.Method = "POST";
                var boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x", NumberFormatInfo.InvariantInfo);
                request.ContentType = "multipart/form-data; boundary=" + boundary;
                boundary = "--" + boundary;

                var dataStream = new MemoryStream();
                byte[] buffer;
                // Write the values
                foreach (string name in values.Keys)
                {
                    buffer = Encoding.ASCII.GetBytes(boundary + Environment.NewLine);
                    dataStream.Write(buffer, 0, buffer.Length);
                    buffer = Encoding.ASCII.GetBytes(string.Format("Content-Disposition: form-data; name=\"{0}\"{1}{1}", name, Environment.NewLine));
                    dataStream.Write(buffer, 0, buffer.Length);
                    buffer = Encoding.UTF8.GetBytes(values[name] + Environment.NewLine);
                    dataStream.Write(buffer, 0, buffer.Length);
                }

                // Write the file
                buffer = Encoding.ASCII.GetBytes(boundary + Environment.NewLine);
                dataStream.Write(buffer, 0, buffer.Length);
                buffer = Encoding.UTF8.GetBytes($"Content-Disposition: form-data; name=\"file\"; filename=\"{fileName}\"{Environment.NewLine}");
                dataStream.Write(buffer, 0, buffer.Length);
                buffer = Encoding.ASCII.GetBytes(string.Format("Content-Type: {0}{1}{1}", contentType, Environment.NewLine));
                dataStream.Write(buffer, 0, buffer.Length);
                ms.CopyTo(dataStream);
                buffer = Encoding.ASCII.GetBytes(Environment.NewLine);
                dataStream.Write(buffer, 0, buffer.Length);

                buffer = Encoding.ASCII.GetBytes(boundary + "--");
                dataStream.Write(buffer, 0, buffer.Length);


                dataStream.Position = 0;
                // Important part: set content length to directly write to network socket
                request.ContentLength = dataStream.Length;
                var requestStream = request.GetRequestStream();

                // Write data in chunks and report progress
                var size = dataStream.Length;
                const int chunkSize = 64 * 1024;
                buffer = new byte[chunkSize];
                long bytesSent = 0;
                int readBytes;
                while ((readBytes = dataStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    requestStream.Write(buffer, 0, readBytes);
                    bytesSent += readBytes;

                    var status = "Uploading... " + bytesSent / 1024 + "KB of " + size / 1024 + "KB";
                    var percentage = Convert.ToInt32(100 * bytesSent / size);
                    progress(status, percentage);
                }

                // Get response from host
                using (var response = request.GetResponse())
                using (var responseStream = response.GetResponseStream())
                using (var stream = new MemoryStream())
                {
                    responseStream.CopyTo(stream);
                    var result = Encoding.Default.GetString(stream.ToArray());
                    completed(result == string.Empty
                        ? "failed:" + result
                        : "ok:" + result);
                }
            }
            catch (Exception e)
            {
                completed(e.ToString());
            }
        }
    }
}
