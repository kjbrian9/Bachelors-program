    using Octokit;
using System.ComponentModel;
using System.Runtime.Intrinsics.X86;
using System.Text.RegularExpressions;

namespace Bakalar
{
    public class issue_info
    {
        public static Regex is_visual_regex = new Regex(@"https://user-images.githubusercontent.com/[a-zA-Z0-9\-/]+\.[a-zA-Z0-9]+");
        public static Regex word_count_regex = new Regex(@"(\w)+");
        public static string word_regex_pattern = @"([a-zA-Z]{4,})+";

        public static string[] image_file_exts = { "jpg", "png", "jpeg" ,"jfif","pjpeg","pjp","svg","bmp","tif","tiff"};
        public static string[] video_file_exts = { "mp4", "gif", "mov" };

        public issue_info()
        {
            // An empty constructor is needed for the deserialization to work.

        }

        public issue_info(Issue issue)
        {
            number = issue.Number;
            title = issue.Title;
            description = issue.Body;
            comment_count = issue.Comments;
            created_at = issue.CreatedAt.DateTime;
            valid_resolution_time = true; //if >30sec && <1year

            if (issue.ClosedAt != null)
            {
                // Checking if the resolution time is less than 30 seconds or more than 1 year
                if ((issue.ClosedAt.Value.DateTime - issue.CreatedAt.DateTime).Seconds <= 30)
                    valid_resolution_time = false;
                if ((issue.ClosedAt.Value.DateTime - issue.CreatedAt.DateTime).Days >= 365)
                    valid_resolution_time = false;

                resolution_time = issue.ClosedAt.Value.DateTime - issue.CreatedAt.DateTime;
            }
            reg();
        }

        public bool valid_resolution_time;
        public int number;
        public string title;
        public string description;
        public DateTime created_at;

        public List<string> words;


        public TimeSpan first_comment_time; //first comment time
        public TimeSpan resolution_time; //resolution time
        public double comment_count; //comments
        public double description_word_count; //description length
        public bool contains_image;
        public bool contains_video;
        public int image_count; //images
        public int video_count; //videos
        public bool is_visual_check()
        {
            if (contains_image || contains_video)
                return true;
            else
                return false;
        }
        public void reg()
        {
            if (description != null)
            {

                var visual_matches = is_visual_regex.Matches(description);

                foreach (Match match in visual_matches)
                {
                    string file_ext = Path.GetExtension(match.Value).ToLower()[1..];

                    if (image_file_exts.Contains(file_ext))
                    {
                        contains_image = true;
                        image_count++;
                    }

                    if (video_file_exts.Contains(file_ext))
                    {
                        contains_video = true;
                        video_count++;
                    }
                }
                foreach (Match match in visual_matches)
                {
                    description = description.Replace(match.Value, "");
                }
                description_word_count = word_count_regex.Matches(description).Count;
            }
            
            if (description != null)
            {
                var word_matches = Regex.Matches(description.ToLower(), word_regex_pattern);
                words = word_matches.Select(m => m.Value).Where(m => !ignore_words_list.Contains(m)).ToList();
            }
        }

        public static List<string> ignore_words_list = new List<string>()
        {
            "at","to","the","as","it","this","that"
        };

    }
}
