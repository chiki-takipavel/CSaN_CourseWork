using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace CourseWork_CSaN
{
    class FtpClient
    {
        FtpWebRequest ftpRequest;
        FtpWebResponse ftpResponse;
        private string host;
        private string username;
        private string password;

        public string Host { get => host; set => host = value.Trim(); }
        public string Username { get => username; set => username = value.Trim(); }
        public string Password { get => password; set => password = value.Trim(); }
        public bool UseSSL { get; set; } = false;

        /// <summary>
        /// Метод LIST для получения подробного списока файлов и каталогов на FTP-сервере
        /// </summary>
        /// <param name="path">Путь директории</param>
        /// <returns>Список файлов и каталогов</returns>
        public List<FileStruct> ListDirectory(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    path = "/";
                }

                ftpRequest = (FtpWebRequest)WebRequest.Create("ftp://" + Host + path);
                ftpRequest.Credentials = new NetworkCredential(Username, Password);
                ftpRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                ftpRequest.EnableSsl = UseSSL;
                ftpRequest.UseBinary = true;
                ftpRequest.UsePassive = true;
                ftpRequest.KeepAlive = true;

                string response;
                using (ftpResponse = (FtpWebResponse)ftpRequest.GetResponse())
                {
                    using StreamReader stream = new StreamReader(ftpResponse.GetResponseStream(), System.Text.Encoding.ASCII);
                    response = stream.ReadToEnd();
                }

                FileDirectoryParser parser = new FileDirectoryParser(response); //Парсим полученные данные
                return parser.FullList;
            }
            catch
            {
                throw new Exception("Невозможно подключиться к серверу.");
            }
        }

        /// <summary>
        /// Метод RETR для загрузки файла с FTP-сервера
        /// </summary>
        /// <param name="path">Путь директории</param>
        /// <param name="fileName">Имя файла</param>
        /// <param name="downloadPath">Путь, где будет сохранён файл</param>
        public void DownloadFile(string path, string fileName, string downloadPath)
        {
            ftpRequest = (FtpWebRequest)WebRequest.Create("ftp://" + Host + path + "/" + fileName);
            ftpRequest.Credentials = new NetworkCredential(Username, Password);
            ftpRequest.Method = WebRequestMethods.Ftp.DownloadFile;
            ftpRequest.EnableSsl = UseSSL;
            ftpRequest.UseBinary = true;
            ftpRequest.UsePassive = true;
            ftpRequest.KeepAlive = true;

            using FileStream downloadedFile = new FileStream(downloadPath, FileMode.Create, FileAccess.ReadWrite);
            using (ftpResponse = (FtpWebResponse)ftpRequest.GetResponse())
            {
                using Stream responseStream = ftpResponse.GetResponseStream();
                byte[] buffer = new byte[1024];
                int size;
                while ((size = responseStream.Read(buffer, 0, 1024)) > 0)
                {
                    downloadedFile.Write(buffer, 0, size);
                }
            }
        }

        /// <summary>
        /// Метод STOR для загрузки файла на FTP-сервер
        /// </summary>
        /// <param name="path">Путь директории</param>
        /// <param name="fileName">Имя файла</param>
        public void UploadFile(string path, string fileName)
        {
            //для имени файла
            string shortName = fileName.Remove(0, fileName.LastIndexOf("\\") + 1);

            ftpRequest = (FtpWebRequest)WebRequest.Create("ftp://" + Host + path + shortName);
            ftpRequest.Credentials = new NetworkCredential(Username, Password);
            ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;
            ftpRequest.EnableSsl = UseSSL;
            ftpRequest.UseBinary = true;
            ftpRequest.UsePassive = true;
            ftpRequest.KeepAlive = true;

            byte[] fileToBytes;
            using (FileStream uploadedFile = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                fileToBytes = new byte[uploadedFile.Length];
                uploadedFile.Read(fileToBytes, 0, fileToBytes.Length);
            }
            using Stream writer = ftpRequest.GetRequestStream();
            writer.Write(fileToBytes, 0, fileToBytes.Length);
        }

        //метод протокола FTP DELE для удаления файла с FTP-сервера 
        public void DeleteFile(string path, string fileName)
        {
            ftpRequest = (FtpWebRequest)WebRequest.Create("ftp://" + Host + path + "/" + fileName);
            ftpRequest.Credentials = new NetworkCredential(Username, Password);
            ftpRequest.Method = WebRequestMethods.Ftp.DeleteFile;
            ftpRequest.EnableSsl = UseSSL;
            ftpRequest.UseBinary = true;
            ftpRequest.UsePassive = true;
            ftpRequest.KeepAlive = true;

            using FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
        }

        //метод протокола FTP MKD для создания каталога на FTP-сервере 
        public void CreateDirectory(string path, string folderName)
        {
            ftpRequest = (FtpWebRequest)WebRequest.Create("ftp://" + Host + path + folderName);
            ftpRequest.Credentials = new NetworkCredential(Username, Password);
            ftpRequest.Method = WebRequestMethods.Ftp.MakeDirectory;
            ftpRequest.EnableSsl = UseSSL;
            ftpRequest.UseBinary = true;
            ftpRequest.UsePassive = true;
            ftpRequest.KeepAlive = true;

            using (ftpResponse = (FtpWebResponse)ftpRequest.GetResponse()) { }
        }

        //метод протокола FTP RMD для удаления каталога с FTP-сервера 
        public void RemoveDirectory(string path)
        {
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create("ftp://" + Host + path);
            ftpRequest.Credentials = new NetworkCredential(Username, Password);
            ftpRequest.Method = WebRequestMethods.Ftp.RemoveDirectory;
            ftpRequest.EnableSsl = UseSSL;
            ftpRequest.UseBinary = true;
            ftpRequest.UsePassive = true;
            ftpRequest.KeepAlive = true;

            FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
            ftpResponse.Close();
        }

        public string ParentDirectory(string path)
        {
            path.TrimEnd('/');
            if (Host.IndexOf('/') > 0)
            {
                path = path.Remove(path.LastIndexOf('/'));
            }
            else
            {
                path = "";
            }

            return path;
        }

        public string GetDirectoryPathFromHost()
        {
            string path;
            if (Host.IndexOf('/') > 0)
            {
                path = Host.Substring(Host.IndexOf('/'));
            }
            else
            {
                path = "";
            }

            return path;
        }
    }
}
