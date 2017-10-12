using System;
using System.Collections.Generic;
using System.Text;

namespace EBot.Commands.E621
{
    class SortingHandler
    {
        private List<PostObject> _Posts;

        public SortingHandler(List<PostObject> posts)
        {
            this._Posts = posts;
        }

        public List<PostObject> GetBelowScore(int score)
        {
            List<PostObject> results = new List<PostObject>();
            foreach (E621.PostObject p in this._Posts)
            {
                if (p.fav_count < score)
                {
                    results.Add(p);
                }
            }
            return results;
        }

        public List<PostObject> GetOverScore(int score)
        {
            List<PostObject> results = new List<PostObject>();
            foreach (E621.PostObject p in this._Posts)
            {
                if (p.fav_count > score)
                {
                    results.Add(p);
                }
            }
            return results;
        }

        public List<PostObject> GetScore(int score)
        {
            List<PostObject> results = new List<PostObject>();
            foreach (E621.PostObject p in this._Posts)
            {
                if (p.fav_count == score)
                {
                    results.Add(p);
                }
            }
            return results;
        }

        public List<PostObject> GetTags(string tag)
        {
            List<PostObject> results = new List<PostObject>();
            foreach (E621.PostObject p in this._Posts)
            {
                if (p.tags.ToLower().Contains(tag.ToLower()))
                {
                    results.Add(p);
                }
            }
            return results;
        }

        public List<PostObject> GetRatings(string rating)
        {
            List<PostObject> results = new List<PostObject>();
            foreach (E621.PostObject p in this._Posts)
            {
                if (p.rating.ToLower().Contains(rating.ToLower()))
                {
                    results.Add(p);
                }
            }
            return results;
        }

        public static PostObject GetRandom(List<PostObject> posts)
        {
            Random rand = new Random();
            return posts.Count == 0 ? null: posts[rand.Next(0, posts.Count)];
        }

        public PostObject GetRandom()
        {
            Random rand = new Random();
            return this._Posts.Count == 0 ? null : this._Posts[rand.Next(0, this._Posts.Count)];
        }
    }
}
