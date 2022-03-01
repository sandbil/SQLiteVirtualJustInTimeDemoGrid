﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Data.SQLite;
using System.IO;
using System.Text;

namespace VirtualJustInTimeDemoGrid
{
    public interface IDataPageRetriever
    {
        DataTable SupplyPageOfData(int lowerPageBoundary, int rowsPerPage);
        int RefreshRowCount();
    }
    public class SQLiteDataStore : IDataPageRetriever
    {
        private string tableName;
        private string[] fields;
        private string fieldList;
        private string filterStr;
        public SQLiteCommand command;

        public SQLiteDataStore(string connectionString, string tableName, string[] fields, string filter = null)
        {
            SQLiteConnection connection = new SQLiteConnection(connectionString);
            connection.Open();
            command = connection.CreateCommand();
            this.tableName = tableName;
            this.fields = fields;
            this.fieldList = string.Join(",", fields);
            this.filterStr = filter ?? "";
        }

        private int rowCountValue = -1;

        public int RowCount
        {
            get
            {
                // Return the existing value if it has already been determined.
                if (rowCountValue != -1)
                {
                    return rowCountValue;
                }
                command.CommandText = $"SELECT COUNT(*) FROM {tableName} {filterStr}" ;
                rowCountValue = Convert.ToInt32(command.ExecuteScalar());
                return rowCountValue;
            }
        }

        public int RefreshRowCount()
        {
            command.CommandText = $"SELECT COUNT(*) FROM {tableName} {filterStr}";
            rowCountValue = Convert.ToInt32(command.ExecuteScalar());
            return rowCountValue;
        }

        private DataColumnCollection columnsValue;

        public DataColumnCollection Columns
        {
            get
            {
                // Return the existing value if it has already been determined.
                if (columnsValue != null)
                {
                    return columnsValue;
                }

                // Retrieve the column information from the database.
                command.CommandText = "SELECT rowid," + fieldList + " FROM " + tableName;
                SQLiteDataAdapter adapter = new SQLiteDataAdapter();
                adapter.SelectCommand = command;
                DataTable table = new DataTable();
                table.Locale = System.Globalization.CultureInfo.InvariantCulture;
                adapter.FillSchema(table, SchemaType.Source);
                columnsValue = table.Columns;
                return columnsValue;
            }
        }

        private string commaSeparatedListOfColumnNamesValue = null;

        private string CommaSeparatedListOfColumnNames
        {
            get
            {
                // Return the existing value if it has already been determined.
                if (commaSeparatedListOfColumnNamesValue != null)
                {
                    return commaSeparatedListOfColumnNamesValue;
                }

                // Store a list of column names for use in the
                // SupplyPageOfData method.
                System.Text.StringBuilder commaSeparatedColumnNames =
                    new System.Text.StringBuilder();
                bool firstColumn = true;
                foreach (DataColumn column in Columns)
                {
                    if (!firstColumn)
                    {
                        commaSeparatedColumnNames.Append(", ");
                    }
                    commaSeparatedColumnNames.Append(column.ColumnName);
                    firstColumn = false;
                }

                commaSeparatedListOfColumnNamesValue = commaSeparatedColumnNames.ToString();
                return commaSeparatedListOfColumnNamesValue;
            }
        }

        // Declare variables to be reused by the SupplyPageOfData method.
        //private string columnToSortBy;

        private SQLiteDataAdapter adapter = new SQLiteDataAdapter();

        public DataTable SupplyPageOfData(int lowerPageBoundary, int rowsPerPage)
        {
            // Retrieve the specified number of rows from the database, starting
            // with the row specified by the lowerPageBoundary parameter.

            command.CommandText = $"SELECT rowid, {fieldList} FROM {tableName} {filterStr} LIMIT {rowsPerPage} OFFSET {lowerPageBoundary}";

            adapter.SelectCommand = command;

            DataTable table = new DataTable();
            table.Locale = System.Globalization.CultureInfo.InvariantCulture;
            adapter.Fill(table);
            return table;
        }

        public DataTable PageOfDataForExport(int lowerPageBoundary, int rowsPerPage)
        {
            // Retrieve the specified number of rows from the database, starting
            // with the row specified by the lowerPageBoundary parameter.

            command.CommandText = $"SELECT {fieldList} FROM {tableName} {filterStr} LIMIT {rowsPerPage} OFFSET {lowerPageBoundary}";
            adapter.SelectCommand = command;

            DataTable table = new DataTable();
            table.Locale = System.Globalization.CultureInfo.InvariantCulture;
            adapter.Fill(table);
            return table;
        }

