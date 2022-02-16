using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SQLite;
using Pullenti.Address.Gui;
using System.Diagnostics;


namespace Pullenti.Address.Gui.Gui
{
    public partial class AutotestListCtrlDB : UserControl
    {
        public AutotestListCtrlDB()
        {
            InitializeComponent();
            if (DesignMode) return;
            //dataGridView1.Rows.CollectionChanged += new System.ComponentModel.CollectionChangeEventHandler(this.DataGridViewRowCollection1_CollectionChanged);

            //dataGridView1.Rows..GetFirstRow
        }

        public int rowPerPage = 15;

        private Cache memoryCache;
        private string connectionString;
        private string table;
        private string fields;
        public string filter;

        public event EventHandler CurrentChanged;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]

        public int RowCount
        {
            get
            {
                return dataGridView1.RowCount;
            }
            set
            {
                dataGridView1.RowCount = value;
            }
        }

        public DataGridViewRowCollection Rows
        {
            get
            {
                return dataGridView1.Rows;
            }
        }
        public int? CurrentRowIndex
        {
            get
            {
                return dataGridView1.CurrentRow?.Index;
            }
        }
        public Cache MemCache
        {
            get
            {
                return memoryCache;
            }
            set
            {
                memoryCache = value;
            }
        }

        public void Open(string connectionStr, string openTable, string fieldsStr = "*", string filterStr = null)
        {
            Debug.WriteLine("(Open before) RowCount: " + RowCount + ", " + "DataCount: " + memoryCache?.AllRowCount + ", RowIndex: " + dataGridView1.CurrentRow?.Index);
            connectionString = connectionStr; table = openTable; fields = fieldsStr; filter = filterStr;
            DataRetriever retriever = new DataRetriever(connectionString, table, String.Join(", ", fields), filter);
            memoryCache = new Cache(retriever, rowPerPage);
            MemCache = memoryCache;
            //dataGridView1.RowCount = 0;
            dataGridView1.Rows.Clear();
            dataGridView1.RowCount = retriever.RowCount;
            dataGridView1.Refresh();
            Debug.WriteLine("(Open after) RowCount: " + RowCount + ", " + "DataCount: " + memoryCache.AllRowCount + ", RowIndex: " + dataGridView1.CurrentRow?.Index);

        }

