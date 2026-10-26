namespace GitForce
{
    partial class FormAbout
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
            btOK = new System.Windows.Forms.Button();
            label1 = new System.Windows.Forms.Label();
            labelVersion = new System.Windows.Forms.Label();
            textLic = new System.Windows.Forms.TextBox();
            labelCopyright = new System.Windows.Forms.Label();
            linkGPL = new System.Windows.Forms.LinkLabel();
            labelBuild = new System.Windows.Forms.Label();
            groupBox1 = new System.Windows.Forms.GroupBox();
            linkBaltazarStudios = new System.Windows.Forms.LinkLabel();
            label5 = new System.Windows.Forms.Label();
            btCopyEmail = new System.Windows.Forms.Button();
            label2 = new System.Windows.Forms.Label();
            labelNewVersionAvailable = new System.Windows.Forms.Label();
            btDownload = new System.Windows.Forms.Button();
            label3 = new System.Windows.Forms.Label();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // btOK
            // 
            btOK.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            btOK.Location = new System.Drawing.Point(479, 460);
            btOK.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btOK.Name = "btOK";
            btOK.Size = new System.Drawing.Size(100, 35);
            btOK.TabIndex = 0;
            btOK.Text = "OK";
            btOK.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            label1.Location = new System.Drawing.Point(16, 14);
            label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(170, 20);
            label1.TabIndex = 2;
            label1.Text = "A GIT Visual Client";
            // 
            // labelVersion
            // 
            labelVersion.AutoSize = true;
            labelVersion.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            labelVersion.Location = new System.Drawing.Point(16, 57);
            labelVersion.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelVersion.Name = "labelVersion";
            labelVersion.Size = new System.Drawing.Size(68, 17);
            labelVersion.TabIndex = 3;
            labelVersion.Text = "Version:";
            // 
            // textLic
            // 
            textLic.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            textLic.Location = new System.Drawing.Point(16, 252);
            textLic.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            textLic.Multiline = true;
            textLic.Name = "textLic";
            textLic.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            textLic.Size = new System.Drawing.Size(561, 196);
            textLic.TabIndex = 4;
            // 
            // labelCopyright
            // 
            labelCopyright.Location = new System.Drawing.Point(8, 31);
            labelCopyright.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelCopyright.Name = "labelCopyright";
            labelCopyright.Size = new System.Drawing.Size(412, 20);
            labelCopyright.TabIndex = 5;
            labelCopyright.Text = "Written by Goran Devic";
            // 
            // linkGPL
            // 
            linkGPL.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            linkGPL.AutoSize = true;
            linkGPL.Location = new System.Drawing.Point(541, 18);
            linkGPL.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            linkGPL.Name = "linkGPL";
            linkGPL.Size = new System.Drawing.Size(48, 20);
            linkGPL.TabIndex = 6;
            linkGPL.TabStop = true;
            linkGPL.Tag = "";
            linkGPL.Text = "GPLv3";
            linkGPL.LinkClicked += LinkGplClicked;
            // 
            // labelBuild
            // 
            labelBuild.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            labelBuild.AutoSize = true;
            labelBuild.Location = new System.Drawing.Point(316, 57);
            labelBuild.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelBuild.Name = "labelBuild";
            labelBuild.Size = new System.Drawing.Size(43, 20);
            labelBuild.TabIndex = 7;
            labelBuild.Text = "Build";
            // 
            // groupBox1
            // 
            groupBox1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(linkBaltazarStudios);
            groupBox1.Controls.Add(label5);
            groupBox1.Controls.Add(btCopyEmail);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(labelCopyright);
            groupBox1.Location = new System.Drawing.Point(16, 95);
            groupBox1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            groupBox1.Size = new System.Drawing.Size(563, 148);
            groupBox1.TabIndex = 8;
            groupBox1.TabStop = false;
            groupBox1.Text = "Contact information";
            // 
            // linkBaltazarStudios
            // 
            linkBaltazarStudios.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            linkBaltazarStudios.AutoSize = true;
            linkBaltazarStudios.Location = new System.Drawing.Point(385, 31);
            linkBaltazarStudios.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            linkBaltazarStudios.Name = "linkBaltazarStudios";
            linkBaltazarStudios.Size = new System.Drawing.Size(178, 20);
            linkBaltazarStudios.TabIndex = 13;
            linkBaltazarStudios.TabStop = true;
            linkBaltazarStudios.Text = "www.baltazarstudios.com";
            linkBaltazarStudios.LinkClicked += LinkBaltazarStudiosLinkClicked;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            label5.Location = new System.Drawing.Point(8, 101);
            label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(184, 17);
            label5.TabIndex = 12;
            label5.Text = "https://github.com/philgerm57/GitForce/issues";
            // 
            // btCopyEmail
            // 
            btCopyEmail.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btCopyEmail.Location = new System.Drawing.Point(463, 84);
            btCopyEmail.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btCopyEmail.Name = "btCopyEmail";
            btCopyEmail.Size = new System.Drawing.Size(92, 35);
            btCopyEmail.TabIndex = 8;
            btCopyEmail.Text = "Open";
            btCopyEmail.UseVisualStyleBackColor = true;
            btCopyEmail.Click += BtCopyEmailClick;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(8, 76);
            label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(376, 20);
            label2.TabIndex = 6;
            label2.Text = "For comments, suggestions or bug reports, open an issue on GitHub:";
            // 
            // labelNewVersionAvailable
            // 
            labelNewVersionAvailable.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            labelNewVersionAvailable.AutoSize = true;
            labelNewVersionAvailable.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            labelNewVersionAvailable.ForeColor = System.Drawing.Color.Brown;
            labelNewVersionAvailable.Location = new System.Drawing.Point(16, 468);
            labelNewVersionAvailable.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelNewVersionAvailable.Name = "labelNewVersionAvailable";
            labelNewVersionAvailable.Size = new System.Drawing.Size(286, 17);
            labelNewVersionAvailable.TabIndex = 0;
            labelNewVersionAvailable.Text = "A new version of GitForce is available!";
            // 
            // btDownload
            // 
            btDownload.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btDownload.Location = new System.Drawing.Point(375, 460);
            btDownload.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btDownload.Name = "btDownload";
            btDownload.Size = new System.Drawing.Size(92, 35);
            btDownload.TabIndex = 1;
            btDownload.Text = "Download";
            btDownload.UseVisualStyleBackColor = true;
            btDownload.Click += DownloadClick;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(8, 51);
            label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(217, 20);
            label3.TabIndex = 14;
            label3.Text = "Modified by Germain Philippe";
            // 
            // FormAbout
            // 
            AcceptButton = btOK;
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = btOK;
            ClientSize = new System.Drawing.Size(595, 514);
            Controls.Add(btDownload);
            Controls.Add(labelNewVersionAvailable);
            Controls.Add(groupBox1);
            Controls.Add(labelBuild);
            Controls.Add(textLic);
            Controls.Add(labelVersion);
            Controls.Add(label1);
            Controls.Add(btOK);
            Controls.Add(linkGPL);
            Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new System.Drawing.Size(570, 455);
            Name = "FormAbout";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            Text = "About GitForce";
            FormClosing += FormAboutFormClosing;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btOK;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label labelVersion;
        private System.Windows.Forms.TextBox textLic;
        private System.Windows.Forms.Label labelCopyright;
        private System.Windows.Forms.LinkLabel linkGPL;
        private System.Windows.Forms.Label labelBuild;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btCopyEmail;
        private System.Windows.Forms.Button btDownload;
        private System.Windows.Forms.Label labelNewVersionAvailable;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.LinkLabel linkBaltazarStudios;
        private System.Windows.Forms.Label label3;
    }
}