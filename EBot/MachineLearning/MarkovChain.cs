using System;
using System.Collections.Generic;
using System.IO;

namespace EBot.MachineLearning
{
    public class MarkovChain
    {
        private string _Path = "External/Markov/";

        public void Learn(string sentence)
        {

            if (!Directory.Exists(_Path))
            {
                Directory.CreateDirectory(_Path);
            }

            sentence = sentence.Trim().ToLower();
            string[] words = sentence.Split(new[] { ' ' });

            for (int i = 0; i < words.Length - 1; i++)
            {
                string word = words[i].ToLower();
                string next = words[i + 1].ToLower();
                string path = this._Path + word;
                File.AppendAllText(path, next + "\n");
            }
        }

        public List<string> Generate()
        {
            Random rand = new Random();
            string[] files = Directory.GetFiles(this._Path);
            string word = files[rand.Next(0, files.Length - 1)];
            string[] dirs = word.Split('/');
            word = dirs[dirs.Length - 1];

            return Generate(word, int.MaxValue);
        }

        public List<string> Generate(string firstWord)
        {
            return Generate(firstWord, int.MaxValue);
        }

        public List<string> Generate(string firstWord, int wordcount)
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
                    string[] words = File.ReadAllLines(path);
                    string temp = words[rand.Next(0, words.Length - 1)];

                    while(current == temp)
                    {
                        temp = words[rand.Next(0, words.Length - 1)];
                    }

                    current = temp;
                    results.Add(current);
                }
                else
                {
                    break;
                }
            }

            return results;
        }
    }
}