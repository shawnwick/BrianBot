using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oracle.DataAccess.Client;
using Oracle.DataAccess.Types;
using System.Data;
using System.Xml;
using System.Timers;
using System.Data.Common;

namespace BrianBot
{
    class OracleClass
    {
        public OracleConnection conn = null;
        public OracleTransaction tran = null;
        
        private string connectionString;
        private XmlClass xmlFile;
        private Timer _ReadDbTimer;  // there will be multiple of these for each, so can check when finished
        
        /// <summary>
        /// Constructor
        /// </summary>
        public OracleClass()
        {
            xmlFile = new XmlClass();
        }

        public void ReadTimerSetup()
        {
            _ReadDbTimer = new Timer();
            _ReadDbTimer.Elapsed += new ElapsedEventHandler(ReadDbNow);
            _ReadDbTimer.Interval = 2000;
            _ReadDbTimer.Enabled = true;
        }

        private void ReadDbNow(object source, ElapsedEventArgs e)
        {
            Console.WriteLine("Do a database read.");
            Reader();
        }

        /// <summary>
        /// Setup connection based on XML or default values.
        /// </summary>
        public void ConnectionSetup(string UserId, string Password)
        {
            // Read XML File //
            xmlFile.ReadFile();

            System.Data.Common.DbConnectionStringBuilder dbs = new DbConnectionStringBuilder();
            //dbs.us

            // Store Builder Values //
            Oracle.DataAccess.Client.OracleConnectionStringBuilder builder = new Oracle.DataAccess.Client.OracleConnectionStringBuilder();
            //builder.UserID = xmlFile.XmlValues["UserID"];
            //builder.Password = xmlFile.XmlValues["Password"];
            builder.UserID = UserId;
            builder.Password = Password;
           
            builder.MinPoolSize = Convert.ToInt32(xmlFile.XmlValues["MinPoolSize"]);
            builder.ConnectionLifeTime = Convert.ToInt32(xmlFile.XmlValues["ConnectionLifeTime"]);
            builder.ConnectionTimeout = Convert.ToInt32(xmlFile.XmlValues["ConnectionTimeout"]);
            builder.IncrPoolSize = Convert.ToInt32(xmlFile.XmlValues["IncrPoolSize"]);
            builder.DecrPoolSize = Convert.ToInt32(xmlFile.XmlValues["DecrPoolSize"]);
            builder.MaxPoolSize = Convert.ToInt32(xmlFile.XmlValues["MaxPoolSize"]);
            builder.ValidateConnection = Convert.ToBoolean(xmlFile.XmlValues["ValidateConnection"]);
            
            builder.DataSource = string.Format("(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL={0})(HOST={1})(PORT={2})))(CONNECT_DATA=(SERVICE_NAME={3})))", 
                                                xmlFile.XmlValues["Protocol"], 
                                                xmlFile.XmlValues["Host"], 
                                                xmlFile.XmlValues["Port"], 
                                                xmlFile.XmlValues["ServiceName"]);
            connectionString = builder.ConnectionString;
        }

        /// <summary>
        /// Connect with Database and return connection.
        /// </summary>
        /// <returns></returns>
        public void Connect(string UserId, string Password, string Privilege = "")
        {
            ConnectionSetup(UserId, Password);
            
            if (Privilege != "")
                connectionString = "DBA Privilege=" + Privilege + ";" + connectionString;
            
            Console.WriteLine("UserID - " + UserId);
            Console.WriteLine("Password - " + Password);
            Console.WriteLine("Privilege - " + Privilege);

            try
            {
                conn = new OracleConnection(connectionString);
                conn.Open();
                Console.WriteLine("Database Connection Success!");
            }
            catch (OracleException e)
            {
                Console.WriteLine(e.Message);

                if (conn != null)
                {
                    try
                    {
                        conn.Dispose();
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// For executing sql scripts that are read into the program.
        /// </summary>
        /// <param name="CommandText"></param>
        public void NonQueryText(string CommandText)
        {
            try
            {
                OracleCommand cmd = new OracleCommand();
                cmd.Connection = conn;
                cmd.CommandText = CommandText;
                cmd.CommandType = CommandType.Text;
                cmd.Transaction = tran;
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                tran.Rollback();
                Console.WriteLine(e.ToString());
            }
        }

        public void NonQueryProcedure(ProcedureInputClass pic)
        {
            try
            {
                OracleCommand cmd = new OracleCommand();
                cmd.Connection = conn;
                
                cmd.CommandText = pic.procedureName;
                cmd.CommandType = CommandType.StoredProcedure;
                
                //cmd.Parameters.Add("i_section_number", 1);
                for (int i = 0; i < pic.numberOfParameters; i++)
                {
                    cmd.Parameters.Add(pic.parameterName[i], pic.parameterValue[i]);
                }
                
                cmd.Transaction = tran;
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                tran.Rollback();
                Console.WriteLine(e.ToString());
            }
        }

        public void Reader()
        {
            OracleCommand cmd = new OracleCommand();
            cmd.Connection = conn;
            cmd.CommandText = "select * from lynx.account";
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                var depth = dr.Depth;
                var fields = dr.FieldCount;
                Console.WriteLine("Depth: " + depth);
                Console.WriteLine("Field Count: " + fields);

                for (int j = 0; j < fields; j++)
                {
                    try
                    {
                        object check = dr.GetValue(j);
                        if (check is System.DBNull)
                        {
                            Console.WriteLine("Null");
                        }
                        if (check is int)
                        {
                            Console.WriteLine(dr.GetInt32(j).ToString());
                        }
                        if (check is decimal)
                        {
                            Console.WriteLine(dr.GetDecimal(j).ToString());
                        }
                        if (check is string)
                        {
                            Console.WriteLine(dr.GetString(j));
                        }
                    }
                    catch
                    {
                        //retString += ",";
                        continue;
                    }
                }

                // Just for test /
                return;
            }

            //var test = dr.GetString(0);
            //conn.Dispose();
        }
    }
}
