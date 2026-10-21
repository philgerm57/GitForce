using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GitForce
{
    /// <summary>
    /// Stash the changes of some or all files, with a message and a mode:
    ///  - Discard: the changes are removed from the working files (the usual stash)
    ///  - Keep index: the staged changes stay staged and in the working files
    ///  - Keep all: the changes are saved in the stash and also stay in the working files
    /// </summary>
    public class FormStashSave : Form
    {
        private readonly ClassRepo repo;
        private readonly TextBox textMessage = new TextBox();
        private readonly RadioButton rbDiscard = new RadioButton { Text = "Discard: remove the changes from the working files", Checked = true };
        private readonly RadioButton rbKeepIndex = new RadioButton { Text = "Keep index: staged changes stay staged" };
        private readonly RadioButton rbKeepAll = new RadioButton { Text = "Keep all: save the changes, keep working on them" };
        private readonly CheckBox checkUntracked = new CheckBox { Text = "Include untracked (new) files" };
        private readonly ListView listFiles = new ListView();
        private readonly Label labelInfo = new Label();

        /// <summary>
        /// Show the dialog; files preselects the files to stash (all the changed files if null)
        /// </summary>
        public static bool Stash(ClassRepo repo, IEnumerable<string> files)
        {
            using (FormStashSave form = new FormStashSave(repo, files))
                return form.ShowDialog() == DialogResult.OK;
        }

        private FormStashSave(ClassRepo repo, IEnumerable<string> preselect)
        {
            this.repo = repo;
            Text = "Stash";
            Icon = Properties.Resources.git;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(620, 520);
            MinimumSize = new Size(480, 420);
            ShowInTaskbar = false;

            Label l1 = new Label { Text = "Message:", Location = new Point(10, 12), AutoSize = true };
            textMessage.Location = new Point(10, 32);
            textMessage.Width = ClientSize.Width - 20;
            textMessage.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            GroupBox mode = new GroupBox { Text = "Mode", Location = new Point(10, 64), Size = new Size(ClientSize.Width - 20, 100),
                                           Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };
            int y = 20;
            foreach (RadioButton rb in new[] { rbDiscard, rbKeepIndex, rbKeepAll })
            {
                rb.Location = new Point(12, y);
                rb.AutoSize = true;
                mode.Controls.Add(rb);
                y += 24;
            }

            checkUntracked.Location = new Point(12, 172);
            checkUntracked.AutoSize = true;
            checkUntracked.CheckedChanged += (s, e) => LoadFiles(CheckedFiles());

            Label l2 = new Label { Text = "Files to stash:", Location = new Point(10, 198), AutoSize = true };
            listFiles.Location = new Point(10, 218);
            listFiles.Size = new Size(ClientSize.Width - 20, ClientSize.Height - 218 - 80);
            listFiles.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
            listFiles.View = View.Details;
            listFiles.CheckBoxes = true;
            listFiles.FullRowSelect = true;
            listFiles.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            listFiles.Columns.Add("File", 460);
            listFiles.Columns.Add("Status", 100);
            // The event also comes while the list creates its window, before its items are ready
            listFiles.ItemChecked += (s, e) => { if (IsHandleCreated) BeginInvoke((Action)UpdateInfo); };

            Button btAll = new Button { Text = "All", Size = new Size(60, 25), Anchor = AnchorStyles.Left | AnchorStyles.Bottom };
            Button btNone = new Button { Text = "None", Size = new Size(60, 25), Anchor = AnchorStyles.Left | AnchorStyles.Bottom };
            btAll.Location = new Point(10, ClientSize.Height - 72);
            btNone.Location = new Point(76, ClientSize.Height - 72);
            btAll.Click += (s, e) => { foreach (ListViewItem i in listFiles.Items) i.Checked = true; };
            btNone.Click += (s, e) => { foreach (ListViewItem i in listFiles.Items) i.Checked = false; };
            labelInfo.Location = new Point(146, ClientSize.Height - 68);
            labelInfo.Size = new Size(ClientSize.Width - 156, 20);
            labelInfo.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            Button ok = new Button { Text = "Stash", Size = new Size(90, 28), Anchor = AnchorStyles.Right | AnchorStyles.Bottom };
            Button cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new Size(90, 28), Anchor = AnchorStyles.Right | AnchorStyles.Bottom };
            cancel.Location = new Point(ClientSize.Width - 100, ClientSize.Height - 38);
            ok.Location = new Point(cancel.Left - 98, cancel.Top);
            ok.Click += (s, e) => DoStash();
            CancelButton = cancel;

            Controls.AddRange(new Control[] { l1, textMessage, mode, checkUntracked, l2, listFiles, btAll, btNone, labelInfo, ok, cancel });
            ClassTheme.Apply(this);

            List<string> pre = preselect == null ? null : preselect.Select(Normalize).ToList();
            // Show untracked files when the user selected some of them
            ClassStatus status = new ClassStatus(repo);
            if (pre != null && pre.Any(p => status.Xcode(p.Replace('/', Path.DirectorySeparatorChar)) == '?'))
                checkUntracked.Checked = true;
            LoadFiles(pre);
        }

        private static string Normalize(string path)
        {
            return path.Replace('\\', '/').TrimEnd('/');
        }

        private List<string> CheckedFiles()
        {
            return listFiles.Items.Cast<ListViewItem>().Where(i => i != null && i.Checked).Select(i => (string)i.Tag).ToList();
        }

        /// <summary>
        /// List the changed files; check those that are preselected or inside a preselected folder
        /// </summary>
        private void LoadFiles(List<string> preselect)
        {
            listFiles.BeginUpdate();
            listFiles.Items.Clear();
            ClassStatus status = new ClassStatus(repo);
            foreach (string file in status.GetFiles().OrderBy(f => f))
            {
                char x = status.Xcode(file), yc = status.Ycode(file);
                if (x == '?' && !checkUntracked.Checked)
                    continue;
                string path = Normalize(file);
                string state = x == '?' ? "untracked" : (x != ' ' && yc != ' ') ? "staged + modified" : x != ' ' ? "staged" : "modified";
                if (x == 'U' || yc == 'U')
                    state = "unmerged";
                bool check = preselect == null || preselect.Count == 0 || preselect.Contains(".") ||
                             preselect.Any(p => path == p || path.StartsWith(p + "/"));
                listFiles.Items.Add(new ListViewItem(new[] { path, state }) { Tag = path, Checked = check });
            }
            listFiles.EndUpdate();
            UpdateInfo();
        }

        private void UpdateInfo()
        {
            int n = listFiles.Items.Cast<ListViewItem>().Count(i => i != null && i.Checked);
            labelInfo.Text = n + " of " + listFiles.Items.Count + " file(s) selected";
        }

        private void DoStash()
        {
            List<string> files = CheckedFiles();
            if (files.Count == 0)
            {
                MessageBox.Show("Select the files to stash.", "Stash", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string message = textMessage.Text.Trim();
            bool all = files.Count == listFiles.Items.Count;

            StringBuilder args = new StringBuilder("stash push");
            if (message.Length > 0)
                args.Append(" -m \"" + message.Replace("\"", "\\\"") + "\"");
            if (rbKeepIndex.Checked)
                args.Append(" --keep-index");
            if (checkUntracked.Checked)
                args.Append(" --include-untracked");
            string listFile = null;
            if (!all)
            {
                // The paths go through a file: there may be too many for a command line
                listFile = Path.Combine(Path.GetTempPath(), "GitForce-stash-" + Guid.NewGuid().ToString("N") + ".txt");
                File.WriteAllText(listFile, string.Join("\0", files) + "\0", new UTF8Encoding(false));
                args.Append(" --pathspec-from-file=\"" + listFile.Replace('\\', '/') + "\" --pathspec-file-nul");
            }
            try
            {
                ExecResult result = repo.RunCmd(args.ToString());
                if (!result.Success())
                {
                    MessageBox.Show("The stash failed:\n\n" + result.stderr, "Stash", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (rbKeepAll.Checked)
                {
                    // Put the changes back, staged as they were; the stash keeps its copy
                    ExecResult back = repo.RunCmd("stash apply --index");
                    if (!back.Success())
                        MessageBox.Show("The changes are stashed, but they could not be restored in the working files:\n\n" + back.stderr +
                                        "\n\nApply the stash from the Unstash dialog.", "Stash", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                DialogResult = DialogResult.OK;
            }
            finally
            {
                if (listFile != null)
                    try { File.Delete(listFile); } catch (Exception) { }
            }
        }
    }

    /// <summary>
    /// The list of stashes: their files and differences, and the commands to apply (keeping the
    /// stash), pop (apply and remove), drop, rename, or create a branch from a stash.
    /// </summary>
    public class FormStashList : Form
    {
        private readonly ClassRepo repo;
        private readonly ListView listStashes = new ListView();
        private readonly ListView listFiles = new ListView();
        private readonly DiffViewControl diff = new DiffViewControl { Dock = DockStyle.Fill };
        private readonly CheckBox checkIndex = new CheckBox { Text = "Restore staged state", AutoSize = true };
        private readonly Button btApply = new Button { Text = "Apply", Size = new Size(80, 27) };
        private readonly Button btPop = new Button { Text = "Pop", Size = new Size(80, 27) };
        private readonly Button btDrop = new Button { Text = "Drop", Size = new Size(80, 27) };
        private readonly Button btRename = new Button { Text = "Edit message...", Size = new Size(110, 27) };
        private readonly Button btBranch = new Button { Text = "New branch...", Size = new Size(100, 27) };

        public static void ShowStashes(ClassRepo repo)
        {
            using (FormStashList form = new FormStashList(repo))
                form.ShowDialog();
            App.DoRefresh();
        }

        private FormStashList(ClassRepo repo)
        {
            this.repo = repo;
            Text = "Stashes";
            Icon = Properties.Resources.git;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1150, 760);
            MinimumSize = new Size(700, 450);
            ShowInTaskbar = false;

            listStashes.Dock = DockStyle.Fill;
            listStashes.View = View.Details;
            listStashes.FullRowSelect = true;
            listStashes.HideSelection = false;
            listStashes.MultiSelect = false;
            listStashes.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            listStashes.Columns.Add("Stash", 80);
            listStashes.Columns.Add("Date", 125);
            listStashes.Columns.Add("Branch", 110);
            listStashes.Columns.Add("Message", 400);
            listStashes.SelectedIndexChanged += (s, e) => LoadFiles();
            listStashes.DoubleClick += (s, e) => Apply(false);

            listFiles.Dock = DockStyle.Fill;
            listFiles.View = View.Details;
            listFiles.FullRowSelect = true;
            listFiles.HideSelection = false;
            listFiles.MultiSelect = false;
            listFiles.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            listFiles.Columns.Add("", 26);
            listFiles.Columns.Add("File", 360);
            listFiles.SelectedIndexChanged += (s, e) => ShowFile();

            SplitContainer top = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical };
            top.Panel1.Controls.Add(listStashes);
            top.Panel2.Controls.Add(listFiles);
            SplitContainer main = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
            main.Panel1.Controls.Add(top);
            main.Panel2.Controls.Add(diff);

            FlowLayoutPanel buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(6, 6, 6, 0) };
            Button btClose = new Button { Text = "Close", Size = new Size(80, 27), DialogResult = DialogResult.Cancel };
            checkIndex.Margin = new Padding(3, 7, 12, 3);
            buttons.Controls.AddRange(new Control[] { btApply, btPop, checkIndex, btDrop, btRename, btBranch, btClose });
            CancelButton = btClose;
            new ToolTip().SetToolTip(btApply, "Apply the changes of the stash to the working files and keep the stash");
            new ToolTip().SetToolTip(btPop, "Apply the changes of the stash and remove it from the list");
            new ToolTip().SetToolTip(checkIndex, "Also restore which changes were staged");
            new ToolTip().SetToolTip(btBranch, "Create a branch from the commit the stash was made on, check it out and apply the stash there");
            btApply.Click += (s, e) => Apply(false);
            btPop.Click += (s, e) => Apply(true);
            btDrop.Click += (s, e) => Drop();
            btRename.Click += (s, e) => Rename();
            btBranch.Click += (s, e) => Branch();

            Controls.Add(main);
            Controls.Add(buttons);
            ClassTheme.Apply(this);
            Load += (s, e) =>
            {
                main.SplitterDistance = main.Height * 2 / 5;
                top.SplitterDistance = top.Width * 3 / 5;
                LoadStashes(0);
            };
        }

        private string SelectedStash()
        {
            return listStashes.SelectedItems.Count == 1 ? (string)listStashes.SelectedItems[0].Tag : null;
        }

        private void LoadStashes(int select)
        {
            listStashes.BeginUpdate();
            listStashes.Items.Clear();
            string output = DiffViewControl.RunGitRaw(repo.Path, "stash list --format=%gd%x1f%ci%x1f%gs");
            foreach (string line in output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] f = line.TrimEnd('\r').Split('\x1f');
                if (f.Length < 3)
                    continue;
                // "On main: message" or "WIP on main: abc1234 subject"
                string branch = string.Empty, message = f[2];
                int colon = message.IndexOf(": ", StringComparison.Ordinal);
                if (colon > 0)
                {
                    string head = message.Substring(0, colon);
                    branch = head.StartsWith("WIP on ") ? head.Substring(7) : head.StartsWith("On ") ? head.Substring(3) : head;
                    message = message.Substring(colon + 2);
                }
                string date = f[1].Length >= 16 ? f[1].Substring(0, 16) : f[1];
                listStashes.Items.Add(new ListViewItem(new[] { f[0], date, branch, message }) { Tag = f[0] });
            }
            listStashes.EndUpdate();
            bool any = listStashes.Items.Count > 0;
            btApply.Enabled = btPop.Enabled = btDrop.Enabled = btRename.Enabled = btBranch.Enabled = any;
            if (any)
                listStashes.Items[Math.Min(select, listStashes.Items.Count - 1)].Selected = true;
            else
            {
                listFiles.Items.Clear();
                diff.Clear("There are no stashes.");
            }
        }

        /// <summary>
        /// Files of the selected stash: the tracked changes, and the untracked files it saved
        /// </summary>
        private void LoadFiles()
        {
            listFiles.BeginUpdate();
            listFiles.Items.Clear();
            string stash = SelectedStash();
            if (stash != null)
            {
                string tracked = DiffViewControl.RunGitRaw(repo.Path, "stash show --name-status -z " + stash);
                AddFiles(tracked, false);
                // Untracked files are kept in the third parent of the stash commit
                string untracked = DiffViewControl.RunGitRaw(repo.Path, "ls-tree -r -z --name-only " + stash + "^3");
                foreach (string path in untracked.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries))
                    listFiles.Items.Add(new ListViewItem(new[] { "?", path }) { Tag = new[] { "?", path, path }, ToolTipText = "Untracked" });
            }
            listFiles.EndUpdate();
            diff.Clear();
            if (listFiles.Items.Count > 0)
                listFiles.Items[0].Selected = true;
        }

        private void AddFiles(string nameStatus, bool untracked)
        {
            string[] t = nameStatus.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < t.Length; )
            {
                string code = t[i].Trim();
                if (code.Length == 0) { i++; continue; }
                char kind = code[0];
                string oldPath, path;
                if ((kind == 'R' || kind == 'C') && i + 2 < t.Length) { oldPath = t[i + 1]; path = t[i + 2]; i += 3; }
                else if (i + 1 < t.Length) { oldPath = path = t[i + 1]; i += 2; }
                else break;
                listFiles.Items.Add(new ListViewItem(new[] { kind.ToString(), oldPath != path ? oldPath + "  →  " + path : path })
                    { Tag = new[] { kind.ToString(), oldPath, path } });
            }
        }

        /// <summary>
        /// Show a file of the stash: the version it was based on (left) and the stashed version (right)
        /// </summary>
        private void ShowFile()
        {
            string stash = SelectedStash();
            if (stash == null || listFiles.SelectedItems.Count != 1)
                return;
            string[] f = (string[])listFiles.SelectedItems[0].Tag;
            char kind = f[0][0];
            int code;
            byte[] before = null, after = null;
            if (kind != 'A' && kind != '?')
            {
                before = DiffViewControl.RunGitBytes(repo.Path, "show \"" + stash + "^1:" + f[1] + "\"", out code);
                if (code != 0) before = null;
            }
            if (kind != 'D')
            {
                string tree = kind == '?' ? stash + "^3" : stash;
                after = DiffViewControl.RunGitBytes(repo.Path, "show \"" + tree + ":" + f[2] + "\"", out code);
                if (code != 0) after = null;
            }
            diff.ShowCompare(f[2], before == null ? "(new file)" : after == null ? "(deleted)" : "base  ↔  stash", before, after);
        }

        private void Apply(bool pop)
        {
            string stash = SelectedStash();
            if (stash == null)
                return;
            int index = listStashes.SelectedIndices[0];
            ExecResult result = repo.RunCmd("stash " + (pop ? "pop" : "apply") + (checkIndex.Checked ? " --index " : " ") + stash);
            if (!result.Success())
            {
                List<string> conflicts = new ClassStatus(repo).GetUnmerged();
                if (conflicts.Count > 0)
                {
                    if (MessageBox.Show("The stash was applied with conflicts in " + conflicts.Count + " file(s)" +
                                        (pop ? " and was kept in the list" : "") + ".\n\nResolve them now?",
                            "Apply stash", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                        ClassMerge.ResolveConflicts(repo, conflicts);
                }
                else
                    MessageBox.Show("The stash could not be applied:\n\n" + result.stderr.Trim() +
                                    "\n\nIf your working files have changes in the same files, commit or stash them first, " +
                                    "or use \"New branch...\" to apply the stash on a new branch.",
                        "Apply stash", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            App.DoRefresh();
            LoadStashes(index);
        }

        private void Drop()
        {
            string stash = SelectedStash();
            if (stash == null)
                return;
            if (MessageBox.Show("Remove " + stash + " (" + listStashes.SelectedItems[0].SubItems[3].Text + ")?\n\nIts changes will be lost.",
                    "Drop stash", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;
            int index = listStashes.SelectedIndices[0];
            repo.RunCmd("stash drop " + stash);
            LoadStashes(index);
        }

        /// <summary>
        /// Git has no command to change the message of a stash: make a copy of the stash commit
        /// (same content and parents) with the new message, store it as the newest stash and drop
        /// the old entry. The stash moves to the top of the list.
        /// </summary>
        private void Rename()
        {
            string stash = SelectedStash();
            if (stash == null)
                return;
            ListViewItem item = listStashes.SelectedItems[0];
            string message = FormTextInput.Ask("Edit stash message", "Message of " + stash + ":", item.SubItems[3].Text, false);
            if (message == null || (message = message.Trim()).Length == 0 || message == item.SubItems[3].Text)
                return;
            string branch = item.SubItems[2].Text;
            string full = (branch.Length > 0 ? "On " + branch + ": " : string.Empty) + message;
            string quoted = "\"" + full.Replace("\"", "\\\"") + "\"";

            // "<stash> <parent1> <parent2> [<parent3>]"
            string[] ids = DiffViewControl.RunGitRaw(repo.Path, "rev-list --parents -n 1 " + stash).Trim().Split(' ');
            if (ids.Length < 2)
                return;
            StringBuilder args = new StringBuilder("commit-tree " + ids[0] + "^{tree}");
            for (int i = 1; i < ids.Length; i++)
                args.Append(" -p " + ids[i]);
            args.Append(" -m " + quoted);
            string copy = DiffViewControl.RunGitRaw(repo.Path, args.ToString()).Trim();
            if (copy.Length == 0)
                return;
            int index = listStashes.SelectedIndices[0];
            // Once the copy is stored on top, the old entry is one further down the list
            if (repo.RunCmd("stash store -m " + quoted + " " + copy).Success())
                repo.RunCmd("stash drop stash@{" + (index + 1) + "}");
            LoadStashes(0);
        }

        private void Branch()
        {
            string stash = SelectedStash();
            if (stash == null)
                return;
            string name = FormTextInput.Ask("Branch from stash", "Name of the new branch:", string.Empty, false);
            if (name == null || (name = name.Trim()).Length == 0)
                return;
            ExecResult result = repo.RunCmd("stash branch \"" + name + "\" " + stash);
            if (!result.Success())
                MessageBox.Show(result.stderr, "Branch from stash", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            App.DoRefresh();
            LoadStashes(0);
        }
    }
}
