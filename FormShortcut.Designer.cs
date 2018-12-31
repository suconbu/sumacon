namespace Suconbu.Sumacon
{
    partial class FormShortcut
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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.uxShortcutList = new System.Windows.Forms.ListView();
            this.uxCommandText = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Font = new System.Drawing.Font("Yu Gothic UI", 9F);
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.uxShortcutList);
            this.splitContainer1.Panel1.Font = new System.Drawing.Font("Yu Gothic UI", 9F);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.uxCommandText);
            this.splitContainer1.Panel2.Font = new System.Drawing.Font("Yu Gothic UI", 9F);
            this.splitContainer1.Size = new System.Drawing.Size(933, 562);
            this.splitContainer1.SplitterDistance = 266;
            this.splitContainer1.SplitterWidth = 5;
            this.splitContainer1.TabIndex = 0;
            // 
            // uxShortcutList
            // 
            this.uxShortcutList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxShortcutList.Font = new System.Drawing.Font("Yu Gothic UI", 9F);
            this.uxShortcutList.FullRowSelect = true;
            this.uxShortcutList.Location = new System.Drawing.Point(0, 0);
            this.uxShortcutList.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.uxShortcutList.Name = "uxShortcutList";
            this.uxShortcutList.Size = new System.Drawing.Size(933, 266);
            this.uxShortcutList.TabIndex = 7;
            this.uxShortcutList.UseCompatibleStateImageBehavior = false;
            this.uxShortcutList.View = System.Windows.Forms.View.Details;
            // 
            // uxCommandText
            // 
            this.uxCommandText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxCommandText.Font = new System.Drawing.Font("Yu Gothic UI", 9F);
            this.uxCommandText.Location = new System.Drawing.Point(0, 0);
            this.uxCommandText.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.uxCommandText.Multiline = true;
            this.uxCommandText.Name = "uxCommandText";
            this.uxCommandText.Size = new System.Drawing.Size(933, 291);
            this.uxCommandText.TabIndex = 8;
            // 
            // FormShortcut
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(933, 562);
            this.Controls.Add(this.splitContainer1);
            this.Font = new System.Drawing.Font("Yu Gothic UI", 9F);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "FormShortcut";
            this.Text = "FormShortcut";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ListView uxShortcutList;
        private System.Windows.Forms.TextBox uxCommandText;
    }
}