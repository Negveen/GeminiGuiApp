namespace GeminiGuiApp
{
    partial class SettingsForm
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
            btnClearCache = new Button();
            label1 = new Label();
            chkShiftEnter = new CheckBox();
            SuspendLayout();
            // 
            // btnClearCache
            // 
            btnClearCache.BackColor = Color.Firebrick;
            btnClearCache.ForeColor = SystemColors.ButtonHighlight;
            btnClearCache.Location = new Point(50, 79);
            btnClearCache.Name = "btnClearCache";
            btnClearCache.Size = new Size(176, 23);
            btnClearCache.TabIndex = 0;
            btnClearCache.Text = "Очистить кэш CLI";
            btnClearCache.UseVisualStyleBackColor = false;
            btnClearCache.Click += btnClearCache_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(88, 105);
            label1.Name = "label1";
            label1.Size = new Size(102, 15);
            label1.TabIndex = 1;
            label1.Text = "Остальное скоро";
            // 
            // chkShiftEnter
            // 
            chkShiftEnter.AutoSize = true;
            chkShiftEnter.Location = new Point(12, 25);
            chkShiftEnter.Name = "chkShiftEnter";
            chkShiftEnter.Size = new Size(236, 19);
            chkShiftEnter.TabIndex = 2;
            chkShiftEnter.Text = "Отправлять сообщения по Shift+Enter";
            chkShiftEnter.UseVisualStyleBackColor = true;
            chkShiftEnter.CheckedChanged += chkShiftEnter_CheckedChanged;
            // 
            // SettingsForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(284, 129);
            Controls.Add(chkShiftEnter);
            Controls.Add(label1);
            Controls.Add(btnClearCache);
            Name = "SettingsForm";
            Text = "SettingsForm";
            Load += SettingsForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnClearCache;
        private Label label1;
        private CheckBox chkShiftEnter;
    }
}