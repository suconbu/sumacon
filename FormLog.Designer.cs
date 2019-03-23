namespace Suconbu.Sumacon
{
    partial class FormLog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormLog));
            this.uxToolStripContainer = new System.Windows.Forms.ToolStripContainer();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.uxToolStrip = new System.Windows.Forms.ToolStrip();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.uxToolStripContainer.BottomToolStripPanel.SuspendLayout();
            this.uxToolStripContainer.TopToolStripPanel.SuspendLayout();
            this.uxToolStripContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // uxToolStripContainer
            // 
            // 
            // uxToolStripContainer.BottomToolStripPanel
            // 
            this.uxToolStripContainer.BottomToolStripPanel.Controls.Add(this.statusStrip1);
            // 
            // uxToolStripContainer.ContentPanel
            // 
            this.uxToolStripContainer.ContentPanel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.uxToolStripContainer.ContentPanel.Size = new System.Drawing.Size(903, 174);
            this.uxToolStripContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxToolStripContainer.Location = new System.Drawing.Point(0, 0);
            this.uxToolStripContainer.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.uxToolStripContainer.Name = "uxToolStripContainer";
            this.uxToolStripContainer.Size = new System.Drawing.Size(903, 221);
            this.uxToolStripContainer.TabIndex = 0;
            this.uxToolStripContainer.Text = "toolStripContainer1";
            // 
            // uxToolStripContainer.TopToolStripPanel
            // 
            this.uxToolStripContainer.TopToolStripPanel.Controls.Add(this.uxToolStrip);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Dock = System.Windows.Forms.DockStyle.None;
            this.statusStrip1.Location = new System.Drawing.Point(0, 0);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(903, 22);
            this.statusStrip1.TabIndex = 0;
            // 
            // uxToolStrip
            // 
            this.uxToolStrip.Dock = System.Windows.Forms.DockStyle.None;
            this.uxToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.uxToolStrip.Location = new System.Drawing.Point(3, 0);
            this.uxToolStrip.Name = "uxToolStrip";
            this.uxToolStrip.Size = new System.Drawing.Size(102, 25);
            this.uxToolStrip.TabIndex = 0;
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "arrow_up.png");
            this.imageList1.Images.SetKeyName(1, "arrow_down.png");
            this.imageList1.Images.SetKeyName(2, "flag_blue.png");
            this.imageList1.Images.SetKeyName(3, "flag_blue_back.png");
            this.imageList1.Images.SetKeyName(4, "flag_blue_go.png");
            this.imageList1.Images.SetKeyName(5, "flag_blue_delete.png");
            // 
            // FormLog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(903, 221);
            this.Controls.Add(this.uxToolStripContainer);
            this.Font = new System.Drawing.Font("Yu Gothic UI", 9F);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "FormLog";
            this.Text = "FormLog";
            this.uxToolStripContainer.BottomToolStripPanel.ResumeLayout(false);
            this.uxToolStripContainer.BottomToolStripPanel.PerformLayout();
            this.uxToolStripContainer.TopToolStripPanel.ResumeLayout(false);
            this.uxToolStripContainer.TopToolStripPanel.PerformLayout();
            this.uxToolStripContainer.ResumeLayout(false);
            this.uxToolStripContainer.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ToolStripContainer uxToolStripContainer;
        private System.Windows.Forms.ToolStrip uxToolStrip;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ImageList imageList1;
    }
}