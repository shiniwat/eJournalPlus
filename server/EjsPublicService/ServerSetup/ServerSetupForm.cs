///	-----------------------------------------------------------------
///	ServerConfigForm.cs: main form code behind.
///	Copyright shiniwa, 2009, All Rights Reserved.
///	current owner : shiniwa
///	-----------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ServiceProcess;
using System.Configuration;
using System.Xml.Linq;
using System.IO;
using System.Data.SqlClient;
using System.Threading;

namespace ServerSetup
{
    internal enum sqlCheckResult
    {
        Invalid,
        Okay,
        BypassSqlSetup,
        UserCancel,
        Unknown,
    }

    public partial class ServerSetupForm : Form
    {
        public ServerSetupForm()
        {
            InitializeComponent();
        }

        private void _cancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Event handler that gets called when the form gets loaded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ServerSetupForm_Load(object sender, EventArgs e)
        {
            // populate this machine name as the default server name
            string machineName = Environment.MachineName;
            string serverName = machineName;
            bool foundService = false;
            // Let's see if SQLSERVER service exist on this machine
            //	Check to see SQLSERVER service
            try
            {
                ServiceController scSqlSrv = new ServiceController("SQLSERVER");
                if (scSqlSrv.Status == ServiceControllerStatus.Running)
                {
                    //	OK
                    foundService = true;
                }
            }
            catch (Win32Exception we)
            {
                System.Diagnostics.Debug.WriteLine(we.Message);
            }
            catch (InvalidOperationException ie)
            {
                System.Diagnostics.Debug.WriteLine(ie.Message);
            }

            if (!foundService)
            {
                try
                {
                    ServiceController scSqlExpress = new ServiceController("MSSQL$SQLEXPRESS");
                    if (scSqlExpress.Status == ServiceControllerStatus.Running)
                    {
                        serverName += "\\SQLEXPRESS";
                        foundService = true;
                    }
                }
                catch (Win32Exception we)
                {
                    System.Diagnostics.Debug.WriteLine(we.Message);
                }
                catch (InvalidOperationException ie)
                {
                    System.Diagnostics.Debug.WriteLine(ie.Message);
                }
            }
            this._serverName.Text = serverName;
            this._adminName.Text = "sa";
            this._serviceName.Text = machineName + ":80";
            this._virtualEndPoint.Text = ConfigurationManager.AppSettings["virtualEndPoint"];
        }

        /// <summary>
        /// Verify sql parameters user specified.
        /// </summary>
        /// <returns></returns>
        private sqlCheckResult VerifySqlParam()
        {
            sqlCheckResult result = sqlCheckResult.Unknown;
            //	let's see if SqlConnection is correct.
            SqlConnection connection = new SqlConnection();
            connection.ConnectionString = string.Format("Server={0};User Id={1};Password={2};Connection Timeout=20;",
                                                _serverName.Text, _adminName.Text, _adminPassword.Text);
            try
            {
                connection.Open();
                string dbName = ConfigurationManager.AppSettings["databaseName"];
                if (IsDbExists(connection, dbName))
                {
                    // DB exists.
                    DialogResult dr = MessageBox.Show("The target dababase exists already\nClick Yes to bypass the database setup, No to cancel the whole process", "Server Setup", MessageBoxButtons.YesNo);
                    if (dr == DialogResult.Yes)
                    {
                        result = sqlCheckResult.BypassSqlSetup;
                    }
                    else
                    {
                        result = sqlCheckResult.UserCancel;
                    }
                }
                else
                {
                    result = sqlCheckResult.Okay;
                }
                connection.Close();
            }
            catch (SqlException se)
            {
                System.Diagnostics.Debug.WriteLine(se.Message);
                MessageBox.Show(se.Message);
                result = sqlCheckResult.Invalid;
            }
            return result;
        }

        /// <summary>
        /// Run the script where the configuration specified.
        /// </summary>
        /// <returns></returns>
        private bool RunScript()
        {
            string currentDir = ConfigurationManager.AppSettings["scriptRunDir"];
            string prevCD = System.Environment.CurrentDirectory;
            if (!string.IsNullOrEmpty(currentDir))
            {
                string fullPath = Path.GetFullPath(currentDir);
                if (fullPath != null)
                {
                    System.Environment.CurrentDirectory = fullPath;
                }
            }
            System.Environment.CurrentDirectory = currentDir;
            string runExe = ConfigurationManager.AppSettings["runScriptExe"];
            string runFormat = ConfigurationManager.AppSettings["runScriptParam"];
            if (!string.IsNullOrEmpty(runExe) && !string.IsNullOrEmpty(runFormat))
            {
                string runParam = string.Format(runFormat, _serverName.Text, _adminName.Text, _adminPassword.Text);
                System.Diagnostics.Process proc = System.Diagnostics.Process.Start(runExe, runParam);
                proc.WaitForExit();
                System.Environment.CurrentDirectory = prevCD;
                return true;
            }
            System.Environment.CurrentDirectory = prevCD;
            return false;
        }

