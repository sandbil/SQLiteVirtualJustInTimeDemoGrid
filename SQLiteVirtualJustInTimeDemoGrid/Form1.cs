using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VirtualJustInTimeDemoGrid;

namespace SQLiteVirtualJustInTimeDemoGrid
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private Cache memoryCache;
        private string connectionString = "Data Source = ..\\..\\db\\test.db";
        private string table = "table1";
        private string filterStr = null;
        private string[] fields = { "status", "txtAdr", "realObj", "level","timeNer" };



        private void Form1_Load(object sender, EventArgs e)
        {
            DataRetriever retriever = new DataRetriever(connectionString, table, fields, filterStr);
            retriever.DropTable();
            retriever.CreateTable();
            retriever.LoadTestData();
            virtualJustInTimeDemoGrid1.Columns = fields;
            virtualJustInTimeDemoGrid1.Open(connectionString, table, fields, filterStr);
        }

        

        private void tsbCreate_Click(object sender, EventArgs e)
        {

        }
    }
}
