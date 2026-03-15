namespace GeminiGuiApp
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            txtPrompt = new TextBox();
            btnSelectFile = new Button();
            btnSend = new Button();
            rtbOutput = new RichTextBox();
            btnSelectFolder = new Button();
            btnSettings = new Button();
            splitContainer1 = new SplitContainer();
            lstChats = new ListBox();
            ctxChatMenu = new ContextMenuStrip(components);
            tsmPin = new ToolStripMenuItem();
            tsmRename = new ToolStripMenuItem();
            tsmDelete = new ToolStripMenuItem();
            btnNewChat = new Button();
            panel1 = new Panel();
            lblContextUsage = new Label();
            txtSelectedPath = new TextBox();
            btnResetPath = new Button();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ctxChatMenu.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // txtPrompt
            // 
            txtPrompt.Location = new Point(111, 6);
            txtPrompt.Multiline = true;
            txtPrompt.Name = "txtPrompt";
            txtPrompt.Size = new Size(674, 62);
            txtPrompt.TabIndex = 0;
            txtPrompt.KeyDown += txtPrompt_KeyDown;
            // 
            // btnSelectFile
            // 
            btnSelectFile.Location = new Point(12, 6);
            btnSelectFile.Name = "btnSelectFile";
            btnSelectFile.Size = new Size(83, 23);
            btnSelectFile.TabIndex = 1;
            btnSelectFile.Text = "Select file";
            btnSelectFile.UseVisualStyleBackColor = true;
            btnSelectFile.Click += btnSelectFile_Click;
            // 
            // btnSend
            // 
            btnSend.Location = new Point(791, 6);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(75, 23);
            btnSend.TabIndex = 3;
            btnSend.Text = "Send";
            btnSend.UseVisualStyleBackColor = true;
            btnSend.Click += btnSend_Click;
            // 
            // rtbOutput
            // 
            rtbOutput.Dock = DockStyle.Fill;
            rtbOutput.Location = new Point(0, 0);
            rtbOutput.Name = "rtbOutput";
            rtbOutput.ReadOnly = true;
            rtbOutput.Size = new Size(926, 647);
            rtbOutput.TabIndex = 4;
            rtbOutput.Text = "";
            // 
            // btnSelectFolder
            // 
            btnSelectFolder.Location = new Point(12, 35);
            btnSelectFolder.Name = "btnSelectFolder";
            btnSelectFolder.Size = new Size(83, 23);
            btnSelectFolder.TabIndex = 5;
            btnSelectFolder.Text = "Select folder";
            btnSelectFolder.UseVisualStyleBackColor = true;
            btnSelectFolder.Click += btnSelectFolder_Click;
            // 
            // btnSettings
            // 
            btnSettings.Dock = DockStyle.Bottom;
            btnSettings.Location = new Point(0, 740);
            btnSettings.Name = "btnSettings";
            btnSettings.Size = new Size(191, 23);
            btnSettings.TabIndex = 9;
            btnSettings.Text = "Settings";
            btnSettings.UseVisualStyleBackColor = true;
            btnSettings.Click += btnSettings_Click;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(btnSettings);
            splitContainer1.Panel1.Controls.Add(lstChats);
            splitContainer1.Panel1.Controls.Add(btnNewChat);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(rtbOutput);
            splitContainer1.Panel2.Controls.Add(panel1);
            splitContainer1.Size = new Size(1121, 763);
            splitContainer1.SplitterDistance = 191;
            splitContainer1.TabIndex = 10;
            // 
            // lstChats
            // 
            lstChats.ContextMenuStrip = ctxChatMenu;
            lstChats.Dock = DockStyle.Fill;
            lstChats.FormattingEnabled = true;
            lstChats.Location = new Point(0, 23);
            lstChats.Name = "lstChats";
            lstChats.Size = new Size(191, 740);
            lstChats.TabIndex = 1;
            lstChats.SelectedIndexChanged += lstChats_SelectedIndexChanged;
            lstChats.MouseDown += lstChats_MouseDown;
            lstChats.MouseMove += lstChats_MouseMove;
            // 
            // ctxChatMenu
            // 
            ctxChatMenu.Items.AddRange(new ToolStripItem[] { tsmPin, tsmRename, tsmDelete });
            ctxChatMenu.Name = "ctxChatMenu";
            ctxChatMenu.Size = new Size(216, 70);
            // 
            // tsmPin
            // 
            tsmPin.Name = "tsmPin";
            tsmPin.Size = new Size(215, 22);
            tsmPin.Text = "📌 Закрепить / Открепить";
            tsmPin.Click += tsmPin_Click;
            // 
            // tsmRename
            // 
            tsmRename.Name = "tsmRename";
            tsmRename.Size = new Size(215, 22);
            tsmRename.Text = "✏️ Переименовать";
            tsmRename.Click += tsmRename_Click;
            // 
            // tsmDelete
            // 
            tsmDelete.Name = "tsmDelete";
            tsmDelete.Size = new Size(215, 22);
            tsmDelete.Text = "🗑️ Удалить";
            tsmDelete.Click += tsmDelete_Click;
            // 
            // btnNewChat
            // 
            btnNewChat.Dock = DockStyle.Top;
            btnNewChat.Location = new Point(0, 0);
            btnNewChat.Name = "btnNewChat";
            btnNewChat.Size = new Size(191, 23);
            btnNewChat.TabIndex = 0;
            btnNewChat.Text = "+ New chat";
            btnNewChat.UseVisualStyleBackColor = true;
            btnNewChat.Click += btnNewChat_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(lblContextUsage);
            panel1.Controls.Add(txtSelectedPath);
            panel1.Controls.Add(btnResetPath);
            panel1.Controls.Add(txtPrompt);
            panel1.Controls.Add(btnSelectFile);
            panel1.Controls.Add(btnSelectFolder);
            panel1.Controls.Add(btnSend);
            panel1.Dock = DockStyle.Bottom;
            panel1.Location = new Point(0, 647);
            panel1.Name = "panel1";
            panel1.Size = new Size(926, 116);
            panel1.TabIndex = 1;
            // 
            // lblContextUsage
            // 
            lblContextUsage.AutoSize = true;
            lblContextUsage.Location = new Point(791, 32);
            lblContextUsage.Name = "lblContextUsage";
            lblContextUsage.Size = new Size(77, 15);
            lblContextUsage.TabIndex = 8;
            lblContextUsage.Text = "Контекст: -%";
            // 
            // txtSelectedPath
            // 
            txtSelectedPath.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            txtSelectedPath.AutoCompleteSource = AutoCompleteSource.FileSystem;
            txtSelectedPath.Location = new Point(111, 76);
            txtSelectedPath.Name = "txtSelectedPath";
            txtSelectedPath.Size = new Size(674, 23);
            txtSelectedPath.TabIndex = 7;
            txtSelectedPath.TextChanged += txtSelectedPath_TextChanged;
            // 
            // btnResetPath
            // 
            btnResetPath.Location = new Point(76, 76);
            btnResetPath.Name = "btnResetPath";
            btnResetPath.Size = new Size(29, 23);
            btnResetPath.TabIndex = 6;
            btnResetPath.Text = "❌";
            btnResetPath.UseVisualStyleBackColor = true;
            btnResetPath.Click += btnResetPath_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1121, 763);
            Controls.Add(splitContainer1);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ctxChatMenu.ResumeLayout(false);
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TextBox txtPrompt;
        private Button btnSelectFile;
        private Button btnSend;
        private RichTextBox rtbOutput;
        private Button btnSelectFolder;
        private Button btnSettings;
        private SplitContainer splitContainer1;
        private ListBox lstChats;
        private Button btnNewChat;
        private Panel panel1;
        private ContextMenuStrip ctxChatMenu;
        private ToolStripMenuItem tsmPin;
        private ToolStripMenuItem tsmRename;
        private ToolStripMenuItem tsmDelete;
        private Button btnResetPath;
        private TextBox txtSelectedPath;
        private Label lblContextUsage;
    }
}
