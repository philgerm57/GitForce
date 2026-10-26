using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace GitForce
{
    /// <summary>
    /// Memo of the git commands, grouped by type in an accordion, each one with a short
    /// explanation. A click puts the command in the command line, a double-click runs it in the
    /// current repository; the output is shown on the right.
    /// </summary>
    public class FormGitCommands : Form
    {
        private class Command
        {
            public string Text;         // Without the leading "git "
            public string Description;
            public bool Danger;         // Loses work or rewrites history: asks before running
        }

        private class Group
        {
            public string Title;
            public readonly List<Command> Commands = new List<Command>();
        }

        #region Commands

        private static readonly List<Group> groups = BuildGroups();

        private static List<Group> BuildGroups()
        {
            var list = new List<Group>();
            Group g = null;
            void G(string title) { g = new Group { Title = title }; list.Add(g); }
            void C(string text, string description, bool danger = false) =>
                g.Commands.Add(new Command { Text = text, Description = description, Danger = danger });

            G("État et inspection");
            C("status", "État de la copie de travail : fichiers modifiés, indexés et non suivis.");
            C("status -s -b", "État en version courte, avec la branche courante et son avance/retard.");
            C("diff", "Modifications de la copie de travail qui ne sont pas encore indexées.");
            C("diff --staged", "Modifications indexées, celles qui partiront au prochain commit.");
            C("diff --stat", "Résumé des modifications : fichiers touchés et nombre de lignes.");
            C("diff <commit1> <commit2>", "Différences entre deux commits (SHA, branches ou tags).");
            C("show <commit>", "Détail d'un commit : message, auteur et modifications.");
            C("show --stat HEAD", "Fichiers modifiés par le dernier commit.");
            C("blame <fichier>", "Pour chaque ligne du fichier, le commit et l'auteur qui l'ont modifiée en dernier.");
            C("ls-files", "Liste des fichiers suivis par git.");
            C("ls-files --others --exclude-standard", "Liste des fichiers non suivis (hors fichiers ignorés).");
            C("check-ignore -v <fichier>", "Indique quelle règle du .gitignore ignore ce fichier.");
            C("grep -n \"<texte>\"", "Cherche un texte dans les fichiers suivis, avec les numéros de ligne.");

            G("Index (staging)");
            C("add <fichier>", "Ajoute le fichier à l'index pour le prochain commit.");
            C("add -A", "Indexe toutes les modifications : ajouts, modifications et suppressions.");
            C("add -u", "Indexe les modifications et suppressions des fichiers déjà suivis seulement.");
            C("restore --staged <fichier>", "Retire le fichier de l'index sans toucher à son contenu.");
            C("restore <fichier>", "Annule les modifications non indexées du fichier (perdues définitivement).", true);
            C("rm --cached <fichier>", "Arrête le suivi du fichier en le gardant sur le disque.");
            C("rm <fichier>", "Supprime le fichier du disque et du suivi.", true);
            C("mv <ancien> <nouveau>", "Renomme ou déplace un fichier suivi.");

            G("Commits");
            C("commit -m \"<message>\"", "Enregistre le contenu de l'index dans un nouveau commit.");
            C("commit -a -m \"<message>\"", "Indexe les fichiers suivis modifiés et commite en une fois.");
            C("commit --amend --no-edit", "Ajoute l'index au dernier commit en gardant son message.", true);
            C("commit --amend -m \"<message>\"", "Remplace le message du dernier commit.", true);
            C("revert <commit>", "Crée un commit qui annule les modifications d'un commit précédent.");
            C("cherry-pick <commit>", "Applique sur la branche courante les modifications d'un autre commit.");

            G("Historique");
            C("log --oneline --graph --decorate --all -n 30", "Graphe des 30 derniers commits de toutes les branches.");
            C("log -n 20 --stat", "Les 20 derniers commits avec les fichiers modifiés.");
            C("log --author=\"<nom>\" --oneline", "Commits d'un auteur.");
            C("log --follow --oneline -- <fichier>", "Historique d'un fichier, y compris à travers ses renommages.");
            C("log -S \"<texte>\" --oneline", "Commits qui ont ajouté ou retiré ce texte.");
            C("log --since=\"2 weeks ago\" --oneline", "Commits des deux dernières semaines.");
            C("shortlog -sn", "Nombre de commits par auteur.");
            C("reflog -n 30", "Derniers déplacements de HEAD : permet de retrouver un commit « perdu ».");
            C("describe --tags", "Nom du commit courant d'après le tag le plus proche.");

            G("Branches et fusion");
            C("branch", "Liste des branches locales (la courante est marquée d'une *).");
            C("branch -a -vv", "Toutes les branches, locales et distantes, avec leur dernier commit et leur suivi.");
            C("switch <branche>", "Passe sur une autre branche.");
            C("switch -c <nouvelle-branche>", "Crée une branche à partir du commit courant et passe dessus.");
            C("branch -m <ancien> <nouveau>", "Renomme une branche.");
            C("branch -d <branche>", "Supprime une branche déjà fusionnée.");
            C("branch -D <branche>", "Supprime une branche même si elle n'est pas fusionnée.", true);
            C("merge <branche>", "Fusionne la branche indiquée dans la branche courante.");
            C("merge --no-ff <branche>", "Fusionne en créant toujours un commit de fusion.");
            C("merge --abort", "Abandonne une fusion en conflit et revient à l'état d'avant.");
            C("rebase <branche>", "Rejoue les commits de la branche courante au-dessus d'une autre (réécrit l'historique).", true);
            C("rebase --continue", "Reprend un rebase après la résolution des conflits.");
            C("rebase --abort", "Abandonne un rebase en cours.");

            G("Dépôts distants");
            C("remote -v", "Liste des dépôts distants avec leurs adresses.");
            C("remote add origin <url>", "Ajoute un dépôt distant nommé origin.");
            C("fetch --all --prune", "Récupère les nouveautés de tous les dépôts distants sans modifier vos branches.");
            C("pull", "Récupère et fusionne les nouveautés de la branche suivie.");
            C("pull --rebase", "Récupère les nouveautés et rejoue vos commits locaux par-dessus.");
            C("push", "Envoie les commits de la branche courante vers sa branche distante.");
            C("push -u origin <branche>", "Publie une nouvelle branche et la fait suivre par la branche locale.");
            C("push --tags", "Envoie tous les tags locaux.");
            C("push --force-with-lease", "Écrase la branche distante, sauf si quelqu'un d'autre l'a modifiée entre-temps.", true);
            C("push origin --delete <branche>", "Supprime une branche sur le dépôt distant.", true);

            G("Tags");
            C("tag", "Liste des tags.");
            C("tag <nom>", "Pose un tag léger sur le commit courant.");
            C("tag -a <nom> -m \"<message>\"", "Pose un tag annoté (avec auteur, date et message).");
            C("tag -d <nom>", "Supprime un tag local.");
            C("push origin <nom>", "Envoie un tag vers le dépôt distant.");

            G("Remise (stash)");
            C("stash list", "Liste des remises enregistrées.");
            C("stash push -m \"<message>\"", "Met de côté les modifications en cours et nettoie la copie de travail.");
            C("stash push -u -m \"<message>\"", "Comme ci-dessus, en incluant les fichiers non suivis.");
            C("stash show -p stash@{0}", "Contenu détaillé d'une remise.");
            C("stash apply stash@{0}", "Réapplique une remise en la gardant dans la liste.");
            C("stash pop", "Réapplique la dernière remise et la retire de la liste.");
            C("stash drop stash@{0}", "Supprime une remise.", true);
            C("stash clear", "Supprime toutes les remises.", true);

            G("Annuler et nettoyer");
            C("reset --soft HEAD~1", "Défait le dernier commit en gardant ses modifications indexées.");
            C("reset HEAD~1", "Défait le dernier commit en gardant ses modifications, non indexées.");
            C("reset --hard HEAD", "Annule toutes les modifications des fichiers suivis (perdues définitivement).", true);
            C("checkout <commit> -- <fichier>", "Remet un fichier dans l'état où il était à un commit donné.", true);
            C("clean -n -d", "Montre les fichiers non suivis que clean supprimerait (sans rien supprimer).");
            C("clean -f -d", "Supprime les fichiers et dossiers non suivis.", true);

            G("Configuration");
            C("config --global user.name \"<nom>\"", "Nom d'auteur utilisé dans vos commits.");
            C("config --global user.email \"<email>\"", "Adresse e-mail utilisée dans vos commits.");
            C("config --list --show-origin", "Tous les réglages en vigueur et le fichier d'où vient chacun.");
            C("config --global core.autocrlf true", "Windows : convertit les fins de ligne en CRLF à l'extraction et en LF au commit.");
            C("config --global init.defaultBranch main", "Nom de la branche créée par git init.");
            C("--version", "Version de git installée.");

            G("Maintenance");
            C("fsck", "Vérifie l'intégrité des objets du dépôt.");
            C("count-objects -vH", "Taille du dépôt et nombre d'objets.");
            C("gc", "Compacte le dépôt et supprime les objets inutiles.");
            C("rev-parse HEAD", "SHA complet du commit courant.");
            C("rev-parse --show-toplevel", "Dossier racine du dépôt.");
            return list;
        }

        #endregion

        #region Accordion controls

        /// <summary>
        /// Header of a group: a chevron, the title and the number of commands
        /// </summary>
        private class GroupHeader : Control
        {
            public readonly Group Group;
            public bool Expanded;
            public int Count;
            private bool hover;

            public GroupHeader(Group group)
            {
                Group = group;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
                Cursor = Cursors.Hand;
            }

            protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

            protected override void OnPaint(PaintEventArgs e)
            {
                Color back = ClassTheme.Map(SystemColors.Control);
                if (hover)
                    back = ClassTheme.IsDark ? ControlPaint.Light(back, 0.2f) : ControlPaint.Dark(back, 0.05f);
                e.Graphics.Clear(back);
                Color fore = ClassTheme.Map(SystemColors.ControlText);
                using (Pen line = new Pen(ClassTheme.Map(SystemColors.ControlDark)))
                    e.Graphics.DrawLine(line, 0, Height - 1, Width, Height - 1);

                // Chevron: pointing right when closed, down when open
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int cx = 14, cy = Height / 2, s = 4;
                using (Pen pen = new Pen(fore, 2f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round })
                {
                    if (Expanded)
                        e.Graphics.DrawLines(pen, new[] { new Point(cx - s, cy - s / 2), new Point(cx, cy + s / 2), new Point(cx + s, cy - s / 2) });
                    else
                        e.Graphics.DrawLines(pen, new[] { new Point(cx - s / 2, cy - s), new Point(cx + s / 2, cy), new Point(cx - s / 2, cy + s) });
                }
                using (Font bold = new Font(Font, FontStyle.Bold))
                {
                    TextRenderer.DrawText(e.Graphics, Group.Title, bold, new Rectangle(28, 0, Width - 80, Height), fore,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                }
                TextRenderer.DrawText(e.Graphics, Count.ToString(), Font, new Rectangle(Width - 50, 0, 40, Height),
                    ClassTheme.Map(SystemColors.GrayText), TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
            }
        }

        /// <summary>
        /// One command: "git ..." in a fixed font with its explanation below
        /// </summary>
        private class CommandRow : Control
        {
            public readonly Command Command;
            public bool Selected;
            private bool hover;
            private readonly Font mono;

            public CommandRow(Command command, Font mono)
            {
                Command = command;
                this.mono = mono;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            }

            protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

            protected override void OnPaint(PaintEventArgs e)
            {
                Color back = ClassTheme.Map(SystemColors.Window);
                Color fore = ClassTheme.Map(SystemColors.WindowText);
                Color gray = ClassTheme.Map(SystemColors.GrayText);
                if (Selected)
                {
                    back = ClassTheme.Map(SystemColors.Highlight);
                    fore = gray = ClassTheme.Map(SystemColors.HighlightText);
                }
                else if (hover)
                    back = ClassTheme.IsDark ? ControlPaint.Light(back, 0.15f) : ControlPaint.Dark(back, 0.03f);
                e.Graphics.Clear(back);

                int half = Height / 2;
                int x = 28;
                if (Command.Danger)
                {
                    // Commands that lose work or rewrite history get a warning mark
                    Color warn = Selected ? fore : (ClassTheme.IsDark ? Color.FromArgb(255, 140, 120) : Color.Firebrick);
                    TextRenderer.DrawText(e.Graphics, "⚠", Font, new Rectangle(8, 2, 20, half), warn, TextFormatFlags.VerticalCenter);
                }
                TextRenderer.DrawText(e.Graphics, "git " + Command.Text, mono, new Rectangle(x, 2, Width - x - 6, half), fore,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(e.Graphics, Command.Description, Font, new Rectangle(x, half - 1, Width - x - 6, half - 2), gray,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
        }

        /// <summary>
        /// Stacks its visible children from top to bottom, at the full width, and scrolls them
        /// </summary>
        private class StackPanel : Panel
        {
            public StackPanel()
            {
                AutoScroll = true;
                DoubleBuffered = true;
            }

            protected override void OnLayout(LayoutEventArgs e)
            {
                int y = 0;
                int width = ClientSize.Width;
                foreach (Control c in Controls)
                {
                    if (!c.Visible)
                        continue;
                    c.SetBounds(0, y + AutoScrollPosition.Y, width, c.Height);
                    y += c.Height;
                }
                AutoScrollMinSize = new Size(0, y);
                base.OnLayout(e);
            }
        }

        #endregion

        private static FormGitCommands instance;

        private readonly StackPanel stack = new StackPanel { Dock = DockStyle.Fill };
        private readonly TextBox textFilter = new TextBox { Dock = DockStyle.Top };
        private readonly Label labelRepo = new Label { Dock = DockStyle.Top, AutoEllipsis = true, Height = 24, TextAlign = ContentAlignment.MiddleLeft };
        private readonly Label labelHelp = new Label { Dock = DockStyle.Top, Height = 44, AutoEllipsis = true, Padding = new Padding(0, 4, 0, 0) };
        private readonly TextBox textCommand = new TextBox { Dock = DockStyle.Fill };
        private readonly Button btRun = new Button { Text = "Exécuter", Dock = DockStyle.Right, Width = 90 };
        private readonly Button btStop = new Button { Text = "Arrêter", Dock = DockStyle.Right, Width = 80, Enabled = false };
        private readonly Button btClear = new Button { Text = "Effacer", Dock = DockStyle.Right, Width = 80 };
        private readonly RichTextBox textOutput = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, WordWrap = false, DetectUrls = false, BorderStyle = BorderStyle.None };
        private readonly List<GroupHeader> headers = new List<GroupHeader>();
        private readonly List<string> history = new List<string>();
        private int historyIndex;
        private readonly Font mono = new Font("Consolas", 10f);
        private CommandRow selectedRow;

        // The running git process, its output not yet shown and the timer that shows it
        private Process process;
        private readonly StringBuilder pending = new StringBuilder();
        private readonly Timer flushTimer = new Timer { Interval = 100 };
        private int outputLength;
        private const int MaxOutput = 4 * 1024 * 1024;

        /// <summary>
        /// Show the window (there is only one), bringing it to the front if it is already open
        /// </summary>
        public static void ShowWindow(IWin32Window owner)
        {
            if (instance == null || instance.IsDisposed)
            {
                instance = new FormGitCommands();
                instance.Show(owner);
            }
            else
            {
                if (instance.WindowState == FormWindowState.Minimized)
                    instance.WindowState = FormWindowState.Normal;
                instance.Activate();
            }
            instance.UpdateRepo();
        }

        public FormGitCommands()
        {
            Text = "Commandes Git";
            Icon = ClassTheme.AppIcon;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1250, 760);
            MinimumSize = new Size(700, 400);
            KeyPreview = true;

            // Left: the filter and the accordion of commands
            textFilter.PlaceholderText = "Filtrer les commandes (ex. : stash, branche, annuler)";
            textFilter.TextChanged += (s, e) => ApplyFilter();
            Panel left = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6) };
            left.Controls.Add(stack);
            left.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 6 });
            left.Controls.Add(textFilter);
            BuildAccordion();

            // Right: the repository, the explanation of the command, the command line and the output
            Panel line = new Panel { Dock = DockStyle.Top, Height = 30, Padding = new Padding(0, 3, 0, 3) };
            Label labelGit = new Label { Text = "git", Dock = DockStyle.Left, Width = 34, TextAlign = ContentAlignment.MiddleLeft, Font = mono };
            textCommand.Font = mono;
            line.Controls.Add(textCommand);
            line.Controls.Add(labelGit);
            line.Controls.Add(btClear);
            line.Controls.Add(btStop);
            line.Controls.Add(btRun);
            textOutput.Font = mono;

            Panel right = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6) };
            right.Controls.Add(textOutput);
            right.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 6 });
            right.Controls.Add(line);
            right.Controls.Add(labelHelp);
            right.Controls.Add(labelRepo);

            SplitContainer split = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel1 };
            split.Panel1.Controls.Add(left);
            split.Panel2.Controls.Add(right);
            Controls.Add(split);
            Load += (s, e) => split.SplitterDistance = Math.Min(560, ClientSize.Width / 2);

            labelHelp.Text = "Un clic met la commande dans la ligne de commande, un double-clic l'exécute. " +
                             "Remplacez les <valeurs> avant d'exécuter. ⚠ : commande qui peut perdre des modifications ou réécrire l'historique.";

            btRun.Click += (s, e) => Run();
            btStop.Click += (s, e) => Stop();
            btClear.Click += (s, e) => { textOutput.Clear(); outputLength = 0; };
            textCommand.KeyDown += TextCommandKeyDown;
            flushTimer.Tick += (s, e) => Flush();
            AcceptButton = btRun;

            ClassTheme.Apply(this);
            ClassWinGeometry.Restore(this);
            textOutput.BackColor = ClassTheme.Map(SystemColors.Window);
            FormClosing += (s, e) => { ClassWinGeometry.Save(this); Stop(); flushTimer.Stop(); };
            Shown += (s, e) => textCommand.Focus();
        }

        /// <summary>
        /// One header per group followed by its commands; only the first group is open
        /// </summary>
        private void BuildAccordion()
        {
            int headerHeight = Font.Height + 16;
            int rowHeight = Font.Height * 2 + 14;
            stack.SuspendLayout();
            foreach (Group group in groups)
            {
                GroupHeader header = new GroupHeader(group) { Height = headerHeight, Count = group.Commands.Count };
                header.Click += (s, e) => Toggle(header);
                headers.Add(header);
                stack.Controls.Add(header);
                foreach (Command command in group.Commands)
                {
                    CommandRow row = new CommandRow(command, mono) { Height = rowHeight, Visible = false, Tag = header };
                    row.MouseClick += (s, e) => SelectRow(row, false);
                    row.MouseDoubleClick += (s, e) => SelectRow(row, true);
                    new ToolTip().SetToolTip(row, "git " + command.Text + "\n" + command.Description);
                    stack.Controls.Add(row);
                }
            }
            stack.ResumeLayout();
            Toggle(headers[0]);
        }

        /// <summary>
        /// Open a group and close the others; a click on the open group closes it
        /// </summary>
        private void Toggle(GroupHeader header)
        {
            if (textFilter.Text.Trim().Length > 0)
                textFilter.Text = string.Empty;     // Back to the accordion
            bool open = !header.Expanded;
            stack.SuspendLayout();
            foreach (GroupHeader h in headers)
            {
                h.Expanded = open && h == header;
                h.Invalidate();
            }
            foreach (Control c in stack.Controls)
                if (c is CommandRow)
                    c.Visible = ((GroupHeader)c.Tag).Expanded;
            stack.ResumeLayout();
            stack.PerformLayout();
            if (open)
                stack.ScrollControlIntoView(header);
        }

        /// <summary>
        /// With a filter, show every matching command under its open group, and hide the groups
        /// without any; without a filter, go back to the accordion with its first group open
        /// </summary>
        private void ApplyFilter()
        {
            string[] words = textFilter.Text.Trim().ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            stack.SuspendLayout();
            if (words.Length == 0)
            {
                foreach (GroupHeader h in headers)
                {
                    h.Visible = true;
                    h.Expanded = false;
                    h.Count = h.Group.Commands.Count;
                }
                foreach (Control c in stack.Controls)
                    if (c is CommandRow)
                        c.Visible = false;
                stack.ResumeLayout();
                Toggle(headers[0]);
                return;
            }
            foreach (GroupHeader h in headers)
                h.Count = 0;
            foreach (Control c in stack.Controls)
            {
                CommandRow row = c as CommandRow;
                if (row == null)
                    continue;
                GroupHeader header = (GroupHeader)row.Tag;
                string text = ("git " + row.Command.Text + " " + row.Command.Description + " " + header.Group.Title).ToLowerInvariant();
                row.Visible = words.All(text.Contains);
                if (row.Visible)
                    header.Count++;
            }
            foreach (GroupHeader h in headers)
            {
                h.Visible = h.Count > 0;
                h.Expanded = true;
                h.Invalidate();
            }
            stack.ResumeLayout();
            stack.PerformLayout();
            stack.AutoScrollPosition = Point.Empty;
        }

        /// <summary>
        /// Put the command in the command line, with its first value to replace selected;
        /// run it at once on a double-click
        /// </summary>
        private void SelectRow(CommandRow row, bool run)
        {
            if (selectedRow != null)
            {
                selectedRow.Selected = false;
                selectedRow.Invalidate();
            }
            selectedRow = row;
            row.Selected = true;
            row.Invalidate();

            labelHelp.Text = (row.Command.Danger ? "⚠ " : string.Empty) + row.Command.Description;
            textCommand.Text = row.Command.Text;
            Match m = Regex.Match(textCommand.Text, "<[^>]+>");
            textCommand.Focus();
            if (m.Success)
                textCommand.Select(m.Index, m.Length);
            else
                textCommand.Select(textCommand.TextLength, 0);
            if (run)
                Run();
        }

        /// <summary>
        /// Up and Down go through the commands already run
        /// </summary>
        private void TextCommandKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.KeyCode != Keys.Up && e.KeyCode != Keys.Down) || history.Count == 0)
                return;
            historyIndex = Math.Max(0, Math.Min(history.Count, historyIndex + (e.KeyCode == Keys.Up ? -1 : 1)));
            textCommand.Text = historyIndex < history.Count ? history[historyIndex] : string.Empty;
            textCommand.Select(textCommand.TextLength, 0);
            e.Handled = e.SuppressKeyPress = true;
        }

        /// <summary>
        /// Show the repository the commands run in
        /// </summary>
        private void UpdateRepo()
        {
            string path = App.Repos?.Current?.Path;
            labelRepo.Text = path != null ? "Dépôt : " + path : "Aucun dépôt sélectionné : les commandes s'exécutent sans dépôt.";
        }

        /// <summary>
        /// Run the command line in the current repository
        /// </summary>
        private void Run()
        {
            if (process != null)
                return;
            string args = textCommand.Text.Trim();
            if (args.StartsWith("git ", StringComparison.OrdinalIgnoreCase))
                args = args.Substring(4).Trim();
            if (args.Length == 0)
                return;

            Match m = Regex.Match(args, "<[^>]+>");
            if (m.Success)
            {
                MessageBox.Show(this, "Remplacez d'abord " + m.Value + " par une valeur.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                textCommand.Focus();
                textCommand.Select(textCommand.Text.IndexOf(m.Value, StringComparison.Ordinal), m.Length);
                return;
            }
            if (IsDangerous(args) &&
                MessageBox.Show(this, "Cette commande peut perdre des modifications ou réécrire l'historique :\n\ngit " + args + "\n\nL'exécuter ?",
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            if (history.Count == 0 || history[history.Count - 1] != args)
                history.Add(args);
            historyIndex = history.Count;
            UpdateRepo();

            string workDir = App.Repos?.Current?.Path ?? Environment.CurrentDirectory;
            AppendText(Environment.NewLine + "> git " + args + Environment.NewLine, Accent());

            ProcessStartInfo psi = new ProcessStartInfo(Properties.Settings.Default.GitPath, "-c core.quotepath=false " + args)
            {
                WorkingDirectory = workDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            // Nothing can be typed in a hidden console: no prompt for a password, no editor
            // opened for a merge message, and Notepad as the editor when none is configured
            psi.Environment["GIT_TERMINAL_PROMPT"] = "0";
            psi.Environment["GIT_MERGE_AUTOEDIT"] = "no";
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GIT_EDITOR")) &&
                DiffViewControl.RunGitRaw(workDir, "config core.editor").Trim().Length == 0)
                psi.Environment["GIT_EDITOR"] = "notepad";

            try
            {
                Process p = new Process { StartInfo = psi, EnableRaisingEvents = true };
                p.OutputDataReceived += (s, e) => { if (e.Data != null) lock (pending) pending.AppendLine(e.Data); };
                p.ErrorDataReceived += (s, e) => { if (e.Data != null) lock (pending) pending.AppendLine(e.Data); };
                p.Exited += (s, e) => BeginInvoke((Action)(() => Finished(p, args)));
                p.Start();
                p.StandardInput.Close();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                process = p;
                btRun.Enabled = false;
                btStop.Enabled = true;
                flushTimer.Start();
            }
            catch (Exception ex)
            {
                AppendText(ex.Message + Environment.NewLine, ErrorColor());
            }
        }

        /// <summary>
        /// The command ended: show the rest of its output and its exit code, then refresh the
        /// main window if the command may have changed something
        /// </summary>
        private void Finished(Process p, string args)
        {
            if (IsDisposed)
                return;
            p.WaitForExit();        // Wait for the end of the redirected output
            Flush();
            flushTimer.Stop();
            int code = p.ExitCode;
            p.Dispose();
            process = null;
            btRun.Enabled = true;
            btStop.Enabled = false;
            if (code == 0)
                AppendText("(terminé)" + Environment.NewLine, ClassTheme.Map(SystemColors.GrayText));
            else
                AppendText("(code de sortie " + code + ")" + Environment.NewLine, ErrorColor());
            textCommand.Focus();
            textCommand.SelectAll();

            if (!IsReadOnly(args) && App.Repos?.Current != null)
                App.DoRefresh();
        }

        /// <summary>
        /// Stop the running command
        /// </summary>
        private void Stop()
        {
            try
            {
                if (process != null && !process.HasExited)
                    process.Kill(true);
            }
            catch (Exception)
            {
                // It ended in the meantime
            }
        }

        /// <summary>
        /// Show the output received since the last call
        /// </summary>
        private void Flush()
        {
            string text;
            lock (pending)
            {
                text = pending.ToString();
                pending.Clear();
            }
            if (text.Length == 0 || outputLength > MaxOutput)
                return;
            if (outputLength + text.Length > MaxOutput)
                text = text.Substring(0, MaxOutput - outputLength) + Environment.NewLine + "... (sortie tronquée)" + Environment.NewLine;
            AppendText(text, ClassTheme.Map(SystemColors.WindowText));
        }

        private void AppendText(string text, Color color)
        {
            textOutput.SelectionStart = textOutput.TextLength;
            textOutput.SelectionLength = 0;
            textOutput.SelectionColor = color;
            textOutput.AppendText(text);
            textOutput.ScrollToCaret();
            outputLength += text.Length;
        }

        private static Color Accent()
        {
            return ClassTheme.IsDark ? Color.FromArgb(120, 170, 255) : Color.FromArgb(0, 90, 200);
        }

        private static Color ErrorColor()
        {
            return ClassTheme.IsDark ? Color.FromArgb(255, 140, 120) : Color.Firebrick;
        }

        /// <summary>
        /// Commands that lose changes or rewrite history
        /// </summary>
        private static bool IsDangerous(string args)
        {
            return Regex.IsMatch(args,
                @"^(reset\b.*--hard|clean\b(?!.*(-n|--dry-run)\b)|push\b.*(--force|\s-f\b|--delete|\s-d\b|\s:)|branch\b.*\s-D\b|" +
                @"stash\s+(drop|clear)\b|restore\b(?!.*--staged)|checkout\b.*\s--\s|rebase\b(?!.*--(abort|continue))|" +
                @"commit\b.*--amend|rm\b(?!.*--cached)|filter-branch\b|update-ref\b.*-d\b|tag\b.*\s-d\b)");
        }

        /// <summary>
        /// Commands that only read: no need to refresh the main window after them
        /// </summary>
        private static bool IsReadOnly(string args)
        {
            string verb = args.Split(' ')[0];
            if (new[] { "status", "diff", "show", "log", "blame", "ls-files", "check-ignore", "grep", "shortlog", "reflog",
                        "describe", "rev-parse", "fsck", "count-objects", "--version", "help", "ls-remote", "cat-file" }.Contains(verb))
                return true;
            return Regex.IsMatch(args, @"^(stash\s+(list|show)|remote\s*(-v)?$|branch\s*(-a|-r|-v|-vv|\s)*$|tag\s*$|config\b.*(--list|--get))");
        }
    }
}
