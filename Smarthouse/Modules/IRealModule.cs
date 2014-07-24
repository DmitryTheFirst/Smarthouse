using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Smarthouse.Modules
{
    interface IRealModule : IModule
    {
        XmlNode Cfg { get; set; }//path to config
    }
}
