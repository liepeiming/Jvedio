﻿using SuperUtils.Sql;
using System.Data.SQLite;

namespace Jvedio.Entity
{
    public class MySqlite : Sqlite
    {
        public MySqlite(string path) : base(path)
        {
            SqlitePath = path;
            cn = new SQLiteConnection("data source=" + SqlitePath);
            cn.Open();
            cmd = new SQLiteCommand();
            cmd.Connection = cn;
        }

        public void CloseDB()
        {
            this.Close();
        }
    }
}
