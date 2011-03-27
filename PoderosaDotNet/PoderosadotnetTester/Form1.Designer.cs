using PoderosaDotNet;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    partial class Form1
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
            this.SuspendLayout();
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(292, 266);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(true);

            SSH2Session testsession=new SSH2Session();
            testsession.Connect();
            //this.SetTopLevel(false);
            Form _control=testsession.asForm;
            //this.TopLevelControl = true;
            _control.FormBorderStyle = FormBorderStyle.None;
            _control.Visible = true;
            _control.Dock=DockStyle.Fill;
            _control.TopLevel = false;
            this.Controls.Add(_control);

            //testsession.Connect();

            //Form _form2 = new Form();
            //SSH2Session test2 = new SSH2Session();
            //test2.Connect();
            //Form _form2=test2.asForm;
            //_form2.FormBorderStyle = FormBorderStyle.Fixed3D;
            //_form2.Visible = true;
            //_form2.Width = 300;
            //_form2.Height = 300;
           // _form2.Show();
        }

        #endregion

    }
}

