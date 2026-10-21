using System.Collections.Generic;

namespace GitForce
{
    /// <summary>
    /// Contains the code to handle various help links and access to the online help
    /// </summary>
    static class ClassHelp
    {
        /// <summary>
        /// Dictionary containing the translation of help topics to the web pages
        /// </summary>
        private static readonly Dictionary<string, string> Webhelp = new Dictionary<string, string>
        {
            {"Home", @"https://baltazarstudios.com/software/gitforce/"},
            {"Getting Started", @"https://baltazarstudios.com/software/gitforce#getting-started"},
            {"Edit Tools", @"https://baltazarstudios.com/software/gitforce-tools"},
            {"HTTPS Authentication", @"https://confluence.atlassian.com/fisheye/permanent-authentication-for-git-repositories-over-http-s-298977121.html"},
            {"SSH Windows", @"https://baltazarstudios.com/software/gitforce-ssh"},
            {"Workspace", @"https://baltazarstudios.com/software/gitforce-workspaces"},
            {"Update Check", @"https://github.com/gdevic/GitForce/releases"},
            {"Download", @"https://github.com/gdevic/GitForce/releases"},
            {"Discussion", @"https://sourceforge.net/p/gitforce/discussion"},
            {"GPLv3", @"https://www.gnu.org/licenses/gpl-3.0.en.html"},
            {"BaltazarStudios", @"https://www.baltazarstudios.com" }
        };

        /// <summary>
        /// Open the French getting started guide. It is embedded in the program: write it to the
        /// temporary folder and hand it to the PDF reader associated with it.
        /// </summary>
        public static void OpenFrenchGuide()
        {
            const string name = "GitForce-Guide-FR.pdf";
            try
            {
                string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GitForce");
                System.IO.Directory.CreateDirectory(dir);
                string file = System.IO.Path.Combine(dir, name);
                using (System.IO.Stream res = typeof(ClassHelp).Assembly.GetManifestResourceStream(name))
                {
                    if (res == null)
                        throw new System.IO.FileNotFoundException("The guide is missing from this build of GitForce.", name);
                    using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                    {
                        res.CopyTo(ms);
                        byte[] data = ms.ToArray();
                        // Rewrite it only when it changed: the file may be open in the reader
                        if (!System.IO.File.Exists(file) || new System.IO.FileInfo(file).Length != data.Length)
                            System.IO.File.WriteAllBytes(file, data);
                    }
                }
                ClassUtils.ShellOpen(file);
            }
            catch (System.Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Unable to open the guide: " + ex.Message, "Guide de démarrage",
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Given the topic, open the relevant help page online
        /// </summary>
        public static void Handler(string topic)
        {
            if (!Webhelp.ContainsKey(topic))
            {
                App.PrintStatusMessage("Internal Error: Please report that `topic " + topic + "` not found!", MessageType.Error);
                return;
            }
            // Hand this to the shared helper rather than starting a browser here: it knows how
            // to launch one on each platform, and it reports a failure instead of throwing.
            // Launching a browser can always fail, so this must never escape to the caller.
            ClassUtils.OpenWebLink(Webhelp[topic]);
        }
    }
}
