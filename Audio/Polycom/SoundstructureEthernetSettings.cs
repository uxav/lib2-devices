 
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Crestron.SimplSharp;

namespace UX.Lib2.Devices.Audio.Polycom
{
    public class SoundstructureEthernetSettings
    {
        internal SoundstructureEthernetSettings(string fromValueString)
        {
            try
            {
                var info = fromValueString;
                info = info.Replace("\'", "");
                var infoParts = info.Split(',');
                foreach (var part in infoParts)
                {
                    var paramName = part.Split('=')[0];
                    var value = part.Split('=')[1];

                    switch (paramName)
                    {
                        case "addr": IPAddress = value; break;
                        case "gw": Gateway = value; break;
                        case "nm": SubnetMask = value; break;
                        case "dns":
                            if (value.Contains(' '))
                                foreach (var d in value.Split(' '))
                                    _dns.Add(d);
                            else
                                _dns.Add(value);
                            break;
                        default:
                            if (paramName == "mode")
                            {
                                if (value == "dhcp")
                                    DHCPEnabled = true;
                                else
                                    DHCPEnabled = false;
                            }
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error parsing {0} information, {1}", this.GetType(), e.Message);
            }
        }

        public string IPAddress { get; protected set; }
        public string SubnetMask { get; protected set; }
        public string Gateway { get; protected set; }
        public bool DHCPEnabled { get; protected set; }
        readonly List<string> _dns = new List<string>();
        public ReadOnlyCollection<string> Dns
        {
            get
            {
                return new ReadOnlyCollection<string>(_dns);
            }
        }
    }
}