using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CourseWork_CSaN
{
    /// <summary>
    /// Структура для хранения информации о файле или каталоге
    /// </summary>
    public struct FileStruct
    {
        public string Flags { get; set; }
        public bool IsDirectory { get; set; }
        public string Owner { get; set; }
        public string FileSize { get; set; }
        public string CreateTime { get; set; }
        public string Name { get; set; }
        public string IconType { get; set; }
    }

    public enum RecordsStyle
    {
        UnixStyle,
        WindowsStyle,
        Unknown
    }

    public class FileDirectoryParser
    {
        const string NAME_FOLDER = "Каталог";
        const string TYPE_FOLDER = "Folder";
        const string TYPE_FILE = "File";

        readonly string[] sizeSuffixes = { "байт", "КБ", "МБ", "ГБ", "ТБ", "ПБ", "ЭБ", "ЗБ", "ЙБ" };
        readonly string patternWindowsStyle = @"^(?<datetime>\d+-\d+-\d+\s+\d+:\d+(?:AM|PM))\s+(?<sizeordir><DIR>|\d+)\s+(?<name>.+)$";
        readonly string windowsDateTimeFormat = "MM-dd-yy  hh:mmtt";
        readonly string patternUnixStyle = @"^(?<flags>[\w-]+)\s+(?<inode>\d+)\s+(?<owner>\w+)\s+(?<group>\w+)\s+" +
            @"(?<size>\d+)\s+(?<datetime>\w+\s+\d+\s+\d+|\w+\s+\d+\s+\d+:\d+)\s+(?<name>.+)$";
        readonly string[] unixHourMinFormats = { "MMM dd HH:mm", "MMM dd H:mm", "MMM d HH:mm", "MMM d H:mm" };
        readonly string[] unixYearFormats = { "MMM dd yyyy", "MMM d yyyy" };
        readonly IFormatProvider cultureInfo = CultureInfo.GetCultureInfo("en-us");
        Regex regexStyle;

        public List<FileStruct> FullList { get; }

        public FileDirectoryParser(string response)
        {
            FullList = GetList(response);
        }

        /// <summary>
        /// Получает из ответа список файлов и каталогов
        /// </summary>
        /// <param name="data">Данные ответа</param>
        /// <returns>Список файлов и каталогов</returns>
        private List<FileStruct> GetList(string data)
        {
            List<FileStruct> listResult = new List<FileStruct>
            {
                new FileStruct() { Name = "..", IconType = "ArrowBackCircle" }
            };
            string[] records = data.Split('\n');
            RecordsStyle style = GetRecordsStyle(records);  //Получаем стиль записей на сервере
            switch (style)
            {
                case RecordsStyle.UnixStyle:
                    regexStyle = new Regex(patternUnixStyle);
                    break;
                case RecordsStyle.WindowsStyle:
                    regexStyle = new Regex(patternWindowsStyle);
                    break;
                case RecordsStyle.Unknown:
                    return listResult;
            }
            foreach (string record in records)
            {
                if (!string.IsNullOrWhiteSpace(record))
                {
                    FileStruct fileStruct;
                    if (style == RecordsStyle.UnixStyle)
                    {
                        fileStruct = ParseFileStructUnixStyle(record);
                    }
                    else
                    {
                        fileStruct = ParseFileStructWindowsStyle(record);
                    }
                    if (fileStruct.Name != "" && fileStruct.Name != "." && fileStruct.Name != "..")
                    {
                        listResult.Add(fileStruct);
                    }
                }
            }
            return listResult;
        }

        /// <summary>
        /// Определяет операционную систему, на которой работает FTP-сервер
        /// </summary>
        /// <param name="records">Записи</param>
        /// <returns>Стиль записей</returns>
        public RecordsStyle GetRecordsStyle(string[] records)
        {
            foreach (string record in records)
            {
                if (record.Length > 10 && Regex.IsMatch(record.Substring(0, 10), "(-|d)((-|r)(-|w)(-|x)){3}"))
                {
                    return RecordsStyle.UnixStyle;
                }
                else if (record.Length > 8 && Regex.IsMatch(record.Substring(0, 8), "[0-9]{2}-[0-9]{2}-[0-9]{2}"))
                {
                    return RecordsStyle.WindowsStyle;
                }
            }
            return RecordsStyle.Unknown;
        }

        /// <summary>
        /// Парсинг записи, если FTP-сервер работает на Windows (WindowsStyle)
        /// </summary>
        /// <param name="record">Запись</param>
        /// <returns>Структура файла</returns>
        private FileStruct ParseFileStructWindowsStyle(string record)
        {
            FileStruct fileStruct = new FileStruct();
            Match match = regexStyle.Match(record);
            fileStruct.CreateTime = DateTime.ParseExact(match.Groups["datetime"].Value, windowsDateTimeFormat,
                cultureInfo, DateTimeStyles.None).ToString("f");
            if (match.Groups["sizeordir"].Value == "<DIR>")
            {
                fileStruct.IsDirectory = true;
                fileStruct.FileSize = NAME_FOLDER;
                fileStruct.IconType = TYPE_FOLDER;
            }
            else
            {
                fileStruct.IsDirectory = false;
                fileStruct.FileSize = AddSizeSuffix(long.Parse(match.Groups["sizeordir"].Value));
                fileStruct.IconType = TYPE_FILE;
            }
            fileStruct.Name = match.Groups["name"].Value.Trim();

            return fileStruct;
        }

        /// <summary>
        /// Парсинг записи, если FTP-сервер работает на Unix (UnixStyle)
        /// </summary>
        /// <param name="record">Запись</param>
        /// <returns>Структура файла</returns>
        private FileStruct ParseFileStructUnixStyle(string record)
        {
            FileStruct fileStruct = new FileStruct();
            Match match = regexStyle.Match(record);
            fileStruct.Flags = match.Groups["flags"].Value;
            fileStruct.IsDirectory = fileStruct.Flags[0] == 'd';
            fileStruct.Owner = match.Groups["owner"].Value;
            if (!fileStruct.IsDirectory)
            {
                fileStruct.FileSize = AddSizeSuffix(long.Parse(match.Groups["size"].Value, cultureInfo));
                fileStruct.IconType = TYPE_FILE;
            }
            else
            {
                fileStruct.FileSize = NAME_FOLDER;
                fileStruct.IconType = TYPE_FOLDER;
            }
            string tempDateTime = Regex.Replace(match.Groups["datetime"].Value, @"\s+", " ");
            if (tempDateTime.IndexOf(':') >= 0)
            {
                fileStruct.CreateTime = DateTime.ParseExact(tempDateTime, unixHourMinFormats, cultureInfo, DateTimeStyles.None).ToString("f");
            }
            else
            {
                fileStruct.CreateTime = DateTime.ParseExact(tempDateTime, unixYearFormats, cultureInfo, DateTimeStyles.None).ToString("f");
            }
            fileStruct.Name = match.Groups["name"].Value.Trim();

            return fileStruct;
        }

        /// <summary>
        /// Переводит количество байт в КБ, МБ, ГБ и т.д.
        /// </summary>
        /// <param name="value">Размер в байтах</param>
        /// <returns>Размер с суффиксом</returns>
        private string AddSizeSuffix(long value)
        {
            if (value < 0) { return "-" + AddSizeSuffix(-value); }
            if (value == 0) { return string.Format("{0:n0} {1}", 0, sizeSuffixes[0]); }

            int mag = (int)Math.Log(value, 1024);
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));
            if (Math.Round(adjustedSize) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n0} {1}", adjustedSize, sizeSuffixes[mag]);
        }
    }
}
