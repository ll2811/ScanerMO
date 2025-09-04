using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Scaner
{
    class TestFile
    {
        Zip zp = new Zip { };

        public byte TestFileCSV(SkanFileInfo sf,int count_n ,int len_s, int field_n, string listTest, byte[] p)
        {//"len_s"-max длина строки; "field"-количество полей в строке; listTest-перечень тестов файла, разделённых символом  ","; p-массив с порядковыми номерами проверяемых полей
            byte sing = 0;      //флаг
            ulong test = 0;      //битовые флаги выявленных ошибок
            string err = "";    //выявленные локальные ошибки в ветках проверок
            sf.errNumber = 0;   //код ошибки обработки
            sf.errCount = 0;    //кол-во выявленных ошибок
            sf.errTest = "";    //текстовое описание выявленных ошибок

            string path = Path.Combine(sf.tmpDir, sf.fName);
            FileInfo fileInf = new FileInfo(@path);

            if (fileInf.Exists)
            {   // файл доступен
                if ((int)fileInf.Length != 0)
                {
                    int count = System.IO.File.ReadAllLines(@path).Length;  // кол-во строк в файле
                    int[] array = new int[count];   // номера строк в файле

                    // 1 - часть. проверка файла на корректность формата 
                    //проверки: (1),(9)
                    if (listTest.IndexOf("1") != -1)
                    {//(1)- проверка размера файла на минимальное значение(в байтах) с учётом числа: строк * (полей + обязательных полей(min длины) + коды перевода строк)
                        if ((int)fileInf.Length < (field_n + 3 * p.Length + 2) * count)
                        {
                            sf.errCount++;
                            sing = 1; test = 1;
                            sf.errTest = "(1)";
                        }
                    }
                    if (listTest.IndexOf("9") != -1)
                    {//(10) - проверка нормы количества строк
                        if (count_n <= count)
                        {
                            sf.errCount++;
                            sing = 1; test |= 256;
                            sf.errTest = (sf.errTest == "") ? "(9)" : sf.errTest+ ",(9)";
                        }
                    }

                    // 2 - часть(побайтовое чтение файла) проверка на корректность формату
                    //проверки: (2),(3),(4)
                    if (sing == 0)
                    {
                        using (FileStream fstream = File.OpenRead(@path))
                        {
                            if (listTest.IndexOf("2") != -1)
                            {//(2) - имеется некорректное двоичное значение, нарушающее принадлежность файла к текстовому формату
                                int k = 0; int b;
                                fstream.Seek(0, SeekOrigin.Begin);
                                for (int i = 0; i < fstream.Length; i++)
                                {
                                    b = fstream.ReadByte();
                                    if (!((b >= 0x20 && b <= 0x7A) || (b >= 0xE0 && b <= 0xFF) || (b >= 0xC0 && b <= 0xDF) || b == 0xA || b == 0xD || b == 0xB9 || b == 0xA8 || b == 0xB8))
                                    {
                                        k++;
                                    }
                                }
                                if (k > 0)
                                {
                                    sf.errCount++;
                                    sing = 2; test |= 2;
                                    sf.errTest = (sf.errTest == "") ? "(2)" : sf.errTest + ",(2)";
                                }
                            }
                            if (listTest.IndexOf("3") != -1)
                            {//(3) - файл не оканчивается на символ [;] или ["]
                                byte[] output = new byte[1];
                                fstream.Seek(-1, SeekOrigin.End); // перемещаем указатель в конец файла, отступаем на 1 байт назад
                                fstream.Read(output, 0, output.Length); // считываем байт в текущей позиции
                                if (!(((int)output[0] == 0x22) || ((int)output[0] == 0x3B)))
                                {
                                    sf.errCount++;
                                    sing = 2; test |= 4;
                                    sf.errTest = (sf.errTest == "") ? "(3)" : sf.errTest + ",(3)";
                                }
                            }
                            if (listTest.IndexOf("4") != -1)
                            {//(4)- в файле отсутствует код (возврат каретки + новая строка (CRLF)) или их кол-во != (кол-во строк)-1
                                int k = 0;
                                fstream.Seek(0, SeekOrigin.Begin);
                                for (int i = 0; i < fstream.Length; i++)
                                {
                                    if (0x0D == fstream.ReadByte())
                                    {
                                        k++;
                                    }
                                }
                                if (k != count - 1)
                                {
                                    sf.errCount++;
                                    sing = 2; test |= 8;
                                    sf.errTest = (sf.errTest == "") ? "(4)" : sf.errTest + ",(4)";
                                }
                            }
                        }
                    }

                    // 3 - часть(построчное чтение файла), проверка на корректность формату
                    //проверки: (5),(6),(7),(8),(10)
                    if (sing == 0)
                    {
                        using (StreamReader fstream = new StreamReader(path, System.Text.Encoding.Default))
                        {
                            int i = 1;          //текущий номер строки файла
                            string line = "";   //содержимое текущей строки
                            ulong testl = 0;      //битовые флаги выявленных ошибок
                            while ((line = fstream.ReadLine()) != null)
                            {
                                err = "";
                                if (listTest.IndexOf("5") != -1)
                                {//(5) - длина строки превышает допустимый размер
                                    if (line.Length > len_s)
                                    {
                                        sf.errCount++;
                                        sing = 3; testl |= 16;
                                        err = err + "(5)";
                                    }
                                }
                                if (listTest.IndexOf("6") != -1)
                                {// (6) проверка кол-во полей
                                    Regex regex = new Regex(@";");
                                    //Regex regex = new Regex(@"\b;\w*\b");
                                    MatchCollection matches = regex.Matches(line);
                                    if (matches.Count != field_n - 1)
                                    {
                                        sf.errCount++;
                                        sing = 3; testl |= 32;
                                        err = err + "(6)";
                                    }
                                    if ((test & 48) == 0 && listTest.IndexOf("10") != -1)
                                    {// (10) проверка наличие значений обязательных полей
                                        int s = 0; int b = 0; int t = 0;
                                        for (int k = 0; k <= p.Length - 1; k++)
                                        {
                                            if (p[k] == 1)
                                            {
                                                b = 0;
                                                t = matches[p[k] - 1].Index;
                                            }
                                            else
                                            {
                                                if ((p[k] == matches.Count + 1))
                                                    t = line.Length;
                                                else
                                                    t = matches[p[k] - 1].Index;
                                                b = matches[p[k] - 2].Index + 1;
                                            }
                                            s = t - b;
                                            if (line.Substring(b, s).Trim().Trim('"').Trim().Length == 0)
                                            {
                                                sf.errCount++;
                                                sing = 3; testl |= 512;
                                                err = ((err.Length > 0) ? (err + @", ") : "") + p[k] + @"(10)";  // (n(5)) - номер некорректного поля(ошибка) 
                                            }
                                            //if (line.Substring(s, matches[p[k]].Index - s).Trim().Trim('"').Length == 0)
                                            //{
                                            //    sf.errCount++;
                                            //    sing = test |= 16;
                                            //    err = err + " (" + (p[k] + 1) + "(5))";  // (n(5)) - номер некорректного поля(ошибка) 
                                            //}
                                            //if (k < p.Length-1) s = matches[p[k + 1] - 1].Index + 1;
                                        }
                                    }
                                }
                                if (listTest.IndexOf("7") != -1)
                                {// (7) - проверка на корректность номера записи
                                    string tmp = line.Substring(0, line.IndexOf(';', 0)).Trim().Trim('"');
                                    bool success = Int32.TryParse(tmp, out int number);
                                    if (success)
                                    {//заполнение массива порядковыми номерами строк, для проверки на уникальность
                                        array[i - 1] = Int32.Parse(tmp);
                                    }
                                    else
                                    {
                                        sf.errCount++;
                                        sing = 3; testl |= 64;
                                        err = err + "(7)";
                                    }
                                    if ((testl & 624) > 0)
                                    {
                                        sf.errTest = sf.errTest + i + " " + err + "\r\n";
                                    }
                                }
                                test |= testl;
                                testl &= 399;
                                i++;
                            }
                            bool isUnique = array.Distinct().Count() == array.Count();
                            if (!isUnique)
                            {// (8) проверка номера строки на уникальность, 2 варианта(работают на удивление быстро)
                             // 1. bool isUnique = array.Distinct().Count() == array.Count();
                             // 2. var diffChecker = new HashSet<int>(); bool allDifferent = array.All(diffChecker.Add);
                                sf.errCount++;
                                sing = 3; test |= 128;
                                sf.errTest = sf.errTest + "(8)";
                            }
                        }
                    }

                    if (sing==1)
                    {
                        if (test == 1) sf.errNumber = 15;
                        if (test == 256) sf.errNumber = 13;
                    }
                    if (sing == 2)
                    {
                        if ((test & 14) > 0) sf.errNumber = 18;
                    }
                    if (sing == 3)
                    {
                        if ((test & 752) > 0) sf.errNumber = 18;
                    }

                }
                else
                {// файл пустой
                    sing = 1;
                    sf.errCount = 1;
                    sf.errNumber = 15;
                    sf.errTxt = string.Format("Размера файла: \"" + Path.GetFileName(sf.fName) + "\" не соответствует минимальному значению, с учётом количества строк и обязательных сведений.");
                    sf.errTest = "(1)";
                }
            }
            else
            {// файл недоступен
                sing = 1;
                sf.errCount = 1;
                sf.errNumber = 4;
                sf.errTxt = string.Format("Файл: \"" + Path.GetFileName(sf.fName) + "\" - недоступен для работы.");
            }

            if (sf.errTest != "") TestOutErr();
            return sing;

            void TestOutErr()
            {//сформировать файл выявленных ошибок ФЛК
                string txt = "";
                string fName = @"err_" + Path.GetFileNameWithoutExtension(sf.fName) + @".txt"; //путь формирования файла со списком выявленных ошибок
                txt = string.Format("Файл: \"" + Path.GetFileName(sf.fName) + "\" содержит ошибки формата \"Соглашение по передаче сведений прикрепления ЗЛ к МО\".\r\nКоличество ошибок: " + sf.errCount);
                sf.errTxt = txt + "\r\nСписок ошибок будет выгружен в файл: \"" + fName + "\".";
                fName = Path.Combine(sf.arcOutDir, fName); //путь формирования файла со списком выявленных ошибок

                fName = Path.Combine(sf.tmpDir, fName);

                if (File.Exists(fName))
                    File.Delete(fName); //если есть файл - удалю

                WriteTextFile(fName, txt);
                string[] lines = {
                "------------------------------------------------------------------------------------------------------------",
                "Формат сообщения ошибок:",
                "  - ошибка уровня \"Файл\": (КодОшибки). Примеры осообщения шибки: (5);",
                "  - ошибка уровня \"Строка\": НомерСтроки (КодОшибки). Пример осообщения: 3 (5), (6);",
                "  - ошибка уровня \"Поле\": НомерСтроки НомерПоля(КодОшибки). Пример сообщения: 4 5(10), 24(10) ",
                "Описание кодов ошибок.",
                " Уровень \"Файл\" - эти ошибки прерывают дальнейшую обработку файла:",
                "    (1) - файл не содержит сведений или не соответствует минимальному значению, с учётом количества строк и обязательных сведений;",
                "    (2) - имеется некорректное двоичное значение, нарушающее принадлежность файла к текстовому формату;",
                "    (3) - файл не оканчивается на символ [;] или [\"];",
                "    (4) - в файле отсутствует(либо некорректна) последовательность кода разделения строк (CRLF);",
                "    (5) - длина строки превышает допустимый размер;",
                "    (6) - указано неверное количество полей в строке;",
                "    (7) - указано неверное значение номера строки;",
                "    (8) - нарушена уникальность порядкового номера строк;",
                "    (9) - файл содержит ненормированное количество строк.",
                " Уровень \"Строка\" - ошибки прерывают обработку строки:",
                "    (10) - отсутствует либо некорректное значение обязательного поля;",
                "    (11) - в контексте с другими значениями, отсутствует либо неверное значение условно-обязательного поля.",
                " Уровень \"Поле\" - ошибка не приводящая к прерыванию обработки строки, при условии необязательности поля;",
                "    (12) - отсутствует либо неверное значение необязательного поля.",
                "------------------------------------------------------------------------------------------------------------" };
                // + ((sf.errTest).Length > 100 ? sf.errTest.Remove(100) + "..." : sf.errTest))
                File.AppendAllLines(fName, lines, System.Text.Encoding.Default);
                WriteTextFile(fName, sf.errTest);
                if (File.Exists(fName))
                    zp.CreateZip(fName);   //упакую файл и перенесу в каталог arcInpDir
                fName = fName.Replace(".txt", ".zip");
                File.Copy(fName, Path.Combine(sf.outDir, Path.GetFileName(fName)), true);
                //File.Copy(fName, Path.Combine(sf.arcOutDir, Path.GetFileName(fName)), true);
                //File.Delete(fName);
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
                MessageBox.Show(string.Format("TestFileCSV. Ошибка записи в файл: '{0}'! {1}{2}", PathOutFile, ex.Message, DateTime.Now));
                return true;
            }
        }
    }
}

