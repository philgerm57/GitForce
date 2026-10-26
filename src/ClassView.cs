using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GitForce.Properties;

namespace GitForce
{
    /// <summary>
    /// This class contains support functions and data definitions
    /// for the various views within forms.
    /// </summary>
    public static class ClassView
    {
        /// <summary>
        /// Enumeration of icons for files at different stage
        /// </summary>
        public enum Img
        {
            FileUnmodified,
            FileModified,
            FileAdded,
            FileDeleted,
            FileRenamed,
            FileCopied,
            FileUnmerged,
            FileUntracked,

            FolderClosed,
            FolderOpened,
            DatabaseClosed,
            DatabaseOpened,
            ChangelistRoot,
            Changelist,
            FolderClosedModified,
            FolderOpenedModified,

            SubmoduleClosed,
            SubmoduleOpened,
            SubmoduleModified,
            SubmoduleUninitialized,

            // Folders use pairs of images (closed, opened) so that expanding a node can set bit 0
            FolderIgnored,
            FolderIgnoredOpened,
            FileIgnored,
        }

        /// <summary>
        /// Describes a mapping from image number into the resource icon
        /// </summary>
        private static readonly Dictionary<Img, Icon> Res = new Dictionary<Img, Icon> {
            { Img.FileUnmodified,       Resources.TreeIconU },
            { Img.FileModified,         Resources.TreeIconM },
            { Img.FileAdded,            Resources.TreeIconA },
            { Img.FileDeleted,          Resources.TreeIconD },
            { Img.FileRenamed,          Resources.TreeIconR },
            { Img.FileCopied,           Resources.TreeIconC },
            { Img.FileUnmerged,         Resources.TreeIconX },
            { Img.FileUntracked,        Resources.TreeIconQ },

            { Img.FolderClosed,         Resources.TreeIconFC },
            { Img.FolderOpened,         Resources.TreeIconFO },
            { Img.DatabaseClosed,       Resources.TreeIconDB },
            { Img.DatabaseOpened,       Resources.TreeIconDB },
            { Img.ChangelistRoot,       Resources.Change0 },
            { Img.Changelist,           Resources.Change1 },
            { Img.FolderClosedModified, Resources.TreeIconFCM },
            { Img.FolderOpenedModified, Resources.TreeIconFOM },

            { Img.SubmoduleClosed,       Resources.TreeIconSM },
            { Img.SubmoduleOpened,       Resources.TreeIconSMO },
            { Img.SubmoduleModified,     Resources.TreeIconSMM },
            { Img.SubmoduleUninitialized, Resources.TreeIconSMU },
        };

        /// <summary>
        /// Describes a mapping from a git file status code to the image associated with it
        /// </summary>
        private static readonly Dictionary<char, Img> Staticons = new Dictionary<char, Img> {
            { ' ', Img.FileUnmodified },
            { 'M', Img.FileModified },
            { 'T', Img.FileModified },
            { 'A', Img.FileAdded },
            { 'D', Img.FileDeleted },
            { 'R', Img.FileRenamed },
            { 'C', Img.FileCopied },
            { 'U', Img.FileUnmerged },
            { '?', Img.FileUntracked },
        };

