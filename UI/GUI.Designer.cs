namespace DofusHuntHelper
{
    partial class GUI
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
            buttonCaptureCoords = new Button();
            richTextBox1 = new RichTextBox();
            buttonStartStop = new Button();
            lblCoordinates = new Label();
            comboBoxScreens = new ComboBox();
            textBoxPort = new TextBox();
            label2 = new Label();
            label3 = new Label();
            SuspendLayout();
            // 
            // button1
            // 
            buttonCaptureCoords.Location = new Point(12, 12);
            buttonCaptureCoords.Name = "buttonCaptureCoords";
            buttonCaptureCoords.Size = new Size(119, 36);
            buttonCaptureCoords.TabIndex = 0;
            buttonCaptureCoords.Text = "Setup Coordonnée";
            buttonCaptureCoords.UseVisualStyleBackColor = true;
            buttonCaptureCoords.Click += buttonCapture_Click;
            // 
            // richTextBox1
            // 
            richTextBox1.Location = new Point(12, 157);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(776, 179);
            richTextBox1.TabIndex = 1;
            richTextBox1.Text = "";
            // 
            // button2
            // 
            buttonStartStop.Location = new Point(12, 115);
            buttonStartStop.Name = "button2";
            buttonStartStop.Size = new Size(119, 36);
            buttonStartStop.TabIndex = 2;
            buttonStartStop.Text = "Start";
            buttonStartStop.UseVisualStyleBackColor = true;
            buttonStartStop.Click += buttonStartStop_Click;
            // 
            // label1
            // 
            lblCoordinates.AutoSize = true;
            lblCoordinates.Location = new Point(12, 73);
            lblCoordinates.Name = "lblCoordinates";
            lblCoordinates.Size = new Size(84, 15);
            lblCoordinates.TabIndex = 3;
            lblCoordinates.Text = "Coordonnées: ";
            // 
            // comboBox1
            // 
            comboBoxScreens.FormattingEnabled = true;
            comboBoxScreens.Location = new Point(590, 123);
            comboBoxScreens.Name = "comboBoxScreens";
            comboBoxScreens.Size = new Size(198, 23);
            comboBoxScreens.TabIndex = 4;
            comboBoxScreens.SelectedIndexChanged += comboBoxScreens_SelectedIndexChanged;
            // 
            // textBox1
            // 
            textBoxPort.Location = new Point(688, 20);
            textBoxPort.Name = "textBoxPort";
            textBoxPort.Size = new Size(100, 23);
            textBoxPort.TabIndex = 5;
            textBoxPort.Text = "COM6";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(619, 23);
            label2.Name = "label2";
            label2.Size = new Size(63, 15);
            label2.TabIndex = 6;
            label2.Text = "Serial Port ";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(536, 126);
            label3.Name = "label3";
            label3.Size = new Size(48, 15);
            label3.TabIndex = 7;
            label3.Text = "Screen: ";
            // 
            // GUI
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 346);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(textBoxPort);
            Controls.Add(comboBoxScreens);
            Controls.Add(lblCoordinates);
            Controls.Add(buttonStartStop);
            Controls.Add(richTextBox1);
            Controls.Add(buttonCaptureCoords);
            Name = "GUI";
            Text = "GUI";
            Load += GUI_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button buttonCaptureCoords;
        private RichTextBox richTextBox1;
        private Button buttonStartStop;
        private Label lblCoordinates;
        private ComboBox comboBoxScreens;
        private Label label2;
        private Label label3;
        private TextBox textBoxPort;
    }
}