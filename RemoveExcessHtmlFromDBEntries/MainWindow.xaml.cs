using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RemoveExcessHtmlFromDBEntries
{
    public partial class MainWindow : Window
    {
        private static string _dbName;
        private static string _serverName;
        private static string _user;
        private static string _password;
        private static string _tableName;
        private static bool _isInputValid = false;
        private static string _columnName;
        private readonly RemoveExcessHtml _removeExcessHtml = new RemoveExcessHtml();
        private readonly SqlConnectionManager _sqlConnectionManager = new SqlConnectionManager();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnTestConnection_Click(object sender, RoutedEventArgs e)
        {
            RetrieveInputs();

            if (_isInputValid)
            {
                string testMessage = _sqlConnectionManager.TestConnection();
                SetTextBlockWarningFont(testMessage);

                TextBlockInformation.Text = testMessage;
            }

        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            TextBoxDbName.Text = string.Empty;
            TextBoxServerName.Text = string.Empty;
            TextBoxUserName.Text = string.Empty;
            TextBoxPassword.Text = string.Empty;
            TextBoxColumnName.Text = string.Empty;
            TextBoxTableName.Text = string.Empty;
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            RetrieveInputs();
            if (_isInputValid)
            {
                DataTable dataForUpdate  = _sqlConnectionManager.RetrieveData();
                DataTable updatedData = _removeExcessHtml.RemoveHtml(dataForUpdate);
                int rowsUpdated = _sqlConnectionManager.UpdateDatabase(updatedData);
                SetInformationText(rowsUpdated);
            }
        }

        private void RetrieveInputs()
        {
            _dbName = TextBoxDbName.Text;
            _serverName = TextBoxServerName.Text;
            _user = TextBoxUserName.Text;
            _password = TextBoxPassword.Text;
            _tableName = TextBoxTableName.Text;
            _columnName = TextBoxColumnName.Text;

            IsValid(_dbName, _serverName, _user, _password, _tableName, _columnName);
            if (_isInputValid)
            {
                _sqlConnectionManager.AddConnectionDetails(_dbName, _serverName, _user, _password, _tableName, _columnName);
            }
        }

        public void IsValid(string dbName, string serverName, string userName, string password, string tableName, string columnName)
        {           
            if (dbName == "" || serverName == "" || userName == "" || password == "" || tableName == "" || columnName == "")
            {
                _isInputValid = false;
                TextBlockInformation.Text = "All inputs are required";
                TextBlockInformation.Foreground = Brushes.Red;
            }
            else
            {
                _isInputValid = true;
            }
        }

        public void SetTextBlockWarningFont(string result)
        {
            if(result.Contains("Failed"))
                TextBlockInformation.Foreground = Brushes.Red;
            else
                TextBlockInformation.Foreground = Brushes.Green;
        }
        private void SetInformationText(int rowsUpdated)
        {
            if (rowsUpdated == -1)
            {
                TextBlockInformation.Foreground = Brushes.Red;
                TextBlockInformation.Text = "There was an error no rows were updated";
            }
            else
            {
                TextBlockInformation.Foreground = Brushes.Green;
                TextBlockInformation.Text = $"{rowsUpdated} rows were updated";
            }
        }
    }
}
