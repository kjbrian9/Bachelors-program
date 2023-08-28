using Octokit;
using System.Collections.Generic;
using System.Text.Unicode;

namespace Bakalar
{
    public static class data
    {
        #region GitHub token

        public static string token_file_name = "token.txt";

        public static string read_or_ask_token() 
        {
            string result;
            //try to read the token from file:
            if (File.Exists("token.txt")) 
            {
                result = File.ReadAllText("token.txt");
                Console.WriteLine("GitHub token read from file");
            }
            else 
            {
                // ask the user for the token, then save it:
                Console.WriteLine("The program needs a GitHub Personal Access Token to function. Generate it here: https://github.com/settings/tokens");
                Console.Write("Enter a GitHub Personal Access Token: ");
                result = Console.ReadLine();
                write_token(result);
            }
            return result;
        }

        public static void write_token(string token)
        {
            try
            {
                File.WriteAllText(token_file_name, token);
            } 
            catch (Exception ex)
            {
                Console.WriteLine("Could not write token to file " + ex.Message);
            }
            
        }

        #endregion

        #region API rate limit

        private static int rate_limit_extra_seconds = 3;

        private static async Task handle_api_rate_limit(GitHubClient client) {
            Console.Write("Ran out of API rates:");

            TimeSpan reset_time = new(); //the amount of time we have to wait for the reset

            var ratelimit = await client.Miscellaneous.GetRateLimits(); //get github rate limit information

            if (ratelimit == null) 
            {
                //if github doesnt know, assume one minute and keep trying
                Console.WriteLine("no information about rate limits from api, waiting a minute...");
                reset_time = TimeSpan.FromMinutes(1);
            }
            else 
            {
                //check for both core and search api rate limits:
                //if we ran out of both,add them together
                if (ratelimit.Resources.Core.Remaining < 1) {
                    Console.Write(" core limit reached ");
                    reset_time += (ratelimit.Resources.Core.Reset - DateTime.Now);
                }
                if (ratelimit.Resources.Search.Remaining < 1) {
                    Console.Write(" search limit reached");
                    reset_time += (ratelimit.Resources.Search.Reset - DateTime.Now);
                }
                Console.WriteLine();
            }

            reset_time += TimeSpan.FromSeconds(rate_limit_extra_seconds); //add extra seconds just in case
            Console.WriteLine("reset at and waiting until: " + (DateTime.Now + reset_time) + " (" + reset_time.Minutes + " minutes, " + reset_time.Seconds + " seconds)");

            //wait out the reset time:

            // await Task.Delay(reset_time);

            //show progress:
            DateTime start_date_time = DateTime.Now;
            while (DateTime.Now < start_date_time + reset_time) 
            {
                int seconds_remaining = (int)((start_date_time + reset_time) - DateTime.Now).TotalSeconds;
                Console.Write("\r" + seconds_remaining + " seconds remaining...                        ");

                await Task.Delay(TimeSpan.FromSeconds(1)); //refresh text every 1 second
            }
        }

        #endregion

        public static async Task<List<repo_info>> load_data(bool ignore_saved_data = false) {
            if (ignore_saved_data == false && serialization.check_serialization()) 
            {
                Console.WriteLine("Saved serialized data found!");
                Console.WriteLine("Deserializing data from file " + serialization.serialization_file_name);
                return await serialization.deserialize_data(serialization.serialization_file_name);
            }

            if (ignore_saved_data) Console.Write("ignore_saved_data: ");
            else Console.WriteLine("Data not yet serialized: ");
            Console.WriteLine("loading from GitHub...");

            List<repo_info> data = await load_data_from_github();
            await serialization.serialize_data(data, serialization.serialization_file_name);

            return data;
        }

        //data loading from github:

        private async static Task<List<repo_info>> load_data_from_github() {
            string? github_token = read_or_ask_token();

            Console.WriteLine("Collecting data from GitHub");

            var client = new GitHubClient(new ProductHeaderValue("DataHound", "2.3"));

            client.Credentials = new Credentials(github_token);

            //make sure the token works:
            try 
            {
                var user = await client.User.Current();
                Console.WriteLine("token owner: " + user.Name);
            }
            catch (Exception ex) 
            {
                Console.WriteLine("API failed: " + ex.Message + " - make sure your token is valid (token.txt)");
                Environment.Exit(ex.HResult);
            }

            var search_request = new SearchRepositoriesRequest()
            {
                PerPage = repo_search_per_page,
                Page = 0,
                Stars = Octokit.Range.GreaterThan(repo_search_stars_min),
                Created = DateRange.GreaterThan(new DateTimeOffset(repo_search_creation_date_min))
            };

            var repo_infos = await get_repo_infos_from_search(client, search_request);

            foreach (var repo in repo_infos) 
            {
                Console.WriteLine("  - repo id: " + repo.id + " name: " + repo.full_name);

                //make a search request for issues within this repo:
                SearchIssuesRequest issue_search_request = new()
                {
                    Type = IssueTypeQualifier.Issue,
                    State = ItemState.Closed,
                    Comments = Octokit.Range.GreaterThan(issue_search_comments_min),
                    //Created = new DateRange(new DateTimeOffset(issue_search_created_min), new DateTimeOffset(issue_search_created_max)),
                    Repos = new() { repo.full_name },

                    Exclusions = new SearchIssuesRequestExclusions()
                    {
                        Labels = issue_search_exclude_labels
                    },

                    PerPage = issue_search_per_page,
                    Page = 0
                };

                repo.issues = await get_issues_for_repo(repo, client, issue_search_request);
            }

            return repo_infos;
        }

        private static bool print_api_rates = false;