        public void UpdateOneRow(SQLiteParameter[] updatePrms, string[] updatePairs)
        {
            try
            {
                int rowid = Convert.ToInt32(dataGridView1.CurrentRow.Cells["rowid"].Value);
                int curInd = dataGridView1.CurrentRow.Index;
                Debug.WriteLine("(before update) RowCount: " + RowCount + ", " + "DataCount: " + memoryCache.AllRowCount + ", RowIndex: " + curInd);
                using (SQLiteConnection dbConnection = new SQLiteConnection(connectionString))
                {
                    dbConnection.Open();
                    using (SQLiteCommand cmd = dbConnection.CreateCommand())
                    {
                        cmd.CommandText = String.Format("update {0} set {1} WHERE rowid = {2};", table, String.Join(", ", updatePairs), rowid);
                        cmd.Parameters.AddRange(updatePrms);
                        cmd.ExecuteNonQuery();
                    }
                };
                
                memoryCache.RefreshPage(curInd);
                //dataGridView1.Rows.Clear();
                dataGridView1.RowCount = memoryCache.AllRowCount;
                //dataGridView1.ClearSelection();
                /*if (curInd <= memoryCache.AllRowCount-1)
                {
                    dataGridView1.CurrentCell = dataGridView1.Rows[curInd].Cells[1];
                    dataGridView1.Rows[curInd].Selected = true;
                }*/
                    
                dataGridView1.Refresh();
                Debug.WriteLine("(after update) RowCount: " + RowCount + ", " + "DataCount: " + memoryCache.AllRowCount + ", RowIndex: " + curInd);

            }
            catch (Exception ex)
            {
               // Debug.Assert(false, ex.Message);
                MessageBox.Show(ex.Message);
            }
            
        }
        private void dataGridView1_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dataGridView1.RowCount) return;
            if (memoryCache?.AllRowCount == 0 || dataGridView1.RowCount != memoryCache?.AllRowCount)
                return;

            switch (dataGridView1.Columns[e.ColumnIndex].Name)
            {
                case "rowid":
                    e.Value = memoryCache.RetrieveElement(e.RowIndex, "rowid");
                    break;
                case "ColumnStatus":
                    if (Int32.TryParse(memoryCache.RetrieveElement(e.RowIndex, "status"), out int i))
                        e.Value = (AutotestStatus)i;
                    else
                        e.Value = AutotestStatus.Undefined;
                    break;
                case "ColumnText":
                    e.Value = memoryCache.RetrieveElement(e.RowIndex, "txtAdr");
                    break;
                case "ColumnResult":
                    e.Value = memoryCache.RetrieveElement(e.RowIndex, "realObj");
                    break;
                case "ColumnLevel":
                    e.Value = memoryCache.RetrieveElement(e.RowIndex, "level");
                    break;
                case "ColumnTime":
                    e.Value = memoryCache.RetrieveElement(e.RowIndex, "timeNer");
                    break;
            }
        }

        private bool IsCellValid(int columnIndex, int rowIndex)
        {
            if (columnIndex == -1) return false;
            if (rowIndex == -1) return false;
            if (rowIndex >= RowCount) return false;
            return true;
        }
        private void dataGridView1_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {

            if (!IsCellValid(e.ColumnIndex, e.RowIndex)) return;

            var r = dataGridView1.Rows[e.RowIndex];
            string status = r.Cells["ColumnStatus"].Value.ToString();
            r.Cells[e.ColumnIndex].Style.BackColor = status == "Error" ? Color.LightPink :
                status == "Warning" ? Color.Yellow :
                status == "Undefined" ? Color.LightGray : Color.White;
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            Debug.WriteLine("(SelectionChanged) RowCount: " + RowCount + ", " + "DataCount: " + memoryCache.AllRowCount + ", RowIndex: " + dataGridView1.CurrentRow.Index);
            CurrentChanged(sender, e);
        }

        private void dataGridView1_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            try
            {
                if (MessageBox.Show("Вы действительно хотите удалить эту строку?", "Внимание", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    e.Cancel = true;
                }
                else
                {
                    int rowid = Convert.ToInt32(e.Row.Cells["rowid"].Value);
                    using (SQLiteConnection dbConnection = new SQLiteConnection(connectionString))
                    {
                        dbConnection.Open();
                        using (SQLiteCommand cmd = dbConnection.CreateCommand())
                        {
                            cmd.CommandText = String.Format("DELETE FROM {0} WHERE rowid = {1};", table, rowid);
                            cmd.ExecuteNonQuery();
                        }
                    };
                    memoryCache.RefreshPage(e.Row.Index);
                    Debug.Assert(memoryCache.AllRowCount != RowCount, "UserDeletingRow - rowcount is equal");
                    Debug.WriteLine("(UserDeletingRow) RowCount: " + RowCount + ", " + "DataCount: " + memoryCache.AllRowCount + ", RowIndex: " + e.Row.Index);
                }
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                e.Cancel = true;
            }
        }

        private void DataGridViewRowCollection1_CollectionChanged(Object sender, CollectionChangeEventArgs e)
        {

            Debug.WriteLine("(SelectionChanged) RowCount: " + RowCount + ", " + "DataCount: " + memoryCache.AllRowCount + ", RowIndex: " + e.Element);
            System.Text.StringBuilder messageBoxCS = new System.Text.StringBuilder();
            messageBoxCS.AppendFormat("{0} = {1}", "Action", e.Action);
            messageBoxCS.AppendLine();
            messageBoxCS.AppendFormat("{0} = {1}", "Element", e.Element);
            messageBoxCS.AppendLine();
            //MessageBox.Show(messageBoxCS.ToString(), "CollectionChanged Event");
        }

        private void dataGridView1_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            Debug.WriteLine("(RowsRemoved) RowCount: " + RowCount + ", " + "DataCount: " + memoryCache.AllRowCount + ", RowIndex: " + e.RowIndex);

            //System.Text.StringBuilder messageBoxCS = new System.Text.StringBuilder();
            /*messageBoxCS.AppendFormat("{0} = {1}", "Action", e.Action);
            messageBoxCS.AppendLine();
            messageBoxCS.AppendFormat("{0} = {1}", "Element", e.Element);
            messageBoxCS.AppendLine();
            MessageBox.Show(messageBoxCS.ToString(), "CollectionChanged Event");*/
        }
    }
}
