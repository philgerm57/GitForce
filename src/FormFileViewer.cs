using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace GitForce
{
    /// <summary>
    /// Built-in file viewer: shows text and source files with line numbers, and images
    /// (png, jpg, gif, bmp, ico, tif) with zoom. Other binary files are described and can be
    /// handed to the associated application.
    /// This form is not modal, so user can keep several files open.
    /// </summary>
    public class FormFileViewer : Form
    {
        /// <summary>
        /// Tag of the "Edit Using" menu items that open a file in this viewer
        /// </summary>
        public const string MenuTag = "<GitForce built-in viewer>";

        private static readonly HashSet<string> ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico", ".tif", ".tiff" };

        private const long MaxTextSize = 50 * 1024 * 1024;

        private readonly string file;
        private string[] lines = new string[0];
        private List<ClassSyntax.Token>[] tokens;

        private readonly ToolStrip toolStrip = new ToolStrip();
        private readonly ToolStripLabel labelInfo = new ToolStripLabel();
        private readonly ToolStripLabel labelFind = new ToolStripLabel("Find:");
        private readonly ToolStripTextBox textFind = new ToolStripTextBox();
        private readonly ToolStripSeparator sepFind = new ToolStripSeparator();
        private readonly ToolStripSeparator sepImage = new ToolStripSeparator();
        private readonly ToolStripButton btFind = new ToolStripButton("Find next");
        private readonly ToolStripButton btFit = new ToolStripButton("Fit");
        private readonly ToolStripButton btZoomIn = new ToolStripButton("Zoom +");
        private readonly ToolStripButton btZoomOut = new ToolStripButton("Zoom -");
        private readonly ToolStripButton btActual = new ToolStripButton("100%");
        private readonly ToolStripButton btOpen = new ToolStripButton("Open externally");
        private readonly ToolStripButton btRefresh = new ToolStripButton("Refresh");

        private readonly DataGridView grid = new DataGridView();
        private readonly Panel imagePanel = new Panel();
        private readonly PictureBox picture = new PictureBox();
        private readonly Label labelMessage = new Label();
        private double zoom = 1.0;

        /// <summary>
        /// Open a file in a new viewer window
        /// </summary>
        public static void View(string file)
        {
            new FormFileViewer(file).Show();
        }

        public FormFileViewer(string file)
        {
            this.file = file;

            Text = "View: " + file;
            Icon = Properties.Resources.git;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1000, 750);
            MinimumSize = new Size(400, 250);

            textFind.ToolTipText = "Text to find (Ctrl+F, then Enter or F3)";
            textFind.AutoSize = false;
            textFind.Width = 200;
            textFind.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    FindNext();
                    e.SuppressKeyPress = true;
                }
            };
            btFind.Click += (s, e) => FindNext();
            btFit.CheckOnClick = true;
            btFit.Checked = true;
            btFit.ToolTipText = "Fit the image to the window";
            btFit.CheckedChanged += (s, e) => LayoutImage();
            btZoomIn.Click += (s, e) => SetZoom(zoom * 1.25);
            btZoomOut.Click += (s, e) => SetZoom(zoom / 1.25);
            btActual.Click += (s, e) => SetZoom(1.0);
            btOpen.ToolTipText = "Open the file with the application associated with it";
            btOpen.Click += (s, e) => ClassUtils.ShellOpen(this.file);
            btRefresh.ToolTipText = "Reload the file (F5)";
            btRefresh.Click += (s, e) => LoadFile();
            toolStrip.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip.Items.AddRange(new ToolStripItem[] {
                labelFind, textFind, btFind, sepFind,
                btFit, btZoomIn, btZoomOut, btActual, sepImage,
                btOpen, btRefresh, new ToolStripSeparator(), labelInfo });

            grid.Dock = DockStyle.Fill;
            grid.VirtualMode = true;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.RowHeadersVisible = false;
            grid.ColumnHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.None;
            grid.BorderStyle = BorderStyle.None;
            grid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            grid.DefaultCellStyle.Font = Properties.Settings.Default.commitFont;
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            grid.RowTemplate.Height = grid.DefaultCellStyle.Font.Height + 3;
            grid.Columns.Add(new DataGridViewTextBoxColumn { SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewTextBoxColumn { SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.Columns[0].DefaultCellStyle.ForeColor = Color.Gray;
            grid.CellValueNeeded += (s, e) =>
            {
                if (e.RowIndex < lines.Length)
                    e.Value = e.ColumnIndex == 0 ? (e.RowIndex + 1).ToString() : lines[e.RowIndex];
            };
            grid.CellPainting += (s, e) =>
            {
                // Source code is drawn with its syntax colors
                if (tokens != null && e.ColumnIndex == 1 && e.RowIndex >= 0 && e.RowIndex < lines.Length)
                    ClassSyntax.PaintCell(e, lines[e.RowIndex], tokens[e.RowIndex]);
            };
            grid.Resize += (s, e) => SizeColumns();

            imagePanel.Dock = DockStyle.Fill;
            imagePanel.AutoScroll = true;
            imagePanel.Resize += (s, e) => LayoutImage();
            picture.SizeMode = PictureBoxSizeMode.Zoom;
            imagePanel.Controls.Add(picture);

            labelMessage.Dock = DockStyle.Fill;
            labelMessage.TextAlign = ContentAlignment.MiddleCenter;

            Controls.Add(grid);
            Controls.Add(imagePanel);
            Controls.Add(labelMessage);
            Controls.Add(toolStrip);

            ClassTheme.Apply(this);
            Color window = ClassTheme.Map(SystemColors.Window);
            Color text = ClassTheme.Map(SystemColors.WindowText);
            grid.BackgroundColor = window;
            grid.DefaultCellStyle.BackColor = window;
            grid.DefaultCellStyle.ForeColor = text;
            grid.DefaultCellStyle.SelectionBackColor = ClassTheme.IsDark ? Color.FromArgb(38, 79, 120) : Color.FromArgb(173, 214, 255);
            grid.DefaultCellStyle.SelectionForeColor = text;
            grid.Columns[0].DefaultCellStyle.BackColor = ClassTheme.IsDark ? Color.FromArgb(37, 37, 38) : Color.FromArgb(248, 248, 248);
            imagePanel.BackColor = ClassTheme.IsDark ? Color.FromArgb(30, 30, 30) : Color.FromArgb(230, 230, 230);

            Load += (s, e) => LoadFile();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Control | Keys.F:
                    textFind.Focus();
                    textFind.SelectAll();
                    return true;
                case Keys.F3: FindNext(); return true;
                case Keys.F5: LoadFile(); return true;
                case Keys.Escape: Close(); return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (picture.Image != null)
                picture.Image.Dispose();
            base.OnFormClosed(e);
        }

        /// <summary>
        /// Load the file and pick the view that suits its content
        /// </summary>
        private void LoadFile()
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                FileInfo info = new FileInfo(file);
                if (!info.Exists)
                {
                    ShowMessage("File does not exist:\n" + file);
                    return;
                }
                string size = FormatSize(info.Length);

                if (ImageExtensions.Contains(info.Extension) && LoadImage(size))
                    return;

                if (info.Length > MaxTextSize)
                {
                    ShowMessage("The file is too large to be shown (" + size + ").\nUse \"Open externally\".");
                    return;
                }
                byte[] data = File.ReadAllBytes(file);
                if (IsBinary(data))
                {
                    ShowMessage("Binary file (" + size + ").\nUse \"Open externally\" to open it with its associated application.");
                    return;
                }

                string encoding;
                string content = Decode(data, out encoding);
                lines = content.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                    lines[i] = ExpandTabs(lines[i].TrimEnd('\r'));
                if (lines.Length > 1 && lines[lines.Length - 1].Length == 0)
                    Array.Resize(ref lines, lines.Length - 1);    // Trailing newline

                ClassSyntax syntax = ClassSyntax.ForFile(file);
                tokens = syntax != null ? syntax.TokenizeAll(lines) : null;

                SetMode(grid);
                grid.RowCount = 0;
                grid.RowCount = lines.Length;
                SizeColumns();
                labelInfo.Text = string.Format("{0} lines   {1}   {2}", lines.Length, size, encoding);
            }
            catch (Exception ex)
            {
                ShowMessage("Unable to read the file:\n" + ex.Message);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private bool LoadImage(string size)
        {
            Image image;
            try
            {
                // Load from a memory copy so the file is not kept locked while it is shown
                using (MemoryStream ms = new MemoryStream(File.ReadAllBytes(file)))
                using (Image fromFile = Image.FromStream(ms))
                    image = new Bitmap(fromFile);
            }
            catch (Exception)
            {
                return false;   // Not a readable image: fall back to the text/binary view
            }
            if (picture.Image != null)
                picture.Image.Dispose();
            picture.Image = image;
            SetMode(imagePanel);
            labelInfo.Text = string.Format("{0} x {1} pixels   {2}", image.Width, image.Height, size);
            LayoutImage();
            return true;
        }

        private void ShowMessage(string message)
        {
            labelMessage.Text = message;
            labelInfo.Text = string.Empty;
            SetMode(labelMessage);
        }

        /// <summary>
        /// Show one of the views and the toolbar buttons that apply to it
        /// </summary>
        private void SetMode(Control view)
        {
            grid.Visible = view == grid;
            imagePanel.Visible = view == imagePanel;
            labelMessage.Visible = view == labelMessage;
            labelFind.Visible = textFind.Visible = btFind.Visible = sepFind.Visible = view == grid;
            btFit.Visible = btZoomIn.Visible = btZoomOut.Visible = btActual.Visible = sepImage.Visible = view == imagePanel;
        }

        private void SetZoom(double value)
        {
            zoom = Math.Max(0.05, Math.Min(32.0, value));
            btFit.Checked = false;
            LayoutImage();
        }

        /// <summary>
        /// Size and center the picture: fitted to the panel (never enlarged) or at the zoom factor
        /// </summary>
        private void LayoutImage()
        {
            Image image = picture.Image;
            if (image == null || !imagePanel.Visible)
                return;
            Size area = imagePanel.ClientSize;
            if (btFit.Checked)
                zoom = Math.Min(1.0, Math.Min((double)area.Width / image.Width, (double)area.Height / image.Height));
            Size size = new Size(Math.Max(1, (int)(image.Width * zoom)), Math.Max(1, (int)(image.Height * zoom)));
            picture.Size = size;
            picture.Location = new Point(
                Math.Max(0, (area.Width - size.Width) / 2) + imagePanel.AutoScrollPosition.X,
                Math.Max(0, (area.Height - size.Height) / 2) + imagePanel.AutoScrollPosition.Y);
            btActual.Text = (int)Math.Round(zoom * 100) + "%";
        }

        private void SizeColumns()
        {
            int charWidth = TextRenderer.MeasureText("MMMMMMMMMM", grid.DefaultCellStyle.Font).Width / 10;
            int longest = 0;
            foreach (string line in lines)
                longest = Math.Max(longest, line.Length);
            grid.Columns[0].Width = charWidth * (lines.Length.ToString().Length + 1) + 6;
            int fill = grid.ClientSize.Width - grid.Columns[0].Width - SystemInformation.VerticalScrollBarWidth;
            grid.Columns[1].Width = Math.Min(65000, Math.Max(Math.Max(100, fill), (longest + 2) * charWidth));
        }

        /// <summary>
        /// Select the next line containing the search text, wrapping around at the end
        /// </summary>
        private void FindNext()
        {
            string what = textFind.Text;
            if (what.Length == 0 || lines.Length == 0)
                return;
            int start = grid.CurrentCell != null ? grid.CurrentCell.RowIndex + 1 : 0;
            for (int n = 0; n < lines.Length; n++)
            {
                int i = (start + n) % lines.Length;
                if (lines[i].IndexOf(what, StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    grid.CurrentCell = grid.Rows[i].Cells[1];
                    if (i < grid.FirstDisplayedScrollingRowIndex || i >= grid.FirstDisplayedScrollingRowIndex + grid.DisplayedRowCount(false))
                        grid.FirstDisplayedScrollingRowIndex = Math.Max(0, i - 5);
                    return;
                }
            }
            System.Media.SystemSounds.Beep.Play();
        }

        /// <summary>
        /// A file is considered binary if its beginning contains a NUL character
        /// (UTF-16 text is recognized by its byte order mark first)
        /// </summary>
        public static bool IsBinary(byte[] data)
        {
            if (data.Length >= 2 && ((data[0] == 0xFF && data[1] == 0xFE) || (data[0] == 0xFE && data[1] == 0xFF)))
                return false;
            int n = Math.Min(data.Length, 8000);
            for (int i = 0; i < n; i++)
                if (data[i] == 0)
                    return true;
            return false;
        }

        /// <summary>
        /// Decode text: honor a byte order mark, otherwise use UTF-8 if it is valid and Latin-1 if not
        /// </summary>
        /// <summary>
        /// Decode text the same way as the viewer does
        /// </summary>
        public static string DecodeText(byte[] data)
        {
            string name;
            return Decode(data, out name);
        }

        private static string Decode(byte[] data, out string name)
        {
            using (StreamReader reader = new StreamReader(new MemoryStream(data), new UTF8Encoding(false, true), true))
            {
                try
                {
                    string text = reader.ReadToEnd();
                    Encoding used = reader.CurrentEncoding;
                    name = used is UTF8Encoding ? "UTF-8" : used.WebName.ToUpperInvariant();
                    return text;
                }
                catch (DecoderFallbackException)
                {
                    name = "ISO-8859-1";
                    return Encoding.Latin1.GetString(data);
                }
            }
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

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024) return bytes + " bytes";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("0.0") + " KB";
            return (bytes / (1024.0 * 1024.0)).ToString("0.0") + " MB";
        }
    }
}
