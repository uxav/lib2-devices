 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace UX.Lib2.Devices.Audio.Yamaha
{
    public class Scene
    {
        private readonly Snapshot _snapshot;

        internal Scene(Snapshot snapshot)
        {
            _snapshot = snapshot;
        }

        public int Number
        {
            get { return _snapshot.Index; }
        }

        public string Name
        {
            get { return _snapshot.Values[1]; }
        }

        public string Comment
        {
            get { return _snapshot.Values[2]; }
        }

        public SceneType Type
        {
            get
            {
                return (SceneType)Enum.Parse(typeof(SceneType), _snapshot.Values[3], true);
            }
        }

        public void Recall()
        {
            _snapshot.Desk.Send("ssrecall_ex {0} {1}", _snapshot.Address, _snapshot.Index);
        }
    }

    public enum SceneType
    {
        Empty,
        User,
        PreInst
    }
}