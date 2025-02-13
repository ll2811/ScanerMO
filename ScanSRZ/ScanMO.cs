        private void ScanMO(string f/*, SkanFileInfo sf*/) {
            int id;     // порядковый номер файла в таблице MO_LOG
            string mo;  // код МО
            int tk;     // счётчик тактов
            Guid g;     // ид файла
            int i;      // кол-во сформированных откреплений
            string txt = ""; //

            sf.fName = Path.GetFileName(f.ToUpper());   //имя файла обрабатываемого прикрепления

            sf.arcInpDir = this.textBox3.Text + @"Inp\" + String.Join(@"\", sf.fName.Substring(9, 4), sf.fName.Substring(13, 2), sf.fName.Substring(15, 2), @"\");  //склад - полученные файлы
            if (!Directory.Exists(sf.arcInpDir))
                Directory.CreateDirectory(sf.arcInpDir);

            sf.arcOutDir = this.textBox3.Text + @"Out\" + String.Join(@"\", sf.fName.Substring(9, 4), sf.fName.Substring(13, 2), sf.fName.Substring(15, 2), @"\");  //склад - ответные файлы 
            if (!Directory.Exists(sf.arcOutDir))
                Directory.CreateDirectory(sf.arcOutDir);

            FileMove(f, Path.Combine(sf.tmpDir, sf.fName)); //перенесу в tmpDir

            if (StreamTestFail(24, "1,2,3,4,5,6", new byte[] { 1, 2, 3, 6, 8, 9, 16, 17, 19 }) == 0) //проверка - корректность формата
            {   //корректный формат
                sf.argsStr = "select top 1 id from PolicyRegister.dbo.MO_LOG where fname='" + sf.fName + "' and [Status]=1 order by DateLoad desc";   //запрос-проверка обработки файла ранее
                if (GetIdRecordTbl(@sf.argsStr) == 0) //проверка обработки
                {
                    sf.argsStr = "if exists (select 1 from dbo.MO_FS where FileName='" + sf.fName + "' and [Status]=0) delete from dbo.MO_FS where FileName='" + sf.fName + "';";   //запрос-если обработка повторна и некорректна - удалю запись об файле
                    RunCommandSQL(GetConnectionString(), @sf.argsStr).Wait();
                    if (flag)
                    {   //новый файл
                        tk = Environment.TickCount;
                        this.SetTextBox6(string.Format("Получен файл: {0}: в {1}", sf.fName, DateTime.Now));
                        g = Guid.NewGuid(); // определю GUIDs для файла.

                        StreamTextToServer(Path.Combine(sf.tmpDir, sf.fName), g).Wait();   //загружу в таблицу БД
                        if (flag)
                        {   //если загрузка в таблицу выполнилась корректно
                            sf.fNameArc = sf.fName.Replace(".CSV", ".ZIP");   //имя текущего архива
                            CreateZip(Path.Combine(sf.tmpDir, sf.fName), Path.Combine(sf.arcInpDir, sf.fNameArc));   //упакую и перенесу в каталог arcInpDir

                            //обработка файла МО
                            sf.argsStr = "exec dbo.sp_FileProcess_mo_2_2 '" + @f + "', 2, '" + g + "'";   //запрос для обработки файла
                            RunCommandSQL(GetConnectionString(), @sf.argsStr).Wait(); //обработка
                            if (flag)
                            {   //если обработка выполнена корректно
                                StreamBLOBToServer(Path.Combine(sf.arcInpDir, sf.fNameArc), g).Wait();   //сохраню обработанный файл в FileStreams БД
                                                                                                         //выгрузка протокола
                                sf.fName = sf.fName.Replace("MO", "E");   //рабочее имя файла протокола
                                OwnedToMO_CSV(Path.Combine(sf.tmpDir, sf.fName), "tmp_mo_f", ""); //выгрузка протокола

                                sf.fNameArc = sf.fName.Replace(".CSV", ".ZIP");   //рабочее имя архива протокола
                                CreateZip(Path.Combine(sf.tmpDir, sf.fName), Path.Combine(sf.arcOutDir, sf.fNameArc));
                                FileCopy(Path.Combine(sf.arcOutDir, sf.fNameArc), Path.Combine(sf.outDir, sf.fNameArc));

                                //выгрузка откреплений
                                sf.argsStr = "select top 1 id from [policyregister].[dbo].[mo_log] where fname = '" + Path.GetFileName(f) + "' order by id desc";  //запрос-для определения номера файла
                                id = GetIdRecordTbl(@sf.argsStr);  //получу номер файла
                                if (id > 0)
                                {
                                    sf.argsStr = "insert PeopleAttach.dbo.MO_LOG_STATUS(id, dt, status) values(" + id + ", getdate(), 1);"; //запрос-признак выполнения выгрузки протокола
                                    GetIdRecordTbl(@sf.argsStr);

                                    mo = Path.GetFileName(f).Substring(3, 6); // код МО
                                                                              //запрос-определения списка МО для формирования файлов открепления
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
                                                    //this.SetTextBox6(string.Format("Откреплено от МО: {0}. Кол-во: {1}", reader.GetValue(0).ToString(), reader.GetValue(1).ToString()));
                                                    sf.argsStr = "exec dbo.sp_FileProcess_mo_3_1 " + id + ",'" + reader.GetValue(0).ToString() + "'";   //запрос-обработка
                                                    RunCommandSQL(GetConnectionString(), @sf.argsStr).Wait(); //обработка файла
                                                    if (flag)
                                                    {   // если обработка выполнена, выгружу открепление
                                                        sf.argsStr = "select FileNum from PeopleAttach.dbo.MO_FILE_STATIST where MO = " + reader.GetValue(0).ToString() + " and FileYear = year(getdate())";  //запрос получения номера файла открепления
                                                        int k = (int)GetIdRecordTbl(@sf.argsStr);
                                                        if (k == 0)
                                                        { //запрос-вставка инф-ция по файлу открепления
                                                            sf.argsStr = "insert into PeopleAttach.dbo.MO_FILE_STATIST(MO,FileYear,FileNum) values(" + reader.GetValue(0).ToString() + ",year(getdate()),1);" +
                                                                "select FileNum from PeopleAttach.dbo.MO_FILE_STATIST where MO = " + reader.GetValue(0).ToString() + " and FileYear = year(getdate())";
                                                        }
                                                        else
                                                        { //запрос-обновить инф-цию по файлу открепления
                                                            sf.argsStr = "update PeopleAttach.dbo.MO_FILE_STATIST set FileNum = FileNum + 1 where MO = " + reader.GetValue(0).ToString() + " and FileYear = year(getdate());" +
                                                                "select FileNum from PeopleAttach.dbo.MO_FILE_STATIST where MO = " + reader.GetValue(0).ToString() + " and FileYear = year(getdate())";
                                                        }

                                                        sf.fName = @"O2" + reader.GetValue(0).ToString() + DateTime.Now.ToString("yyyyMMdd") + GetIdRecordTbl(@sf.argsStr).ToString() + @".CSV"; //сформирую имя файла открепления
                                                        OwnedToMO_CSV(Path.Combine(sf.tmpDir, sf.fName), "tmp_mo_f", "");

                                                        sf.fNameArc = sf.fName.Replace(".CSV", ".ZIP");   //определю имя архива
                                                        FileDelete(Path.Combine(sf.arcOutDir, sf.fNameArc));  //если есть файл - удалю
                                                        CreateZip(Path.Combine(sf.tmpDir, sf.fName), Path.Combine(sf.arcOutDir, sf.fNameArc));   //упакую
                                                        FileDelete(Path.Combine(sf.outDir, sf.fNameArc));  //если в есть файл - удалю
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
                                            this.SetTextBox6(ex.Message);
                                        }
                                    }
                                }
                                this.SetTextBox6(string.Format("Время обработки: ({0})", GetDate(Environment.TickCount - tk)));
                            }
                            else
                            {   //если обработка выполнена некорректно
                                sf.fName = Path.GetFileName(f.ToUpper()).Replace("MO", "E");
                                sf.fNameArc = sf.fNameArc.Replace(".CSV", ".ZIP");
                                OwnedToMO_CSV(Path.Combine(sf.tmpDir, sf.fName), "tmp_mo_f", "");
                                CreateZip(Path.Combine(sf.tmpDir, sf.fName), Path.Combine(sf.arcOutDir, sf.fNameArc));
                                FileCopy(Path.Combine(sf.arcOutDir, sf.fNameArc), Path.Combine(sf.outDir, sf.fNameArc));
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
                }
            }
            else
            {   //некорректный формат, сформирую описание ошибки
                txt = string.Format(errList[11] + " Файл: \"" + Path.GetFileName(f) + "\" " + errList[13] + ". Кол-во ошибок: " + sf.errCount + ". Список ошибок(код/строка): " + ((sf.errTest).Length > 100 ? sf.errTest.Remove(100) + "..." : sf.errTest));
            }
            if (txt != "") 
            {
                if (File.Exists(Path.Combine(sf.tmpDir, sf.fName)))
                    CreateZip(Path.Combine(sf.tmpDir, sf.fName), Path.Combine(sf.arcInpDir, sf.fName.Replace(".CSV", ".ZIP")));   //упакую файл и перенесу в каталог arcInpDir
                this.SetTextBox6(txt);
                sf.fName = sf.fName.Replace("MO", "E");     //имя протокола ответа
                WriteTextFile(Path.Combine(sf.arcOutDir, sf.fName), txt);   //сформирую ответ в МО
                CreateZip(Path.Combine(sf.arcOutDir, sf.fName));
                sf.fNameArc = sf.fName.Replace(".CSV", ".ZIP");
                FileCopy(Path.Combine(sf.arcOutDir, sf.fNameArc), Path.Combine(sf.outDir, sf.fNameArc));
            }
        }