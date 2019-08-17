using Energize.Essentials.Helpers;
using System.Collections.Generic;
using System.IO;

namespace Energize.Essentials
{
    public class LanguageData
    {
        public static LanguageData Instance { get; } = Initialize();

        private Dictionary<string, Dictionary<string, string>> LanguageFiles;
        private Dictionary<string, string> DefaultFile;

        private static LanguageData Initialize()
        {
            LanguageData data = new LanguageData
            {
                LanguageFiles = new Dictionary<string, Dictionary<string, string>>(),
                DefaultFile = new Dictionary<string, string>()
            };

            if (!Directory.Exists("Data/langs")) return data;

            string[] langFiles = Directory.GetFiles("Data/langs");
            foreach(string langFile in langFiles)
            {
                FileInfo fileInfo = new FileInfo(langFile);
                string name = fileInfo.Name.Remove(2);
                string yaml = File.ReadAllText(fileInfo.FullName);
                if (YamlHelper.TryDeserialize(yaml, out Dictionary<string, string> values))
                {
                    data.LanguageFiles.Add(name, values);
                    if (name.Equals("EN"))
                        data.DefaultFile = values;
                }
                else
                {
                    data.LanguageFiles.Add(name, new Dictionary<string, string>());
                }
            }

            return data;
        }

        /// <summary>
        /// Translates to the language of the country code passed if the language file is loaded
        /// </summary>
        /// <param name="countryCode">The country code corresponding to the language we want to translate to</param>
        /// <param name="identifier">The identifier of the phrase we want to get</param>
        /// <returns>A translated phrase corresponding to the passed identifier</returns>
        public string GetPhrase(string countryCode, string identifier)
        {
            if (this.LanguageFiles.TryGetValue(countryCode, out Dictionary<string, string> values) && values.TryGetValue(identifier, out string phrase))
                return phrase;
            else if (this.DefaultFile.TryGetValue(identifier, out string defaultPhrase))
                return defaultPhrase;

            return identifier;
        }
    }
}
