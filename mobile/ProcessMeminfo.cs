using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Suconbu.Mobile
{
    public class ProcessMeminfo
    {
        public int Pid { get; private set; }
        public OverallEntry NativeHeap { get { return this.overallEntries.GetValue(OverallItem.Native_Heap, new OverallEntry()); } }
        public OverallEntry DalvikHeap { get { return this.overallEntries.GetValue(OverallItem.Dalvik_Heap, new OverallEntry()); } }
        public OverallEntry DalvikOther { get { return this.overallEntries.GetValue(OverallItem.Dalvik_Other, new OverallEntry()); } }
        public OverallEntry Stack { get { return this.overallEntries.GetValue(OverallItem.Stack, new OverallEntry()); } }
        public OverallEntry Ashmem { get { return this.overallEntries.GetValue(OverallItem.Ashmem, new OverallEntry()); } }
        public OverallEntry OthreDev { get { return this.overallEntries.GetValue(OverallItem.Other_dev, new OverallEntry()); } }
        public OverallEntry SoMmap { get { return this.overallEntries.GetValue(OverallItem.So_mmap, new OverallEntry()); } }
        public OverallEntry DexMmap { get { return this.overallEntries.GetValue(OverallItem.Dex_mmap, new OverallEntry()); } }
        public OverallEntry OatMmap { get { return this.overallEntries.GetValue(OverallItem.Oat_mmap, new OverallEntry()); } }
        public OverallEntry ArtMmap { get { return this.overallEntries.GetValue(OverallItem.Art_mmap, new OverallEntry()); } }
        public OverallEntry OtherMmap { get { return this.overallEntries.GetValue(OverallItem.Other_mmap, new OverallEntry()); } }
        public OverallEntry EglMtrack { get { return this.overallEntries.GetValue(OverallItem.EGL_mtrack, new OverallEntry()); } }
        public OverallEntry GlMtrack { get { return this.overallEntries.GetValue(OverallItem.GL_mtrack, new OverallEntry()); } }
        public OverallEntry OtherMtrack { get { return this.overallEntries.GetValue(OverallItem.Other_mtrack, new OverallEntry()); } }
        public OverallEntry Unknown { get { return this.overallEntries.GetValue(OverallItem.Unknown, new OverallEntry()); } }
        public OverallEntry Total { get { return this.overallEntries.GetValue(OverallItem.TOTAL, new OverallEntry()); } }
        public int Views { get { return this.objectsEntries.GetValue(ObjectsItem.Views, 0); } }
        public int ViewRootImpl { get { return this.objectsEntries.GetValue(ObjectsItem.ViewRootImpl, 0); } }
        public int AppContexts { get { return this.objectsEntries.GetValue(ObjectsItem.AppContexts, 0); } }
        public int Activities { get { return this.objectsEntries.GetValue(ObjectsItem.Activities, 0); } }
        public int Assets { get { return this.objectsEntries.GetValue(ObjectsItem.Assets, 0); } }
        public int AssetManagers { get { return this.objectsEntries.GetValue(ObjectsItem.AssetManagers, 0); } }
        public int Local_Binders { get { return this.objectsEntries.GetValue(ObjectsItem.Local_Binders, 0); } }
        public int Proxy_Binders { get { return this.objectsEntries.GetValue(ObjectsItem.Proxy_Binders, 0); } }
        public int Parcel_memory { get { return this.objectsEntries.GetValue(ObjectsItem.Parcel_memory, 0); } }
        public int Parcel_count { get { return this.objectsEntries.GetValue(ObjectsItem.Parcel_count, 0); } }
        public int Death_Recipients { get { return this.objectsEntries.GetValue(ObjectsItem.Death_Recipients, 0); } }
        public int OpenSSL_Sockets { get { return this.objectsEntries.GetValue(ObjectsItem.OpenSSL_Sockets, 0); } }
        public int WebViews { get { return this.objectsEntries.GetValue(ObjectsItem.WebViews, 0); } }

        enum OverallItem { Native_Heap, Dalvik_Heap, Dalvik_Other, Stack, Ashmem, Other_dev, So_mmap, Dex_mmap, Oat_mmap, Art_mmap, Other_mmap, EGL_mtrack, GL_mtrack, Other_mtrack, Unknown, TOTAL }
        Dictionary<OverallItem, OverallEntry> overallEntries = new Dictionary<OverallItem, OverallEntry>();

        enum ObjectsItem { Views, ViewRootImpl, AppContexts, Activities, Assets, AssetManagers, Local_Binders, Proxy_Binders, Parcel_memory, Parcel_count, Death_Recipients, OpenSSL_Sockets, WebViews }
        Dictionary<ObjectsItem, int> objectsEntries = new Dictionary<ObjectsItem, int>();

        readonly Regex overallPattern = new Regex(@"^\s*(\.?\w+(?: \w+)?)\s+(.+)");
        readonly Regex objectsPattern = new Regex(@"^\s*(\w+(?: \w+)?):\s+(\d+)(?:\s+(\w+(?: \w+)?):\s+(\d+))?");

        public static CommandContext GetAsync(Device device, ProcessEntry process, Action<ProcessMeminfo> onFinished)
        {
            var meminfo = new ProcessMeminfo() { Pid = process.Pid };
            return device.RunCommandAsync($"shell dumpsys meminfo {process.Pid}", output =>
            {
                if (output == null)
                {
                    onFinished?.Invoke(meminfo);
                    return;
                }

                if (meminfo.TryParseOverall(output)) return;
                if (meminfo.TryParseObjects(output)) return;
            });
        }

        bool TryParseOverall(string input)
        {
            var match = this.overallPattern.Match(input);
            if (!match.Success) return false;

            var name = match.Groups[1].Value;
            if (!GetMatchedItemName(name, typeof(OverallItem), out var itemName)) return false;

            var item = (OverallItem)Enum.Parse(typeof(OverallItem), itemName);
            var tokens = match.Groups[2].Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var values = tokens.Select(t => int.TryParse(t, out var v) ? v : 0).ToArray();
            this.overallEntries[item] =  new OverallEntry()
            {
                PssTotal = (values.Length > 0) ? values[0] : 0,
                PrivateDirty = (values.Length > 1) ? values[1] : 0,
                PrivateClean = (values.Length > 2) ? values[2] : 0,
                SwapPssDirty = (values.Length > 3) ? values[3] : 0,
                HeapSize = (values.Length > 4) ? values[4] : 0,
                HeapAlloc = (values.Length > 5) ? values[5] : 0,
                HeapFree = (values.Length > 6) ? values[6] : 0
            };
            return true;
        }

        bool TryParseObjects(string input)
        {
            var match = this.objectsPattern.Match(input);
            if (!match.Success) return false;

            int count = match.Groups.Count / 2;

            for (var i = 0; i < count; i++)
            {
                var name = match.Groups[1].Value;
                if (!GetMatchedItemName(name, typeof(ObjectsItem), out var itemName)) return false;

                var item = (ObjectsItem)Enum.Parse(typeof(ObjectsItem), itemName);
                this.objectsEntries[item] = int.TryParse(match.Groups[2].Value, out var v) ? v : 0;
            }
            return true;
        }

        static bool GetMatchedItemName(string name, Type enumType, out string matchedName)
        {
            var comparedName = name.Replace(".", "").Replace(" ", "_");
            foreach(var enumName in Enum.GetNames(enumType))
            {
                if(comparedName == enumName)
                {
                    matchedName = enumName;
                    return true;
                }
            }
            matchedName = null;
            return false;
        }

        ProcessMeminfo()
        {
        }

        public struct OverallEntry
        {
            public int PssTotal { get; internal set; }
            public int PrivateDirty { get; internal set; }
            public int PrivateClean { get; internal set; }
            public int SwapPssDirty { get; internal set; }
            public int HeapSize { get; internal set; }
            public int HeapAlloc { get; internal set; }
            public int HeapFree { get; internal set; }
        }
    }
}
