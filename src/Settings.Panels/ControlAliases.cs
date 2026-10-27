using System;
using System.Drawing;
using System.Windows.Forms;

namespace GitForce.Settings.Panels
{
    public partial class ControlAliases : UserControl, IUserSettings
    {
        public ControlAliases()
        {
            InitializeComponent();

            // What an alias is and how to write one
            Label help = new Label
            {
                Dock = DockStyle.Top,
                Height = 150,
                BackColor = SystemColors.Info,
                Padding = new Padding(3),
                Text = "Git aliases are short names for git commands, saved in your global git settings (~/.gitconfig). " +
                       "Write one alias per line as  name=command  (without \"git\"):" + Environment.NewLine +
                       "    st=status -s -b" + Environment.NewLine +
                       "    co=checkout" + Environment.NewLine +
                       "    lg=log --oneline --graph --decorate --all" + Environment.NewLine +
                       "Then \"git lg\" runs \"git log --oneline --graph --decorate --all\", in the command line (F3), " +
                       "in Tools > Git Commands and in any terminal. A command starting with ! runs in the shell. " +
                       "Lines starting with # are ignored. Apply or OK saves the list."
            };
            // The help keeps the height its text needs at the width of the page
            Resize += (s, e) => help.Height = TextRenderer.MeasureText(help.Text, help.Font,
                new Size(Math.Max(100, ClientSize.Width - help.Padding.Horizontal), int.MaxValue),
                TextFormatFlags.WordBreak).Height + help.Padding.Vertical + 6;
            textBoxAliases.Font = new Font("Consolas", 10f);
            textBoxAliases.ScrollBars = ScrollBars.Both;
            textBoxAliases.WordWrap = false;
            textBoxAliases.AcceptsReturn = true;
            Controls.Add(help);
            textBoxAliases.BringToFront();
        }

        /// <summary>
        /// Initialize pertinent settings
        /// </summary>
        /// <param name="options">All git global settings</param>
        public void Init(string[] options)
        {
            foreach (string s in options)
            {
                // Each setting is "section.name" and its value on the next line
                int newline = s.IndexOf('\n');
                if (newline > 0 && s.StartsWith("alias.", StringComparison.OrdinalIgnoreCase))
                    textBoxAliases.AppendText(s.Substring(6, newline - 6) + "=" + s.Substring(newline + 1) + Environment.NewLine);
            }

            // Add the dirty (modified) value changed helper
            textBoxAliases.TextChanged += ControlDirtyHelper.ControlDirty;
        }

        /// <summary>
        /// Control received a focus (true) or lost a focus (false)
        /// </summary>
        public void Focus(bool focused)
        {
        }

        /// <summary>
        /// Apply changed settings
        /// </summary>
        public void ApplyChanges()
        {
            if (textBoxAliases.Tag != null)
            {
                // Remove all global aliases and then rebuild them (there may be no alias section yet)
                ClassGit.Run("config --global --remove-section alias");

                foreach (string line in textBoxAliases.Lines)
                {
                    // name=command: the command may itself contain '=' (log --format=%h)
                    string text = line.Trim();
                    int equal = text.IndexOf('=');
                    if (text.Length == 0 || text.StartsWith("#") || equal <= 0)
                        continue;
                    string name = text.Substring(0, equal).Trim();
                    string command = text.Substring(equal + 1).Trim();
                    if (name.Length > 0 && command.Length > 0)
                        ClassConfig.SetGlobal("alias." + name, command);
                }

                textBoxAliases.Tag = null;
            }
        }
    }
}
