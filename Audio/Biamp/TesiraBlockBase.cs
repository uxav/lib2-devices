 
using System;
using System.Collections.Generic;
using Crestron.SimplSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.DeviceSupport;

namespace UX.Lib2.Devices.Audio.Biamp
{
    public abstract class TesiraBlockBase
    {
        private readonly Tesira _device;
        private readonly string _instanceTag;
        protected readonly Dictionary<string, TesiraAttributeCode> Subscriptions = new Dictionary<string, TesiraAttributeCode>();
        private CTimer _subscribeTimer;
        private string _name;

        protected TesiraBlockBase(Tesira device, string instanceTag)
        {
            _device = device;
            _device.DeviceCommunicatingChange += OnDeviceCommunicatingChange;
            _device.ReceivedData += OnReceivedData;
            _instanceTag = instanceTag;
            _device.Controls[_instanceTag] = this;
        }

        public Tesira Device
        {
            get { return _device; }
        }

        public string InstanceTag
        {
            get { return _instanceTag; }
        }

        public string Name
        {
            get
            {
                return string.IsNullOrEmpty(_name) ? InstanceTag : _name;
            }
            set { _name = value; }
        }

        public abstract TesiraBlockType Type { get; }

        public event TesiraBlockInitializedEventHandler HasInitialized;

        protected void OnDeviceCommunicatingChange(IDevice device, bool communicating)
        {
            if (communicating)
            {
                ControlShouldInitialize();

                _subscribeTimer = new CTimer(specific =>
                {
                    foreach (var value in Subscriptions)
                    {
                        SendSubscribe(value.Key, value.Value);
                    }
                }, 1000);
            }
        }

        protected abstract void ControlShouldInitialize();

        private void OnReceivedData(TtpSshClient client, TesiraMessage message)
        {
            if (message.Type != TesiraMessageType.Notification && message.Id == InstanceTag)
            {
                var response = message as TesiraResponse;
                if (response != null && response.Type == TesiraMessageType.OkWithResponse)
                {
                    Debug.WriteSuccess(GetType().Name + " \"" + InstanceTag + "\"", "Received {0}\r\n{1}",
                        response.AttributeCode, response.TryParseResponse().ToString(Formatting.Indented));
                    ReceivedResponse(response);
                }
            }
            else if (message.Type == TesiraMessageType.Notification && Subscriptions.ContainsKey(message.Id))
            {
                var response = message as TesiraNotification;
                if (response != null)
                {
                    Debug.WriteSuccess(GetType().Name + " \"" + InstanceTag + "\"", "Received notification \"{0}\"\r\n{1}",
                        Subscriptions[message.Id], response.TryParseResponse().ToString(Formatting.Indented));
                    ReceivedNotification(Subscriptions[message.Id], response.TryParseResponse());
                }
            }
        }

        protected abstract void ReceivedResponse(TesiraResponse response);
        protected abstract void ReceivedNotification(TesiraAttributeCode attributeCode, JToken data);

        public abstract void Subscribe();

        public virtual void Unsubscribe()
        {
            foreach (var publishToken in Subscriptions.Keys)
            {
                Unsubscribe(publishToken);
            }
        }

        protected void Subscribe(TesiraAttributeCode attributeCode)
        {
            var token = InstanceTag + "_" + attributeCode.ToCommandString();
            Subscriptions[token] = attributeCode;
            if (!Device.DeviceCommunicating) return;
            SendSubscribe(token, attributeCode);
        }

        private void SendSubscribe(string publishToken, TesiraAttributeCode attributeCode)
        {
            var message = Tesira.FormatBaseMessage(InstanceTag, TesiraCommand.Subscribe, attributeCode) + " \"" +
                          publishToken + "\" " + 200;
            Device.Send(message);
        }

        protected void Unsubscribe(string publishToken)
        {
            if (Subscriptions.ContainsKey(publishToken))
            {
                if (Device.DeviceCommunicating)
                {
                    var message =
                        Tesira.FormatBaseMessage(InstanceTag, TesiraCommand.Unsubscribe, Subscriptions[publishToken]) +
                        " \"" + publishToken + "\"";
                    Device.Send(message);
                }

                Subscriptions.Remove(publishToken);
            }
        }

        protected virtual void OnInitialized()
        {
            CloudLog.Notice("Tesira block {0} \"{1}\" OnInitialized()", GetType().Name, Name);
            var handler = HasInitialized;
            if (handler == null) return;
            try
            {
                handler(this);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }
    }

    public enum TesiraBlockType
    {
        LevelControlBlock,
        MuteControlBlock,
        DialerBlock,
        LogicStateBlock,
        SourceSelectorBlock,
        InputBlock,
        DanteInputBlock,
        AecInputBlock
    }

    public delegate void TesiraBlockInitializedEventHandler(TesiraBlockBase block);
}