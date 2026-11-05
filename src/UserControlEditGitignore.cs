using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace GitForce
{
    public partial class UserControlEditGitignore : UserControl
    {
        /// <summary>
        /// Current file name, displayed on the control
        /// </summary>
        private string FileName
        {
            set
            {
                labelFileName.Text = value;
            }
        }

        /// <summary>
        /// The text was changed since it was loaded (setting the Lines in code resets TextBox.Modified)
        /// </summary>
        public bool Modified { get; private set; }

        /// <summary>
        /// The loaded file had Windows line endings
        /// </summary>
        private bool crlf = Environment.NewLine == "\r\n";
        private bool loading;

        public UserControlEditGitignore()
        {
            InitializeComponent();
            textBox.Font = new System.Drawing.Font("Consolas", 10f);
            // The file name above, the editor in all the rest, whatever the scale of the page
            labelFileName.AutoSize = false;
            labelFileName.Dock = DockStyle.Top;
            labelFileName.Height = labelFileName.Font.Height + 6;
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.BringToFront();
            textBox.TextChanged += (s, e) => { if (!loading) Modified = true; };
        }

        /// <summary>
        /// Show an explanation above an editor that fills its page, with the height its text needs
        /// </summary>
        public static void AddHelp(Control page, Control editor, string text)
        {
            Label help = new Label
            {
                Dock = DockStyle.Top,
                BackColor = System.Drawing.SystemColors.Info,
                Padding = new Padding(3),
                Text = text
            };
            page.Controls.Add(help);
            editor.BringToFront();
            page.Resize += (s, e) => help.Height = help.GetPreferredSize(new System.Drawing.Size(Math.Max(100, page.ClientSize.Width), 0)).Height + 4;
        }

        /// <summary>
        /// Nothing to edit: show why
        /// </summary>
        public void ShowMessage(string message)
        {
            loading = true;
            textBox.Text = message;
            textBox.ReadOnly = true;
            btAdd.Enabled = false;
            FileName = string.Empty;
            loading = false;
        }

        /// <summary>
        /// A file that does not exist yet: it is created when something is written in it
        /// </summary>
        public void ShowNewFile(string file)
        {
            loading = true;
            textBox.Clear();
            FileName = file + "  (new file)";
            loading = false;
            Modified = false;
        }

        /// <summary>
        /// Loads specified text file as a gitignore file
        /// </summary>
        public bool LoadGitIgnore(string file)
        {
            bool result = true;
            try
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    // Read a configuration file which may use Windows or Unix specific newline code:
                    // CR = 0D  \r
                    // LF = 0A  \n
                    // Windows OS uses CR LF, Unix OS uses LF only
                    string content = sr.ReadToEnd();
                    crlf = content.Contains("\r\n") || (content.IndexOf('\n') < 0 && Environment.NewLine == "\r\n");
                    content = content.Replace("\r", String.Empty);        // Remove 0D (\r) character
                    content = content.Replace("\n", Environment.NewLine); // Make sure we use OS-specific newline
                    loading = true;
                    textBox.Text = content;
                    loading = false;
                }
                FileName = file;
                Modified = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Edit gitignore", MessageBoxButtons.OK, MessageBoxIcon.Error);
                result = false;
            }
            return result;
        }

        /// <summary>
        /// Saves the edited content of a gitignore listbox into a file
        /// </summary>
        public bool SaveGitIgnore(string file)
        {
            return SaveGitIgnore(file, false);
        }

        /// <summary>
        /// Saves the edited content into a file. With keepLayout, the blank lines are kept (they
        /// group the patterns of a .gitignore) and the file keeps its line endings.
        /// </summary>
        public bool SaveGitIgnore(string file, bool keepLayout)
        {
            bool result = true;
            if (keepLayout)
            {
                try
                {
                    string text = string.Join(crlf ? "\r\n" : "\n", textBox.Lines.Select(l => l.TrimEnd())).TrimEnd('\r', '\n');
                    File.WriteAllText(file, text.Length > 0 ? text + (crlf ? "\r\n" : "\n") : string.Empty);
                    FileName = file;
                    Modified = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Edit gitignore", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    result = false;
                }
                return result;
            }
            try
            {
                using (StreamWriter sw = new StreamWriter(file))
                {
                    // Write the content of the text box while trimming each line and ignoring empty ones
                    for (int i = 0; i < textBox.Lines.Length; i++)
                    {
                        if (textBox.Lines[i].Trim().Length > 0)
                            sw.WriteLine(textBox.Lines[i].Trim());
                    }
                }
                FileName = file;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Edit gitignore", MessageBoxButtons.OK, MessageBoxIcon.Error);
                result = false;
            }
            return result;
        }

        /// <summary>
        /// Add all selected items to the gitignore text box
        /// Dont add duplicates.
        /// </summary>
        private void BtAddClick(object sender, EventArgs e)
        {
            List<string> items = (from string s in listFilters.SelectedItems select s.Split(' ').First()).ToList();
            items.InsertRange(0, textBox.Lines);
            textBox.Lines = items.Distinct().ToArray();
        }
    }
}
