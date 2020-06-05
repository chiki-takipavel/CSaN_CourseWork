using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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
        private const string FILE_FILTER = "Любой файл|*.*";
        private const string ERROR_STRING = "Ошибка";
        private const string NEW_DIRECTORY = "Новый каталог";

        readonly FtpClient ftp;
        readonly ObservableCollection<FileStruct> listFiles;
        private readonly Microsoft.Win32.OpenFileDialog openFileDialog;
        private readonly Microsoft.Win32.SaveFileDialog saveFileDialog;

        public MainWindow()
        {
            InitializeComponent();
            ftp = new FtpClient();
            listFiles = new ObservableCollection<FileStruct>();
            lvFiles.ItemsSource = listFiles;
            openFileDialog = new Microsoft.Win32.OpenFileDialog() { Filter = FILE_FILTER };
            saveFileDialog = new Microsoft.Win32.SaveFileDialog() { Filter = FILE_FILTER };
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
            OpenDirectoryOrDownloadFile();
        }

        private void ListViewFilesDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.All;
        }

        private void ListViewFilesDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                UploadFiles(files);
            }
        }

        private void MenuItemCreateFolderClick(object sender, RoutedEventArgs e)
        {
            CreateDirectory();
        }

        private void MenuItemUploadClick(object sender, RoutedEventArgs e)
        {
            if (openFileDialog.ShowDialog() == true)
            {
                UploadFiles(openFileDialog.FileNames);
            }
        }

        private void MenuItemOpenClick(object sender, RoutedEventArgs e)
        {
            OpenDirectoryOrDownloadFile();
        }

        private void MenuItemDeleteClick(object sender, RoutedEventArgs e)
        {
            DeleteFileOrDirectory();
        }

        private async void FtpConnect()
        {
            try
            {
                ftp.Host = tbHost.Text;
                tbHost.Text = ftp.Host;
                ftp.Username = tbLogin.Text;
                ftp.Password = tbPassword.Password;

                listFiles.Clear();
                (await Task.Run(() => ftp.ListDirectory(ftp.CurrentDirectory))).ForEach(x => listFiles.Add(x));
                tbCurrentPath.Text = string.IsNullOrWhiteSpace(ftp.CurrentDirectory) ? "/" : ftp.CurrentDirectory;
                sbStatus.MessageQueue.Enqueue(ftp.Status);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ERROR_STRING, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OpenDirectoryOrDownloadFile()
        {
            FileStruct fileStruct = (FileStruct)lvFiles.SelectedItem;
            try
            {
                string name = fileStruct.Name;
                if (fileStruct.IsDirectory)
                {
                    ftp.CurrentDirectory += "/" + name;

                    listFiles.Clear();
                    (await Task.Run(() => ftp.ListDirectory(ftp.CurrentDirectory))).ForEach(x => listFiles.Add(x));
                    tbCurrentPath.Text = string.IsNullOrWhiteSpace(ftp.CurrentDirectory) ? "/" : ftp.CurrentDirectory;
                }
                else if (name == "..")
                {
                    ftp.CurrentDirectory = ftp.ParentDirectory(ftp.CurrentDirectory);

                    listFiles.Clear();
                    (await Task.Run(() => ftp.ListDirectory(ftp.CurrentDirectory))).ForEach(x => listFiles.Add(x));
                    tbCurrentPath.Text = string.IsNullOrWhiteSpace(ftp.CurrentDirectory) ? "/" : ftp.CurrentDirectory;
                }
                else
                {
                    saveFileDialog.FileName = name;
                    if (saveFileDialog.ShowDialog() == true)
                    {
                        await Task.Run(() => ftp.DownloadFile(ftp.CurrentDirectory, name, saveFileDialog.FileName));
                        sbStatus.MessageQueue.Enqueue(ftp.Status);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ERROR_STRING, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CreateDirectory()
        {
            try
            {
                DialogWithTextBox dialog = new DialogWithTextBox { Title = NEW_DIRECTORY };
                if (dialog.ShowDialog() == true)
                {
                    string newDirectoryName = dialog.NewName;
                    if (!string.IsNullOrEmpty(newDirectoryName))
                    {
                        await Task.Run(() => ftp.CreateDirectory(ftp.CurrentDirectory, newDirectoryName));
                        sbStatus.MessageQueue.Enqueue(ftp.Status);
                        listFiles.Clear();
                        (await Task.Run(() => ftp.ListDirectory(ftp.CurrentDirectory))).ForEach(x => listFiles.Add(x));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ERROR_STRING, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void UploadFiles(string[] files)
        {
            try
            {
                await Task.Run(() => ftp.UploadFiles(ftp.CurrentDirectory, files));
                sbStatus.MessageQueue.Enqueue(ftp.Status);
                listFiles.Clear();
                (await Task.Run(() => ftp.ListDirectory(ftp.CurrentDirectory))).ForEach(x => listFiles.Add(x));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ERROR_STRING, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteFileOrDirectory()
        {
            FileStruct fileStruct = (FileStruct)lvFiles.SelectedItem;
            try
            {
                if (fileStruct.IsDirectory)
                {
                    await Task.Run(() => ftp.RemoveDirectory(ftp.CurrentDirectory, fileStruct.Name));
                }
                else
                {
                    await Task.Run(() => ftp.DeleteFile(ftp.CurrentDirectory, fileStruct.Name));
                }
                sbStatus.MessageQueue.Enqueue(ftp.Status);
                listFiles.Clear();
                (await Task.Run(() => ftp.ListDirectory(ftp.CurrentDirectory))).ForEach(x => listFiles.Add(x));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ERROR_STRING, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
