using System;
using System.Drawing;
using System.Windows.Forms;

namespace GitForce
{
    /// <summary>
    /// Built-in diff viewer window. It runs "git diff" with the same arguments that would have
    /// been passed to "git difftool" and shows the result in a DiffViewControl, so GitForce can
    /// show differences without any external diff utility installed.
    /// This form is not modal, so user can keep several diffs open.
    /// </summary>
    public class FormDiffViewer : Form
    {
        private readonly DiffViewControl diff = new DiffViewControl { Dock = DockStyle.Fill };

        public FormDiffViewer(string title, string diffArgs, Func<string, ExecResult> runGit)
        {
            Text = "Diff: " + title;
            Icon = Properties.Resources.git;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1200, 750);
            MinimumSize = new Size(500, 300);

            Controls.Add(diff);
            ClassTheme.Apply(this);

            Load += (s, e) => diff.ShowGitDiff(diffArgs, runGit);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
