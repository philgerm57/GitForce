using System;
using System.Drawing;
using System.Windows.Forms;

namespace GitForce
{
    /// <summary>
    /// Reset the current branch to a commit, choosing between the five modes of git reset
    /// (mixed, soft, hard, keep, merge), each with what it does to the index and the files.
    /// </summary>
    public class FormResetAdvanced : Form
    {
        private readonly RadioButton rbMixed, rbSoft, rbHard, rbKeep, rbMerge;
        private readonly CheckBox checkLineEndings = new CheckBox
        {
            Text = "Thoroughly fix line endings according to .gitattributes", AutoSize = true, Enabled = false,
            Margin = new Padding(40, 0, 3, 6)
        };

        /// <summary>
        /// Ask for the mode and reset the current branch of the repository to the commit
        /// </summary>
        public static void ResetTo(ClassRepo repo, string sha, string subject)
        {
            if (repo == null || string.IsNullOrEmpty(sha))
                return;
            string mode;
            bool lineEndings;
            using (FormResetAdvanced form = new FormResetAdvanced(repo, sha, subject))
            {
                if (form.ShowDialog() != DialogResult.OK)
                    return;
                mode = form.Mode;
                lineEndings = form.checkLineEndings.Enabled && form.checkLineEndings.Checked;
            }

            ExecResult result = repo.RunCmd("reset --" + mode + " " + sha);
            if (result.Success() && lineEndings)
            {
                // Forget the index and check every file out again, so that each one gets the line
                // endings that .gitattributes asks for
                result = repo.RunCmd("rm --cached -r -q .");
                if (result.Success())
                    result = repo.RunCmd("reset --hard");
            }
            if (!result.Success())
                MessageBox.Show(result.stderr.Trim(), "Reset", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            App.DoRefresh();
        }

        private FormResetAdvanced(ClassRepo repo, string sha, string subject)
        {
            Text = "Reset";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(10);

            string branch = repo.Run("rev-parse --abbrev-ref HEAD").stdout.Trim();
            string shortSha = sha.Length > 8 ? sha.Substring(0, 8) : sha;
            int width = 560;

            FlowLayoutPanel flow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Fill
            };
            Label title = new Label { Text = "Reset to commit " + shortSha, AutoSize = true, Margin = new Padding(3, 0, 3, 4) };
            title.Font = new Font(Font.FontFamily, Font.Size * 1.5f);
            flow.Controls.Add(title);
            if (!string.IsNullOrEmpty(subject))
                flow.Controls.Add(Note(subject, width, 0, FontStyle.Italic));
            flow.Controls.Add(Note("Reset the current branch (" + branch + ") to the selected commit and optionally update " +
                                   "the index and the working tree.", width, 0, FontStyle.Regular));
            flow.Controls.Add(new Label { Height = 1, Width = width, BorderStyle = BorderStyle.Fixed3D, Margin = new Padding(3, 8, 3, 8) });

            rbMixed = Option(flow, "Reset the index but not the working tree - 'mixed'",
                "The changes stay in your files; staged changes are no longer staged.", width, true);
            rbSoft = Option(flow, "Don't touch the index nor the working tree - 'soft'",
                "The index will show all the changes between the previous HEAD and the selected commit, ready to be committed again.", width, false);
            rbHard = Option(flow, "Reset the index and the working tree - 'hard'",
                "Current staged and local changes will be lost!", width, false, true);
            flow.Controls.Add(checkLineEndings);
            rbKeep = Option(flow, "Reset the index and the working tree, skipping modified files - 'keep'",
                "Similar to 'hard', but your local changes are kept. If a file to be replaced has working tree or index changes, " +
                "the reset is aborted.", width, false);
            rbMerge = Option(flow, "Reset the index and the working tree, skipping working tree modified files - 'merge'",
                "Similar to 'hard'. If a file to be replaced has working tree changes, the reset is aborted.", width, false);
            rbHard.CheckedChanged += (s, e) => checkLineEndings.Enabled = rbHard.Checked;

            FlowLayoutPanel buttons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Width = width, Margin = new Padding(3, 10, 3, 0)
            };
            Button btCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            Button btReset = new Button { Text = "Reset", DialogResult = DialogResult.OK, AutoSize = true };
            buttons.Controls.Add(btCancel);
            buttons.Controls.Add(btReset);
            buttons.MinimumSize = new Size(width, 0);
            flow.Controls.Add(buttons);
            Controls.Add(flow);
            AcceptButton = btReset;
            CancelButton = btCancel;

