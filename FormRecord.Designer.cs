namespace Suconbu.Sumacon
{
    partial class FormRecord
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormRecord));
            this.uxImageList = new System.Windows.Forms.ImageList(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.uxSaveDirectoryText = new System.Windows.Forms.TextBox();
            this.uxToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.uxStartButton = new System.Windows.Forms.Button();
            this.uxSettingPanel = new System.Windows.Forms.TableLayoutPanel();
            this.label3 = new System.Windows.Forms.Label();
            this.uxPatternText = new System.Windows.Forms.TextBox();
            this.uxOuterPanel = new System.Windows.Forms.TableLayoutPanel();
            this.uxSplitContainer = new System.Windows.Forms.SplitContainer();
            this.flowLayoutPanel4 = new System.Windows.Forms.FlowLayoutPanel();
            this.uxCountNumeric = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.flowLayoutPanel3 = new System.Windows.Forms.FlowLayoutPanel();
            this.radioButton4 = new System.Windows.Forms.RadioButton();
            this.radioButton3 = new System.Windows.Forms.RadioButton();
            this.label6 = new System.Windows.Forms.Label();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.radioButton5 = new System.Windows.Forms.RadioButton();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.label4 = new System.Windows.Forms.Label();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.axWindowsMediaPlayer1 = new AxWMPLib.AxWindowsMediaPlayer();
            this.uxSettingPanel.SuspendLayout();
            this.uxOuterPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.uxSplitContainer)).BeginInit();
            this.uxSplitContainer.Panel2.SuspendLayout();
            this.uxSplitContainer.SuspendLayout();
            this.flowLayoutPanel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.uxCountNumeric)).BeginInit();
            this.flowLayoutPanel3.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.axWindowsMediaPlayer1)).BeginInit();
            this.SuspendLayout();
            // 
            // uxImageList
            // 
            this.uxImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("uxImageList.ImageStream")));
            this.uxImageList.TransparentColor = System.Drawing.Color.Transparent;
            this.uxImageList.Images.SetKeyName(0, "page.png");
            this.uxImageList.Images.SetKeyName(1, "folder.png");
            this.uxImageList.Images.SetKeyName(2, "page_copy.png");
            this.uxImageList.Images.SetKeyName(3, "cross.png");
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(109, 31);
            this.label1.TabIndex = 0;
            this.label1.Text = "Save directory (PC):";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // uxPcSaveDirectoryText
            // 
            this.uxSaveDirectoryText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxSaveDirectoryText.Location = new System.Drawing.Point(118, 4);
            this.uxSaveDirectoryText.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.uxSaveDirectoryText.Name = "uxPcSaveDirectoryText";
            this.uxSaveDirectoryText.Size = new System.Drawing.Size(561, 23);
            this.uxSaveDirectoryText.TabIndex = 2;
            // 
            // uxStartButton
            // 
            this.uxStartButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxStartButton.Location = new System.Drawing.Point(691, 4);
            this.uxStartButton.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.uxStartButton.Name = "uxStartButton";
            this.uxStartButton.Size = new System.Drawing.Size(143, 100);
            this.uxStartButton.TabIndex = 15;
            this.uxStartButton.Text = "Button";
            this.uxStartButton.UseVisualStyleBackColor = true;
            // 
            // uxSettingPanel
            // 
            this.uxSettingPanel.ColumnCount = 2;
            this.uxSettingPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.uxSettingPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.uxSettingPanel.Controls.Add(this.label1, 0, 0);
            this.uxSettingPanel.Controls.Add(this.uxSaveDirectoryText, 1, 0);
            this.uxSettingPanel.Controls.Add(this.label3, 0, 2);
            this.uxSettingPanel.Controls.Add(this.uxPatternText, 1, 2);
            this.uxSettingPanel.Controls.Add(this.flowLayoutPanel1, 1, 3);
            this.uxSettingPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxSettingPanel.Location = new System.Drawing.Point(3, 4);
            this.uxSettingPanel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.uxSettingPanel.Name = "uxSettingPanel";
            this.uxSettingPanel.RowCount = 4;
            this.uxSettingPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.uxSettingPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.uxSettingPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.uxSettingPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.uxSettingPanel.Size = new System.Drawing.Size(682, 100);
            this.uxSettingPanel.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Location = new System.Drawing.Point(3, 31);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(109, 31);
            this.label3.TabIndex = 0;
            this.label3.Text = "File name pattern:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // uxPatternText
            // 
            this.uxPatternText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxPatternText.Location = new System.Drawing.Point(118, 35);
            this.uxPatternText.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.uxPatternText.Name = "uxPatternText";
            this.uxPatternText.Size = new System.Drawing.Size(561, 23);
            this.uxPatternText.TabIndex = 2;
            // 
            // uxOuterPanel
            // 
            this.uxOuterPanel.ColumnCount = 2;
            this.uxOuterPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 82.27666F));
            this.uxOuterPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 17.72334F));
            this.uxOuterPanel.Controls.Add(this.uxSplitContainer, 0, 1);
            this.uxOuterPanel.Controls.Add(this.uxStartButton, 1, 0);
            this.uxOuterPanel.Controls.Add(this.uxSettingPanel, 0, 0);
            this.uxOuterPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxOuterPanel.Location = new System.Drawing.Point(0, 0);
            this.uxOuterPanel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.uxOuterPanel.Name = "uxOuterPanel";
            this.uxOuterPanel.RowCount = 2;
            this.uxOuterPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.uxOuterPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.uxOuterPanel.Size = new System.Drawing.Size(837, 487);
            this.uxOuterPanel.TabIndex = 5;
            // 
            // uxSplitContainer
            // 
            this.uxOuterPanel.SetColumnSpan(this.uxSplitContainer, 2);
            this.uxSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxSplitContainer.Location = new System.Drawing.Point(3, 112);
            this.uxSplitContainer.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.uxSplitContainer.Name = "uxSplitContainer";
            // 
            // uxSplitContainer.Panel2
            // 
            this.uxSplitContainer.Panel2.Controls.Add(this.axWindowsMediaPlayer1);
            this.uxSplitContainer.Size = new System.Drawing.Size(831, 371);
            this.uxSplitContainer.SplitterDistance = 271;
            this.uxSplitContainer.SplitterWidth = 5;
            this.uxSplitContainer.TabIndex = 17;
            // 
            // flowLayoutPanel4
            // 
            this.flowLayoutPanel4.Controls.Add(this.label5);
            this.flowLayoutPanel4.Controls.Add(this.uxCountNumeric);
            this.flowLayoutPanel4.Location = new System.Drawing.Point(385, 4);
            this.flowLayoutPanel4.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.flowLayoutPanel4.Name = "flowLayoutPanel4";
            this.flowLayoutPanel4.Size = new System.Drawing.Size(150, 28);
            this.flowLayoutPanel4.TabIndex = 29;
            // 
            // uxCountNumeric
            // 
            this.uxCountNumeric.Location = new System.Drawing.Point(72, 4);
            this.uxCountNumeric.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.uxCountNumeric.Name = "uxCountNumeric";
            this.uxCountNumeric.Size = new System.Drawing.Size(60, 23);
            this.uxCountNumeric.TabIndex = 20;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label5.Location = new System.Drawing.Point(3, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(63, 31);
            this.label5.TabIndex = 23;
            this.label5.Text = "Time (sec):";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // flowLayoutPanel3
            // 
            this.flowLayoutPanel3.Controls.Add(this.label6);
            this.flowLayoutPanel3.Controls.Add(this.radioButton3);
            this.flowLayoutPanel3.Controls.Add(this.radioButton4);
            this.flowLayoutPanel3.Location = new System.Drawing.Point(159, 4);
            this.flowLayoutPanel3.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.flowLayoutPanel3.Name = "flowLayoutPanel3";
            this.flowLayoutPanel3.Size = new System.Drawing.Size(220, 27);
            this.flowLayoutPanel3.TabIndex = 28;
            // 
            // radioButton4
            // 
            this.radioButton4.AutoSize = true;
            this.radioButton4.Location = new System.Drawing.Point(127, 4);
            this.radioButton4.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.radioButton4.Name = "radioButton4";
            this.radioButton4.Size = new System.Drawing.Size(74, 19);
            this.radioButton4.TabIndex = 22;
            this.radioButton4.TabStop = true;
            this.radioButton4.Text = "Economy";
            this.radioButton4.UseVisualStyleBackColor = true;
            // 
            // radioButton3
            // 
            this.radioButton3.AutoSize = true;
            this.radioButton3.Location = new System.Drawing.Point(57, 4);
            this.radioButton3.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.radioButton3.Name = "radioButton3";
            this.radioButton3.Size = new System.Drawing.Size(64, 19);
            this.radioButton3.TabIndex = 23;
            this.radioButton3.TabStop = true;
            this.radioButton3.Text = "Normal";
            this.radioButton3.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label6.Location = new System.Drawing.Point(3, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(48, 27);
            this.label6.TabIndex = 23;
            this.label6.Text = "Quality:";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.Controls.Add(this.label4);
            this.flowLayoutPanel2.Controls.Add(this.radioButton2);
            this.flowLayoutPanel2.Controls.Add(this.radioButton1);
            this.flowLayoutPanel2.Controls.Add(this.radioButton5);
            this.flowLayoutPanel2.Location = new System.Drawing.Point(3, 4);
            this.flowLayoutPanel2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(150, 26);
            this.flowLayoutPanel2.TabIndex = 27;
            // 
            // radioButton5
            // 
            this.radioButton5.AutoSize = true;
            this.radioButton5.Location = new System.Drawing.Point(3, 31);
            this.radioButton5.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.radioButton5.Name = "radioButton5";
            this.radioButton5.Size = new System.Drawing.Size(65, 19);
            this.radioButton5.TabIndex = 24;
            this.radioButton5.TabStop = true;
            this.radioButton5.Text = "Quarter";
            this.radioButton5.UseVisualStyleBackColor = true;
            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Location = new System.Drawing.Point(88, 4);
            this.radioButton1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(47, 19);
            this.radioButton1.TabIndex = 21;
            this.radioButton1.TabStop = true;
            this.radioButton1.Text = "Half";
            this.radioButton1.UseVisualStyleBackColor = true;
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Location = new System.Drawing.Point(39, 4);
            this.radioButton2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(43, 19);
            this.radioButton2.TabIndex = 23;
            this.radioButton2.TabStop = true;
            this.radioButton2.Text = "Full";
            this.radioButton2.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label4.Location = new System.Drawing.Point(3, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(30, 27);
            this.label4.TabIndex = 23;
            this.label4.Text = "Size:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.flowLayoutPanel2);
            this.flowLayoutPanel1.Controls.Add(this.flowLayoutPanel3);
            this.flowLayoutPanel1.Controls.Add(this.flowLayoutPanel4);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(118, 65);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(561, 42);
            this.flowLayoutPanel1.TabIndex = 3;
            // 
            // axWindowsMediaPlayer1
            // 
            this.axWindowsMediaPlayer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.axWindowsMediaPlayer1.Enabled = true;
            this.axWindowsMediaPlayer1.Location = new System.Drawing.Point(0, 0);
            this.axWindowsMediaPlayer1.Name = "axWindowsMediaPlayer1";
            this.axWindowsMediaPlayer1.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axWindowsMediaPlayer1.OcxState")));
            this.axWindowsMediaPlayer1.Size = new System.Drawing.Size(555, 371);
            this.axWindowsMediaPlayer1.TabIndex = 0;
            // 
            // FormRecord
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(837, 487);
            this.Controls.Add(this.uxOuterPanel);
            this.Font = new System.Drawing.Font("Yu Gothic UI", 9F);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "FormRecord";
            this.Text = "FormRecord";
            this.uxSettingPanel.ResumeLayout(false);
            this.uxSettingPanel.PerformLayout();
            this.uxOuterPanel.ResumeLayout(false);
            this.uxSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.uxSplitContainer)).EndInit();
            this.uxSplitContainer.ResumeLayout(false);
            this.flowLayoutPanel4.ResumeLayout(false);
            this.flowLayoutPanel4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.uxCountNumeric)).EndInit();
            this.flowLayoutPanel3.ResumeLayout(false);
            this.flowLayoutPanel3.PerformLayout();
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel2.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.axWindowsMediaPlayer1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ImageList uxImageList;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox uxSaveDirectoryText;
        private System.Windows.Forms.ToolTip uxToolTip;
        private System.Windows.Forms.TableLayoutPanel uxOuterPanel;
        private System.Windows.Forms.Button uxStartButton;
        private System.Windows.Forms.TableLayoutPanel uxSettingPanel;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox uxPatternText;
        private System.Windows.Forms.SplitContainer uxSplitContainer;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.RadioButton radioButton1;
        private System.Windows.Forms.RadioButton radioButton5;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel3;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.RadioButton radioButton3;
        private System.Windows.Forms.RadioButton radioButton4;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown uxCountNumeric;
        private AxWMPLib.AxWindowsMediaPlayer axWindowsMediaPlayer1;
    }
}