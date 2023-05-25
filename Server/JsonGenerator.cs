using System.Collections.Concurrent;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace GameRules.Scripts.Server
{
    public class JsonGenerator
    {
        private readonly StringBuilder _sb;
        private readonly JsonTextWriter _writer;
        
        private static readonly ConcurrentBag<JsonGenerator> Generators  = new ConcurrentBag<JsonGenerator>();

        private JsonGenerator()
        {
            _sb = new StringBuilder(256);
            _writer = new JsonTextWriter(new StringWriter(_sb));
        }

        public static JsonGenerator Begin()
        {
            if(!Generators.TryTake(out var generator))
                generator = new JsonGenerator();

            generator._sb.Clear();
            generator._writer.WriteStartObject();
            return generator;
        }

        public JsonGenerator AddParam(string key, string value)
        {
            _writer.WritePropertyName(key);
            _writer.WriteValue(value);

            return this;
        }
        
        public JsonGenerator AddParam(string key, int value)
        {
            _writer.WritePropertyName(key);
            _writer.WriteValue(value);

            return this;
        }
        
        public JsonGenerator AddParam(string key, float value)
        {
            _writer.WritePropertyName(key);
            _writer.WriteValue(value);

            return this;
        }
        
        public JsonGenerator AddParam(string key, uint value)
        {
            _writer.WritePropertyName(key);
            _writer.WriteValue(value);

            return this;
        }

        public string Release()
        {
            _writer.WriteEndObject();
            _writer.Flush();
            var result = _sb.ToString();
            
            Generators.Add(this);
            return result;
        }
    }
}