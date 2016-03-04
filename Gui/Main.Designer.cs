namespace Gui
{
    partial class Main
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing)
        {
            if ( disposing && (components != null) ) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.linkFeedLink = new System.Windows.Forms.LinkLabel();
            this.labelFeedSource = new System.Windows.Forms.Label();
            this.labelFrom = new System.Windows.Forms.Label();
            this.textBoxFeedDesc = new System.Windows.Forms.TextBox();
            this.textBoxFeedTitle = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // notifyIcon
            // 
            this.notifyIcon.Text = "RssReaderFs";
            this.notifyIcon.Visible = true;
            // 
            // linkFeedLink
            // 
            this.linkFeedLink.AutoSize = true;
            this.linkFeedLink.Font = new System.Drawing.Font("ＭＳ ゴシック", 9F);
            this.linkFeedLink.Location = new System.Drawing.Point(12, 58);
            this.linkFeedLink.Name = "linkFeedLink";
            this.linkFeedLink.Size = new System.Drawing.Size(47, 12);
            this.linkFeedLink.TabIndex = 1;
            this.linkFeedLink.TabStop = true;
            this.linkFeedLink.Text = "Open...";
            this.linkFeedLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkFeedLink_LinkClicked);
            // 
            // labelFeedSource
            // 
            this.labelFeedSource.AutoSize = true;
            this.labelFeedSource.Font = new System.Drawing.Font("游ゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelFeedSource.Location = new System.Drawing.Point(104, 56);
            this.labelFeedSource.Name = "labelFeedSource";
            this.labelFeedSource.Size = new System.Drawing.Size(90, 16);
            this.labelFeedSource.TabIndex = 2;
            this.labelFeedSource.Text = "FEED SOURCE";
            // 
            // labelFrom
            // 
            this.labelFrom.AutoSize = true;
            this.labelFrom.Font = new System.Drawing.Font("游ゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelFrom.Location = new System.Drawing.Point(65, 58);
            this.labelFrom.Name = "labelFrom";
            this.labelFrom.Size = new System.Drawing.Size(33, 16);
            this.labelFrom.TabIndex = 3;
            this.labelFrom.Text = "from";
            // 
            // textBoxFeedDesc
            // 
            this.textBoxFeedDesc.Font = new System.Drawing.Font("游ゴシック", 10F);
            this.textBoxFeedDesc.Location = new System.Drawing.Point(3, 78);
            this.textBoxFeedDesc.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxFeedDesc.Multiline = true;
            this.textBoxFeedDesc.Name = "textBoxFeedDesc";
            this.textBoxFeedDesc.ReadOnly = true;
            this.textBoxFeedDesc.Size = new System.Drawing.Size(324, 101);
            this.textBoxFeedDesc.TabIndex = 4;
            // 
            // textBoxFeedTitle
            // 
            this.textBoxFeedTitle.Font = new System.Drawing.Font("游ゴシック", 11F);
            this.textBoxFeedTitle.Location = new System.Drawing.Point(3, 0);
            this.textBoxFeedTitle.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxFeedTitle.Multiline = true;
            this.textBoxFeedTitle.Name = "textBoxFeedTitle";
            this.textBoxFeedTitle.ReadOnly = true;
            this.textBoxFeedTitle.Size = new System.Drawing.Size(324, 54);
            this.textBoxFeedTitle.TabIndex = 5;
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(331, 182);
            this.Controls.Add(this.textBoxFeedTitle);
            this.Controls.Add(this.textBoxFeedDesc);
            this.Controls.Add(this.labelFrom);
            this.Controls.Add(this.labelFeedSource);
            this.Controls.Add(this.linkFeedLink);
            this.Font = new System.Drawing.Font("游ゴシック", 9F);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "Main";
            this.Text = "RssReaderFs";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Main_FormClosed);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.LinkLabel linkFeedLink;
        private System.Windows.Forms.Label labelFeedSource;
        private System.Windows.Forms.Label labelFrom;
        private System.Windows.Forms.TextBox textBoxFeedDesc;
        private System.Windows.Forms.TextBox textBoxFeedTitle;
    }
}

