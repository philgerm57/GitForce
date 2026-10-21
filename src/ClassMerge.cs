using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace GitForce
{
    /// <summary>
    /// Manage various merge programs and merge execution
    /// </summary>
    class ClassMerge
    {
        // Common merge utilities:
        //
        // We bundle together Windows and Linux utilities
        // Since we build this app in 32-bit mode, on 64-bit OS Windows Program Files will return a (x86) folder variant
        private static readonly string ProgramFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        private static readonly string ProgramFilesX64 = ProgramFilesX86.Contains(" (x86)") ? ProgramFilesX86.Replace(" (x86)", "") : ProgramFilesX86;
        private static readonly List<AppHelper> Candidates = new List<AppHelper> {
                //   Config    Short name        Path                                                         Arguments
                // Windows OS (x32):
                new AppHelper( "p4merge",        Path.Combine(ProgramFilesX86,@"Perforce\P4Merge.exe"),       "%1 %2 %3 %4" ),
                new AppHelper( "WinMerge",       Path.Combine(ProgramFilesX86,@"WinMerge\WinMergeU.exe"),     "/e /x /u %2 %3 %4" ),
                new AppHelper( "BC3",            Path.Combine(ProgramFilesX86,@"Beyond Compare 3\BComp.exe"), "%2 %3 %1 %4" ),
                new AppHelper( "BC4",            Path.Combine(ProgramFilesX86,@"Beyond Compare 4\BComp.exe"), "%2 %3 %1 %4" ),
                new AppHelper( "KDiff3",         Path.Combine(ProgramFilesX86,@"KDiff3\kdiff3.exe"),          "%1 %2 %3 -o %4" ),
                // Windows OS (x64):
                new AppHelper( "p4merge",        Path.Combine(ProgramFilesX64,@"Perforce\P4Merge.exe"),       "%1 %2 %3 %4" ),
                new AppHelper( "WinMerge",       Path.Combine(ProgramFilesX64,@"WinMerge\WinMergeU.exe"),     "/e /x /u %2 %3 %4" ),
                new AppHelper( "BC3",            Path.Combine(ProgramFilesX64,@"Beyond Compare 3\BComp.exe"), "%2 %3 %1 %4" ),
                new AppHelper( "BC4",            Path.Combine(ProgramFilesX64,@"Beyond Compare 4\BComp.exe"), "%2 %3 %1 %4" ),
                new AppHelper( "BC5",            Path.Combine(ProgramFilesX64,@"Beyond Compare 5\BComp.exe"), "%2 %3 %1 %4" ),
                new AppHelper( "KDiff3",         Path.Combine(ProgramFilesX64,@"KDiff3\kdiff3.exe"),          "%1 %2 %3 -o %4" ),

                // Linux OS:
                new AppHelper( "KDiff3",         @"/usr/bin/kdiff3",            "%1 %2 %3 %4" ),
                new AppHelper( "TKDiff",         @"/usr/bin/tkdiff",            "%2 %3 -o %4" ),
                new AppHelper( "Meld",           @"/usr/bin/meld",              "%1 %2 %3 %4" ),
                new AppHelper( "xxdiff",         @"/usr/bin/xxdiff",            "%2 %3 -M %4" ),
                new AppHelper( "Diffuse",        @"/usr/bin/diffuse",           "%1 %2 %3"    ),
                new AppHelper( "BCompare",       @"/usr/bin/bcompare",          "%1 %2 %3"    ),

                new AppHelper( "Emerge",         @"/usr/bin/emerge",            "%1 %2 %3 %4" ), // Not tested!
                new AppHelper( "vimdiff",        @"/usr/bin/vimdiff",           "%1 %2 %3 %4" ), // Not tested!
                new AppHelper( "gvimdiff",       @"/usr/bin/gvimdiff",          "%1 %2 %3 %4" ), // Not tested!
                new AppHelper( "ecmerge",        @"/usr/bin/ecmerge",           "%1 %2 %3 %4" ), // Not tested!
                new AppHelper( "tortoisemerge",  @"/usr/bin/tortoisemerge",     "%1 %2 %3 %4" ), // Not tested!
                new AppHelper( "opendiff",       @"/usr/bin/opendiff",          "%1 %2 %3 %4" ), // Not tested!
        };

        /// <summary>
        /// Name and description of the built-in merge tool (FormMergeResolve)
        /// </summary>
        public const string BuiltInName = "GitForce";
        public const string BuiltInLabel = "(GitForce built-in merge tool)";
        public static readonly AppHelper BuiltIn = new AppHelper(BuiltInName, BuiltInLabel, string.Empty);

        private List<AppHelper> merge = new List<AppHelper>();

        /// <summary>
        /// Init code to be called on the application startup.
        /// Return false if no merge utility was found and user wanted to quit the app.
        /// </summary>
        public bool Initialize()
        {
            if (IsBuiltIn())
                return true;
            // Verify the application default merge utility
            AppHelper app = new AppHelper(Properties.Settings.Default.MergeAppHelper);
            if (File.Exists(app.Path))
            {
                Configure(app);
                return true;
            }

            // Search for any of the predefined tools
            merge = GetDetected();

            // If none of the pre-set merge apps are present, use the built-in merge tool
            if (merge.Count == 0)
            {
                Properties.Settings.Default.MergeAppHelper = BuiltIn.ToString();
                return true;
            }

            // Otherwise, at least one merge app is present, select it as default
            Properties.Settings.Default.MergeAppHelper = merge[0].ToString();

            Configure(merge[0]);
            return true;
        }

        /// <summary>
        /// Configure a given application helper to be a Git merge utility
        /// </summary>
        public static void Configure(AppHelper app)
        {
            // Configure application only if it is valid and external
            if (app.Name != string.Empty && app.Name != BuiltInName)
            {
                string path = app.Path.Replace('\\', '/');
                string usr = app.Args.
                    Replace("%1", "$BASE").
                    Replace("%2", "$LOCAL").
                    Replace("%3", "$REMOTE").
                    Replace("%4", "$MERGED");
                string arg = "'" + path + "' " + usr;
                ClassConfig.SetGlobal("mergetool." + app.Name + ".path", path);
                ClassConfig.SetGlobal("mergetool." + app.Name + ".cmd", arg);
                ClassConfig.SetGlobal("mergetool." + app.Name + ".trustExitCode", "false");

                // Set the default merge tool
                ClassConfig.SetGlobal("merge.tool", app.Name);
                ClassConfig.SetGlobal("mergetool.keepBackup", "false");
            }
        }

        /// <summary>
        /// Returns true if the built-in merge tool is selected instead of an external tool
        /// </summary>
        public static bool IsBuiltIn()
        {
            return new AppHelper(Properties.Settings.Default.MergeAppHelper).Name == BuiltInName;
        }

        /// <summary>
        /// Resolve the conflicts of the given files (all the unmerged files if none are given),
        /// with the built-in merge tool or with the external one through "git mergetool"
        /// </summary>
        public static void ResolveConflicts(ClassRepo repo, List<string> files)
        {
            List<string> unmerged = new ClassStatus(repo).GetUnmerged();
            List<string> todo = files == null ? new List<string>() : files.FindAll(f => unmerged.Contains(f));
            if (todo.Count == 0)
                todo = unmerged;
            if (todo.Count == 0)
            {
                MessageBox.Show("There are no conflicts to resolve.", "Resolve conflicts", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (IsBuiltIn())
            {
                foreach (string file in todo)
                    if (!FormMergeResolve.Resolve(repo, file))
                        break;      // Cancelled: leave the other files for later
            }
            else
                repo.RunCmd("mergetool " + GetMergeCmd() + (files != null && todo.Count == 1 ? " \"" + todo[0] + "\"" : string.Empty));
            App.DoRefresh();
        }

        /// <summary>
        /// Merge a branch into the current branch. If the merge stops on conflicts, offer to resolve them.
        /// </summary>
        public static void MergeBranch(ClassRepo repo, string branch)
        {
            ExecResult result = repo.RunCmd("merge --no-edit " + branch);
            List<string> unmerged = new ClassStatus(repo).GetUnmerged();
            if (unmerged.Count > 0)
            {
                if (MessageBox.Show(string.Format("Merging {0} stopped on conflicts in {1} file(s):\n\n{2}\n\nResolve them now?",
                        branch, unmerged.Count, string.Join("\n", unmerged.GetRange(0, Math.Min(10, unmerged.Count)))),
                        "Merge", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    ResolveConflicts(repo, unmerged);
                else
                    App.PrintStatusMessage("Resolve the conflicts from the Commits tab (Resolve menu), then submit the merge.", MessageType.General);
            }
            else if (result.Success())
                App.PrintStatusMessage("Merged " + branch, MessageType.General);
            App.DoRefresh();
        }

        /// <summary>
        /// Return a proper merge command.
        /// This function is called from the actual menu item to merge/resolve a file.
        /// </summary>
        public static string GetMergeCmd()
        {
            // Get the application default merge utility
            AppHelper app = new AppHelper(Properties.Settings.Default.MergeAppHelper);
            string cmd = string.Format(" --tool={0} --no-prompt ", app.Name);

            return cmd;
        }

        /// <summary>
        /// Returns a list of detected Merge application helpers
        /// </summary>
        public static List<AppHelper> GetDetected()
        {
            return AppHelper.Scan(Candidates);
        }
    }
}
