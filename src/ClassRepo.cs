using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace GitForce
{
    /// <summary>
    /// Class containing a single repository and a set of functions operating on it
    /// </summary>
    [Serializable]
    public class ClassRepo : IComparable
    {
        /// <summary>
        /// Root local directory of the repository
        /// </summary>
        private string Root;

        /// <summary>
        /// User.name configuration setting for this repo
        /// </summary>
        public string UserName;

        /// <summary>
        /// User.email coniguration setting for this repo
        /// </summary>
        public string UserEmail;

        /// <summary>
        /// Set of remotes associated with this repository
        /// </summary>
        public readonly ClassRemotes Remotes = new ClassRemotes();

        /// <summary>
        /// Set of active commits currently existing for this repo
        /// </summary>
        public readonly ClassCommits Commits = new ClassCommits();

        /// <summary>
        /// Set of branches for the current repo
        /// </summary>
        public readonly ClassBranches Branches = new ClassBranches();

        /// <summary>
        /// Set of submodules for the current repo. This is refreshed
        /// on every App global refresh, so it does not need to be preserved across sessions.
        /// </summary>
        [NonSerialized]
        public ClassSubmodules Submodules;

        /// <summary>
        /// Current format can be tree view or list view
        /// </summary>
        public bool IsTreeView = true;

        /// <summary>
        /// Current sorting rule for the view
        /// </summary>
        public GitDirectoryInfo.SortBy SortBy = GitDirectoryInfo.SortBy.Name;

        /// <summary>
        /// Stores a set of paths that are expanded in the left pane view.
        /// Although used only by the view, keeping it here saves it across the sessions.
        /// WAR: This is marked as Non-Serialized since Mono 2.6.7 does not support serialization of HashSet.
        /// </summary>
        [NonSerialized]
        private HashSet<string> viewExpansionSet = new HashSet<string>();

        /// <summary>
        /// Stats of all files known to git in this repo. This status is refreshed
        /// on every App global refresh, so it does not need to be preserved across sessions.
        /// </summary>
        [NonSerialized]
        public ClassStatus Status;

        /// <summary>
        /// Per-row background colors for the Revisions tab, keyed by commit SHA.
        /// Session-only: marked NonSerialized so it is not written to the workspace
        /// file and is not restored across app runs. Lazily initialized because
        /// BinaryFormatter does not run field initializers on deserialization.
        /// </summary>
        [NonSerialized]
        private Dictionary<string, Color> _revColors;
        public Dictionary<string, Color> RevColors
        {
            get { return (_revColors ?? (_revColors = new Dictionary<string, Color>())); }
        }

        /// <summary>
        /// Class constructor
        /// </summary>
        public ClassRepo(string newRoot)
        {
            Root = newRoot;
        }

        /// <summary>
        /// Returns or sets the directory path of the repo
        /// </summary>
        /// <returns>Path to the git repository</returns>
        public string Path
        {
            get { return ClassUtils.GetCleanPath(Root); }
            set { Root = ClassUtils.GetCleanPath(value); }
        }

        /// <summary>
        /// Initializes repo class with user.name and user.email fields.
        /// Returns True if the repo appears valid and these settings are read.
        /// </summary>
        public bool Init()
        {
            // We assume that a valid git repo will always have core.bare entry
            if (ClassConfig.GetLocal(this, "core.bare") == string.Empty)
                return false;
            UserName = ClassConfig.GetLocal(this, "user.name");
            UserEmail = ClassConfig.GetLocal(this, "user.email");
            Status = new ClassStatus(this);
            return true;
        }

        /// <summary>
        /// ToString override returns the repository root path.
        /// </summary>
        public override string ToString()
        {
            return Root;
        }

        /// <summary>
        /// Implement default comparator so these classes can be sorted by their (root) name
        /// </summary>
        public int CompareTo(object obj)
        {
            if (obj == null) return 1;
            ClassRepo a = obj as ClassRepo;
            return a != null ? ToString().CompareTo(a.ToString()) : 1;
        }

        /// <summary>
        /// Mark a specific path as expanded in the view
        /// </summary>
        public void ExpansionSet(string path)
        {
            viewExpansionSet.Add(path);
        }

        /// <summary>
        /// Mark a specific path as collapsed in the view,
        /// or remove all paths from the list of expanded paths (if the path given is null)
        /// </summary>
        public void ExpansionReset(string path)
        {
            if (path == null)
                viewExpansionSet = new HashSet<string>();
            else
                viewExpansionSet.Remove(path);
        }

        /// <summary>
        /// Return true if the path is marked as expanded
        /// </summary>
        public bool IsExpanded(string path)
        {
            return viewExpansionSet.Contains(path);
        }

        /// <summary>
        /// Converts a list of (relative) files into a quoted list,
        /// further flattened into a string suitable to send to a git command.
        /// </summary>
        private string QuoteAndFlattenPaths(List<string> files)
        {
            List<string> quoted = files.Select(file => "\"" + file + "\"").ToList();
            return string.Join(" ", quoted.ToArray());
        }

        /// <summary>
        /// Partition files into main repo files and submodule files, then run the given
        /// git command on each group in the appropriate directory context.
        /// </summary>
        /// <param name="cmdPrefix">Git command prefix (e.g., "add --")</param>
        /// <param name="files">List of file paths relative to repo root</param>
        private void RunCmdWithSubmoduleSupport(string cmdPrefix, List<string> files)
        {
            // Partition files: main repo vs submodules
            List<string> mainRepoFiles = new List<string>();
            Dictionary<string, List<string>> submoduleFiles = new Dictionary<string, List<string>>();

            if (Submodules != null)
            {
                foreach (string file in files)
                {
                    ClassSubmodules.Submodule sm;
                    string relativePath;
                    if (Submodules.GetContainingSubmodule(file, out sm, out relativePath))
                    {
                        if (!submoduleFiles.ContainsKey(sm.Path))
                            submoduleFiles[sm.Path] = new List<string>();
                        submoduleFiles[sm.Path].Add(relativePath);
                    }
                    else
                        mainRepoFiles.Add(file);
                }
            }
            else
                mainRepoFiles = files;

            // Run command for main repo files
            if (mainRepoFiles.Count > 0)
            {
                string list = QuoteAndFlattenPaths(mainRepoFiles);
                RunCmd(cmdPrefix + " " + list);
            }

            // Run command for each submodule
            foreach (var kvp in submoduleFiles)
            {
                string smPath = kvp.Key;
                string list = QuoteAndFlattenPaths(kvp.Value);
                RunCmd(cmdPrefix + " " + list, false, smPath);
            }
        }

        /// <summary>
        /// Add untracked files to Git repository
        /// </summary>
        public void GitAdd(List<string> files)
        {
            string list = QuoteAndFlattenPaths(files);
            App.PrintStatusMessage("Adding " + list, MessageType.General);
            RunCmdWithSubmoduleSupport("add --", files);
            // Any git command that adds/updates files in the index might cause file check for TABs
            ClassTabCheck.CheckForTabs(files);
        }

        /// <summary>
        /// Update modified files
        /// </summary>
        public void GitUpdate(List<string> files)
        {
            string list = QuoteAndFlattenPaths(files);
            App.PrintStatusMessage("Updating " + list, MessageType.General);
            RunCmdWithSubmoduleSupport("add --", files);
            // Any git command that adds/updates files in the index might cause file check for TABs
            ClassTabCheck.CheckForTabs(files);
        }

        /// <summary>
        /// Stage files and folders: put every change under the given paths into the index,
        /// including new and deleted files
        /// </summary>
        public void GitStage(List<string> paths)
        {
            App.PrintStatusMessage("Staging " + QuoteAndFlattenPaths(paths), MessageType.General);
            RunCmdWithSubmoduleSupport("add -A --", paths);
            // Any git command that adds/updates files in the index might cause file check for TABs
            ClassTabCheck.CheckForTabs(paths);
        }

        /// <summary>
        /// Unstage files and folders: take every change under the given paths out of the index,
        /// leaving the files in the working directory untouched
        /// </summary>
        public void GitUnstage(List<string> paths)
        {
            App.PrintStatusMessage("Unstaging " + QuoteAndFlattenPaths(paths), MessageType.General);
            // Before the first commit there is no HEAD to reset to: simply empty the index instead
            if (Run("rev-parse --verify -q HEAD").Success())
                RunCmdWithSubmoduleSupport("reset -q HEAD --", paths);
            else
                RunCmdWithSubmoduleSupport("rm --cached -r -q --", paths);
        }

        /// <summary>
        /// Add files and folders to the .gitignore file at the root of the repository.
        /// Files are added by their path; folders as "folder/*" so that all of their files and
        /// subfolders are ignored. Patterns are anchored to the root with a leading '/'.
        /// Returns the list of patterns that were added (patterns already present are skipped).
        /// </summary>
        public List<string> GitIgnore(List<string> paths)
        {
            string ignoreFile = System.IO.Path.Combine(Root, ".gitignore");
            List<string> existing = File.Exists(ignoreFile)
                ? File.ReadAllLines(ignoreFile).Select(s => s.Trim()).ToList()
                : new List<string>();

            List<string> added = new List<string>();
            foreach (string p in paths)
            {
                string rel = p.Replace('\\', '/').Trim('/');
                if (rel.Length == 0 || rel == ".")
                    continue;   // Ignoring the whole repository makes no sense
                string pattern = "/" + EscapeIgnorePattern(rel);
                if (Directory.Exists(System.IO.Path.Combine(Root, p)))
                    pattern += "/*";
                if (!existing.Contains(pattern) && !added.Contains(pattern))
                    added.Add(pattern);
            }
            if (added.Count == 0)
                return added;

            AppendToGitignore(added);
            App.PrintStatusMessage("Added to .gitignore: " + string.Join(" ", added), MessageType.General);
            return added;
        }

        /// <summary>
        /// Append lines to the .gitignore file at the root of the repository
        /// </summary>
        /// <summary>
        /// Ignore whole folders with all of their content: add "/folder/" to the .gitignore file at
        /// the root. Returns the patterns that were added.
        /// </summary>
        public List<string> GitIgnoreFolders(List<string> folders)
        {
            string ignoreFile = System.IO.Path.Combine(Root, ".gitignore");
            List<string> existing = File.Exists(ignoreFile)
                ? File.ReadAllLines(ignoreFile).Select(s => s.Trim()).ToList()
                : new List<string>();
            List<string> added = new List<string>();
            foreach (string folder in folders)
            {
                string rel = folder.Replace('\\', '/').Trim('/');
                if (rel.Length == 0 || rel == ".")
                    continue;
                string pattern = "/" + EscapeIgnorePattern(rel) + "/";
                // The same folder may already be ignored with "/folder/*" by the Ignore command
                if (!existing.Contains(pattern) && !existing.Contains(pattern + "*") && !added.Contains(pattern))
                    added.Add(pattern);
            }
            if (added.Count > 0)
            {
                AppendToGitignore(added);
                App.PrintStatusMessage("Added to .gitignore: " + string.Join(" ", added), MessageType.General);
            }
            return added;
        }

        /// <summary>
        /// Ignore all the files with the given extensions (".bak"), in every folder: add "*.bak"
        /// to the .gitignore file at the root. Returns the patterns that were added.
        /// </summary>
        public List<string> GitIgnoreExtensions(List<string> extensions)
        {
            string ignoreFile = System.IO.Path.Combine(Root, ".gitignore");
            List<string> existing = File.Exists(ignoreFile)
                ? File.ReadAllLines(ignoreFile).Select(s => s.Trim()).ToList()
                : new List<string>();
            List<string> added = new List<string>();
            foreach (string ext in extensions)
            {
                string pattern = "*" + EscapeIgnorePattern(ext.StartsWith(".") ? ext : "." + ext);
                if (!existing.Contains(pattern) && !added.Contains(pattern))
                    added.Add(pattern);
            }
            if (added.Count > 0)
            {
                AppendToGitignore(added);
                App.PrintStatusMessage("Added to .gitignore: " + string.Join(" ", added), MessageType.General);
            }
            return added;
        }

        private void AppendToGitignore(List<string> lines)
        {
            string ignoreFile = System.IO.Path.Combine(Root, ".gitignore");
            // Start on a new line if the file does not end with one
            StringBuilder text = new StringBuilder();
            if (File.Exists(ignoreFile))
            {
                string content = File.ReadAllText(ignoreFile);
                if (content.Length > 0 && !content.EndsWith("\n"))
                    text.Append(Environment.NewLine);
            }
            foreach (string line in lines)
                text.Append(line).Append(Environment.NewLine);
            File.AppendAllText(ignoreFile, text.ToString());
        }

        /// <summary>
        /// Stop ignoring files and folders, so they show up again as untracked.
        /// The ignore rule is removed when it is written for that path only, in a .gitignore or
        /// exclude file of this repository; a broader rule ("*.log", "bin/") or a rule from
        /// outside the repository is overridden by adding a "!path" rule to the root .gitignore.
        /// Returns the paths that are still ignored (git can not re-include a file whose parent
        /// folder is ignored).
        /// </summary>
        public List<string> GitUnignore(List<string> paths)
        {
            List<string> still = new List<string>();
            foreach (string p in paths)
            {
                string rel = p.Replace('\\', '/').Trim('/');
                bool isDir = Directory.Exists(System.IO.Path.Combine(Root, p));
                string negation = null;
                for (int attempt = 0; ; attempt++)
                {
                    string source, pattern;
                    int line;
                    if (!GetIgnoreRule(rel, isDir, out source, out line, out pattern))
                        break;          // No longer ignored
                    if (attempt >= 10 || negation != null)
                    {
                        // A "!path" rule has no effect inside an ignored folder: do not leave it behind
                        if (negation != null)
                            RemoveLastLine(System.IO.Path.Combine(Root, ".gitignore"), negation);
                        still.Add(p);
                        break;
                    }

                    string file = System.IO.Path.GetFullPath(System.IO.Path.Combine(Root, source));
                    string target = PathRelativeToIgnoreFile(file, rel);
                    if (target != null && IsRuleForPath(pattern, target))
                    {
                        RemoveLine(file, line);
                        App.PrintStatusMessage(string.Format("Removed \"{0}\" from {1}", pattern, source), MessageType.General);
                    }
                    else
                    {
                        negation = "!/" + EscapeIgnorePattern(rel) + (isDir ? "/" : "");
                        AppendToGitignore(new List<string> { negation });
                        App.PrintStatusMessage(string.Format("Added \"{0}\" to .gitignore (overrides \"{1}\" in {2})", negation, pattern, source), MessageType.General);
                    }
                }
            }
            return still;
        }

        /// <summary>
        /// Find the rule that makes git ignore a path. For a folder whose content is ignored by
        /// a rule like "folder/*" (the folder itself does not match), look at its content.
        /// Returns false if the path is not ignored.
        /// </summary>
        private bool GetIgnoreRule(string rel, bool isDir, out string source, out int line, out string pattern)
        {
            source = pattern = string.Empty;
            line = 0;
            List<string> probes = new List<string> { rel };
            if (isDir)
                probes.Add(rel + "/.gitforce-probe");
            foreach (string probe in probes)
            {
                ExecResult result = Run("check-ignore -v --no-index -- \"" + probe + "\"");
                if (!result.Success())
                    continue;
                // Output format: <source>:<line number>:<pattern><TAB><path>
                string first = result.stdout.Split('\n')[0];
                int tab = first.IndexOf('\t');
                System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(
                    tab >= 0 ? first.Substring(0, tab) : first, @"^(.*):(\d+):(.*)$");
                if (!m.Success)
                    continue;
                if (m.Groups[3].Value.StartsWith("!"))
                    return false;   // The last matching rule is a negation: not ignored
                source = m.Groups[1].Value;
                line = int.Parse(m.Groups[2].Value);
                pattern = m.Groups[3].Value;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the path relative to the folder of the ignore file that would contain its rule,
        /// or null if that file is not one of this repository's .gitignore or exclude files
        /// </summary>
        private string PathRelativeToIgnoreFile(string file, string rel)
        {
            string root = System.IO.Path.GetFullPath(Root).TrimEnd(System.IO.Path.DirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;
            if (!file.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return null;
            string fileRel = file.Substring(root.Length).Replace('\\', '/');
            if (fileRel == ".git/info/exclude")
                return rel;
            if (System.IO.Path.GetFileName(file) != ".gitignore")
                return null;
            string dir = fileRel.Length > ".gitignore".Length ? fileRel.Substring(0, fileRel.Length - ".gitignore".Length - 1) : string.Empty;
            if (dir.Length == 0)
                return rel;
            return rel.StartsWith(dir + "/", StringComparison.OrdinalIgnoreCase) ? rel.Substring(dir.Length + 1) : null;
        }

        /// <summary>
        /// Returns true if an ignore pattern names exactly this path (relative to its ignore file),
        /// as the rules GitIgnore() writes do: "/path", "/path/", "/path/*", "dir/file".
        /// A pattern without a slash ("bin", "*.log") matches at any level, so it is not.
        /// </summary>
        private static bool IsRuleForPath(string pattern, string target)
        {
            string p = pattern.Trim();
            foreach (string suffix in new[] { "/**", "/*", "/" })
                if (p.EndsWith(suffix) && !p.EndsWith("\\" + suffix))
                {
                    p = p.Substring(0, p.Length - suffix.Length);
                    break;
                }
            bool anchored = p.StartsWith("/") || p.Contains("/");
            p = p.TrimStart('/');
            StringBuilder unescaped = new StringBuilder();
            for (int i = 0; i < p.Length; i++)
            {
                if (p[i] == '\\' && i + 1 < p.Length)
                    i++;
                unescaped.Append(p[i]);
            }
            return anchored && string.Equals(unescaped.ToString(), target, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Remove the last line of a text file that has the given content
        /// </summary>
        private static void RemoveLastLine(string file, string content)
        {
            List<string> lines = File.ReadAllText(file).Replace("\r\n", "\n").Split('\n').ToList();
            int index = lines.FindLastIndex(s => s.Trim() == content);
            if (index >= 0)
                RemoveLine(file, index + 1);
        }

        /// <summary>
        /// Remove a line (1-based) from a text file, keeping its line endings
        /// </summary>
        private static void RemoveLine(string file, int line)
        {
            string text = File.ReadAllText(file);
            string nl = text.Contains("\r\n") ? "\r\n" : "\n";
            List<string> lines = text.Replace("\r\n", "\n").Split('\n').ToList();
            if (line >= 1 && line <= lines.Count)
                lines.RemoveAt(line - 1);
            File.WriteAllText(file, string.Join(nl, lines));
        }

        /// <summary>
        /// Escape the characters that have a special meaning in a .gitignore pattern
        /// so that a path matches only itself
        /// </summary>
        private static string EscapeIgnorePattern(string path)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in path)
            {
                if (c == '*' || c == '?' || c == '[' || c == '\\')
                    sb.Append('\\');
                sb.Append(c);
            }
            string s = sb.ToString();
            if (s.StartsWith("#") || s.StartsWith("!"))
                s = "\\" + s;
            if (s.EndsWith(" "))
                s = s.Substring(0, s.Length - 1) + "\\ ";
            return s;
        }

        /// <summary>
        /// Returns the files under the given paths that are tracked by git
        /// </summary>
        public List<string> GetTrackedFiles(List<string> paths)
        {
            ExecResult result = Run("ls-files -- " + QuoteAndFlattenPaths(paths));
            if (!result.Success())
                return new List<string>();
            return result.stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        /// <summary>
        /// Stop tracking files and folders without deleting them from the working directory
        /// </summary>
        public void GitUntrack(List<string> paths)
        {
            App.PrintStatusMessage("Untracking " + QuoteAndFlattenPaths(paths), MessageType.General);
            RunCmdWithSubmoduleSupport("rm --cached -r -q --", paths);
        }

        /// <summary>
        /// Delete a list of files
        /// </summary>
        public void GitDelete(List<string> files) { GitDelete("", files); }
        public void GitDelete(string tag, List<string> files)
        {
            string list = QuoteAndFlattenPaths(files);
            App.PrintStatusMessage("Removing " + list, MessageType.General);
            string cmd = string.IsNullOrEmpty(tag) ? "rm --" : "rm " + tag + " --";
            RunCmdWithSubmoduleSupport(cmd, files);
        }

        /// <summary>
        /// Rename a list of files
        /// </summary>
        public void GitRename(List<string> files)
        {
            string list = QuoteAndFlattenPaths(files);
            App.PrintStatusMessage("Renaming " + list, MessageType.General);
            RunCmdWithSubmoduleSupport("add --", files);
            // Any git command that adds/updates files in the index might cause file check for TABs
            ClassTabCheck.CheckForTabs(files);
        }

        /// <summary>
        /// Moves a file to a different name or different location
        /// </summary>
        public void GitMove(string srcFile, string dstFile)
        {
            App.PrintStatusMessage(string.Format("Moving {0} to {1}", srcFile, dstFile), MessageType.General);
            RunCmd("mv \"" + srcFile + "\" \"" + dstFile + "\"");
        }

        /// <summary>
        /// Checkout a list of files
        /// </summary>
        public void GitCheckout(string options, List<string> files)
        {
            string list = QuoteAndFlattenPaths(files);
            App.PrintStatusMessage("Checkout " + options + " " + list, MessageType.General);
            string cmd = string.IsNullOrEmpty(options) ? "checkout --" : "checkout " + options + " --";
            RunCmdWithSubmoduleSupport(cmd, files);
        }

        /// <summary>
        /// Revert a list of files
        /// </summary>
        public void GitRevert(List<string> files)
        {
            string list = QuoteAndFlattenPaths(files);
            App.PrintStatusMessage("Reverting " + list, MessageType.General);
            RunCmdWithSubmoduleSupport("checkout --", files);
        }

        /// <summary>
        /// Reset a list of files to a specific head.
        /// Returns true if a git command succeeded, false otherwise.
        /// </summary>
        public bool GitReset(string head, List<string> files)
        {
            string list = QuoteAndFlattenPaths(files);
            App.PrintStatusMessage(string.Format("Resetting to {0}: {1}", head, list), MessageType.General);
            RunCmdWithSubmoduleSupport("reset " + head + " --", files);
            return true; // Note: With submodule support, individual results are harder to track
        }

        /// <summary>
        /// Run the external diff command on a given file or a list of files
        /// </summary>
        public void GitDiff(string tag, List<string> files)
        {
            string list = QuoteAndFlattenPaths(files);
            if (list == "\"\"") // For now, we don't want to match all paths but only diff selected files
            {
                App.PrintStatusMessage("Diffing: No files selected and we don't want to match all paths.", MessageType.General);
                return;
            }
            App.PrintStatusMessage("Diffing " + list, MessageType.General);

            // Check if files are in a submodule (handle first file for simplicity)
            if (Submodules != null && files.Count > 0)
            {
                ClassSubmodules.Submodule sm;
                string relativePath;
                if (Submodules.GetContainingSubmodule(files[0], out sm, out relativePath))
                {
                    // Run diff in submodule context
                    List<string> smFiles = new List<string>();
                    foreach (string file in files)
                    {
                        string relPath;
                        ClassSubmodules.Submodule dummy;
                        if (Submodules.GetContainingSubmodule(file, out dummy, out relPath))
                            smFiles.Add(relPath);
                    }
                    string smList = QuoteAndFlattenPaths(smFiles);
                    ClassDiff.Show(smList, tag + " -- " + smList,
                        args => Run(args, false, sm.Path),
                        cmd => { Directory.SetCurrentDirectory(sm.Path); ClassGit.Run(cmd, true); });
                    return;
                }
            }

            ClassDiff.Show(list, tag + " -- " + list, args => Run(args), cmd => RunCmd(cmd, true));
        }

        /// <summary>
        /// Commit a list of files.
        /// Returns true if commit succeeded, false otherwise.
        /// </summary>
        public bool GitCommit(string cmd, bool isAmend, List<string> files)
        {
            string list = QuoteAndFlattenPaths(files);
            App.PrintStatusMessage("Submit " + list, MessageType.General);

            // A commit of given paths takes their content from the disk: an untracked file whose
            // removal is staged (untrack) would be added back. Move such files away during the commit.
            List<KeyValuePair<string, string>> moved = new List<KeyValuePair<string, string>>();
            try
            {
                foreach (string file in files)
                {
                    string full = System.IO.Path.Combine(Root, file);
                    if (Status != null && Status.Xcode(file) == 'D' && File.Exists(full))
                    {
                        string aside = full + ".gitforce-" + Guid.NewGuid().ToString("N").Substring(0, 8);
                        File.Move(full, aside);
                        moved.Add(new KeyValuePair<string, string>(full, aside));
                    }
                }
                return GitCommitFiles(cmd, isAmend, files, list);
            }
            finally
            {
                foreach (var m in moved)
                {
                    try { File.Move(m.Value, m.Key); }
                    catch (Exception ex) { App.PrintStatusMessage("Unable to restore " + m.Key + " (saved as " + m.Value + "): " + ex.Message, MessageType.Error); }
                }
            }
        }

        private bool GitCommitFiles(string cmd, bool isAmend, List<string> files, string list)
        {
            ExecResult result;

            // See below Run() for the description of the problem with long commands.
            // The Run() function breaks any command into chunks of 2000 characters or less
            // and issues them separately. This can be done on every command except 'commit'
            // since that would introduce multiple commits, which is probably not what the user
            // wants. Hence, we break commit at this level into an initial commit of a single
            // file (the first file on the list), and append for each successive chunk.
            if (isAmend == false && list.Length >= 2000)
            {
                result = RunCmd("commit " + cmd + " -- " + "\"" + files[0] + "\"");
                if (result.Success() == false)
                    return false;
                isAmend = true;
                files.RemoveAt(0);
                list = QuoteAndFlattenPaths(files);
            }
            result = RunCmd(string.Format("commit {0} {1} -- {2}", cmd, isAmend ? "--amend" : "", list));
            return result.Success();
        }

        /// <summary>
        /// Run a git command with logging to status window.
        /// Use this function with all user-initiated commands.
        /// </summary>
        /// <param name="args">Git command arguments</param>
        /// <param name="async">If true, run asynchronously</param>
        /// <param name="workDir">Working directory (null = repo root)</param>
        /// <returns>ExecResult with stdout, stderr, and return code</returns>
        public ExecResult RunCmd(string args, bool async = false, string workDir = null)
        {
            // Print the actual command line to the status window only if user selected that setting
            if (Properties.Settings.Default.logCommands)
            {
                string msg = "git " + args;
                if (workDir != null && workDir != Root)
                    msg += " (in " + workDir + ")";
                App.PrintStatusMessage(msg, MessageType.Command);
            }

            // Run the command and print the response to the status window
            ExecResult result = Run(args, async, workDir);
            if (result.stdout.Length > 0)
                App.PrintStatusMessage(result.stdout, MessageType.Output);

            // If the command caused an error, print it also
            if (result.Success() == false)
                App.PrintStatusMessage(result.stderr, MessageType.Error);

            return result;
        }

        /// <summary>
        /// Run a git command in the context of a repository.
        /// </summary>
        /// <param name="args">Git command arguments</param>
        /// <param name="async">If true, run asynchronously</param>
        /// <param name="workDir">Working directory (null = repo root)</param>
        /// <returns>ExecResult with stdout, stderr, and return code</returns>
        public ExecResult Run(string args, bool async = false, string workDir = null)
        {
            ExecResult output = new ExecResult();
            try
            {
                Directory.SetCurrentDirectory(workDir ?? Root);

                // Set the HTTPS password
                string password = Remotes.GetPassword("");
                ClassUtils.AddEnvar("PASSWORD", password);

                // The Windows limit to the command line argument length is about 8K
                // We may hit that limit when doing operations on a large number of files.
                //
                // However, when sending a long list of files, git was hanging unless
                // the total length was much less than that, so I set it to about 2000 chars
                // which seemed to work fine.

                if (args.Length < 2000)
                    return ClassGit.Run(args, async);

                // Partition the args into "[command] -- [set of file chunks < 2000 chars]"
                // Basically we have to rebuild the command into multiple instances with
                // same command but with file lists not larger than about 2K
                int sep = args.IndexOf(" -- ", StringComparison.Ordinal);
                if (sep < 0)
                {
                    // There is no file list to chunk on. Slicing the command at a fixed offset
                    // would silently corrupt it, so run it whole and record that we saw it:
                    // a command this long without a pathspec separator is a bug elsewhere.
                    App.PrintLogMessage("Long git command without a ' -- ' separator, running it unsplit: " + args, MessageType.Error);
                    return ClassGit.Run(args, async);
                }
                int i = sep + 3;
                string cmd = args.Substring(0, i + 1);
                args = args.Substring(i);       // We separate git command up to and until the list of files

                App.PrintLogMessage("Processing large amount of files: please wait...", MessageType.General);

                // Add files individually up to the length limit using the starting " file delimiter
                string[] files = args.Split(new [] {" \""}, StringSplitOptions.RemoveEmptyEntries);
                // Note: files in the list are now stripped from their initial " character!
                i = 0;
                do
                {
                    StringBuilder batch = new StringBuilder(2100);
                    while (batch.Length < 2000 && i < files.Length)
                        batch.Append("\"" + files[i++] + " ");

                    output = ClassGit.Run(cmd + batch, async);
                    if (output.Success() == false)
                        break;
                } while (i < files.Length);
            }
            catch (Exception ex)
            {
                App.PrintLogMessage(ex.Message, MessageType.Error);
            }
            return output;
        }
    }
}
