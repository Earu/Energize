using Energize.Interfaces.Services.Generation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Energize.Services.Generation
{
    [Service("Minesweeper")]
    public class MineSweeper : IMinesweeperService //this really sounds like a meme
    {
        public string Generate(int width, int height, int amount)
        {
            Random rand = new Random();
            Dictionary<(int, int), int> mines = new Dictionary<(int, int), int>();
            for (int i = 0; i < amount; i++)
            {
                int x = rand.Next(0, width);
                int y = rand.Next(0, height);
                while (mines.ContainsKey((y, x)))
                {
                    x = rand.Next(0, width);
                    y = rand.Next(0, height);
                }
                mines.Add((y, x), 0);
            }

            string mine = "💥";
            string result = string.Empty;
            string[] types = new string[]
            {
                    "⬜", "one", "two", "three", "four", "five", "six", "seven", "eight"
            };

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    string casecontent;
                    if (mines.ContainsKey((y, x)))
                    {
                        casecontent = mine;
                    }
                    else
                    {
                        int minecount = 0;
                        //above
                        if (mines.ContainsKey((y - 1, x - 1)) && mines[(y - 1, x - 1)] == 0)
                            minecount++;
                        if (mines.ContainsKey((y - 1, x)) && mines[(y - 1, x)] == 0)
                            minecount++;
                        if (mines.ContainsKey((y - 1, x + 1)) && mines[(y - 1, x + 1)] == 0)
                            minecount++;

                        //around
                        if (mines.ContainsKey((y, x - 1)) && mines[(y, x - 1)] == 0)
                            minecount++;
                        if (mines.ContainsKey((y, x + 1)) && mines[(y, x + 1)] == 0)
                            minecount++;

                        //under
                        if (mines.ContainsKey((y + 1, x - 1)) && mines[(y + 1, x - 1)] == 0)
                            minecount++;
                        if (mines.ContainsKey((y + 1, x)) && mines[(y + 1, x)] == 0)
                            minecount++;
                        if (mines.ContainsKey((y + 1, x + 1)) && mines[(y + 1, x + 1)] == 0)
                            minecount++;

                        casecontent = minecount == 0 ? types[0] : $":{types[minecount]}:";
                    }
                    result += $"||{casecontent}||";
                }
                result += "\n";
            }
            return result;
        }

        public void Initialize() { }

        public Task InitializeAsync()
            => Task.CompletedTask;
    }
}
