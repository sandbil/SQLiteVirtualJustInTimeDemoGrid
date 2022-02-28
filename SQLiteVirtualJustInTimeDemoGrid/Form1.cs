﻿using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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

        private static string dbPath = "db";
        private string connectionString = "Data Source = " + dbPath + "\\test.db";
        private string table = "table1";
        private string filterStr = null;
        private string[] fields = { "status", "level", "testField", "fld"  };
        Boolean ignoreTextChanged = false;



        private void Form1_Load(object sender, EventArgs e)
        {
            if (!Directory.Exists(dbPath))
                Directory.CreateDirectory(dbPath);
            SQLiteDataStore sqliteDS = new SQLiteDataStore(connectionString, table, fields, filterStr);
            sqliteDS.DropTable();
            sqliteDS.CreateTable();
            sqliteDS.LoadTestData();
            virtualJustInTimeDemoGrid1.AddColumns(fields);
            virtualJustInTimeDemoGrid1.Open(connectionString, table, fields, filterStr);
            toolStripStatusLabel1.Text = String.Format("total {0} rec", virtualJustInTimeDemoGrid1.RowCount);

            cmBoxLevel.Items.Add("All levels");
            for (int i = 0; i <= 9; i++)
                cmBoxLevel.Items.Add(String.Format("level-{0}", i));
            cmBoxLevel.SelectedIndex = 0;

            cmBoxStatus.Items.Add("All statuses");
            for (int i = 0; i <= 8; i++)
                cmBoxStatus.Items.Add(String.Format("status-{0}", i));
            cmBoxStatus.SelectedIndex = 0;
        }



        private void tsbCreate_Click(object sender, EventArgs e)
        {

        }

        private void virtualJustInTimeDemoGrid1_CurrentChanged(object sender, EventArgs e)
        {
            Debug.WriteLine("(CurrentChanged) RowCount: " + virtualJustInTimeDemoGrid1.RowCount + ", " +  ", RowIndex: " + virtualJustInTimeDemoGrid1.CurrentRowIndex);
            int rowIndex = (int)virtualJustInTimeDemoGrid1.CurrentRowIndex;
            ignoreTextChanged = true;
            tBtestField.Text = virtualJustInTimeDemoGrid1.Rows[rowIndex].Cells[3].Value?.ToString();
            tBFld.Text = virtualJustInTimeDemoGrid1.Rows[rowIndex].Cells[4].Value?.ToString();
            ignoreTextChanged = false;

        }

        private void tsbDelete_Click(object sender, EventArgs e)
        {
            virtualJustInTimeDemoGrid1.Focus();
            SendKeys.Send("{DEL}");
            toolStripStatusLabel1.Text = String.Format("total {0} rec", virtualJustInTimeDemoGrid1.RowCount);
        }

        private void cmBox_SelectionChangeCommited(object sender, EventArgs e)
        {
            if (cmBoxStatus.SelectedIndex != 0)
                filterStr = String.Format(" where status = '{0}'", cmBoxStatus.SelectedItem.ToString());
            else
                filterStr = null;

            if (cmBoxLevel.SelectedIndex != 0)
            {
                if (filterStr != null)
                    filterStr += String.Format(" and level = '{0}'", cmBoxLevel.SelectedItem.ToString());
                else
                    filterStr = String.Format(" where level = '{0}'", cmBoxLevel.SelectedItem.ToString());
            }

            virtualJustInTimeDemoGrid1.Open(connectionString, table, fields, filterStr);
            virtualJustInTimeDemoGrid1.Focus();
            toolStripStatusLabel1.Text = String.Format("total {0} rec", virtualJustInTimeDemoGrid1.RowCount);

        }

        private void tsbUpdate_Click(object sender, EventArgs e)
        {
            List<KeyValuePair<string, object>> updateData = new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("status", "status-0"),
                new KeyValuePair<string, object>("level", ""),
                new KeyValuePair<string, object>("fld", "")
            };
            virtualJustInTimeDemoGrid1.UpdateCurRow(updateData);
            toolStripStatusLabel1.Text = String.Format("total {0} rec", virtualJustInTimeDemoGrid1.RowCount);
        }

        private void tBtestField_TextChanged(object sender, EventArgs e)
        {
            if (ignoreTextChanged ) return;
            List<KeyValuePair<string, object>> updateData = new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("testField", tBtestField.Text)
            };
            virtualJustInTimeDemoGrid1.UpdateCurRow(updateData);
            toolStripStatusLabel1.Text = String.Format("total {0} rec", virtualJustInTimeDemoGrid1.RowCount);
        }
    }
}
