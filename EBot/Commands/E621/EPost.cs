using System;
using System.Collections.Generic;
using System.Text;

namespace EBot.Commands.E621
{
    class EPost
    {
        public int id;
        public string tags;
        public bool? locked_tags;
        public string description;
        public int creator_id;
        public string author;
        public int change;
        public string source;
        public int fav_count;
        public string md5;
        public int file_size;
        public string file_url;
        public string file_ext;
        public string preview_url;
        public int preview_width;
        public int preview_height;
        public string sample_url;
        public int sample_width;
        public int sample_height;
        public string rating;
        public string status;
        public int width;
        public int height;
        public bool has_comments;
        public bool has_notes;
        public bool has_children;
        public bool? parent_id;
        public string[] artist;
        public string[] sources;
    }
}