        /// <summary>
        /// Returns the image list to be used with a tree view showing the files
        /// </summary>
        public static ImageList GetImageList()
        {
            ImageList il = new ImageList();
            il.ImageSize = new Size(32, 16);
            il.ColorDepth = ColorDepth.Depth32Bit;

            if (Properties.Settings.Default.ModernIcons)
            {
                // Line icons in the order of the Img enumeration: a state on the left, an object on the right
                il.Images.Add(ClassIcons.TreeImage(null, "file"));                    // FileUnmodified
                il.Images.Add(ClassIcons.TreeImage("bmod", "file"));                  // FileModified
                il.Images.Add(ClassIcons.TreeImage("badd", "file"));                  // FileAdded
                il.Images.Add(ClassIcons.TreeImage("bdel", "file"));                  // FileDeleted
                il.Images.Add(ClassIcons.TreeImage("bren", "file"));                  // FileRenamed
                il.Images.Add(ClassIcons.TreeImage("bcopy", "file"));                 // FileCopied
                il.Images.Add(ClassIcons.TreeImage("bunmerged", "file"));             // FileUnmerged
                il.Images.Add(ClassIcons.TreeImage("buntracked", "file"));            // FileUntracked
                il.Images.Add(ClassIcons.TreeImage(null, "treefolder"));              // FolderClosed
                il.Images.Add(ClassIcons.TreeImage(null, "treefolderopen"));          // FolderOpened
                il.Images.Add(ClassIcons.TreeImage(null, "btRepos"));                 // DatabaseClosed
                il.Images.Add(ClassIcons.TreeImage(null, "btRepos"));                 // DatabaseOpened
                il.Images.Add(ClassIcons.TreeImage(null, "changelists"));             // ChangelistRoot
                il.Images.Add(ClassIcons.TreeImage(null, "changelist"));              // Changelist
                il.Images.Add(ClassIcons.TreeImage("bmod", "treefolder"));            // FolderClosedModified
                il.Images.Add(ClassIcons.TreeImage("bmod", "treefolderopen"));        // FolderOpenedModified
                il.Images.Add(ClassIcons.TreeImage(null, "submodule"));               // SubmoduleClosed
                il.Images.Add(ClassIcons.TreeImage(null, "submodule"));               // SubmoduleOpened
                il.Images.Add(ClassIcons.TreeImage("bmod", "submodulemod"));          // SubmoduleModified
                il.Images.Add(ClassIcons.TreeImage(null, "submoduleoff", true));      // SubmoduleUninitialized
                il.Images.Add(ClassIcons.TreeImage("eyeoff", "treefolder", true));    // FolderIgnored
                il.Images.Add(ClassIcons.TreeImage("eyeoff", "treefolderopen", true));// FolderIgnoredOpened
                il.Images.Add(ClassIcons.TreeImage("eyeoff", "file", true));          // FileIgnored
                return il;
            }

            foreach (var kvp in Res)
                il.Images.Add(kvp.Value);

            // Images for ignored folders and files are drawn from the existing ones
            il.Images.Add(MakeIgnoredImage(Resources.TreeIconFC, true));
            il.Images.Add(MakeIgnoredImage(Resources.TreeIconFO, true));
            il.Images.Add(MakeIgnoredImage(Resources.TreeIconU, false));

            return il;
        }

        /// <summary>
        /// Draws the image of an ignored file or folder: a faded file or folder with a red "no entry" sign
        /// </summary>
        private static Bitmap MakeIgnoredImage(Icon icon, bool folder)
        {
            Bitmap result = new Bitmap(32, 16);
            using (Bitmap source = icon.ToBitmap())
            using (Graphics g = Graphics.FromImage(result))
            {
                using (ImageAttributes attr = new ImageAttributes())
                {
                    attr.SetColorMatrix(new ColorMatrix { Matrix33 = 0.45f });
                    // File icons have their status symbol on the left half: keep only the file picture
                    Rectangle src = folder ? new Rectangle(0, 0, source.Width, source.Height) : new Rectangle(16, 0, 16, 16);
                    g.DrawImage(source, new Rectangle(folder ? 0 : 16, 0, src.Width, src.Height),
                        src.X, src.Y, src.Width, src.Height, GraphicsUnit.Pixel, attr);
                }
                g.SmoothingMode = SmoothingMode.AntiAlias;
                RectangleF sign = folder ? new RectangleF(0.5f, 4.5f, 10, 10) : new RectangleF(2.5f, 2.5f, 11, 11);
                using (Pen pen = new Pen(Color.FromArgb(215, 25, 25), 2f))
                {
                    g.FillEllipse(Brushes.White, sign);
                    g.DrawEllipse(pen, sign);
                    g.DrawLine(pen, sign.Left + 2.5f, sign.Bottom - 2.5f, sign.Right - 2.5f, sign.Top + 2.5f);
                }
            }
            return result;
        }

