using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace GitForce
{
    /// <summary>
    /// Built-in diff viewer. It runs "git diff" with the same arguments that would have been
    /// passed to "git difftool" and shows the result side by side or as a unified diff, so
    /// GitForce can show differences without any external diff utility installed.
    /// This form is not modal, so user can keep several diffs open.
    /// </summary>
    public class FormDiffViewer : Form
    {
        /// <summary>
        /// Kind of a displayed line
        /// </summary>
        private enum RowKind { Context, Changed, Deleted, Added, Hunk, Info }

        /// <summary>
        /// One displayed line. In unified mode only one side is set for deleted/added lines.
        /// </summary>
        private class Row
        {
            public RowKind Kind;
            public int OldNo, NewNo;            // Line numbers, 0 if not applicable
            public string OldText = string.Empty;
            public string NewText = string.Empty;
        }

        /// <summary>
        /// Parsed diff of a single file
        /// </summary>
        private class FileDiff
        {
            public string Name = string.Empty;
            public readonly List<Row> SideBySide = new List<Row>();
            public readonly List<Row> Unified = new List<Row>();
            public override string ToString() { return Name; }
        }

        /// <summary>
        /// Runs a git command in the proper directory and returns its result
        /// </summary>
        private readonly Func<string, ExecResult> runGit;

        /// <summary>
        /// Arguments to "git diff": revisions/options followed by " -- " and the file list
        /// </summary>
        private readonly string diffArgs;

        private List<FileDiff> files = new List<FileDiff>();
        private List<Row> rows = new List<Row>();

        private readonly ToolStrip toolStrip = new ToolStrip();
        private readonly ToolStripComboBox comboFiles = new ToolStripComboBox();
        private readonly ToolStripButton btSideBySide = new ToolStripButton("Side by side");
        private readonly ToolStripButton btWholeFile = new ToolStripButton("Whole file");
        private readonly ToolStripButton btPrev = new ToolStripButton("Previous change");
        private readonly ToolStripButton btNext = new ToolStripButton("Next change");
        private readonly ToolStripButton btRefresh = new ToolStripButton("Refresh");
        private readonly ToolStripLabel labelStats = new ToolStripLabel();
        private readonly DataGridView grid = new DataGridView();

        // Colors: the light palette and its dark counterpart
        private Color colAdded, colDeleted, colAddedText, colDeletedText, colHunk, colEmpty, colNumbers, colNumbersText;

        public FormDiffViewer(string title, string diffArgs, Func<string, ExecResult> runGit)
        {
            this.diffArgs = diffArgs;
            this.runGit = runGit;

            Text = "Diff: " + title;
            Icon = Properties.Resources.git;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1200, 750);
            MinimumSize = new Size(500, 300);
            KeyPreview = true;

            comboFiles.DropDownStyle = ComboBoxStyle.DropDownList;
            comboFiles.AutoSize = false;
            comboFiles.Width = 350;
            comboFiles.SelectedIndexChanged += (s, e) => ShowFile();
            btSideBySide.CheckOnClick = true;
            btSideBySide.Checked = true;
            btSideBySide.CheckedChanged += (s, e) => ShowFile();
            btWholeFile.CheckOnClick = true;
            btWholeFile.CheckedChanged += (s, e) => LoadDiff();
            btWholeFile.ToolTipText = "Show the whole file instead of only the changed sections";
            btPrev.ToolTipText = "Previous change (Shift+F7)";
            btNext.ToolTipText = "Next change (F7)";
            btPrev.Click += (s, e) => GotoChange(false);
            btNext.Click += (s, e) => GotoChange(true);
            btRefresh.ToolTipText = "Refresh (F5)";
            btRefresh.Click += (s, e) => LoadDiff();
            foreach (ToolStripButton bt in new[] { btSideBySide, btWholeFile, btPrev, btNext, btRefresh })
                bt.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStrip.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip.Items.AddRange(new ToolStripItem[] {
                new ToolStripLabel("File:"), comboFiles, new ToolStripSeparator(),
                btSideBySide, btWholeFile, new ToolStripSeparator(),
                btPrev, btNext, new ToolStripSeparator(), btRefresh, labelStats });

            grid.Dock = DockStyle.Fill;
            grid.VirtualMode = true;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.AllowUserToOrderColumns = false;
            grid.RowHeadersVisible = false;
            grid.ColumnHeadersVisible = false;
            grid.MultiSelect = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.None;
            grid.BorderStyle = BorderStyle.None;
            grid.ScrollBars = ScrollBars.Both;
            grid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            grid.DefaultCellStyle.Font = Properties.Settings.Default.commitFont;
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            grid.RowTemplate.Height = grid.DefaultCellStyle.Font.Height + 3;
            grid.CellValueNeeded += GridCellValueNeeded;
            grid.CellFormatting += GridCellFormatting;
            grid.Resize += (s, e) => SizeColumns();

            Controls.Add(grid);
            Controls.Add(toolStrip);

            ClassTheme.Apply(this);
            SetColors();

            Load += (s, e) => LoadDiff();
        }

        /// <summary>
        /// Keyboard shortcuts: F7 / Shift+F7 move between changes, F5 refreshes, Esc closes
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.F7: GotoChange(true); return true;
                case Keys.Shift | Keys.F7: GotoChange(false); return true;
                case Keys.F5: LoadDiff(); return true;
                case Keys.Escape: Close(); return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>
        /// Pick the colors for the current (light or dark) theme
        /// </summary>
        private void SetColors()
        {
            bool dark = ClassTheme.IsDark;
            Color window = ClassTheme.Map(SystemColors.Window);
            Color text = ClassTheme.Map(SystemColors.WindowText);
            colAdded = dark ? Color.FromArgb(35, 70, 40) : Color.FromArgb(221, 255, 221);
            colDeleted = dark ? Color.FromArgb(85, 35, 35) : Color.FromArgb(255, 221, 221);
            colAddedText = text;
            colDeletedText = text;
            colHunk = dark ? Color.FromArgb(40, 50, 80) : Color.FromArgb(225, 235, 255);
            colEmpty = dark ? Color.FromArgb(45, 45, 48) : Color.FromArgb(240, 240, 240);
            colNumbers = dark ? Color.FromArgb(37, 37, 38) : Color.FromArgb(248, 248, 248);
            colNumbersText = Color.Gray;

            grid.BackgroundColor = window;
            grid.GridColor = colEmpty;
            grid.DefaultCellStyle.BackColor = window;
            grid.DefaultCellStyle.ForeColor = text;
            grid.DefaultCellStyle.SelectionBackColor = dark ? Color.FromArgb(38, 79, 120) : Color.FromArgb(173, 214, 255);
            grid.DefaultCellStyle.SelectionForeColor = text;
        }

        /// <summary>
        /// Run git diff and parse its output, keeping the current file selected when possible
        /// </summary>
        private void LoadDiff()
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                string context = btWholeFile.Checked ? "-U1000000" : "-U3";
                string args = "diff --no-color --no-ext-diff --no-prefix " + context + " " + diffArgs;
                ExecResult result = runGit(args);
                if (!result.Success() && result.stdout.Length == 0)
                {
                    files = new List<FileDiff>();
                    FileDiff error = new FileDiff { Name = "(error)" };
                    foreach (string line in result.stderr.Split('\n'))
                        error.SideBySide.Add(new Row { Kind = RowKind.Info, OldText = line.TrimEnd('\r') });
                    error.Unified.AddRange(error.SideBySide);
                    files.Add(error);
                }
                else
                    files = Parse(result.stdout);

                string current = comboFiles.SelectedItem != null ? comboFiles.SelectedItem.ToString() : null;
                comboFiles.Items.Clear();
                foreach (FileDiff f in files)
                    comboFiles.Items.Add(f);
                int index = 0;
                for (int i = 0; i < files.Count; i++)
                    if (files[i].Name == current)
                        index = i;
                if (files.Count > 0)
                    comboFiles.SelectedIndex = index;   // Calls ShowFile()
                else
                    ShowFile();
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// Display the currently selected file in the current mode
        /// </summary>
        private void ShowFile()
        {
            FileDiff f = comboFiles.SelectedItem as FileDiff;
            if (f == null)
            {
                f = new FileDiff();
                f.SideBySide.Add(new Row { Kind = RowKind.Info, OldText = "No differences." });
                f.Unified.AddRange(f.SideBySide);
            }
            rows = btSideBySide.Checked ? f.SideBySide : f.Unified;

            int added = 0, deleted = 0;
            foreach (Row r in f.Unified)
            {
                if (r.Kind == RowKind.Added) added++;
                if (r.Kind == RowKind.Deleted) deleted++;
            }
            labelStats.Text = string.Format("   {0} file(s)   +{1}  -{2}", files.Count, added, deleted);

            grid.SuspendLayout();
            grid.RowCount = 0;
            grid.Columns.Clear();
            if (btSideBySide.Checked)
            {
                AddColumn(true); AddColumn(false); AddColumn(true); AddColumn(false);
            }
            else
            {
                AddColumn(true); AddColumn(true); AddColumn(false);
            }
            grid.RowCount = rows.Count;
            SizeColumns();
            grid.ResumeLayout();

            // Position the view on the first change
            grid.ClearSelection();
            if (rows.Count > 0)
            {
                grid.FirstDisplayedScrollingRowIndex = 0;
                GotoChange(true, -1);
            }
        }

        private void AddColumn(bool lineNumbers)
        {
            DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn
            {
                SortMode = DataGridViewColumnSortMode.NotSortable,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Tag = lineNumbers
            };
            if (lineNumbers)
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.Columns.Add(col);
        }

        /// <summary>
        /// Line number columns are sized to the largest number, text columns share the remaining
        /// width but grow to the longest line so the grid scrolls horizontally instead of clipping
        /// </summary>
        private void SizeColumns()
        {
            if (grid.Columns.Count == 0)
                return;
            Font font = grid.DefaultCellStyle.Font;
            int maxNo = 1, maxOld = 0, maxNew = 0;
            foreach (Row r in rows)
            {
                maxNo = Math.Max(maxNo, Math.Max(r.OldNo, r.NewNo));
                maxOld = Math.Max(maxOld, r.OldText.Length);
                maxNew = Math.Max(maxNew, r.NewText.Length);
            }
            int charWidth = TextRenderer.MeasureText("MMMMMMMMMM", font).Width / 10;
            int noWidth = charWidth * (maxNo.ToString().Length + 1) + 6;
            int textCols = btSideBySide.Checked ? 2 : 1;
            int numberCols = grid.Columns.Count - textCols;
            int available = grid.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - numberCols * noWidth;
            int fill = Math.Max(100, available / textCols);

            foreach (DataGridViewColumn col in grid.Columns)
            {
                if ((bool)col.Tag)
                    col.Width = noWidth;
                else
                {
                    bool isNew = btSideBySide.Checked && col.Index == 3;
                    int longest = (btSideBySide.Checked ? (isNew ? maxNew : maxOld) : Math.Max(maxOld, maxNew)) + 2;
                    col.Width = Math.Min(65000, Math.Max(fill, longest * charWidth + 8));
                }
            }
        }

        /// <summary>
        /// Select the next (or previous) block of changed lines, starting after (before) the current row
        /// </summary>
        private void GotoChange(bool forward, int from = int.MinValue)
        {
            if (rows.Count == 0)
                return;
            if (from == int.MinValue)
                from = grid.CurrentCell != null ? grid.CurrentCell.RowIndex : (forward ? -1 : rows.Count);

            int i = from;
            if (forward)
            {
                // Skip the rest of the current block, then find the start of the next one
                while (i >= 0 && i < rows.Count && IsChange(rows[i])) i++;
                if (i < 0) i = 0;
                while (i < rows.Count && !IsChange(rows[i])) i++;
            }
            else
            {
                // Step back over the current block and the unchanged lines before it,
                // then move to the start of that previous block
                i = Math.Min(i, rows.Count - 1);
                while (i >= 0 && IsChange(rows[i])) i--;
                while (i >= 0 && !IsChange(rows[i])) i--;
                while (i > 0 && IsChange(rows[i - 1])) i--;
            }
            if (i < 0 || i >= rows.Count)
                return;

            grid.ClearSelection();
            grid.CurrentCell = grid.Rows[i].Cells[grid.Columns.Count - 1];
            int end = i;
            while (end < rows.Count && IsChange(rows[end])) grid.Rows[end++].Selected = true;
            grid.FirstDisplayedScrollingRowIndex = Math.Max(0, i - 5);
        }

        private static bool IsChange(Row r)
        {
            return r.Kind == RowKind.Changed || r.Kind == RowKind.Added || r.Kind == RowKind.Deleted;
        }

        private void GridCellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex >= rows.Count)
                return;
            Row r = rows[e.RowIndex];
            if (r.Kind == RowKind.Hunk || r.Kind == RowKind.Info)
            {
                // Header rows show their text in the first text column
                int textColumn = btSideBySide.Checked ? 1 : 2;
                e.Value = e.ColumnIndex == textColumn ? r.OldText : string.Empty;
                return;
            }
            if (btSideBySide.Checked)
            {
                switch (e.ColumnIndex)
                {
                    case 0: e.Value = r.OldNo > 0 ? r.OldNo.ToString() : string.Empty; break;
                    case 1: e.Value = r.OldText; break;
                    case 2: e.Value = r.NewNo > 0 ? r.NewNo.ToString() : string.Empty; break;
                    case 3: e.Value = r.NewText; break;
                }
            }
            else
            {
                switch (e.ColumnIndex)
                {
                    case 0: e.Value = r.OldNo > 0 ? r.OldNo.ToString() : string.Empty; break;
                    case 1: e.Value = r.NewNo > 0 ? r.NewNo.ToString() : string.Empty; break;
                    case 2:
                        string mark = r.Kind == RowKind.Added ? "+ " : r.Kind == RowKind.Deleted ? "- " : "  ";
                        e.Value = mark + (r.Kind == RowKind.Added ? r.NewText : r.OldText);
                        break;
                }
            }
        }

        private void GridCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= rows.Count)
                return;
            Row r = rows[e.RowIndex];
            bool numbers = (bool)grid.Columns[e.ColumnIndex].Tag;

            if (r.Kind == RowKind.Hunk)
            {
                e.CellStyle.BackColor = colHunk;
                e.CellStyle.ForeColor = colNumbersText;
            }
            else if (numbers)
            {
                if (r.Kind != RowKind.Info)
                    e.CellStyle.BackColor = colNumbers;
                e.CellStyle.ForeColor = colNumbersText;
            }
            else
                SetDiffColors(e.ColumnIndex, r, e.CellStyle);

            // Keep the diff colors visible on selected rows by tinting them instead
            e.CellStyle.SelectionBackColor = Blend(e.CellStyle.BackColor, grid.DefaultCellStyle.SelectionBackColor);
            e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
        }

        private static Color Blend(Color a, Color b)
        {
            return Color.FromArgb((a.R + b.R) / 2, (a.G + b.G) / 2, (a.B + b.B) / 2);
        }

        private void SetDiffColors(int column, Row r, DataGridViewCellStyle style)
        {
            bool leftSide = btSideBySide.Checked && column == 1;
            bool rightSide = btSideBySide.Checked && column == 3;
            switch (r.Kind)
            {
                case RowKind.Added:
                    style.BackColor = leftSide ? colEmpty : colAdded;
                    style.ForeColor = colAddedText;
                    break;
                case RowKind.Deleted:
                    style.BackColor = rightSide ? colEmpty : colDeleted;
                    style.ForeColor = colDeletedText;
                    break;
                case RowKind.Changed:
                    style.BackColor = leftSide ? colDeleted : colAdded;
                    break;
            }
        }

        #region Unified diff parser

        private static readonly Regex HunkHeader = new Regex(@"^@@ -(\d+)(?:,\d+)? \+(\d+)(?:,\d+)? @@(.*)$");

        /// <summary>
        /// Parse the output of "git diff" into a list of files with both views prepared
        /// </summary>
        private static List<FileDiff> Parse(string text)
        {
            List<FileDiff> list = new List<FileDiff>();
            FileDiff f = null;
            int oldNo = 0, newNo = 0;
            bool inHunk = false;
            List<Row> deleted = new List<Row>();     // Pending "-" lines waiting to pair with "+" lines
            List<Row> added = new List<Row>();

            Action flush = () =>
            {
                if (f == null) return;
                int n = Math.Max(deleted.Count, added.Count);
                for (int i = 0; i < n; i++)
                {
                    Row d = i < deleted.Count ? deleted[i] : null;
                    Row a = i < added.Count ? added[i] : null;
                    f.SideBySide.Add(new Row
                    {
                        Kind = d != null && a != null ? RowKind.Changed : d != null ? RowKind.Deleted : RowKind.Added,
                        OldNo = d != null ? d.OldNo : 0,
                        OldText = d != null ? d.OldText : string.Empty,
                        NewNo = a != null ? a.NewNo : 0,
                        NewText = a != null ? a.NewText : string.Empty
                    });
                }
                f.Unified.AddRange(deleted);
                f.Unified.AddRange(added);
                deleted.Clear();
                added.Clear();
            };

            foreach (string rawLine in text.Split('\n'))
            {
                string line = rawLine.TrimEnd('\r');

                if (line.StartsWith("diff --git "))
                {
                    flush();
                    f = new FileDiff { Name = FileNameFromHeader(line) };
                    list.Add(f);
                    inHunk = false;
                    continue;
                }
                if (f == null)
                    continue;

                Match m = HunkHeader.Match(line);
                if (m.Success)
                {
                    flush();
                    oldNo = int.Parse(m.Groups[1].Value);
                    newNo = int.Parse(m.Groups[2].Value);
                    inHunk = true;
                    Row hunk = new Row { Kind = RowKind.Hunk, OldText = line };
                    f.SideBySide.Add(hunk);
                    f.Unified.Add(hunk);
                    continue;
                }

                if (!inHunk)
                {
                    // File header: show only the informative lines (new/deleted/renamed/binary)
                    if (line.StartsWith("+++ ") || line.StartsWith("--- ") || line.StartsWith("index ") || line.Length == 0)
                        continue;
                    if (line.StartsWith("rename to "))
                        f.Name = line.Substring(10);
                    Row info = new Row { Kind = RowKind.Info, OldText = line };
                    f.SideBySide.Add(info);
                    f.Unified.Add(info);
                    continue;
                }

                if (line.Length == 0 && rawLine.Length == 0)
                    continue;               // Trailing empty string after the last newline
                char c = line.Length > 0 ? line[0] : ' ';
                string body = line.Length > 0 ? ExpandTabs(line.Substring(1)) : string.Empty;
                switch (c)
                {
                    case '-':
                        deleted.Add(new Row { Kind = RowKind.Deleted, OldNo = oldNo++, OldText = body });
                        break;
                    case '+':
                        added.Add(new Row { Kind = RowKind.Added, NewNo = newNo++, NewText = body });
                        break;
                    case '\\':
                        break;              // "\ No newline at end of file"
                    default:
                        flush();
                        Row ctx = new Row { Kind = RowKind.Context, OldNo = oldNo++, NewNo = newNo++, OldText = body, NewText = body };
                        f.SideBySide.Add(ctx);
                        f.Unified.Add(ctx);
                        break;
                }
            }
            flush();
            return list;
        }

        /// <summary>
        /// Extract the file name from a "diff --git a b" line (with --no-prefix, the two names
        /// are the paths themselves, possibly quoted)
        /// </summary>
        private static string FileNameFromHeader(string line)
        {
            string names = line.Substring(11);
            if (names.EndsWith("\""))
            {
                int start = names.LastIndexOf(" \"", StringComparison.Ordinal);
                if (start >= 0)
                    return names.Substring(start + 2, names.Length - start - 3);
            }
            // Both names are equal unless the file was renamed; take the second half
            int half = (names.Length - 1) / 2;
            if (names.Length % 2 == 1 && names[half] == ' ' && names.Substring(0, half) == names.Substring(half + 1))
                return names.Substring(half + 1);
            int space = names.LastIndexOf(' ');
            return space >= 0 ? names.Substring(space + 1) : names;
        }

        private static string ExpandTabs(string s)
        {
            if (s.IndexOf('\t') < 0)
                return s;
            System.Text.StringBuilder sb = new System.Text.StringBuilder(s.Length + 16);
            foreach (char c in s)
            {
                if (c == '\t')
                    sb.Append(' ', 4 - sb.Length % 4);
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

        #endregion
    }
}
