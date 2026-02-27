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
            txtPrompt = new TextBox();
            btnSelectFile = new Button();
            lblSelectedPath = new Label();
            btnSend = new Button();
            rtbOutput = new RichTextBox();
            btnSelectFolder = new Button();
            btnClearChat = new Button();
            SuspendLayout();
            // 
            // txtPrompt
            // 
            txtPrompt.Location = new Point(12, 392);
            txtPrompt.Multiline = true;
            txtPrompt.Name = "txtPrompt";
            txtPrompt.Size = new Size(674, 62);
            txtPrompt.TabIndex = 0;
            // 
            // btnSelectFile
            // 
            btnSelectFile.Location = new Point(12, 12);
            btnSelectFile.Name = "btnSelectFile";
            btnSelectFile.Size = new Size(83, 23);
            btnSelectFile.TabIndex = 1;
            btnSelectFile.Text = "Select file";
            btnSelectFile.UseVisualStyleBackColor = true;
            btnSelectFile.Click += btnSelectFile_Click;
            // 
            // lblSelectedPath
            // 
            lblSelectedPath.AutoSize = true;
            lblSelectedPath.Location = new Point(317, 16);
            lblSelectedPath.Name = "lblSelectedPath";
            lblSelectedPath.Size = new Size(51, 15);
            lblSelectedPath.TabIndex = 2;
            lblSelectedPath.Text = "Selected";
            // 
            // btnSend
            // 
            btnSend.Location = new Point(692, 431);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(75, 23);
            btnSend.TabIndex = 3;
            btnSend.Text = "Send";
            btnSend.UseVisualStyleBackColor = true;
            btnSend.Click += btnSend_Click;
            // 
            // rtbOutput
            // 
            rtbOutput.Location = new Point(12, 41);
            rtbOutput.Name = "rtbOutput";
            rtbOutput.Size = new Size(755, 345);
            rtbOutput.TabIndex = 4;
            rtbOutput.Text = "";
            // 
            // btnSelectFolder
            // 
            btnSelectFolder.Location = new Point(101, 12);
            btnSelectFolder.Name = "btnSelectFolder";
            btnSelectFolder.Size = new Size(83, 23);
            btnSelectFolder.TabIndex = 5;
            btnSelectFolder.Text = "Select folder";
            btnSelectFolder.UseVisualStyleBackColor = true;
            btnSelectFolder.Click += btnSelectFolder_Click;
            // 
            // btnClearChat
            // 
            btnClearChat.Location = new Point(692, 402);
            btnClearChat.Name = "btnClearChat";
            btnClearChat.Size = new Size(75, 23);
            btnClearChat.TabIndex = 6;
            btnClearChat.Text = "Clear chat";
            btnClearChat.UseVisualStyleBackColor = true;
            btnClearChat.Click += btnClearChat_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(784, 466);
            Controls.Add(btnClearChat);
            Controls.Add(btnSelectFolder);
            Controls.Add(rtbOutput);
            Controls.Add(btnSend);
            Controls.Add(lblSelectedPath);
            Controls.Add(btnSelectFile);
            Controls.Add(txtPrompt);
            Name = "Form1";
            Text = "Form1";
            FormClosing += Form1_FormClosing;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtPrompt;
        private Button btnSelectFile;
        private Label lblSelectedPath;
        private Button btnSend;
        private RichTextBox rtbOutput;
        private Button btnSelectFolder;
        private Button btnClearChat;
    }
}
