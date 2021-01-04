 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Crestron.SimplSharp.Reflection;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Cisco
{
    public abstract class CodecApiElement : IDisposable
    {
        private readonly CiscoTelePresenceCodec _codec;

        #region Fields

        private readonly CodecApiElement _parent;
        private readonly string _apiNameSpace;

        #endregion

        #region Constructors

        protected CodecApiElement(CiscoTelePresenceCodec codec)
        {
            _codec = codec;
            _codec.StatusReceived += OnStatusReceived;
            _apiNameSpace = GetType().Name;
        }

        protected CodecApiElement(CiscoTelePresenceCodec codec, int indexer)
            : this(codec)
        {
            _apiNameSpace = _apiNameSpace + "[" + indexer + "]";            
        }

        protected CodecApiElement(CodecApiElement parent, string propertyName)
        {
            _codec = parent.Codec;

            _parent = parent;
            _parent.ChildElementChange += ParentOnChildElementChange;

            _apiNameSpace = string.Format("{0}{1}{2}",
                parent.ApiNameSpace, parent.ApiNameSpace.EndsWith("]") ? "" : ".",
                propertyName);
        }

        protected CodecApiElement(CodecApiElement parent, string propertyName, int indexer)
            : this(parent, propertyName)
        {
            _apiNameSpace = _apiNameSpace + "[" + indexer + "]";
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events

        public event CodecApiElementChangeEventHandler StatusChange;
        internal event CodecApiChildElementChangeEventHandler ChildElementChange;

        #endregion

        #region Delegates
        #endregion

        #region Properties

        public CiscoTelePresenceCodec Codec
        {
            get { return _codec; }
        }

        internal CodecApiElement ParentElement
        {
            get { return _parent; }
        }

        internal string ApiNameSpace
        {
            get { return _apiNameSpace; }
        }

        #endregion

        #region Methods

        public void GetUpdate()
        {
            Codec.Send("xStatus {0}", Regex.Replace(_apiNameSpace, @"\.|\[|\]", " "));
        }

        protected string[] UpdateFromStatus(StatusUpdateItem[] items)
        {
            var propertyNames = new List<string>();
            var childElementsToUpdate = new List<CodecApiElement>();

            foreach (var match in from statusUpdateItem in items.Where(i => i.Path.StartsWith(_apiNameSpace))
                let pattern = Regex.Replace(_apiNameSpace, @"\.|\[|\]", @"\$&") + @"\.?(\w+)\.?"
                select Regex.Match(statusUpdateItem.Path, pattern)
                into match
                where match.Success
                select match)
            {
                try
                {
                    var field = GetFieldWithAttributeName(match.Groups[1].Value);
                    if(field == null) continue;
                    if (!field.FieldType.IsSubclassOf(typeof (CodecApiElement).GetCType())) continue;
                    var element = (CodecApiElement) field.GetValue(this);
                    if (childElementsToUpdate.Contains(element)) continue;
                    childElementsToUpdate.Add(element);
                    propertyNames.Add(match.Groups[1].Value);
                }
                catch (Exception e)
                {
                    CloudLog.Error("Updating status for CodecApiElement property \"{0}\", {1}", match, e.Message);
                }
            }

            foreach (var statusUpdateItem in items.Where(i => i.Path == _apiNameSpace))
            {
                try
                {
                    if (statusUpdateItem.PropertyName.Length == 0) continue;
                    var field = GetFieldWithAttributeName(statusUpdateItem.PropertyName);
                    //Debug.WriteInfo(field.Name, field.FieldType.Name);
                    
                    if (field == null)
                    {
                        //Debug.WriteError("Error", "parsing property \"{0}\" in {1}, value = {2}, field is null",
                        //   statusUpdateItem.PropertyName, _apiNameSpace, statusUpdateItem.StringValue);
                        HandleUndefinedPropertyItem(statusUpdateItem);
                    }
                    else if (field.FieldType == typeof (bool))
                    {
                        field.SetValue(this, bool.Parse(statusUpdateItem.StringValue),
                            BindingFlags.NonPublic | BindingFlags.Instance, null, null);
                        propertyNames.Add(statusUpdateItem.PropertyName);
                    }
                    else if (field.FieldType == typeof (string))
                    {
                        field.SetValue(this, statusUpdateItem.StringValue,
                            BindingFlags.NonPublic | BindingFlags.Instance, null, null);
                        propertyNames.Add(statusUpdateItem.PropertyName);
                    }
                    else if (field.FieldType == typeof (int))
                    {
                        field.SetValue(this, (int) statusUpdateItem.Value,
                            BindingFlags.NonPublic | BindingFlags.Instance, null, null);
                        propertyNames.Add(statusUpdateItem.PropertyName);
                    }
                    else if (field.FieldType == typeof (double))
                    {
                        field.SetValue(this, statusUpdateItem.Value,
                            BindingFlags.NonPublic | BindingFlags.Instance, null, null);
                        propertyNames.Add(statusUpdateItem.PropertyName);
                    }
                    else if (field.FieldType == typeof (TimeSpan))
                    {
                        field.SetValue(this, TimeSpan.FromSeconds(statusUpdateItem.Value),
                            BindingFlags.NonPublic | BindingFlags.Instance, null, null);
                        propertyNames.Add(statusUpdateItem.PropertyName);
                    }
                    else if (field.FieldType.IsEnum)
                    {
                        field.SetValue(this, Enum.Parse(field.FieldType, statusUpdateItem.StringValue, false),
                            BindingFlags.NonPublic | BindingFlags.Instance, null, null);
                        propertyNames.Add(statusUpdateItem.PropertyName);
                    }
                    else if (field.FieldType == typeof(DateTime) && statusUpdateItem.PropertyName == "Duration")
                    {
                        var time = DateTime.Now - TimeSpan.FromSeconds(statusUpdateItem.Value);
                        field.SetValue(this, time, BindingFlags.NonPublic | BindingFlags.Instance, null, null);
                        propertyNames.Add(statusUpdateItem.PropertyName);
                    }
                }
                catch(Exception e)
                {
                    Debug.WriteError("Error", "parsing property \"{0}\" in {1}, value = {2}, {3}",
                        statusUpdateItem.PropertyName, _apiNameSpace, statusUpdateItem.StringValue, e.Message);
                    HandleUndefinedPropertyItem(statusUpdateItem);
                }
            }

            foreach (
                var statusUpdateItem in
                    items.Where(i => i.Path.StartsWith(_apiNameSpace) && i.Path != _apiNameSpace && i.Path.EndsWith("]"))
                )
            {
                var path = _apiNameSpace.Replace(".", @"\.");
                var match = Regex.Match(statusUpdateItem.Path, path + @"\.(\w+)\[(\d+)\]");
                if(!match.Success) continue;
                var propertyName = match.Groups[1].Value;
                var index = int.Parse(match.Groups[2].Value);
                if (propertyNames.All(n => n != propertyName))
                    propertyNames.Add(propertyName);

                try
                {
                    var field = GetFieldWithAttributeName(propertyName);
                    if (field == null)
                    {
                        HandleUndefinedPropertyArray(propertyName, index, items); 
                    }
                    else if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                    {
#if DEBUG
                        Debug.WriteInfo("Field", "Dictionary<{0},{1}>", field.FieldType.GetGenericArguments()[0].Name, field.FieldType.GetGenericArguments()[1].Name);
#endif
                        if (field.FieldType.GetGenericArguments()[0] != typeof (int)) continue;
                        var itemType = field.FieldType.GetGenericArguments()[1];
                        var dict = field.GetValue(this);
                        var keyExistsMethod = field.FieldType.GetMethod("ContainsKey", new CType[] { typeof(int) });
                        var exists = (bool) keyExistsMethod.Invoke(dict, new object[] {index});
                        if (exists && statusUpdateItem.Attributes == "ghost=True")
                        {
#if DEBUG
                            //Debug.WriteWarn("removing");
#endif
                            try
                            {
                                var removeMethod = field.FieldType.GetMethod("Remove", new CType[] {typeof (int)});
                                removeMethod.Invoke(dict, new object[] {index});
                                continue;
                            }
                            catch (Exception e)
                            {
                                CloudLog.Error("Error while trying to remove \"{0}\" item, {1}", match.ToString(), e.Message);
                                continue;
                            }
                        }

                        if (exists)
                        {
#if DEBUG
                            //Debug.WriteInfo("updating"); 
#endif
                            var p = field.FieldType.GetProperty("Item");
                            var obj = (CodecApiElement) p.GetValue(dict, new object[] {index});
                            if (!childElementsToUpdate.Contains(obj))
                            {
                                childElementsToUpdate.Add(obj);
                            }
                            continue;
                        }
#if DEBUG
                        //Debug.WriteSuccess("adding"); 
#endif
                        var ctor =
                            itemType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new CType[]
                            {typeof (CodecApiElement), typeof(string), typeof (int)}, null);

                        if (ctor == null)
                        {
                            CloudLog.Error("Updating array property, {0}.{2}[{1}], ctor is null, check defined ctor",
                                _apiNameSpace, index, propertyName);
                            continue;
                        }
#if DEBUG
                        //Debug.WriteNormal("ctor", ctor.ToString()); 
#endif
                        var newItem = (CodecApiElement) ctor.Invoke(new object[] {this, propertyName, index});
                        var addMethod = field.FieldType.GetMethod("Add", new CType[] {typeof (int), itemType});
                        addMethod.Invoke(dict, new object[] {index, newItem});
                        try
                        {
                            if (!childElementsToUpdate.Contains(newItem))
                            {
                                childElementsToUpdate.Add(newItem);
                            }
                        }
                        catch (Exception e)
                        {
                            CloudLog.Exception(e);
                            throw e;
                        }
                    }
                }
                catch (Exception e)
                {
                    CloudLog.Error("Updating array property, {0}.{2}[{1}], {3}", _apiNameSpace, index, propertyName,
                        e.Message);
                    HandleUndefinedPropertyArray(propertyName, index, items);                    
                }
            }

            foreach (var element in childElementsToUpdate)
            {
                //Debug.WriteNormal(string.Format("Updating element {0}", element.GetType().Name));

                OnChildElementChange(element, items);
            }

            foreach (var name in propertyNames)
            {
                //Debug.WriteInfo(_apiNameSpace, "{0} updated", name);                
            }

            return propertyNames.ToArray();
        }

        private FieldInfo GetFieldWithAttributeName(string attributeName)
        {
            return (from fieldInfo in GetType().GetCType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                let attributes = fieldInfo.GetCustomAttributes(typeof (CodecApiNameAttribute), false)
                where attributes.Any(attribute => ((CodecApiNameAttribute) attribute).Name == attributeName)
                select fieldInfo).FirstOrDefault();
        }

        private void OnStatusReceived(CiscoTelePresenceCodec codec, StatusUpdateItem[] items)
        {
            //Debug.WriteInfo(_apiNameSpace, "OnStatusReceived");
            var names = UpdateFromStatus(items);
            if (names.Any())
            {
                OnStatusChanged(this, names);
            }
        }

        protected virtual void OnChildElementChange(CodecApiElement childelement, StatusUpdateItem[] statusupdate)
        {
            var handler = ChildElementChange;
            try
            {
                if (handler != null) handler(childelement, statusupdate);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        private void ParentOnChildElementChange(CodecApiElement childElement, StatusUpdateItem[] statusUpdate)
        {
            if(childElement != this) return;
            //Debug.WriteInfo(_apiNameSpace, "OnStatusReceived");
            var names = UpdateFromStatus(statusUpdate);
            if (names.Any())
            {
                OnStatusChanged(this, names);
            }
        }

        protected virtual void HandleUndefinedPropertyArray(string propertyName, int index, StatusUpdateItem[] statusUpdate)
        {
            
        }

        protected virtual void HandleUndefinedPropertyItem(StatusUpdateItem item)
        {
            Debug.WriteWarn("HandleUndefinedPropertyName({0})", item);
        }

        protected virtual void OnStatusChanged(CodecApiElement element, string[] propertyNamesWhichUpdated)
        {
#if DEBUG
            foreach (var name in propertyNamesWhichUpdated)
            {
                try
                {
                    var f = GetFieldWithAttributeName(name);
                    if (f != null)
                    {
                        Debug.WriteInfo(_apiNameSpace, "{0} = {1}", name, f.GetValue(this).ToString());
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteWarn(_apiNameSpace, "Could not get value for debug on property: {0}, {1}", name, e.Message);
                }
            }
#endif
            if (StatusChange == null) return;
            try
            {
                StatusChange(this, propertyNamesWhichUpdated);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        #endregion

        public void Dispose()
        {
            _codec.StatusReceived -= OnStatusReceived;
        }
    }

    public delegate void CodecApiElementChangeEventHandler(CodecApiElement element, string[] propertyNamesWhichUpdated);

    internal delegate void CodecApiChildElementChangeEventHandler(
        CodecApiElement childElement, StatusUpdateItem[] statusUpdate);
}