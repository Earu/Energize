using Energize.Essentials;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Energize.Services.Generation
{
    public class MarkovChain
    {
        private static readonly string _Path = Config.Instance.URIs.MarkovDirectory;
        private static readonly int _MaxDepth = 2;
        private static readonly string _Extension = ".markov";
        private static readonly char[] _Separators = { ' ', '.', ',', '!', '?', ';', '_', '\n' };
        private static readonly char[] _WinInvalidChars = { '<', '>', '*', '|', ':', '\"', '`' };

        private void SaveWord(string path, string text, Logger logger)
        {
            try
            {
                File.AppendAllText(path, text);
            }
            catch
            {
                logger.Nice("Markov", ConsoleColor.Red, $"Could not save markov data [ word: {text}; path: {path} ]");
            }
        }

        public void Learn(string sentence, Logger logger)
        {
            if(sentence == null) return;

            if (!Directory.Exists(_Path))
                Directory.CreateDirectory(_Path);

            sentence = sentence.Trim().ToLower();
            sentence = Regex.Replace(sentence,@"https?://\S*","LINK-REMOVED ")
                .Replace("\\", "/")
                .Replace("/", " ")
                .Replace("\"", string.Empty);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                foreach (char invalidchar in _WinInvalidChars)
                    sentence = sentence.Replace($"{invalidchar}", string.Empty);
            }

            string[] words = sentence.Split(_Separators);

            for (int i = 0; i < words.Length - 1; i++)
            {
                string word = words[i].ToLower().Trim();
                string next = i + 1 > words.Length ? "END_SEQUENCE" : words[i + 1].ToLower().Trim();

                string path = _Path + word + _Extension;
                this.SaveWord(path, $"{next}\n", logger);

                string left = word + _Extension;
                for(int depth = 1; depth < _MaxDepth; depth++)
                {
                    if(i - depth >= 0)
                    {
                        left = $"{words[i - depth].Trim()}_{left}";
                        path = _Path + left;

                        this.SaveWord(path, next + "\n", logger);
                    }
                }
            }
        }

        public string Generate(string firstpart, int wordcount)
        {
            Random rand = new Random();
            string result = string.Empty;
            List<string> currentinput = new List<string>(firstpart.Split(_Separators));

            for(int i = 0; i < wordcount; i++)
            {
                string path = _Path + string.Join('_',currentinput) + _Extension;
                if(File.Exists(path))
                {
                    string[] nexts = File.ReadAllLines(path);
                    string next = nexts[rand.Next(0,nexts.Length)].Trim();

                    if(next == "END_SEQUENCE") break;

                    result += $" {next}";

                    currentinput.Add(next);
                    if(currentinput.Count > _MaxDepth)
                        currentinput.RemoveAt(0);
                }
                else
                {
                    break;
                }
            }

            return result;
        }
    }
}
