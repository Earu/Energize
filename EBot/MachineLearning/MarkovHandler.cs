using System.Collections.Generic;
using System.Threading.Tasks;

namespace EBot.MachineLearning
{
    public class MarkovHandler
    {
        private static MarkovChain _Chain = new MarkovChain();

        public static void Learn(string data)
        {
            _Chain.Learn(data);
        }

        public static string Generate(string data)
        {
            string result = "";

            if (data == "")
            {
                List<string> words = _Chain.Generate();
                foreach (string word in words)
                {
                    result += " " + word;
                }

                return result;
            }
            else
            {
                string[] parts = data.Split(' ');
                string firstword = parts[parts.Length - 1];
                string firstpart = string.Join(' ', parts, 0, parts.Length - 1);
                List<string> words = _Chain.Generate(firstword);

                foreach (string word in words)
                {
                    result += " " + word;
                }

                return firstpart + " " + result.TrimStart();
            }
        }
    }
}
