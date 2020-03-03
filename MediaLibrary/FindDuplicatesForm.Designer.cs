namespace MediaLibrary
{
    partial class FindDuplicatesForm
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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.duplicatesList = new System.Windows.Forms.ListView();
            this.pathHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.instructionsLabel = new System.Windows.Forms.Label();
            this.titleLabel = new System.Windows.Forms.Label();
            this.sizeChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            ((System.ComponentModel.ISupportInitialize)(this.sizeChart)).BeginInit();
            this.SuspendLayout();
            // 
            // duplicatesList
            // 
            this.duplicatesList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.duplicatesList.CheckBoxes = true;
            this.duplicatesList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.pathHeader});
            this.duplicatesList.Enabled = false;
            this.duplicatesList.HideSelection = false;
            this.duplicatesList.Location = new System.Drawing.Point(12, 105);
            this.duplicatesList.Name = "duplicatesList";
            this.duplicatesList.Size = new System.Drawing.Size(776, 304);
            this.duplicatesList.TabIndex = 0;
            this.duplicatesList.UseCompatibleStateImageBehavior = false;
            this.duplicatesList.View = System.Windows.Forms.View.Details;
            this.duplicatesList.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.DuplicatesList_ItemChecked);
            // 
            // pathHeader
            // 
            this.pathHeader.Text = "Path";
            this.pathHeader.Width = 200;
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(620, 415);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 1;
            this.cancelButton.Text = "&Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // okButton
            // 
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.Enabled = false;
            this.okButton.Location = new System.Drawing.Point(713, 415);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 2;
            this.okButton.Text = "&OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Location = new System.Drawing.Point(13, 415);
            this.progressBar.Maximum = 1000;
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(601, 23);
            this.progressBar.TabIndex = 3;
            this.progressBar.Visible = false;
            // 
            // instructionsLabel
            // 
            this.instructionsLabel.AutoSize = true;
            this.instructionsLabel.Location = new System.Drawing.Point(12, 43);
            this.instructionsLabel.Name = "instructionsLabel";
            this.instructionsLabel.Size = new System.Drawing.Size(296, 26);
            this.instructionsLabel.TabIndex = 4;
            this.instructionsLabel.Text = "Select the items in each group you wish to keep.\r\nIf you do not select any items " +
    "in a group, all items will be kept.";
            // 
            // titleLabel
            // 
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.titleLabel.Location = new System.Drawing.Point(12, 9);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(162, 26);
            this.titleLabel.TabIndex = 5;
            this.titleLabel.Text = "Find Duplicates";
            // 
            // sizeChart
            // 
            this.sizeChart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.sizeChart.BackColor = System.Drawing.Color.Transparent;
            chartArea1.BackColor = System.Drawing.Color.Transparent;
            chartArea1.Name = "pieArea";
            this.sizeChart.ChartAreas.Add(chartArea1);
            legend1.BackColor = System.Drawing.Color.Transparent;
            legend1.MaximumAutoSize = 75F;
            legend1.Name = "pieLegend";
            this.sizeChart.Legends.Add(legend1);
            this.sizeChart.Location = new System.Drawing.Point(433, 9);
            this.sizeChart.Name = "sizeChart";
            series1.ChartArea = "pieArea";
            series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Doughnut;
            series1.CustomProperties = "PieLabelStyle=Disabled";
            series1.Legend = "pieLegend";
            series1.Name = "pieSeries";
            this.sizeChart.Series.Add(series1);
            this.sizeChart.Size = new System.Drawing.Size(354, 90);
            this.sizeChart.TabIndex = 6;
            this.sizeChart.Text = "Size";
            // 
            // FindDuplicatesForm
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.sizeChart);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.instructionsLabel);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.duplicatesList);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FindDuplicatesForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Find Duplicates";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FindDuplicatesForm_FormClosing);
            this.Load += new System.EventHandler(this.FindDuplicatesForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.sizeChart)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView duplicatesList;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.ColumnHeader pathHeader;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label instructionsLabel;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.DataVisualization.Charting.Chart sizeChart;
    }
}
