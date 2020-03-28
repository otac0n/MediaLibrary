namespace MediaLibrary
{
    partial class PersonSearchBox
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.searchBox = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // searchBox
            // 
            this.searchBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.searchBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.searchBox.Location = new System.Drawing.Point(0, 0);
            this.searchBox.Name = "searchBox";
            this.searchBox.Size = new System.Drawing.Size(145, 21);
            this.searchBox.TabIndex = 0;
            this.searchBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.SearchBox_DrawItem);
            this.searchBox.MeasureItem += new System.Windows.Forms.MeasureItemEventHandler(this.SearchBox_MeasureItem);
            this.searchBox.SelectionChangeCommitted += new System.EventHandler(this.SearchBox_SelectionChangeCommitted);
            this.searchBox.TextUpdate += new System.EventHandler(this.SearchBox_TextUpdate);
            // 
            // PersonSearchBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.searchBox);
            this.Name = "PersonSearchBox";
            this.Size = new System.Drawing.Size(145, 29);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox searchBox;
    }
}
