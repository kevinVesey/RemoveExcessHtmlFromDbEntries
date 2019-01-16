using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RemoveExcessHtmlFromDBEntries
{
    public class SqlConnectionManager
    {
        public string ConnectionString;
        public string TableName { get; set; }
        public string HtmlColumnName { get; set; }
        public string PrimaryKeyColumn { get; set; }

        public void AddConnectionDetails(string dbName, string serverName, string userName, string password, string tableName, string columnName)
        {
            ConnectionString = $"user id={userName};password={password};data source={serverName}; initial catalog={dbName}";
            TableName = tableName;
            HtmlColumnName = columnName;
        }
        private void GetPrimaryKeyColumnName()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    DataTable primaryKeydata = new DataTable();
                    String retrievePrimaryKey =
                        $"SELECT Col.Column_Name from INFORMATION_SCHEMA.TABLE_CONSTRAINTS Tab, INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE Col WHERE  Col.Constraint_Name = Tab.Constraint_Name AND Col.Table_Name = Tab.Table_Name AND Constraint_Type = 'PRIMARY KEY' AND Col.Table_Name = '{TableName}'";
                    SqlCommand command = conn.CreateCommand();
                    command.CommandText = retrievePrimaryKey;
                    SqlDataAdapter data = new SqlDataAdapter(command);
                    data.Fill(primaryKeydata);
                    PrimaryKeyColumn = primaryKeydata.Rows[0][0].ToString();
                }
            }
            catch
            {
                MessageBox.Show("There was an error retrieving the primary key from the inputted table");
            }
        }
        public DataTable RetrieveData()
        {
            DataTable dataForUpdate = new DataTable();
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    if(string.IsNullOrEmpty(PrimaryKeyColumn))
                        GetPrimaryKeyColumnName();
                    conn.Open();
                    SqlCommand command = conn.CreateCommand();
                    SqlDataAdapter data = new SqlDataAdapter(command);
                    string commandString = $"select {PrimaryKeyColumn}, {HtmlColumnName} from {TableName}";
                    command.CommandText = commandString;
                    data.Fill(dataForUpdate);                                 
                }
            }
            catch
            {
                MessageBox.Show("there was an error run test for more information");
            }
            if (dataForUpdate.Rows.Count == 0)
                MessageBox.Show("No data was found in the table column provided");

            return dataForUpdate;
        }

        public int UpdateDatabase(DataTable updatedTable)
        {
            int updatedrows = -1;
            const string newValueColumn = "UpdatedValue";
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                foreach (DataRow updatedRow in updatedTable.Rows)
                {
                    string commandString = $"update {TableName} Set {HtmlColumnName} = '{updatedRow[newValueColumn]}' where {PrimaryKeyColumn} = {updatedRow[PrimaryKeyColumn]};";
                    SqlCommand command = conn.CreateCommand();
                    command.CommandText = commandString;
                    updatedrows += command.ExecuteNonQuery();
                }

              
            }
            return updatedrows;
        }
        public string TestConnection()
        {
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                DataTable testDataTable = new DataTable();
                try
                {
                    conn.Open();
                }
                catch
                {
                    return "Failed: There was a problem connecting to DB check database inputs:";
                }
                try
                {
                    GetPrimaryKeyColumnName();
                    string commandString = $"select Top 1 {PrimaryKeyColumn}, {HtmlColumnName} from {TableName}";
                    SqlCommand command = conn.CreateCommand();
                    command.CommandText = commandString;
                    SqlDataAdapter data = new SqlDataAdapter(command);

                    data.Fill(testDataTable);
                }
                catch
                {
                    return "Failed: Connection to DB succeeded there was an error getting data from table";
                }
                if (testDataTable.Rows.Count == 0)
                {
                    return "Failed: No data was found in the table column provided";
                }
            }
            return "Connection Succeeded";
        }
    }
}
