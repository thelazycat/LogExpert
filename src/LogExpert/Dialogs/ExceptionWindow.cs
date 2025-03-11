﻿using System;
using System.Drawing;
using System.Windows.Forms;

namespace LogExpert.Dialogs
{
    public partial class ExceptionWindow : Form
    {
        #region Fields

        private readonly string _errorText;
        
        private readonly string _stackTrace;

        #endregion

        #region cTor

        public ExceptionWindow(string errorText, string stackTrace)
        {
            InitializeComponent();

            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;

            _errorText = errorText;
            _stackTrace = stackTrace;
            
            stackTraceTextBox.Text = _errorText + @"\n\n" + _stackTrace;
            stackTraceTextBox.Select(0, 0);
        }

        #endregion

        #region Private Methods

        private void CopyToClipboard()
        {
            Clipboard.SetText(_errorText + @"\n\n" + _stackTrace);
        }

        #endregion

        #region Events handler

        private void copyButton_Click(object sender, EventArgs e)
        {
            CopyToClipboard();
        }

        #endregion
    }
}