using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Injector
{
    public class FileSearcher
    {
        public IEnumerable<string> ListDirectoryFiles(string path, string searchPattern = "*.*")
        {
            var filesList = new List<string> { path };
            foreach (var file in ListDirectoryFilesInternal(path, searchPattern))
                filesList.Add(file);

            return filesList;
        }

        private static IEnumerable<string> ListDirectoryFilesInternal(string root, string searchPattern = "*.*")
        {
            var result = new List<string>();
            var pending = new Stack<string>();

            pending.Push(root);
            while (pending.Count != 0)
            {
                var path = pending.Pop();
                string[] next = null;
                try
                {
                    Thread.Sleep(1);
                    next = Directory.GetFiles(path, searchPattern);
                    result.AddRange(next);
                }
                catch
                {

                }

                if (next != null && next.Length != 0)
                    foreach (var file in next) yield return file;
                try
                {
                    next = Directory.GetDirectories(path);
                    foreach (var subdir in next)
                        if (IsBlackListPath(subdir))
                            pending.Push(subdir);
                }
                catch
                {

                }
            }
        }

        private static bool IsBlackListPath(string path)
        {
            const string blackListStrings = "Windows,$Recycle.Bin,MSOCache,PerfLogs,System Volume Information";
            foreach (var blackListString in blackListStrings.Split(','))
            {
                if (path.Contains(blackListString))
                    return false;
            }

            return true;
        }
    }
}
