using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Scaner
{
    public partial class UserMessageBox : Form
    {
        public UserMessageBox()
        {
            InitializeComponent();
            timer1.Interval = 10000; // 5 секунд
            timer1.Tick += Timer1_Tick;
        }
        private void UserMessageBox_Load(object sender, EventArgs e)
        {
            timer1.Start(); // Запускаем таймер при загрузке формы
        }
        private void Timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop(); // Останавливаем таймер
            this.Close(); // Закрываем форму
        }
    }
}
