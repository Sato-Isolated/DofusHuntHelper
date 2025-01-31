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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GUI));
            buttonCaptureCoords = new Button();
            richTextBox1 = new RichTextBox();
            buttonStartStop = new Button();
            lblCoordinates = new Label();
            comboBoxScreens = new ComboBox();
            textBoxPort = new TextBox();
            label2 = new Label();
            label3 = new Label();
            KeyboardModeCheckBox = new CheckBox();
            groupBox1 = new GroupBox();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // buttonCaptureCoords
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
            richTextBox1.Location = new Point(12, 209);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(320, 179);
            richTextBox1.TabIndex = 1;
            richTextBox1.Text = "";
            // 
            // buttonStartStop
            // 
            buttonStartStop.Location = new Point(213, 12);
            buttonStartStop.Name = "buttonStartStop";
            buttonStartStop.Size = new Size(119, 36);
            buttonStartStop.TabIndex = 2;
            buttonStartStop.Text = "Start";
            buttonStartStop.UseVisualStyleBackColor = true;
            buttonStartStop.Click += buttonStartStop_Click;
            // 
            // lblCoordinates
            // 
            lblCoordinates.AutoSize = true;
            lblCoordinates.Location = new Point(50, 31);
            lblCoordinates.Name = "lblCoordinates";
            lblCoordinates.Size = new Size(84, 15);
            lblCoordinates.TabIndex = 3;
            lblCoordinates.Text = "Coordonnées: ";
            // 
            // comboBoxScreens
            // 
            comboBoxScreens.FormattingEnabled = true;
            comboBoxScreens.Location = new Point(140, 82);
            comboBoxScreens.Name = "comboBoxScreens";
            comboBoxScreens.Size = new Size(100, 23);
            comboBoxScreens.TabIndex = 4;
            comboBoxScreens.SelectedIndexChanged += comboBoxScreens_SelectedIndexChanged;
            // 
            // textBoxPort
            // 
            textBoxPort.Location = new Point(140, 53);
            textBoxPort.Name = "textBoxPort";
            textBoxPort.Size = new Size(100, 23);
            textBoxPort.TabIndex = 5;
            textBoxPort.Text = "COM6";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(71, 56);
            label2.Name = "label2";
            label2.Size = new Size(63, 15);
            label2.TabIndex = 6;
            label2.Text = "Serial Port ";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(86, 85);
            label3.Name = "label3";
            label3.Size = new Size(48, 15);
            label3.TabIndex = 7;
            label3.Text = "Screen: ";
            // 
            // KeyboardModeCheckBox
            // 
            KeyboardModeCheckBox.AutoSize = true;
            KeyboardModeCheckBox.Location = new Point(12, 54);
            KeyboardModeCheckBox.Name = "KeyboardModeCheckBox";
            KeyboardModeCheckBox.Size = new Size(110, 19);
            KeyboardModeCheckBox.TabIndex = 8;
            KeyboardModeCheckBox.Text = "Keyboard Mode";
            KeyboardModeCheckBox.UseVisualStyleBackColor = true;
            KeyboardModeCheckBox.CheckedChanged += checkBox1_CheckedChanged;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(lblCoordinates);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(textBoxPort);
            groupBox1.Controls.Add(comboBoxScreens);
            groupBox1.Location = new Point(12, 79);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(320, 124);
            groupBox1.TabIndex = 9;
            groupBox1.TabStop = false;
            // 
            // GUI
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(347, 400);
            Controls.Add(groupBox1);
            Controls.Add(KeyboardModeCheckBox);
            Controls.Add(buttonStartStop);
            Controls.Add(richTextBox1);
            Controls.Add(buttonCaptureCoords);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "GUI";
            Text = "Dofus Hunt Helper";
            Load += GUI_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
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
        private CheckBox KeyboardModeCheckBox;
        private GroupBox groupBox1;
    }
}