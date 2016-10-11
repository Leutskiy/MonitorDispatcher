namespace Reciever
{
    partial class RecieverView
    {
        /// <summary>
        /// Требуется переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Обязательный метод для поддержки конструктора - не изменяйте
        /// содержимое данного метода при помощи редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RecieverView));
            this.picboxRecievedImage = new System.Windows.Forms.PictureBox();
            this.cntxtMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.fullScreenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.standartScreenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.timerReciever = new System.Windows.Forms.Timer(this.components);
            this.simpleTimerReciver = new ExtControlLibrary.SimpleTimer();
            this.axWMPOnlyVideo = new AxWMPLib.AxWindowsMediaPlayer();
            ((System.ComponentModel.ISupportInitialize)(this.picboxRecievedImage)).BeginInit();
            this.cntxtMenuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.axWMPOnlyVideo)).BeginInit();
            this.SuspendLayout();
            // 
            // picboxRecievedImage
            // 
            this.picboxRecievedImage.BackColor = System.Drawing.Color.DarkRed;
            this.picboxRecievedImage.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.picboxRecievedImage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picboxRecievedImage.Location = new System.Drawing.Point(0, 0);
            this.picboxRecievedImage.Name = "picboxRecievedImage";
            this.picboxRecievedImage.Size = new System.Drawing.Size(1185, 681);
            this.picboxRecievedImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picboxRecievedImage.TabIndex = 0;
            this.picboxRecievedImage.TabStop = false;
            this.picboxRecievedImage.Paint += new System.Windows.Forms.PaintEventHandler(this.picboxRecievedImage_Paint);
            this.picboxRecievedImage.Resize += new System.EventHandler(this.picboxRecievedImage_Resize);
            // 
            // cntxtMenuStrip
            // 
            this.cntxtMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fullScreenToolStripMenuItem,
            this.standartScreenToolStripMenuItem});
            this.cntxtMenuStrip.Name = "cntxtMenuStrip";
            this.cntxtMenuStrip.Size = new System.Drawing.Size(156, 48);
            // 
            // fullScreenToolStripMenuItem
            // 
            this.fullScreenToolStripMenuItem.Name = "fullScreenToolStripMenuItem";
            this.fullScreenToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
            this.fullScreenToolStripMenuItem.Text = "Full screen";
            this.fullScreenToolStripMenuItem.Click += new System.EventHandler(this.fullScreenToolStripMenuItem_Click);
            // 
            // standartScreenToolStripMenuItem
            // 
            this.standartScreenToolStripMenuItem.Name = "standartScreenToolStripMenuItem";
            this.standartScreenToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
            this.standartScreenToolStripMenuItem.Text = "Standart screen";
            this.standartScreenToolStripMenuItem.Click += new System.EventHandler(this.standartScreenToolStripMenuItem_Click);
            // 
            // timerReciever
            // 
            this.timerReciever.Enabled = true;
            this.timerReciever.Interval = 1000;
            this.timerReciever.Tick += new System.EventHandler(this.timerReciever_Tick);
            // 
            // simpleTimerReciver
            // 
            this.simpleTimerReciver.AutoSize = true;
            this.simpleTimerReciver.DeltaX = 0;
            this.simpleTimerReciver.DeltaY = 0;
            this.simpleTimerReciver.Location = new System.Drawing.Point(308, 12);
            this.simpleTimerReciver.Name = "simpleTimerReciver";
            this.simpleTimerReciver.Size = new System.Drawing.Size(543, 119);
            this.simpleTimerReciver.TabIndex = 3;
            // 
            // axWMPOnlyVideo
            // 
            this.axWMPOnlyVideo.Enabled = true;
            this.axWMPOnlyVideo.Location = new System.Drawing.Point(308, 161);
            this.axWMPOnlyVideo.Name = "axWMPOnlyVideo";
            this.axWMPOnlyVideo.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axWMPOnlyVideo.OcxState")));
            this.axWMPOnlyVideo.Size = new System.Drawing.Size(543, 468);
            this.axWMPOnlyVideo.TabIndex = 4;
            this.axWMPOnlyVideo.Visible = false;
            this.axWMPOnlyVideo.PlayStateChange += new AxWMPLib._WMPOCXEvents_PlayStateChangeEventHandler(this.axWMPOnlyVideo_PlayStateChange);
            // 
            // RecieverView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.DarkRed;
            this.ClientSize = new System.Drawing.Size(1185, 681);
            this.ContextMenuStrip = this.cntxtMenuStrip;
            this.Controls.Add(this.axWMPOnlyVideo);
            this.Controls.Add(this.simpleTimerReciver);
            this.Controls.Add(this.picboxRecievedImage);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RecieverView";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Desktop casting software - View";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.RecieverView_FormClosing);
            this.Load += new System.EventHandler(this.RecieverView_Load);
            this.Resize += new System.EventHandler(this.RecieverView_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.picboxRecievedImage)).EndInit();
            this.cntxtMenuStrip.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.axWMPOnlyVideo)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox picboxRecievedImage;
        private System.Windows.Forms.ContextMenuStrip cntxtMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem fullScreenToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem standartScreenToolStripMenuItem;
        private System.Windows.Forms.Timer timerReciever;
        private ExtControlLibrary.SimpleTimer simpleTimerReciver;
        private AxWMPLib.AxWindowsMediaPlayer axWMPOnlyVideo;
    }
}

