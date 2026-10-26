using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace GitForce
{
    /// <summary>
    /// Diff viewer control, shown side by side or as a unified diff, with syntax highlighting.
    /// It works in two modes:
    ///  - git diff: runs "git diff" with the same arguments that would have been passed to
    ///    "git difftool" (used by FormDiffViewer)
    ///  - compare: shows the whole file, its last committed version on the left and the current
    ///    one on the right (used in the main window when a file is selected)
    /// </summary>
    public class DiffViewControl : UserControl
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
            public List<ClassSyntax.Token> OldTokens, NewTokens;
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
        /// Runs a git command in the proper directory and returns its result (git diff mode)
        /// </summary>
        private Func<string, ExecResult> runGit;

        /// <summary>
        /// Arguments to "git diff": revisions/options followed by " -- " and the file list.
        /// Null in the compare mode.
        /// </summary>
        private string diffArgs;

        private List<FileDiff> files = new List<FileDiff>();
        private List<Row> rows = new List<Row>();
        private ClassSyntax syntax;

        private readonly ToolStrip toolStrip = new ToolStrip();
        private readonly ToolStripLabel labelTitle = new ToolStripLabel();
        private readonly ToolStripLabel labelFile = new ToolStripLabel("File:");
        private readonly ToolStripComboBox comboFiles = new ToolStripComboBox();
        private readonly ToolStripSeparator sepFiles = new ToolStripSeparator();
        private readonly ToolStripButton btSideBySide = new ToolStripButton("Side by side");
        private readonly ToolStripButton btWholeFile = new ToolStripButton("Whole file");
        private readonly ToolStripButton btPrev = new ToolStripButton("Previous change");
        private readonly ToolStripButton btNext = new ToolStripButton("Next change");
        private readonly ToolStripButton btRefresh = new ToolStripButton("Refresh");
        private readonly ToolStripLabel labelStats = new ToolStripLabel();
        private readonly DataGridView grid = new DataGridView();

        /// <summary>
        /// Reloads the content in the compare mode (set by the owner of the control)
        /// </summary>
        public Action RefreshCompare;

        // Colors: the light palette and its dark counterpart
        private Color colAdded, colDeleted, colHunk, colEmpty, colNumbers, colNumbersText;

        public DiffViewControl()
        {
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
            btRefresh.Click += (s, e) => Reload();
            foreach (ToolStripButton bt in new[] { btSideBySide, btWholeFile, btPrev, btNext, btRefresh })
                bt.DisplayStyle = ToolStripItemDisplayStyle.Text;
            labelTitle.Font = new Font(labelTitle.Font, FontStyle.Bold);
            toolStrip.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip.Items.AddRange(new ToolStripItem[] {
                labelTitle, labelFile, comboFiles, sepFiles,
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
            grid.CellPainting += GridCellPainting;
            grid.Resize += (s, e) => SizeColumns();

            Controls.Add(grid);
            Controls.Add(toolStrip);
            SetColors();
            Clear();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.F7: GotoChange(true); return true;
                case Keys.Shift | Keys.F7: GotoChange(false); return true;
                case Keys.F5: Reload(); return true;
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

        #region Public interface

        /// <summary>
        /// Show the result of "git diff" with the given arguments
        /// </summary>
        public void ShowGitDiff(string diffArgs, Func<string, ExecResult> runGit)
        {
            this.diffArgs = diffArgs;
            this.runGit = runGit;
            labelTitle.Visible = false;
            labelFile.Visible = comboFiles.Visible = sepFiles.Visible = btWholeFile.Visible = true;
            LoadDiff();
        }

        /// <summary>
        /// Empty the view, optionally showing a message
        /// </summary>
        public void Clear(string message = "")
        {
            diffArgs = null;
            labelTitle.Text = string.Empty;
            labelTitle.Visible = true;
            labelFile.Visible = comboFiles.Visible = sepFiles.Visible = btWholeFile.Visible = false;
            FileDiff f = new FileDiff();
            if (message.Length > 0)
                f.SideBySide.Add(new Row { Kind = RowKind.Info, OldText = message });
            f.Unified.AddRange(f.SideBySide);
            SetFiles(new List<FileDiff> { f }, null);
        }

        /// <summary>
        /// Show a whole file: the previous version (left) against the current one (right).
        /// oldBytes is null if there is no previous version (a new file), newBytes is null if the
        /// file does not exist any more.
        /// </summary>
        public void ShowCompare(string name, string description, byte[] oldBytes, byte[] newBytes)
        {
            diffArgs = null;
            labelTitle.Text = name + "   " + description;
            labelTitle.Visible = true;
            labelFile.Visible = comboFiles.Visible = sepFiles.Visible = btWholeFile.Visible = false;

            FileDiff f = new FileDiff { Name = name };
            const int maxSize = 8 * 1024 * 1024;
            if ((oldBytes != null && oldBytes.Length > maxSize) || (newBytes != null && newBytes.Length > maxSize))
                f.SideBySide.Add(new Row { Kind = RowKind.Info, OldText = "The file is too large to be compared here." });
            else if ((oldBytes != null && FormFileViewer.IsBinary(oldBytes)) || (newBytes != null && FormFileViewer.IsBinary(newBytes)))
                f.SideBySide.Add(new Row { Kind = RowKind.Info, OldText = "Binary file." });
            else
            {
                string oldText = oldBytes != null ? Normalize(FormFileViewer.DecodeText(oldBytes)) : null;
                string newText = newBytes != null ? Normalize(FormFileViewer.DecodeText(newBytes)) : null;
                if (oldText == null || newText == null || oldText == newText)
                {
                    // One side only, or both the same: no need to compute differences
                    string[] oldLines = SplitLines(oldText);
                    string[] newLines = SplitLines(newText);
                    bool same = oldText != null && oldText == newText;
                    int n = Math.Max(oldLines.Length, newLines.Length);
                    for (int i = 0; i < n; i++)
                    {
                        Row r = new Row { Kind = RowKind.Context };
                        if (i < oldLines.Length) { r.OldNo = i + 1; r.OldText = oldLines[i]; }
                        if (i < newLines.Length) { r.NewNo = i + 1; r.NewText = newLines[i]; }
                        if (!same)
                            r.Kind = oldText == null ? RowKind.Context : RowKind.Deleted;
                        f.SideBySide.Add(r);
                    }
                }
                else
                {
                    FileDiff d = DiffTexts(oldText, newText);
                    if (d != null)
                        f = d;
                    f.Name = name;
                }
            }
            if (f.Unified.Count == 0)
                f.Unified.AddRange(f.SideBySide);
            SetFiles(new List<FileDiff> { f }, name);
        }

        #endregion

        private void Reload()
        {
            if (diffArgs != null)
                LoadDiff();
            else if (RefreshCompare != null)
                RefreshCompare();
        }

        private static string Normalize(string text)
        {
            return text.Replace("\r\n", "\n");
        }

        private static string[] SplitLines(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new string[0];
            if (text.EndsWith("\n"))
                text = text.Substring(0, text.Length - 1);
            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
                lines[i] = ExpandTabs(lines[i]);
            return lines;
        }

        /// <summary>
        /// Compute the differences of two texts with "git diff --no-index" on temporary files
        /// </summary>
        private static FileDiff DiffTexts(string oldText, string newText)
        {
            string dir = Path.Combine(Path.GetTempPath(), "GitForce-diff-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(dir);
                string a = Path.Combine(dir, "a"), b = Path.Combine(dir, "b");
                File.WriteAllText(a, oldText, new UTF8Encoding(false));
                File.WriteAllText(b, newText, new UTF8Encoding(false));
                string output = RunGitRaw(dir, "-c core.autocrlf=false diff --no-index --no-color --no-ext-diff --no-prefix -U1000000 -- a b");
                List<FileDiff> parsed = Parse(output);
                return parsed.Count > 0 ? parsed[0] : null;
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch (Exception) { }
            }
        }

        /// <summary>
        /// Run git and return its standard output, without the side effects of the shared git runner
        /// (status messages, busy indicator and message pumping)
        /// </summary>
        public static string RunGitRaw(string workDir, string args)
        {
            return Encoding.UTF8.GetString(RunGitBytes(workDir, args, out _));
        }

        /// <summary>
        /// Run git and return its raw standard output and its exit code
        /// </summary>
        public static byte[] RunGitBytes(string workDir, string args, out int exitCode)
        {
            ProcessStartInfo psi = new ProcessStartInfo(Properties.Settings.Default.GitPath, args)
            {
                WorkingDirectory = workDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (Process p = Process.Start(psi))
            using (MemoryStream ms = new MemoryStream())
            {
                var err = p.StandardError.ReadToEndAsync();
                p.StandardOutput.BaseStream.CopyTo(ms);
                p.WaitForExit();
                err.Wait();
                exitCode = p.ExitCode;
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Run git diff and parse its output, keeping the current file selected when possible
        /// </summary>
        private void LoadDiff()
        {
            if (diffArgs == null)
                return;
            Cursor = Cursors.WaitCursor;
            try
            {
                string context = btWholeFile.Checked ? "-U1000000" : "-U3";
                string args = "diff --no-color --no-ext-diff --no-prefix " + context + " " + diffArgs;
                ExecResult result = runGit(args);
                List<FileDiff> list;
                if (!result.Success() && result.stdout.Length == 0)
                {
                    FileDiff error = new FileDiff { Name = "(error)" };
                    foreach (string line in result.stderr.Split('\n'))
                        error.SideBySide.Add(new Row { Kind = RowKind.Info, OldText = line.TrimEnd('\r') });
                    error.Unified.AddRange(error.SideBySide);
                    list = new List<FileDiff> { error };
                }
                else
                    list = Parse(result.stdout);
                SetFiles(list, comboFiles.SelectedItem != null ? comboFiles.SelectedItem.ToString() : null);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// Show a new set of files, selecting the named one if present
        /// </summary>
        private void SetFiles(List<FileDiff> list, string select)
        {
            files = list;
            comboFiles.Items.Clear();
            foreach (FileDiff f in files)
                comboFiles.Items.Add(f);
            int index = 0;
            for (int i = 0; i < files.Count; i++)
                if (files[i].Name == select)
                    index = i;
            if (files.Count > 0 && comboFiles.SelectedIndex != index)
                comboFiles.SelectedIndex = index;   // Calls ShowFile()
            else
                ShowFile();
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
                if (diffArgs != null)
                    f.SideBySide.Add(new Row { Kind = RowKind.Info, OldText = "No differences." });
                f.Unified.AddRange(f.SideBySide);
            }
            rows = btSideBySide.Checked ? f.SideBySide : f.Unified;
            syntax = ClassSyntax.ForFile(f.Name);
            if (syntax != null)
                Highlight(rows);

            int added = 0, deleted = 0;
            foreach (Row r in f.SideBySide)
            {
                if (r.Kind == RowKind.Added || r.Kind == RowKind.Changed) added++;
                if (r.Kind == RowKind.Deleted || r.Kind == RowKind.Changed) deleted++;
            }
            labelStats.Text = diffArgs != null
                ? string.Format("   {0} file(s)   +{1}  -{2}", files.Count, added, deleted)
                : (added + deleted > 0 ? string.Format("   +{0}  -{1}", added, deleted) : string.Empty);

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

        /// <summary>
        /// Prepare the syntax tokens of both sides, following each side as its own sequence of
        /// lines. Both views list the lines of each side in the same order, so they get the same tokens.
        /// </summary>
        private void Highlight(List<Row> list)
        {
            ClassSyntax.State oldState = ClassSyntax.State.Normal, newState = ClassSyntax.State.Normal;
            foreach (Row r in list)
            {
                if (r.Kind == RowKind.Hunk)
                {
                    // Lines between hunks are not known: start again from a clean state
                    oldState = newState = ClassSyntax.State.Normal;
                    continue;
                }
                if (r.Kind == RowKind.Info)
                    continue;
                if (r.OldNo > 0)
                    r.OldTokens = syntax.Tokenize(r.OldText, ref oldState);
                if (r.NewNo > 0)
                    r.NewTokens = syntax.Tokenize(r.NewText, ref newState);
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
                    // Side by side, both versions share the visible width (the end of long lines is cut)
                    col.Width = btSideBySide.Checked ? fill : Math.Min(65000, Math.Max(fill, longest * charWidth + 8));
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
            // The first column, so that the grid does not scroll to the right (whole rows are selected)
            grid.CurrentCell = grid.Rows[i].Cells[0];
            int end = i;
            while (end < rows.Count && IsChange(rows[end])) grid.Rows[end++].Selected = true;
            try
            {
                grid.FirstDisplayedScrollingRowIndex = Math.Max(0, i - 5);
            }
            catch (InvalidOperationException)
            {
                // The grid has no room to show rows yet (collapsed panel)
            }
        }

        private static bool IsChange(Row r)
        {
            return r.Kind == RowKind.Changed || r.Kind == RowKind.Added || r.Kind == RowKind.Deleted;
        }

        /// <summary>
        /// Text and tokens shown in a text column of a row
        /// </summary>
        private string CellText(Row r, int column, out List<ClassSyntax.Token> tokens, out string prefix)
        {
            tokens = null;
            prefix = string.Empty;
            if (btSideBySide.Checked)
            {
                if (column == 1) { tokens = r.OldTokens; return r.OldText; }
                tokens = r.NewTokens;
                return r.NewText;
            }
            // Unified: a line exists on the new side when it was added or has no old version
            bool newSide = r.Kind == RowKind.Added || (r.OldNo == 0 && r.NewNo > 0);
            prefix = r.Kind == RowKind.Added ? "+ " : r.Kind == RowKind.Deleted ? "- " : "  ";
            tokens = newSide ? r.NewTokens : r.OldTokens;
            return newSide ? r.NewText : r.OldText;
        }

        private void GridCellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex >= rows.Count)
                return;
            Row r = rows[e.RowIndex];
            bool numbers = (bool)grid.Columns[e.ColumnIndex].Tag;
            if (r.Kind == RowKind.Hunk || r.Kind == RowKind.Info)
            {
                // Header rows show their text in the first text column
                int textColumn = btSideBySide.Checked ? 1 : 2;
                e.Value = e.ColumnIndex == textColumn ? r.OldText : string.Empty;
                return;
            }
            if (numbers)
            {
                bool old = e.ColumnIndex == 0;
                int no = old ? r.OldNo : r.NewNo;
                e.Value = no > 0 ? no.ToString() : string.Empty;
                return;
            }
            List<ClassSyntax.Token> tokens;
            string prefix;
            string text = CellText(r, e.ColumnIndex, out tokens, out prefix);
            e.Value = prefix + text;
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

        /// <summary>
        /// Draw code lines with their syntax colors
        /// </summary>
        private void GridCellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (syntax == null || e.RowIndex < 0 || e.RowIndex >= rows.Count || e.ColumnIndex < 0)
                return;
            if ((bool)grid.Columns[e.ColumnIndex].Tag)
                return;
            Row r = rows[e.RowIndex];
            if (r.Kind == RowKind.Hunk || r.Kind == RowKind.Info)
                return;
            List<ClassSyntax.Token> tokens;
            string prefix;
            string text = CellText(r, e.ColumnIndex, out tokens, out prefix);
            ClassSyntax.PaintCell(e, text, tokens, prefix);
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
                    break;
                case RowKind.Deleted:
                    style.BackColor = rightSide ? colEmpty : colDeleted;
                    break;
                case RowKind.Changed:
                    style.BackColor = leftSide ? colDeleted : colAdded;
                    break;
                case RowKind.Context:
                    // A side without that line (a file with no previous version)
                    if ((leftSide && r.OldNo == 0) || (rightSide && r.NewNo == 0))
                        style.BackColor = colEmpty;
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
            StringBuilder sb = new StringBuilder(s.Length + 16);
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
