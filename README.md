---
title: DataGridView VirtualMode
description: simple example DataGridView in VirtualMode with SQLite.  
tags: C# DataGridView VirtualMode Just-In-Time Data Loading

---
DataGridView VirtualMode example with some of diagnostic capability
=========
This simple example shows DGV in virtual mode. 
Just-In-Time Data Loading is implemented. SQLite is used as a data store.
Some diagnostic messages will be printed to the output console VS:
 The RowCount of the DataGridView
 The Count of the data source
 The current RowIndex

RowCount of DataGridView and Count of data source must be synchronized all the time and
immediate update if row is deleted or inserted.




It's tested with VS 2017   

## Links
https://docs.microsoft.com/ru-ru/dotnet/desktop/winforms/controls/implementing-virtual-mode-jit-data-loading-in-the-datagrid?view=netframeworkdesktop-4.8
https://stackoverflow.com/questions/60658921/how-to-keep-virtualmode-datagridview-from-calling-cellvalueneeded-while-updating/62241176#62241176

