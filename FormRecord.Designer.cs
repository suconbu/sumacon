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
            this.flowLayoutPanel3 = new System.Windows.Forms.FlowLayoutPanel();
            this.uxTimeNumeric = new System.Windows.Forms.NumericUpDown();
            this.flowLayoutPanel5 = new System.Windows.Forms.FlowLayoutPanel();
            this.uxTime10 = new System.Windows.Forms.RadioButton();
            this.uxTime30 = new System.Windows.Forms.RadioButton();
            this.uxTime60 = new System.Windows.Forms.RadioButton();
            this.uxTime180 = new System.Windows.Forms.RadioButton();
            this.uxApproxLabel = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.uxPatternText = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.label4 = new System.Windows.Forms.Label();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.uxSize1 = new System.Windows.Forms.RadioButton();
            this.uxSize2 = new System.Windows.Forms.RadioButton();
            this.uxSize4 = new System.Windows.Forms.RadioButton();
            this.label6 = new System.Windows.Forms.Label();
            this.flowLayoutPanel4 = new System.Windows.Forms.FlowLayoutPanel();
            this.uxQualityNormal = new System.Windows.Forms.RadioButton();
            this.uxQuarityEconomy = new System.Windows.Forms.RadioButton();
            this.uxTimestampCheck = new System.Windows.Forms.CheckBox();
            this.uxOuterPanel = new System.Windows.Forms.TableLayoutPanel();
            this.uxSplitContainer = new System.Windows.Forms.SplitContainer();
            this.axWindowsMediaPlayer1 = new AxWMPLib.AxWindowsMediaPlayer();
            this.uxSettingPanel.SuspendLayout();
            this.flowLayoutPanel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.uxTimeNumeric)).BeginInit();
            this.flowLayoutPanel5.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.flowLayoutPanel4.SuspendLayout();
            this.uxOuterPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.uxSplitContainer)).BeginInit();
            this.uxSplitContainer.Panel2.SuspendLayout();
            this.uxSplitContainer.SuspendLayout();
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
            this.label1.Size = new System.Drawing.Size(101, 29);
            this.label1.TabIndex = 0;
            this.label1.Text = "Save directory:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // uxSaveDirectoryText
            // 
            this.uxSaveDirectoryText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxSaveDirectoryText.Location = new System.Drawing.Point(110, 3);
            this.uxSaveDirectoryText.Name = "uxSaveDirectoryText";
            this.uxSaveDirectoryText.Size = new System.Drawing.Size(569, 23);
            this.uxSaveDirectoryText.TabIndex = 1;
            // 
            // uxStartButton
            // 
            this.uxStartButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxStartButton.Location = new System.Drawing.Point(691, 3);
            this.uxStartButton.Name = "uxStartButton";
            this.uxStartButton.Size = new System.Drawing.Size(143, 132);
            this.uxStartButton.TabIndex = 0;
            this.uxStartButton.Text = "Button";
            this.uxStartButton.UseVisualStyleBackColor = true;
            // 
            // uxSettingPanel
            // 
            this.uxSettingPanel.ColumnCount = 2;
            this.uxSettingPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.uxSettingPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.uxSettingPanel.Controls.Add(this.flowLayoutPanel3, 1, 3);
            this.uxSettingPanel.Controls.Add(this.label1, 0, 0);
            this.uxSettingPanel.Controls.Add(this.uxSaveDirectoryText, 1, 0);
            this.uxSettingPanel.Controls.Add(this.label3, 0, 1);
            this.uxSettingPanel.Controls.Add(this.uxPatternText, 1, 1);
            this.uxSettingPanel.Controls.Add(this.label2, 0, 2);
            this.uxSettingPanel.Controls.Add(this.label5, 0, 3);
            this.uxSettingPanel.Controls.Add(this.flowLayoutPanel1, 1, 2);
            this.uxSettingPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxSettingPanel.Location = new System.Drawing.Point(3, 3);
            this.uxSettingPanel.Name = "uxSettingPanel";
            this.uxSettingPanel.RowCount = 4;
            this.uxSettingPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.uxSettingPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.uxSettingPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.uxSettingPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.uxSettingPanel.Size = new System.Drawing.Size(682, 132);
            this.uxSettingPanel.TabIndex = 5;
            // 
            // flowLayoutPanel3
            // 
            this.flowLayoutPanel3.Controls.Add(this.uxTimeNumeric);
            this.flowLayoutPanel3.Controls.Add(this.flowLayoutPanel5);
            this.flowLayoutPanel3.Controls.Add(this.uxApproxLabel);
            this.flowLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel3.Location = new System.Drawing.Point(107, 92);
            this.flowLayoutPanel3.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel3.Name = "flowLayoutPanel3";
            this.flowLayoutPanel3.Size = new System.Drawing.Size(575, 40);
            this.flowLayoutPanel3.TabIndex = 34;
            this.flowLayoutPanel3.WrapContents = false;
            // 
            // uxTimeNumeric
            // 
            this.uxTimeNumeric.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxTimeNumeric.Location = new System.Drawing.Point(3, 6);
            this.uxTimeNumeric.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.uxTimeNumeric.Name = "uxTimeNumeric";
            this.uxTimeNumeric.Size = new System.Drawing.Size(50, 23);
            this.uxTimeNumeric.TabIndex = 4;
            // 
            // flowLayoutPanel5
            // 
            this.flowLayoutPanel5.AutoSize = true;
            this.flowLayoutPanel5.Controls.Add(this.uxTime10);
            this.flowLayoutPanel5.Controls.Add(this.uxTime30);
            this.flowLayoutPanel5.Controls.Add(this.uxTime60);
            this.flowLayoutPanel5.Controls.Add(this.uxTime180);
            this.flowLayoutPanel5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel5.Location = new System.Drawing.Point(56, 0);
            this.flowLayoutPanel5.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel5.Name = "flowLayoutPanel5";
            this.flowLayoutPanel5.Size = new System.Drawing.Size(167, 31);
            this.flowLayoutPanel5.TabIndex = 51;
            this.flowLayoutPanel5.WrapContents = false;
            // 
            // uxTime10
            // 
            this.uxTime10.Appearance = System.Windows.Forms.Appearance.Button;
            this.uxTime10.AutoSize = true;
            this.uxTime10.Location = new System.Drawing.Point(1, 3);
            this.uxTime10.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.uxTime10.Name = "uxTime10";
            this.uxTime10.Size = new System.Drawing.Size(29, 25);
            this.uxTime10.TabIndex = 47;
            this.uxTime10.TabStop = true;
            this.uxTime10.Tag = "10";
            this.uxTime10.Text = "10";
            this.uxTime10.UseVisualStyleBackColor = true;
            // 
            // uxTime30
            // 
            this.uxTime30.Appearance = System.Windows.Forms.Appearance.Button;
            this.uxTime30.AutoSize = true;
            this.uxTime30.Location = new System.Drawing.Point(32, 3);
            this.uxTime30.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.uxTime30.Name = "uxTime30";
            this.uxTime30.Size = new System.Drawing.Size(29, 25);
            this.uxTime30.TabIndex = 48;
            this.uxTime30.TabStop = true;
            this.uxTime30.Tag = "30";
            this.uxTime30.Text = "30";
            this.uxTime30.UseVisualStyleBackColor = true;
            // 
            // uxTime60
            // 
            this.uxTime60.Appearance = System.Windows.Forms.Appearance.Button;
            this.uxTime60.AutoSize = true;
            this.uxTime60.Location = new System.Drawing.Point(63, 3);
            this.uxTime60.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.uxTime60.Name = "uxTime60";
            this.uxTime60.Size = new System.Drawing.Size(29, 25);
            this.uxTime60.TabIndex = 49;
            this.uxTime60.TabStop = true;
            this.uxTime60.Tag = "60";
            this.uxTime60.Text = "60";
            this.uxTime60.UseVisualStyleBackColor = true;
            // 
            // uxTime180
            // 
            this.uxTime180.Appearance = System.Windows.Forms.Appearance.Button;
            this.uxTime180.AutoSize = true;
            this.uxTime180.Location = new System.Drawing.Point(94, 3);
            this.uxTime180.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.uxTime180.Name = "uxTime180";
            this.uxTime180.Size = new System.Drawing.Size(72, 25);
            this.uxTime180.TabIndex = 50;
            this.uxTime180.TabStop = true;
            this.uxTime180.Tag = "180";
            this.uxTime180.Text = "180 (Max.)";
            this.uxTime180.UseVisualStyleBackColor = true;
            // 
            // uxApproxLabel
            // 
            this.uxApproxLabel.AutoSize = true;
            this.uxApproxLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxApproxLabel.Location = new System.Drawing.Point(235, 0);
            this.uxApproxLabel.Margin = new System.Windows.Forms.Padding(12, 0, 3, 0);
            this.uxApproxLabel.Name = "uxApproxLabel";
            this.uxApproxLabel.Size = new System.Drawing.Size(96, 31);
            this.uxApproxLabel.TabIndex = 52;
            this.uxApproxLabel.Text = "(Approx. 123MB)";
            this.uxApproxLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Location = new System.Drawing.Point(3, 29);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(101, 29);
            this.label3.TabIndex = 2;
            this.label3.Text = "File name pattern:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // uxPatternText
            // 
            this.uxPatternText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxPatternText.Location = new System.Drawing.Point(110, 32);
            this.uxPatternText.Name = "uxPatternText";
            this.uxPatternText.Size = new System.Drawing.Size(569, 23);
            this.uxPatternText.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Location = new System.Drawing.Point(3, 58);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(101, 34);
            this.label2.TabIndex = 4;
            this.label2.Text = "Video setting:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label5.Location = new System.Drawing.Point(3, 92);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(101, 40);
            this.label5.TabIndex = 5;
            this.label5.Text = "Limit time (sec):";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.label4);
            this.flowLayoutPanel1.Controls.Add(this.flowLayoutPanel2);
            this.flowLayoutPanel1.Controls.Add(this.label6);
            this.flowLayoutPanel1.Controls.Add(this.flowLayoutPanel4);
            this.flowLayoutPanel1.Controls.Add(this.uxTimestampCheck);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(107, 58);
            this.flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(575, 34);
            this.flowLayoutPanel1.TabIndex = 33;
            this.flowLayoutPanel1.WrapContents = false;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label4.Location = new System.Drawing.Point(3, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(57, 31);
            this.label4.TabIndex = 39;
            this.label4.Text = "View size:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.AutoSize = true;
            this.flowLayoutPanel2.Controls.Add(this.uxSize1);
            this.flowLayoutPanel2.Controls.Add(this.uxSize2);
            this.flowLayoutPanel2.Controls.Add(this.uxSize4);
            this.flowLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel2.Location = new System.Drawing.Point(63, 0);
            this.flowLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(108, 31);
            this.flowLayoutPanel2.TabIndex = 47;
            this.flowLayoutPanel2.WrapContents = false;
            // 
            // uxSize1
            // 
            this.uxSize1.Appearance = System.Windows.Forms.Appearance.Button;
            this.uxSize1.AutoSize = true;
            this.uxSize1.Location = new System.Drawing.Point(1, 3);
            this.uxSize1.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.uxSize1.Name = "uxSize1";
            this.uxSize1.Size = new System.Drawing.Size(34, 25);
            this.uxSize1.TabIndex = 43;
            this.uxSize1.TabStop = true;
            this.uxSize1.Text = "1/1";
            this.uxSize1.UseVisualStyleBackColor = true;
            // 
            // uxSize2
            // 
            this.uxSize2.Appearance = System.Windows.Forms.Appearance.Button;
            this.uxSize2.AutoSize = true;
            this.uxSize2.Location = new System.Drawing.Point(37, 3);
            this.uxSize2.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.uxSize2.Name = "uxSize2";
            this.uxSize2.Size = new System.Drawing.Size(34, 25);
            this.uxSize2.TabIndex = 44;
            this.uxSize2.TabStop = true;
            this.uxSize2.Text = "1/2";
            this.uxSize2.UseVisualStyleBackColor = true;
            // 
            // uxSize4
            // 
            this.uxSize4.Appearance = System.Windows.Forms.Appearance.Button;
            this.uxSize4.AutoSize = true;
            this.uxSize4.Location = new System.Drawing.Point(73, 3);
            this.uxSize4.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.uxSize4.Name = "uxSize4";
            this.uxSize4.Size = new System.Drawing.Size(34, 25);
            this.uxSize4.TabIndex = 45;
            this.uxSize4.TabStop = true;
            this.uxSize4.Text = "1/4";
            this.uxSize4.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label6.Location = new System.Drawing.Point(183, 0);
            this.label6.Margin = new System.Windows.Forms.Padding(12, 0, 3, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(48, 31);
            this.label6.TabIndex = 49;
            this.label6.Text = "Quality:";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // flowLayoutPanel4
            // 
            this.flowLayoutPanel4.AutoSize = true;
            this.flowLayoutPanel4.Controls.Add(this.uxQualityNormal);
            this.flowLayoutPanel4.Controls.Add(this.uxQuarityEconomy);
            this.flowLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel4.Location = new System.Drawing.Point(234, 0);
            this.flowLayoutPanel4.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel4.Name = "flowLayoutPanel4";
            this.flowLayoutPanel4.Size = new System.Drawing.Size(126, 31);
            this.flowLayoutPanel4.TabIndex = 50;
            this.flowLayoutPanel4.WrapContents = false;
            // 
            // uxQualityNormal
            // 
            this.uxQualityNormal.Appearance = System.Windows.Forms.Appearance.Button;
            this.uxQualityNormal.AutoSize = true;
            this.uxQualityNormal.Location = new System.Drawing.Point(1, 3);
            this.uxQualityNormal.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.uxQualityNormal.Name = "uxQualityNormal";
            this.uxQualityNormal.Size = new System.Drawing.Size(56, 25);
            this.uxQualityNormal.TabIndex = 46;
            this.uxQualityNormal.TabStop = true;
            this.uxQualityNormal.Text = "Normal";
            this.uxQualityNormal.UseVisualStyleBackColor = true;
            // 
            // uxQuarityEconomy
            // 
            this.uxQuarityEconomy.Appearance = System.Windows.Forms.Appearance.Button;
            this.uxQuarityEconomy.AutoSize = true;
            this.uxQuarityEconomy.Location = new System.Drawing.Point(59, 3);
            this.uxQuarityEconomy.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.uxQuarityEconomy.Name = "uxQuarityEconomy";
            this.uxQuarityEconomy.Size = new System.Drawing.Size(66, 25);
            this.uxQuarityEconomy.TabIndex = 47;
            this.uxQuarityEconomy.TabStop = true;
            this.uxQuarityEconomy.Text = "Economy";
            this.uxQuarityEconomy.UseVisualStyleBackColor = true;
            // 
            // uxShowTimestampCheck
            // 
            this.uxTimestampCheck.AutoSize = true;
            this.uxTimestampCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxTimestampCheck.Location = new System.Drawing.Point(372, 3);
            this.uxTimestampCheck.Margin = new System.Windows.Forms.Padding(12, 3, 3, 3);
            this.uxTimestampCheck.Name = "uxShowTimestampCheck";
            this.uxTimestampCheck.Size = new System.Drawing.Size(113, 25);
            this.uxTimestampCheck.TabIndex = 51;
            this.uxTimestampCheck.Text = "Show timestamp";
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
            this.uxSplitContainer.Location = new System.Drawing.Point(3, 142);
            this.uxSplitContainer.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.uxSplitContainer.Name = "uxSplitContainer";
            // 
            // uxSplitContainer.Panel2
            // 
            this.uxSplitContainer.Panel2.Controls.Add(this.axWindowsMediaPlayer1);
            this.uxSplitContainer.Size = new System.Drawing.Size(831, 341);
            this.uxSplitContainer.SplitterDistance = 271;
            this.uxSplitContainer.SplitterWidth = 5;
            this.uxSplitContainer.TabIndex = 0;
            // 
            // axWindowsMediaPlayer1
            // 
            this.axWindowsMediaPlayer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.axWindowsMediaPlayer1.Enabled = true;
            this.axWindowsMediaPlayer1.Location = new System.Drawing.Point(0, 0);
            this.axWindowsMediaPlayer1.Name = "axWindowsMediaPlayer1";
            this.axWindowsMediaPlayer1.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axWindowsMediaPlayer1.OcxState")));
            this.axWindowsMediaPlayer1.Size = new System.Drawing.Size(555, 341);
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
            this.flowLayoutPanel3.ResumeLayout(false);
            this.flowLayoutPanel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.uxTimeNumeric)).EndInit();
            this.flowLayoutPanel5.ResumeLayout(false);
            this.flowLayoutPanel5.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel2.PerformLayout();
            this.flowLayoutPanel4.ResumeLayout(false);
            this.flowLayoutPanel4.PerformLayout();
            this.uxOuterPanel.ResumeLayout(false);
            this.uxSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.uxSplitContainer)).EndInit();
            this.uxSplitContainer.ResumeLayout(false);
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
        private System.Windows.Forms.Label label5;
        private AxWMPLib.AxWindowsMediaPlayer axWindowsMediaPlayer1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel3;
        private System.Windows.Forms.NumericUpDown uxTimeNumeric;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel5;
        private System.Windows.Forms.RadioButton uxTime10;
        private System.Windows.Forms.RadioButton uxTime30;
        private System.Windows.Forms.RadioButton uxTime60;
        private System.Windows.Forms.RadioButton uxTime180;
        private System.Windows.Forms.Label uxApproxLabel;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.RadioButton uxSize1;
        private System.Windows.Forms.RadioButton uxSize2;
        private System.Windows.Forms.RadioButton uxSize4;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel4;
        private System.Windows.Forms.RadioButton uxQualityNormal;
        private System.Windows.Forms.RadioButton uxQuarityEconomy;
        private System.Windows.Forms.CheckBox uxTimestampCheck;
    }
}