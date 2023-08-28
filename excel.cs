using Bakalar;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bakalarkaversion2
{
    internal class excel
    {

        public static void save_to_csv(List<repo_info> repos)
        {
            List<issue_info> issues_with_images = new();
            List<issue_info> issues_with_videos = new();
            List<issue_info> issues_with_text = new();
            int issue_count = 0;

            foreach (repo_info repo in repos)
            {
                // Collect the issues based *from all repos* based on their properties:
                issues_with_images.AddRange(repo.issues.Where(x => x.contains_image));
                issues_with_videos.AddRange(repo.issues.Where(x => x.contains_video));
                issues_with_text.AddRange(repo.issues.Where(x => x.is_visual_check() == false));
                issue_count += repo.issues.Count;
                // ...
            }

            Console.WriteLine("Saving data...");

            //filepaths
            string result1 = @"boxplotcsv\description_length_data.csv";
            string result2 = @"boxplotcsv\comment_count_data.csv";
            string result3 = @"boxplotcsv\first_comment.csv";
            string result4 = @"boxplotcsv\resolved.csv";

            Directory.CreateDirectory("boxplotcsv");

            File.Delete(result1);
            File.Delete(result2);
            File.Delete(result3);
            File.Delete(result4);

            string separator = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
            StringBuilder output_one = new StringBuilder();
            StringBuilder output_two = new StringBuilder();
            StringBuilder output_three = new StringBuilder();
            StringBuilder output_four = new StringBuilder();


            string[] issue_info_heading = { "image", "video", "text" };




            output_one.AppendLine(string.Join(separator, issue_info_heading));
            output_two.AppendLine(string.Join(separator, issue_info_heading));
            output_three.AppendLine(string.Join(separator, issue_info_heading));
            output_four.AppendLine(string.Join(separator, issue_info_heading));
            //writing in the file



            // Stats:

            //description length
            int ii = 0; ;
            for (int i = 0; i < issue_count; i++)
            {
                string[] description_length = {
                  i < issues_with_images.Count ? issues_with_images[i].description_word_count.ToString():" ",
                  i < issues_with_videos.Count ? issues_with_videos[i].description_word_count.ToString():" ",
                  i < issues_with_text.Count ? issues_with_text[i].description_word_count.ToString():" ",
                };

                string[] comment_count = {
                  i < issues_with_images.Count ? issues_with_images[i].comment_count.ToString():" ",
                  i < issues_with_videos.Count ? issues_with_videos[i].comment_count.ToString():" ",
                  i < issues_with_text.Count ? issues_with_text[i].comment_count.ToString():" ",
                };

                string[] first_comment_time = {
                  i < issues_with_images.Count ? (issues_with_images[i].first_comment_time.TotalHours / 24).ToString():" ",
                  i < issues_with_videos.Count ? (issues_with_videos[i].first_comment_time.TotalHours / 24).ToString():" ",
                  i < issues_with_text.Count ? (issues_with_text[i].first_comment_time.TotalHours / 24).ToString():" "
                };
                string[] resolved_time = {
                  i < issues_with_images.Count ? (issues_with_images[i].resolution_time.TotalHours / 24).ToString():" ",
                  i < issues_with_videos.Count ? (issues_with_videos[i].resolution_time.TotalHours / 24).ToString():" ",
                  i < issues_with_text.Count ? (issues_with_text[i].resolution_time.TotalHours / 24).ToString():" ",
                };



                output_one.AppendLine(string.Join(separator, description_length));
                output_two.AppendLine(string.Join(separator, comment_count));
                output_three.AppendLine(string.Join(separator, first_comment_time));
                output_four.AppendLine(string.Join(separator, resolved_time));

                ii = i;
            }
            Console.WriteLine("ii is :" + ii);
            try
            {
                File.AppendAllText(result1, output_one.ToString());
                File.AppendAllText(result2, output_two.ToString());
                File.AppendAllText(result3, output_three.ToString());
                File.AppendAllText(result4, output_four.ToString());

            }
            catch (Exception ex)
            {
                Console.WriteLine("Data could not be written to the CSV file. Reason: " + ex.Message);
                return;
            }
            Console.WriteLine("Data has been saved");
        }








    }
}