        public int ExportToCSV(string[] fieldsForExport, string strFilePath, string encoding = "UTF-8", bool haveHeaders = false, int rowForExport = 10000)
        {
            int expRowCount = 0;
            StreamWriter sw = new StreamWriter(strFilePath, true, Encoding.GetEncoding(encoding));
            //headers  
            if (haveHeaders)
            {
                sw.Write(string.Join(";", fieldsForExport));
                sw.Write(sw.NewLine);
            }

            string fieldListForExport = string.Join(",", fieldsForExport);
            int pageCount = (int)Math.Ceiling((double)RowCount / rowForExport);
            for (int i = 0; i < pageCount; i++)
            {
                command.CommandText = $"SELECT {fieldListForExport} FROM {tableName} {filterStr} LIMIT {rowForExport} OFFSET {i * rowForExport}";
                adapter.SelectCommand = command;
                DataTable exportTable = new DataTable();
                exportTable.Locale = System.Globalization.CultureInfo.InvariantCulture;
                adapter.Fill(exportTable);
                foreach (DataRow dr in exportTable.Rows)
                {
                    for (int j = 0; j < exportTable.Columns.Count; j++)
                    {
                        if (!Convert.IsDBNull(dr[j]))
                        {
                            string value = dr[j].ToString();
                            if (value.Contains(';'))
                            {
                                value = $"\"{value}\"";
                                sw.Write(value);
                            }
                            else
                            {
                                sw.Write(dr[j].ToString());
                            }
                        }
                        if (j < exportTable.Columns.Count - 1)
                        {
                            sw.Write(";");
                        }
                    }
                    expRowCount++;
                    sw.Write(sw.NewLine);
                }
            }
            sw.Close();
            return expRowCount;
        }

        public void CreateTable()
        {
            string listFields = string.Join(" text, ", fields) + " text";
            command.CommandText = $"CREATE TABLE IF NOT EXISTS {tableName} ({listFields});";
            command.ExecuteNonQuery();
        }

        public void DropTable()
        {
            command.CommandText = $"DROP TABLE IF EXISTS {tableName};" ;
            command.ExecuteNonQuery();
        }
        public void InsertSQLiteRow(string[] insertFields, string[] insertData)
        {
            string listFields = string.Join(", ", insertFields);
            string listValues = $"'{string.Join("', '", insertData)}'";
            command.CommandText = $"insert into {tableName} ({listFields}) values ({listValues});";
            command.ExecuteNonQuery();
        }
        public void UpdateSQLiteRow(List<KeyValuePair<string, object>> updateData, int rowid)
        {
            List<SQLiteParameter> lstPrmsValue = new List<SQLiteParameter>();
            List<string> lstUpdatePairs = new List<string>();
            foreach (var pair in updateData)
            {
                lstUpdatePairs.Add($"{pair.Key} = @{pair.Key} ");
                lstPrmsValue.Add(new SQLiteParameter($"@{pair.Key}", pair.Value));
            }
            command.CommandText = $"update {tableName} set {string.Join(", ", lstUpdatePairs.ToArray())} WHERE rowid = {rowid};";
            command.Parameters.AddRange(lstPrmsValue.ToArray());
            command.ExecuteNonQuery();
        }
        public void DeleteSQLiteRow(int rowid)
        {
            command.CommandText = $"DELETE FROM {tableName} WHERE rowid = {rowid};";
            command.ExecuteNonQuery();
        }

        public void LoadTestData()
        {
            string[] insertDataArray = new string[fields.Length];
            int status = 1; int level = 1;
            using (SQLiteTransaction transaction = command.Connection.BeginTransaction())
            {
                command.Transaction = transaction;
                for (int i = 1; i <= 1000; i++)
                {
                    for (int j = 0; j < fields.Length; j++)
                        insertDataArray[j] = $"{fields[j]}-{i}";
                    if (status == 6)  status = 0;
                    insertDataArray[0] = $"{fields[0]}-{status}";

                    if (level == 10) level = 0;
                    insertDataArray[1] = $"{fields[1]}-{level}";

                    InsertSQLiteRow(fields, insertDataArray);
                    status++;
                    level++;

                }
                transaction.Commit();
            }
        }
    }
    public class Cache
    {
        private static int RowsPerPage;

        // Represents one page of datcur.
        public struct DataPage
        {
            public DataTable table;
            private int lowestIndexValue;
            private int highestIndexValue;

            public DataPage(DataTable table, int rowIndex)
            {
                this.table = table;
                lowestIndexValue = MapToLowerBoundary(rowIndex);
                highestIndexValue = MapToUpperBoundary(rowIndex);
                System.Diagnostics.Debug.Assert(lowestIndexValue >= 0);
                System.Diagnostics.Debug.Assert(highestIndexValue >= 0);
            }

            public int LowestIndex
            {
                get
                {
                    return lowestIndexValue;
                }
            }

            public int HighestIndex
            {
                get
                {
                    return highestIndexValue;
                }
            }

            public static int MapToLowerBoundary(int rowIndex)
            {
                // Return the lowest index of a page containing the given index.
                return (rowIndex / RowsPerPage) * RowsPerPage;
            }

            private static int MapToUpperBoundary(int rowIndex)
            {
                // Return the highest index of a page containing the given index.
                return MapToLowerBoundary(rowIndex) + RowsPerPage - 1;
            }
        }

