using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace GitForce.Settings.Panels
{
    public partial class ControlGitignore : UserControl, IUserSettings
    {
        /// <summary>
        /// File containing user gitignore settings
        /// </summary>
        private string excludesFile;

        public ControlGitignore()
        {
            InitializeComponent();
            UserControlEditGitignore.AddHelp(this, userControlEditGitignore,
                "Git excludes file (git config core.excludesFile): patterns ignored in ALL your repositories, on this " +
                "computer only. It is never committed, so it suits files that only you produce: editor and IDE files " +
                "(.vs/, *.suo, .idea/), system files (Thumbs.db, desktop.ini), personal notes. Patterns that belong to a " +
                "project go in its .gitignore file instead (the \".gitignore\" page).");
        }

        /// <summary>
        /// Initialize pertinent settings
        /// </summary>
        /// <param name="options">All git global settings</param>
        public void Init(string[] options)
        {
            // Get a path to user excludes file, or create that file if it is not defined
            excludesFile = options.FirstOrDefault(s => s.Contains("core.excludesfile"));

            // If the file does not exist, reset it
            if (excludesFile == null)
                excludesFile = Path.Combine(App.AppHome, ".gitexcludesfile");

            excludesFile = excludesFile.Split('\n').Last();

            if (!File.Exists(excludesFile))
                try
                {
                    File.CreateText(excludesFile).Close();
                }
                catch (Exception ex)
                {
                    App.PrintStatusMessage("Error loading " + excludesFile + ": " + ex.Message, MessageType.Error);
                }

            userControlEditGitignore.LoadGitIgnore(excludesFile);
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
            userControlEditGitignore.SaveGitIgnore(excludesFile);
            ClassConfig.SetGlobal("core.excludesfile", excludesFile);
        }
    }
}
