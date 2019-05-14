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
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.uxProcessAndThreadSplitContainer = new System.Windows.Forms.SplitContainer();
            this.toolStripContainer1.ContentPanel.SuspendLayout();
            this.toolStripContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.uxProcessAndThreadSplitContainer)).BeginInit();
            this.uxProcessAndThreadSplitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStripContainer1
            // 
            // 
            // toolStripContainer1.ContentPanel
            // 
            this.toolStripContainer1.ContentPanel.Controls.Add(this.uxProcessAndThreadSplitContainer);
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
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "cross.png");
            // 
            // uxProcessAndThreadSplitContainer
            // 
            this.uxProcessAndThreadSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxProcessAndThreadSplitContainer.Location = new System.Drawing.Point(0, 0);
            this.uxProcessAndThreadSplitContainer.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.uxProcessAndThreadSplitContainer.Name = "uxProcessAndThreadSplitContainer";
            this.uxProcessAndThreadSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.uxProcessAndThreadSplitContainer.Size = new System.Drawing.Size(933, 537);
            this.uxProcessAndThreadSplitContainer.SplitterDistance = 275;
            this.uxProcessAndThreadSplitContainer.SplitterWidth = 5;
            this.uxProcessAndThreadSplitContainer.TabIndex = 1;
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
            ((System.ComponentModel.ISupportInitialize)(this.uxProcessAndThreadSplitContainer)).EndInit();
            this.uxProcessAndThreadSplitContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ToolStripContainer toolStripContainer1;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.SplitContainer uxProcessAndThreadSplitContainer;
    }
}