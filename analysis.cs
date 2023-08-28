    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Numerics.Statistics;
    using System.Text;

    namespace Bakalar
    {

        
        

        public class analysis_result
        {
            public analysis_result(string name, double value)
            {
                this.name = name;
                this.value = value;
            }

            public string name;
            public double value;
        }

        public class issue_category_values
        {
            public issue_category_values() { }
            public issue_category_values(double images_values, double videos_values, double texts_values)
            {
                this.images_value = images_values;
                this.videos_value = videos_values;
                this.texts_value = texts_values;
            }
            public double images_value;
            public double videos_value;
            public double texts_value;
        }

        public class statistic_values
        {
            public statistic_values(IEnumerable<double> images_list, IEnumerable<double> videos_list, IEnumerable<double> texts_list)
            {
                first_quartiles = new(analysis.get_first_quartile(images_list), analysis.get_first_quartile(videos_list), analysis.get_first_quartile(texts_list));
                third_quartiles = new(analysis.get_upper_quartile(images_list), analysis.get_upper_quartile(videos_list), analysis.get_upper_quartile(texts_list));

                mins = new(analysis.get_min(images_list), analysis.get_min(videos_list), analysis.get_min(texts_list));
                maxes = new(analysis.get_max(images_list), analysis.get_max(videos_list), analysis.get_max(texts_list));
                means = new(analysis.get_mean(images_list), analysis.get_mean(videos_list), analysis.get_mean(texts_list));
                medians = new(analysis.get_median(images_list), analysis.get_median(videos_list), analysis.get_median(texts_list));
            }

            public issue_category_values first_quartiles;
            public issue_category_values third_quartiles;
            public issue_category_values mins, maxes;
            public issue_category_values means;
            public issue_category_values medians;
        }

        public class statistic_results
        {
            public statistic_results(List<repo_info> repos)
            {
            List<issue_info> issues_with_images = new();
            List<issue_info> issues_with_videos = new();
            List<issue_info> issues_with_text = new();

            foreach (repo_info repo in repos)
            {
                // Collect the issues based *from all repos* based on their properties:
                issues_with_images.AddRange(repo.issues.Where(x => x.contains_image));
                issues_with_videos.AddRange(repo.issues.Where(x => x.contains_video));
                issues_with_text.AddRange(repo.issues.Where(x => x.is_visual_check() == false));
                // ...
            }

            issue_count = new(issues_with_images.Count, issues_with_videos.Count, issues_with_text.Count);

                description_length = new(
                issues_with_images.Select(issue => issue.description_word_count),
                issues_with_videos.Select(issue => issue.description_word_count),
                issues_with_text.Select(issue =>   issue.description_word_count)
                );

                comment_count = new(
                issues_with_images.Select(issue => issue.comment_count),
                issues_with_videos.Select(issue => issue.comment_count),
                issues_with_text.Select(issue => issue.comment_count)
                );

                first_comment = new(
                issues_with_images.Select(issue => issue.first_comment_time.TotalHours / 24),
                issues_with_videos.Select(issue => issue.first_comment_time.TotalHours / 24),
                issues_with_text.Select(issue => issue.first_comment_time.TotalHours / 24)
                );

                resolved_time = new(
                issues_with_images.Select(issue => issue.resolution_time.TotalHours / 24),
                issues_with_videos.Select(issue => issue.resolution_time.TotalHours / 24),
                issues_with_text.Select(issue => issue.resolution_time.TotalHours / 24)
                );
            }

            public issue_category_values issue_count;

            public statistic_values description_length;
            public statistic_values comment_count;
            public statistic_values first_comment;
            public statistic_values resolved_time;
        }

        public static class analysis
        {



        public static (List<KeyValuePair<string, int>> image, List<KeyValuePair<string, int>> video, List<KeyValuePair<string, int>> text) get_word_occurances(List<repo_info> repos)
        {
                List<issue_info> issues_with_image = new();
                List<issue_info> issues_with_video = new();
                List<issue_info> issues_with_text = new();

                foreach (repo_info repo in repos)
                {
                    // Collect the issues based *from all repos* based on their properties:
                    issues_with_image.AddRange(repo.issues.Where(x => x.contains_image));
                    issues_with_video.AddRange(repo.issues.Where(x => x.contains_video));
                    issues_with_text.AddRange(repo.issues.Where(x => x.is_visual_check() == false));
                    // ...
                }

                Dictionary<string, int> dict_image = new();

            foreach (var issue in issues_with_image)
            {
                if (issue.words == null) continue;
                foreach (string word in issue.words)
                {
                    if (!dict_image.ContainsKey(word)) dict_image[word] = 1;
                    else dict_image[word] += 1;
                }
            }

            var list_image = dict_image.ToList();
            list_image = list_image.OrderByDescending(x => x.Value).ToList();

            // 

            Dictionary<string, int> dict_video = new();

            foreach (var issue in issues_with_video)
            {
                if (issue.words == null) continue;
                foreach (string word in issue.words)
                {
                    if (!dict_video.ContainsKey(word)) dict_video[word] = 1;
                    else dict_video[word] += 1;
                }
            }

            var list_video = dict_video.ToList();
            list_video = list_video.OrderByDescending(x => x.Value).ToList();

            // 

            Dictionary<string, int> dict_text = new();

            foreach (var issue in issues_with_text)
            {
                if (issue.words == null) continue;
                foreach (string word in issue.words)
                {
                    if (!dict_text.ContainsKey(word)) dict_text[word] = 1;
                    else dict_text[word] += 1;
                }
            }

            var list_text = dict_text.ToList();
            list_text = list_text.OrderByDescending(x => x.Value).ToList();

            return (list_image, list_video, list_text); //top words for each categary
        }

        public static double get_median(IEnumerable<double> list)
            {
                var array = list.ToArray();

                // "Sample array, must be sorted ascendingly."
                // https://numerics.mathdotnet.com/api/MathNet.Numerics.Statistics/SortedArrayStatistics.htm#Median

                Array.Sort(array);
                return SortedArrayStatistics.Median(array);


            }

            public static double get_first_quartile(IEnumerable<double> list)
            {
                var array = list.ToArray();

                Array.Sort(array);
                return SortedArrayStatistics.LowerQuartile(array);
            
            }

            public static double get_upper_quartile(IEnumerable<double> list)
            {
                var array = list.ToArray();

                Array.Sort(array);
                return SortedArrayStatistics.UpperQuartile(array);

            }

            public static double get_max(IEnumerable<double> list)
            {
                var array = list.ToArray();

                Array.Sort(array);
                return SortedArrayStatistics.Maximum(array);

            }
            public static double get_min(IEnumerable<double> list)
            {
                var array = list.ToArray();

                Array.Sort(array);
                return SortedArrayStatistics.Minimum(array);

            }

            public static double get_mean(IEnumerable<double> list)
            {
                var array = list.ToArray();

                Array.Sort(array);
                return array.Mean();
               //return SortedArrayStatistics.m(array);

            }

            public static statistic_results calculate_statistics(List<repo_info> repos)
            {
                return new statistic_results(repos);
            }

            public static void report(List<repo_info> repos, statistic_results results)
            {
                Console.WriteLine("Reporting results...");
                Console.WriteLine("Count of repositories: " + repos.Count);

                Console.WriteLine("Count of issues with images: " + results.issue_count.images_value);
                Console.WriteLine("Count of issues with videos: " + results.issue_count.videos_value);
                Console.WriteLine("Count of issues without images/videos: " + results.issue_count.texts_value);

                // Comment word count:
                Console.WriteLine("Median for word count in issues with images: " + results.description_length.medians.images_value);
                Console.WriteLine("Median for word count in issues with videos: " + results.description_length.medians.videos_value);
                Console.WriteLine("Median for word count in issues with text only: " + results.description_length.medians.texts_value);

                // Comment count:
                Console.WriteLine("Median for comment count in issues with images: " + results.comment_count.medians.images_value);
                Console.WriteLine("Median for comment count in issues with videos: " + results.comment_count.medians.videos_value);
                Console.WriteLine("Median for comment count in issues with text only: " + results.comment_count.medians.texts_value);

                //First comment days
                Console.WriteLine("Median for days until the first comment was made in issues with images: " + results.first_comment.medians.images_value);
                Console.WriteLine("Median for days until the first comment was made in issues with videos: " + results.first_comment.medians.videos_value);
                Console.WriteLine("Median for days until the first comment was made in issues with text only: " + results.first_comment.medians.texts_value);

                Console.WriteLine("Median for days until resolved in issues with images: " + results.resolved_time.medians.images_value);
                Console.WriteLine("Median for days until resolved in issues with videos: " + results.resolved_time.medians.videos_value);
                Console.WriteLine("Median for days until resolved in issues with text only: " + results.resolved_time.medians.texts_value);
            }

            public static void save_to_csv(List<repo_info> repos, statistic_results stats_results)
            {

                Console.WriteLine("Saving data...");

                //filepaths
                string result1 = @"bakalarkacsv\total_collected_data.csv";
                // string result2 = @"bakalarkacsv\issues_images_data.csv";
                string result2 = @"bakalarkacsv\description_length_data.csv";

                string result3 = @"bakalarkacsv\comment_count_data.csv";
                string result4 = @"bakalarkacsv\first_comment.csv";
                string result5 = @"bakalarkacsv\resolved.csv";

                Directory.CreateDirectory("bakalarkacsv");

                File.Delete(result1);
                File.Delete(result2);
                File.Delete(result3);
                File.Delete(result4);
                File.Delete(result5);

                string separator = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
                StringBuilder output_one = new StringBuilder();
                //StringBuilder output_two = new StringBuilder();
                StringBuilder output_three = new StringBuilder();
                StringBuilder output_four = new StringBuilder();
                StringBuilder output_five = new StringBuilder();
                StringBuilder output_two = new StringBuilder();

            
                string[] issue_info_heading_all = { "Count of repositories", "Count of issues with images", "Count of issues with videos", "Count of issues without images/videos" };

                string[] issue_info_heading_all_issues = { "Repository name","Issue title", "Count of images", "Count of videos", "Count of comments", "days until resolved", "days until the first comment was made" };

                string[] description_length_headings = { "issue type", "min", "max","first quartile", "third quartile", "mean", "median" };
                string[] first_comment_headings = { "issue type", "min", "max","first quartile", "third quartile", "mean", "median" };
                string[] comment_count_headings= { "issue type", "min", "max", "first quartile", "third quartile", "mean", "median" };
                string[] resolved_headings = { "issue type", "min", "max", "first quartile", "third quartile", "mean", "median" };



                output_one.AppendLine(string.Join(separator, issue_info_heading_all_issues));
                output_two.AppendLine(string.Join(separator, description_length_headings));
                output_three.AppendLine(string.Join(separator, first_comment_headings));
                output_four.AppendLine(string.Join(separator, comment_count_headings));
                output_five.AppendLine(string.Join(separator, resolved_headings));
                //writing in the file

                //object[] total_info = { repos.Count, stats_results.issue_count.images_value, stats_results.issue_count.videos_value, stats_results.issue_count.texts_value };



                // Stats:
            
                //description length
                string[] description_length_image = { "image", stats_results.description_length.mins.images_value.ToString(), stats_results.description_length.maxes.images_value.ToString(),
                stats_results.description_length.first_quartiles.images_value.ToString(),stats_results.description_length.third_quartiles.images_value.ToString(),stats_results.description_length.means.images_value.ToString()
                ,stats_results.description_length.medians.images_value.ToString()};

                string[] description_length_video = { "video", stats_results.description_length.mins.videos_value.ToString(), stats_results.description_length.maxes.videos_value.ToString(),
                stats_results.description_length.first_quartiles.videos_value.ToString(),stats_results.description_length.third_quartiles.videos_value.ToString(),stats_results.description_length.means.videos_value.ToString()
                ,stats_results.description_length.medians.videos_value.ToString()};

                string[] description_length_text = { "text", stats_results.description_length.mins.texts_value.ToString(), stats_results.description_length.maxes.texts_value.ToString(),
                stats_results.description_length.first_quartiles.texts_value.ToString(),stats_results.description_length.third_quartiles.texts_value.ToString(),stats_results.description_length.means.texts_value.ToString()
                ,stats_results.description_length.medians.texts_value.ToString() };

                //comment count
                string[] comment_count_image = { "image", stats_results.comment_count.mins.images_value.ToString(), stats_results.comment_count.maxes.images_value.ToString(),
                    stats_results.comment_count.first_quartiles.images_value.ToString(), stats_results.comment_count.third_quartiles.images_value.ToString(),stats_results.comment_count.means.images_value.ToString(),
                stats_results.comment_count.medians.images_value.ToString()};

                string[] comment_count_video = { "video", stats_results.comment_count.mins.videos_value.ToString(), stats_results.comment_count.maxes.videos_value.ToString(),
                    stats_results.comment_count.first_quartiles.videos_value.ToString(), stats_results.comment_count.third_quartiles.videos_value.ToString(),stats_results.comment_count.means.videos_value.ToString(),
                stats_results.comment_count.medians.videos_value.ToString()};

                string[] comment_count_text = { "text", stats_results.comment_count.mins.texts_value.ToString(), stats_results.comment_count.maxes.texts_value.ToString(),
                    stats_results.comment_count.first_quartiles.texts_value.ToString(), stats_results.comment_count.third_quartiles.texts_value.ToString(),stats_results.comment_count.means.texts_value.ToString(),
                stats_results.comment_count.medians.texts_value.ToString()};

                //first comment time

                string[] first_comment_image = { "image", stats_results.first_comment.mins.images_value.ToString(), stats_results.first_comment.maxes.images_value.ToString(),
                    stats_results.first_comment.first_quartiles.images_value.ToString(), stats_results.first_comment.third_quartiles.images_value.ToString(),stats_results.first_comment.means.images_value.ToString(),
                stats_results.first_comment.medians.images_value.ToString()};

                string[] first_comment_video = { "video", stats_results.first_comment.mins.videos_value.ToString(), stats_results.first_comment.maxes.videos_value.ToString(),
                    stats_results.first_comment.first_quartiles.videos_value.ToString(), stats_results.first_comment.third_quartiles.videos_value.ToString(),stats_results.first_comment.means.videos_value.ToString(),
                stats_results.first_comment.medians.videos_value.ToString()};

                string[] first_comment_text = { "text", stats_results.first_comment.mins.texts_value.ToString(), stats_results.first_comment.maxes.texts_value.ToString(),
                    stats_results.first_comment.first_quartiles.texts_value.ToString(), stats_results.first_comment.third_quartiles.texts_value.ToString(),stats_results.first_comment.means.texts_value.ToString(),
                stats_results.first_comment.medians.texts_value.ToString()};



                //resolved time
                string[] resolved_time_image = { "image", stats_results.resolved_time.mins.images_value.ToString(), stats_results.resolved_time.maxes.images_value.ToString(),
                    stats_results.resolved_time.first_quartiles.images_value.ToString(), stats_results.resolved_time.third_quartiles.images_value.ToString(),stats_results.resolved_time.means.images_value.ToString(),
                stats_results.resolved_time.medians.images_value.ToString()};

                string[] resolved_time_video = { "video", stats_results.resolved_time.mins.videos_value.ToString(), stats_results.resolved_time.maxes.videos_value.ToString(),
                    stats_results.resolved_time.first_quartiles.videos_value.ToString(), stats_results.resolved_time.third_quartiles.videos_value.ToString(),stats_results.resolved_time.means.videos_value.ToString(),
                stats_results.resolved_time.medians.videos_value.ToString()};

                string[] resolved_time_text = { "text", stats_results.resolved_time.mins.texts_value.ToString(), stats_results.resolved_time.maxes.texts_value.ToString(),
                    stats_results.resolved_time.first_quartiles.texts_value.ToString(), stats_results.resolved_time.third_quartiles.texts_value.ToString(),stats_results.resolved_time.means.texts_value.ToString(),
                stats_results.resolved_time.medians.texts_value.ToString()};







                output_two.AppendLine(string.Join(separator, description_length_image));
                output_two.AppendLine(string.Join(separator, description_length_video));
                output_two.AppendLine(string.Join(separator, description_length_text));


                output_three.AppendLine(string.Join(separator, comment_count_image));
                output_three.AppendLine(string.Join(separator, comment_count_video));
                output_three.AppendLine(string.Join(separator, comment_count_text));

                output_four.AppendLine(string.Join(separator, first_comment_image));
                output_four.AppendLine(string.Join(separator, first_comment_video));
                output_four.AppendLine(string.Join(separator, first_comment_text));

                output_five.AppendLine(string.Join(separator, resolved_time_image));
                output_five.AppendLine(string.Join(separator, resolved_time_video));
                output_five.AppendLine(string.Join(separator, resolved_time_text));






                foreach (var repo in repos)
                {
                    foreach (var issue in repo.issues)
                    {
                        string[] all_issues = { repo.full_name.ToString(), issue.title.ToString(), issue.image_count.ToString(), issue.video_count.ToString(), issue.comment_count.ToString(),
                            issue.resolution_time.ToString(),(issue.first_comment_time.TotalHours / 24).ToString()};

                        output_one.AppendLine(string.Join(separator, all_issues));
                    }
                }
                try
                {
                    File.AppendAllText(result1, output_one.ToString());
                    File.AppendAllText(result2, output_two.ToString());
                    File.AppendAllText(result3, output_three.ToString());
                    File.AppendAllText(result4, output_four.ToString());
                    File.AppendAllText(result5, output_five.ToString());

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

