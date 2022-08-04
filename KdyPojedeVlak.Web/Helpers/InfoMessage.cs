using System;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace KdyPojedeVlak.Web.Helpers
{
    public static class InfoMessage
    {
        private const string MessageClassKey = "messageClass";
        private const string MessageStrKey = "messageStr";

        public static void RegisterMessage(ITempDataDictionary tempData, MessageClass type, string message)
        {
            if (HasMessage(tempData)) throw new InvalidOperationException("A message is already waiting");
            tempData[MessageClassKey] = type.ToString().ToLowerInvariant();
            tempData[MessageStrKey] = message;
        }

        public static bool HasMessage(ITempDataDictionary tempData) => tempData.ContainsKey(MessageClassKey);
        public static string GetMessageClass(ITempDataDictionary tempData) => (string) tempData[MessageClassKey];
        public static string GetMessageStr(ITempDataDictionary tempData) => (string) tempData[MessageStrKey];

        public static void DropMessage(ITempDataDictionary tempData)
        {
            tempData.Remove(MessageClassKey);
            tempData.Remove(MessageStrKey);
        }
    }

    public enum MessageClass
    {
        Success,
        Info,
        Warning,
        Danger
    }
}