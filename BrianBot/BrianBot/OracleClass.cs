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
        
        private string _ConnectionString;
        private XmlClass _xFile;
        private Timer _ReadDbTimer;  // there will be multiple of these for each, so can check when finished
        
        /// <summary>
        /// Constructor
        /// </summary>
        public OracleClass()
        {
            _xFile = new XmlClass();
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
        public void ConnectionSetup()
        {
            // Read XML File //
            _xFile.ReadFile();

            // Store Builder Values //
            Oracle.DataAccess.Client.OracleConnectionStringBuilder builder = new Oracle.DataAccess.Client.OracleConnectionStringBuilder();
            builder.MinPoolSize = Convert.ToInt32(_xFile.XmlValues["MinPoolSize"]);
            builder.ConnectionLifeTime = Convert.ToInt32(_xFile.XmlValues["ConnectionLifeTime"]);
            builder.ConnectionTimeout = Convert.ToInt32(_xFile.XmlValues["ConnectionTimeout"]);
            builder.IncrPoolSize = Convert.ToInt32(_xFile.XmlValues["IncrPoolSize"]);
            builder.DecrPoolSize = Convert.ToInt32(_xFile.XmlValues["DecrPoolSize"]);
            builder.MaxPoolSize = Convert.ToInt32(_xFile.XmlValues["MaxPoolSize"]);
            builder.ValidateConnection = Convert.ToBoolean(_xFile.XmlValues["ValidateConnection"]);
            builder.UserID = _xFile.XmlValues["UserID"];
            builder.Password = _xFile.XmlValues["Password"];
            builder.DataSource = string.Format("(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL={0})(HOST={1})(PORT={2})))(CONNECT_DATA=(SERVICE_NAME={3})))", 
                                                _xFile.XmlValues["Protocol"], 
                                                _xFile.XmlValues["Host"], 
                                                _xFile.XmlValues["Port"], 
                                                _xFile.XmlValues["ServiceName"]);
            _ConnectionString = builder.ConnectionString;
        }

        /// <summary>
        /// Connect with Database and return connection.
        /// </summary>
        /// <returns></returns>
        public void Connect()
        {
            ConnectionSetup();

            try
            {
                conn = new OracleConnection(_ConnectionString);
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
        public void NonQuery(string CommandText)
        {
            // This can be done for a lot of executenonquery //
            // Might need to bring this out in front of all the sql files, then one big commit at the end?
            OracleTransaction tran = conn.BeginTransaction();
            
            try
            {
                OracleCommand cmd = new OracleCommand();
                cmd.Connection = conn;
                cmd.CommandText = CommandText;
                cmd.CommandType = CommandType.Text;
                cmd.Transaction = tran;
                cmd.ExecuteNonQuery();
                tran.Commit();
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
