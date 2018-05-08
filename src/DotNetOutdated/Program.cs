using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetOutdated
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var projectFiles = new HashSet<string>() { ".csproj", ".fsproj" };
            string firstProjectFile = Directory.EnumerateFiles("./").FirstOrDefault(x => projectFiles.Contains(Path.GetExtension(x)));

            if (firstProjectFile == null)
            {
                Console.WriteLine("No project file found");
                return;
            }

            var dependencies = ProjectParser.GetAllDependencies(firstProjectFile);
            var client = new HttpNuGetClient();  
            var requests = dependencies.Select(x => client.GetPackageInfo(x.Name));
            var responses = Task.WhenAll(requests).Result.Where(response => response != null).ToArray();
            var data = new List<DependencyStatus>();

            for (int i = 0; i < responses.Length; i++)
            {
                var dependency = dependencies.ElementAt(i);
                var package = responses[i];
                var status = DependencyStatus.Check(dependency, package);

                if (status.LatestVersion > status.Dependency.CurrentVersion)
                {
                    data.Add(status);
                }
            }

            data.ToStringTable(
                new[] { "Package", "Current", "Wanted", "Stable", "Latest"},
                r => {
                    if (r.Dependency.CurrentVersion < r.WantedVersion)
                        return ConsoleColor.Yellow;

                    if (r.Dependency.CurrentVersion == r.WantedVersion &&
                        r.Dependency.CurrentVersion < r.StableVersion)
                        return ConsoleColor.Red;

                    return ConsoleColor.White;
                }, 
                a => a.Package.Name, 
                a => a.Dependency.CurrentVersion, 
                a => a.WantedVersion, 
                a => a.StableVersion, 
                a => a.LatestVersion
            );
            Console.ResetColor();
        }
    }
}