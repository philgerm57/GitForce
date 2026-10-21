using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace GitForce
{
    /// <summary>
    /// Operations that change the history and the references of a repository: edit the message
    /// of a commit, rename a branch, create, rename and delete tags, create a branch at a commit.
    /// </summary>
    public static class ClassHistoryEdit
    {
        #region Edit the message of a commit

        /// <summary>
        /// Let the user edit the message of a commit and rewrite it. The last commit is amended;
        /// an older commit of the current branch is rewritten with a rebase that rewords it,
        /// which also rewrites the commits after it.
        /// </summary>
        public static bool EditMessage(ClassRepo repo, string sha)
        {
            ExecResult info = repo.Run("log -1 --format=%H%x1f%P%x1f%B " + sha);
            if (!info.Success())
                return false;
            string[] f = info.stdout.Split(new[] { '\x1f' }, 3);
            if (f.Length < 3)
                return false;
            string full = f[0].Trim();
            string[] parents = f[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string message = f[2].TrimEnd();

            string head = repo.Run("rev-parse HEAD").stdout.Trim();
            bool isHead = full == head;
            if (!isHead)
            {
                if (!repo.Run("merge-base --is-ancestor " + full + " HEAD").Success())
                {
                    MessageBox.Show("Only the commits of the current branch can be edited. Switch to the branch of this commit first.",
                        "Edit commit message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }
                if (parents.Length > 1)
                {
                    MessageBox.Show("The message of a merge commit can only be edited when it is the last commit of the branch.",
                        "Edit commit message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }
                if (new ClassStatus(repo).IsMergeState())
                {
                    MessageBox.Show("Finish or abort the merge in progress first.", "Edit commit message",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }
            }

            string newMessage = FormTextInput.Ask("Edit commit message",
                "Message of commit " + full.Substring(0, Math.Min(7, full.Length)) + ":", message, true);
            if (newMessage == null || newMessage.Trim() == message.Trim() || newMessage.Trim().Length == 0)
                return false;

            // A commit that is already on a remote branch: rewriting it changes the shared history
            string remotes = repo.Run("branch -r --contains " + full).stdout.Trim();
            if (remotes.Length > 0 && MessageBox.Show(
                    "This commit is already pushed (" + remotes.Split('\n')[0].Trim() + ").\n\n" +
                    "Changing its message rewrites the history: the branch will have to be force-pushed, " +
                    "and other people working on it will have to reset their copy.\n\nChange the message anyway?",
                    "Edit commit message", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return false;

            string msgFile = Path.Combine(Path.GetTempPath(), "GitForce-msg-" + Guid.NewGuid().ToString("N") + ".txt");
            try
            {
                File.WriteAllText(msgFile, newMessage.Replace("\r\n", "\n").TrimEnd() + "\n", new UTF8Encoding(false));
                string posix = msgFile.Replace('\\', '/');
                ExecResult result;
                if (isHead)
                {
                    // --only: change the message, leave what is staged out of the commit
                    result = RunGit(repo, "commit --amend --only -q -F \"" + posix + "\"", null);
                }
                else
                {
                    // Rebase from the parent, turning the "pick" of this commit into "reword";
                    // the editor called for the new message copies it from our file
                    string shortSha = full.Substring(0, 7);
                    var env = new Dictionary<string, string>
                    {
                        { "GIT_SEQUENCE_EDITOR", "sed -i -e 's/^pick \\(" + shortSha + "[0-9a-f]*\\)/reword \\1/'" },
                        { "GIT_EDITOR", "cp '" + posix + "'" }
                    };
                    string from = parents.Length == 0 ? "--root" : full + "^";
                    result = RunGit(repo, "rebase -i --autostash --rebase-merges " + from, env);
                    if (!result.Success())
                    {
                        RunGit(repo, "rebase --abort", null);
                        MessageBox.Show("The commit could not be rewritten; the branch was left as it was.\n\n" + result.stderr,
                            "Edit commit message", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                App.DoRefresh();
                return result.Success();
            }
            finally
            {
                try { File.Delete(msgFile); } catch (Exception) { }
            }
        }

        #endregion

        #region Branches and tags

        /// <summary>
        /// Rename a local branch
        /// </summary>
        public static bool RenameBranch(ClassRepo repo, string branch)
        {
            string name = FormTextInput.Ask("Rename branch", "New name of branch \"" + branch + "\":", branch, false);
            if (name == null || (name = name.Trim()) == branch || !ValidName(repo, name, true))
                return false;
            bool ok = RunGit(repo, "branch -m \"" + branch + "\" \"" + name + "\"", null).Success();
            if (ok && repo.Run("config --get branch." + name + ".remote").Success())
                App.PrintStatusMessage("The branch was renamed locally. Its remote branch keeps its old name until you push the new one.", MessageType.General);
            App.DoRefresh();
            return ok;
        }

        /// <summary>
        /// Create a branch at a commit, optionally switching to it
        /// </summary>
        public static bool NewBranchAt(ClassRepo repo, string sha)
        {
            string name = FormTextInput.Ask("New branch", "Name of the new branch starting at " + sha.Substring(0, Math.Min(7, sha.Length)) + ":", string.Empty, false);
            if (name == null || (name = name.Trim()).Length == 0 || !ValidName(repo, name, true))
                return false;
            bool ok = RunGit(repo, "branch \"" + name + "\" " + sha, null).Success();
            if (ok && MessageBox.Show("Switch to branch \"" + name + "\" now?", "New branch",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                RunGit(repo, "checkout \"" + name + "\"", null);
            App.DoRefresh();
            return ok;
        }

        /// <summary>
        /// Create a tag on a commit. With a message, the tag is an annotated tag.
        /// </summary>
        public static bool NewTag(ClassRepo repo, string sha)
        {
            string name = FormTextInput.Ask("New tag", "Name of the tag on commit " + sha.Substring(0, Math.Min(7, sha.Length)) + ":", string.Empty, false);
            if (name == null || (name = name.Trim()).Length == 0 || !ValidName(repo, name, false))
                return false;
            string message = FormTextInput.Ask("New tag", "Message of the tag (leave empty for a lightweight tag):", string.Empty, true);
            if (message == null)
                return false;
            string args = message.Trim().Length > 0
                ? "tag -a \"" + name + "\" -F \"" + WriteTemp(message) + "\" " + sha
                : "tag \"" + name + "\" " + sha;
            bool ok = RunGit(repo, args, null).Success();
            App.DoRefresh();
            return ok;
        }

        /// <summary>
        /// Rename a tag: create it under the new name on the same commit (keeping the message
        /// of an annotated tag), then delete the old one
        /// </summary>
        public static bool RenameTag(ClassRepo repo, string tag)
        {
            string name = FormTextInput.Ask("Rename tag", "New name of tag \"" + tag + "\":", tag, false);
            if (name == null || (name = name.Trim()) == tag || !ValidName(repo, name, false))
                return false;
            bool annotated = repo.Run("cat-file -t \"refs/tags/" + tag + "\"").stdout.Trim() == "tag";
            string args;
            if (annotated)
            {
                string message = repo.Run("tag -l --format=%(contents) \"" + tag + "\"").stdout;
                args = "tag -a \"" + name + "\" -F \"" + WriteTemp(message) + "\" \"" + tag + "^{}\"";
            }
            else
                args = "tag \"" + name + "\" \"" + tag + "\"";
            bool ok = RunGit(repo, args, null).Success() && RunGit(repo, "tag -d \"" + tag + "\"", null).Success();
            if (ok)
                App.PrintStatusMessage("Tag renamed locally. If it was pushed, push the new tag and delete the old one on the remote.", MessageType.General);
            App.DoRefresh();
            return ok;
        }

        /// <summary>
        /// Delete a tag (locally)
        /// </summary>
        public static bool DeleteTag(ClassRepo repo, string tag)
        {
            if (MessageBox.Show("Delete tag \"" + tag + "\"?", "Delete tag", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return false;
            bool ok = RunGit(repo, "tag -d \"" + tag + "\"", null).Success();
            App.DoRefresh();
            return ok;
        }

        private static bool ValidName(ClassRepo repo, string name, bool branch)
        {
            bool ok = branch
                ? repo.Run("check-ref-format --branch \"" + name + "\"").Success()
                : repo.Run("check-ref-format \"refs/tags/" + name + "\"").Success();
            if (!ok)
                MessageBox.Show("\"" + name + "\" is not a valid " + (branch ? "branch" : "tag") + " name.", "Invalid name",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return ok;
        }

        private static string WriteTemp(string text)
        {
            string file = Path.Combine(Path.GetTempPath(), "GitForce-tag-" + Guid.NewGuid().ToString("N") + ".txt");
            File.WriteAllText(file, text.Replace("\r\n", "\n"), new UTF8Encoding(false));
            ClassGlobals.TempFiles.Add(file);
            return file.Replace('\\', '/');
        }

        #endregion

        /// <summary>
        /// Run git in the repository root with extra environment variables, showing the command
        /// and its result in the log like the other user commands
        /// </summary>
        private static ExecResult RunGit(ClassRepo repo, string args, Dictionary<string, string> env)
        {
            if (Properties.Settings.Default.logCommands)
                App.PrintStatusMessage("git " + args, MessageType.Command);
            ExecResult result = new ExecResult();
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(Properties.Settings.Default.GitPath, args)
                {
                    WorkingDirectory = repo.Path,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                if (env != null)
                    foreach (var kv in env)
                        psi.Environment[kv.Key] = kv.Value;
                using (Process p = Process.Start(psi))
                {
                    var err = p.StandardError.ReadToEndAsync();
                    result.stdout = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    result.stderr = err.Result;
                    result.retcode = p.ExitCode;
                }
            }
            catch (Exception ex)
            {
                result.stderr = ex.Message;
            }
            if (result.stdout.Trim().Length > 0)
                App.PrintStatusMessage(result.stdout.TrimEnd(), MessageType.Output);
            if (!result.Success())
                App.PrintStatusMessage(result.stderr.TrimEnd(), MessageType.Error);
            return result;
        }
    }

    /// <summary>
    /// Simple dialog asking for a text: a single line, or several lines for a message
    /// </summary>
    public class FormTextInput : Form
    {
        private readonly TextBox text = new TextBox();

        public static string Ask(string title, string prompt, string initial, bool multiline)
        {
            using (FormTextInput form = new FormTextInput(title, prompt, initial, multiline))
                return form.ShowDialog() == DialogResult.OK ? form.text.Text : null;
        }

        private FormTextInput(string title, string prompt, string initial, bool multiline)
        {
            Text = title;
            Icon = Properties.Resources.git;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = multiline ? FormBorderStyle.Sizable : FormBorderStyle.FixedDialog;
            MinimizeBox = MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = multiline ? new Size(620, 340) : new Size(460, 110);
            MinimumSize = new Size(360, multiline ? 220 : 150);

            Label label = new Label { Text = prompt, AutoSize = false, Location = new Point(10, 10), Size = new Size(ClientSize.Width - 20, 20),
                                      Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };
            text.Multiline = multiline;
            text.AcceptsReturn = multiline;
            text.ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None;
            if (multiline)
                text.Font = Properties.Settings.Default.commitFont;
            text.Text = (initial ?? string.Empty).Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
            text.Location = new Point(10, 34);
            text.Size = new Size(ClientSize.Width - 20, ClientSize.Height - 34 - 48);
            text.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;

            Button ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Size = new Size(85, 27) };
            Button cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new Size(85, 27) };
            cancel.Location = new Point(ClientSize.Width - cancel.Width - 10, ClientSize.Height - 37);
            ok.Location = new Point(cancel.Left - ok.Width - 8, cancel.Top);
            ok.Anchor = cancel.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            if (!multiline)
                AcceptButton = ok;
            CancelButton = cancel;

            Controls.AddRange(new Control[] { label, text, ok, cancel });
            ClassTheme.Apply(this);
            Shown += (s, e) =>
            {
                text.Focus();
                if (multiline)
                    text.Select(0, 0);
                else
                    text.SelectAll();
            };
        }
    }
}
