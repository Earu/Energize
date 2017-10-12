using EBot.Logs;
using System;
using System.Collections.Generic;
using System.Text;

namespace EBot.Commands.E621
{
    class Search
    {
        public static List<PostObject> Find(string search,List<PostObject> posts)
        {
            SortingHandler sorter = new SortingHandler(posts);
            List<PostObject> results = new List<PostObject>();
            List<PostObject> tags = sorter.GetTags(search);
            List<PostObject> ratings = sorter.GetRatings(search);
            foreach(PostObject p in posts)
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
            foreach(PostObject p in tags)
            {
                results.Add(p);
            }
            foreach (PostObject p in ratings)
            {
                results.Add(p);
            }
            return results;
        }

        public static PostObject Handle(List<PostObject> initposts,List<string> args)
        {
            List<PostObject> posts = Find(args[0],initposts);
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
                            List<PostObject> results = sorter.GetOverScore(score);
                            return parsed ? SortingHandler.GetRandom(results) : null;

                        }
                        else if (arg.Contains("below"))
                        {
                            bool parsed = int.TryParse(args[3], out score);
                            List<PostObject> results = sorter.GetBelowScore(score);
                            return parsed ? SortingHandler.GetRandom(results) : null;
                        }
                        else
                        {
                            bool parsed = int.TryParse(arg, out score);
                            List<PostObject> results = sorter.GetScore(score);
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
