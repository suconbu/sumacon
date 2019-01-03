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
            this.label2 = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.uxTimeNumeric = new System.Windows.Forms.NumericUpDown();
            this.uxTimeBar = new System.Windows.Forms.TrackBar();
            this.label5 = new System.Windows.Forms.Label();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.uxApproxLabel = new System.Windows.Forms.Label();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.label6 = new System.Windows.Forms.Label();
            this.uxQualityNormal = new System.Windows.Forms.RadioButton();
            this.uxQuarityEconomy = new System.Windows.Forms.RadioButton();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.label4 = new System.Windows.Forms.Label();
            this.uxSize1 = new System.Windows.Forms.RadioButton();
            this.uxSize2 = new System.Windows.Forms.RadioButton();
            this.uxSize4 = new System.Windows.Forms.RadioButton();
            this.uxOuterPanel = new System.Windows.Forms.TableLayoutPanel();
            this.uxSplitContainer = new System.Windows.Forms.SplitContainer();
            this.axWindowsMediaPlayer1 = new AxWMPLib.AxWindowsMediaPlayer();
            this.uxSettingPanel.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.uxTimeNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.uxTimeBar)).BeginInit();
            this.tableLayoutPanel2.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
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
            this.label1.Size = new System.Drawing.Size(101, 31);
            this.label1.TabIndex = 0;
            this.label1.Text = "Save directory:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // uxSaveDirectoryText
            // 
            this.uxSaveDirectoryText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxSaveDirectoryText.Location = new System.Drawing.Point(110, 4);
            this.uxSaveDirectoryText.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.uxSaveDirectoryText.Name = "uxSaveDirectoryText";
            this.uxSaveDirectoryText.Size = new System.Drawing.Size(569, 23);
            this.uxSaveDirectoryText.TabIndex = 1;
            // 
            // uxStartButton
            // 
            this.uxStartButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxStartButton.Location = new System.Drawing.Point(691, 4);
            this.uxStartButton.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.uxStartButton.Name = "uxStartButton";
            this.uxStartButton.Size = new System.Drawing.Size(143, 130);
            this.uxStartButton.TabIndex = 0;
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
            this.uxSettingPanel.Controls.Add(this.label3, 0, 1);
            this.uxSettingPanel.Controls.Add(this.uxPatternText, 1, 1);
            this.uxSettingPanel.Controls.Add(this.label2, 0, 2);
            this.uxSettingPanel.Controls.Add(this.tableLayoutPanel1, 1, 3);
            this.uxSettingPanel.Controls.Add(this.label5, 0, 3);
            this.uxSettingPanel.Controls.Add(this.tableLayoutPanel2, 1, 2);
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
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Location = new System.Drawing.Point(3, 31);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(101, 31);
            this.label3.TabIndex = 2;
            this.label3.Text = "File name pattern:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // uxPatternText
            // 
            this.uxPatternText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxPatternText.Location = new System.Drawing.Point(110, 35);
            this.uxPatternText.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.uxPatternText.Name = "uxPatternText";
            this.uxPatternText.Size = new System.Drawing.Size(569, 23);
            this.uxPatternText.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Location = new System.Drawing.Point(3, 62);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(101, 33);
            this.label2.TabIndex = 4;
            this.label2.Text = "Video setting:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.uxTimeNumeric, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.uxTimeBar, 1, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(107, 95);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(575, 37);
            this.tableLayoutPanel1.TabIndex = 32;
            // 
            // uxTimeNumeric
            // 
            this.uxTimeNumeric.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxTimeNumeric.Location = new System.Drawing.Point(3, 4);
            this.uxTimeNumeric.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.uxTimeNumeric.Name = "uxTimeNumeric";
            this.uxTimeNumeric.Size = new System.Drawing.Size(60, 23);
            this.uxTimeNumeric.TabIndex = 0;
            // 
            // uxTimeBar
            // 
            this.uxTimeBar.AutoSize = false;
            this.uxTimeBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxTimeBar.Location = new System.Drawing.Point(69, 3);
            this.uxTimeBar.Name = "uxTimeBar";
            this.uxTimeBar.Size = new System.Drawing.Size(503, 31);
            this.uxTimeBar.TabIndex = 1;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label5.Location = new System.Drawing.Point(3, 95);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(101, 37);
            this.label5.TabIndex = 5;
            this.label5.Text = "Limit time (sec):";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 3;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Controls.Add(this.uxApproxLabel, 2, 0);
            this.tableLayoutPanel2.Controls.Add(this.flowLayoutPanel1, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.flowLayoutPanel2, 0, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(107, 62);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.Size = new System.Drawing.Size(575, 33);
            this.tableLayoutPanel2.TabIndex = 33;
            // 
            // uxApproxLabel
            // 
            this.uxApproxLabel.AutoSize = true;
            this.uxApproxLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxApproxLabel.Location = new System.Drawing.Point(403, 0);
            this.uxApproxLabel.Name = "uxApproxLabel";
            this.uxApproxLabel.Size = new System.Drawing.Size(169, 33);
            this.uxApproxLabel.TabIndex = 34;
            this.uxApproxLabel.Text = "(Approx. 123MB)";
            this.uxApproxLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.label6);
            this.flowLayoutPanel1.Controls.Add(this.uxQualityNormal);
            this.flowLayoutPanel1.Controls.Add(this.uxQuarityEconomy);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(200, 0);
            this.flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(200, 33);
            this.flowLayoutPanel1.TabIndex = 33;
            this.flowLayoutPanel1.WrapContents = false;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label6.Location = new System.Drawing.Point(3, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(48, 33);
            this.label6.TabIndex = 7;
            this.label6.Text = "Quality:";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // uxQualityNormal
            // 
            this.uxQualityNormal.Appearance = System.Windows.Forms.Appearance.Button;
            this.uxQualityNormal.AutoSize = true;
            this.uxQualityNormal.Location = new System.Drawing.Point(57, 4);
            this.uxQualityNormal.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.uxQualityNormal.Name = "uxQualityNormal";
            this.uxQualityNormal.Size = new System.Drawing.Size(56, 25);
            this.uxQualityNormal.TabIndex = 8;
            this.uxQualityNormal.TabStop = true;
            this.uxQualityNormal.Text = "Normal";
            this.uxQualityNormal.UseVisualStyleBackColor = true;
            // 
            // uxQuarityEconomy
            // 
            this.uxQuarityEconomy.Appearance = System.Windows.Forms.Appearance.Button;
            this.uxQuarityEconomy.AutoSize = true;
            this.uxQuarityEconomy.Location = new System.Drawing.Point(119, 4);
            this.uxQuarityEconomy.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.uxQuarityEconomy.Name = "uxQuarityEconomy";
            this.uxQuarityEconomy.Size = new System.Drawing.Size(66, 25);
            this.uxQuarityEconomy.TabIndex = 9;
            this.uxQuarityEconomy.TabStop = true;
            this.uxQuarityEconomy.Text = "Economy";
            this.uxQuarityEconomy.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.Controls.Add(this.label4);
            this.flowLayoutPanel2.Controls.Add(this.uxSize1);
            this.flowLayoutPanel2.Controls.Add(this.uxSize2);
            this.flowLayoutPanel2.Controls.Add(this.uxSize4);
            this.flowLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(200, 33);
            this.flowLayoutPanel2.TabIndex = 32;
            this.flowLayoutPanel2.WrapContents = false;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label4.Location = new System.Drawing.Point(3, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(57, 33);
            this.label4.TabIndex = 0;
            this.label4.Text = "View size:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // uxSize1
            // 
            this.uxSize1.Appearance = System.Windows.Forms.Appearance.Button;
            this.uxSize1.AutoSize = true;
            this.uxSize1.Location = new System.Drawing.Point(66, 4);
            this.uxSize1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.uxSize1.Name = "uxSize1";
            this.uxSize1.Size = new System.Drawing.Size(34, 25);
            this.uxSize1.TabIndex = 1;
            this.uxSize1.TabStop = true;
            this.uxSize1.Text = "1/1";
            this.uxSize1.UseVisualStyleBackColor = true;
            // 
            // uxSize2
            // 
            this.uxSize2.Appearance = System.Windows.Forms.Appearance.Button;
            this.uxSize2.AutoSize = true;
            this.uxSize2.Location = new System.Drawing.Point(106, 4);
            this.uxSize2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.uxSize2.Name = "uxSize2";
            this.uxSize2.Size = new System.Drawing.Size(34, 25);
            this.uxSize2.TabIndex = 2;
            this.uxSize2.TabStop = true;
            this.uxSize2.Text = "1/2";
            this.uxSize2.UseVisualStyleBackColor = true;
            // 
            // uxSize4
            // 
            this.uxSize4.Appearance = System.Windows.Forms.Appearance.Button;
            this.uxSize4.AutoSize = true;
            this.uxSize4.Location = new System.Drawing.Point(146, 4);
            this.uxSize4.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.uxSize4.Name = "uxSize4";
            this.uxSize4.Size = new System.Drawing.Size(34, 25);
            this.uxSize4.TabIndex = 3;
            this.uxSize4.TabStop = true;
            this.uxSize4.Text = "1/4";
            this.uxSize4.UseVisualStyleBackColor = true;
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
            this.tableLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.uxTimeNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.uxTimeBar)).EndInit();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel2.PerformLayout();
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
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.NumericUpDown uxTimeNumeric;
        private System.Windows.Forms.TrackBar uxTimeBar;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Label uxApproxLabel;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.RadioButton uxQualityNormal;
        private System.Windows.Forms.RadioButton uxQuarityEconomy;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.RadioButton uxSize1;
        private System.Windows.Forms.RadioButton uxSize2;
        private System.Windows.Forms.RadioButton uxSize4;
    }
}