namespace Suconbu.Sumacon
{
    partial class FormPerformance
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormPerformance));
            this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
            this.uxBaseSplitContainer = new System.Windows.Forms.SplitContainer();
            this.uxProcessAndThreadSplitContainer = new System.Windows.Forms.SplitContainer();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.toolStripContainer1.ContentPanel.SuspendLayout();
            this.toolStripContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.uxBaseSplitContainer)).BeginInit();
            this.uxBaseSplitContainer.Panel1.SuspendLayout();
            this.uxBaseSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.uxProcessAndThreadSplitContainer)).BeginInit();
            this.uxProcessAndThreadSplitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStripContainer1
            // 
            // 
            // toolStripContainer1.ContentPanel
            // 
            this.toolStripContainer1.ContentPanel.Controls.Add(this.uxBaseSplitContainer);
            this.toolStripContainer1.ContentPanel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(933, 537);
            this.toolStripContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolStripContainer1.Location = new System.Drawing.Point(0, 0);
            this.toolStripContainer1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.toolStripContainer1.Name = "toolStripContainer1";
            this.toolStripContainer1.Size = new System.Drawing.Size(933, 562);
            this.toolStripContainer1.TabIndex = 0;
            this.toolStripContainer1.Text = "toolStripContainer1";
            // 
            // uxBaseSplitContainer
            // 
            this.uxBaseSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxBaseSplitContainer.Location = new System.Drawing.Point(0, 0);
            this.uxBaseSplitContainer.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.uxBaseSplitContainer.Name = "uxBaseSplitContainer";
            this.uxBaseSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // uxBaseSplitContainer.Panel1
            // 
            this.uxBaseSplitContainer.Panel1.Controls.Add(this.uxProcessAndThreadSplitContainer);
            this.uxBaseSplitContainer.Size = new System.Drawing.Size(933, 537);
            this.uxBaseSplitContainer.SplitterDistance = 355;
            this.uxBaseSplitContainer.SplitterWidth = 5;
            this.uxBaseSplitContainer.TabIndex = 0;
            // 
            // uxProcessAndThreadSplitContainer
            // 
            this.uxProcessAndThreadSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxProcessAndThreadSplitContainer.Location = new System.Drawing.Point(0, 0);
            this.uxProcessAndThreadSplitContainer.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.uxProcessAndThreadSplitContainer.Name = "uxProcessAndThreadSplitContainer";
            this.uxProcessAndThreadSplitContainer.Size = new System.Drawing.Size(933, 355);
            this.uxProcessAndThreadSplitContainer.SplitterDistance = 310;
            this.uxProcessAndThreadSplitContainer.SplitterWidth = 5;
            this.uxProcessAndThreadSplitContainer.TabIndex = 0;
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "cross.png");
            // 
            // FormPerformance
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(933, 562);
            this.Controls.Add(this.toolStripContainer1);
            this.Font = new System.Drawing.Font("Yu Gothic UI", 9F);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "FormPerformance";
            this.Text = "FormPerformance";
            this.toolStripContainer1.ContentPanel.ResumeLayout(false);
            this.toolStripContainer1.ResumeLayout(false);
            this.toolStripContainer1.PerformLayout();
            this.uxBaseSplitContainer.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.uxBaseSplitContainer)).EndInit();
            this.uxBaseSplitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.uxProcessAndThreadSplitContainer)).EndInit();
            this.uxProcessAndThreadSplitContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ToolStripContainer toolStripContainer1;
        private System.Windows.Forms.SplitContainer uxBaseSplitContainer;
        private System.Windows.Forms.SplitContainer uxProcessAndThreadSplitContainer;
        private System.Windows.Forms.ImageList imageList1;
    }
}