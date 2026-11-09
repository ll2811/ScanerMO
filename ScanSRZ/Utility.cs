using System;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace Scaner
{
    class Utility
    {
        const int ERROR_FILE_NOT_FOUND = 2; //ошибка - нет файла
        const int ERROR_ACCESS_DENIED = 5;  //ошибка - нет доступа

        /// <summary>
        /// Включение режима "старт при перезагрузке Windows". Добывить ключ в реестр.
        /// </summary>
        public void EnableAutoRun() {
            try
            {
                // Открываем нужную ветку в реестре
                //Microsoft.Win32.RegistryKey Key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\", true);
                Microsoft.Win32.RegistryKey Key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                //добавим ключ (первый параметр - название ключа, второй параметр - путь к исполняемому файлу нашей программы)
                Key.SetValue("VerificationAttachment", System.Reflection.Assembly.GetEntryAssembly().Location);
                Key.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка доступа к реестру. {ex.Message}");
            }
        }

        /// <summary>
        /// Выключение режима "старт при перезагрузке Windows". Удаление ключа из реестра.
        /// </summary>
        public void DisenableAutoRun() {
            try
            {
                //Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                key.DeleteValue("ScanSRZ", false);
                key.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка доступа к реестру. {ex.Message}");
            }
        }

        /// <summary>
        /// Приводит кол-во миллисекунд к формату: День Час Мин Сек.. 
        /// </summary>
        /// <param name="Milliseconds">Кол-во миллисекунд.</param>
        /// <returns>Период времени в формате: День Час Мин Сек.</returns>
        public string GetTime(double Milliseconds) {
            TimeSpan time = TimeSpan.FromMilliseconds(Milliseconds);
            //return string.Format("{0}д {1}ч {2}м {3}с", time.Days, time.Hours, time.Minutes, time.Seconds);
            return string.Format("{0} {1} {2} {3}с", (time.Days == 0) ? "" : time.Days.ToString() + "д", (time.Hours == 0) ? "" : time.Hours.ToString() + "ч", (time.Minutes == 0) ? "" : time.Minutes.ToString() + "м", time.Seconds);
        }

        /// <summary>
        /// Выполненить приложение ОС в синхронном режиме.
        /// </summary>
        /// <param name="commandText">Командная строка запуска приложения.</param>
        /// <param name="argumentText">Аргумент строки запуска приложения.</param>
        private void RunCommandExtApp(string commandText, string argumentText) {
            Process proc1 = new Process();
            try
            {
                proc1.StartInfo.CreateNoWindow = true;
                proc1.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                proc1.StartInfo.FileName = commandText;
                proc1.StartInfo.Arguments = argumentText;
                proc1.Start();
                proc1.WaitForExit();
            }
            catch (Win32Exception e)
            {
                if (e.NativeErrorCode == ERROR_FILE_NOT_FOUND)
                    MessageBox.Show($"Проверьте наличие файла.(Ошибка: {e.Message})");
                else if (e.NativeErrorCode == ERROR_ACCESS_DENIED)
                    MessageBox.Show($"Нет доступа к ресурсу.(Ошибка: {e.Message})");
            }
            finally
            {
                proc1.Dispose();
            }
        }

        /// <summary>
        /// Фомирует путь каталога, на основе выбора временного периода. 
        /// </summary>
        /// <param name="path">Основной путь.</param>
        /// <param name="timeframe">Рамки времени для построения пути каталога.</param>
        /// <returns>Путь расположения каталога.</returns>
        public string GetStorageDepth(string path, string timeframe) {
            string p = "";
            switch (timeframe)
            {
                case "Год":
                    p = string.Join(@"\", path.Substring(9, 4), @"\");
                    break;
                case "Месяц":
                    p = string.Join(@"\", path.Substring(9, 4), path.Substring(13, 2), @"\");
                    break;
                case "День":
                    p = string.Join(@"\", path.Substring(9, 4), path.Substring(13, 2), path.Substring(15, 2), @"\");
                    break;
            }
            return p;
        }

        /// <summary>
        /// Удалить файл, если есть.
        /// </summary>
        /// <param name="path">Полный путь файла.</param>
        public void FileDelete(string path) {
            if (File.Exists(path)) File.Delete(path);
        }

        /// <summary>
        /// Переместить файл, предварительно удалив, если есть в новом месте.
        /// </summary>
        /// <param name="inpFile">Полный путь нахождения файла.</param>
        /// <param name="outFile">Полный путь переноса файла.</param>
        public void FileMove(string inpFile, string outFile) {
            if (File.Exists(outFile))
                File.Delete(outFile);
            File.Move(inpFile, outFile);
        }

        /// <summary>
        /// Копировать файл, предварительно удалив, если есть в новом месте.
        /// </summary>
        /// <param name="inpFile">Полный путь нахождения файла.</param>
        /// <param name="outFile">Полный путь копирования файла.</param>
        public void FileCopy(string inpFile, string outFile) {
            if (File.Exists(outFile))
                File.Delete(outFile);
            File.Copy(inpFile, outFile);
        }

        /// <summary>
        /// Получить строку подключения к БД.
        /// </summary>
        public string GetConnectionString() { 
            return "Data Source=" + Properties.Settings.Default.NameSrvSQL + ";Initial Catalog=PeopleAttach;Integrated Security=True;Asynchronous Processing=True;Application Name=ScanSRZ";
        }

        /// <summary>
        /// Проверить доступ подключения к БД.
        /// </summary>
        public int TestConnectSQL_2() {
            Cursor tempCursor = Cursor.Current;     //запомню текущее изображение мышки
            Cursor.Current = Cursors.WaitCursor;    //сделаю изображение "ожидания"

            string connectionString = GetConnectionString() + ";Connection Timeout=1";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    return 7;
                }
                catch (Exception)
                {
                    return 8;
                }
                finally
                {
                    Cursor.Current = tempCursor;    //восстановлю изображение мышки
                }
            }
        }

        /// <summary>
        /// Проверить доступ подключения к БД.
        /// </summary>
        /// <comment>
        /// Возвращаем nullable тип, так как записей может не быть
        /// </comment>
        public async Task<int?> GetIdRecordTblAsync(string queryString) {
            SqlConnection connection = null;
            SqlCommand cmd = null;
            try
            {
                connection = new SqlConnection(GetConnectionString());
                cmd = new SqlCommand(queryString, connection);
                cmd.CommandTimeout = 30;
                await connection.OpenAsync().ConfigureAwait(false);
                object result = await cmd.ExecuteScalarAsync().ConfigureAwait(false);

                if (result == null || Convert.IsDBNull(result))
                {
                    //SetLabel10("В таблице нет записей, удовлетворяющих условию запроса.");
                    return null;
                }

                if (result is int i)
                    return i;
                // Если результат пришел как long (например, из БД BIGINT), конвертируем его
                if (result is long l && l <= int.MaxValue)
                    return (int)l;
                throw new InvalidOperationException($"Неожиданный тип данных ID: {result.GetType()}");
            }
            catch (SqlException ex)
            {
                //SetTextBox6($"GetIdRecordTbl. Ошибка SQL {queryString}. (Код: {ex.Number}): {ex.Message}");
                throw new NotImplementedException($"GetIdRecordTblAsync. Ошибка SQL {queryString}.", ex);
            }
            catch (Exception ex)
            {
                //SetTextBox6($"GetIdRecordTbl. Общая ошибка выполнения {queryString}. (Ошибка: {ex.Message})");
                throw new NotImplementedException($"GetIdRecordTblAsync. Общая ошибка выполнения {queryString}.", ex);
            }
            finally
            {
                if (cmd != null)
                    cmd.Dispose();
                if (connection != null)
                {
                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        connection.Close();
                    }
                    connection.Dispose();
                }
            }
        }

 
        /// <summary>
        /// Выполнить, в асинхронном режиме, скрипт в БД.
        /// </summary>
        /// <param name="queryString">Скрипт T-SQL.</param>
        public async Task<int> RunCommandSQL(string queryString)
        {   // процедура выполнения, в асинхронном режиме (версия 3)
            int affectedRows = 0;
            try
            {
                using (var connection = new SqlConnection(GetConnectionString()))
                using (var cmd = new SqlCommand(queryString, connection))
                {
                    // Установка разумного таймаута (например, 60 секунд)
                    cmd.CommandTimeout = 60;

                    await connection.OpenAsync().ConfigureAwait(false);

                    // Используем ExecuteNonQueryAsync вместо Begin/End
                    affectedRows = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                    return affectedRows;
                }
            }
            catch (SqlException ex)
            {
                // Логируем ошибку в файл или систему сбора телеметрии
                //LogError($"Ошибка выполнения SQL-запроса. Query: {queryString}. Error: ({ex.Number}): {ex.Message}");
                //throw new NotImplementedException($"RunCommandSQL. Ошибка выполнения SQL-запроса: {queryString}.", ex);

                // Пробрасываем исключение выше, чтобы UI слой сам решил, как его показать
                throw;
            }
        }

        private async Task StreamTextToServer(string PathInpFile)
        {
            //string fn = Path.GetFileNameWithoutExtension(Path.GetFileName(PathInpFile));    //имя файла без расширения
            //string query = "if Object_ID(N'PeopleAttach.dbo.tmp_EGIS', N'U') is not null drop table PeopleAttach.dbo.tmp_EGIS;";
            //RunCommandSQL(@query).Wait();
            //if (flag)
            //{
            //    query = "create table PeopleAttach.dbo.tmp_EGIS(csv nvarchar(max))";
            //    RunCommandSQL(@query).Wait();
            //    if (flag)
            //    {
            //        query = "insert into dbo.tmp_EGIS(csv) values(@" + fn + ")";     //SQL-запрос
            //        using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            //        {
            //            await connection.OpenAsync();
            //            using (SqlCommand cmd = new SqlCommand(query, connection))
            //            {
            //                try
            //                {
            //                    using (StreamReader file = new StreamReader(PathInpFile, Encoding.Default))
            //                    {
            //                        cmd.Parameters.Add("@" + fn + "", SqlDbType.NVarChar, -1).Value = file; //можно file.ReadToEnd()
            //                        await cmd.ExecuteNonQueryAsync();
            //                        flag = true;
            //                    }
            //                }
            //                catch (Exception ex)
            //                {
            //                    SetTextBox6(string.Format("StreamTextToServer. Файл не был обработан из-за ошибки ввода данных в БД! {0}{1}", ex.Message, DateTime.Now));
            //                    flag = false;
            //                }
            //                finally
            //                {
            //                    if (connection != null)
            //                        connection.Close();
            //                    connection.Dispose();
            //                }
            //            }
            //        }
            //    }
            //}
        }
        //public async Task<HttpWebRequest> ResponceAsync(string xmlData)
        //{
        //    string uri = @"http://example.com:80/soa-infra/services/default/";
        //    string response = "same xml-string content";
        //    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
        //    request.Method = "POST";
        //    request.ContentType = "text/xml;charset=utf-8";
        //    request.Headers["SOAPAction"] = "\"ExecuteFinancialTransaction\"";
        //    byte[] reqContent = Encoding.UTF8.GetBytes(response);
        //    request.ContentLength = reqContent.Length;
        //    using (var stream = request.GetRequestStream())
        //    {
        //        stream.Write(reqContent, 0, reqContent.Length);
        //    }
        //    await Task.Delay(Settings.AsyncCallbackDelay);
        //    return await Task.FromResult(request);
        //}


        /*
        private void GetVersionMSSQL()
        {   // процедура определения версии MSSQL
            RegistryView registryView = Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32;
            using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView))
            {
                RegistryKey instanceKey = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL", false);
                if (instanceKey != null)
                {
                    foreach (var instanceName in instanceKey.GetValueNames())
                    {
                        this.SetTextBox6(Environment.MachineName + @"" + instanceName);
                    }
                }
            }

            ServiceController[] services = ServiceController.GetServices();
            foreach (ServiceController service in services)
            {
                if (service.ServiceName.StartsWith("MSSQL", StringComparison.OrdinalIgnoreCase))
                {
            string instanceName = service.ServiceName.Contains("$") ? service.ServiceName.Split('$')[1] : "";
                    instanceName.Dump();
                }
            }
        }

        private int RunCommandSQL(string connectString, string queryString)
        {   // процедура выполнения, в асинхронном режиме (версия 1)
            SqlConnection connection = new SqlConnection(connectString);
            SqlCommand command = new SqlCommand(queryString, connection);
            try
            {
                int tk = Environment.TickCount;
                connection.Open();
                IAsyncResult result = command.BeginExecuteNonQuery(null, null);
                while (!result.IsCompleted)
                {
                    System.Threading.Thread.Sleep(1000);    // ожидание 1 секунды 
                    this.SetLabel10(string.Format("Обработка: {0}.", GetDate(Environment.TickCount - tk)));
                }
                command.EndExecuteNonQuery(result);
                return 1;
            }
            catch (Exception ex)
            {
                this.SetTextBox6(string.Format("Ошибка выполнения SQL-запроса {0}. (Ошибка: {1})", queryString, ex.Message));
                return 0;
            }
            finally
            {
                if (connection != null) connection.Close();
                command.Dispose();
            }
        }

        private void RunCommandSQL_2(string connectString, string queryString)
        { // процедура выполнения, в асинхронном режиме (версия 2)
            if (isExecutingSQL)
            {
                MessageBox.Show(this, "Уже выполняется. Пожалуйста, подождите, когда текущий запрос будет выполнен.");
            }
            else
            {
                SqlCommand command = null;
                try
                {
                    isExecutingSQL = true;
                    DisplayStatus("Подключение...");
                    connection = new SqlConnection(connectString);
                    command = new SqlCommand(queryString, connection);
                    connection.Open();
                    DisplayStatus("Выполнение...");
                    AsyncCallback callback = new AsyncCallback(HandleCallback);
                    command.BeginExecuteNonQuery(callback, command);
                }
                catch (Exception ex)
                {
                    isExecutingSQL = false;
                    DisplayStatus(string.Format("Ready (last error: {0})", ex.Message));
                    if (connection != null)
                    {
                        connection.Close();
                    }
                }
            }
        }

        private void DisplayStatus(string Text)
        {
             if (label10.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(DisplayStatus);
                Invoke(d, new object[] { Text });
            }
            else
            {
                label10.Text = label10.Text + Text;
            }
        }
        */
    }
}
