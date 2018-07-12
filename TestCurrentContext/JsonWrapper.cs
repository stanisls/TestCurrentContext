using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace TestCurrentContext
{
    public static class JsonWrapper
    {
        public static string Serialize(object obj, bool doFormatting = false)
        {
            var serializer = new JsonSerializer();
            var json = new StringBuilder(1000);
            using (var sw = new StringWriter(json))
            {
                using (var jw = new JsonTextWriter(sw))
                {
                    if (doFormatting)
                    {
                        jw.Formatting = Formatting.Indented;
                    }

                    serializer.Serialize(jw, obj);
                }
            }

            return json.ToString();
        }
        
        public static T Deserialize<T>(Stream stream)
        {
            // ReSharper disable AssignNullToNotNullAttribute
            using (var reader = new StreamReader(stream))
            // ReSharper restore AssignNullToNotNullAttribute
            {
                using (var jsonReader = new JsonTextReader(reader))
                {
                    var jsonSerializer = new JsonSerializer();
                    return jsonSerializer.Deserialize<T>(jsonReader);
                }
            }
        }

        public static T Deserialize<T>(string str)
        {
            return JsonConvert.DeserializeObject<T>(str);
        }
    }
}