        /// <summary>
        /// Traverse tree nodes and assign an icon corresponding to each file status.
        /// Since each file has 2 status codes, X and Y, for index with relation to the
        /// git cache, and for current file with relation to the index, isIndex
        /// specifies which code will be used.
        /// </summary>
        public static void ViewAssignIcon(ClassStatus status, TreeNode rootNode, bool isIndex, bool markIgnored = false, bool combined = false)
        {
            TreeNodeCollection nodes = rootNode.Nodes;
            foreach (TreeNode tn in nodes)
            {

                if (tn.Tag is ClassCommit)
                    tn.ImageIndex = (int)Img.Changelist;
                else
                {
                    string name = isIndex ? tn.Text : tn.Tag.ToString();
                    string pathToCheck = name.TrimEnd(Path.DirectorySeparatorChar);

                    // First check if this is a submodule (works for both leaf nodes and folders)
                    if (App.Repos.Current != null && App.Repos.Current.Submodules != null && App.Repos.Current.Submodules.IsSubmodule(pathToCheck))
                    {
                        var sm = App.Repos.Current.Submodules.Get(pathToCheck);
                        if (!sm.IsInitialized)
                            tn.ImageIndex = (int)Img.SubmoduleUninitialized;
                        else if (sm.StatusCode == '+')
                            tn.ImageIndex = (int)Img.SubmoduleModified;
                        else
                            tn.ImageIndex = tn.IsExpanded ? (int)Img.SubmoduleOpened : (int)Img.SubmoduleClosed;
                    }
                    else if (markIgnored && !status.IsMarked(name) && status.IsIgnored(pathToCheck))
                    {
                        bool isFolder = tn.Nodes.Count > 0 || Directory.Exists(Path.Combine(App.Repos.Current.Path, pathToCheck));
                        tn.ImageIndex = isFolder ? (int)Img.FolderIgnored : (int)Img.FileIgnored;
                    }
                    else if (status.IsMarked(name) || tn.Nodes.Count == 0)
                    {
                        // File node - assign status icon
                        char icon = isIndex ? status.Xcode(name) : status.Ycode(name);
                        if (combined && !isIndex && icon == ' ')
                            icon = status.Xcode(name);
                        if (combined && status.IsMarked(name))
                        {
                            char x = status.Xcode(name), y = status.Ycode(name);
                            tn.ToolTipText = x == '?' ? "Untracked (new file)"
                                           : (x != ' ' && y != ' ') ? "Staged, then modified again"
                                           : x != ' ' ? "Staged (in Pending Changelists)" : "Modified, not staged";
                        }
                        if (Staticons.ContainsKey(icon))
                            tn.ImageIndex = (int)Staticons[icon];
                        else
                            tn.ImageIndex = 0;
                    }
                    else
                    {
                        // Folder node - check for modified descendants
                        tn.ImageIndex = status.HasModifiedDescendants(name)
                           ? (int)Img.FolderClosedModified
                           : (int)Img.FolderClosed;
                    }
                }
                ViewAssignIcon(status, tn, isIndex, markIgnored, combined);
            }
        }

        public static void ViewUpdateToolTips (TreeNode rootNode) {
            TreeNodeCollection nodes = rootNode.Nodes;
            foreach (TreeNode tn in nodes)
            {
                switch ((Img)tn.ImageIndex) {
                    case Img.FileUnmodified: /* no tooltip for unmodified files */ break;
                    case Img.FileModified:   tn.ToolTipText = "Modified"; break;
                    case Img.FileAdded:      tn.ToolTipText = "Added"; break;
                    case Img.FileDeleted:    tn.ToolTipText = "Deleted"; break;
                    case Img.FileRenamed:    tn.ToolTipText = "Renamed"; break;
                    case Img.FileCopied:     tn.ToolTipText = "Copied"; break;
                    case Img.FileUnmerged:   tn.ToolTipText = "Unmerged"; break;
                    case Img.FileUntracked:  tn.ToolTipText = "Untracked"; break;
                    case Img.FileIgnored:    tn.ToolTipText = "Ignored (.gitignore)"; break;
                    case Img.FolderIgnored:
                    case Img.FolderIgnoredOpened: tn.ToolTipText = "Ignored folder (.gitignore)"; break;

                    case Img.FolderClosed:
                    case Img.FolderOpened:   /* no tooltip for unmodified folders */ break;

                    case Img.DatabaseClosed:
                    case Img.DatabaseOpened: tn.ToolTipText = "Repository"; break;

                    case Img.ChangelistRoot:
                    case Img.Changelist:     tn.ToolTipText = "Changelist"; break;

                    case Img.FolderClosedModified:
                    case Img.FolderOpenedModified:
                        tn.ToolTipText = "Folder contains changes";
                        break;

                    case Img.SubmoduleClosed:
                    case Img.SubmoduleOpened:
                        tn.ToolTipText = "Submodule";
                        break;
                    case Img.SubmoduleModified:
                        tn.ToolTipText = "Submodule (modified - different commit)";
                        break;
                    case Img.SubmoduleUninitialized:
                        tn.ToolTipText = "Submodule (not initialized)";
                        break;

                    default:
                        // tn.ToolTipText = "Unknown...";
                        break;
                }

                ViewUpdateToolTips (tn);
            }

        }

