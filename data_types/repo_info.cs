using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bakalar {
    public class repo_info {
        public repo_info(){ }

        public repo_info(Repository repo) {
            id = repo.Id;
            owner = repo.Owner.Login;
            name = repo.Name;
            full_name = repo.FullName;
            issues = new();
        }
        public long id;

        public string owner;
        public string name;
        public string full_name;

        public List<issue_info> issues;
    }
}