        #region Repositories

        private static DateTime repo_search_creation_date_min = new DateTime(2021, 1, 1);

        private static int repo_search_start_page = 1; //4;
        private static int repo_search_pages = 10; //3;
        private static int repo_search_per_page = 100; //100 ; //count of repos per page
        private static int repo_search_stars_min = 10;

        private async static Task<List<repo_info>> get_repo_infos_from_search(GitHubClient client, SearchRepositoriesRequest search_request)
        {
            Console.WriteLine("- Getting repositories");

            List<repo_info> repo_infos = new();
            for (int i = repo_search_start_page ; i < repo_search_pages + repo_search_start_page; i++)
            {
                search_request.Page = i;
                Console.Write("  - page " + (i - repo_search_start_page) + " / " + repo_search_pages + " - ");

                SearchRepositoryResult? repositories;
                try
                {
                    repositories = await client.Search.SearchRepo(search_request);
                }
                catch (RateLimitExceededException ex) 
                {
                    //when there's no more rates throw away the list and just retry
                    //since every search is just one single request, it isn't too bad of a waste

                    Console.WriteLine("\n    - API rate limit error: " + ex.Message);
                    await handle_api_rate_limit(client);

                    if (i > 0) i -= 1; //rewind loop to try again when we have rates again
                    continue;
                }

                var ratelimit = client.GetLastApiInfo()?.RateLimit;
                if (print_api_rates && ratelimit != null) 
                {
                    Console.WriteLine($"\n    - API rate limit status: {ratelimit.Remaining}/{ratelimit.Limit} -- resets at: " + ratelimit.Reset.ToLocalTime());
                }

                if (repositories == null) 
                {
                    Console.WriteLine("  - error: null issues");
                    return repo_infos;
                }
                //if (repositories.IncompleteResults) Console.WriteLine("  - incomplete results");

                foreach (var item in repositories.Items)
                {
                    repo_info repo = new(item);
                    repo_infos.Add(repo);
                }

                Console.WriteLine("got " + repo_infos.Count + " repositories");
            }
            return repo_infos;
        }

        #endregion

        #region Issues

        private static DateTime issue_search_created_min = new DateTime(2021, 1, 1);
        private static DateTime issue_search_created_max = DateTime.Now;

        private static int issue_search_comments_min = 1;
        //private static int issue_search_comments_max = 100; //100;

        private static List<string> issue_search_exclude_labels = new() { "enhancement", "invalid", "duplicate","documentation","question",
            "wontfix","help wanted"}; // exclude these labels

        private static int issue_search_per_page = 100; // default
        private static int issue_search_pages = 10;

        private async static Task<List<issue_info>> get_issues_for_repo(repo_info repo, GitHubClient client, SearchIssuesRequest issue_search_request)
        {
            Console.WriteLine("    - Getting issues:");

            List<issue_info> issue_infos = new();

            // Perform the issue search
            for (int i = 0; i < issue_search_pages; i++)
            {
                issue_search_request.Page = i; // keep going through the pages

                SearchIssuesResult? issue_search_result;
                try 
                {
                    issue_search_result = await client.Search.SearchIssues(issue_search_request);
                }
                catch (RateLimitExceededException ex) 
                {
                    Console.WriteLine("    - API rate limit error: " + ex.Message);
                    await handle_api_rate_limit(client);

                    if (i > 0) i -= 1; //rewind loop to try again when we have rates again
                    continue;
                }

                var ratelimit = client.GetLastApiInfo()?.RateLimit;
                if (print_api_rates && ratelimit != null) 
                {
                    Console.WriteLine($"    - API rate limit status: {ratelimit.Remaining}/{ratelimit.Limit} -- resets at: " + ratelimit.Reset.ToLocalTime());
                }

                if (issue_search_result == null)
                {
                    Console.WriteLine("       - error: null issues");
                    return issue_infos;
                }
                if (issue_search_result.IncompleteResults) Console.WriteLine("       - incomplete results");
                if (issue_search_result.Items.Count == 0) continue;

                var issues = issue_search_result.Items;

                Console.WriteLine("       - page " + i + "/" + issue_search_pages + ", " + issues.Count + " issues/" + issue_search_result.TotalCount + " total");

                //stop if there are no more issues
                if (issue_search_result.Items.Count == 0) break;

                Console.WriteLine("       - processing comments...");

                foreach (var issue in issues)
                {
                    var issue_info = new issue_info(issue);

                    if (issue_info.valid_resolution_time == false) 
                    {
                        continue;
                    }

                    //store information about first comment creation time:
                    List<IssueComment> comments = new();
                    try 
                    {
                        // Construct the API endpoint URI:
                        // The final URI should be:
                        // https://api.github.com/repos/octokit/octokit.net/issues/1785/comments?per_page=1&page=1
                        Uri uri = new Uri($"https://api.github.com/repos/{repo.owner}/{repo.name}/issues/{issue.Number}/comments?per_page=1&page=1");

                        // Create and POST the response:
                        IApiResponse<List<IssueComment>> response = await client.Connection.Get<List<IssueComment>>(uri, null);
                        // This here should now be the first comment as an OctoKit object that we can use as any other:
                        comments = response.Body;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error while collecting comments: " + ex.Message);
                        // assume we ran out of rates
                        await handle_api_rate_limit(client);
                    }

                    if (comments.Count > 0) 
                    {
                        issue_info.first_comment_time = (comments[0].CreatedAt.DateTime - issue_info.created_at);
                    }

                    issue_infos.Add(issue_info);
                }
            }
            Console.WriteLine("    - issues done");

            return issue_infos;
        }

        #endregion
    }
}
