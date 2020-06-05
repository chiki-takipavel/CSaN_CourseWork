using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace CourseWork_CSaN
{
    class FtpClient
    {
        const int bufferSize = 1024;

        private FtpWebRequest ftpRequest;
        private FtpWebResponse ftpResponse;
        private string scheme;
        private string host;
        private string username;
        private string password;
        private string status;

        public string Host { get => host; set => host = ParseUri(value.Trim()); }
        public string Username { get => username; set => username = value.Trim(); }
        public string Password { get => password; set => password = value.Trim(); }
        public string Status { get => status; set => status = value.Trim(); }
        public string CurrentDirectory { get; set; }
        public bool UseSSL { get; set; } = false;

        /// <summary>
        /// Метод LIST для получения подробного списка файлов и каталогов на FTP-сервере
        /// </summary>
        /// <param name="path">Путь директории</param>
        /// <returns>Список файлов и каталогов</returns>
        public List<FileStruct> ListDirectory(string path)
        {
            try
            {
                path = string.IsNullOrWhiteSpace(path) ? "/" : path;
                ftpRequest = (FtpWebRequest)WebRequest.Create(Uri.EscapeUriString(scheme + Host + path));
                ftpRequest.Credentials = new NetworkCredential(Username, Password);
                ftpRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                ftpRequest.EnableSsl = UseSSL;
                ftpRequest.UseBinary = true;
                ftpRequest.UsePassive = true;
                ftpRequest.KeepAlive = true;

                string response;
                using (ftpResponse = (FtpWebResponse)ftpRequest.GetResponse())
                {
                    using StreamReader stream = new StreamReader(ftpResponse.GetResponseStream(), Encoding.ASCII);
                    response = stream.ReadToEnd();
                    Status = ftpResponse.StatusDescription;
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
            try
            {
                ftpRequest = (FtpWebRequest)WebRequest.Create(Uri.EscapeUriString(scheme + Host + path + "/" + fileName));
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
                    while ((size = responseStream.Read(buffer, 0, bufferSize)) > 0)
                    {
                        downloadedFile.Write(buffer, 0, size);
                    }
                    Status = ftpResponse.StatusDescription;
                }
            }
            catch
            {
                throw new Exception("Невозможно скачать файл с сервера.");
            }
        }

        /// <summary>
        /// Метод STOR для загрузки файла на FTP-сервер
        /// </summary>
        /// <param name="path">Путь директории</param>
        /// <param name="fileName">Имя файла</param>
        public void UploadFiles(string path, string[] fileNames)
        {
            try
            {
                byte[] fileContents;
                foreach (string fileName in fileNames)
                {
                    ftpRequest = (FtpWebRequest)WebRequest.Create(Uri.EscapeUriString(scheme + Host + path + "/" + Path.GetFileName(fileName)));
                    ftpRequest.Credentials = new NetworkCredential(Username, Password);
                    ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;
                    ftpRequest.EnableSsl = UseSSL;
                    ftpRequest.UseBinary = true;
                    ftpRequest.UsePassive = true;
                    ftpRequest.KeepAlive = true;

                    using (StreamReader uploadedFile = new StreamReader(fileName))
                    {
                        fileContents = Encoding.UTF8.GetBytes(uploadedFile.ReadToEnd());
                    }
                    using (Stream requestStream = ftpRequest.GetRequestStream())
                    {
                        requestStream.Write(fileContents, 0, fileContents.Length);
                    }
                    using (ftpResponse = (FtpWebResponse)ftpRequest.GetResponse())
                    {
                        Status = ftpResponse.StatusDescription;
                    }
                }
            }
            catch
            {
                throw new Exception("Невозможно загрузить файлы на сервер.");
            }
        }

        /// <summary>
        /// Метод DELE для удаления файла с FTP-сервера
        /// </summary>
        /// <param name="path"></param>
        /// <param name="fileName"></param>
        public void DeleteFile(string path, string fileName)
        {
            try
            {
                ftpRequest = (FtpWebRequest)WebRequest.Create(Uri.EscapeUriString(scheme + Host + path + "/" + fileName));
                ftpRequest.Credentials = new NetworkCredential(Username, Password);
                ftpRequest.Method = WebRequestMethods.Ftp.DeleteFile;
                ftpRequest.EnableSsl = UseSSL;
                ftpRequest.UseBinary = true;
                ftpRequest.UsePassive = true;
                ftpRequest.KeepAlive = true;

                using (ftpResponse = (FtpWebResponse)ftpRequest.GetResponse())
                {
                    Status = ftpResponse.StatusDescription;
                }
            }
            catch
            {
                throw new Exception("Невозможно удалить файл на сервере.");
            }
        }

        /// <summary>
        /// Метод MKD для создания каталога на FTP-сервере
        /// </summary>
        /// <param name="path">Путь директории</param>
        /// <param name="folderName">Имя создаваемого каталога</param>
        public void CreateDirectory(string path, string folderName)
        {
            try
            {
                ftpRequest = (FtpWebRequest)WebRequest.Create(Uri.EscapeUriString(scheme + Host + path + "/" + folderName));
                ftpRequest.Credentials = new NetworkCredential(Username, Password);
                ftpRequest.Method = WebRequestMethods.Ftp.MakeDirectory;
                ftpRequest.EnableSsl = UseSSL;
                ftpRequest.UseBinary = true;
                ftpRequest.UsePassive = true;
                ftpRequest.KeepAlive = true;

                using (ftpResponse = (FtpWebResponse)ftpRequest.GetResponse())
                {
                    Status = ftpResponse.StatusDescription;
                }
            }
            catch
            {
                throw new Exception("Невозможно создать каталог на сервере.");
            }
        }

        /// <summary>
        /// Метод RMD для удаления каталога с FTP-сервера
        /// </summary>
        /// <param name="path">Путь директории</param>
        /// <param name="folderName">Имя каталога для удаления</param>
        public void RemoveDirectory(string path, string folderName)
        {
            try
            {
                ftpRequest = (FtpWebRequest)WebRequest.Create(Uri.EscapeUriString(scheme + Host + path + "/" + folderName));
                ftpRequest.Credentials = new NetworkCredential(Username, Password);
                ftpRequest.Method = WebRequestMethods.Ftp.RemoveDirectory;
                ftpRequest.EnableSsl = UseSSL;
                ftpRequest.UseBinary = true;
                ftpRequest.UsePassive = true;
                ftpRequest.KeepAlive = true;

                using (ftpResponse = (FtpWebResponse)ftpRequest.GetResponse())
                {
                    Status = ftpResponse.StatusDescription;
                }
            }
            catch
            {
                throw new Exception("Невозможно скачать файл с сервера.");
            }
        }

        /// <summary>
        /// Получение схемы, адреса хоста и текущего пути из URI
        /// </summary>
        /// <param name="uriString">URI</param>
        /// <returns>Адрес хоста</returns>
        private string ParseUri(string uriString)
        {
            if (Uri.TryCreate(host, UriKind.Absolute, out Uri uri))
            {
                scheme = (uri.Scheme == Uri.UriSchemeFtp) ? uri.Scheme : string.Empty;
                CurrentDirectory = uri.AbsolutePath;
                return uri.GetComponents(UriComponents.HostAndPort, UriFormat.UriEscaped);
            }
            else
            {
                scheme = Uri.UriSchemeFtp + Uri.SchemeDelimiter;
                int pathPos = uriString.IndexOf('/');
                if (pathPos >= 0)
                {
                    CurrentDirectory = uriString.Substring(pathPos);
                    uriString = uriString.Remove(pathPos);
                }
                else
                {
                    CurrentDirectory = string.Empty;
                }
                if (Uri.CheckHostName(uriString) == UriHostNameType.Unknown)
                    uriString = string.Empty;
                return uriString;
            }
        }

        /// <summary>
        /// Возвращает путь родительского каталога
        /// </summary>
        /// <param name="path">Путь каталога</param>
        /// <returns>Путь родительского каталога</returns>
        public string GetParentDirectory(string path)
        {
            path.TrimEnd('/');
            if (path.LastIndexOf('/') >= 0)
            {
                path = path.Remove(path.LastIndexOf('/'));
            }
            else
            {
                path = string.Empty;
            }

            return path;
        }
    }
}
