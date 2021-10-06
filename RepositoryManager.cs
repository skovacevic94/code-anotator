using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace code_anotator
{
    public class RepositoryManager
    {
        public string CacheDir { get; }

        public List<Repository> Repositories { get; set; }

        public RepositoryManager(string cacheDir)
        {
            CacheDir = cacheDir;

            Repositories = new List<Repository>();
            foreach(var repoLocalDirectory in Directory.EnumerateDirectories(cacheDir))
            {
                Repositories.Add(new Repository(repoLocalDirectory));
            }
        }

        public void CloneRepository(string url)
        {
            string repoName = url.Split('/').Where(x => !string.IsNullOrWhiteSpace(x)).LastOrDefault();
            string downloadPath = Path.Combine(CacheDir, repoName);
            Directory.CreateDirectory(downloadPath);
            LibGit2Sharp.Repository.Clone(url, downloadPath);

            Repositories.Add(new Repository(downloadPath));
        }
    }
}
