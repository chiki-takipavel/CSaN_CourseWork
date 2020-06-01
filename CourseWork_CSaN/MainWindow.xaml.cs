using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CourseWork_CSaN
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly FtpClient ftp;
        string serverDir = "";
        readonly ObservableCollection<FileStruct> listFiles;

        public MainWindow()
        {
            InitializeComponent();
            ftp = new FtpClient();
            listFiles = new ObservableCollection<FileStruct>();
            lvFiles.ItemsSource = listFiles;
        }

        private void ButtonConnectClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbHost.Text))
            {
                MessageBox.Show("Имя сервера не может быть пустым.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            FtpConnect();
        }

        private void ListViewFilesMouseDown(object sender, MouseButtonEventArgs e)
        {
            HitTestResult result = VisualTreeHelper.HitTest(this, e.GetPosition(this));
            if (result.VisualHit.GetType() != typeof(ListViewItem))
            {
                lvFiles.SelectedItem = null;
                lvFiles.ContextMenu.Visibility = listFiles.Count != 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void ListViewFilesMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lvFiles.SelectedItem == null || e.ChangedButton != MouseButton.Left) { return; }
            FileStruct fileStruct = (FileStruct)lvFiles.SelectedItem;
            OpenDirectoryOrDownloadFile(fileStruct);
        }

        private void ListViewFilesDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
        }

        private void ListViewFilesDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length == 1)
                {
                    UploadFile(files[0]);
                }
                else
                {
                    // show error
                }
            }
        }

        private void MenuItemCreateFolderClick(object sender, RoutedEventArgs e)
        {

        }

        private void MenuItemUploadClick(object sender, RoutedEventArgs e)
        {

        }

        private void MenuItemOpenClick(object sender, RoutedEventArgs e)
        {
            if (lvFiles.SelectedItem == null) { return; }
            FileStruct fileStruct = (FileStruct)lvFiles.SelectedItem;
            OpenDirectoryOrDownloadFile(fileStruct);
        }

        private void MenuItemDeleteClick(object sender, RoutedEventArgs e)
        {

        }

        private async void FtpConnect()
        {
            try
            {
                ftp.Host = tbHost.Text;
                ftp.Username = tbLogin.Text;
                ftp.Password = tbPassword.Password;

                if (ftp.Host.IndexOf('/') > 0)
                {
                    serverDir = ftp.Host.Substring(ftp.Host.IndexOf('/'));
                }
                else
                {
                    serverDir = "";
                }
                listFiles.Clear();
                (await Task.Run(() => ftp.ListDirectory(serverDir))).ForEach(x => listFiles.Add(x));
            }
            catch (Exception ex)
            {
            }
        }

        private async void OpenDirectoryOrDownloadFile(FileStruct fileStruct)
        {
            try
            {
                string name = fileStruct.Name;
                if (fileStruct.IsDirectory)
                {
                    serverDir += "/" + name;
                    tbHost.Text = ftp.Host + serverDir;

                    listFiles.Clear();
                    (await Task.Run(() => ftp.ListDirectory(serverDir))).ForEach(x => listFiles.Add(x));
                }
                else if (name == "..")
                {
                    serverDir = ftp.ParentDirectory(serverDir);
                    tbHost.Text = ftp.Host + serverDir;

                    listFiles.Clear();
                    (await Task.Run(() => ftp.ListDirectory(serverDir))).ForEach(x => listFiles.Add(x));
                }
                else
                {
                    await Task.Run(() => ftp.DownloadFile(serverDir, name));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CreateDirectory()
        {

        }

        private async void UploadFile(string file)
        {
            await Task.Run(() => ftp.UploadFile(serverDir, Path.GetFileName(file)));
        }

        private async void DeleteFileOrDirectory(FileStruct fileStruct)
        {

        }
    }
}
