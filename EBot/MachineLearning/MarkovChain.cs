using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace EBot.MachineLearning
{
    public class MarkovChain
    {
        public string _Path = "External/Markov/";

        public async Task Learn(string sentence)
        {
            sentence = sentence.Trim().ToLower();
            string[] words = sentence.Split(new[] { ' ' });

            for (int i = 0; i < words.Length - 1; i++)
            {
                string word = words[i].ToLower();
                string next = words[i + 1].ToLower();

                string path = this._Path + word;
                using (StreamWriter writer = File.AppendText(path))
                {
                    await writer.WriteAsync(next);
                    await writer.WriteLineAsync();
                    await writer.FlushAsync();
                }
            }
        }

        public async Task<List<string>> Generate()
        {
            Random rand = new Random();
            string[] files = Directory.GetFiles(this._Path);
            string word = files[rand.Next(0, files.Length - 1)];

            return await Generate(word, int.MaxValue);
        }

        public async Task<List<string>> Generate(string firstWord)
        {
            return await Generate(firstWord, int.MaxValue);
        }

        public async Task<List<string>> Generate(string firstWord, int wordcount)
        {
            List<string> results = new List<string>();
            int count = 0;
            Random rand = new Random();
            string current = firstWord;

            results.Add(current);

            while (++count < wordcount)
            {
                string path = this._Path + current;

                if (File.Exists(path))
                {
                    string[] words = await File.ReadAllLinesAsync(path);
                    int nextid = rand.Next(0,words.Length-1);
                    current = words[nextid];

                    results.Add(current);
                }
                else
                {
                    if(count < 2) //happens sometimes
                    {
                        results.Add(current);
                    }

                    break;
                }
            }

            return results;
        }
    }
}