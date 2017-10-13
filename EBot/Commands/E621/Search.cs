using EBot.Logs;
using System;
using System.Collections.Generic;
using System.Text;

namespace EBot.Commands.E621
{
    class Search
    {
        public static List<EPost> Find(string search,List<EPost> posts)
        {
            SortingHandler sorter = new SortingHandler(posts);
            List<EPost> results = new List<EPost>();
            List<EPost> tags = sorter.GetTags(search);
            List<EPost> ratings = sorter.GetRatings(search);
            foreach(EPost p in posts)
            {
                if (p.author.ToLower().Contains(search.ToLower()))
                {
                    results.Add(p);
                }
                for(int i = 0;i < p.artist.Length; i++)
                {
                    if (p.artist[i].ToLower().Contains(search.ToLower()))
                    {
                        results.Add(p);
                    }
                }
            }
            foreach(EPost p in tags)
            {
                results.Add(p);
            }
            foreach (EPost p in ratings)
            {
                results.Add(p);
            }
            return results;
        }

        public static EPost Handle(List<EPost> initposts,List<string> args)
        {
            List<EPost> posts = Find(args[0],initposts);
            SortingHandler sorter = new SortingHandler(posts);

            if (args.Count > 1 && !string.IsNullOrWhiteSpace(args[1]))
            {
                if (args[1].ToLower().Contains("score"))
                {
                    string arg = args[2];
                    if (!string.IsNullOrWhiteSpace(arg))
                    {
                        arg = arg.ToLower().Trim();
                        int score;

                        if (arg.Contains("over"))
                        {
                            bool parsed = int.TryParse(args[3], out score);
                            List<EPost> results = sorter.GetOverScore(score);
                            return parsed ? SortingHandler.GetRandom(results) : null;

                        }
                        else if (arg.Contains("below"))
                        {
                            bool parsed = int.TryParse(args[3], out score);
                            List<EPost> results = sorter.GetBelowScore(score);
                            return parsed ? SortingHandler.GetRandom(results) : null;
                        }
                        else
                        {
                            bool parsed = int.TryParse(arg, out score);
                            List<EPost> results = sorter.GetScore(score);
                            return parsed ? SortingHandler.GetRandom(results) : null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }

            }

            return sorter.GetRandom();
        }
    }
}
