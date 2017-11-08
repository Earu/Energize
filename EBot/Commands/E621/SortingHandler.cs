using System;
using System.Collections.Generic;

namespace EBot.Commands.E621
{
    class SortingHandler
    {
        private List<EPost> _Posts;

        public SortingHandler(List<EPost> posts)
        {
            this._Posts = posts;
        }

        public List<EPost> GetBelowScore(int score)
        {
            List<EPost> results = new List<EPost>();
            foreach (E621.EPost p in this._Posts)
            {
                if (p.fav_count < score)
                {
                    results.Add(p);
                }
            }
            return results;
        }

        public List<EPost> GetOverScore(int score)
        {
            List<EPost> results = new List<EPost>();
            foreach (E621.EPost p in this._Posts)
            {
                if (p.fav_count > score)
                {
                    results.Add(p);
                }
            }
            return results;
        }

        public List<EPost> GetScore(int score)
        {
            List<EPost> results = new List<EPost>();
            foreach (E621.EPost p in this._Posts)
            {
                if (p.fav_count == score)
                {
                    results.Add(p);
                }
            }
            return results;
        }

        public List<EPost> GetTags(string tag)
        {
            List<EPost> results = new List<EPost>();
            foreach (E621.EPost p in this._Posts)
            {
                if (p.tags.ToLower().Contains(tag.ToLower()))
                {
                    results.Add(p);
                }
            }
            return results;
        }

        public List<EPost> GetRatings(string rating)
        {
            List<EPost> results = new List<EPost>();
            foreach (E621.EPost p in this._Posts)
            {
                if (p.rating.ToLower().Contains(rating.ToLower()))
                {
                    results.Add(p);
                }
            }
            return results;
        }

        public static EPost GetRandom(List<EPost> posts)
        {
            Random rand = new Random();
            return posts.Count == 0 ? null: posts[rand.Next(0, posts.Count)];
        }

        public EPost GetRandom()
        {
            Random rand = new Random();
            return this._Posts.Count == 0 ? null : this._Posts[rand.Next(0, this._Posts.Count)];
        }
    }
}
