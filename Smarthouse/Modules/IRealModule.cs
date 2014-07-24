using System.Xml;

namespace Smarthouse.Modules
{
    interface IRealModule : IModule
    {
        XmlNode Cfg { get; set; }//path to config
    }
}