        /// <summary>
        /// Check to see if the target database exists already.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="dbName"></param>
        /// <returns></returns>
        private bool IsDbExists(SqlConnection connection, string dbName)
        {
            string sql = string.Format("select * from {0}.sys.objects where type_desc = 'USER_TABLE'", dbName);
            SqlCommand command = new SqlCommand(sql, connection);
            bool exists = false;
            try
            {
                SqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    exists = true;
                }
                reader.Close();
            }
            catch (SqlException se)
            {
                System.Diagnostics.Debug.WriteLine(se.Message);
                //  probably, the database does not exist.
            }
            return exists;
        }

        /// <summary>
        /// Event handler when user pressed setup button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _configureButton_Click(object sender, EventArgs e)
        {
            sqlCheckResult result = VerifySqlParam();
            if (result == sqlCheckResult.Invalid || result == sqlCheckResult.UserCancel || result == sqlCheckResult.Unknown)
            {
                return;
            }

            //  make sure the target db does not exist.
            //  and bypass if exists
            if (result != sqlCheckResult.BypassSqlSetup)
            {
                bool runOK = RunScript();
                if (!runOK)
                {
                    return;
                }
            }

            string dbName = ConfigurationManager.AppSettings["databaseName"];
            string targetConfig = ConfigurationManager.AppSettings["configFileName"];
            if (!string.IsNullOrEmpty(targetConfig))
            {
                XElement root = XElement.Load(targetConfig);
                if (root != null)
                {
                    bool modified = false;
                    XElement settingElement = root.Element("appSettings");
                    if (settingElement != null)
                    {
                        foreach (XElement child in settingElement.Elements())
                        {
                            if (child.Name == "add")
                            {
                                XAttribute keyAttr = child.FirstAttribute;
                                if (keyAttr != null && keyAttr.Name == "key" && keyAttr.Value == "baseAddress")
                                {
                                    XAttribute valAttr = keyAttr.NextAttribute;
                                    if (valAttr != null)
                                    {
                                        valAttr.Value = string.Format("http://{0}/EjsWcfSvc/EjsPublic.svc", _serviceName.Text);
                                        modified = true;
                                    }
                                }
                                else if (keyAttr != null && keyAttr.Name == "key" && keyAttr.Value == "connectionString")
                                {
                                    XAttribute valAttr = keyAttr.NextAttribute;
                                    if (valAttr != null)
                                    {
                                        valAttr.Value = string.Format("Server={0};Database={1};User Id={2};Password={3};Connection Timeout=90;",
                                                _serverName.Text, dbName, _adminName.Text, _adminPassword.Text);
                                        modified = true;
                                    }
                                }
                            }
                        }
                    }
                    if (modified)
                    {
                        root.Save("temp.xml");
                        root = null;
                        File.Copy("temp.xml", targetConfig, true);
                        //  and delete the temp
                        File.Delete("temp.xml");
                    }

                    RunWCFReg();

                    //  @todo: run MSI setup here.
                    RunMsiSetup();

					//	Very weired, but anyhow, we may need to run WCFReg twice...
					RunWCFReg();
					
					//  OK finished.
                    //MessageBox.Show("Server setup finished");
                    //  delay the exit somewhat
                    
                    this.Close();
                }
            }
        }

        private void RunWCFReg()
        {
            //  this is supposed to be C:\Windows\Microsoft.NET\Framework\v3.0\Windows Communication Foundation\ServiceModelReg.exe"
            string systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string targetDir = string.Format(@"{0}\..\Microsoft.NET\Framework\v3.0\Windows Communication Foundation\", systemFolder);
            targetDir = Path.GetFullPath(targetDir);
            string targetExe = Path.Combine(targetDir, "ServiceModelReg.exe");
            System.Diagnostics.Process proc = System.Diagnostics.Process.Start(targetExe, "-i");
            proc.WaitForExit();
        }

        /// <summary>
        /// Run MSI setup
        /// </summary>
        private void RunMsiSetup()
        {
            string runExe = ConfigurationManager.AppSettings["runMsiSetup"];
            if (!string.IsNullOrEmpty(runExe))
            {
                System.Diagnostics.Process proc = System.Diagnostics.Process.Start(runExe);
                proc.WaitForExit();
            }
            Thread.Sleep(1000);
        }
    }
}
