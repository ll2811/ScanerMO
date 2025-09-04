using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scaner
{
    class Utility
    {
        public string GetStorageDepth(string path, string box3)
        {//из "path" - сформировать путь к каталогу, глубиной заданной в "box3"
            string p = "";
            switch (box3)
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
