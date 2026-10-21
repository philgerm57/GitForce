using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace GitForce
{
    /// <summary>
    /// Built-in merge tool: resolves the conflicts that "git merge" left in a file.
    /// The top shows the two versions of the current conflict ("ours", the current branch, and
    /// "theirs", the merged branch); the bottom is the whole file, editable, where each conflict
    /// can be replaced by one side, both sides, or edited by hand. Saving writes the file and
    /// marks it resolved (git add).
    /// </summary>
    public class FormMergeResolve : Form
    {
        /// <summary>
        /// A conflict block in the text: "<<<<<<< ours" ... ["||||||| base" ...] "=======" ... ">>>>>>> theirs"
        /// </summary>
        private class Conflict
        {
            public int Start, Length;
            public string Ours = string.Empty, Base, Theirs = string.Empty;
            public string OursLabel = string.Empty, TheirsLabel = string.Empty;
        }

        private readonly ClassRepo repo;
        private readonly string file;           // Relative to the repo root
        private readonly string fullPath;
        private Encoding encoding = new UTF8Encoding(false);
        private bool crlf;

        private readonly ToolStrip toolStrip = new ToolStrip();
        private readonly ToolStripButton btPrev = new ToolStripButton("▲ Previous");
        private readonly ToolStripButton btNext = new ToolStripButton("▼ Next");
        private readonly ToolStripButton btOurs = new ToolStripButton("Use ours");
        private readonly ToolStripButton btTheirs = new ToolStripButton("Use theirs");
        private readonly ToolStripButton btOursTheirs = new ToolStripButton("Ours + theirs");
        private readonly ToolStripButton btTheirsOurs = new ToolStripButton("Theirs + ours");
        private readonly ToolStripButton btAllOurs = new ToolStripButton("All ours");
        private readonly ToolStripButton btAllTheirs = new ToolStripButton("All theirs");
        private readonly ToolStripLabel labelCount = new ToolStripLabel();
        private readonly Label labelOurs = new Label();
        private readonly Label labelTheirs = new Label();
        private readonly RichTextBox textOurs = new RichTextBox();
        private readonly RichTextBox textTheirs = new RichTextBox();
        private readonly RichTextBox textResult = new RichTextBox();
        private readonly Button btSave = new Button();
        private readonly Button btCancel = new Button();
        private readonly Label labelHelp = new Label();

        private Color colOurs, colTheirs, colMarker;

        /// <summary>
        /// Resolve the conflicts of a file. Returns true if the file was saved and marked resolved.
        /// </summary>
        public static bool Resolve(ClassRepo repo, string file)
        {
            using (FormMergeResolve form = new FormMergeResolve(repo, file))
                return form.ShowDialog() == DialogResult.OK;
        }

        public FormMergeResolve(ClassRepo repo, string file)
        {
            this.repo = repo;
            this.file = file;
            fullPath = Path.Combine(repo.Path, file);

            Text = "Resolve conflicts: " + file;
            Icon = Properties.Resources.git;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1200, 820);
            MinimumSize = new Size(700, 450);
            KeyPreview = true;

            bool dark = ClassTheme.IsDark;
            colOurs = dark ? Color.FromArgb(30, 60, 95) : Color.FromArgb(214, 234, 255);
            colTheirs = dark ? Color.FromArgb(90, 60, 25) : Color.FromArgb(255, 232, 204);
            colMarker = dark ? Color.FromArgb(70, 70, 75) : Color.FromArgb(225, 225, 225);

            btPrev.Click += (s, e) => GotoConflict(false);
            btNext.Click += (s, e) => GotoConflict(true);
            btOurs.Click += (s, e) => ResolveCurrent(c => c.Ours);
            btTheirs.Click += (s, e) => ResolveCurrent(c => c.Theirs);
            btOursTheirs.Click += (s, e) => ResolveCurrent(c => c.Ours + c.Theirs);
            btTheirsOurs.Click += (s, e) => ResolveCurrent(c => c.Theirs + c.Ours);
            btAllOurs.Click += (s, e) => ResolveAll(c => c.Ours);
            btAllTheirs.Click += (s, e) => ResolveAll(c => c.Theirs);
            btPrev.ToolTipText = "Previous conflict (Shift+F7)";
            btNext.ToolTipText = "Next conflict (F7)";
            btOurs.ToolTipText = "Replace the current conflict with the version of the current branch (Alt+1)";
            btTheirs.ToolTipText = "Replace the current conflict with the version of the merged branch (Alt+2)";
            btOursTheirs.ToolTipText = "Keep both versions, ours first";
            btTheirsOurs.ToolTipText = "Keep both versions, theirs first";
            toolStrip.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip.Items.AddRange(new ToolStripItem[] {
                btPrev, btNext, new ToolStripSeparator(),
                btOurs, btTheirs, btOursTheirs, btTheirsOurs, new ToolStripSeparator(),
                btAllOurs, btAllTheirs, new ToolStripSeparator(), labelCount });
            foreach (ToolStripItem item in toolStrip.Items)
                if (item is ToolStripButton)
                    item.DisplayStyle = ToolStripItemDisplayStyle.Text;

            Font code = Properties.Settings.Default.commitFont;
            foreach (RichTextBox box in new[] { textOurs, textTheirs, textResult })
            {
                box.Dock = DockStyle.Fill;
                box.Font = code;
                box.WordWrap = false;
                box.DetectUrls = false;
                box.HideSelection = false;
                box.ScrollBars = RichTextBoxScrollBars.Both;
            }
            textOurs.ReadOnly = textTheirs.ReadOnly = true;
            textResult.AcceptsTab = true;
            textResult.SelectionChanged += (s, e) => ShowCurrent();

            labelOurs.Dock = labelTheirs.Dock = DockStyle.Top;
            labelOurs.Height = labelTheirs.Height = 20;
            labelOurs.TextAlign = labelTheirs.TextAlign = ContentAlignment.MiddleLeft;
            labelOurs.Font = labelTheirs.Font = new Font(Font, FontStyle.Bold);

            SplitContainer sides = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical };
            sides.Panel1.Controls.Add(textOurs);
            sides.Panel1.Controls.Add(labelOurs);
            sides.Panel2.Controls.Add(textTheirs);
            sides.Panel2.Controls.Add(labelTheirs);

            Label labelResult = new Label
            {
                Dock = DockStyle.Top, Height = 20, TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(Font, FontStyle.Bold), Text = "Result (editable)"
            };
            SplitContainer main = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
            main.Panel1.Controls.Add(sides);
            main.Panel2.Controls.Add(textResult);
            main.Panel2.Controls.Add(labelResult);

            Panel bottom = new Panel { Dock = DockStyle.Bottom, Height = 42 };
            btSave.Text = "Save and mark resolved";
            btSave.Size = new Size(170, 28);
            btSave.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            btSave.Click += (s, e) => Save();
            btCancel.Text = "Cancel";
            btCancel.Size = new Size(90, 28);
            btCancel.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            btCancel.DialogResult = DialogResult.Cancel;
            labelHelp.AutoSize = false;
            labelHelp.Location = new Point(10, 8);
            labelHelp.Size = new Size(800, 28);
            labelHelp.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            labelHelp.Text = "Choose a version for each conflict, or edit the result by hand. Ours = current branch, theirs = merged branch.";
            bottom.Controls.Add(labelHelp);
            bottom.Controls.Add(btSave);
            bottom.Controls.Add(btCancel);
            bottom.Resize += (s, e) =>
            {
                btCancel.Location = new Point(bottom.ClientSize.Width - btCancel.Width - 10, 7);
                btSave.Location = new Point(btCancel.Left - btSave.Width - 8, 7);
                labelHelp.Width = Math.Max(100, btSave.Left - 20);
            };
            CancelButton = btCancel;

            Controls.Add(main);
            Controls.Add(toolStrip);
            Controls.Add(bottom);

            ClassTheme.Apply(this);
            Load += (s, e) =>
            {
                main.SplitterDistance = main.Height * 2 / 5;
                sides.SplitterDistance = sides.Width / 2;
                LoadFile();
            };
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.F7: GotoConflict(true); return true;
                case Keys.Shift | Keys.F7: GotoConflict(false); return true;
                case Keys.Alt | Keys.D1: ResolveCurrent(c => c.Ours); return true;
                case Keys.Alt | Keys.D2: ResolveCurrent(c => c.Theirs); return true;
                case Keys.Control | Keys.S: Save(); return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void LoadFile()
        {
            byte[] data;
            try
            {
                data = File.ReadAllBytes(fullPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to read " + file + ":\n" + ex.Message, "Resolve conflicts", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.Cancel;
                return;
            }
            if (FormFileViewer.IsBinary(data))
            {
                MessageBox.Show(file + " is a binary file. Use Resolve > Accept ours or Accept theirs.", "Resolve conflicts",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.Cancel;
                return;
            }

            // Keep the encoding and the line endings of the file when it is saved
            if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
                encoding = new UTF8Encoding(true);
            string text;
            try
            {
                text = new UTF8Encoding(false, true).GetString(data);
                if (text.Length > 0 && text[0] == '﻿')
                    text = text.Substring(1);
            }
            catch (DecoderFallbackException)
            {
                encoding = Encoding.Latin1;
                text = Encoding.Latin1.GetString(data);
            }
            crlf = text.Contains("\r\n");
            textResult.Text = text.Replace("\r\n", "\n");
            Highlight();
            if (FindConflicts(textResult.Text).Count == 0)
                labelHelp.Text = "This file has no conflict markers. Save it to mark it resolved.";
            GotoConflict(true, true);
        }

        /// <summary>
        /// Find the conflict blocks in a text with "\n" line ends
        /// </summary>
        private static List<Conflict> FindConflicts(string text)
        {
            List<Conflict> list = new List<Conflict>();
            int pos = 0;
            while (pos < text.Length)
            {
                int start = LineStartingWith(text, "<<<<<<<", pos);
                if (start < 0)
                    break;
                int sep = LineStartingWith(text, "=======", start);
                int end = sep < 0 ? -1 : LineStartingWith(text, ">>>>>>>", sep);
                if (end < 0)
                    break;
                int baseStart = LineStartingWith(text, "|||||||", start);
                if (baseStart > sep)
                    baseStart = -1;

                int endOfEnd = text.IndexOf('\n', end);
                endOfEnd = endOfEnd < 0 ? text.Length : endOfEnd + 1;
                int afterStart = NextLine(text, start);
                int oursEnd = baseStart >= 0 ? baseStart : sep;
                Conflict c = new Conflict
                {
                    Start = start,
                    Length = endOfEnd - start,
                    OursLabel = text.Substring(start + 7, afterStart - start - 7).Trim(),
                    TheirsLabel = text.Substring(end + 7, endOfEnd - end - 7).Trim(),
                    Ours = text.Substring(afterStart, oursEnd - afterStart),
                    Theirs = text.Substring(NextLine(text, sep), end - NextLine(text, sep))
                };
                if (baseStart >= 0)
                    c.Base = text.Substring(NextLine(text, baseStart), sep - NextLine(text, baseStart));
                list.Add(c);
                pos = endOfEnd;
            }
            return list;
        }

        /// <summary>
        /// Position of the first line at or after pos that starts with the given marker, or -1
        /// </summary>
        private static int LineStartingWith(string text, string marker, int pos)
        {
            int i = pos;
            while (i < text.Length)
            {
                if (string.CompareOrdinal(text, i, marker, 0, marker.Length) == 0)
                    return i;
                int nl = text.IndexOf('\n', i);
                if (nl < 0)
                    return -1;
                i = nl + 1;
            }
            return -1;
        }

        private static int NextLine(string text, int pos)
        {
            int nl = text.IndexOf('\n', pos);
            return nl < 0 ? text.Length : nl + 1;
        }

        /// <summary>
        /// The conflict at the caret, or null
        /// </summary>
        private Conflict CurrentConflict()
        {
            int caret = textResult.SelectionStart;
            foreach (Conflict c in FindConflicts(textResult.Text))
                if (caret >= c.Start && caret <= c.Start + c.Length)
                    return c;
            return null;
        }

        /// <summary>
        /// Show the two versions of the current conflict at the top
        /// </summary>
        private void ShowCurrent()
        {
            List<Conflict> all = FindConflicts(textResult.Text);
            labelCount.Text = all.Count == 0 ? "   No conflict left" : "   " + all.Count + " conflict(s) left";
            Conflict c = CurrentConflict();
            bool has = c != null;
            btOurs.Enabled = btTheirs.Enabled = btOursTheirs.Enabled = btTheirsOurs.Enabled = has;
            btAllOurs.Enabled = btAllTheirs.Enabled = btPrev.Enabled = btNext.Enabled = all.Count > 0;
            labelOurs.Text = "Ours" + (has && c.OursLabel.Length > 0 ? ": " + c.OursLabel : " (current branch)");
            labelTheirs.Text = "Theirs" + (has && c.TheirsLabel.Length > 0 ? ": " + c.TheirsLabel : " (merged branch)");
            string ours = has ? c.Ours : string.Empty, theirs = has ? c.Theirs : string.Empty;
            if (textOurs.Text != ours)
                textOurs.Text = ours;
            if (textTheirs.Text != theirs)
                textTheirs.Text = theirs;
            textOurs.BackColor = has ? colOurs : ClassTheme.Map(SystemColors.Window);
            textTheirs.BackColor = has ? colTheirs : ClassTheme.Map(SystemColors.Window);
        }

        /// <summary>
        /// Move the caret to the next (or previous) conflict and select it
        /// </summary>
        private void GotoConflict(bool forward, bool fromStart = false)
        {
            List<Conflict> all = FindConflicts(textResult.Text);
            if (all.Count == 0)
            {
                ShowCurrent();
                return;
            }
            int caret = fromStart ? -1 : textResult.SelectionStart;
            Conflict target = null;
            if (forward)
            {
                foreach (Conflict c in all)
                    if (c.Start > caret) { target = c; break; }
                if (target == null) target = all[0];
            }
            else
            {
                // The last conflict that starts before the current one (or before the caret)
                Conflict current = CurrentConflict();
                int pos = current != null ? current.Start : caret;
                for (int i = all.Count - 1; i >= 0; i--)
                    if (all[i].Start < pos) { target = all[i]; break; }
                if (target == null)
                    target = all[all.Count - 1];
            }
            textResult.Select(target.Start, 0);
            textResult.ScrollToCaret();
            textResult.Select(target.Start, target.Length);
            textResult.Focus();
            ShowCurrent();
        }

        private void ResolveCurrent(Func<Conflict, string> choose)
        {
            Conflict c = CurrentConflict();
            if (c == null)
                return;
            Replace(c, choose(c));
            Highlight();
            textResult.Select(c.Start, 0);
            GotoConflict(true);
        }

        private void ResolveAll(Func<Conflict, string> choose)
        {
            List<Conflict> all = FindConflicts(textResult.Text);
            // From the end, so that the positions of the conflicts before stay valid
            for (int i = all.Count - 1; i >= 0; i--)
                Replace(all[i], choose(all[i]));
            Highlight();
            ShowCurrent();
        }

        private void Replace(Conflict c, string with)
        {
            textResult.Select(c.Start, c.Length);
            textResult.SelectionBackColor = textResult.BackColor;
            textResult.SelectedText = with;
        }

        /// <summary>
        /// Color the conflicts in the result: markers in gray, ours in blue, theirs in orange
        /// </summary>
        private void Highlight()
        {
            int selStart = textResult.SelectionStart, selLength = textResult.SelectionLength;
            textResult.SuspendLayout();
            textResult.SelectAll();
            textResult.SelectionBackColor = textResult.BackColor;
            string text = textResult.Text;
            foreach (Conflict c in FindConflicts(text))
            {
                int afterStart = NextLine(text, c.Start);
                PaintRange(c.Start, afterStart - c.Start, colMarker);
                PaintRange(afterStart, c.Ours.Length, colOurs);
                int sep = LineStartingWith(text, "=======", c.Start);
                int baseStart = LineStartingWith(text, "|||||||", c.Start);
                if (baseStart >= 0 && baseStart < sep)
                    PaintRange(baseStart, sep - baseStart, colMarker);
                int theirsStart = NextLine(text, sep);
                PaintRange(sep, theirsStart - sep, colMarker);
                PaintRange(theirsStart, c.Theirs.Length, colTheirs);
                int end = theirsStart + c.Theirs.Length;
                PaintRange(end, c.Start + c.Length - end, colMarker);
            }
            textResult.Select(selStart, selLength);
            textResult.ResumeLayout();
        }

        private void PaintRange(int start, int length, Color color)
        {
            if (length <= 0)
                return;
            textResult.Select(start, length);
            textResult.SelectionBackColor = color;
        }

        /// <summary>
        /// Write the result and mark the file resolved
        /// </summary>
        private void Save()
        {
            string text = textResult.Text;
            int left = FindConflicts(text).Count;
            if (left > 0 && MessageBox.Show(left + " conflict(s) are not resolved: the file will keep their markers.\n\nSave and mark the file resolved anyway?",
                    "Resolve conflicts", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;
            try
            {
                if (crlf)
                    text = text.Replace("\n", "\r\n");
                File.WriteAllText(fullPath, text, encoding);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to write " + file + ":\n" + ex.Message, "Resolve conflicts", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            repo.RunCmd("add -- \"" + file.Replace('\\', '/') + "\"");
            DialogResult = DialogResult.OK;
        }
    }
}
