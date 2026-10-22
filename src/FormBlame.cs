using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace GitForce
{
    /// <summary>
    /// Blame view: each line of a file at a given commit with the commit, author and date that
    /// last changed it. Lines of the same commit are grouped; double-click describes the commit.
    /// </summary>
    public class FormBlame : Form
    {
        private class Line
        {
            public string Sha, Author, Summary, Text;
            public DateTime Date;
            public int Number;
            public bool FirstOfGroup;
            public int Group;
        }

        private readonly ClassRepo repo;
        private readonly List<Line> lines = new List<Line>();
        private List<ClassSyntax.Token>[] tokens;
        private readonly DataGridView grid = new DataGridView();
        private readonly Label labelInfo = new Label();

        public static void ShowBlame(ClassRepo repo, string rev, string path)
        {
            new FormBlame(repo, rev, path).Show();
        }

        private FormBlame(ClassRepo repo, string rev, string path)
        {
            this.repo = repo;
            Text = "Blame: " + path + " @ " + (rev.Length > 10 ? rev.Substring(0, 7) : rev);
            Icon = Properties.Resources.git;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1150, 750);

            labelInfo.Dock = DockStyle.Top;
            labelInfo.Height = 22;
            labelInfo.TextAlign = ContentAlignment.MiddleLeft;
            labelInfo.Padding = new Padding(4, 0, 0, 0);

            grid.Dock = DockStyle.Fill;
            grid.VirtualMode = true;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = grid.AllowUserToDeleteRows = grid.AllowUserToResizeRows = false;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.None;
            grid.BorderStyle = BorderStyle.None;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.EnableHeadersVisualStyles = false;
            grid.DefaultCellStyle.Font = Properties.Settings.Default.commitFont;
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            grid.RowTemplate.Height = grid.DefaultCellStyle.Font.Height + 3;
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Commit", Width = 70 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Author", Width = 120 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Date", Width = 85 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Line", Width = 50, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            foreach (DataGridViewColumn c in grid.Columns)
                c.SortMode = DataGridViewColumnSortMode.NotSortable;
            grid.CellValueNeeded += GridCellValueNeeded;
            grid.CellFormatting += GridCellFormatting;
            grid.CellPainting += (s, e) =>
            {
                if (tokens != null && e.ColumnIndex == 4 && e.RowIndex >= 0 && e.RowIndex < lines.Count)
                    ClassSyntax.PaintCell(e, lines[e.RowIndex].Text, tokens[e.RowIndex]);
            };
            grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= lines.Count)
                    return;
                using (FormShowChangelist form = new FormShowChangelist())
                {
                    form.LoadChangelist(lines[e.RowIndex].Sha);
                    form.ShowDialog();
                }
            };
            grid.CellToolTipTextNeeded += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex < lines.Count && e.ColumnIndex < 3)
                {
                    Line l = lines[e.RowIndex];
                    e.ToolTipText = l.Sha.Substring(0, 10) + "  " + l.Author + "  " + l.Date.ToString("g") + Environment.NewLine + l.Summary;
                }
            };

            Controls.Add(grid);
            Controls.Add(labelInfo);
            ClassTheme.Apply(this);
            Color window = ClassTheme.Map(SystemColors.Window);
            grid.BackgroundColor = grid.DefaultCellStyle.BackColor = window;
            grid.DefaultCellStyle.ForeColor = ClassTheme.Map(SystemColors.WindowText);
            grid.DefaultCellStyle.SelectionBackColor = ClassTheme.IsDark ? Color.FromArgb(38, 79, 120) : Color.FromArgb(204, 228, 255);
            grid.DefaultCellStyle.SelectionForeColor = grid.DefaultCellStyle.ForeColor;
            grid.ColumnHeadersDefaultCellStyle.BackColor = ClassTheme.Map(SystemColors.Control);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = ClassTheme.Map(SystemColors.ControlText);

            Load += (s, e) => LoadBlame(rev, path);
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

        /// <summary>
        /// Read "git blame --porcelain": a header line "sha orig final [count]" for each line,
        /// the details of a commit the first time it appears, then the line itself after a TAB
        /// </summary>
        private void LoadBlame(string rev, string path)
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                string output = DiffViewControl.RunGitRaw(repo.Path, "blame --porcelain " + rev + " -- \"" + path + "\"");
                var info = new Dictionary<string, Line>();
                Line current = null;
                foreach (string raw in output.Split('\n'))
                {
                    if (raw.StartsWith("\t"))
                    {
                        if (current != null)
                        {
                            Line known = info[current.Sha];
                            lines.Add(new Line { Sha = current.Sha, Number = current.Number, Author = known.Author, Date = known.Date,
                                                 Summary = known.Summary, Text = ExpandTabs(raw.Substring(1).TrimEnd('\r')) });
                        }
                        current = null;
                        continue;
                    }
                    string[] f = raw.TrimEnd('\r').Split(' ');
                    if (current == null && f.Length >= 3 && f[0].Length == 40)
                    {
                        current = new Line { Sha = f[0], Number = int.Parse(f[2]) };
                        if (!info.ContainsKey(f[0]))
                            info[f[0]] = new Line { Sha = f[0] };
                        continue;
                    }
                    if (current == null)
                        continue;
                    Line commit = info[current.Sha];
                    string value = raw.Length > f[0].Length ? raw.Substring(f[0].Length + 1).TrimEnd('\r') : string.Empty;
                    switch (f[0])
                    {
                        case "author": commit.Author = value; break;
                        case "author-time":
                            long t;
                            if (long.TryParse(value, out t))
                                commit.Date = DateTimeOffset.FromUnixTimeSeconds(t).LocalDateTime;
                            break;
                        case "summary": commit.Summary = value; break;
                    }
                }

                // Group the consecutive lines of a commit
                int group = 0;
                for (int i = 0; i < lines.Count; i++)
                {
                    lines[i].FirstOfGroup = i == 0 || lines[i - 1].Sha != lines[i].Sha;
                    if (lines[i].FirstOfGroup && i > 0)
                        group++;
                    lines[i].Group = group;
                }
                ClassSyntax syntax = ClassSyntax.ForFile(path);
                tokens = syntax != null ? syntax.TokenizeAll(lines.ConvertAll(l => l.Text)) : null;
                grid.RowCount = lines.Count;
                labelInfo.Text = lines.Count == 0
                    ? "No blame information (the file may not exist in this commit)."
                    : string.Format("{0} lines, {1} commits. Double-click a line to see its commit.", lines.Count, info.Count);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void GridCellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex >= lines.Count)
                return;
            Line l = lines[e.RowIndex];
            switch (e.ColumnIndex)
            {
                case 0: e.Value = l.FirstOfGroup ? l.Sha.Substring(0, 7) : string.Empty; break;
                case 1: e.Value = l.FirstOfGroup ? l.Author : string.Empty; break;
                case 2: e.Value = l.FirstOfGroup ? l.Date.ToString("d", CultureInfo.CurrentCulture) : string.Empty; break;
                case 3: e.Value = l.Number.ToString(); break;
                case 4: e.Value = l.Text; break;
            }
        }

        private void GridCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= lines.Count)
                return;
            // Alternate the background of the groups of lines so that each commit stands out
            if (lines[e.RowIndex].Group % 2 == 1)
                e.CellStyle.BackColor = ClassTheme.IsDark ? Color.FromArgb(38, 38, 42) : Color.FromArgb(244, 246, 250);
            if (e.ColumnIndex < 4)
                e.CellStyle.ForeColor = Color.Gray;
        }

        private static string ExpandTabs(string s)
        {
            if (s.IndexOf('\t') < 0)
                return s;
            var sb = new System.Text.StringBuilder();
            foreach (char c in s)
            {
                if (c == '\t') sb.Append(' ', 4 - sb.Length % 4);
                else sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
