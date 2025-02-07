﻿using Jvedio.Core.Config.Base;
using Jvedio.Core.Crawler;
using Jvedio.Core.Plugins.Crawler;
using Jvedio.Entity.CommonSQL;
using Newtonsoft.Json;
using SuperUtils.Common;
using SuperUtils.Framework.ORM.Attributes;
using SuperUtils.Framework.ORM.Wrapper;
using System.Collections.Generic;
using System.Linq;

namespace Jvedio.Core.Config
{
    public class ServerConfig : AbstractConfig
    {
        private ServerConfig() : base("Servers")
        {
        }

        private static ServerConfig instance = null;

        public static ServerConfig createInstance()
        {
            if (instance == null) instance = new ServerConfig();
            return instance;
        }

        public List<CrawlerServer> CrawlerServers { get; set; }

        public override void Read()
        {
            CrawlerServers = new List<CrawlerServer>();
            SelectWrapper<AppConfig> wrapper = new SelectWrapper<AppConfig>();
            wrapper.Eq("ConfigName", ConfigName);
            AppConfig appConfig = MapperManager.appConfigMapper.SelectOne(wrapper);
            if (appConfig == null || appConfig.ConfigId == 0) return;
            List<Dictionary<object, object>> dicts = JsonUtils.TryDeserializeObject<List<Dictionary<object, object>>>(appConfig.ConfigValue);

            if (dicts == null || CrawlerManager.PluginMetaDatas == null) return;
            foreach (Dictionary<object, object> d in dicts)
            {
                CrawlerServer server = new CrawlerServer();
                if (!server.HasAllKeys(d)) continue;
                if (d.ContainsKey("PluginID")) server.PluginID = d["PluginID"].ToString();
                if (string.IsNullOrEmpty(server.PluginID)) continue;
                if (!CrawlerManager.PluginMetaDatas.Where(arg => arg.PluginID.Equals(server.PluginID)).Any()) continue;
                server.Url = d["Url"].ToString();
                server.Cookies = d["Cookies"].ToString();
                server.Enabled = "true".Equals(d["Enabled"].ToString().ToLower());
                server.LastRefreshDate = d["LastRefreshDate"].ToString();
                server.Headers = d["Headers"].ToString();
                int.TryParse(d["Available"].ToString(), out int available);
                server.Available = available;
                CrawlerServers.Add(server);
            }
        }

        public override void Save()
        {
            if (CrawlerServers != null)
            {
                AppConfig appConfig = new AppConfig();
                appConfig.ConfigName = ConfigName;
                appConfig.ConfigValue = JsonConvert.SerializeObject(CrawlerServers);
                MapperManager.appConfigMapper.Insert(appConfig, InsertMode.Replace);
            }
        }
    }
}
