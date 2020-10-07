using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NetCore.Core.MongoDb.Utils
{
    public static class ArrayExtensions
    {
        public static IEnumerable<IEnumerable<TSource>> SplitToBatchs<TSource>(
            this IEnumerable<TSource> source, int size = 20)
        {
            TSource[] bucket = null;
            var count = 0;

            foreach (var item in source)
            {
                if (bucket == null)
                    bucket = new TSource[size];

                bucket[count++] = item;
                if (count != size)
                    continue;

                yield return bucket;

                bucket = null;
                count = 0;
            }

            if (bucket != null && count > 0)
                yield return bucket.Take(count);
        }
    
        public static Task Post(
            this IDictionary<string, object> form, string url)
        {
            return form.Post<string>(url);
        }
        public static async Task<T> Post<T>(
            this IDictionary<string, object> form, string url)
        {
            object result = null;

            try
            {
                var body = string.Empty;
                
                if (form?.Keys != null && form.Keys.Count > 0)
                {
                    foreach (var key in form.Keys)
                        body += "&" + key + "=" + form[key];

                    body = body.Substring(1);
                }

                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";

                using (var ms = new MemoryStream())
                using (var writer = new StreamWriter(await request.GetRequestStreamAsync()))
                    writer.Write(body);

                var content = null as string;

                using (var response = (HttpWebResponse)(await request.GetResponseAsync()))
                using (var stream = response.GetResponseStream())
                using (var sr = new StreamReader(stream))
                    content = sr.ReadToEnd();

                if (!string.IsNullOrEmpty(content))
                {
                    if (typeof(T) == typeof(string))
                        result = content;
                    else
                        result = JsonConvert.DeserializeObject<T>(content);
                }
            }
            catch { }

            return (T)result;
        }
    }
}