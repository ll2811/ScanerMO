using System;
using System.IO;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.Zip;
//using System.Threading;
//using System.Runtime.InteropServices;


namespace Scaner
{
    class Zip
    {
        Utility ut = new Utility { };
        

        //[DllImport("user32.dll", SetLastError = true)]
        //static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        //[DllImport("user32.dll", SetLastError = true)]
        //static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        //const uint WM_CLOSE = 0x0010;

        public bool UnZipFile(string lpFileName, string lpDirName, string arcDir)
        {
            TestFile tf = new TestFile { };
            bool unzip = false;
            if (tf.ValidateZipFile(lpFileName))
            {
                // если каталога нет, то создать - извлечь файл
                using (ZipInputStream s = new ZipInputStream(System.IO.File.OpenRead(lpFileName)))
                {
                    ZipEntry theEntry;
                    while ((theEntry = s.GetNextEntry()) != null)
                    {
                        string fileName = lpFileName;
                        if (!Directory.Exists(lpDirName))
                            Directory.CreateDirectory(lpDirName);
                        if (fileName != String.Empty)
                        {
                            if (System.IO.File.Exists(lpDirName + "\\" + theEntry.Name)) // если в каталоге есть файл - удалю
                                System.IO.File.Delete(lpDirName + "\\" + theEntry.Name);
                            using (System.IO.FileStream streamWriter = System.IO.File.Create(lpDirName + "\\" + theEntry.Name))
                            {
                                int size = 2048;
                                byte[] data = new byte[2048];
                                while (true)
                                {
                                    size = s.Read(data, 0, data.Length);
                                    if (size > 0)
                                    {
                                        streamWriter.Write(data, 0, size);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                unzip = true;
                            }
                        }
                    }
                }
            }
            else
            {
                //MessageBox.Show(string.Format("Файл: '{0}' - не соответствует своему формату,\nбудет перемещён в папку \"Temp\", каталога обработанных файлов! {1}", lpFileName, DateTime.Now), "Ошибка чтения файла! Сообщение закроется через 5 сек.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //MessageBox.Show("Ошибка чтения файла.");
                //Thread.Sleep(5000);
                //IntPtr mbWnd = FindWindow(null, "Ошибка чтения файла.");
                //if (mbWnd != IntPtr.Zero)
                //{
                //    // Отправляем сообщение о закрытии
                //    PostMessage(mbWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                //}
                using (var form = new UserMessageBox())
                {
                    form.ShowDialog(); // Окно закроется автоматически через 5 секунд
                }

                string arcTemp = arcDir + @"Temp\";  //склад - полученных "кривых" файлов
                if (!Directory.Exists(arcTemp))
                    Directory.CreateDirectory(arcTemp);
                ut.FileMove(lpFileName, Path.Combine(arcTemp, Path.GetFileName(lpFileName))); //файл перемещён в arcTemp
            }
            return unzip;
        }

        /// <summary>
        /// Упаковать файл в каталог нахождения исходного файла
        /// </summary>
        /// <param name="InpFile"></param>
        public bool CreateZip(string InpFile)
        {   // 1-если в каталоге есть аналог файла, удалить; 2-упаковать файл; 3-удалить исходный файл
            string OutFile;
            try
            {
                OutFile = InpFile.Replace(Path.GetExtension(InpFile), ".ZIP");
                if (File.Exists(OutFile)) // если в каталоге есть файл - удалю
                    File.Delete(OutFile);
                using (ZipOutputStream s = new ZipOutputStream(System.IO.File.Create(OutFile)))
                {
                    s.SetLevel(9); // 0 - store only to 9 - means best compression
                    byte[] buffer = new byte[4096];
                    ZipEntry entry = new ZipEntry(Path.GetFileName(InpFile))
                    {
                        DateTime = DateTime.Now
                    };
                    s.UseZip64 = UseZip64.Off;
                    s.PutNextEntry(entry);
                    using (System.IO.FileStream fs = System.IO.File.OpenRead(InpFile))
                    {
                        // Using a fixed size buffer here makes no noticeable difference for output
                        // but keeps a lid on memory usage.
                        int sourceBytes;
                        do
                        {
                            sourceBytes = fs.Read(buffer, 0, buffer.Length);
                            s.Write(buffer, 0, sourceBytes);
                        } while (sourceBytes > 0);
                    }
                    s.Finish();
                    s.Close();
                }
                System.IO.File.Delete(InpFile);
                return false;
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Ошибка при создании архива {0}", ex);
                //this.SetTextBox6(string.Format("Ошибка при создании архива. (Ошибка: {0})", ex));
                //MessageBox.Show(string.Format("Ошибка при создании архива. (Ошибка: {0})", ex));
                MessageBox.Show(string.Format("Ошибка при создании архива.: '{0}'! {1}{2}", InpFile, ex.Message, DateTime.Now));
                return true;
            }
        }
        /// <summary>
        /// Упаковать файл в каталог, отличный от каталога исходного файла
        /// </summary>
        /// <param name="InpFile"></param>
        /// <param name="OutFile"></param>
        public bool CreateZip(string InpFile, string OutFile)
        {   // 1-создать каталог, если его нет; 2-очистить каталог от аналога файла; 3-упаковать файл; 4-удалить исходный файл
            if (!Directory.Exists(Path.GetDirectoryName(OutFile)))
                Directory.CreateDirectory(Path.GetDirectoryName(OutFile));
            // Упаковать zip-файл в папку
            try
            {
                if (File.Exists(OutFile)) // если в каталоге есть файл - удалю
                    File.Delete(OutFile);
                using (ZipOutputStream s = new ZipOutputStream(System.IO.File.Create(OutFile)))
                {
                    s.SetLevel(9); // 0 - store only to 9 - means best compression
                    byte[] buffer = new byte[4096];
                    ZipEntry entry = new ZipEntry(Path.GetFileName(InpFile))
                    {
                        DateTime = DateTime.Now
                    };
                    s.UseZip64 = UseZip64.Off;
                    s.PutNextEntry(entry);
                    using (System.IO.FileStream fs = System.IO.File.OpenRead(InpFile))
                    {
                        // Using a fixed size buffer here makes no noticeable difference for output
                        // but keeps a lid on memory usage.
                        int sourceBytes;
                        do
                        {
                            sourceBytes = fs.Read(buffer, 0, buffer.Length);
                            s.Write(buffer, 0, sourceBytes);
                        } while (sourceBytes > 0);
                    }
                    s.Finish();
                    s.Close();
                }
                System.IO.File.Delete(InpFile);
                return false;
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Ошибка при создании архива {0}", ex);
                //this.SetTextBox6(string.Format("Ошибка при создании архива. (Ошибка: {0})", ex));
                //MessageBox.Show(string.Format("Ошибка при создании архива. (Ошибка: {0})", ex));
                MessageBox.Show(string.Format("Ошибка при создании архива.: '{0}'! {1}{2}", InpFile, ex.Message, DateTime.Now));
                return true;
            }
        }
    }
}