        /// <summary>
        /// Build a tree view given the list of files
        /// </summary>
        /// <param name="tnRoot">Root node to base the tree on</param>
        /// <param name="list">List of files in git format</param>
        /// <param name="sortBy">File sorting order</param>
        public static void BuildTree(TreeNode tnRoot, List<string> list, GitDirectoryInfo.SortBy sortBy)
        {
            BuildTreeRecurse(tnRoot, list, sortBy);
        }

        /// <summary>
        /// Recursive function to traverse the virtual directory of
        /// a flat git response files and build a visual tree component.
        /// </summary>
        /// <param name="tnRoot">Root node to base the tree on</param>
        /// <param name="list">List of files in git format</param>
        /// <param name="sortBy">File sorting order</param>
        private static void BuildTreeRecurse(TreeNode tnRoot, List<string> list, GitDirectoryInfo.SortBy sortBy)
        {
            string rootPath = tnRoot.Tag.ToString();
            GitDirectoryInfo dir = new GitDirectoryInfo(rootPath, list);
            GitDirectoryInfo[] dirs = dir.GetDirectories();

            foreach (GitDirectoryInfo d in dirs)
            {
                TreeNode tn = new TreeNode(d.Name);
                tn.Tag = d.FullName + Path.DirectorySeparatorChar;
                tnRoot.Nodes.Add(tn);
                BuildTreeRecurse(tn, d.List, sortBy);
            }

            GitFileInfo[] files = dir.GetFiles(sortBy);

            foreach (GitFileInfo file in files)
            {
                TreeNode tn = new TreeNode(file.Name);
                tn.Tag = file.FullName;
                tnRoot.Nodes.Add(tn);
            }
        }

        /// <summary>
        /// Builds a flat list of tree nodes based on a given list of files
        /// </summary>
        /// <param name="tnRoot">Root node to build the list on</param>
        /// <param name="list">List of relative path file names</param>
        public static void BuildFileList(TreeNode tnRoot, List<string> list, GitDirectoryInfo.SortBy sortBy)
        {
            // Sort the list according to the sorting rule
            if (sortBy == GitDirectoryInfo.SortBy.Name)
                list.Sort();
            else
            {
                var vSort = from s in list
                            orderby Path.GetExtension(s), s ascending
                            select s;
                list = vSort.ToList();
            }

            // Build a list view);
            foreach (string s in list)
            {
                TreeNode tn = new TreeNode(s);
                tn.Tag = s;
                tnRoot.Nodes.Add(tn);
            }
        }

        /// <summary>
        /// Build a tree view of commit nodes using hints from repo commit groups.
        /// Commit nodes have Tags containing absolute paths to files,
        /// while the relative paths (with respect to repo root) are simply in the Text.
        /// </summary>
        public static TreeNode BuildCommitsView(ClassRepo repo, List<string> list)
        {
            repo.Commits.Rebuild(list);

            // Step 1: Build the default list with all files under this status class
            TreeNode root = new TreeNode("Pending Changelists");
            root.Tag = "root";                      // Tag of the root node

            foreach (var c in repo.Commits.Bundle)
            {
                var commitNode = new TreeNode(GetCommitNodeText(c)) { Tag = c };

                if (c.IsCollapsed) commitNode.Collapse();

                foreach (var f in c.Files)
                {
                    var tn = new TreeNode(f) { Tag = f };
                    commitNode.Nodes.Add(tn);
                }

                root.Nodes.Add(commitNode);
            }
            root.ExpandAll();
            return root;
        }

        public static string GetCommitNodeText(ClassCommit commit)
        {
            if (commit == null) return string.Empty;
            var rerult = commit.DescriptionTitle;
            if (commit.Files.Count > 0)
            {
                rerult += " (" + commit.Files.Count + ")";
            }
            return rerult;
        }
    }
}
