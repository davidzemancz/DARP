using DARP.Views;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DARP.Services
{
    public class ModelViewSerializationService
    {
        public void Serialize(Stream stream, IEnumerable<IModelView> modelViews, string name)
        {
            using (Utf8JsonWriter jsonWriter = new(stream))
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WriteStartArray(name);
                foreach (IModelView item in modelViews)
                {
                    jsonWriter.WriteRawValue(JsonSerializer.Serialize(item.GetModelObj()));
                }
                jsonWriter.WriteEndArray();
                jsonWriter.WriteEndObject();
            }
        }

        public void Deserialize()
        {
            
        }

        public void SerializeMany(Stream stream, IDictionary<string, IEnumerable<IModelView>> modelViews)
        {
            using(Utf8JsonWriter jsonWriter = new(stream))
            {
                jsonWriter.WriteStartObject();
                foreach ((string name, IEnumerable<IModelView> mv) in modelViews)
                {
                    jsonWriter.WriteStartArray(name);
                    foreach (IModelView item in mv)
                    {
                        jsonWriter.WriteRawValue(JsonSerializer.Serialize(item.GetModelObj()));
                    }
                    jsonWriter.WriteEndArray();
                }
                jsonWriter.WriteEndObject();
            }
        }
    }
}
