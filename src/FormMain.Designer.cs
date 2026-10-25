namespace GitForce
{
    partial class FormMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            menuMain = new System.Windows.Forms.MenuStrip();
            menuMainFile = new System.Windows.Forms.ToolStripMenuItem();
            menuMainEdit = new System.Windows.Forms.ToolStripMenuItem();
            menuMainSelectAll = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
            menuMainStash = new System.Windows.Forms.ToolStripMenuItem();
            menuMainUnstash = new System.Windows.Forms.ToolStripMenuItem();
            menuMainView = new System.Windows.Forms.ToolStripMenuItem();
            menuMainPendingChanges = new System.Windows.Forms.ToolStripMenuItem();
            menuMainSubmittedChanges = new System.Windows.Forms.ToolStripMenuItem();
            menuMainBranches = new System.Windows.Forms.ToolStripMenuItem();
            menuMainRepos = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            menuView0 = new System.Windows.Forms.ToolStripMenuItem();
            menuView1 = new System.Windows.Forms.ToolStripMenuItem();
            menuView2 = new System.Windows.Forms.ToolStripMenuItem();
            menuView3 = new System.Windows.Forms.ToolStripMenuItem();
            menuView4 = new System.Windows.Forms.ToolStripMenuItem();
            menuView5 = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            menuMainRefresh = new System.Windows.Forms.ToolStripMenuItem();
            menuViewLogWindow = new System.Windows.Forms.ToolStripMenuItem();
            menuViewCommandLine = new System.Windows.Forms.ToolStripMenuItem();
            menuMainSettings = new System.Windows.Forms.ToolStripMenuItem();
            menuMainOptions = new System.Windows.Forms.ToolStripMenuItem();
            menuMainManageKeys = new System.Windows.Forms.ToolStripMenuItem();
            menuMainManageHttps = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            menuMainSwitchRemoteRepo = new System.Windows.Forms.ToolStripMenuItem();
            xToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            menuMainEditRemoteRepo = new System.Windows.Forms.ToolStripMenuItem();
            menuMainFetchFromRemote = new System.Windows.Forms.ToolStripMenuItem();
            menuMainPullFromRemote = new System.Windows.Forms.ToolStripMenuItem();
            menuMainPushToRemote = new System.Windows.Forms.ToolStripMenuItem();
            menuMainChangelist = new System.Windows.Forms.ToolStripMenuItem();
            menuMainBranch = new System.Windows.Forms.ToolStripMenuItem();
            menuMainRepository = new System.Windows.Forms.ToolStripMenuItem();
            menuMainTools = new System.Windows.Forms.ToolStripMenuItem();
            menuMainHelp = new System.Windows.Forms.ToolStripMenuItem();
            homeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            gettingStartedMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            checkForUpdatesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            statusInfoLabel = new System.Windows.Forms.ToolStripStatusLabel();
            statusStrip = new System.Windows.Forms.StatusStrip();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            splitContainer2 = new System.Windows.Forms.SplitContainer();
            rightTabControl = new TabEx();
            listStatus = new ListBoxEx();
            menuStatus = new System.Windows.Forms.ContextMenuStrip(components);
            menuStatusCopy = new System.Windows.Forms.ToolStripMenuItem();
            menuStatusSelectAll = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            menuStatusClear = new System.Windows.Forms.ToolStripMenuItem();
            cmdBox = new TextBoxEx();
            toolStrip = new System.Windows.Forms.ToolStrip();
            btAdd = new System.Windows.Forms.ToolStripButton();
            btUpdate = new System.Windows.Forms.ToolStripButton();
            btUpdateAll = new System.Windows.Forms.ToolStripButton();
            btRevert = new System.Windows.Forms.ToolStripButton();
            btDelete = new System.Windows.Forms.ToolStripButton();
            btDeleteFs = new System.Windows.Forms.ToolStripButton();
            btEdit = new System.Windows.Forms.ToolStripButton();
            toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
            btPull = new System.Windows.Forms.ToolStripButton();
            btPush = new System.Windows.Forms.ToolStripButton();
            toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
            btChangelists = new System.Windows.Forms.ToolStripButton();
            btSubmitted = new System.Windows.Forms.ToolStripButton();
            btBranches = new System.Windows.Forms.ToolStripButton();
            btRepos = new System.Windows.Forms.ToolStripButton();
            toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            btCancelOperation = new System.Windows.Forms.ToolStripButton();
            btOptions = new System.Windows.Forms.ToolStripButton();
            btSsh = new System.Windows.Forms.ToolStripButton();
            btNewVersion = new System.Windows.Forms.ToolStripButton();
            btHttps = new System.Windows.Forms.ToolStripButton();
            timerBusy = new System.Windows.Forms.Timer(components);
            loadWk = new System.Windows.Forms.OpenFileDialog();
            saveWk = new System.Windows.Forms.SaveFileDialog();
            openTools = new System.Windows.Forms.OpenFileDialog();
            saveTools = new System.Windows.Forms.SaveFileDialog();
            createWk = new System.Windows.Forms.SaveFileDialog();
            importWk = new System.Windows.Forms.OpenFileDialog();
            menuMain.SuspendLayout();
            statusStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            menuStatus.SuspendLayout();
            toolStrip.SuspendLayout();
            SuspendLayout();
            // 
            // menuMain
            // 
            menuMain.ImageScalingSize = new System.Drawing.Size(20, 20);
            menuMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { menuMainFile, menuMainEdit, menuMainView, menuMainSettings, menuMainChangelist, menuMainBranch, menuMainRepository, menuMainTools, menuMainHelp });
            menuMain.Location = new System.Drawing.Point(0, 0);
            menuMain.Name = "menuMain";
            menuMain.Padding = new System.Windows.Forms.Padding(8, 3, 0, 3);
            menuMain.Size = new System.Drawing.Size(1045, 30);
            menuMain.TabIndex = 0;
            menuMain.Text = "menuStrip1";
            // 
            // menuMainFile
            // 
            menuMainFile.Name = "menuMainFile";
            menuMainFile.Size = new System.Drawing.Size(46, 24);
            menuMainFile.Text = "File";
            menuMainFile.DropDownOpening += MenuMainFileDropDownOpening;
            // 
            // menuMainEdit
            // 
            menuMainEdit.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { menuMainSelectAll, toolStripSeparator9, menuMainStash, menuMainUnstash });
            menuMainEdit.Name = "menuMainEdit";
            menuMainEdit.Size = new System.Drawing.Size(49, 24);
            menuMainEdit.Text = "Edit";
            // 
            // menuMainSelectAll
            // 
            menuMainSelectAll.Name = "menuMainSelectAll";
            menuMainSelectAll.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A;
            menuMainSelectAll.Size = new System.Drawing.Size(206, 26);
            menuMainSelectAll.Text = "Select All";
            menuMainSelectAll.Click += MenuMainSelectAllClick;
            // 
            // toolStripSeparator9
            // 
            toolStripSeparator9.Name = "toolStripSeparator9";
            toolStripSeparator9.Size = new System.Drawing.Size(203, 6);
            // 
            // menuMainStash
            // 
            menuMainStash.Enabled = false;
            menuMainStash.Name = "menuMainStash";
            menuMainStash.Size = new System.Drawing.Size(206, 26);
            menuMainStash.Text = "Stash";
            menuMainStash.Click += MenuMainStashClick;
            // 
            // menuMainUnstash
            // 
            menuMainUnstash.Enabled = false;
            menuMainUnstash.Name = "menuMainUnstash";
            menuMainUnstash.Size = new System.Drawing.Size(206, 26);
            menuMainUnstash.Text = "Unstash";
            menuMainUnstash.Click += MenuMainUnstashClick;
            // 
            // menuMainView
            // 
            menuMainView.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { menuMainPendingChanges, menuMainSubmittedChanges, menuMainBranches, menuMainRepos, toolStripSeparator3, menuView0, menuView1, menuView2, menuView3, menuView4, menuView5, toolStripSeparator4, menuMainRefresh, menuViewLogWindow, menuViewCommandLine });
            menuMainView.Name = "menuMainView";
            menuMainView.Size = new System.Drawing.Size(55, 24);
            menuMainView.Text = "View";
            menuMainView.DropDownOpened += MenuMainViewDropDownOpened;
            // 
            // menuMainPendingChanges
            // 
            menuMainPendingChanges.Name = "menuMainPendingChanges";
            menuMainPendingChanges.ShortcutKeys = System.Windows.Forms.Keys.F6;
            menuMainPendingChanges.Size = new System.Drawing.Size(319, 26);
            menuMainPendingChanges.Tag = "Commits";
            menuMainPendingChanges.Text = "Pending Changelists";
            menuMainPendingChanges.Click += RightPanelSelectionEvent;
            // 
            // menuMainSubmittedChanges
            // 
            menuMainSubmittedChanges.Name = "menuMainSubmittedChanges";
            menuMainSubmittedChanges.ShortcutKeys = System.Windows.Forms.Keys.F7;
            menuMainSubmittedChanges.Size = new System.Drawing.Size(319, 26);
            menuMainSubmittedChanges.Tag = "Revisions";
            menuMainSubmittedChanges.Text = "Submitted Changelists";
            menuMainSubmittedChanges.Click += RightPanelSelectionEvent;
            // 
            // menuMainBranches
            // 
            menuMainBranches.Name = "menuMainBranches";
            menuMainBranches.ShortcutKeys = System.Windows.Forms.Keys.F8;
            menuMainBranches.Size = new System.Drawing.Size(319, 26);
            menuMainBranches.Tag = "Branches";
            menuMainBranches.Text = "Branches";
            menuMainBranches.Click += RightPanelSelectionEvent;
            // 
            // menuMainRepos
            // 
            menuMainRepos.Name = "menuMainRepos";
            menuMainRepos.ShortcutKeys = System.Windows.Forms.Keys.F10;
            menuMainRepos.Size = new System.Drawing.Size(319, 26);
            menuMainRepos.Tag = "Repos";
            menuMainRepos.Text = "Repository";
            menuMainRepos.Click += RightPanelSelectionEvent;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new System.Drawing.Size(316, 6);
            // 
            // menuView0
            // 
            menuView0.Name = "menuView0";
            menuView0.ShortcutKeys = System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.D1;
            menuView0.Size = new System.Drawing.Size(319, 26);
            menuView0.Tag = "0";
            menuView0.Text = "Git Status of All Files";
            menuView0.Click += ViewSetByMenuItem;
            // 
            // menuView1
            // 
            menuView1.Name = "menuView1";
            menuView1.ShortcutKeys = System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.D2;
            menuView1.Size = new System.Drawing.Size(319, 26);
            menuView1.Tag = "1";
            menuView1.Text = "Git Status";
            menuView1.Click += ViewSetByMenuItem;
            // 
            // menuView2
            // 
            menuView2.Name = "menuView2";
            menuView2.ShortcutKeys = System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.D3;
            menuView2.Size = new System.Drawing.Size(319, 26);
            menuView2.Tag = "2";
            menuView2.Text = "Git View of Repo";
            menuView2.Click += ViewSetByMenuItem;
            // 
            // menuView3
            // 
            menuView3.Name = "menuView3";
            menuView3.ShortcutKeys = System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.D4;
            menuView3.Size = new System.Drawing.Size(319, 26);
            menuView3.Tag = "3";
            menuView3.Text = "Local File View";
            menuView3.Click += ViewSetByMenuItem;
            // 
            // menuView4
            // 
            menuView4.Name = "menuView4";
            menuView4.ShortcutKeys = System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.D5;
            menuView4.Size = new System.Drawing.Size(319, 26);
            menuView4.Tag = "4";
            menuView4.Text = "Local Files Not in Repo";
            menuView4.Click += ViewSetByMenuItem;
            // 
            // menuView5
            // 
            menuView5.Name = "menuView5";
            menuView5.ShortcutKeys = System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.D6;
            menuView5.Size = new System.Drawing.Size(319, 26);
            menuView5.Tag = "5";
            menuView5.Text = "Local Files Not in .gitignore";
            menuView5.Click += ViewSetByMenuItem;
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new System.Drawing.Size(316, 6);
            // 
            // menuMainRefresh
            // 
            menuMainRefresh.Name = "menuMainRefresh";
            menuMainRefresh.ShortcutKeys = System.Windows.Forms.Keys.F5;
            menuMainRefresh.Size = new System.Drawing.Size(319, 26);
            menuMainRefresh.Text = "Refresh Active Pane";
            menuMainRefresh.Click += MenuRefreshAll;
            // 
            // menuViewLogWindow
            // 
            menuViewLogWindow.Name = "menuViewLogWindow";
            menuViewLogWindow.Size = new System.Drawing.Size(319, 26);
            menuViewLogWindow.Text = "Log Window";
            menuViewLogWindow.Click += LogWindowToolStripMenuItemClick;
            // 
            // menuViewCommandLine
            // 
            menuViewCommandLine.Name = "menuViewCommandLine";
            menuViewCommandLine.ShortcutKeys = System.Windows.Forms.Keys.F3;
            menuViewCommandLine.Size = new System.Drawing.Size(319, 26);
            menuViewCommandLine.Text = "Command Line";
            menuViewCommandLine.Click += MenuViewCommandLineClick;
            // 
            // menuMainSettings
            // 
            menuMainSettings.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { menuMainOptions, menuMainManageKeys, menuMainManageHttps, toolStripSeparator5, menuMainSwitchRemoteRepo, menuMainEditRemoteRepo, menuMainFetchFromRemote, menuMainPullFromRemote, menuMainPushToRemote });
            menuMainSettings.Name = "menuMainSettings";
            menuMainSettings.Size = new System.Drawing.Size(76, 24);
            menuMainSettings.Text = "Settings";
            // 
            // menuMainOptions
            // 
            menuMainOptions.Name = "menuMainOptions";
            menuMainOptions.Size = new System.Drawing.Size(239, 26);
            menuMainOptions.Text = "Options...";
            menuMainOptions.Click += MenuOptions;
            // 
            // menuMainManageKeys
            // 
            menuMainManageKeys.Enabled = false;
            menuMainManageKeys.Name = "menuMainManageKeys";
            menuMainManageKeys.Size = new System.Drawing.Size(239, 26);
            menuMainManageKeys.Text = "Manage SSH Keys...";
            menuMainManageKeys.Click += MenuMainManageKeysClick;
            // 
            // menuMainManageHttps
            // 
            menuMainManageHttps.Name = "menuMainManageHttps";
            menuMainManageHttps.Size = new System.Drawing.Size(239, 26);
            menuMainManageHttps.Text = "Manage HTTPS Pass...";
            menuMainManageHttps.Click += MenuMainManageHttpsClick;
            // 
            // toolStripSeparator5
            // 
            toolStripSeparator5.Name = "toolStripSeparator5";
            toolStripSeparator5.Size = new System.Drawing.Size(236, 6);
            // 
            // menuMainSwitchRemoteRepo
            // 
            menuMainSwitchRemoteRepo.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { xToolStripMenuItem });
            menuMainSwitchRemoteRepo.Name = "menuMainSwitchRemoteRepo";
            menuMainSwitchRemoteRepo.Size = new System.Drawing.Size(239, 26);
            menuMainSwitchRemoteRepo.Text = "Switch Remote Repo...";
            // 
            // xToolStripMenuItem
            // 
            xToolStripMenuItem.Name = "xToolStripMenuItem";
            xToolStripMenuItem.Size = new System.Drawing.Size(99, 26);
            xToolStripMenuItem.Text = "x";
            // 
            // menuMainEditRemoteRepo
            // 
            menuMainEditRemoteRepo.Name = "menuMainEditRemoteRepo";
            menuMainEditRemoteRepo.Size = new System.Drawing.Size(239, 26);
            menuMainEditRemoteRepo.Text = "Edit Remote Repos...";
            menuMainEditRemoteRepo.Click += MenuEditRemoteRepos;
            // 
            // menuMainFetchFromRemote
            // 
            menuMainFetchFromRemote.Name = "menuMainFetchFromRemote";
            menuMainFetchFromRemote.Size = new System.Drawing.Size(239, 26);
            menuMainFetchFromRemote.Text = "Fetch from Remote";
            menuMainFetchFromRemote.Click += MenuRepoFetch;
            // 
            // menuMainPullFromRemote
            // 
            menuMainPullFromRemote.Name = "menuMainPullFromRemote";
            menuMainPullFromRemote.Size = new System.Drawing.Size(239, 26);
            menuMainPullFromRemote.Text = "Pull from Remote";
            menuMainPullFromRemote.Click += MenuRepoPull;
            // 
            // menuMainPushToRemote
            // 
            menuMainPushToRemote.Name = "menuMainPushToRemote";
            menuMainPushToRemote.Size = new System.Drawing.Size(239, 26);
            menuMainPushToRemote.Text = "Push to Remote";
            menuMainPushToRemote.Click += MenuRepoPush;
            // 
            // menuMainChangelist
            // 
            menuMainChangelist.Name = "menuMainChangelist";
            menuMainChangelist.Size = new System.Drawing.Size(92, 24);
            menuMainChangelist.Text = "Changelist";
            menuMainChangelist.DropDownOpening += MenuMainChangelistDropDownOpening;
            // 
            // menuMainBranch
            // 
            menuMainBranch.Name = "menuMainBranch";
            menuMainBranch.Size = new System.Drawing.Size(82, 24);
            menuMainBranch.Text = "Branches";
            menuMainBranch.DropDownOpening += MenuMainBranchDropDownOpening;
            // 
            // menuMainRepository
            // 
            menuMainRepository.Name = "menuMainRepository";
            menuMainRepository.Size = new System.Drawing.Size(94, 24);
            menuMainRepository.Text = "Repository";
            menuMainRepository.DropDownOpening += MenuMainRepositoryDropDownOpening;
            // 
            // menuMainTools
            // 
            menuMainTools.Name = "menuMainTools";
            menuMainTools.Size = new System.Drawing.Size(58, 24);
            menuMainTools.Text = "Tools";
            menuMainTools.DropDownOpening += MenuMainToolsDropDownOpening;
            // 
            // menuMainHelp
            // 
            menuMainHelp.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { homeMenuItem, gettingStartedMenuItem, checkForUpdatesMenuItem, toolStripSeparator6, aboutToolStripMenuItem });
            menuMainHelp.Name = "menuMainHelp";
            menuMainHelp.Size = new System.Drawing.Size(55, 24);
            menuMainHelp.Text = "Help";
            // 
            // homeMenuItem
            // 
            homeMenuItem.Name = "homeMenuItem";
            homeMenuItem.Size = new System.Drawing.Size(213, 26);
            homeMenuItem.Tag = "Home";
            homeMenuItem.Text = "Home";
            homeMenuItem.Click += WebsiteClick;
            // 
            // gettingStartedMenuItem
            // 
            gettingStartedMenuItem.Name = "gettingStartedMenuItem";
            gettingStartedMenuItem.Size = new System.Drawing.Size(213, 26);
            gettingStartedMenuItem.Tag = "Getting Started";
            gettingStartedMenuItem.Text = "Getting Started";
            gettingStartedMenuItem.Click += WebsiteClick;
            // 
            // checkForUpdatesMenuItem
            // 
            checkForUpdatesMenuItem.Name = "checkForUpdatesMenuItem";
            checkForUpdatesMenuItem.Size = new System.Drawing.Size(213, 26);
            checkForUpdatesMenuItem.Tag = "Update Check";
            checkForUpdatesMenuItem.Text = "Check for Updates";
            checkForUpdatesMenuItem.Click += WebsiteClick;
            // 
            // toolStripSeparator6
            // 
            toolStripSeparator6.Name = "toolStripSeparator6";
            toolStripSeparator6.Size = new System.Drawing.Size(210, 6);
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new System.Drawing.Size(213, 26);
            aboutToolStripMenuItem.Text = "About...";
            aboutToolStripMenuItem.Click += AboutToolStripMenuItemClick;
            // 
            // statusInfoLabel
            // 
            statusInfoLabel.Name = "statusInfoLabel";
            statusInfoLabel.Size = new System.Drawing.Size(196, 28);
            statusInfoLabel.Text = "GitForce - A Git Visual Client";
            // 
            // statusStrip
            // 
            statusStrip.AutoSize = false;
            statusStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { statusInfoLabel });
            statusStrip.Location = new System.Drawing.Point(0, 738);
            statusStrip.Name = "statusStrip";
            statusStrip.Padding = new System.Windows.Forms.Padding(1, 0, 19, 0);
            statusStrip.Size = new System.Drawing.Size(1045, 34);
            statusStrip.TabIndex = 1;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            splitContainer1.Location = new System.Drawing.Point(0, 57);
            splitContainer1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(listStatus);
            splitContainer1.Panel2.Controls.Add(cmdBox);
            splitContainer1.Size = new System.Drawing.Size(1045, 681);
            splitContainer1.SplitterDistance = 386;
            splitContainer1.SplitterWidth = 6;
            splitContainer1.TabIndex = 2;
            // 
            // splitContainer2
            // 
            splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            splitContainer2.Location = new System.Drawing.Point(0, 0);
            splitContainer2.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(rightTabControl);
            splitContainer2.Size = new System.Drawing.Size(1045, 386);
            splitContainer2.SplitterDistance = 497;
            splitContainer2.SplitterWidth = 5;
            splitContainer2.TabIndex = 0;
            // 
            // rightTabControl
            // 
            rightTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            rightTabControl.Location = new System.Drawing.Point(0, 0);
            rightTabControl.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            rightTabControl.Name = "rightTabControl";
            rightTabControl.SelectedIndex = 0;
            rightTabControl.ShowToolTips = true;
            rightTabControl.Size = new System.Drawing.Size(543, 386);
            rightTabControl.TabIndex = 0;
            rightTabControl.SelectedIndexChanged += RightPanelSelectionEvent;
            // 
            // listStatus
            // 
            listStatus.ContextMenuStrip = menuStatus;
            listStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            listStatus.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            listStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            listStatus.FormattingEnabled = true;
            listStatus.IntegralHeight = false;
            listStatus.ItemHeight = 15;
            listStatus.Location = new System.Drawing.Point(0, 0);
            listStatus.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            listStatus.Name = "listStatus";
            listStatus.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            listStatus.Size = new System.Drawing.Size(1045, 262);
            listStatus.TabIndex = 0;
            listStatus.DrawItem += ListStatusDrawItem;
            // 
            // menuStatus
            // 
            menuStatus.ImageScalingSize = new System.Drawing.Size(20, 20);
            menuStatus.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { menuStatusCopy, menuStatusSelectAll, toolStripSeparator1, menuStatusClear });
            menuStatus.Name = "menuStatus";
            menuStatus.Size = new System.Drawing.Size(141, 82);
            // 
            // menuStatusCopy
            // 
            menuStatusCopy.Name = "menuStatusCopy";
            menuStatusCopy.Size = new System.Drawing.Size(140, 24);
            menuStatusCopy.Text = "Copy";
            menuStatusCopy.Click += MenuStatusCopyClick;
            // 
            // menuStatusSelectAll
            // 
            menuStatusSelectAll.Name = "menuStatusSelectAll";
            menuStatusSelectAll.Size = new System.Drawing.Size(140, 24);
            menuStatusSelectAll.Text = "Select All";
            menuStatusSelectAll.Click += MenuStatusSelectAllClick;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new System.Drawing.Size(137, 6);
            // 
            // menuStatusClear
            // 
            menuStatusClear.Name = "menuStatusClear";
            menuStatusClear.Size = new System.Drawing.Size(140, 24);
            menuStatusClear.Text = "Clear";
            menuStatusClear.Click += MenuSelectClearClick;
            // 
            // cmdBox
            // 
            cmdBox.AcceptsReturn = true;
            cmdBox.BackColor = System.Drawing.SystemColors.Info;
            cmdBox.Dock = System.Windows.Forms.DockStyle.Bottom;
            cmdBox.Font = new System.Drawing.Font("Segoe UI Symbol", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            cmdBox.Location = new System.Drawing.Point(0, 262);
            cmdBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            cmdBox.Name = "cmdBox";
            cmdBox.Size = new System.Drawing.Size(1045, 27);
            cmdBox.TabIndex = 1;
            cmdBox.TextReady += CmdBoxTextReady;
            // 
            // toolStrip
            // 
            toolStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { btAdd, btUpdate, btUpdateAll, btRevert, btDelete, btDeleteFs, btEdit, toolStripSeparator8, btPull, btPush, toolStripSeparator7, btChangelists, btSubmitted, btBranches, btRepos, toolStripSeparator2, btCancelOperation, btOptions, btSsh, btNewVersion, btHttps });
            toolStrip.Location = new System.Drawing.Point(0, 30);
            toolStrip.Name = "toolStrip";
            toolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            toolStrip.Size = new System.Drawing.Size(1045, 27);
            toolStrip.TabIndex = 3;
            toolStrip.Text = "toolStrip1";
            // 
            // btAdd
            // 
            btAdd.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            btAdd.Enabled = false;
            btAdd.Image = (System.Drawing.Image)resources.GetObject("btAdd.Image");
            btAdd.ImageTransparentColor = System.Drawing.Color.Magenta;
            btAdd.Name = "btAdd";
            btAdd.Size = new System.Drawing.Size(29, 24);
            btAdd.Text = "Add file to Git";
            // 
            // btUpdate
            // 
            btUpdate.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            btUpdate.Enabled = false;
            btUpdate.Image = (System.Drawing.Image)resources.GetObject("btUpdate.Image");
            btUpdate.ImageTransparentColor = System.Drawing.Color.Magenta;
            btUpdate.Name = "btUpdate";
            btUpdate.Size = new System.Drawing.Size(29, 24);
            btUpdate.Text = "Update Changelist";
            // 
            // btUpdateAll
            // 
            btUpdateAll.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            btUpdateAll.Enabled = false;
            btUpdateAll.Image = (System.Drawing.Image)resources.GetObject("btUpdateAll.Image");
            btUpdateAll.ImageTransparentColor = System.Drawing.Color.Magenta;
            btUpdateAll.Name = "btUpdateAll";
            btUpdateAll.Size = new System.Drawing.Size(29, 24);
            btUpdateAll.Text = "Update Changelist with All Files";
            // 
            // btRevert
            // 
            btRevert.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            btRevert.Enabled = false;
            btRevert.Image = (System.Drawing.Image)resources.GetObject("btRevert.Image");
            btRevert.ImageTransparentColor = System.Drawing.Color.Magenta;
            btRevert.Name = "btRevert";
            btRevert.Size = new System.Drawing.Size(29, 24);
            btRevert.Text = "Revert";
            // 
            // btDelete
            // 
            btDelete.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            btDelete.Enabled = false;
            btDelete.Image = (System.Drawing.Image)resources.GetObject("btDelete.Image");
            btDelete.ImageTransparentColor = System.Drawing.Color.Magenta;
            btDelete.Name = "btDelete";
            btDelete.Size = new System.Drawing.Size(29, 24);
            btDelete.Text = "Open for Delete";
            // 
            // btDeleteFs
            // 
            btDeleteFs.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            btDeleteFs.Enabled = false;
            btDeleteFs.Image = (System.Drawing.Image)resources.GetObject("btDeleteFs.Image");
            btDeleteFs.ImageTransparentColor = System.Drawing.Color.Magenta;
            btDeleteFs.Name = "btDeleteFs";
            btDeleteFs.Size = new System.Drawing.Size(29, 24);
            btDeleteFs.Text = "Delete from File System";
            // 
            // btEdit
            // 
            btEdit.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            btEdit.Enabled = false;
            btEdit.Image = (System.Drawing.Image)resources.GetObject("btEdit.Image");
            btEdit.ImageTransparentColor = System.Drawing.Color.Magenta;
            btEdit.Name = "btEdit";
            btEdit.Size = new System.Drawing.Size(29, 24);
            btEdit.Text = "Edit using default editor";
            // 
            // toolStripSeparator8
            // 
            toolStripSeparator8.Name = "toolStripSeparator8";
            toolStripSeparator8.Size = new System.Drawing.Size(6, 27);
            // 
            // btPull
            // 
            btPull.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            btPull.Enabled = false;
            btPull.Image = (System.Drawing.Image)resources.GetObject("btPull.Image");
            btPull.ImageTransparentColor = System.Drawing.Color.Magenta;
            btPull.Name = "btPull";
            btPull.Size = new System.Drawing.Size(29, 24);
            btPull.Text = "Pull from Remote Repo + Ctrl to All";
            btPull.Click += MenuRepoPull;
            // 
            // btPush
            // 
            btPush.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            btPush.Enabled = false;
            btPush.Image = (System.Drawing.Image)resources.GetObject("btPush.Image");
            btPush.ImageTransparentColor = System.Drawing.Color.Magenta;
            btPush.Name = "btPush";
            btPush.Size = new System.Drawing.Size(29, 24);
            btPush.Text = "Push to Remote Repo + Ctrl to All";
            btPush.Click += MenuRepoPush;
            // 
            // toolStripSeparator7
            // 
            toolStripSeparator7.Name = "toolStripSeparator7";
            toolStripSeparator7.Size = new System.Drawing.Size(6, 27);
            // 
            // btChangelists
            // 
            btChangelists.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            btChangelists.Image = (System.Drawing.Image)resources.GetObject("btChangelists.Image");
            btChangelists.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            btChangelists.ImageTransparentColor = System.Drawing.Color.Black;
            btChangelists.Name = "btChangelists";
            btChangelists.Size = new System.Drawing.Size(29, 24);
            btChangelists.Tag = "Commits";
            btChangelists.Text = "View Pending Changelists (F6)";
            btChangelists.Click += RightPanelSelectionEvent;
            // 
            // btSubmitted
            // 
            btSubmitted.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            btSubmitted.Image = (System.Drawing.Image)resources.GetObject("btSubmitted.Image");
            btSubmitted.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            btSubmitted.ImageTransparentColor = System.Drawing.Color.Black;
            btSubmitted.Name = "btSubmitted";
            btSubmitted.Size = new System.Drawing.Size(29, 24);
            btSubmitted.Tag = "Revisions";
            btSubmitted.Text = "View Submitted Changelists (F7)";
            btSubmitted.Click += RightPanelSelectionEvent;
            // 
            // btBranches
            // 
            btBranches.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            btBranches.Image = (System.Drawing.Image)resources.GetObject("btBranches.Image");
            btBranches.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            btBranches.ImageTransparentColor = System.Drawing.Color.Black;
            btBranches.Name = "btBranches";
            btBranches.Size = new System.Drawing.Size(29, 24);
            btBranches.Tag = "Branches";
            btBranches.Text = "View Branches (F8)";
            btBranches.Click += RightPanelSelectionEvent;
            // 
            // btRepos
            // 
            btRepos.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            btRepos.Image = (System.Drawing.Image)resources.GetObject("btRepos.Image");
            btRepos.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            btRepos.ImageTransparentColor = System.Drawing.Color.Black;
            btRepos.Name = "btRepos";
            btRepos.Size = new System.Drawing.Size(29, 24);
            btRepos.Tag = "Repos";
            btRepos.Text = "View Repositories (F10)";
            btRepos.Click += RightPanelSelectionEvent;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new System.Drawing.Size(6, 27);
            // 
            // btCancelOperation
            // 
            btCancelOperation.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            btCancelOperation.Enabled = false;
            btCancelOperation.Image = (System.Drawing.Image)resources.GetObject("btCancelOperation.Image");
            btCancelOperation.ImageTransparentColor = System.Drawing.Color.Magenta;
            btCancelOperation.Name = "btCancelOperation";
            btCancelOperation.Size = new System.Drawing.Size(29, 24);
            btCancelOperation.Text = "Cancel Operation";
            btCancelOperation.Click += BtCancelOperationClick;
            // 
            // btOptions
            // 
            btOptions.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            btOptions.Image = (System.Drawing.Image)resources.GetObject("btOptions.Image");
            btOptions.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            btOptions.ImageTransparentColor = System.Drawing.Color.Black;
            btOptions.Name = "btOptions";
            btOptions.Size = new System.Drawing.Size(29, 24);
            btOptions.Text = "Set GitForce Options";
            btOptions.Click += MenuOptions;
            // 
            // btSsh
            // 
            btSsh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            btSsh.Enabled = false;
            btSsh.Image = (System.Drawing.Image)resources.GetObject("btSsh.Image");
            btSsh.ImageTransparentColor = System.Drawing.Color.Magenta;
            btSsh.Name = "btSsh";
            btSsh.Size = new System.Drawing.Size(29, 24);
            btSsh.Text = "Manage SSH Keys";
            btSsh.Click += MenuMainManageKeysClick;
            // 
            // btNewVersion
            // 
            btNewVersion.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            btNewVersion.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            btNewVersion.Image = (System.Drawing.Image)resources.GetObject("btNewVersion.Image");
            btNewVersion.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            btNewVersion.ImageTransparentColor = System.Drawing.Color.Transparent;
            btNewVersion.Name = "btNewVersion";
            btNewVersion.Size = new System.Drawing.Size(44, 24);
            btNewVersion.Text = "A new version of GitForce is available!";
            btNewVersion.Visible = false;
            btNewVersion.Click += NewVersionButtonClick;
            // 
            // btHttps
            // 
            btHttps.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            btHttps.Image = (System.Drawing.Image)resources.GetObject("btHttps.Image");
            btHttps.ImageTransparentColor = System.Drawing.Color.Magenta;
            btHttps.Name = "btHttps";
            btHttps.Size = new System.Drawing.Size(29, 24);
            btHttps.ToolTipText = "Manage HTTPS Passwords";
            btHttps.Click += MenuMainManageHttpsClick;
            // 
            // timerBusy
            // 
            timerBusy.Interval = 200;
            timerBusy.Tick += TimerBusyTick;
            // 
            // loadWk
            // 
            loadWk.DefaultExt = "*.giw";
            loadWk.Filter = "Workspace files (*.giw)|*.giw|All files (*.*)|*.*";
            loadWk.Title = "Load Workspace";
            // 
            // saveWk
            // 
            saveWk.DefaultExt = "*.giw";
            saveWk.Filter = "Workspace files (*.giw)|*.giw|All files (*.*)|*.*";
            saveWk.Title = "Save Workspace As";
            // 
            // openTools
            // 
            openTools.DefaultExt = "*.xml";
            openTools.Filter = "Custom tools files (*.xml)|*.xml|All files (*.*)|*.*";
            openTools.Title = "Read Custom Tools from a File";
            // 
            // saveTools
            // 
            saveTools.DefaultExt = "*.xml";
            saveTools.Filter = "Custom tools files (*.xml)|*.xml|All files (*.*)|*.*";
            saveTools.Title = "Save Custom Tools to a File";
            // 
            // createWk
            // 
            createWk.DefaultExt = "*.giw";
            createWk.Filter = "Workspace files (*.giw)|*.giw|All files (*.*)|*.*";
            createWk.Title = "Create Workspace At";
            // 
            // importWk
            // 
            importWk.DefaultExt = "*.giw";
            importWk.Filter = "Workspace files (*.giw)|*.giw|All files (*.*)|*.*";
            importWk.Title = "Import Workspace";
            // 
            // FormMain
            // 
            AllowDrop = true;
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1045, 772);
            Controls.Add(splitContainer1);
            Controls.Add(toolStrip);
            Controls.Add(statusStrip);
            Controls.Add(menuMain);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Location = new System.Drawing.Point(300, 300);
            MainMenuStrip = menuMain;
            Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            Name = "FormMain";
            StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            Text = "GitForce";
            FormClosing += FormMainFormClosing;
            DragDrop += FormMainDragDrop;
            DragEnter += FormMainDragEnter;
            menuMain.ResumeLayout(false);
            menuMain.PerformLayout();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            menuStatus.ResumeLayout(false);
            toolStrip.ResumeLayout(false);
            toolStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuMain;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripMenuItem menuMainFile;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.ContextMenuStrip menuStatus;
        private System.Windows.Forms.ToolStripMenuItem menuStatusCopy;
        private System.Windows.Forms.ToolStripMenuItem menuStatusSelectAll;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem menuStatusClear;
        private System.Windows.Forms.ToolStripMenuItem menuMainEdit;
        private System.Windows.Forms.ToolStripMenuItem menuMainView;
        private System.Windows.Forms.ToolStripMenuItem menuViewLogWindow;
        private System.Windows.Forms.ToolStripMenuItem menuMainSettings;
        private System.Windows.Forms.ToolStripMenuItem menuMainChangelist;
        private System.Windows.Forms.ToolStripMenuItem menuMainBranch;
        private System.Windows.Forms.ToolStripMenuItem menuMainRepository;
        private System.Windows.Forms.ToolStripMenuItem menuMainHelp;
        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton btChangelists;
        private System.Windows.Forms.ToolStripButton btSubmitted;
        private System.Windows.Forms.ToolStripButton btBranches;
        private System.Windows.Forms.ToolStripButton btRepos;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton btCancelOperation;
        private System.Windows.Forms.ToolStripButton btOptions;
        private System.Windows.Forms.ToolStripButton btSsh;
        private System.Windows.Forms.ToolStripButton btHttps;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem menuMainSelectAll;
        private System.Windows.Forms.ToolStripMenuItem menuMainPendingChanges;
        private System.Windows.Forms.ToolStripMenuItem menuMainSubmittedChanges;
        private System.Windows.Forms.ToolStripMenuItem menuMainBranches;
        private System.Windows.Forms.ToolStripMenuItem menuMainRepos;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem menuView0;
        private System.Windows.Forms.ToolStripMenuItem menuView1;
        private System.Windows.Forms.ToolStripMenuItem menuView2;
        private System.Windows.Forms.ToolStripMenuItem menuView3;
        private System.Windows.Forms.ToolStripMenuItem menuView4;
        private System.Windows.Forms.ToolStripMenuItem menuView5;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripMenuItem menuMainRefresh;
        private System.Windows.Forms.ToolStripMenuItem menuMainOptions;
        private System.Windows.Forms.ToolStripMenuItem menuMainManageKeys;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripMenuItem menuMainSwitchRemoteRepo;
        private System.Windows.Forms.ToolStripMenuItem xToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem menuMainEditRemoteRepo;
        private System.Windows.Forms.ToolStripMenuItem menuMainPullFromRemote;
        private System.Windows.Forms.ToolStripMenuItem menuMainPushToRemote;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
        private System.Windows.Forms.ToolStripStatusLabel statusInfoLabel;
        private System.Windows.Forms.Timer timerBusy;
        private System.Windows.Forms.ToolStripButton btAdd;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
        private System.Windows.Forms.ToolStripButton btUpdate;
        private System.Windows.Forms.ToolStripButton btDelete;
        private System.Windows.Forms.ToolStripButton btDeleteFs;
        private System.Windows.Forms.ToolStripButton btEdit;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
        private System.Windows.Forms.ToolStripButton btPull;
        private System.Windows.Forms.ToolStripButton btPush;
        private System.Windows.Forms.ToolStripButton btUpdateAll;
        private System.Windows.Forms.ToolStripButton btRevert;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator9;
        private System.Windows.Forms.ToolStripMenuItem menuMainStash;
        private System.Windows.Forms.ToolStripMenuItem menuMainUnstash;
        private System.Windows.Forms.OpenFileDialog loadWk;
        private System.Windows.Forms.SaveFileDialog saveWk;
        private System.Windows.Forms.ToolStripMenuItem menuMainTools;
        private System.Windows.Forms.OpenFileDialog openTools;
        private System.Windows.Forms.SaveFileDialog saveTools;
        private System.Windows.Forms.ToolStripMenuItem gettingStartedMenuItem;
        private System.Windows.Forms.ToolStripMenuItem menuMainFetchFromRemote;
        private System.Windows.Forms.ToolStripMenuItem menuViewCommandLine;
        protected TextBoxEx cmdBox;
        private System.Windows.Forms.ToolStripButton btNewVersion;
        private System.Windows.Forms.ToolStripMenuItem checkForUpdatesMenuItem;
        private TabEx rightTabControl;
        private System.Windows.Forms.ToolStripMenuItem homeMenuItem;
        private System.Windows.Forms.SaveFileDialog createWk;
        private ListBoxEx listStatus;
        private System.Windows.Forms.OpenFileDialog importWk;
        private System.Windows.Forms.ToolStripMenuItem menuMainManageHttps;
    }
}

