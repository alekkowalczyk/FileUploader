using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileUploader
{
    class Demo
    {
        public static void Upload()
        {
            var log = new StringBuilder();
            FileUploader.UploadFile(
                url: "http://example.com/upload",
                filePath: @"C:/some_file.txt",
                values: new NameValueCollection
                {
                    ["key1"] = "value1",
                    ["key2"] = "value2"
                }, 
                progress: (s,e) => log.AppendLine($"Progress: {s} - {e}"),
                completed: (s) => log.AppendLine($"Completed: {s}")
                );
        }
    }
}
