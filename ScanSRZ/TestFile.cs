using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Scaner
{
    class TestFile
    {
        Zip zp = new Zip { };
        public bool ValidateZipFile(string fileToTest)
        {
            byte[] codezip; //50 4B 03 04 14 00 00 00 08 00   или   50 4B 30 30 50 4B 03 04
            codezip = new byte[5] { 0x50, 0x4B, 0x03, 0x04, 0x14 };//, 0x00, 0x00, 0x00, 0x08, 0x00 };
            bool test = true;
            try
            {
                string path = fileToTest;
                FileInfo fileInf = new FileInfo(@path);

                if (fileInf.Exists)
                {   // файл доступен
                    if ((int)fileInf.Length != 0)
                    {//(побайтовое чтение файла) проверка на корректность формату
                        using (FileStream fstream = File.OpenRead(@path))
                        {
                            int b;
                            fstream.Seek(0, SeekOrigin.Begin);
                            for (int i = 0; (i < fstream.Length) & (i < codezip.Length - 1); i++)
                            {
                                b = fstream.ReadByte();
                                if (b != codezip[i])
                                {
                                    test = false;
                                }
                            }
                        }
                    }
                    else test = false;
                }
                else test = false;
            }
            catch
            {
                test = false;

                string txt = "";
                string fName = @"err_" + Path.GetFileNameWithoutExtension(fileToTest) + @".txt"; //имя файла ошибок
                txt = string.Format("Ошибка обработки. Файл: \"" + Path.GetFileName(fileToTest) + "\" не соотвествует формату описанному в: \"Порядок по передаче сведений прикрепления ЗЛ к МО\"\r\nлибо не содержит информации для обработки.");

                string tmpDir = Properties.Settings.Default.Path_Tmp;
                string outDir = Properties.Settings.Default.Path_Out;
                fName = Path.Combine(tmpDir, fName); //путь для файла ошибок

                if (File.Exists(fName))
                    File.Delete(fName); //если есть файл - удалю

                WriteTextFile(fName, txt);

                //string[] lines = {" "};
                //File.AppendAllLines(fName, lines, System.Text.Encoding.Default);
                //WriteTextFile(fName, sf.errTest);
                if (File.Exists(fName))
                    zp.CreateZip(fName);   //упакую файл и перенесу в каталог arcInpDir
                fName = fName.Replace(".txt", ".zip");
                File.Copy(fName, Path.Combine(outDir, Path.GetFileName(fName)), true);
            }

            return test;
        }

        public byte TestFileCSV(SkanFileInfo sf,int count_n ,int len_s, int field_n, string listTest, byte[] p)
        {//count_n-макс.число строк; len_s-max длина строки; field-количество полей в строке; listTest-перечень тестов файла, разделённых символом-','; p-массив с порядковыми номерами проверяемых полей
 
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
                    int count = Convert.ToInt32(File.ReadAllLines(@path).Length);  // кол-во строк в файле
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
                        if (count_n < count)
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
                                        err = (err == "") ? "-" + i : err + "," + i;
                                    }
                                }
                                if (k > 0)
                                {
                                    sf.errCount++;
                                    sing = 2; test |= 2;
                                    sf.errTest = (sf.errTest == "") ? "(2)" : sf.errTest + ",(2)";
                                    sf.errTest = sf.errTest + err;
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
                                int k1 = 0; int k2 = 0; int b = 0;
                                fstream.Seek(0, SeekOrigin.Begin);
                                for (int i = 0; i < fstream.Length; i++)
                                {
                                    b = fstream.ReadByte();
                                    if (0x0D == b) k1++;
                                    if (0x0A == b) k2++;
                                }
                                if (k1 != k2 || k1 != count - 1)
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
                        if ((test & 240) > 0)
                        {
                            sf.errNumber = 18;    //исключу из ошибок блокирующих обработку ошибки обязательности полей (10)
                        }
                        else 
                        {
                            sing = 0;
                        }
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
            {//сформировать файл ошибок ФЛК
                string txt = "";
                string fName = @"err_" + Path.GetFileNameWithoutExtension(sf.fName) + @".txt"; //имя файла ошибок
                txt = string.Format("Файл: \"" + Path.GetFileName(sf.fName) + "\" содержит ошибки формата \"Соглашение по передаче сведений прикрепления ЗЛ к МО\".\r\nКоличество ошибок: " + sf.errCount);
                sf.errTxt = txt + "\r\nСписок ошибок будет выгружен в файл: \"" + fName + "\".";

                fName = Path.Combine(sf.arcOutDir, fName); //путь для файла ошибок
                //fName = Path.Combine(sf.tmpDir, fName); //путь для файла ошибок

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
                "    (2) - файл содержит байтовое значение, отличное от кодировки Microsoft CP1251, после тире, через запятую будет указан выявленный адрес(Dec) байта от начала файла;",
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

        public byte StreamTestFile(SkanFileInfo sf, int field, string listTest, byte[] p)
        {//проверка файла. "field"-количество полей в строке; listTest-перечень тестов файла, разделённых символом  ","; p-массив с порядковыми номерами проверяемых полей
            byte sing = 0;      //флаг
            byte test = 0;      //битовые флаги выявленных ошибок
            sf.errNumber = 0;   //код ошибки обработки
            sf.errCount = 0;    //кол-во выявленных ошибок
            sf.errTest = "";    //текстовое описание выявленных ошибок

            string path = Path.Combine(sf.tmpDir, sf.fName);
            FileInfo fileInf = new FileInfo(@path);
            if (fileInf.Exists)
            {
                if ((int)fileInf.Length != 0)
                {// непустой файл
                    if (listTest.IndexOf("1") != -1)
                    {// проверка размера файла на минимальное значение с учётом числом полей и числом обязательных полей
                        if ((int)fileInf.Length < (field + 3 * p.Length))
                        {
                            sing = test = 1;
                            sf.errCount++;
                            sf.errTest = "(1)\r\n";
                        }
                    }
                    if (listTest.IndexOf("2") != -1)
                    {// проверка последнего байта в файле на соответствие "двойной ковычке" или "точке с запятой"
                        using (FileStream fstream = File.OpenRead(@path))
                        {
                            byte[] output = new byte[1];
                            fstream.Seek(-1, SeekOrigin.End); // перемещаем указатель в конец файла, отступаем на 1 байт назад
                            fstream.Read(output, 0, output.Length); // считываем байт в текущей позиции
                                                                    //проверка последнего значения в файле на соответствие "двойной ковычке" или "точке с запятой"
                            if (!(((int)output[0] == 34) || ((int)output[0] == 59)))
                            {
                                sing = test |= 2;
                                sf.errCount++;
                                sf.errTest = sf.errTest + "(2)\r\n";
                            }
                        }
                    }
                    if (sing == 0 & listTest.IndexOf("3") != -1)
                    {// проверка корректности формата файла
                        using (StreamReader fstream = new StreamReader(path, System.Text.Encoding.Default))
                        {
                            int i = 1;          //номер строки файла
                            string line = "";   //содержимое текущей строки
                            string err = "";    //выявленные ошибки

                            while ((line = fstream.ReadLine()) != null)
                            {
                                Regex regex = new Regex(@";");
                                //Regex regex = new Regex(@"\b;\w*\b");
                                MatchCollection matches = regex.Matches(line);

                                if (listTest.IndexOf("4") != -1)
                                { //проверка кол-во полей
                                    if (matches.Count != field - 1)
                                    {
                                        sf.errCount++;
                                        sing = test |= 4;
                                        err = err + "(3)";  // (3) - ошибка числа полей
                                    }
                                }
                                if (test != 4 & listTest.IndexOf("5") != -1)
                                {// проверка номер строки
                                    //tmp = line.Substring(0, line.IndexOf(';', 0)).Trim().Trim('"');
                                    //bool success = Int32.TryParse(tmp, out int number);
                                    if (!Int32.TryParse(line.Substring(0, line.IndexOf(';', 0)).Trim().Trim('"'), out int number))
                                    {
                                        sf.errCount++;
                                        sing |= test |= 8;
                                        err = err + "(4)";  // (4) - номер записи некорректен
                                    }
                                }
                                if (test != 4 & listTest.IndexOf("6") != -1)
                                {// проверка наличие данных обязательных полей
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
                                            sing |= test |= 16;
                                            err = ((err.Length > 0) ? (err + @", ") : "") + p[k] + @"(5)";  // (n(5)) - номер некорректного поля(ошибка) 
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
                                if (test > 3)
                                {
                                    sf.errTest = sf.errTest + i + " " + err + "\r\n";
                                }
                                err = "";
                                test &= 3;
                                i++;
                            }
                        }
                    }
                    if (sing > 0)
                    {
                        TestOutErr();
                    }
                    if ((sing & 15) > 0)
                    {
                        sf.errNumber = 13;
                        sing = 1;
                    }
                    else if ((sing & -16) > 0)
                    {
                        sf.errNumber = 18;
                        sing = 1;
                    }
                }
                else
                { // файл пустой
                    sing = 1;
                    sf.errNumber = 15;
                }
            }
            else
            {// файл недоступен
                sing = 1;
                sf.errNumber = 4;
            }
            return sing;

            void TestOutErr()
            {//сформировать файл выявленных ошибок ФЛК
                string txt = "";
                txt = string.Format("Файл: \"" + sf.fName + "\" содержит ошибки формата.\r\nКоличество ошибок: " + sf.errCount);
                sf.fName = Path.Combine(sf.arcOutDir, @"err_" + Path.GetFileNameWithoutExtension(sf.fName).Replace("MO", "E") + @".txt"); //путь формирования файла со списком выявленных ошибок
                sf.errTxt = txt + "\r\nСписок ошибок будет выгружен в файл:\r\n\"" + sf.fName + "\", а также в каталог \"" + sf.outDir + "\" для отправки в МО.";

                if (File.Exists(sf.fName))
                    File.Delete(sf.fName); //если есть файл - удалю
                //txt = string.Format("Файл: \"" + sf.fName + "\" содержит ошибки формата соглашения по передаче сведений.\r\nКоличество ошибок: " + sf.errCount);

                WriteTextFile(sf.fName, txt);
                string[] lines = { "-------------------------------------------------------------------------------------", "Формат сообщений ошибок.", "  Ошибка уровня файла(прерывает обработку файла) - (КодОшибки).", "  Ошибка уровня строки(прерывает обработку записи) - НомерСтроки (КодОшибки).", "  Ошибка уровня поля   - НомерСтроки НомерПоля(КодОшибки).", "Описание кодов ошибок:", "  1 - файл не содержит сведений,", "  2 - файл не оканчивается символом ';' или '\"',", "  3 - неверное количество полей,", "  4 - неверное значение номера записи,", "  5 - отсутствует значение обязательного поля,", "  6 - отсутствует значение условно-обязательного поля,", "  7 - неверное значение поля,", "  8 - превышение норматива прикрепления к доктору.", "-------------------------------------------------------------------------------------", sf.errTest };
                // + ((sf.errTest).Length > 100 ? sf.errTest.Remove(100) + "..." : sf.errTest))
                File.AppendAllLines(sf.fName, lines, System.Text.Encoding.Default);
                //WriteTextFile(sf.fName, sf.errTest);
                File.Copy(sf.fName, Path.Combine(sf.outDir, Path.GetFileName(sf.fName)), true);
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

