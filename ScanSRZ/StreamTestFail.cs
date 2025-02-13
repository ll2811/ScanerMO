         private byte StreamTestFail(int field, string listTest, byte[] p)
        {//"field"-количество полей в строке; listTest-перечень тестов файла, разделЄнных символом  ","; p-массив с пор€дковыми номерами провер€емых полей
            byte sing = 0;      //флаг
            byte test = 0;      //битовые флаги вы€вленных ошибок
            sf.errNumber = 0;   //код ошибки обработки
            sf.errCount = 0;    //кол-во вы€вленных ошибок
            sf.errTest = "";    //текстовое описание вы€вленных ошибок

            string path = Path.Combine(sf.tmpDir, sf.fName);
            FileInfo fileInf = new FileInfo(@path);
            if (fileInf.Exists)
            {
                if ((int)fileInf.Length != 0)
                {// непустой файл
                    if (listTest.IndexOf("1") != -1)
                    {// проверка размера файла на минимальное значение с учЄтом числом полей и числом об€зательных полей
                        if ((int)fileInf.Length < (field + 3 * p.Length))
                        {
                            sing = test = 1;
                            sf.errNumber = 15;
                            sf.errCount++;
                            sf.errTest = "(1)\r\n";
                        }
                    }
                    if (listTest.IndexOf("2") != -1)
                    {// проверка последнего байта в файле на соответствие "двойной ковычке" или "точке с зап€той"
                        using (FileStream fstream = File.OpenRead(@path))
                        {
                            byte[] output = new byte[1];
                            fstream.Seek(-1, SeekOrigin.End); // перемещаем указатель в конец файла, отступаем на 1 байт назад
                            fstream.Read(output, 0, output.Length); // считываем байт в текущей позиции
                                                                    //проверка последнего значени€ в файле на соответствие "двойной ковычке" или "точке с зап€той"
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
                            string err = "";    //вы€вленные ошибки

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
                                if (sing != 4 & listTest.IndexOf("5") != -1)
                                {// проверка номер строки
                                    //tmp = line.Substring(0, line.IndexOf(';', 0)).Trim().Trim('"');
                                    //bool success = Int32.TryParse(tmp, out int number);
                                    if (!Int32.TryParse(line.Substring(0, line.IndexOf(';', 0)).Trim().Trim('"'), out int number))
                                    {
                                        sf.errCount++;
                                        sing = test |= 8;
                                        err = err + "(4)";  // (4) - номер записи некорректен
                                    }
                                }
                                if (sing != 4 & listTest.IndexOf("6") != -1)
                                {// проверка наличие данных об€зательных полей
                                    int s = 0; int b = 0; int t = 0;
                                    for (int k = 0; k <= p.Length - 1; k++)
                                    {
                                        if (p[k] == 1)
                                        {
                                            b = 0;
                                            t = matches[p[k]-1].Index;
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
                                            sing = test |= 16;
                                            err = ((err.Length > 0) ? (err + @", "):"") + @"(" + p[k] + @"(5))";  // (n(5)) - номер некорректного пол€(ошибка) 
                                        }
                                        //if (line.Substring(s, matches[p[k]].Index - s).Trim().Trim('"').Length == 0)
                                        //{
                                        //    sf.errCount++;
                                        //    sing = test |= 16;
                                        //    err = err + " (" + (p[k] + 1) + "(5))";  // (n(5)) - номер некорректного пол€(ошибка) 
                                        //}
                                        //if (k < p.Length-1) s = matches[p[k + 1] - 1].Index + 1;
                                    }
                                }
                                if (test > 3)
                                    sf.errTest = sf.errTest + i + " " + err + "\r\n";
                                err = "";
                                test &= 3;
                                i++;
                            }
                        }
                    }
                }
                else
                { // файл пустой
                    sing = test = 1;
                    sf.errNumber = 15;
                }
            }
            else
            {// файл недоступен
                sing = test = 1;
                sf.errNumber = 4;
            }
            if (sing > 0) sf.errNumber = 13;
            return sing;
        }