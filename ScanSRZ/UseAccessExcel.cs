using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scaner
{
    class UseAccessExcel
    {
        class AccessReader
        {
            public DataSet GetAccDBFile(string pathName, string fileName, string tblName)
            {
                //// строка подключения к MS Access
                //// 2 варианта
                // строка подключения к MS Access
                //public static string connectString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=Workers.mdb;";
                //public static string connectString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=Workers.mdb;";
                //// поле - ссылка на экземпляр класса OleDbConnection для соединения с БД
                //private OleDbConnection myConnection;
                //// создаем экземпляр класса OleDbConnection
                //myConnection = new OleDbConnection(connectString);
                //// открываем соединение с БД
                //myConnection.Open();

                //// ------------------------------------------------------------------------------------------
                ////1 запрос select
                //string query = "SELECT w_name FROM Worker WHERE w_id = 1";
                //// создаем объект OleDbCommand для выполнения запроса к БД MS Access
                //OleDbCommand command = new OleDbCommand(query, myConnection);
                //// выполняем запрос и выводим результат в textBox1
                //textBox1.Text = command.ExecuteScalar().ToString();
                //// ------------------------------------------------------------------------------------------
                ////2 запрос select - многостроковый
                //string query = "SELECT w_name, w_position, w_salary FROM Worker ORDER BY w_salary";
                //// создаем объект OleDbCommand для выполнения запроса к БД MS Access
                //OleDbCommand command = new OleDbCommand(query, myConnection);
                //// получаем объект OleDbDataReader для чтения табличного результата запроса SELECT
                //OleDbDataReader reader = command.ExecuteReader();
                //// очищаем listBox1
                //listBox1.Items.Clear();
                ////в цикле построчно читаем ответ от БД
                //while (reader.Read())
                //{
                //    // выводим данные столбцов текущей строки в listBox1
                //    listBox1.Items.Add(reader[0].ToString() + " " + reader[1].ToString() + " " + reader[2].ToString() + " ");
                //}
                //// закрываем OleDbDataReader
                //reader.Close();
                //// ------------------------------------------------------------------------------------------
                ////3 запрос insert
                //string query = "INSERT INTO Worker (w_name, w_position, w_salary) VALUES ('Михаил', 'Водитель', 20000)";
                //// создаем объект OleDbCommand для выполнения запроса к БД MS Access
                //OleDbCommand command = new OleDbCommand(query, myConnection);
                //// выполняем запрос к MS Access
                //command.ExecuteNonQuery();
                //// ------------------------------------------------------------------------------------------
                ////4 запрос update
                //string query = "UPDATE Worker SET w_salary = 123456 WHERE w_id = 3";
                //// создаем объект OleDbCommand для выполнения запроса к БД MS Access
                //OleDbCommand command = new OleDbCommand(query, myConnection);
                //// выполняем запрос к MS Access
                //command.ExecuteNonQuery();
                //// ------------------------------------------------------------------------------------------
                ////5 запрос delete
                //string query = "DELETE FROM Worker WHERE w_id < 3";
                //// создаем объект OleDbCommand для выполнения запроса к БД MS Access
                //OleDbCommand command = new OleDbCommand(query, myConnection);
                //// выполняем запрос к MS Access
                //command.ExecuteNonQuery();


                //// заркываем соединение с БД
                //myConnection.Close();

                //argsStr = "exec dbo.sp_OutFSB";     //сформирую строку обработки
                //RunCommandSQL(GetConnectionString(), @argsStr).Wait();     //
                //if (flag)
                //{   // если обработка выполнена корректно, выгружу протокол
                //}

                using (OleDbConnection AccessConn =
                new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + pathName + ";Extended Properties=;"))
                {
                    OleDbCommand AccessCommand = new OleDbCommand(@"SELECT * FROM " + fileName, AccessConn);
                    OleDbDataAdapter ExcelAdapter = new OleDbDataAdapter(AccessCommand);
                    AccessConn.Open();
                    DataSet AccessDataSet = new DataSet(tblName);
                    ExcelAdapter.Fill(AccessDataSet);
                    return AccessDataSet;
                }
            }
        }
        class ExcelReader
        {
            public DataSet GetCVSFile(string pathName, string fileName, string tblName)
            {
                using (OleDbConnection ExcelConn =
                new OleDbConnection(@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + pathName + ";Extended Properties=Text;"))
                //            new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + pathName + ";Extended Properties=Text;"))
                //                connStringExcel = "provider=microsoft.jet.oledb.4.0;data source=" + Commons.XLSFedOrderPlanFilesFolderPath + "Order" + orderNum.ToString() + ".XLS ;extended properties=\"excel 8.0;hdr=yes;IMEX=1\"";


                {
                    OleDbCommand ExcelCommand = new OleDbCommand(@"SELECT * FROM " + fileName, ExcelConn);
                    OleDbDataAdapter ExcelAdapter = new OleDbDataAdapter(ExcelCommand);
                    ExcelConn.Open();
                    DataSet ExcelDataSet = new DataSet(tblName);
                    ExcelAdapter.Fill(ExcelDataSet);
                    return ExcelDataSet;
                }
            }
        }
    }
}
