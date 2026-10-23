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
        #region Edit the message and the author of a commit

        /// <summary>
        /// A commit that can be rewritten: the last commit, or an older commit of the current
        /// branch that is not a merge
        /// </summary>
        private class Target
        {
            public string Sha, Message, AuthorName, AuthorEmail;
            public string[] Parents;
            public bool IsHead;
            public int Distance;        // Number of commits after it on the branch
        }

        /// <summary>
        /// Check that a commit can be rewritten and warn when it is already pushed. Returns null if not.
        /// </summary>
        private static Target GetTarget(ClassRepo repo, string sha, string title)
        {
            ExecResult info = repo.Run("log -1 --format=%H%x1f%P%x1f%an%x1f%ae%x1f%B " + sha);
            string[] f = info.Success() ? info.stdout.Split(new[] { '\x1f' }, 5) : new string[0];
            if (f.Length < 5)
            {
                MessageBox.Show("Unable to read commit " + sha + ":\n\n" + info.stderr, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            Target t = new Target
            {
                Sha = f[0].Trim(),
                Parents = f[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries),
                AuthorName = f[2],
                AuthorEmail = f[3],
                Message = f[4].Replace("\r\n", "\n").TrimEnd()
            };
            string head = repo.Run("rev-parse HEAD").stdout.Trim();
            t.IsHead = t.Sha == head;
            if (!t.IsHead)
            {
                if (!repo.Run("merge-base --is-ancestor " + t.Sha + " HEAD").Success())
                {
                    MessageBox.Show("Only the commits of the current branch can be changed. Switch to the branch of this commit first.",
                        title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return null;
                }
                if (t.Parents.Length > 1)
                {
                    MessageBox.Show("A merge commit can only be changed when it is the last commit of the branch.",
                        title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return null;
                }
                if (new ClassStatus(repo).IsMergeState() || Directory.Exists(Path.Combine(repo.Path, ".git", "rebase-merge")))
                {
                    MessageBox.Show("Finish or abort the merge or rebase in progress first.", title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return null;
                }
                int count;
                int.TryParse(repo.Run("rev-list --count " + t.Sha + "..HEAD").stdout.Trim(), out count);
                t.Distance = count;
            }
            return t;
        }

        private static bool ConfirmPushed(ClassRepo repo, Target t, string title)
        {
            string remotes = repo.Run("branch -r --contains " + t.Sha).stdout.Trim();
            if (remotes.Length == 0)
                return true;
            return MessageBox.Show(
                "This commit is already pushed (" + remotes.Split('\n')[0].Trim() + ").\n\n" +
                "Changing it rewrites the history: the branch will have to be force-pushed, " +
                "and other people working on it will have to reset their copy.\n\nChange it anyway?",
                title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
        }

        /// <summary>
        /// Amend a commit with the given "git commit --amend" options. The last commit is amended
        /// directly; an older one is reached with a rebase that stops on it ("edit"), is amended,
        /// and the rebase continues, which also rewrites the commits after it.
        /// Returns the new SHA of the commit, or null on failure.
        /// </summary>
        private static string Rewrite(ClassRepo repo, Target t, string amendArgs, Dictionary<string, string> env, string title)
        {
            ExecResult result;
            if (t.IsHead)
            {
                // --only: what is staged stays out of the commit
                result = RunGit(repo, "commit --amend --only -q " + amendArgs, env);
                if (!result.Success())
                {
                    MessageBox.Show("The commit could not be changed:\n\n" + (result.stderr + result.stdout).Trim(), title,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }
                return repo.Run("rev-parse HEAD").stdout.Trim();
            }

            string shortSha = t.Sha.Substring(0, 7);
            var sequence = new Dictionary<string, string>
            {
                { "GIT_SEQUENCE_EDITOR", "sed -i -e 's/^pick \\(" + shortSha + "[0-9a-f]*\\)/edit \\1/'" },
                { "GIT_EDITOR", "true" }
            };
            string from = t.Parents.Length == 0 ? "--root" : t.Sha + "^";
            result = RunGit(repo, "rebase -i --autostash --rebase-merges " + from, sequence);
            string stopped = repo.Run("rev-parse HEAD").stdout.Trim();
            if (stopped != t.Sha)
            {
                RunGit(repo, "rebase --abort", null);
                MessageBox.Show("The commit could not be reached; the branch was left as it was.\n\n" + (result.stderr + result.stdout).Trim(),
                    title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            result = RunGit(repo, "commit --amend --only -q " + amendArgs, env);
            if (result.Success())
                result = RunGit(repo, "rebase --continue", sequence);
            if (!result.Success())
            {
                RunGit(repo, "rebase --abort", null);
                MessageBox.Show("The commit could not be changed; the branch was left as it was.\n\n" + (result.stderr + result.stdout).Trim(),
                    title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            // The commit keeps its place on the branch: the same number of commits after it
            return repo.Run("rev-parse HEAD~" + t.Distance).stdout.Trim();
        }

        /// <summary>
        /// Let the user edit the message of a commit and rewrite it.
        /// Returns the new SHA of the commit, or null if nothing was changed.
        /// </summary>
        public static string EditMessage(ClassRepo repo, string sha)
        {
            const string title = "Edit commit message";
            Target t = GetTarget(repo, sha, title);
            if (t == null)
                return null;
            string newMessage = FormTextInput.Ask(title, "Message of commit " + t.Sha.Substring(0, 7) + ":", t.Message, true);
            if (newMessage == null || newMessage.Trim().Length == 0 || newMessage.Replace("\r\n", "\n").Trim() == t.Message.Trim())
                return null;
            if (!ConfirmPushed(repo, t, title))
                return null;

            string msgFile = Path.Combine(Path.GetTempPath(), "GitForce-msg-" + Guid.NewGuid().ToString("N") + ".txt");
            try
            {
                File.WriteAllText(msgFile, newMessage.Replace("\r\n", "\n").TrimEnd() + "\n", new UTF8Encoding(false));
                string newSha = Rewrite(repo, t, "--cleanup=verbatim -F \"" + msgFile.Replace('\\', '/') + "\"", null, title);
                App.DoRefresh();
                return newSha;
            }
            finally
            {
                try { File.Delete(msgFile); } catch (Exception) { }
            }
        }

        /// <summary>
        /// Let the user change the author (and optionally the committer) of a commit.
        /// Returns the new SHA of the commit, or null if nothing was changed.
        /// </summary>
        public static string EditAuthor(ClassRepo repo, string sha)
        {
            const string title = "Edit commit author";
            Target t = GetTarget(repo, sha, title);
            if (t == null)
                return null;
            string name, email;
            bool committer;
            if (!FormAuthorInput.Ask(t.Sha.Substring(0, 7), t.AuthorName, t.AuthorEmail, out name, out email, out committer))
                return null;
            if (name == t.AuthorName && email == t.AuthorEmail && !committer)
                return null;
            if (!ConfirmPushed(repo, t, title))
                return null;

            string author = (name + " <" + email + ">").Replace("\"", "\\\"");
            Dictionary<string, string> env = null;
            if (committer)
                env = new Dictionary<string, string> { { "GIT_COMMITTER_NAME", name }, { "GIT_COMMITTER_EMAIL", email } };
            string newSha = Rewrite(repo, t, "--no-edit --author=\"" + author + "\"", env, title);
            App.DoRefresh();
            return newSha;
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

    /// <summary>
    /// Dialog asking for the name and the e-mail of an author
    /// </summary>
    public class FormAuthorInput : Form
    {
        private readonly TextBox textName = new TextBox();
        private readonly TextBox textEmail = new TextBox();
        private readonly CheckBox checkCommitter = new CheckBox { Text = "Also set as committer", AutoSize = true };

        public static bool Ask(string commit, string name, string email, out string newName, out string newEmail, out bool committer)
        {
            using (FormAuthorInput form = new FormAuthorInput(commit, name, email))
            {
                bool ok = form.ShowDialog() == DialogResult.OK;
                newName = form.textName.Text.Trim();
                newEmail = form.textEmail.Text.Trim();
                committer = form.checkCommitter.Checked;
                return ok && newName.Length > 0 && newEmail.Length > 0;
            }
        }

        private FormAuthorInput(string commit, string name, string email)
        {
            Text = "Edit commit author";
            Icon = Properties.Resources.git;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(460, 180);

            Label title = new Label { Text = "Author of commit " + commit + ":", Location = new Point(10, 10), AutoSize = true };
            Label l1 = new Label { Text = "Name:", Location = new Point(10, 42), AutoSize = true };
            Label l2 = new Label { Text = "E-mail:", Location = new Point(10, 74), AutoSize = true };
            textName.Location = new Point(70, 38);
            textName.Width = 380;
            textName.Text = name;
            textEmail.Location = new Point(70, 70);
            textEmail.Width = 380;
            textEmail.Text = email;
            checkCommitter.Location = new Point(70, 102);
            new ToolTip().SetToolTip(checkCommitter, "The committer is the person who last applied the commit (shown by some tools instead of the author)");

            Button ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Size = new Size(85, 27), Location = new Point(272, 140) };
            Button cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new Size(85, 27), Location = new Point(365, 140) };
            AcceptButton = ok;
            CancelButton = cancel;
            Controls.AddRange(new Control[] { title, l1, l2, textName, textEmail, checkCommitter, ok, cancel });
            ClassTheme.Apply(this);
        }
    }
}