            // A last warning for the mode that loses changes
            FormClosing += (s, e) =>
            {
                if (DialogResult == DialogResult.OK && rbHard.Checked &&
                    MessageBox.Show(this, "Reset 'hard' to " + shortSha + "?\n\nCurrent staged and local changes will be lost.",
                        Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                    e.Cancel = true;
            };
            ClassTheme.Apply(this);
        }

        /// <summary>
        /// The mode chosen, as git reset names it
        /// </summary>
        private string Mode
        {
            get
            {
                return rbSoft.Checked ? "soft" : rbHard.Checked ? "hard" : rbKeep.Checked ? "keep" : rbMerge.Checked ? "merge" : "mixed";
            }
        }

        private static Label Note(string text, int width, int indent, FontStyle style)
        {
            Label label = new Label
            {
                Text = text, AutoSize = true, MaximumSize = new Size(width - indent, 0), Margin = new Padding(3 + indent, 0, 3, 2)
            };
            if (style != FontStyle.Regular)
                label.Font = new Font(label.Font, style);
            return label;
        }

        private static RadioButton Option(FlowLayoutPanel flow, string text, string description, int width, bool check, bool danger = false)
        {
            RadioButton rb = new RadioButton { Text = text, AutoSize = true, Checked = check, Margin = new Padding(3, 4, 3, 0) };
            flow.Controls.Add(rb);
            Label note = Note(description, width, 18, FontStyle.Regular);
            note.Margin = new Padding(21, 0, 3, 6);
            note.ForeColor = danger ? Color.Firebrick : SystemColors.GrayText;
            note.Click += (s, e) => rb.Checked = true;
            flow.Controls.Add(note);
            return rb;
        }
    }

    /// <summary>
    /// Apply the changes of a commit to the current branch, as a new commit or only to the
    /// working files to review them first
    /// </summary>
    public class FormCherryPick : Form
    {
        private readonly CheckBox checkCommit = new CheckBox
        {
            Text = "Create the commit (otherwise the changes are only applied, to be reviewed and committed)",
            AutoSize = true, Checked = true
        };
        private readonly CheckBox checkOrigin = new CheckBox
        {
            Text = "Add \"(cherry picked from commit ...)\" to the message", AutoSize = true
        };

        public static void CherryPick(ClassRepo repo, string sha, string subject)
        {
            if (repo == null || string.IsNullOrEmpty(sha))
                return;
            string args;
            using (FormCherryPick form = new FormCherryPick(repo, sha, subject))
            {
                if (form.ShowDialog() != DialogResult.OK)
                    return;
                args = (form.checkCommit.Checked ? "" : "--no-commit ") + (form.checkOrigin.Checked && form.checkCommit.Checked ? "-x " : "");
            }

            // A merge commit is applied with the changes it brought to its first parent
            if (repo.Run("rev-list --parents -n 1 " + sha).stdout.Trim().Split(' ').Length > 2)
                args += "-m 1 ";
            ExecResult result = repo.RunCmd("cherry-pick " + args + sha);
            if (!result.Success())
            {
                if (new ClassStatus(repo).GetUnmerged().Count > 0)
                {
                    MessageBox.Show("The commit conflicts with the current branch. Resolve the conflicts, then commit the changes " +
                                    "(or run \"git cherry-pick --abort\" to cancel).", "Cherry-Pick", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClassMerge.ResolveConflicts(repo, null);
                }
                else
                    MessageBox.Show(result.stderr.Trim(), "Cherry-Pick", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            App.DoRefresh();
        }

        private FormCherryPick(ClassRepo repo, string sha, string subject)
        {
            Text = "Cherry-Pick";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(10);

            string branch = repo.Run("rev-parse --abbrev-ref HEAD").stdout.Trim();
            string shortSha = sha.Length > 8 ? sha.Substring(0, 8) : sha;
            int width = 520;

            FlowLayoutPanel flow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Fill
            };
            Label title = new Label { Text = "Cherry-pick commit " + shortSha, AutoSize = true, Margin = new Padding(3, 0, 3, 4) };
            title.Font = new Font(Font.FontFamily, Font.Size * 1.5f);
            flow.Controls.Add(title);
            if (!string.IsNullOrEmpty(subject))
                flow.Controls.Add(new Label { Text = subject, AutoSize = true, MaximumSize = new Size(width, 0), Font = new Font(Font, FontStyle.Italic) });
            flow.Controls.Add(new Label
            {
                Text = "Apply the changes of this commit to the current branch (" + branch + ").",
                AutoSize = true, MaximumSize = new Size(width, 0), Margin = new Padding(3, 4, 3, 10)
            });
            flow.Controls.Add(checkCommit);
            flow.Controls.Add(checkOrigin);
            checkCommit.CheckedChanged += (s, e) => checkOrigin.Enabled = checkCommit.Checked;

            FlowLayoutPanel buttons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft, AutoSize = true, MinimumSize = new Size(width, 0), Margin = new Padding(3, 12, 3, 0)
            };
            Button btCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            Button btOk = new Button { Text = "Cherry-Pick", DialogResult = DialogResult.OK, AutoSize = true };
            buttons.Controls.Add(btCancel);
            buttons.Controls.Add(btOk);
            flow.Controls.Add(buttons);
            Controls.Add(flow);
            AcceptButton = btOk;
            CancelButton = btCancel;
            ClassTheme.Apply(this);
        }
    }
}
