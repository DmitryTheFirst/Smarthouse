using System;
using System.Configuration;

namespace Smarthouse
{
    class pluginsSection : ConfigurationSection
    {
        [ConfigurationProperty("pluginInfos", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(PluginCollection),
            AddItemName = "add",
            ClearItemsName = "clear",
            RemoveItemName = "remove")]
        public PluginCollection Services
        {
            get
            {
                return (PluginCollection)base["pluginInfos"];
            }
        }
    }

    internal class PluginCollection : ConfigurationElementCollection
    {
       

        public PluginConfig this[int index]
        {
            get { return (PluginConfig)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(PluginConfig pluginConfig)
        {
            BaseAdd(pluginConfig);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new PluginConfig();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((PluginConfig)element);
        }

        public void Remove(PluginConfig pluginConfig)
        {
            BaseRemove(pluginConfig);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        public void Remove(string name)
        {
            BaseRemove(name);
        }
    }

    internal class PluginConfig : ConfigurationElement
    {
        [ConfigurationProperty("className", IsRequired = true)]
        public string ClassName
        {
            get
            {
                return (string)base["className"];
            }
            set
            {
                base["className"] = value;
            }
        }

        [ConfigurationProperty("moduleCfgPath", IsRequired = true)]
        public string ConfigPath
        {
            get
            {
                return (string)base["moduleCfgPath"];
            }
            set
            {
                base["moduleCfgPath"] = value;
            }
        }
    }
}