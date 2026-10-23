using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace GitForce.Main.Right.Panels
{
    /// <summary>
    /// Graph of the commits of all branches: each commit is a dot on the lane of its branch,
    /// with lines to its parents, the branch and tag labels and the commit subject.
    /// </summary>
    public class PanelGraph : UserControl
    {
        private enum RefKind { Head, Local, Remote, Tag, Stash }

        private class GraphRef
        {
            public string Name;
            public RefKind Kind;
        }

        /// <summary>
        /// Line drawn in a row: from a lane at the top of the row (or the dot) to a lane at
        /// the bottom of the row (or the dot)
        /// </summary>
        private struct Segment
        {
            public int From, To;        // Lane numbers
            public bool FromDot, ToDot; // Starts at / ends at the dot (middle of the row) instead of the row edge
            public int Color;
        }

        private class GraphRow
        {
            public string Sha, ShortSha, Author, Subject;
            public string[] Parents;
            public DateTime Date;
            public List<GraphRef> Refs = new List<GraphRef>();
            public int Lane, Color;
            public bool IsHead;
            public bool IsStash;        // A stash, drawn on its own lane from the commit it was made on
            public readonly List<Segment> Segments = new List<Segment>();
            public int Lanes;           // Number of lanes used by this row
        }

        private const int LaneWidth = 14;
        private const int MaxLanes = 40;

        private static readonly Color[] LightColors = {
            Color.FromArgb(0, 114, 189), Color.FromArgb(217, 83, 25), Color.FromArgb(46, 160, 67),
            Color.FromArgb(162, 20, 47), Color.FromArgb(126, 47, 142), Color.FromArgb(0, 150, 160),
            Color.FromArgb(200, 140, 0), Color.FromArgb(100, 100, 100) };
        private static readonly Color[] DarkColors = {
            Color.FromArgb(86, 156, 214), Color.FromArgb(240, 130, 80), Color.FromArgb(106, 200, 110),
            Color.FromArgb(240, 100, 120), Color.FromArgb(190, 130, 230), Color.FromArgb(80, 200, 210),
            Color.FromArgb(230, 190, 80), Color.FromArgb(170, 170, 170) };

        private List<GraphRow> rows = new List<GraphRow>();

        /// <summary>
        /// Number of lanes of the widest row: the text of all rows starts after it
        /// </summary>
        private int lanesWidth = 1;
        private bool dirty = true;

        private readonly ToolStrip toolStrip = new ToolStrip();
        private readonly ToolStripButton btAll = new ToolStripButton("All branches") { CheckOnClick = true, Checked = true };
        private readonly ToolStripButton btRefresh = new ToolStripButton("Refresh");
        private readonly ToolStripLabel labelInfo = new ToolStripLabel();
        private readonly DataGridView grid = new DataGridView();
        private readonly ContextMenuStrip contextMenu = new ContextMenuStrip();

        public PanelGraph()
        {
            btAll.ToolTipText = "Show the commits of all branches, or only of the current branch";
            btAll.CheckedChanged += (s, e) => { dirty = true; LoadGraph(); };
            btRefresh.ToolTipText = "Refresh (F5)";
            btRefresh.Click += (s, e) => { dirty = true; LoadGraph(); };
            foreach (ToolStripButton bt in new[] { btAll, btRefresh })
                bt.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStrip.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip.Items.AddRange(new ToolStripItem[] {
                new ToolStripLabel("git Graph"), new ToolStripSeparator(), btAll, btRefresh, new ToolStripSeparator(), labelInfo });

            grid.Dock = DockStyle.Fill;
            grid.VirtualMode = true;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.RowHeadersVisible = false;
            grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.None;
            grid.BorderStyle = BorderStyle.None;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.EnableHeadersVisualStyles = false;
            grid.RowTemplate.Height = Math.Max(20, Font.Height + 7);
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Graph / Description", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 100, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Author", Width = 110, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Date", Width = 115, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SHA1", Width = 65, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.CellValueNeeded += GridCellValueNeeded;
            grid.CellPainting += GridCellPainting;
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) Describe(e.RowIndex); };
            // A click on a commit lists its files at the right of the log
            grid.SelectionChanged += (s, e) =>
            {
                GraphRow r = SelectedRow();
                if (r != null && grid.Focused && App.MainForm != null)
                    App.MainForm.ShowCommitFiles(r.Sha, r.IsStash ? r.ShortSha : null);
            };
            grid.CellMouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
                    grid.CurrentCell = grid.Rows[e.RowIndex].Cells[0];
            };
            grid.CellToolTipTextNeeded += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex < rows.Count && e.ColumnIndex == 0)
                {
                    GraphRow r = rows[e.RowIndex];
                    e.ToolTipText = r.ShortSha + "  " + r.Subject +
                        (r.Refs.Count > 0 ? Environment.NewLine + string.Join(", ", r.Refs.Select(x => x.Name)) : "");
                }
            };

            contextMenu.Opening += ContextMenuOpening;
            grid.ContextMenuStrip = contextMenu;

            Controls.Add(grid);
            Controls.Add(toolStrip);

            App.Refresh += () => { dirty = true; if (Visible) LoadGraph(); };
            VisibleChanged += (s, e) => { if (Visible && dirty) LoadGraph(); };
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Color window = ClassTheme.Map(SystemColors.Window);
            Color text = ClassTheme.Map(SystemColors.WindowText);
            grid.BackgroundColor = window;
            grid.DefaultCellStyle.BackColor = window;
            grid.DefaultCellStyle.ForeColor = text;
            grid.DefaultCellStyle.SelectionBackColor = ClassTheme.IsDark ? Color.FromArgb(38, 79, 120) : Color.FromArgb(204, 228, 255);
            grid.DefaultCellStyle.SelectionForeColor = text;
            grid.ColumnHeadersDefaultCellStyle.BackColor = ClassTheme.Map(SystemColors.Control);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = ClassTheme.Map(SystemColors.ControlText);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = grid.ColumnHeadersDefaultCellStyle.BackColor;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F5)
            {
                dirty = true;
                LoadGraph();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        #region Loading and the lane layout

        /// <summary>
        /// Read the history and lay it out
        /// </summary>
        private void LoadGraph()
        {
            dirty = false;
            ClassRepo repo = App.Repos.Current;
            List<GraphRow> list = new List<GraphRow>();
            if (repo != null)
            {
                string limit = Properties.Settings.Default.commitsRetrieveAll
                    ? string.Empty
                    : " -n " + Math.Max(Properties.Settings.Default.commitsRetrieveLast, 1000);
                string args = "log --topo-order --decorate=full" + (btAll.Checked ? " --exclude=refs/stash --all" : string.Empty) + limit +
                              " --format=%H%x1f%h%x1f%P%x1f%an%x1f%ct%x1f%D%x1f%s%x1e";
                ExecResult result = repo.Run(args);
                if (result.Success())
                    list = ParseLog(result.stdout);
                if (btAll.Checked)
                    AddStashes(repo, list);
            }
            LayoutLanes(list);
            rows = list;
            lanesWidth = Math.Min(MaxLanes, list.Count > 0 ? list.Max(r => r.Lanes) : 1);

            grid.RowCount = 0;
            grid.RowCount = rows.Count;
            labelInfo.Text = rows.Count + " commits";
            grid.Invalidate();
        }

        /// <summary>
        /// Show the stashes like branches: each one is a row just above the commit it was made
        /// on, linked to that commit only (the internal commits that hold the staged changes
        /// and the untracked files of a stash are not shown)
        /// </summary>
        private static void AddStashes(ClassRepo repo, List<GraphRow> list)
        {
            ExecResult result = repo.Run("stash list --format=%H%x1f%gd%x1f%P%x1f%an%x1f%ct%x1f%gs%x1e");
            if (!result.Success())
                return;
            List<GraphRow> stashes = new List<GraphRow>();
            foreach (string record in result.stdout.Split('\x1e'))
            {
                string[] f = record.Trim('\r', '\n').Split('\x1f');
                if (f.Length < 6)
                    continue;
                string[] parents = f[2].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                GraphRow r = new GraphRow
                {
                    Sha = f[0], ShortSha = f[1], IsStash = true,
                    Parents = parents.Length > 0 ? new[] { parents[0] } : new string[0],
                    Author = f[3], Subject = f[5]
                };
                long time;
                if (long.TryParse(f[4], out time))
                    r.Date = DateTimeOffset.FromUnixTimeSeconds(time).LocalDateTime;
                r.Refs.Add(new GraphRef { Name = f[1], Kind = RefKind.Stash });
                stashes.Add(r);
            }
            // From the oldest, so that the newest stash of a commit ends up on top
            for (int i = stashes.Count - 1; i >= 0; i--)
            {
                GraphRow s = stashes[i];
                int at = s.Parents.Length > 0 ? list.FindIndex(r => r.Sha == s.Parents[0]) : -1;
                if (at < 0)
                    continue;       // Made on a commit that is not in the list
                list.Insert(at, s);
            }
        }

        private static List<GraphRow> ParseLog(string text)
        {
            List<GraphRow> list = new List<GraphRow>();
            foreach (string record in text.Split('\x1e'))
            {
                string[] f = record.Trim('\r', '\n').Split('\x1f');
                if (f.Length < 7)
                    continue;
                GraphRow r = new GraphRow
                {
                    Sha = f[0],
                    ShortSha = f[1],
                    Parents = f[2].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries),
                    Author = f[3],
                    Subject = f[6]
                };
                long time;
                if (long.TryParse(f[4], out time))
                    r.Date = DateTimeOffset.FromUnixTimeSeconds(time).LocalDateTime;
                ParseRefs(r, f[5]);
                list.Add(r);
            }
            return list;
        }

        /// <summary>
        /// Decorations as given with --decorate=full: "HEAD -> refs/heads/main, refs/remotes/origin/main, tag: refs/tags/v1"
        /// </summary>
        private static void ParseRefs(GraphRow r, string decorations)
        {
            foreach (string part in decorations.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries))
            {
                string d = part.Trim();
                if (d.StartsWith("HEAD -> "))
                {
                    r.IsHead = true;
                    d = d.Substring(8);
                }
                if (d == "HEAD")
                {
                    r.IsHead = true;
                    r.Refs.Add(new GraphRef { Name = "HEAD", Kind = RefKind.Head });
                }
                else if (d.StartsWith("tag: refs/tags/"))
                    r.Refs.Add(new GraphRef { Name = d.Substring(15), Kind = RefKind.Tag });
                else if (d.StartsWith("refs/heads/"))
                    r.Refs.Insert(0, new GraphRef { Name = d.Substring(11), Kind = r.IsHead && r.Refs.Count == 0 ? RefKind.Head : RefKind.Local });
                else if (d.StartsWith("refs/remotes/"))
                {
                    if (!d.EndsWith("/HEAD"))
                        r.Refs.Add(new GraphRef { Name = d.Substring(13), Kind = RefKind.Remote });
                }
                else if (d.Length > 0)
                    r.Refs.Add(new GraphRef { Name = d, Kind = RefKind.Local });
            }
        }

        /// <summary>
        /// Assign lanes. Going from the newest commit down, each lane waits for a given commit
        /// (the parent of the commit above it in that lane). A commit takes the lane that waits
        /// for it (the leftmost if several do: the others join it there), its first parent
        /// continues in the same lane and the other parents of a merge get lanes of their own.
        /// </summary>
        private static void LayoutLanes(List<GraphRow> list)
        {
            List<string> lanes = new List<string>();     // Commit each lane waits for, null if free
            List<int> colors = new List<int>();          // Color of each lane
            int nextColor = 0;

            Func<int, int> freeLane = from =>
            {
                for (int i = Math.Max(0, from); i < lanes.Count; i++)
                    if (lanes[i] == null)
                        return i;
                for (int i = 0; i < lanes.Count; i++)
                    if (lanes[i] == null)
                        return i;
                lanes.Add(null);
                colors.Add(0);
                return lanes.Count - 1;
            };

            foreach (GraphRow r in list)
            {
                // The lane of this commit: the one waiting for it, or a new one (a branch tip)
                int lane = lanes.IndexOf(r.Sha);
                if (lane < 0)
                {
                    lane = freeLane(0);
                    colors[lane] = nextColor++;
                }
                r.Lane = lane;
                r.Color = colors[lane];

                // Upper half of the row: lanes that end at this commit, and lanes passing by
                for (int i = 0; i < lanes.Count; i++)
                {
                    if (lanes[i] == null)
                        continue;
                    if (lanes[i] == r.Sha)
                    {
                        r.Segments.Add(new Segment { From = i, To = lane, ToDot = true, Color = colors[i] });
                        lanes[i] = null;
                    }
                    else
                        r.Segments.Add(new Segment { From = i, To = i, Color = colors[i] });
                }

                // Lower half: a line to the lane of each parent. The first parent continues in
                // this lane unless another lane already waits for it.
                foreach (string parent in r.Parents)
                {
                    int pl = lanes.IndexOf(parent);
                    if (pl < 0)
                    {
                        if (lanes[lane] == null)
                            pl = lane;
                        else
                        {
                            pl = freeLane(lane + 1);
                            colors[pl] = nextColor++;
                        }
                        lanes[pl] = parent;
                    }
                    r.Segments.Add(new Segment { From = lane, To = pl, FromDot = true, Color = colors[pl] });
                }

                // Lanes freed at the right end are not drawn any more
                while (lanes.Count > 0 && lanes[lanes.Count - 1] == null)
                {
                    lanes.RemoveAt(lanes.Count - 1);
                    colors.RemoveAt(colors.Count - 1);
                }
                r.Lanes = lane + 1;
                foreach (Segment seg in r.Segments)
                    r.Lanes = Math.Max(r.Lanes, Math.Max(seg.From, seg.To) + 1);
            }
        }

        #endregion

        #region Grid

        private void GridCellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex >= rows.Count)
                return;
            GraphRow r = rows[e.RowIndex];
            switch (e.ColumnIndex)
            {
                case 0: e.Value = r.Subject; break;
                case 1: e.Value = r.Author; break;
                case 2: e.Value = r.Date.ToString("g", CultureInfo.CurrentCulture); break;
                case 3: e.Value = r.ShortSha; break;
            }
        }

        private static Color LaneColor(int index)
        {
            Color[] palette = ClassTheme.IsDark ? DarkColors : LightColors;
            return palette[((index % palette.Length) + palette.Length) % palette.Length];
        }

        /// <summary>
        /// Draw the graph, the labels of the branches and tags and the subject in the first column
        /// </summary>
        private void GridCellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= rows.Count || e.ColumnIndex != 0)
                return;
            GraphRow r = rows[e.RowIndex];
            Graphics g = e.Graphics;
            Rectangle b = e.CellBounds;
            bool selected = (e.State & DataGridViewElementStates.Selected) != 0;
            Color back = selected ? e.CellStyle.SelectionBackColor : e.CellStyle.BackColor;
            Color fore = selected ? e.CellStyle.SelectionForeColor : e.CellStyle.ForeColor;
            using (SolidBrush brush = new SolidBrush(back))
                g.FillRectangle(brush, b);

            GraphicsState state = g.Save();
            g.SetClip(b);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float top = b.Top, mid = b.Top + b.Height / 2f, bottom = b.Bottom;
            Func<int, float> x = lane => b.Left + 8 + Math.Min(lane, MaxLanes) * LaneWidth;

            foreach (Segment s in r.Segments)
            {
                float x1 = x(s.From), x2 = x(s.To);
                float y1 = s.FromDot ? mid : top, y2 = s.ToDot ? mid : bottom;
                if (x1 == x2)
                {
                    // Straight lines are filled to the pixel so that they join the next row without a gap
                    using (SolidBrush brush = new SolidBrush(LaneColor(s.Color)))
                    {
                        g.SmoothingMode = SmoothingMode.None;
                        g.FillRectangle(brush, (int)x1 - 1, (int)y1, 2, (int)Math.Ceiling(y2) - (int)y1);
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                    }
                }
                else
                {
                    using (Pen pen = new Pen(LaneColor(s.Color), 2f))
                    {
                        float c = (y2 - y1) * 0.6f;
                        g.DrawBezier(pen, x1, y1 - 0.5f, x1, y1 + c, x2, y2 - c, x2, y2 + 0.5f);
                    }
                }
            }

            // The dot of the commit: a ring for the checked out commit, a smaller dot for a merge
            float cx = x(r.Lane), radius = r.Parents.Length > 1 ? 3.5f : 4.5f;
            Color dot = LaneColor(r.Color);
            if (r.IsStash)
            {
                // A stash is a hollow square
                using (SolidBrush brush = new SolidBrush(back))
                    g.FillRectangle(brush, cx - 4.5f, mid - 4.5f, 9, 9);
                using (Pen pen = new Pen(dot, 2f))
                    g.DrawRectangle(pen, cx - 4f, mid - 4f, 8, 8);
            }
            else
            {
                using (SolidBrush brush = new SolidBrush(r.IsHead ? back : dot))
                    g.FillEllipse(brush, cx - radius, mid - radius, radius * 2, radius * 2);
                using (Pen pen = new Pen(r.IsHead ? dot : back, r.IsHead ? 2.5f : 1f))
                    g.DrawEllipse(pen, cx - radius, mid - radius, radius * 2, radius * 2);
            }

            // Labels of the branches and tags, then the subject
            int textX = (int)x(lanesWidth) + 2;
            Font font = e.CellStyle.Font ?? grid.Font;
            foreach (GraphRef gr in r.Refs)
                textX = DrawLabel(g, gr, font, textX, b, r.IsHead) + 4;
            g.Restore(state);

            Rectangle textRect = new Rectangle(textX, b.Top, Math.Max(0, b.Right - textX), b.Height);
            TextRenderer.DrawText(g, r.Subject, font, textRect, fore,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
            e.Handled = true;
        }

        /// <summary>
        /// Draw a label: the current branch in bold, local branches in green, remote branches in
        /// blue, tags in yellow. Returns the right edge of the label.
        /// </summary>
        private static int DrawLabel(Graphics g, GraphRef gr, Font font, int left, Rectangle b, bool head)
        {
            bool dark = ClassTheme.IsDark;
            Color fill, border;
            switch (gr.Kind)
            {
                case RefKind.Head:
                    fill = dark ? Color.FromArgb(40, 90, 50) : Color.FromArgb(200, 240, 200);
                    border = dark ? Color.FromArgb(110, 200, 120) : Color.FromArgb(40, 140, 60);
                    break;
                case RefKind.Local:
                    fill = dark ? Color.FromArgb(35, 70, 45) : Color.FromArgb(225, 245, 225);
                    border = dark ? Color.FromArgb(90, 160, 100) : Color.FromArgb(80, 160, 90);
                    break;
                case RefKind.Remote:
                    fill = dark ? Color.FromArgb(35, 55, 85) : Color.FromArgb(220, 232, 250);
                    border = dark ? Color.FromArgb(100, 140, 210) : Color.FromArgb(80, 120, 190);
                    break;
                case RefKind.Stash:
                    fill = dark ? Color.FromArgb(70, 45, 70) : Color.FromArgb(245, 225, 245);
                    border = dark ? Color.FromArgb(200, 130, 200) : Color.FromArgb(160, 80, 160);
                    break;
                default:
                    fill = dark ? Color.FromArgb(80, 70, 30) : Color.FromArgb(255, 243, 200);
                    border = dark ? Color.FromArgb(210, 180, 80) : Color.FromArgb(200, 160, 40);
                    break;
            }
            string text = gr.Kind == RefKind.Tag ? "◆ " + gr.Name : gr.Kind == RefKind.Stash ? "▣ " + gr.Name : gr.Name;
            using (Font f = gr.Kind == RefKind.Head ? new Font(font, FontStyle.Bold) : null)
            {
                Font use = f ?? font;
                Size size = TextRenderer.MeasureText(g, text, use, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                Rectangle rect = new Rectangle(left, b.Top + (b.Height - size.Height) / 2 - 1, size.Width + 8, size.Height + 2);
                using (GraphicsPath path = RoundRect(rect, 3))
                using (SolidBrush brush = new SolidBrush(fill))
                using (Pen pen = new Pen(border))
                {
                    g.FillPath(brush, path);
                    g.DrawPath(pen, path);
                }
                TextRenderer.DrawText(g, text, use, rect, dark ? Color.White : Color.Black,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                return rect.Right;
            }
        }

        private static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.Left, r.Top, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        #endregion

        #region Commands

        private GraphRow SelectedRow()
        {
            if (grid.CurrentCell == null || grid.CurrentCell.RowIndex >= rows.Count)
                return null;
            return rows[grid.CurrentCell.RowIndex];
        }

        /// <summary>
        /// Show the description of a commit; the dialog buttons move to the next and previous commits
        /// </summary>
        private void Describe(int index)
        {
            using (FormShowChangelist form = new FormShowChangelist())
            {
                DialogResult dlg;
                do
                {
                    grid.CurrentCell = grid.Rows[index].Cells[0];
                    form.LoadChangelist(rows[index].Sha);
                    dlg = form.ShowDialog();
                    if (dlg == DialogResult.Yes && index > 0)
                        index--;
                    if (dlg == DialogResult.No && index < rows.Count - 1)
                        index++;
                } while (dlg != DialogResult.Cancel);
            }
        }

        private void ContextMenuOpening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            contextMenu.Items.Clear();
            GraphRow r = SelectedRow();
            if (r == null)
            {
                e.Cancel = true;
                return;
            }
            ClassRepo repo = App.Repos.Current;
            if (r.IsStash)
            {
                string stash = r.ShortSha;
                Action<string> run = cmd =>
                {
                    ExecResult res = repo.RunCmd(cmd);
                    if (!res.Success())
                    {
                        if (new ClassStatus(repo).GetUnmerged().Count > 0)
                            ClassMerge.ResolveConflicts(repo, null);
                        else
                            MessageBox.Show(res.stderr.Trim(), stash, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    App.DoRefresh();
                };
                contextMenu.Items.Add(new ToolStripMenuItem("Apply " + stash, null, (s, a) => run("stash apply " + stash)));
                contextMenu.Items.Add(new ToolStripMenuItem("Pop " + stash, null, (s, a) => run("stash pop " + stash)));
                contextMenu.Items.Add(new ToolStripMenuItem("Drop " + stash + "...", null, (s, a) =>
                {
                    if (MessageBox.Show("Remove " + stash + " (" + r.Subject + ")?\n\nIts changes will be lost.", "Drop stash",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                        run("stash drop " + stash);
                }));
                contextMenu.Items.Add(new ToolStripSeparator());
                contextMenu.Items.Add(new ToolStripMenuItem("Stashes...", null, (s, a) => FormStashList.ShowStashes(repo)));
                contextMenu.Items.Add(new ToolStripMenuItem("Copy SHA1", null, (s, a) => Clipboard.SetText(r.Sha)));
                return;
            }
            contextMenu.Items.Add(new ToolStripMenuItem("Describe Changelist...", null, (s, a) => Describe(grid.CurrentCell.RowIndex)));
            contextMenu.Items.Add(new ToolStripMenuItem("Edit Commit Message...", null, (s, a) => ClassHistoryEdit.EditMessage(repo, r.Sha)));
            contextMenu.Items.Add(new ToolStripMenuItem("Edit Commit Author...", null, (s, a) => ClassHistoryEdit.EditAuthor(repo, r.Sha)));
            contextMenu.Items.Add(new ToolStripMenuItem("Copy SHA1", null, (s, a) => Clipboard.SetText(r.Sha)));
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(new ToolStripMenuItem("New Branch Here...", null, (s, a) => ClassHistoryEdit.NewBranchAt(repo, r.Sha)));
            contextMenu.Items.Add(new ToolStripMenuItem("New Tag Here...", null, (s, a) => ClassHistoryEdit.NewTag(repo, r.Sha)));

            // One submenu for each branch and tag on this commit
            string current = repo.Run("rev-parse --abbrev-ref HEAD").stdout.Trim();
            bool first = true;
            foreach (GraphRef gr in r.Refs)
            {
                if (gr.Kind == RefKind.Head && gr.Name == "HEAD")
                    continue;
                if (first)
                {
                    contextMenu.Items.Add(new ToolStripSeparator());
                    first = false;
                }
                string name = gr.Name;
                ToolStripMenuItem m = new ToolStripMenuItem((gr.Kind == RefKind.Tag ? "Tag " : "Branch ") + name);
                switch (gr.Kind)
                {
                    case RefKind.Tag:
                        m.DropDownItems.Add(new ToolStripMenuItem("Merge into " + current, null, (s, a) => ClassMerge.MergeBranch(repo, name)));
                        m.DropDownItems.Add(new ToolStripMenuItem("Rename Tag...", null, (s, a) => ClassHistoryEdit.RenameTag(repo, name)));
                        m.DropDownItems.Add(new ToolStripMenuItem("Delete Tag", null, (s, a) => ClassHistoryEdit.DeleteTag(repo, name)));
                        break;
                    case RefKind.Remote:
                        m.DropDownItems.Add(new ToolStripMenuItem("Merge into " + current, null, (s, a) => ClassMerge.MergeBranch(repo, name)));
                        break;
                    default:
                        bool isCurrent = name == current;
                        m.DropDownItems.Add(new ToolStripMenuItem("Switch to " + name, null, (s, a) =>
                        {
                            repo.Branches.Refresh();
                            repo.Branches.SwitchTo(name);
                            App.DoRefresh();
                        }) { Enabled = !isCurrent });
                        m.DropDownItems.Add(new ToolStripMenuItem("Merge into " + current, null, (s, a) => ClassMerge.MergeBranch(repo, name)) { Enabled = !isCurrent });
                        m.DropDownItems.Add(new ToolStripMenuItem("Rename Branch...", null, (s, a) => ClassHistoryEdit.RenameBranch(repo, name)));
                        break;
                }
                contextMenu.Items.Add(m);
            }
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(new ToolStripMenuItem("Refresh", null, (s, a) => { dirty = true; LoadGraph(); }, Keys.F5));
        }

        #endregion
    }
}
