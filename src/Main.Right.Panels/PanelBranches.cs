using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GitForce.Main.Right.Panels
{
    public partial class PanelBranches : UserControl
    {
        enum BranchIcons { Branch, BranchIdle, BranchSelected };

        /// <summary>
        /// Tracks the last repo for which we showed the detached HEAD warning
        /// </summary>
        private static string _lastDetachedWarningRepo;

        /// <summary>
        /// Separator between a branch name and the description of its tip commit in a node text
        /// </summary>
        private const string InfoSeparator = "    ";

        public PanelBranches()
        {
            InitializeComponent();

            if (Properties.Settings.Default.ModernIcons)
            {
                // Line icons, in the order of BranchIcons: the groups, a branch, the current branch
                imageList.Images.Clear();
                imageList.ColorDepth = ColorDepth.Depth32Bit;
                imageList.ImageSize = new Size(16, 16);
                imageList.Images.Add(ClassIcons.Render("btBranches", 16));
                imageList.Images.Add(ClassIcons.Render("bidle", 16, null, 2.6f));
                imageList.Images.Add(ClassIcons.Render("bcurrent", 16));
            }

            // Line icons of the toolbar and of the context menu
            ClassIcons.Attach(contextMenu);

            // Branch names are drawn normally, followed by their tip commit in a dimmed color
            treeBranches.DrawMode = TreeViewDrawMode.OwnerDrawText;
            treeBranches.DrawNode += TreeBranchesDrawNode;

            App.Refresh += BranchesRefresh;
        }

        /// <summary>
        /// Refresh the tree view of branches
        /// </summary>
        public void BranchesRefresh()
        {
            // Use predefined [0] for local and [1] for remote branches
            treeBranches.Nodes[0].Nodes.Clear();
            treeBranches.Nodes[1].Nodes.Clear();

            if (App.Repos.Current != null)
            {
                ClassBranches branches = App.Repos.Current.Branches;
                branches.Refresh();

                // Warn user if switching to a repository in detached HEAD state
                if (Properties.Settings.Default.WarnOnDetachedHead && branches.IsDetached())
                {
                    string repoPath = App.Repos.Current.Path;
                    if (_lastDetachedWarningRepo != repoPath)
                    {
                        _lastDetachedWarningRepo = repoPath;
                        MessageBox.Show(
                            "This repository is in DETACHED HEAD state.\n\n" +
                            "You are not on a branch. Push and pull operations will not work.\n\n" +
                            "To resume normal work, switch to a branch using the Branches panel.",
                            "Detached HEAD",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    // Reset tracking when switching to a non-detached repo
                    _lastDetachedWarningRepo = null;
                }

                Dictionary<string, string> tips = GetBranchTips();

                // Add all local branches to the tree
                foreach (string s in branches.Local)
                {
                    TreeNode tn = new TreeNode(NodeText(s, tips, "refs/heads/"), (int)BranchIcons.BranchIdle, (int)BranchIcons.BranchIdle);
                    tn.Tag = s;
                    if (s == branches.Current)
                        tn.SelectedImageIndex = tn.ImageIndex = (int)BranchIcons.BranchSelected;
                    treeBranches.Nodes[0].Nodes.Add(tn);
                }

                // Add all remote branches to the tree
                foreach (string s in branches.Remote)
                {
                    TreeNode tn = new TreeNode(NodeText(s, tips, "refs/remotes/"), (int)BranchIcons.BranchIdle, (int)BranchIcons.BranchIdle);
                    tn.Tag = s;
                    treeBranches.Nodes[1].Nodes.Add(tn);
                }
            }
            // By default, expand branches
            treeBranches.ExpandAll();
        }

        /// <summary>
        /// Returns the description of the commit each branch points to, keyed by the full ref name:
        /// abbreviated SHA, the tags on that commit, and the commit subject
        /// </summary>
        private static Dictionary<string, string> GetBranchTips()
        {
            Dictionary<string, string> tips = new Dictionary<string, string>();

            // Collect tags by the commit they point to (annotated tags are peeled to their commit)
            Dictionary<string, List<string>> tags = new Dictionary<string, List<string>>();
            ExecResult result = App.Repos.Current.Run("for-each-ref --format=%(objectname)%09%(*objectname)%09%(refname:short) refs/tags");
            if (result.Success())
            {
                foreach (string line in result.stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] f = line.Split('\t');
                    if (f.Length < 3)
                        continue;
                    string sha = f[1].Length > 0 ? f[1] : f[0];
                    if (!tags.ContainsKey(sha))
                        tags[sha] = new List<string>();
                    tags[sha].Add(f[2]);
                }
            }

            result = App.Repos.Current.Run("for-each-ref --format=%(refname)%09%(objectname)%09%(objectname:short)%09%(subject) refs/heads refs/remotes");
            if (result.Success())
            {
                foreach (string line in result.stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] f = line.Split(new[] { '\t' }, 4);
                    if (f.Length < 4)
                        continue;
                    string subject = f[3].Length > 100 ? f[3].Substring(0, 100) + "..." : f[3];
                    string info = f[2] + "  ";
                    if (tags.ContainsKey(f[1]))
                        info += "[" + string.Join(", ", tags[f[1]]) + "]  ";
                    tips[f[0]] = "\u25CF " + info + subject;
                }
            }
            return tips;
        }

        /// <summary>
        /// Text of a branch node: the branch name followed by the description of its tip commit.
        /// Branch names as listed by "git branch -a" may be symbolic ("origin/HEAD -> origin/main"),
        /// those are left as they are.
        /// </summary>
        private static string NodeText(string branch, Dictionary<string, string> tips, string prefix)
        {
            string info;
            if (tips.TryGetValue(prefix + branch, out info))
                return branch + InfoSeparator + info;
            return branch;
        }

        /// <summary>
        /// Draw a branch node: its name in the normal color, the commit description dimmed
        /// </summary>
        private void TreeBranchesDrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            if (e.Bounds.IsEmpty)
                return;
            string text = e.Node.Text;
            string name = text, info = string.Empty;
            int sep = e.Node.Tag != null ? text.IndexOf(InfoSeparator, StringComparison.Ordinal) : -1;
            if (sep > 0)
            {
                name = text.Substring(0, sep);
                info = text.Substring(sep);
            }

            Font font = e.Node.NodeFont ?? treeBranches.Font;
            bool selected = (e.State & TreeNodeStates.Selected) != 0;
            bool focused = selected && treeBranches.Focused;
            Color back = focused ? SystemColors.Highlight : treeBranches.BackColor;
            Color fore = focused ? SystemColors.HighlightText : treeBranches.ForeColor;
            Color dim = focused ? SystemColors.HighlightText : Color.Gray;

            Size nameSize = TextRenderer.MeasureText(e.Graphics, name, font, Size.Empty, TextFormatFlags.NoPadding);
            Size infoSize = TextRenderer.MeasureText(e.Graphics, info, font, Size.Empty, TextFormatFlags.NoPadding);
            Rectangle area = new Rectangle(e.Bounds.X, e.Bounds.Y, Math.Max(e.Bounds.Width, nameSize.Width + infoSize.Width + 4), e.Bounds.Height);
            using (SolidBrush brush = new SolidBrush(back))
                e.Graphics.FillRectangle(brush, area);
            if (selected && !focused)
                ControlPaint.DrawFocusRectangle(e.Graphics, area, fore, back);

            TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix;
            Rectangle r = new Rectangle(area.X + 2, area.Y, nameSize.Width, area.Height);
            TextRenderer.DrawText(e.Graphics, name, font, r, fore, flags);
            if (info.Length > 0)
            {
                r = new Rectangle(r.Right, area.Y, infoSize.Width + 2, area.Height);
                TextRenderer.DrawText(e.Graphics, info, font, r, dim, flags);
            }
        }

        /// <summary>
        /// Helper function that returns selected branch name in the tree view or null
        /// if no valid branch node was selected
        /// </summary>
        private string GetSelectedBranch()
        {
            if (treeBranches.SelectedNode != null && treeBranches.SelectedNode.Tag != null)
                return treeBranches.SelectedNode.Tag.ToString();
            return null;
        }

        /// <summary>
        /// Right-mouse button opens a popup with the context menu
        /// </summary>
        private void TreeBranchesMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && App.Repos.Current != null)
            {
                // Build the context menu to be shown
                treeBranches.SelectedNode = treeBranches.GetNodeAt(e.X, e.Y);
                contextMenu.Items.Clear();
                contextMenu.Items.AddRange(GetContextMenu(contextMenu));
            }
        }

        /// <summary>
        /// Builds ands returns a context menu for branches
        /// </summary>
        public ToolStripItemCollection GetContextMenu(ToolStrip owner)
        {
            // Menus are set in this order:
            //  [0]  New...        -> always open a dialog
            //  [1]  Delete...     -> always open a dialog
            //  [2]  Switch to     -> dialog or switch: local branches only, different from current
            //  [3]  Merge with    -> dialog or merge: branch different from current

            ToolStripMenuItem mNew = new ToolStripMenuItem("New...", null, MenuNewBranchClick);
            ToolStripMenuItem mDelete = new ToolStripMenuItem("Delete...", null, MenuDeleteBranchClick);
            ToolStripMenuItem mSwitchTo = new ToolStripMenuItem("Switch to...", null, MenuSwitchClick);
            ToolStripMenuItem mMergeWith = new ToolStripMenuItem("Merge with...", null, MenuMergeClick);
            ToolStripMenuItem mRename = new ToolStripMenuItem("Rename...", null, MenuRenameClick) { Enabled = false };

            if (App.Repos.Current != null)
            {
                ClassBranches branches = App.Repos.Current.Branches;
                string sel = GetSelectedBranch();

                if (sel != null)
                {
                    if (sel != branches.Current && branches.Local.IndexOf(sel) >= 0)
                        mSwitchTo = new ToolStripMenuItem("Switch to " + sel, null, TreeBranchesDoubleClick);

                    if (sel != branches.Current &&
                        (branches.Local.IndexOf(sel) >= 0 || branches.Remote.IndexOf(sel) >= 0))
                        mMergeWith = new ToolStripMenuItem("Merge " + sel + " into " + branches.Current, null, MenuMergeSelectedClick);

                    if (branches.Local.IndexOf(sel) >= 0)
                    {
                        mRename.Text = "Rename " + sel + "...";
                        mRename.Enabled = true;
                    }
                }
                else
                    mDelete.Enabled = mSwitchTo.Enabled = mMergeWith.Enabled = false;
            }
            else
                mNew.Enabled = mDelete.Enabled = mSwitchTo.Enabled = mMergeWith.Enabled = false;

            ToolStripItemCollection menu = new ToolStripItemCollection(owner, new ToolStripItem[] {
                mNew, mDelete, mSwitchTo, mMergeWith, mRename });

            return menu;
        }

        #region Operations on a branch

        /// <summary>
        /// Double-clicking on a local branch name will switch to it.
        /// Also a context menu handler.
        /// </summary>
        private void TreeBranchesDoubleClick(object sender, EventArgs e)
        {
            App.Repos.Current.Branches.SwitchTo(GetSelectedBranch());
            App.DoRefresh();
        }

        /// <summary>
        /// Switch to a branch using a dialog to select the branch
        /// </summary>
        private static void MenuSwitchClick(object sender, EventArgs e)
        {
            FormSwitchToBranch switchBranch = new FormSwitchToBranch();
            if (switchBranch.ShowDialog() == DialogResult.OK)
                App.DoRefresh();
        }

        /// <summary>
        /// Create a new branch using a dialog to select the branch
        /// </summary>
        private static void MenuNewBranchClick(object sender, EventArgs e)
        {
            FormNewBranch newBranch = new FormNewBranch();
            if (newBranch.ShowDialog() == DialogResult.OK)
                App.DoRefresh();
        }

        /// <summary>
        /// Delete a branch using a dialog to select the branch
        /// </summary>
        private void MenuDeleteBranchClick(object sender, EventArgs e)
        {
            FormDeleteBranch delBranch = new FormDeleteBranch(GetSelectedBranch());
            if (delBranch.ShowDialog() == DialogResult.OK)
                App.DoRefresh();
        }

        /// <summary>
        /// Merge the selected branch into the current one
        /// </summary>
        private void MenuMergeSelectedClick(object sender, EventArgs e)
        {
            string sel = GetSelectedBranch();
            if (sel != null)
                ClassMerge.MergeBranch(App.Repos.Current, sel);
        }

        /// <summary>
        /// Rename the selected local branch
        /// </summary>
        private void MenuRenameClick(object sender, EventArgs e)
        {
            string sel = GetSelectedBranch();
            if (sel != null)
                ClassHistoryEdit.RenameBranch(App.Repos.Current, sel);
        }

        /// <summary>
        /// Merge branch using a dialog to select the branch
        /// </summary>
        private void MenuMergeClick(object sender, EventArgs e)
        {
            FormMergeBranch mergeBranch = new FormMergeBranch();
            if (mergeBranch.ShowDialog() == DialogResult.OK)
                App.DoRefresh();
        }

        #endregion
    }
}
