﻿using SuperControls.Style;
using SuperUtils.IO;
using System;
using System.Windows;
using System.Windows.Documents;

namespace Jvedio
{
    /// <summary>
    /// Settings.xaml 的交互逻辑
    /// </summary>
    public partial class Dialog_About : SuperControls.Style.BaseDialog
    {
        public Dialog_About(Window owner) : base(owner, false)
        {
            InitializeComponent();
        }

        private void OpenUrl(object sender, RoutedEventArgs e)
        {
            Hyperlink hyperlink = sender as Hyperlink;
            FileHelper.TryOpenUrl(hyperlink.NavigateUri.ToString(), (err) =>
            {
                MessageCard.Error(err);
            });
        }

        private void BaseDialog_ContentRendered(object sender, EventArgs e)
        {
            VersionTextBlock.Text = SuperControls.Style.LangManager.GetValueByKey("Version") + $" : {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}";
        }
    }
}