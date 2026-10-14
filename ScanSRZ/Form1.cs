using System;
using System.IO;
//using System.Diagnostics;
//using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Data.Odbc;
using System.Data.SqlClient;
//using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
//using ICSharpCode.SharpZipLib.Zip;
//using System.ServiceModel;
//using System.Reflection;
//using System.Collections.Generic;
//using System.ServiceProcess;
//using Microsoft.Win32;
//using Microsoft.VisualBasic.FileIO;

//test - ветка
[assembly: CLSCompliant(true)]
namespace Scaner
{
    public partial class Form1 : Form
    {
        const int ERROR_FILE_NOT_FOUND = 2; //ошибка - нет файла
        const int ERROR_ACCESS_DENIED = 5;  //ошибка - нет доступа
        string[] errList = {/*( 0)*/"Указанный каталог не существует или не доступен!\nБудет восстановлен каталог по умолчанию\n, или укажите верный новый путь.",
                            /*( 1)*/"Выполняется обработка! Пожалуйста, дождитесь завершения.",
                            /*( 2)*/"Для записи файла, на устройстве недостаточно места!",
                            /*( 3)*/"Произошла ошибка выполнения модуля",
                            /*( 4)*/"Ошибка. Не удалось открыть файл.",
                            /*( 5)*/"Неверное значение параметра! Установлено значение по умолчанию.",
                            /*( 6)*/"Для записи файла, на устройстве недостаточно места!",
                            /*( 7)*/"Успешное подключение к серверу MSSQL.",
                            /*( 8)*/"Не удалось выполнить подключение к серверу MSSQL.\nПроверьте его работоспособность и сетевую доступность.",
                            /*( 9)*/"Активация выгрузки данных",
                            /*(10)*/"Активирована выгрузка из СРЗ персональных сведений ЗЛ",
                            /*(11)*/"Ошибка обработки.",
                            /*(12)*/"был обработан ранее.",
                            /*(13)*/"не соответствует требованиям норматива по количеству записей.",
                            /*(14)*/"отсутствует в каталоге.",
                            /*(15)*/"размера файла не соответствует минимальному значению(с учётом количества строк и обязательных сведений).",
                            /*(16)*/"недокументированная ошибка. Обратитесь к разработчику ПО.",
                            /*(17)*/"не был обработан из-за ошибки при вводе данных в БД",
                            /*(18)*/"содержит ошибки не позволяющие его обработку."
        };
        //private enum FormMode {setting=1, protocol, tols}; //текущий режим формы
        //private byte formMode=1;            //текущий режим формы
        private int start;                  //
        private int finish;                 //
        private int takt;                   //
        //private static Guid g;              //ГУИД
        private bool flag = false;          //флаг выполнения опреации

        delegate string TextCallback(string text);
        delegate void SetTextCallback(string text);
        delegate string GetTextCallback();
        delegate void SetTextControlCallback(Button button, TaskInfo ti);

        private Thread WorkupThread = null;
        private Thread ScanerThread = null;
        private Thread SMOThread = null;
        private Thread FSBThread = null;

        private bool isExecHand;            //флаг обработки файлов MO,SZ,QA,PZ,18000-MED_DR
        private bool isExecScaner;          //флаг запуска сканера
        private bool isExecHand_SMO;        //флаг обработки файлов SMO
        private bool isExecHand_FSB;        //флаг обработки файлов FSB

        TestFile tf = new TestFile { };
        Zip zp = new Zip { };
        Utility ut = new Utility { };

        SkanFileInfo sf = new SkanFileInfo
        {
            nameSrvSQL = @"",
            inpDir = @"",
            outDir = @"",
            arcDir = @"",
            storageDepth = @"",
            arcInpDir = @"",
            arcOutDir = @"",
            tmpDir = Environment.GetEnvironmentVariable("TEMP"),
            pathBCP = @"",
            fName=@"",
            fNameArc =@"",
            errNumber = 0,
            errCount = 0,
            errTest = ""
        };