        public DataPage[] CachedPages
        {
            get
            {
                return cachePages;
            }
        }

        public int AllRowCount;


        private DataPage[] cachePages;
        private IDataPageRetriever dataSupply;

        public Cache(IDataPageRetriever dataSupplier, int rowsPerPage)
        {
            dataSupply = dataSupplier;
            Cache.RowsPerPage = rowsPerPage;
            LoadFirstTwoPages();
            AllRowCount = dataSupply.RefreshRowCount();
        }

        // Sets the value of the element parameter if the value is in the cache.
        private bool IfPageCached_ThenSetElement(int rowIndex, int columnIndex, ref string element)
        {
            if (IsRowCachedInPage(0, rowIndex))
            {
                element = cachePages[0].table
                    .Rows[rowIndex % RowsPerPage][columnIndex].ToString();
                return true;
            }
            else if (IsRowCachedInPage(1, rowIndex))
            {
                element = cachePages[1].table
                    .Rows[rowIndex % RowsPerPage][columnIndex].ToString();
                return true;
            }

            return false;
        }

        //

        public string RetrieveElement(int rowIndex, string columnName)
        {
            string element = null;
            int columnIndex = cachePages[0].table.Columns[columnName].Ordinal;

            if (IfPageCached_ThenSetElement(rowIndex, columnIndex, ref element))
            {
                return element;
            }
            else
            {
                return RetrieveData_CacheIt_ThenReturnElement(
                    rowIndex, columnIndex);
            }
        }
        public string RetrieveElement(int rowIndex, int columnIndex)
        {
            string element = null;

            if (IfPageCached_ThenSetElement(rowIndex, columnIndex, ref element))
            {
                return element;
            }
            else
            {
                return RetrieveData_CacheIt_ThenReturnElement(
                    rowIndex, columnIndex);
            }
        }


        private void LoadFirstTwoPages()
        {
            cachePages = new DataPage[]{
            new DataPage(dataSupply.SupplyPageOfData(
                DataPage.MapToLowerBoundary(0), RowsPerPage), 0),
            new DataPage(dataSupply.SupplyPageOfData(
                DataPage.MapToLowerBoundary(RowsPerPage),
                RowsPerPage), RowsPerPage)};
        }

        private string RetrieveData_CacheIt_ThenReturnElement(
            int rowIndex, int columnIndex)
        {
            // Retrieve a page worth of data containing the requested value.
            DataTable table = dataSupply.SupplyPageOfData(
                DataPage.MapToLowerBoundary(rowIndex), RowsPerPage);

            // Replace the cached page furthest from the requested cell
            // with a new page containing the newly retrieved datcur.
            cachePages[GetIndexToUnusedPage(rowIndex)] = new DataPage(table, rowIndex);

            return RetrieveElement(rowIndex, columnIndex);
        }

        // Returns the index of the cached page most distant from the given index
        // and therefore least likely to be reused.
        private int GetIndexToUnusedPage(int rowIndex)
        {
            if (rowIndex > cachePages[0].HighestIndex &&
                rowIndex > cachePages[1].HighestIndex)
            {
                int offsetFromPage0 = rowIndex - cachePages[0].HighestIndex;
                int offsetFromPage1 = rowIndex - cachePages[1].HighestIndex;
                if (offsetFromPage0 < offsetFromPage1)
                {
                    return 1;
                }
                return 0;
            }
            else
            {
                int offsetFromPage0 = cachePages[0].LowestIndex - rowIndex;
                int offsetFromPage1 = cachePages[1].LowestIndex - rowIndex;
                if (offsetFromPage0 < offsetFromPage1)
                {
                    return 1;
                }
                return 0;
            }
        }

        // Returns a value indicating whether the given row index is contained
        // in the given DataPage.
        private bool IsRowCachedInPage(int pageNumber, int rowIndex)
        {
            return rowIndex <= cachePages[pageNumber].HighestIndex &&
                rowIndex >= cachePages[pageNumber].LowestIndex;
        }

        // Returns the number of the DataPage containing the given row index
        public int PageNumRowCached(int rowIndex)
        {
            if (IsRowCachedInPage(0, rowIndex)) return 0;
            if (IsRowCachedInPage(1, rowIndex)) return 1;
            return -1;
        }

        public void RefreshPages(int rowIndex)
        {
            int usedPage = 0;
            if (IsRowCachedInPage(0, rowIndex))
                usedPage = 0;
            else if (IsRowCachedInPage(1, rowIndex))
                usedPage = 1;
            else
                return;
            DataTable table = dataSupply.SupplyPageOfData(cachePages[usedPage].LowestIndex, RowsPerPage);
            cachePages[usedPage] = new DataPage(table, cachePages[usedPage].LowestIndex);
            cachePages[Math.Abs(usedPage - 1)] = new DataPage(table, cachePages[usedPage].LowestIndex);
            AllRowCount = dataSupply.RefreshRowCount();
        }
    }
}
