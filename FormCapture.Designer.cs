namespace Suconbu.Sumacon
{
    partial class FormCapture
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
            this.components = new System.ComponentModel.Container();
            this.uxOuterPanel = new System.Windows.Forms.TableLayoutPanel();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.uxFileListView = new System.Windows.Forms.ListView();
            this.uxPreviewPicture = new System.Windows.Forms.PictureBox();
            this.uxStartButton = new System.Windows.Forms.Button();
            this.uxSettingPanel = new System.Windows.Forms.TableLayoutPanel();
            this.uxContinuousCheck = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.uxSaveDirectoryText = new System.Windows.Forms.TextBox();
            this.uxPatternText = new System.Windows.Forms.TextBox();
            this.uxConinuousPanel = new System.Windows.Forms.TableLayoutPanel();
            this.uxSkipCheck = new System.Windows.Forms.CheckBox();
            this.uxCountNumeric = new System.Windows.Forms.NumericUpDown();
            this.uxIntervalLabel = new System.Windows.Forms.Label();
            this.uxIntervalNumeric = new System.Windows.Forms.NumericUpDown();
            this.uxCountCheck = new System.Windows.Forms.CheckBox();
            this.uxToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.uxOuterPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.uxPreviewPicture)).BeginInit();
            this.uxSettingPanel.SuspendLayout();
            this.uxConinuousPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.uxCountNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.uxIntervalNumeric)).BeginInit();
            this.SuspendLayout();
            // 
            // uxOuterPanel
            // 
            this.uxOuterPanel.ColumnCount = 2;
            this.uxOuterPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 82.27666F));
            this.uxOuterPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 17.72334F));
            this.uxOuterPanel.Controls.Add(this.splitContainer1, 0, 1);
            this.uxOuterPanel.Controls.Add(this.uxStartButton, 1, 0);
            this.uxOuterPanel.Controls.Add(this.uxSettingPanel, 0, 0);
            this.uxOuterPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxOuterPanel.Location = new System.Drawing.Point(0, 0);
            this.uxOuterPanel.Name = "uxOuterPanel";
            this.uxOuterPanel.RowCount = 2;
            this.uxOuterPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.uxOuterPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.uxOuterPanel.Size = new System.Drawing.Size(800, 450);
            this.uxOuterPanel.TabIndex = 4;
            // 
            // splitContainer1
            // 
            this.uxOuterPanel.SetColumnSpan(this.splitContainer1, 2);
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(3, 97);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.uxFileListView);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.uxPreviewPicture);
            this.splitContainer1.Size = new System.Drawing.Size(794, 350);
            this.splitContainer1.SplitterDistance = 260;
            this.splitContainer1.TabIndex = 16;
            // 
            // uxFileListView
            // 
            this.uxFileListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxFileListView.Location = new System.Drawing.Point(0, 0);
            this.uxFileListView.Name = "uxFileListView";
            this.uxFileListView.Size = new System.Drawing.Size(260, 350);
            this.uxFileListView.TabIndex = 0;
            this.uxFileListView.UseCompatibleStateImageBehavior = false;
            this.uxFileListView.View = System.Windows.Forms.View.Details;
            // 
            // uxPreviewPicture
            // 
            this.uxPreviewPicture.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxPreviewPicture.Location = new System.Drawing.Point(0, 0);
            this.uxPreviewPicture.Name = "uxPreviewPicture";
            this.uxPreviewPicture.Size = new System.Drawing.Size(530, 350);
            this.uxPreviewPicture.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.uxPreviewPicture.TabIndex = 0;
            this.uxPreviewPicture.TabStop = false;
            // 
            // uxStartButton
            // 
            this.uxStartButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxStartButton.Location = new System.Drawing.Point(661, 3);
            this.uxStartButton.Name = "uxStartButton";
            this.uxStartButton.Size = new System.Drawing.Size(136, 88);
            this.uxStartButton.TabIndex = 15;
            this.uxStartButton.Text = "Button";
            this.uxStartButton.UseVisualStyleBackColor = true;
            // 
            // uxSettingPanel
            // 
            this.uxSettingPanel.ColumnCount = 2;
            this.uxSettingPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.uxSettingPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.uxSettingPanel.Controls.Add(this.uxContinuousCheck, 0, 2);
            this.uxSettingPanel.Controls.Add(this.label1, 0, 0);
            this.uxSettingPanel.Controls.Add(this.label2, 0, 1);
            this.uxSettingPanel.Controls.Add(this.uxSaveDirectoryText, 1, 0);
            this.uxSettingPanel.Controls.Add(this.uxPatternText, 1, 1);
            this.uxSettingPanel.Controls.Add(this.uxConinuousPanel, 1, 2);
            this.uxSettingPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxSettingPanel.Location = new System.Drawing.Point(3, 3);
            this.uxSettingPanel.Name = "uxSettingPanel";
            this.uxSettingPanel.RowCount = 3;
            this.uxSettingPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.uxSettingPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.uxSettingPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.uxSettingPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.uxSettingPanel.Size = new System.Drawing.Size(652, 88);
            this.uxSettingPanel.TabIndex = 5;
            // 
            // uxContinuousCheck
            // 
            this.uxContinuousCheck.AutoSize = true;
            this.uxContinuousCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxContinuousCheck.Location = new System.Drawing.Point(3, 61);
            this.uxContinuousCheck.Name = "uxContinuousCheck";
            this.uxContinuousCheck.Size = new System.Drawing.Size(109, 24);
            this.uxContinuousCheck.TabIndex = 14;
            this.uxContinuousCheck.Text = "Continuous:";
            this.uxContinuousCheck.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(109, 29);
            this.label1.TabIndex = 0;
            this.label1.Text = "Save directory (PC):";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Location = new System.Drawing.Point(3, 29);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(109, 29);
            this.label2.TabIndex = 0;
            this.label2.Text = "File name pattern:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // uxSaveDirectoryText
            // 
            this.uxSaveDirectoryText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxSaveDirectoryText.Location = new System.Drawing.Point(118, 3);
            this.uxSaveDirectoryText.Name = "uxSaveDirectoryText";
            this.uxSaveDirectoryText.Size = new System.Drawing.Size(531, 23);
            this.uxSaveDirectoryText.TabIndex = 2;
            // 
            // uxPatternText
            // 
            this.uxPatternText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxPatternText.Location = new System.Drawing.Point(118, 32);
            this.uxPatternText.Name = "uxPatternText";
            this.uxPatternText.Size = new System.Drawing.Size(531, 23);
            this.uxPatternText.TabIndex = 2;
            // 
            // uxConinuousPanel
            // 
            this.uxConinuousPanel.ColumnCount = 6;
            this.uxConinuousPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.uxConinuousPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.uxConinuousPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.uxConinuousPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.uxConinuousPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.uxConinuousPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 111F));
            this.uxConinuousPanel.Controls.Add(this.uxSkipCheck, 4, 0);
            this.uxConinuousPanel.Controls.Add(this.uxCountNumeric, 3, 0);
            this.uxConinuousPanel.Controls.Add(this.uxIntervalLabel, 0, 0);
            this.uxConinuousPanel.Controls.Add(this.uxIntervalNumeric, 1, 0);
            this.uxConinuousPanel.Controls.Add(this.uxCountCheck, 2, 0);
            this.uxConinuousPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxConinuousPanel.Location = new System.Drawing.Point(115, 58);
            this.uxConinuousPanel.Margin = new System.Windows.Forms.Padding(0);
            this.uxConinuousPanel.Name = "uxConinuousPanel";
            this.uxConinuousPanel.RowCount = 1;
            this.uxConinuousPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.uxConinuousPanel.Size = new System.Drawing.Size(537, 30);
            this.uxConinuousPanel.TabIndex = 3;
            // 
            // uxSkipCheck
            // 
            this.uxSkipCheck.AutoSize = true;
            this.uxSkipCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxSkipCheck.Location = new System.Drawing.Point(329, 3);
            this.uxSkipCheck.Name = "uxSkipCheck";
            this.uxSkipCheck.Size = new System.Drawing.Size(107, 24);
            this.uxSkipCheck.TabIndex = 22;
            this.uxSkipCheck.Text = "Skip duplicated";
            this.uxSkipCheck.UseVisualStyleBackColor = true;
            // 
            // uxCountNumeric
            // 
            this.uxCountNumeric.Location = new System.Drawing.Point(263, 3);
            this.uxCountNumeric.Name = "uxCountNumeric";
            this.uxCountNumeric.Size = new System.Drawing.Size(60, 23);
            this.uxCountNumeric.TabIndex = 20;
            // 
            // uxIntervalLabel
            // 
            this.uxIntervalLabel.AutoSize = true;
            this.uxIntervalLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxIntervalLabel.Location = new System.Drawing.Point(3, 0);
            this.uxIntervalLabel.Name = "uxIntervalLabel";
            this.uxIntervalLabel.Size = new System.Drawing.Size(104, 30);
            this.uxIntervalLabel.TabIndex = 14;
            this.uxIntervalLabel.Text = "Min. interval (sec):";
            this.uxIntervalLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // uxIntervalNumeric
            // 
            this.uxIntervalNumeric.Location = new System.Drawing.Point(113, 3);
            this.uxIntervalNumeric.Name = "uxIntervalNumeric";
            this.uxIntervalNumeric.Size = new System.Drawing.Size(60, 23);
            this.uxIntervalNumeric.TabIndex = 19;
            // 
            // uxCountCheck
            // 
            this.uxCountCheck.AutoSize = true;
            this.uxCountCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxCountCheck.Location = new System.Drawing.Point(179, 3);
            this.uxCountCheck.Name = "uxCountCheck";
            this.uxCountCheck.Size = new System.Drawing.Size(78, 24);
            this.uxCountCheck.TabIndex = 21;
            this.uxCountCheck.Text = "# of shots";
            this.uxCountCheck.UseVisualStyleBackColor = true;
            // 
            // FormCapture
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.uxOuterPanel);
            this.Font = new System.Drawing.Font("Yu Gothic UI", 9F);
            this.Name = "FormCapture";
            this.Text = "FormCapture";
            this.uxOuterPanel.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.uxPreviewPicture)).EndInit();
            this.uxSettingPanel.ResumeLayout(false);
            this.uxSettingPanel.PerformLayout();
            this.uxConinuousPanel.ResumeLayout(false);
            this.uxConinuousPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.uxCountNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.uxIntervalNumeric)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel uxOuterPanel;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ListView uxFileListView;
        private System.Windows.Forms.PictureBox uxPreviewPicture;
        private System.Windows.Forms.Button uxStartButton;
        private System.Windows.Forms.TableLayoutPanel uxSettingPanel;
        private System.Windows.Forms.CheckBox uxContinuousCheck;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox uxSaveDirectoryText;
        private System.Windows.Forms.TextBox uxPatternText;
        private System.Windows.Forms.TableLayoutPanel uxConinuousPanel;
        private System.Windows.Forms.NumericUpDown uxCountNumeric;
        private System.Windows.Forms.Label uxIntervalLabel;
        private System.Windows.Forms.NumericUpDown uxIntervalNumeric;
        private System.Windows.Forms.CheckBox uxCountCheck;
        private System.Windows.Forms.CheckBox uxSkipCheck;
        private System.Windows.Forms.ToolTip uxToolTip;
    }
}