        public Form1()
        {
            InitializeComponent();
            //string s;// = System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
            this.Text = "Сканер для СРЗ (версия " + Application.ProductVersion.ToString() + ").";
            //Text = "Сканер файлов по прикреплению ЗЛ к МО 5.(" + System.Windows.Forms.Application.ProductVersion.ToString() + ").";

        }
        private void Form1_Load(object sender, EventArgs e)
        {
            //string Text = "Сканер для СРЗ (версия " + System.Windows.Forms.Application.ProductVersion.ToString() + ")";

            if (checkBox2.Checked)
            {
                ActivationScaner();
                //FormMinimized();
            }
            WriteTextFile(this.textBox3.Text + @"scanlog.csv", string.Format("Запуск программы {0}(v{1}-{2}-{3})", DateTime.Now, System.Windows.Forms.Application.ProductVersion.ToString(), Environment.MachineName, Environment.UserName));
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Properties.Settings.Default.Save();
            WriteTextFile(this.textBox3.Text + @"scanlog.csv", string.Format("Закрытие программы {0}({1}-{2})", DateTime.Now, Environment.MachineName, Environment.UserName));
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isExecScaner | isExecHand | isExecHand_SMO | isExecHand_FSB)
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    if (isExecScaner)
                        MessageBox.Show("Сканер активирован. Отключите режим активности сканера и закройте программу!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    if (isExecHand | isExecHand_SMO | isExecHand_FSB)
                        MessageBox.Show("Выполняется обработка данных. Дождитесь её завершения, а затем закройте программу!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
            if (altF4)
            {
                if (e.CloseReason == CloseReason.UserClosing)
                    e.Cancel = true;
                altF4 = false;
            }
        }
        private bool altF4 = false;

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Alt && e.KeyCode == Keys.F4)
            {
                MessageBox.Show("Данная команда отключена!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                altF4 = true;
            }
        }
        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide(); // прячем форму
                WindowState = FormWindowState.Minimized;
                ShowInTaskbar = false;
                notifyIcon1.Visible = true;
                if (isExecScaner)
                {
                    if (!isExecHand)
                        notifyIcon1.Icon = Properties.Resources.Icon2;
                    notifyIcon1.Text = "Сверка прикрепления. (сканер включен с " + this.comboBox1.Text + " до " + this.comboBox2.Text + " часов)";
                    notifyIcon1.BalloonTipText = "Сканер активирован с " + this.comboBox1.Text + " до " + this.comboBox2.Text + " часов.";
                    notifyIcon1.ShowBalloonTip(500);
                }
                else
                {
                    if (!isExecHand)
                        notifyIcon1.Icon = Properties.Resources.Icon1;
                    notifyIcon1.Text = "Сверка прикрепления. (сканер выключен)";
                    notifyIcon1.BalloonTipText = "Внимание! Сканер выключен.";
                    notifyIcon1.ShowBalloonTip(500);
                }
            }
        }
        private void NotifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Show(); // показываем форму
                WindowState = FormWindowState.Normal;
                ShowInTaskbar = true;
                notifyIcon1.Visible = false;
            }
        }
        private void SetLabel10(string text)
        {   // Если вызывающий поток отличается от потока, который создал элемент управления textbox,
            // то этот метод создает SetTextCallback и вызывает себя асинхронно, используя метод invoke.
            // Если вызывающий поток аналогичен потоку, который создал элемент управления textbox свойство text устанавливается напрямую. 
            // Свойство InvokeRequired выполняет сравнение ID потоков, вызывающего потока и потока создавшим поток. Если потоки разные возвращает "истину".
            if (label10.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetLabel10);
                Invoke(d, new object[] { text });
            }
            else
            {
                label10.Text = text;
            }
        }

        private void SetTextBox6(string text)
        {
            if (textBox6.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetTextBox6);
                Invoke(d, new object[] { text });
            }
            else
            {
                textBox6.AppendText(text + Environment.NewLine);
                WriteTextFile(textBox3.Text + @"scanlog.csv", text);
            }
        }

        //private string GetComboBox1()
        //{
        //    if (comboBox1.InvokeRequired)
        //    {
        //        GetTextCallback d = new GetTextCallback(GetComboBox1);
        //        Invoke(d, new object[] { });
        //        return comboBox1.Text;
        //    }
        //    else
        //    {
        //        return comboBox1.Text;
        //    }
        //}
        //private string GetComboBox2()
        //{
        //    if (comboBox2.InvokeRequired)
        //    {
        //        GetTextCallback d = new GetTextCallback(GetComboBox2);
        //        Invoke(d, new object[] { });
        //        return comboBox2.Text;
        //    }
        //    else
        //    {
        //        return comboBox2.Text;
        //    }
        //}
        private string GetComboBox3()
        {
            if (comboBox3.InvokeRequired)
            {
                GetTextCallback d = new GetTextCallback(GetComboBox3);
                Invoke(d, new object[] { });
                return comboBox3.Text;
            }
            else
            {
                return comboBox3.Text;
            }
        }

        private string GetConnectionString()
        {   /* формирование строки подключения */
            //return "Data Source=" + sf.nameSrvSQL + ";Initial Catalog=PeopleAttach;Integrated Security=True;Asynchronous Processing=True";
            //return global::VerificationAttachment.Properties.Settings.Default.ConnectDB;
            //string strSQL = "Data Source=" + Properties.Settings.Default.NameSrvSQL + ";Initial Catalog=PeopleAttach;Integrated Security=True;Asynchronous Processing=True;Application Name=ScanSRZ";
            //return "Data Source=" + sf.nameSrvSQL + ";Initial Catalog=PeopleAttach;Integrated Security=True;Asynchronous Processing=True;Application Name=ScanSRZ";
            return "Data Source=" + sf.nameSrvSQL + ";Initial Catalog=PeopleAttach;Integrated Security=True;Application Name=ScanSRZ";
        }
        private int GetIdRecordTbl(string queryString)
        {   // получить значение первого поля первой записи в таблице
            int id = 0;
            string sql = queryString;
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                SqlCommand cmd = new SqlCommand(sql, connection);
                try
                {
                    connection.Open();
                    id = (int)cmd.ExecuteScalar();
                }
                catch (NullReferenceException)
                {
                    SetLabel10("Получен новый файл! В указанной таблице нет записей удовлетворяющих условию запроса.");
                }
                catch (Exception ex)
                {
                    SetTextBox6($"GetIdRecordTbl. Ошибка выполнения SQL-запроса {queryString}. (Ошибка: {ex.Message})");
                }
                finally
                {
                    if (connection != null) connection.Close();
                    cmd.Dispose();
                }
            }
            return id;
        }
        private async Task RunCommandSQL(string connectString, string queryString)
        {   // процедура выполнения, в асинхронном режиме (версия 3)
            using (SqlConnection connection = new SqlConnection(connectString))
            {
                await connection.OpenAsync();
                using (SqlCommand cmd = new SqlCommand(queryString, connection))
                {
                    try
                    {
                        int _tk = Environment.TickCount;
                        //connection.Open();
                        IAsyncResult result = cmd.BeginExecuteNonQuery(null, null);
                        while (!result.IsCompleted)
                        {
                            //System.Threading.Thread.Sleep(1000);    // ожидание 1 секунды 
                            Thread.CurrentThread.Join(1000);
                            SetLabel10($"Обработка SQL-запроса: {ut.GetTime(Environment.TickCount - _tk)}.");
                        }
                        cmd.EndExecuteNonQuery(result);
                        flag = true;
                    }
                    catch (SqlException ex)
                    {
                        SetTextBox6($"RunCommandSQL. Ошибка выполнения SQL-запроса {queryString}. (Ошибка: ({ex.Number}): {ex.Message})");
                        flag = false;
                    }
                    catch (InvalidOperationException ex)
                    {
                        SetTextBox6($"Ошибка: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        SetTextBox6($"Ошибка: {ex.Message}");
                    }
                    finally
                    {
                        if (connection != null) connection.Close();
                        cmd.Dispose();
                    }
                }
            }
        }
        private async Task StreamBLOBToServer(string PathInpFile, Guid g)
        {   //загрузка двоичного файла в SQL
            string query= "insert into DBO.MO_FS (id, FileName, FileSaver) values ('" + g + "','" + Path.GetFileName(PathInpFile.Replace("-", "")).ToUpper() + "', @bindata)";
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                await connection.OpenAsync();
                //using (SqlCommand cmd = new SqlCommand("insert into DBO.BinaryStreams (FileName, FileSaver) values ('" + Path.GetFileName(PathInpFile).ToUpper() + "', @bindata)", conn))
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        using (FileStream file = File.Open(PathInpFile, FileMode.Open))
                        {
                            cmd.Parameters.Add("@bindata", SqlDbType.Binary, -1).Value = file;
                            await cmd.ExecuteNonQueryAsync();
                            flag = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.SetTextBox6($"StreamBLOBToServer. Ошибка записи данных в таблицу! {ex.Message}{DateTime.Now}");
                        flag = false;
                    }
                    finally
                    {
                        //if (connection.State == System.Data.ConnectionState.Open) connection.Close();
                        if (connection != null) connection.Close();
                        connection.Dispose();
                    }
                }
            }
        }
 
        private async Task StreamTextToServer(string PathInpFile, Guid g)
        {   // загрузка файла (XML или CSV) в таблицу
            string fn = Path.GetFileNameWithoutExtension(Path.GetFileName(PathInpFile));    //имя файла без расширения
            string fe = Path.GetExtension(PathInpFile);                                     //расширение файла
            string field = (fe == ".xml") ? "FQ_xml" : "FQ_csv";                            //выбор поля
            string query = "insert into dbo.MO_FQ(id, NameFQ, " + field + ") values('" + g + "','" + Path.GetFileName(PathInpFile.Replace("-", "")).ToUpper() + "', @" + fn + ")";     //SQL-запрос

            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                await connection.OpenAsync();
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        using (StreamReader file = new StreamReader(PathInpFile, Encoding.Default))
                        {
                            // Add a parameter which uses the StreamReader we just opened  (Добавим параметр, который использует StreamReader, который мы только что открыли)
                            // Size is set to -1 to indicate "MAX"  (Размер установлен в -1, чтобы указать "MAX")
                            cmd.Parameters.Add("@" + fn + "", (fe == "xml") ? SqlDbType.Xml : SqlDbType.NVarChar, -1).Value = file; //можно file.ReadToEnd()
                            await cmd.ExecuteNonQueryAsync();
                            flag = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        SetTextBox6($"StreamTextToServer. Файл не был обработан из-за ошибки ввода данных в БД! {ex.Message}{DateTime.Now}");
                        flag = false;
                    }
                    finally
                    {
                        //if (connection.State == System.Data.ConnectionState.Open) connection.Close();
                        if (connection != null)
                            connection.Close();
                        connection.Dispose();
                    }
                }
            }
        }
        private bool WriteTextFile(string PathOutFile, string text)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(PathOutFile, true, System.Text.Encoding.Default))
                {
                    sw.WriteLine(text);
                    return false;
                }
            }
            catch (Exception ex)
            {
                SetTextBox6($"WriteTextFile. Ошибка записи в файл: '{PathOutFile}'! {ex.Message}{DateTime.Now}");
                return true;
            }
        }
        private void OwnedToMO_CSV(string PathOutFile, string NameTbl, string FieldList)
        {   // в CSV-файл, из таблицы <NameTbl> выгрузка данных из поля [TXT]. Необязательный параметр <FieldList> - перечень полей содержащихся в поле [TXT], для совместимости со стандартом CSV-формата
            if (File.Exists(PathOutFile)) File.Delete(PathOutFile);

            string query;   //запрос
            int rowcnt;   //кол-во строк в таблице
            int sizeTbl;  //объём данных в байтах + 2байта(/r/n) на строку

            StreamWriter file = new StreamWriter(PathOutFile, false, Encoding.GetEncoding("windows-1251"));
            double Ffree = new DriveInfo(new DriveInfo(PathOutFile).RootDirectory.ToString()).AvailableFreeSpace;
            //double Ffree = new DriveInfo(new DriveInfo(PathOutFile).ToString()).AvailableFreeSpace;

            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                try
                {
                    int _tk = Environment.TickCount;
                    //определю предпологаемый размер выгрузки
                    query = "select count(*) n, sum(len([txt])) s from PeopleAttach.dbo." + NameTbl + ";";
                    SqlCommand command = new SqlCommand(query, connection);
                    connection.Open();
                    SqlDataReader SQLreader = command.ExecuteReader();
                    SQLreader.Read();
                    //Console.WriteLine("row: {0}, size: {1}", SQLreader.GetValue(0).ToString(), SQLreader.GetValue(1).ToString());
                    rowcnt = SQLreader.GetInt32(0);
                    sizeTbl = (rowcnt * 2) + SQLreader.GetInt32(1);
                    SQLreader.Close();
                    connection.Close();
                    SetLabel10($"Выполняется выгрузка: {ut.GetTime(Environment.TickCount - _tk)}.");

                    if (sizeTbl < Ffree)    //если мало места - выгрузки нет
                    {
                        Int32 k = 1;
                        query = "select txt from PeopleAttach.dbo." + NameTbl + " order by cast(substring([txt],2,charindex('\"',[txt],2)-2) as int);";
                        //query = "select txt from PeopleAttach.dbo." + NameTbl + ";";
                        command = new SqlCommand(query, connection);
                        connection.Open();
                        SQLreader = command.ExecuteReader();
                        if (FieldList.Length > 0)
                        {
                            file.WriteLine(FieldList);    //если указана строка с полями, записать в файл
                        }
                        if (SQLreader.HasRows)
                        {
                            while (SQLreader.Read())
                            {
                                if (k < rowcnt)
                                {
                                    file.WriteLine(@"" + SQLreader.GetValue(0).ToString() + @"");
                                }
                                else
                                {
                                    file.Write(@"" + SQLreader.GetValue(0).ToString() + @"");   //последняя строка
                                }
                                k++;
                            }
                            SQLreader.Close();
                            connection.Close();
                            //if (connection != null) connection.Close();
                            //flag = true;
                        }
                        else
                        {
                            if (SQLreader != null)
                                SQLreader.Close();
                            if (connection != null)
                                connection.Close();
                            query = "select txt from PeopleAttach.dbo." + NameTbl + ";";
                            command = new SqlCommand(query, connection);
                            connection.Open();
                            SQLreader = command.ExecuteReader();
                            while (SQLreader.Read())
                            {
                                if (k < rowcnt)
                                {
                                    file.WriteLine(@"" + SQLreader.GetValue(0).ToString() + @"");
                                }
                                else
                                {
                                    file.Write(@"" + SQLreader.GetValue(0).ToString() + @"");   //последняя строка
                                }
                                k++;
                            }
                            SQLreader.Close();
                            connection.Close();
                        }
                    }
                    else
                        SetTextBox6(string.Format(errList[2] + " {0}", DateTime.Now));
                }
                catch (Exception ex)
                {
                    this.SetTextBox6(string.Format(errList[3] + " \"OwnedToMO_CSV\": {0}", ex.Message));
                }
                finally
                {
                    file.Close();
                    //if (!flag)
                    //    File.Delete(PathOutFile);     // если файл открепления пустой, удалю его из каталога выгрузки
                    //if (connection.State == System.Data.ConnectionState.Open) connection.Close();
                    if (connection != null)
                    {
                        connection.Close();
                    }
                    connection.Dispose();
                }
            }
        }

        private void ThreadProcSafe_1()
        {   //обработка файлов. метод выполняется в фоновом потоке
            sf.errNumber = 0;
            sf.inpDir = textBox1.Text;
            sf.outDir = textBox2.Text;
            sf.arcDir = textBox3.Text;
            sf.arcInpDir = "";
            sf.arcOutDir = "";
            sf.storageDepth = Properties.Settings.Default.Storage_Depth; 
//            sf.storageDepth = GetComboBox3();  
            sf.tmpDir = Environment.GetEnvironmentVariable("TEMP");
            sf.nameSrvSQL = textBox4.Text;
            sf.pathBCP = @"" + textBox5.Text + @"";

            if (!Directory.Exists(sf.inpDir))
            {
                SetTextBox6($"{errList[0]} {DateTime.Now}");
                return;
            }
            if (isExecHand)
            {
                SetTextBox6($"{errList[1]} {DateTime.Now}");
            }
            else
            {
                try
                {
                    isExecHand = true;  //блокировка запуска потока
                    SetLabel10("Проверка наличия файлов.");

                    string[] zipList = Directory.GetFiles(sf.inpDir, "*");
                    if (zipList.Length > 0)
                    {   //проверка доступности каталога
                        notifyIcon1.Icon = Properties.Resources.Icon4;

                        /*------------------------------------- обработка файлов от МО. тип-"MO"  -----------------------------------*/
                        /* запрос на прикрепление ЗЛ к МО    */
                        zipList = Directory.GetFiles(sf.inpDir, "mo???????????????*.csv");
                        foreach (string f in zipList)
                        {
                            ScanMO(f/*, sf*/);
                         }
                        /*------------------------------------- обработка файлов от СМО. тип-"PZ" -----------------------------------*/
                        /* запрос по прикреплению ЗЛ к МО контингента ЗЛ в СМО, либо его части по предоставленному списку ЕНП */
                        zipList = Directory.GetFiles(sf.inpDir, "pz1*.zip");
                        foreach (string f in zipList) {
                            ScanPZ(f/*, sf*/);
                        }
                        /*------------------------------------- обработка файлов от МО. тип-"SZ" ------------------------------------*/
                        /* сверка прикрепления ЗЛ к МО по предоставленному МО списку ЗЛ */
                        zipList = Directory.GetFiles(sf.inpDir, "sz???????????????*.zip");
                        foreach (string f in zipList) {
                            ScanSZ(f/*, sf*/);
                        }
                        /*------------------------------------- обработка файлов от ЦС. тип-"18000-MED_DR" ------------------------------------*/
                        /* открепления ЗЛ  МО по предоставленному ЦС списку ЗЛ */
                        zipList = Directory.GetFiles(sf.inpDir, "18000-MED_DR*.csv");
                        foreach (string f in zipList)
                        {
                            ScanMED_DR(f/*, sf*/);
                        }
                        /*------------------------------------- обработка файлов от МО. тип-"QA" ------------------------------------*/
                        /* открепления ЗЛ  МО по предоставленному ЦС списку ЗЛ */
                        zipList = Directory.GetFiles(sf.inpDir, "qa2??????????????*.zip");
                        foreach (string f in zipList)
                        {
                            ScanQA(f/*, sf*/);
                        }

                        notifyIcon1.Icon = Properties.Resources.Icon1;
                    }
                    else
                        SetLabel10("Файлов для обработки нет!");
                }
                catch (Exception ex)
                {
                    SetTextBox6($"{errList[11]} {ex.Message}");
                    //if (connection != null) connection.Close();
                }
                finally
                {
                    isExecHand = false;
                    SetLabel10("Ожидание запуска.");
                }
            }
        }
        private void ScanMO(string f/*, SkanFileInfo sf*/)
        {   //метод - прикрепление ЗЛ к МО
            int id;     // порядковый номер файла в таблице MO_LOG
            string mo;  // код МО
            string moo;  // код МО открепелния
            int tk;     // счётчик тактов
            Guid g;     // ид файла
            int i;      // кол-во сформированных откреплений
            string txt = ""; //
            int maxrec = Convert.ToInt32(textBox8.Text);

            sf.fName = Path.GetFileName(f.ToUpper());   //имя файла обрабатываемого прикрепления
            txt = sf.fName.Substring(2, 1);
            if (txt == "1" | txt == "2" | txt == "3") {
                txt = "";
                sf.arcInpDir = sf.arcDir + @"Inp\" + ut.GetStorageDepth(sf.fName, sf.storageDepth);  //папка - полученные файлы
                if (!Directory.Exists(sf.arcInpDir))
                    Directory.CreateDirectory(sf.arcInpDir);    //сделаю, если её нет

                sf.arcOutDir = sf.arcDir + @"Out\" + ut.GetStorageDepth(sf.fName, sf.storageDepth);  //папка - ответные файлы
                if (!Directory.Exists(sf.arcOutDir))
                    Directory.CreateDirectory(sf.arcOutDir);    //сделаю, если её нет

                ut.FileMove(f, Path.Combine(sf.tmpDir, sf.fName)); //перенесу файл в tmpDir

                if (tf.TestFileCSV(sf, maxrec, 730, 25, "1,2,3,4,5,6,7,8,9,10", new byte[] { 1, 2, 3, 6, 7, 9, 16, 17, 19, 21, 22, 24, 25 }) == 0)   //проверка файла на корректность
                {   //корректный формат
                    sf.argsStr = "select top 1 id from PolicyRegister.dbo.MO_LOG where fname='" + sf.fName + "';";   //запрос-проверка обработки файла ранее
                    if (GetIdRecordTbl(@sf.argsStr) == 0) //проверка обработки
                    {
                        sf.argsStr = "if exists (select 1 from dbo.MO_FS where FileName='" + sf.fName + "' and status=0) delete from dbo.MO_FS where FileName='" + sf.fName + "';";   //запрос-если обработка повторна - удалю запись об файле
                        RunCommandSQL(GetConnectionString(), @sf.argsStr).Wait();
                        if (flag)
                        {   //новый файл
                            tk = Environment.TickCount;
                            SetTextBox6(string.Format("Получен файл: {0}: в {1}", sf.fName, DateTime.Now));
                            g = Guid.NewGuid(); // определю GUIDs для файла.
                            sf.argsStr = "insert into dbo.MO_FS (id, FileName) values ('" + g + "','" + sf.fName + "')";   //вставить запись о файле
                            RunCommandSQL(GetConnectionString(), sf.argsStr).Wait();
                            if (flag)
                            {
                                StreamTextToServer(Path.Combine(sf.tmpDir, sf.fName), g).Wait();   //загружу в таблицу БД
                                if (flag)
                                {   //если загрузка в таблицу выполнилась корректно
                                    sf.fNameArc = sf.fName.Replace(".CSV", ".ZIP");   //имя текущего архива
                                    zp.CreateZip(Path.Combine(sf.tmpDir, sf.fName), Path.Combine(sf.arcInpDir, sf.fNameArc));   //упакую и перенесу в каталог arcInpDir

                                    //обработка файла МО
                                    sf.argsStr = "exec srz.sp_FileProcess_mo1_1 '" + @f + "', 2, '" + g + "'";   //запрос для обработки файла
                                    RunCommandSQL(GetConnectionString(), @sf.argsStr).Wait(); //обработка
                                    if (flag)
                                    {   //если обработка выполнена корректно
                                        //StreamBLOBToServer(Path.Combine(sf.arcInpDir, sf.fNameArc), g).Wait();   //сохраню обработанный файл в FileStreams БД
                                        //выгрузка протокола
                                        sf.fName = sf.fName.Replace("MO", "E");   //рабочее имя файла протокола
                                        OwnedToMO_CSV(Path.Combine(sf.tmpDir, sf.fName), "tmp_mofp where id='" + g + "'", ""); //выгрузка протокола

                                        sf.fNameArc = sf.fName.Replace(".CSV", ".ZIP");   //рабочее имя архива протокола
                                        zp.CreateZip(Path.Combine(sf.tmpDir, sf.fName), Path.Combine(sf.arcOutDir, sf.fNameArc));
                                        ut.FileCopy(Path.Combine(sf.arcOutDir, sf.fNameArc), Path.Combine(sf.outDir, sf.fNameArc));

                                        //выгрузка откреплений
                                        sf.argsStr = "select top 1 id from [policyregister].[dbo].[mo_log] where fname = '" + Path.GetFileName(f) + "' order by id desc";  //запрос-для определения номера файла
                                        id = GetIdRecordTbl(@sf.argsStr);  //получу номер файла
                                        if (id > 0)
                                        {
                                            sf.argsStr = "insert PeopleAttach.dbo.MO_LOG_STATUS(id, dt, status) values(" + id + ", getdate(), 1);"; //запрос-признак выполнения выгрузки протокола
                                            GetIdRecordTbl(@sf.argsStr);

                                            mo = Path.GetFileName(f).Substring(3, 6); // код МО
                                            //запрос-определение списка МО для формирования файлов открепления
                                            string sql = "select [mo], count(*) as [num] from [PeopleAttach]..[MO_LOG_OUT_REC_ATTACH] where [id] = " + id + " and [status]=1 and [mo]<>'" + mo + "' and [mo] not in (select code from [PolicyRegister].[dbo].[LPU] where [dendfix]<getdate() or [AGELPU]>3) group by [mo]";
                                            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                                            {
                                                SqlCommand cmd = new SqlCommand(sql, conn);
                                                try
                                                {
                                                    conn.Open();
                                                    SqlDataReader reader = cmd.ExecuteReader();
                                                    if (reader.HasRows)
                                                    {
                                                        i = 0;
                                                        while (reader.Read())
                                                        {
                                                            moo = reader.GetValue(0).ToString();
                                                            //this.SetTextBox6(string.Format("Откреплено от МО: {0}. Кол-во: {1}", moo, reader.GetValue(1).ToString()));
                                                            sf.argsStr = "exec srz.sp_FileProcess_mo2_1 " + id + ",'" + moo + "'";   //запрос-обработка
                                                            RunCommandSQL(GetConnectionString(), @sf.argsStr).Wait(); //обработка файла
                                                            if (flag)
                                                            {   // если обработка выполнена, выгружу открепление
                                                                sf.argsStr = "select FileNum from PeopleAttach.dbo.MO_FILE_STATIST where MO = " + moo + " and FileYear = year(getdate())";  //запрос получения номера файла открепления
                                                                int k = GetIdRecordTbl(@sf.argsStr);
                                                                if (k == 0)
                                                                { //запрос-вставка инф-ция по файлу открепления
                                                                    sf.argsStr = "insert into PeopleAttach.dbo.MO_FILE_STATIST(MO,FileYear,FileNum) values(" + moo + ",year(getdate()),1);" +
                                                                        "select FileNum from PeopleAttach.dbo.MO_FILE_STATIST where MO = " + moo + " and FileYear = year(getdate())";
                                                                }
                                                                else
                                                                { //запрос-обновить инф-цию по файлу открепления
                                                                    sf.argsStr = "update PeopleAttach.dbo.MO_FILE_STATIST set FileNum = FileNum + 1 where MO = " + moo + " and FileYear = year(getdate());" +
                                                                        "select FileNum from PeopleAttach.dbo.MO_FILE_STATIST where MO = " + moo + " and FileYear = year(getdate())";
                                                                }

                                                                sf.fName = @"O2" + moo + DateTime.Now.ToString("yyyyMMdd") + GetIdRecordTbl(@sf.argsStr).ToString() + @".CSV"; //сформирую имя файла открепления
                                                                OwnedToMO_CSV(Path.Combine(sf.tmpDir, sf.fName), "tmp_mofo where id=" + id + " and lpu='" + moo + "'", "");

                                                                sf.fNameArc = sf.fName.Replace(".CSV", ".ZIP");   //определю имя архива
                                                                ut.FileDelete(Path.Combine(sf.arcOutDir, sf.fNameArc));  //если есть файл - удалю
                                                                zp.CreateZip(Path.Combine(sf.tmpDir, sf.fName), Path.Combine(sf.arcOutDir, sf.fNameArc));   //упакую
                                                                ut.FileDelete(Path.Combine(sf.outDir, sf.fNameArc));  //если в есть файл - удалю
                                                                File.Copy(Path.Combine(sf.arcOutDir, sf.fNameArc), Path.Combine(sf.outDir, sf.fNameArc)); //копирую из arcOutDir в outDir
                                                                i += reader.GetInt32(1);
                                                            }
                                                        }
                                                        this.SetTextBox6(string.Format("Кол-во сформированных откреплений ЗЛ от МО: {0}", i));
                                                    }
                                                    reader.Close();
                                                }
                                                catch (Exception ex)
                                                {
                                                    SetTextBox6(ex.Message);
                                                }
                                            }
                                        }
                                        SetTextBox6(string.Format("Время обработки: ({0})", ut.GetTime(Environment.TickCount - tk)));
                                    }
                                    else
                                    {   //если обработка выполнена некорректно
                                        sf.fName = Path.GetFileName(f.ToUpper()).Replace("MO", "E");
                                        sf.fNameArc = sf.fNameArc.Replace(".CSV", ".ZIP");
                                        OwnedToMO_CSV(Path.Combine(sf.tmpDir, sf.fName), "tmp_mof", "");
                                        zp.CreateZip(Path.Combine(sf.tmpDir, sf.fName), Path.Combine(sf.arcOutDir, sf.fNameArc));
                                        ut.FileCopy(Path.Combine(sf.arcOutDir, sf.fNameArc), Path.Combine(sf.outDir, sf.fNameArc));
                                    }
                                }
                                else
                                {   //ошибки ввода данных в БД
                                    txt = string.Format(errList[11] + " Файл: \"" + Path.GetFileName(f) + "\" " + errList[17] + ", " + errList[16]);
                                }
                            }
                            else
                            {   //ошибки ввода данных в БД
                                txt = string.Format(errList[11] + " Файл: \"" + Path.GetFileName(f) + "\" " + errList[17] + ", " + errList[16]);
                            }
                        }
                    }
                    else
                    {   //повторный файл, сформирую описание ошибки
                        txt = string.Format(errList[11] + " Файл: \"" + Path.GetFileName(f) + "\" " + errList[12]);
                        sf.errNumber = 12;
                    }
                }
                else
                {   //некорректный формат, сформирую описание ошибки
                    txt = sf.errTxt;    // string.Format(errList[11] + " Файл: \"" + Path.GetFileName(f) + "\" " + errList[13] + ". Кол-во ошибок: " + sf.errCount + ". Список ошибок(код/строка): " + ((sf.errTest).Length > 100 ? sf.errTest.Remove(100) + "..." : sf.errTest));
                                        //ut.FileDelete(Path.Combine(sf.tmpDir, Path.GetFileName(sf.fName)));  //если есть файл - удалю
                }
                if (txt != "")
                {
                    SetTextBox6(txt);
 
                    if (File.Exists(Path.Combine(sf.tmpDir, sf.fName)))
                        zp.CreateZip(Path.Combine(sf.tmpDir, sf.fName), Path.Combine(sf.arcInpDir, sf.fName.Replace(".CSV", ".ZIP")));   //упакую файл и перенесу в каталог arcInpDir

                    if (sf.errCount == 0)
                    {
                        if (sf.errNumber == 12) //если файл был повторный, добавлю к имени суфикс '_1'
                            sf.fName = "err_" + Path.GetFileNameWithoutExtension(sf.fName) + ".txt";
                        else
                            sf.fName = Path.Combine(Path.GetDirectoryName(sf.fName), Path.GetFileName(sf.fName).Replace("MO", "E"));     //имя протокола ответа
                        WriteTextFile(Path.Combine(sf.arcOutDir, sf.fName), txt);   //сформирую ответ в МО
                        zp.CreateZip(Path.Combine(sf.arcOutDir, sf.fName));
                        sf.fNameArc = Path.GetFileNameWithoutExtension(sf.fName) + ".ZIP";
                        ut.FileCopy(Path.Combine(sf.arcOutDir, sf.fNameArc), Path.Combine(sf.outDir, sf.fNameArc));
                    }
                }
            }
        }
        private void ScanPZ(string f)
        {
            int tk;          //счётчик тактов
            string q;        //код СМО
            Guid g;          //ГУИД
            string fName = Path.GetFileName(f);

            SetTextBox6(string.Format("Получен файл: {0}, в {1}", fName, DateTime.Now));
            sf.argsStr = "if exists (select 1 from dbo.MO_FS where FileName='" + fName + "') delete from dbo.MO_FS where FileName='" + fName + "';";   //запрос - если обработка повторна - удалю запись об файле
            RunCommandSQL(GetConnectionString(), @sf.argsStr).Wait();
            if (flag)
            {
                tk = Environment.TickCount;
                g = Guid.NewGuid(); // определю GUIDs для файла.
                StreamBLOBToServer(@f, g).Wait();   // сохраню файл в FileStream
                if (flag)
                {   // если обработка выполнена корректно, продолжу. распакую.
                    if (zp.UnZipFile(@f, sf.tmpDir, sf.arcDir))
                    {
                        sf.fName = Path.Combine(sf.tmpDir, fName.ToUpper().Replace(".ZIP", ".CSV"));  // сформирую имя файла 
                        ut.FileDelete(f);  //удалю исходный файл
                        q = Path.GetFileName(sf.fName).Substring(3, 5);
                        sf.argsStr = "exec dbo.sp_FileProcess_pz '" + q + "'";   //сформирую строку обработки 
                        RunCommandSQL(GetConnectionString(), @sf.argsStr).Wait(); //обработка
                        if (flag)
                        {  // если выполнена корректно, выгружу данные
                            sf.fName = sf.fName.Replace("PZ1", "PO1");
                            sf.fNameArc = Path.GetFileName(sf.fName).Replace("CSV", "ZIP");
                            OwnedToMO_CSV(sf.fName, "t_ap2", @"""Num"";""PID"";""ENP"";""PolTp"";""SPol"";""NPol"";""LPU"";""LPUDT""");
                            zp.CreateZip(sf.fName, Path.Combine(sf.outDir, sf.fNameArc));
                            this.SetTextBox6(string.Format("Время обработки: ({0})", ut.GetTime(Environment.TickCount - tk)));
                        }
                    } 
                }
            }
        }
        private void ScanSZ(string f)
        {
            int tk;          //таймер
            Guid g;          //GUID
            //string txt = ""; //
            int maxrec = Convert.ToInt32(this.textBox9.Text);

            sf.fName = Path.GetFileName(f.ToUpper());   //имя обрабатываемого файла сверки
            sf.arcInpDir = sf.arcDir + @"Inp\" + ut.GetStorageDepth(sf.fName, sf.storageDepth);  //склад - полученные файлы
            if (!Directory.Exists(sf.arcInpDir))
                Directory.CreateDirectory(sf.arcInpDir);

            sf.arcOutDir = sf.arcDir + @"Out\" + ut.GetStorageDepth(sf.fName, sf.storageDepth);  //склад - ответные файлы 
            if (!Directory.Exists(sf.arcOutDir))
                Directory.CreateDirectory(sf.arcOutDir);

            this.SetTextBox6(string.Format("Получен файл: {0}: в {1}", sf.fName, DateTime.Now));
            sf.argsStr = "select top 1 1 from dbo.MO_FS where FileName='" + sf.fName + "' and [Status]=1 order by DateLoad desc"; //запрос - проверка наличия корректной обработки
            if (GetIdRecordTbl(@sf.argsStr) == 0)
            {
                sf.argsStr = "if exists (select 1 from dbo.MO_FS where FileName='" + sf.fName + "' and [Status]=0) delete from dbo.MO_FS where FileName='" + sf.fName + "';";   //запрос - если обработка повторна и некорректна - удалю запись об файле
                RunCommandSQL(GetConnectionString(), @sf.argsStr).Wait();
                if (flag)
                {// если корректно, продолжу
                    tk = Environment.TickCount;
                    g = Guid.NewGuid();
                    sf.argsStr = "insert into dbo.MO_FS (id, FileName) values ('" + g + "','" + sf.fName + "')";   //вставить запись о файле
                    RunCommandSQL(GetConnectionString(), sf.argsStr).Wait();
                    if (flag)
                    {// если корректно, продолжу. распакую.   
                        if (zp.UnZipFile(@f, sf.tmpDir, sf.arcDir))
                        {
                            if (File.Exists(@f))
                                ut.FileMove(@f, Path.Combine(sf.arcInpDir, sf.fName)); //перенесу в arcInpDir
                            sf.fName = sf.fName.Replace(".ZIP", ".CSV");   // сформирую имя файла
                            if (File.Exists(Path.Combine(sf.tmpDir, sf.fName))) // если есть файл - обработаю
                            {
                                //if ((StreamTestFile(11,"1,2,3,4,5,6", new byte[] { 1, 2, 5, 6, 8 })) == 0)   //проверка файла на корректность
                                if (tf.TestFileCSV(sf, maxrec, 400, 11, "1,2,3,4,5,6,7,8,9,10", new byte[] { 1, 2, 5, 6, 8 }) == 0)   //проверка файла на корректность
                                {
                                    StreamTextToServer(Path.Combine(sf.tmpDir, sf.fName), g).Wait();   // загружу содержимое файла в таблицу
                                    ut.FileDelete(Path.Combine(sf.tmpDir, sf.fName));  //если в каталоге есть файл - удалю
                                    if (flag)
                                    {
                                        sf.argsStr = "exec dbo.sp_FileProcess_sz '" + @sf.fName + "', 2, '" + g + "'";   // сформирую строку для обработки файла
                                        RunCommandSQL(GetConnectionString(), @sf.argsStr).Wait(); //обработка файла
                                        if (flag)
                                        {   // если обработка выполнена корректно, выгружу данные
                                            sf.fName = sf.fName.Replace("SZ2", "SO2");
                                            OwnedToMO_CSV(Path.Combine(sf.tmpDir, sf.fName), "tmp_szf", "");
                                            //OwnedToMO_CSV(Path.Combine(sf.tmpDir, sf.fName), "t_szf where fname='" + sf.fName + "'", "");
                                            zp.CreateZip(Path.Combine(sf.tmpDir, sf.fName), Path.Combine(sf.arcOutDir, sf.fName.Replace("CSV", "ZIP")));   //создам в arcOutDir
                                            ut.FileCopy(Path.Combine(sf.arcOutDir, sf.fName.Replace("CSV", "ZIP")), Path.Combine(sf.outDir, sf.fName.Replace("CSV", "ZIP"))); //копирую в outDir
                                            this.SetTextBox6(string.Format("Время обработки: ({0})", ut.GetTime(Environment.TickCount - tk)));
                                        }
                                    }
                                }
                                else
                                {
                                    ut.FileDelete(Path.Combine(sf.tmpDir, Path.GetFileName(sf.fName)));  //если есть файл - удалю
                                    this.SetTextBox6(sf.errTxt);
                                }
                            }
                            else
                                this.SetTextBox6(string.Format(errList[11] + " Файл: \"" + Path.Combine(sf.tmpDir, sf.fName) + "\" " + errList[14]));
                        }
                        else
                            this.SetTextBox6(string.Format(errList[11] + " Файл: \"" + Path.Combine(sf.arcDir,"Temp", sf.fName) + "\" " + errList[18]));
                    }
                }
            }
            else
            {
                this.SetTextBox6(string.Format(errList[11] + " Файл: \"" + Path.GetFileName(@f) + "\" " + errList[12]));
                if (File.Exists(@f))
                    zp.CreateZip(@f, Path.Combine(sf.arcInpDir, Path.GetFileName(@f).Replace("CSV", "ZIP")));   //упакую файл и перенесу в каталог arcInpDir
            }
        }
        private void ScanMED_DR(string f)
        {
            int id;          //порядковый номер файла в таблице MO_LOG
            int tk;          //счётчик тактов
            string moo;       //код МО открепления
            Guid g;          //ГУИД

            sf.fName = Path.GetFileName(f.Replace("-", "")).ToUpper(); //Path.GetFileName(f.ToUpper());   //запишу в класс SkanFileInfo имя файла открепления
            sf.argsStr = "select top 1 id from dbo.MO_FS where FileName='" + sf.fName + "';";   //запишу в класс SkanFileInfo строку для проверки обработки файла
            ut.FileMove(f, Path.Combine(sf.tmpDir, sf.fName)); //перенесу файл из inpDir в tmpDir
            if (tf.StreamTestFile(sf, 3, "1,2,3,4", new byte[] { 0 }) == 0) //проверка файла - на корректность формата
            {
                if (GetIdRecordTbl(@sf.argsStr) == 0) //проверка файла - был ли обработан ранее?
                {
                    tk = Environment.TickCount;
                    g = Guid.NewGuid(); // определю GUIDs для файла.
                    int i;  // кол-во сформированных откреплений
                    this.SetTextBox6(string.Format("Получен файл: {0}: в {1}", sf.fName, DateTime.Now));
                    StreamTextToServer(Path.Combine(sf.tmpDir, sf.fName), g).Wait();   //загружу содержимое файла в таблицу БД
                    if (flag)
                    {
                        sf.fNameArc = sf.fName.Replace(".CSV", ".ZIP");   //запишу в класс SkanFileInfo имя архива
                        zp.CreateZip(Path.Combine(sf.tmpDir, sf.fName), Path.Combine(sf.tmpDir, sf.fNameArc));    //создам архив в arcInpDir
                        StreamBLOBToServer(Path.Combine(sf.tmpDir, sf.fNameArc), g).Wait();   //запись архива в FileStreams БД
                        if (File.Exists(Path.Combine(sf.tmpDir, sf.fNameArc))) //перенос архива из tmpDir в arcInpDir
                            ut.FileMove(Path.Combine(sf.tmpDir, sf.fNameArc), Path.Combine(sf.arcInpDir, sf.fNameArc));
                        sf.argsStr = "exec dbo.sp_FileProcess_med_dr_1 '" + @f + "', 2, '" + g + "';";   //сформирую строку для обработки файла
                        RunCommandSQL(GetConnectionString(), @sf.argsStr).Wait(); //обработка файла
                        if (flag)   //если обработка выполнена корректно - выгружу файл протокола и если есть, файлы открепления
                        {
                            //--------------------------------------------------------
                            sf.fName = sf.fName.Replace("DR", "DR_E");   //запишу в класс SkanFileInfo имя файла протокола
                            OwnedToMO_CSV(Path.Combine(sf.tmpDir, sf.fName), "tmp_med_dr_f", ""); //выгрузка протокола
                            sf.fNameArc = sf.fName.Replace(".CSV", ".ZIP");   //запишу в класс SkanFileInfo имя архива
                            zp.CreateZip(Path.Combine(sf.tmpDir, sf.fName), Path.Combine(sf.arcOutDir, sf.fNameArc));  //создам в каталоге arcOutDir
                            ut.FileCopy(Path.Combine(sf.arcOutDir, sf.fNameArc), Path.Combine(sf.outDir, sf.fNameArc)); //копирую в каталог outDir

                            sf.argsStr = "select id from peopleattach.dbo.med_dr_log where fname = '" + Path.GetFileName(f) + "';";  //сформирую строку для определения порядкового номера файла
                            id = GetIdRecordTbl(@sf.argsStr);  //получу номер файла
                            //выгрузка откреплений
                            if (id > 0)
                            {
                                sf.argsStr = "update PeopleAttach.dbo.med_dr_log set ddd=getdate(), status=1, ndu=ndu+1 where id=" + id + ";";
                                GetIdRecordTbl(@sf.argsStr);    //запишу признак выгрузки протокола обработки
                                //mo = Path.GetFileName(f).Substring(3, 6); // код МО
                                string sql = "select mo_srz, count(*) as num from PeopleAttach.dbo.med_dr_out where id = " + id + " and status=1 group by mo_srz;"; //сформируем строку для определения списока МО
                                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                                {
                                    SqlCommand cmd = new SqlCommand(sql, conn);
                                    try
                                    {
                                        conn.Open();
                                        SqlDataReader reader = cmd.ExecuteReader();
                                        if (reader.HasRows)
                                        {
                                            i = 0;
                                            while (reader.Read())
                                            {
                                                moo = reader.GetValue(0).ToString();
                                                //this.SetTextBox6(string.Format("Откреплено от МО: {0}. Кол-во: {1}", moo, reader.GetValue(1).ToString()));
                                                sf.argsStr = "exec dbo.sp_FileProcess_med_dr_2 " + id + ",'" + moo + "';";   //сформирую строку для обработки файла
                                                RunCommandSQL(GetConnectionString(), @sf.argsStr).Wait(); //обработка файла
                                                if (flag)
                                                {   // если обработка выполнена корректно, выгружу открепление
                                                    sf.argsStr = "select FileNum from PeopleAttach.dbo.med_dr_statist where MO = " + moo + " and FileYear = year(getdate());";  //строка для получения порядкового номера файла открепления
                                                    int k = (int)GetIdRecordTbl(@sf.argsStr);
                                                    if (k == 0)  //сформируем строку для получения порядкового номера файла открепления
                                                    {
                                                        sf.argsStr = "insert into PeopleAttach.dbo.med_dr_statist(MO,FileYear,FileNum) values('" + moo + "',year(getdate()),1);" +
                                                            "select FileNum from PeopleAttach.dbo.med_dr_statist where MO = '" + moo + "' and FileYear = year(getdate());";
                                                    }
                                                    else
                                                    {
                                                        sf.argsStr = "update PeopleAttach.dbo.med_dr_statist set FileNum = FileNum + 1 where MO = '" + moo + "' and FileYear = year(getdate());" +
                                                            "select FileNum from PeopleAttach.dbo.med_dr_statist where MO = '" + moo + "' and FileYear = year(getdate());";
                                                    }
                                                    //sf.argsStr = "update PeopleAttach.dbo.med_dr_statist set FileNum = FileNum + 1 where MO = '" + moo + "' and FileYear = year(getdate());" +
                                                    //    "select FileNum from PeopleAttach.dbo.med_dr_statist where MO = '" + moo + "' and FileYear = year(getdate())";  //строка для получения порядкового номера файла открепления

                                                    sf.fName = @"OF2" + moo + DateTime.Now.ToString("yyyyMMdd") + GetIdRecordTbl(@sf.argsStr).ToString() + @".CSV"; //сформирую имя файла открепления

                                                    OwnedToMO_CSV(Path.Combine(sf.tmpDir, sf.fName), "tmp_med_dr_f", "");
                                                    sf.fNameArc = sf.fName.Replace(".CSV", ".ZIP");   //запишу в класс SkanFileInfo имя архива
                                                    zp.CreateZip(Path.Combine(sf.tmpDir, sf.fName), Path.Combine(sf.arcOutDir, sf.fNameArc));   //создам в каталоге arcOutDir
                                                    ut.FileCopy(Path.Combine(sf.arcOutDir, sf.fNameArc), Path.Combine(sf.outDir, sf.fNameArc)); //копирую в каталог outDir
                                                    i += reader.GetInt32(1);
                                                }
                                            }
                                            this.SetTextBox6(string.Format("Обработка завершена. Кол-во сформированных откреплений: {0}.", i));
                                        }
                                        reader.Close();
                                    }
                                    catch (Exception ex)
                                    {
                                        this.SetTextBox6(ex.Message);
                                    }
                                }
                            }
                            else
                            {
                                //this.SetTextBox6(string.Format(errList[11] + " Файл: \"" + Path.GetFileName(f) + "\", для формирования файлов открепления " + errList[15]));

                                //sf.fName = Path.GetFileName(f.ToUpper()).Replace("DR", "DR_E");
                                //OwnedToMO_CSV(Path.Combine(sf.tmpDir, sf.fName), "tmp_med_dr_f", "");
                                //zp.CreateZip(Path.Combine(sf.tmpDir, sf.fName), Path.Combine(sf.arcOutDir, sf.fNameArc));   //упакую протокол в каталоге outDir
                                //ut.FileDelete(Path.Combine(sf.outDir, sf.fNameArc));  //если в каталоге есть файл - удалю
                                //File.Copy(Path.Combine(sf.arcOutDir, sf.fNameArc), Path.Combine(sf.outDir, sf.fNameArc)); //скопирую файл из каталога arcOutDir в каталог outDir
                            }
                        }
                    }
                    else
                    {
                        this.SetTextBox6(string.Format(errList[11] + " Файл: \"" + Path.GetFileName(f) + "\" " + errList[16]));
                        if (File.Exists(f))
                            zp.CreateZip(f, Path.Combine(sf.arcInpDir, Path.GetFileName(f).Replace("CSV", "ZIP")));   //упакую файл и перенесу в каталог arcInpDir
                    }
                }
                else
                {
                    this.SetTextBox6(string.Format(errList[11] + " Файл: \"" + Path.GetFileName(f) + "\" " + errList[13] + ". Кол-во ошибок: " + sf.errCount + ". Список ошибок(код/строка): " + ((sf.errTest).Length > 100 ? sf.errTest.Remove(100) + "..." : sf.errTest)));
                    if (File.Exists(f))
                        zp.CreateZip(f, Path.Combine(sf.arcInpDir, Path.GetFileName(f).Replace("CSV", "ZIP")));   //упакую файл и перенесу в каталог arcInpDir
                }
            }
        }
        private void ScanQA(string f)
        {
            int tk;          //таймер
            Guid g;          //GUID
            int maxrec = Convert.ToInt32(this.textBox9.Text);

            sf.fName = Path.GetFileName(f.ToUpper());   //имя обрабатываемого файла сверки
            sf.arcInpDir = $"{ sf.arcDir}Inp\\{ ut.GetStorageDepth(sf.fName, sf.storageDepth)}";  //склад - полученные файлы
            if (!Directory.Exists(sf.arcInpDir))
                Directory.CreateDirectory(sf.arcInpDir);

            sf.arcOutDir = $"{sf.arcDir}Out\\{ut.GetStorageDepth(sf.fName, sf.storageDepth)}";  //склад - ответные файлы 
            if (!Directory.Exists(sf.arcOutDir))
                Directory.CreateDirectory(sf.arcOutDir);

            this.SetTextBox6(string.Format("Получен файл: {0}: в {1}", sf.fName, DateTime.Now));
            sf.argsStr = $"select top 1 1 from dbo.MO_FS where FileName='{sf.fName}' and [Status]=1 order by DateLoad desc;"; //запрос - проверка наличия корректной обработки
            if (GetIdRecordTbl(@sf.argsStr) == 0)
            {
                sf.argsStr = $"if exists (select 1 from dbo.MO_FS where FileName='{sf.fName}' and [Status]=0) delete from dbo.MO_FS where FileName='{sf.fName}';";   //запрос - если обработка повторна и некорректна - удалю запись об файле
                RunCommandSQL(GetConnectionString(), @sf.argsStr).Wait();
                if (flag)
                {// если корректно, продолжу
                    tk = Environment.TickCount;
                    g = Guid.NewGuid();
                    sf.argsStr = $"insert into dbo.MO_FS (id, FileName) values ('{g}','{sf.fName}');";   //вставить запись о файле
                    RunCommandSQL(GetConnectionString(), sf.argsStr).Wait();
                    if (flag)
                    {// если корректно, продолжу. распакую.
                        if (zp.UnZipFile(@f, sf.tmpDir, sf.arcDir))
                        {
                            if (File.Exists(@f))
                                ut.FileMove(@f, Path.Combine(sf.arcInpDir, sf.fName)); //перенесу в arcInpDir
                            sf.fName = sf.fName.Replace(".ZIP", ".CSV");   // сформирую имя файла
                            if (File.Exists(Path.Combine(sf.tmpDir, sf.fName))) // если есть файл - обработаю
                            {
//                                if (tf.TestFileCSV(sf, maxrec, 230, 9, "1,2,3,4,5,6,7,8,9,10", new byte[] { 1, 3, 4, 6 }) == 0)   //проверка файла на корректность
                                if (tf.TestFileCSV(sf, maxrec, 250, 11, "1,2,3,4,5,6,7,8,9,10", new byte[] { 1, 3, 4, 6 }) == 0)   //проверка файла на корректность
                                    {
                                        StreamTextToServer(Path.Combine(sf.tmpDir, sf.fName), g).Wait();   // загружу содержимое файла в таблицу
                                    ut.FileDelete(Path.Combine(sf.tmpDir, sf.fName));  //если в каталоге есть файл - удалю
                                    if (flag)
                                    {
                                        sf.argsStr = "exec dbo.sp_FileProcess_qa_1_3 '" + @sf.fName + "', '" + g + "'";   // сформирую строку для обработки файла
                                        RunCommandSQL(GetConnectionString(), @sf.argsStr).Wait(); //обработка файла
                                        if (flag)
                                        {   // если обработка выполнена корректно, выгружу данные
                                            sf.fName = sf.fName.Replace("QA2", "RA2");
                                            OwnedToMO_CSV(Path.Combine(sf.tmpDir, sf.fName), "tmp_qaf where id='" + g + "'", "");
                                            zp.CreateZip(Path.Combine(sf.tmpDir, sf.fName), Path.Combine(sf.arcOutDir, sf.fName.Replace("CSV", "ZIP")));   //создам в arcOutDir
                                            ut.FileCopy(Path.Combine(sf.arcOutDir, sf.fName.Replace("CSV", "ZIP")), Path.Combine(sf.outDir, sf.fName.Replace("CSV", "ZIP"))); //копирую в outDir
                                            this.SetTextBox6(string.Format("Время обработки: ({0})", ut.GetTime(Environment.TickCount - tk)));
                                            sf.argsStr = $"delete from dbo.tmp_qaf where id='{g}';";   //удалю записи после выгрузки
                                            RunCommandSQL(GetConnectionString(), @sf.argsStr).Wait();
                                        }
                                    }
                                }
                                else
                                {
                                    ut.FileDelete(Path.Combine(sf.tmpDir, Path.GetFileName(sf.fName)));  //если есть файл - удалю
                                    this.SetTextBox6(sf.errTxt);
                                }
                            }
                            else
                                this.SetTextBox6(string.Format(errList[11] + " Файл: \"" + Path.Combine(sf.tmpDir, sf.fName) + "\" " + errList[14]));
                        }
                        else
                            this.SetTextBox6(string.Format(errList[11] + " Файл: \"" + Path.Combine(sf.arcDir, "Temp", sf.fName) + "\" " + errList[18]));
                    }
                }
            }
            else
            {
                this.SetTextBox6(string.Format(errList[11] + " Файл: \"" + Path.GetFileName(@f) + "\" " + errList[12]));
                if (File.Exists(@f))
                    zp.CreateZip(@f, Path.Combine(sf.arcInpDir, Path.GetFileName(@f).Replace("CSV", "ZIP")));   //упакую файл и перенесу в каталог arcInpDir
            }
        }

        private void ThreadProcSafe_2()
        {
            string outDir;   //папка ответных файлов
            string tmpDir;     //папка временных файлов
            string arcDir;      //папка архивных файлов
            string arcOutDir;   //папка Arc\Out ответных файлов
            string argsStr;     //строка аргумента
            string fName;       //имя файла
            ThreadRunQueryDB q; //
            AutoResetEvent ev2 = new AutoResetEvent(false);
            TaskInfo ti2 = new TaskInfo();

            outDir = this.textBox2.Text;
            arcDir = this.textBox3.Text;
            arcOutDir = arcDir + @"Out\";
            tmpDir = Environment.GetEnvironmentVariable("TEMP");    //папка временных файлов
            sf.nameSrvSQL = this.textBox4.Text;

            if (isExecHand_SMO) {
                this.SetTextBox6(string.Format(errList[1] + " {0}", DateTime.Now));
            }
            else {
                try {
                    isExecHand_SMO = true;  //блокировка запуска потока
                    ti2.isExecHand = true;  //флаг потока - включение
                    ti2.txtLabel = "СМО";
                    ti2.tk = Environment.TickCount;
                    ti2.button = button9;
                    ti2.Handle = ThreadPool.RegisterWaitForSingleObject(ev2, new WaitOrTimerCallback(WaitProc), ti2, 1000, false); //в пуле включить поток WaitProc
                    string sql = "select [code] from [PolicyRegister].[dbo].[SMO] where cast(isnull(de,getdate()) as date) >= cast(getdate() as date)"; //сформируем строку для определения списока СМО
                    using (SqlConnection conn = new SqlConnection(GetConnectionString())) {
                        SqlCommand cmd = new SqlCommand(sql, conn);
                        try {
                            conn.Open();
                            SqlDataReader reader = cmd.ExecuteReader();
                            if (reader.HasRows) {
                                while (reader.Read()) {
                                    argsStr = "exec dbo.sp_OutSMO '" + reader.GetValue(0).ToString() + "'";   //сформирую строку подготовки данных
                                    RunCommandSQL(GetConnectionString(), @argsStr).Wait(); //обработка файла
                                    if (flag)
                                    {// если обработка выполнена корректно, выгружу открепление
                                        fName = "PK1" + reader.GetValue(0).ToString() + DateTime.Now.ToString("yyyyMMdd") + ".accdb";
                                        if (zp.UnZipFile(Path.Combine(arcDir, "KFile.zip"), tmpDir, arcDir))
                                        {
                                            ut.FileMove(Path.Combine(tmpDir, "KFile.accdb"), Path.Combine(tmpDir, fName));
                                            string connectString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + Path.Combine(tmpDir, fName) + ";";
                                            q = new ThreadRunQueryDB("Thread_1", "OleDB", connectString, "select * into register from dbo_kfile_SMO;");
                                            q = new ThreadRunQueryDB("Thread_2", "OleDB", connectString, "drop table dbo_kfile_SMO;");
                                            q = new ThreadRunQueryDB("Thread_3", "OleDB", connectString, "drop table dbo_kfile_FSB;");
                                            zp.CreateZip(Path.Combine(tmpDir, fName), Path.Combine(outDir, Path.GetFileName(fName).Replace("accdb", "zip")));
                                        }
                                    }
                                }
                            }
                            reader.Close();
                        }
                        catch (Exception ex) {
                            this.SetTextBox6(ex.Message);
                        }
                    }
                }
                catch (Exception ex) {
                    this.SetTextBox6(string.Format("Ошибка выполнения. {0}", ex.Message));
                    //if (connection != null) connection.Close();
                }
                finally {
                    isExecHand_SMO = false;
                    ti2.isExecHand = false;  //флаг потока - выключение
                    ev2.Set();   //в пуле выключить поток WaitProc
                    this.SetTextBox6(string.Format("Выгрузка сведений для СМО выполнена. Время обработки: ({0})", ut.GetTime(Environment.TickCount - ti2.tk)));
                    this.SetLabel10("Ожидание запуска.");
                }
            }
        }
        private void ThreadProcSafe_3()
        {
            string outDir;      //папка ответных файлов
            string tmpDir;      //папка временных файлов
            string arcDir;      //папка архивных файлов
            string arcOutDir;   //папка Arc\Out ответных файлов
            string argsStr;     //строка аргумента
            string fName;       //имя файла
            ThreadRunQueryDB q; //
            AutoResetEvent ev3 = new AutoResetEvent(false);
            TaskInfo ti3 = new TaskInfo();

            outDir = this.textBox2.Text;
            arcDir = this.textBox3.Text;
            arcOutDir = arcDir + @"Out\";
            tmpDir = Environment.GetEnvironmentVariable("TEMP");    //папка временных файлов
            sf.nameSrvSQL = this.textBox4.Text;

            if (isExecHand_FSB) {
                this.SetTextBox6(string.Format(errList[1] + " {0}", DateTime.Now));
            }
            else {
                try {
                    isExecHand_FSB = true;  //блокировка запуска потока
                    ti3.isExecHand = true;  //флаг потока - включение
                    ti3.txtLabel = "ФСБ";
                    ti3.tk= Environment.TickCount;
                    ti3.button = button10;
                    ti3.Handle = ThreadPool.RegisterWaitForSingleObject(ev3,new WaitOrTimerCallback(WaitProc),ti3,1000,false); //в пуле включить поток WaitProc
                    argsStr = "exec dbo.sp_OutFSB";   //сформирую строку подготовки данных
                    RunCommandSQL(GetConnectionString(), @argsStr).Wait(); //подготовка данных
                    if (flag)
                    {// если обработка выполнена корректно, выгружу открепление
                        fName = "RG_" + DateTime.Now.ToString("yyyy") + "_1.accdb";
                        if (zp.UnZipFile(Path.Combine(arcDir, "KFile.zip"), tmpDir, arcDir))
                        {
                            ut.FileMove(Path.Combine(tmpDir, "KFile.accdb"), Path.Combine(tmpDir, fName));
                            string connectString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + Path.Combine(tmpDir, fName) + ";";
                            q = new ThreadRunQueryDB("Thread_1", "OleDB", connectString, "select * into register from dbo_kfile_FSB;");
                            q = new ThreadRunQueryDB("Thread_1", "OleDB", connectString, "drop table dbo_kfile_SMO;");
                            q = new ThreadRunQueryDB("Thread_1", "OleDB", connectString, "drop table dbo_kfile_FSB;");
                            zp.CreateZip(Path.Combine(tmpDir, fName), Path.Combine(outDir, Path.GetFileName(fName).Replace("accdb", "zip")));
                        }
                    }
                }
                catch (Exception ex) {
                    this.SetTextBox6(string.Format("Ошибка выполнения. {0}", ex.Message));
                }
                finally {
                    isExecHand_FSB = false;
                    ti3.isExecHand = false;  //флаг потока - выключение
                    ev3.Set();   //в пуле выключить поток WaitProc
                    this.SetTextBox6(string.Format("Выгрузка сведений для ФСБ выполнена. Время обработки: ({0})", ut.GetTime(Environment.TickCount - ti3.tk)));
                    this.SetLabel10("Ожидание запуска.");
                }
            }
        }

        /// <summary>
        /// Включение авто-тактового режима работы сканера по расписанию
        /// (сканер наличия файлов в папке)
        /// </summary>
        /// <returns>Запуск сканера</returns>
        private void RunScaner()
        {   //сканер наличия файлов в папке ( вкл/выкл )
            try {
                while (true) {
                    if (((start <= finish) && ((DateTime.Now.Hour >= start) && (DateTime.Now.Hour <= finish))) || ((start > finish) && ((DateTime.Now.Hour >= start) && (DateTime.Now.Hour <= finish + 12))))
                    {
                        //TimeSpan interval = new TimeSpan(0, 0, takt);
                        //Thread.Sleep(interval);
                        if (!isExecHand) {
                            this.notifyIcon1.Icon = Properties.Resources.Icon3;
                            this.WorkupThread = new Thread(new ThreadStart(this.ThreadProcSafe_1));
                            this.WorkupThread.Start();
                        }
                    }
                    else
                        this.notifyIcon1.Icon = Properties.Resources.Icon2;
                    Thread.Sleep(new TimeSpan(0, 0, takt));
                }
            }
            catch (Exception) {
                //this.SetTextBox6(string.Format("Сканер отключен. {0}", DateTime.Now + ". " + ex.Message));
                this.SetTextBox6($"Сканер отключен. {DateTime.Now}({Environment.MachineName}-{Environment.UserName})");
            }
            finally {
                isExecScaner = false;
                this.SetLabel10("Ожидание запуска.");
                this.notifyIcon1.Icon = Properties.Resources.Icon1;
            }
        }

        /// <summary>
        /// Активация сканера
        /// </summary>
        /// <returns>Вкл/Выкл</returns>
        private void ActivationScaner()
        {   //активация сканера
            if (isExecScaner)
            {  //определим, запущен ли поток сканера
                if (isExecHand)
                {
                    this.SetTextBox6("Идёт обработка! Сканер отключить нельзя!");
                }
                else
                {
                    //выключить поток сканера
                    this.ScanerThread.Abort();
                    this.button8.Text = "Включить";
                    isExecScaner = false;
                    this.comboBox1.Enabled = true;
                    this.comboBox2.Enabled = true;
                    this.textBox7.Enabled = true;
                }
            }
            else
            {
                //включить поток сканера
                this.SetTextBox6($"Сканер включен. {DateTime.Now}({Environment.MachineName}-{Environment.UserName})");
                this.button8.Text = "Отключить";
                start = Convert.ToSByte(this.comboBox1.Text);
                finish = Convert.ToSByte(this.comboBox2.Text);
                takt = Convert.ToInt16(this.textBox7.Text);
                this.comboBox1.Enabled = false;
                this.comboBox2.Enabled = false;
                this.textBox7.Enabled = false;

                isExecScaner = true;
                this.ScanerThread = new Thread(new ThreadStart(this.RunScaner));
                this.ScanerThread.Start();
            }
        }
        public void WaitProc(object state, bool timedOut)
        {
            TaskInfo ti = (TaskInfo)state;
            if (!timedOut)
            {
                if (ti.Handle != null)
                    ti.Handle.Unregister(null);
            }
            //++ti.tk;
            this.InfControl(ti.button, ti);
            //Console.WriteLine("WaitProc() выполнился поток {0}; {1}", Thread.CurrentThread.GetHashCode().ToString(), ti.tk);
        }
        private void InfControl(Button button, TaskInfo ti)
        {
            if (button.InvokeRequired)
            {
                SetTextControlCallback d = new SetTextControlCallback(InfControl);
                this.Invoke(d, new object[] { button, ti });
            }
            else
            {
                if (ti.isExecHand)
                    button.Text = ut.GetTime(Environment.TickCount - ti.tk).ToString();
                else
                    button.Text = ti.txtLabel;
            }
        }

        /// <summary>
        /// Системный проводник - выбор папки. 
        /// </summary>
        /// <returns>Расположение каталога</returns>
        private string FolderBrowserDialog()
        {
            folderBrowserDialog1.SelectedPath = global::Scaner.Properties.Settings.Default.Path_Inp;
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                return folderBrowserDialog1.SelectedPath;
            }
            else return null;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            string tmpPatch;
            folderBrowserDialog1.SelectedPath = global::Scaner.Properties.Settings.Default.Path_Inp;
            //folderBrowserDialog1.RootFolder = Path.GetPathRoot(tmpPatch);
            //folderBrowserDialog1.RootFolder = Environment.SpecialFolder.NetworkShortcuts;
            tmpPatch = FolderBrowserDialog();
            if (tmpPatch != null)
            {
                textBox1.Text = tmpPatch + @"\"; // Properties.Settings.Default.Path_Inp;
                Properties.Settings.Default.Path_Inp = tmpPatch + @"\";
            }
        }
        private void Button2_Click(object sender, EventArgs e)
        {
            string tmpPatch;
            folderBrowserDialog1.SelectedPath = Properties.Settings.Default.Path_Out;
            tmpPatch = FolderBrowserDialog();
            if (tmpPatch != null)
            {
                //global::VerificationAttachment.Properties.Settings.Default.Path_Out = tmpPatch + @"\";
                textBox2.Text = tmpPatch + @"\";// global::VerificationAttachment.Properties.Settings.Default.Path_Out;
                Properties.Settings.Default.Path_Out = tmpPatch + @"\";
            }
        }
        private void Button3_Click(object sender, EventArgs e)
        {
            string tmpPatch;
            folderBrowserDialog1.SelectedPath = Properties.Settings.Default.Path_Tmp;
            tmpPatch = FolderBrowserDialog();
            if (tmpPatch != null) {
                //global::VerificationAttachment.Properties.Settings.Default.Path_Tmp = "" + tmpPatch + @"\";
                textBox3.Text = tmpPatch + @"\";// global::VerificationAttachment.Properties.Settings.Default.Path_Tmp;
                Properties.Settings.Default.Path_Tmp = tmpPatch + @"\";
            }
        }
        private void Button4_Click(object sender, EventArgs e)
        {
            MessageBox.Show(string.Format(errList[ut.TestConnectSQL_2()]), "Внимание!");
            //TestConnectSQL_2();
            /*
            Stream myStream = null;
            //OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = global::VerificationAttachment.Properties.Settings.Default.Path_Arch;
            openFileDialog1.Filter = "exe files (*.exe)|*.exe|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK) {
                try 
                {
                    if ((myStream = openFileDialog1.OpenFile()) != null) 
                    {
                        using (myStream) 
                        {
                            global::VerificationAttachment.Properties.Settings.Default.Path_Arch = openFileDialog1.FileName;
                            textBox4.Text = global::VerificationAttachment.Properties.Settings.Default.Path_Arch;
                        }
                    }
                }
                catch (Exception ex) {
                    MessageBox.Show(errList[4] + ex.Message);
                }
            }
            */
        }
        private void Button5_Click(object sender, EventArgs e) {
            Stream myStream = null;
            //OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = global::Scaner.Properties.Settings.Default.Path_Bcp;
            openFileDialog1.Filter = "exe files (bcp.exe)|bcp.exe|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if ((myStream = openFileDialog1.OpenFile()) != null)
                    {
                        using (myStream)
                        {
                            global::Scaner.Properties.Settings.Default.Path_Bcp = openFileDialog1.FileName;
                            textBox5.Text = global::Scaner.Properties.Settings.Default.Path_Bcp;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(errList[4] + ex.Message);
                }
            }
        }
        private void Button7_Click(object sender, EventArgs e)
        {
            this.SetLabel10("Выполняется обработка");
            if (isExecHand)
            {
                this.SetTextBox6("Идёт обработка. Пожалуйста, дождитесь завершения.");
            }
            else
            {
                this.WorkupThread = new Thread(new ThreadStart(this.ThreadProcSafe_1));
                this.WorkupThread.Start();
            }
        }
        private void Button8_Click(object sender, EventArgs e)
        {
            ActivationScaner();
        }
        private void Button9_Click(object sender, EventArgs e)
        {
            this.SetLabel10(string.Format(errList[9]));// ("Активация выгрузки данных для СМО");
            if (isExecHand_SMO)
            {
                this.SetTextBox6(string.Format(errList[1] + " {0}", DateTime.Now));//("Выполняется выгрузка сведений о ЗЛ. Пожалуйста, дождитесь завершения.");
            }
            else
            {
                //flag_smo = true;
                this.SetTextBox6(string.Format(errList[10] + " для СМО. " + " {0}", DateTime.Now));//("Активирована выгрузка из СРЗ персональных сведений ЗЛ для СМО.");
                this.SMOThread = new Thread(new ThreadStart(this.ThreadProcSafe_2));
                this.SMOThread.Start();
            }
        }
        private void Button10_Click(object sender, EventArgs e)
        {
            this.SetLabel10(string.Format(errList[9]));// ("Активация выгрузки данных для ФСБ");
            if (isExecHand_FSB)
            {
                this.SetTextBox6(string.Format(errList[1] + " {0}", DateTime.Now));//("Выполняется выгрузка сведений о ЗЛ. Пожалуйста, дождитесь завершения.");
            }
            else
            {
                this.SetTextBox6(string.Format(errList[10] + " для ФСБ. " + " {0}", DateTime.Now));//("Активирована выгрузка из СРЗ персональных сведений ЗЛ для ФСБ.");
                this.FSBThread = new Thread(new ThreadStart(this.ThreadProcSafe_3));
                this.FSBThread.Start();
            }
        }

        private void CheckBox1_Click(object sender, EventArgs e)
        {
            bool b;
            b= checkBox1.Checked;
            //вкл/выкл режима загрузки программы при запуске Windows
            Properties.Settings.Default.AutoRunApp = b;
            if (!b) {
                ut.DisenableAutoRun();
                //checkBox1.CheckAlign = ContentAlignment.MiddleRight;
            }
            else {
                ut.EnableAutoRun();
                //checkBox1.CheckAlign = ContentAlignment.MiddleLeft;
            }
        }
        private void CheckBox2_Click(object sender, EventArgs e)
        {
            bool b;
            b = checkBox2.Checked;
            Properties.Settings.Default.AutoRunScan = b;
        }

        private void ComboBox1_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.Time_On = comboBox1.Text;
        }
        private void ComboBox2_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.Time_Off = comboBox2.Text;
        }
        private void ComboBox3_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.Storage_Depth = comboBox3.Text;
        }

        private void TextBox1_Validated(object sender, EventArgs e)
        {
            string s = textBox1.Text.Substring(textBox1.Text.Length - 1);
            s = textBox1.Text + ((s != @"\") ? @"\" : ""); //Path.Combine(textBox1.Text, ((s != @"\") ? @"\" : ""));
            if (!Directory.Exists(s))
            {
                MessageBox.Show(errList[0]);
                //MessageBox.Show("Указанный каталог не существует или не доступен!\nУкажите новый путь к каталогу.");
                textBox1.Text = Properties.Settings.Default.Path_Inp;
            }
            else
            {
                textBox1.Text = s;
                Properties.Settings.Default.Path_Inp = s;
            }
        }
        private void TextBox2_Validated(object sender, EventArgs e)
        {
            string s = textBox2.Text.Substring(textBox2.Text.Length - 1);
            s = textBox2.Text + ((s != @"\") ? @"\" : ""); //Path.Combine(textBox2.Text, ((s != @"\") ? @"\" : ""));
            if (!Directory.Exists(s))
            {
                MessageBox.Show(errList[0]);
                //MessageBox.Show("Указанный каталог не существует или не доступен!\nУкажите новый путь к каталогу.");
                textBox2.Text = Properties.Settings.Default.Path_Out;
            }
            else
            {
                textBox2.Text = s;
                Properties.Settings.Default.Path_Out = s;
            }
        }
        private void TextBox3_Validated(object sender, EventArgs e)
        {
            string s = textBox3.Text.Substring(textBox3.Text.Length - 1);
            s = textBox3.Text + ((s != @"\") ? @"\" : ""); //Path.Combine(textBox3.Text, ((s != @"\") ? @"\" : ""));
            if (!Directory.Exists(s))
            {
                MessageBox.Show(errList[0]);
                //MessageBox.Show("Указанный каталог не существует или не доступен!\nУкажите новый путь к каталогу.");
                textBox3.Text = Properties.Settings.Default.Path_Tmp;
            }
            else
            {
                textBox3.Text = s;
                Properties.Settings.Default.Path_Tmp = s;
            }
        }
        private void TextBox4_Validated(object sender, EventArgs e)
        {
            MessageBox.Show(string.Format(errList[ut.TestConnectSQL_2()]), "Внимание!");
        }
        private void TextBox7_Validated(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBox7.Text) && !Regex.IsMatch(textBox7.Text, @"(\D)"))
            { // очистить ошибку.
                errorProvider1.SetError(this.textBox7, "");
            }
            else
            {  // установить ошибку, если некорректно и установить значение по умолчанию
                errorProvider1.SetError(this.textBox7, errList[5]);
                this.textBox7.Text = "300";
            }
            Properties.Settings.Default.Cycle = textBox7.Text;
        }
        private void TextBox8_Validated(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBox8.Text) && !Regex.IsMatch(textBox8.Text, @"(\D)"))
            { // очистить ошибку.
                errorProvider1.SetError(this.textBox8, "");
            }
            else
            {  // установить ошибку, если некорректно и установить значение по умолчанию
                errorProvider1.SetError(this.textBox8, errList[5]);
                this.textBox8.Text = "3000";
            }
            Properties.Settings.Default.MaxRecMO = textBox8.Text;
        }
        private void TextBox9_Validated(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBox9.Text) && !Regex.IsMatch(textBox9.Text, @"(\D)"))
            { // очистить ошибку.
                errorProvider1.SetError(this.textBox9, "");
            }
            else
            {  // установить ошибку, если некорректно и установить значение по умолчанию
                errorProvider1.SetError(this.textBox8, errList[5]);
                this.textBox9.Text = "99999";
            }
            Properties.Settings.Default.MaxRecSZ = textBox9.Text;
        }
    }

    // ---------  классы и структуры программы  ----------
    class ThreadRunQueryDB
    {//поток для выполнения запроса к базе данных / thread to execute a database query
        Thread thread;
        //string myConnectString;
        private readonly string myConnectString;
        private OleDbConnection myConnection_OleDB;
        private OdbcConnection myConnection_ODBC;

        public ThreadRunQueryDB(string name, string ConnectType, string connectString, string query)
        { //Конструктор получает: имя потока, тип драйвера подключения, строку подключения к БД, запрос к БД 
            myConnectString = connectString;
            switch (ConnectType)
            {
                case "OleDB":
                    thread = new Thread(this.Func_OleDB);
                    break;
                case "ODBC":
                    thread = new Thread(this.Func_ODBC);
                    break;
            }
            thread.Name = name;
            thread.Start(query);//передача параметра в поток
            thread.Join();
        }
        void Func_OleDB(object query)
        {
            using (myConnection_OleDB = new OleDbConnection(myConnectString))
            {
                myConnection_OleDB.Open();
                using (OleDbCommand command = new OleDbCommand(query.ToString(), myConnection_OleDB))
                {
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(string.Format("ThreadRunQueryDB/Func_OleDB. Ошибка выполнения SQL-запроса {0}. (Ошибка: {1})", query.ToString(), ex.Message));
                    }
                    finally
                    {
                        if (myConnection_OleDB != null)
                            myConnection_OleDB.Close();
                        command.Dispose();
                    }
                }
            }
        }
        void Func_ODBC(object query)
        {
            using (myConnection_ODBC = new OdbcConnection(myConnectString))
            {
                myConnection_ODBC.Open();
                using (OdbcCommand command = new OdbcCommand(query.ToString(), myConnection_ODBC))
                {
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(string.Format("ThreadRunQueryDB/Func_ODBC. Ошибка выполнения SQL-запроса {0}. (Ошибка: {1})", query.ToString(), ex.Message));
                    }
                    finally
                    {
                        if (myConnection_ODBC != null)
                            myConnection_ODBC.Close();
                        command.Dispose();
                    }
                }
            }
        }
    }
    public class TaskInfo
    {   // TaskInfo - содержит данные, которые будут переданы в метод обратного вызова.
        public RegisteredWaitHandle Handle = null;  //
        public string txtLabel;         //надпись на объекте
        public Button button;           //объект
        public int tk = 0;              //время запуска потока
        public bool isExecHand=false;   //флаг обработки потока
    }
    public class SkanFileInfo
    {   // SkanFileInfo - содержит данные для работы модулей обработки файлов сканера.
        public string nameSrvSQL;   //имя SQL-сервера
        public string pathBCP;      //папка утилиты BCP

        public string inpDir;       //папка полученных файлов
        public string outDir;       //папка ответных файлов
        public string tmpDir;       //папка временных файлов
        public string arcDir;       //папка архивных файлов

        public string storageDepth; //глубина дерева каталога архивных файлов
        public string arcInpDir;    //папка Arc\Inp полученных файлов
        public string arcOutDir;    //папка Arc\Out ответных файлов

        public string argsStr;      //строка аргумента
        public string fName;        //имя файла
        public string fNameArc;     //имя файла архива
        public int errNumber;       //номер ошибки тестирования файла
        public int errCount;        //кол-во ошибок тестирования файла
        public string errTest;      //спикок ошибок тестирования файла
        public string errTxt;       //описание ошибки тестирования файла
    }
 
}
