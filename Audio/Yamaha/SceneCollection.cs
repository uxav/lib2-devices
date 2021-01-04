 
using System;
using System.Collections;
using System.Collections.Generic;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Audio.Yamaha
{
    public class SceneCollection : IEnumerable<Scene>
    {
        private readonly YamahaDesk _desk;
        private readonly Dictionary<int, Scene> _scenes = new Dictionary<int, Scene>();

        internal SceneCollection(YamahaDesk desk, SnapshotCollection collection)
        {
            _desk = desk;
            collection.SnapshotUpdated += OnSnapshotUpdated;
            foreach (var item in collection)
            {
                _scenes[item.Index] = new Scene(item);
            }
        }

        public event SceneUpdatedEventHandler SceneUpdated;

        private void OnSnapshotUpdated(SnapshotCollection snapshots, int index)
        {
            try
            {
                if (SceneUpdated != null)
                {
                    SceneUpdated(this, index);
                }
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        public Scene this[int number]
        {
            get { return _scenes[number]; }
        }

        public IEnumerator<Scene> GetEnumerator()
        {
            return _scenes.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public delegate void SceneUpdatedEventHandler(SceneCollection sceneCollection, int number);
}