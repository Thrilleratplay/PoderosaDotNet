using Poderosa.ConnectionParam;
using System;
using System.Drawing;

namespace PoderosaDotNet
{
    public interface IPoderosaDotNet
    {
        #region Event Handlers
            event EventHandler ConnectionDisconnect;
            event EventHandler ConnectionSuccess;
            event EventHandler ConnectionError;

            void ConnectionDisconnectHandler(object sender, EventArgs e);
            void ConnectionSuccessHandler(object sender, EventArgs e);
            void ConnectionErrorHandler(object sender, EventArgs e);
        #endregion

        #region Methods
            void Connect();
            void Close();
            
            void CommentLog(string comment);
            void SetLog(LogType logType, string File, bool append);

            void SetPaneColors(Color TextColor, Color BackColor);
            string GetLastLine();
            void SendText(string command);
            void CopySelectedTextToClipboard();
            void PasteTextFromClipboard();
        #endregion
    }
}