using YamlDotNet.Serialization;

namespace Energize.Essentials.Helpers
{
    public class YamlHelper
    {
        public static bool TryDeserialize<T>(string yaml, out T value)
        {
            try
            {
                Deserializer deserializer = new Deserializer();
                value = deserializer.Deserialize<T>(yaml);
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        public static bool TrySerialize(object obj, out string yaml)
        {
            try
            {
                Serializer serializer = new Serializer();
                yaml = serializer.Serialize(obj);
                return true;
            }
            catch
            {
                yaml = string.Empty;
                return false;
            }
        }
    }
}
