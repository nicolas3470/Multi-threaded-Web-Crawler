using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace _5412_Project
{
    public partial class SearchWindow : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public SearchWindow()
        {
            InitializeComponent();
        }

        private void searchButton_Click(object sender, EventArgs e)
        {
            string input = searchBox.Text;
            if (input.Length == 0)
            {
                ErrorLabel.Visible = true;
                searchBox.Select();
            }
            else
            {
                Program.searchString = input;
                Program.searchMade = true;
                this.Close();
            }
        }

        private void searchBox_TextChanged(object sender, EventArgs e)
        {
            if (ErrorLabel.Visible)
            {
                ErrorLabel.Visible = false;
            }
        }

        private void SearchWindow_Shown(object sender, EventArgs e)
        {
            SetForegroundWindow(this.Handle);
            searchBox.Focus();
        }

        private void searchBox_EnterPressed(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                searchButton.PerformClick();
            }
        }
    }
}
