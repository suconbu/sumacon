namespace Suconbu.Sumacon
{
    partial class FormConsole
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
            this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.uxOutputText = new System.Windows.Forms.TextBox();
            this.uxInputText = new System.Windows.Forms.TextBox();
            this.toolStripContainer1.ContentPanel.SuspendLayout();
            this.toolStripContainer1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStripContainer1
            // 
            // 
            // toolStripContainer1.BottomToolStripPanel
            // 
            this.toolStripContainer1.BottomToolStripPanel.Font = new System.Drawing.Font("Yu Gothic UI", 9F);
            // 
            // toolStripContainer1.ContentPanel
            // 
            this.toolStripContainer1.ContentPanel.Controls.Add(this.tableLayoutPanel1);
            this.toolStripContainer1.ContentPanel.Font = new System.Drawing.Font("Yu Gothic UI", 9F);
            this.toolStripContainer1.ContentPanel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(933, 562);
            this.toolStripContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolStripContainer1.Font = new System.Drawing.Font("Yu Gothic UI", 9F);
            // 
            // toolStripContainer1.LeftToolStripPanel
            // 
            this.toolStripContainer1.LeftToolStripPanel.Font = new System.Drawing.Font("Yu Gothic UI", 9F);
            this.toolStripContainer1.Location = new System.Drawing.Point(0, 0);
            this.toolStripContainer1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.toolStripContainer1.Name = "toolStripContainer1";
            // 
            // toolStripContainer1.RightToolStripPanel
            // 
            this.toolStripContainer1.RightToolStripPanel.Font = new System.Drawing.Font("Yu Gothic UI", 9F);
            this.toolStripContainer1.Size = new System.Drawing.Size(933, 562);
            this.toolStripContainer1.TabIndex = 1;
            this.toolStripContainer1.Text = "toolStripContainer1";
            // 
            // toolStripContainer1.TopToolStripPanel
            // 
            this.toolStripContainer1.TopToolStripPanel.Font = new System.Drawing.Font("Yu Gothic UI", 9F);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.uxOutputText, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.uxInputText, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(933, 562);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // uxOutputText
            // 
            this.uxOutputText.BackColor = System.Drawing.SystemColors.Window;
            this.uxOutputText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxOutputText.Font = new System.Drawing.Font("Yu Gothic UI", 9F);
            this.uxOutputText.Location = new System.Drawing.Point(3, 4);
            this.uxOutputText.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.uxOutputText.Multiline = true;
            this.uxOutputText.Name = "uxOutputText";
            this.uxOutputText.ReadOnly = true;
            this.uxOutputText.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.uxOutputText.Size = new System.Drawing.Size(927, 525);
            this.uxOutputText.TabIndex = 1;
            // 
            // uxInputText
            // 
            this.uxInputText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxInputText.Location = new System.Drawing.Point(3, 536);
            this.uxInputText.Name = "uxInputText";
            this.uxInputText.Size = new System.Drawing.Size(927, 23);
            this.uxInputText.TabIndex = 2;
            // 
            // FormConsole
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(933, 562);
            this.Controls.Add(this.toolStripContainer1);
            this.Font = new System.Drawing.Font("Yu Gothic UI", 9F);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "FormConsole";
            this.Text = "FormConsole";
            this.toolStripContainer1.ContentPanel.ResumeLayout(false);
            this.toolStripContainer1.ResumeLayout(false);
            this.toolStripContainer1.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ToolStripContainer toolStripContainer1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TextBox uxOutputText;
        private System.Windows.Forms.TextBox uxInputText;
    }
}