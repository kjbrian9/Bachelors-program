using Bakalar;
using Bakalarkaversion2;
using MathNet.Numerics;
using MathNet.Numerics.Optimization;
using Octokit;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

public static class Program {
    public static bool ignore_saved_data = false;
    public static bool do_analysis = true;          

    public static async Task Main(string[] args) {
        process_args(args);

        List<repo_info> repos = await data.load_data(ignore_saved_data);

        if (do_analysis) {
            var statistics_results = analysis.calculate_statistics(repos);
            
            analysis.report(repos, statistics_results);
            analysis.save_to_csv(repos, statistics_results);
        }

        excel.save_to_csv(repos);
    
    }

    private static void process_args(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (arg == "--clean")
            {
                Console.WriteLine("New data requested.");
                ignore_saved_data = true;
            }
            if (arg == "--token")
            {
                if (i + 1 >= args.Length)
                {
                    Console.WriteLine("No argument for new token. Usage: --token <token>");
                    Environment.Exit(1);
                }
                else
                {
                    string new_token = args[i + 1];
                    data.write_token(new_token);
                }
            }
        }
    }
}