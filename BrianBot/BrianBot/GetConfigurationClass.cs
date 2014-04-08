using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace BrianBot
{
    class GetConfigurationClass
    {
        #region Config Values


        #endregion

        public GetConfigurationClass()
        {
            // Z:\BrianF\X Project\Architecture\Build Automation\$X\Trunk\DB Scripts\10_Structure\E07
            
        }

        public void ReadConfig()
        {
            //string[] dir = Directory.GetDirectories(@"Z:\BrianF\X Project\Architecture\Build Automation\$X\Trunk\DB Scripts\10_Structure\E07");
            
            OracleClass oc = new OracleClass();
            oc.Connect("sys", "dang3r", "sysdba");
            oc.conn.Close();
        }

        public void DropPriorUpgrade()
        {
            OracleClass oc = new OracleClass();
            oc.Connect("lynx", "dang3r");
            oc.tran = oc.conn.BeginTransaction();

            oc.NonQueryText("drop package upgrade_pkg");
            oc.NonQueryText("drop sequence upgrade_command_seq");
            oc.NonQueryText("drop sequence upgrade_migration_job_seq");
            oc.NonQueryText("insert into pl_database_edition values (7,41,'E06','PLX 1.6')");  // These values should be in config some place.

            oc.tran.Commit();
            oc.conn.Close();
        }

        /// <summary>
        /// Importing a new database, this will need some changing.
        /// </summary>
        public void ImportUpgradePackage()
        {
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.WorkingDirectory = @"C:\app\SScribner\product\11.2.0\client_1\BIN";
            p.StartInfo.Arguments = "impdp lynx/dang3r DUMPFILE=ShawnTest.dmp LOGFILE=ShawnTest.log TABLE_EXISTS_ACTION=REPLACE";  // Get dmp and log file names from config.
            p.Start();
            p.WaitForExit();
        }

        public void NewGrants()
        {
            OracleClass oc = new OracleClass();
            oc.Connect("lynx", "dang3r");
            oc.tran = oc.conn.BeginTransaction();

            oc.NonQueryText("grant execute on upgrade_pkg to sys,lynx_dev,lynx_dev_ne,lynx_ne");
            oc.NonQueryText("grant select,update on rt_upgrade to sys,lynx_dev,lynx_dev_ne,lynx_ne");
            oc.NonQueryText("grant select,update on rt_upgrade_command to sys,lynx_dev,lynx_dev_ne,lynx_ne");

            oc.tran.Commit();
            oc.conn.Close();
        }

        public void QualifyDatabaseUnlockAccounts()
        {
            OracleClass oc = new OracleClass();
            oc.Connect("lynx", "dang3r");
            oc.tran = oc.conn.BeginTransaction();

            ProcedureInputClass pic = new ProcedureInputClass();
            pic.numberOfParameters = 0;
            pic.procedureName = "execute lynx.upgrade_pkg.qualify_database";
            oc.NonQueryProcedure(pic);

            oc.NonQueryText("alter user lynx account unlock identified by dang3r");
            oc.NonQueryText("alter user lynx_dev account unlock identified by f3line");
            oc.NonQueryText("alter user lynx_dev_ne account unlock identified by f3line");
            oc.NonQueryText("alter user lynx_ne account unlock identified by f3line");

            oc.tran.Commit();
            oc.conn.Close();
        }

        public void RunUpgrade()
        {
            // This could be done by reading the File Convert "connects" in and storing them, or reading the file name.  Wait for demo of upgrade before testing thsi.
            
            //OracleClass oc = new OracleClass();
            //oc.Connect("lynx", "dang3r");
            //oc.NonQueryText("execute lynx.upgrade_pkg.upgrade(1)");
            //oc.conn.Close();
            //oc.Connect("lynx_ne", "f3line");
            //oc.conn.Close();
            //oc.Connect("lynx_dev_ne", "f3line");
            //oc.conn.Close();
        }
    }
}
