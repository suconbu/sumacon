﻿//------------------------------------------------------------------------------
// <auto-generated>
//     このコードはツールによって生成されました。
//     ランタイム バージョン:4.0.30319.42000
//
//     このファイルへの変更は、以下の状況下で不正な動作の原因になったり、
//     コードが再生成されるときに損失したりします。
// </auto-generated>
//------------------------------------------------------------------------------

namespace Suconbu.Sumacon.Properties {
    using System;
    
    
    /// <summary>
    ///   ローカライズされた文字列などを検索するための、厳密に型指定されたリソース クラスです。
    /// </summary>
    // このクラスは StronglyTypedResourceBuilder クラスが ResGen
    // または Visual Studio のようなツールを使用して自動生成されました。
    // メンバーを追加または削除するには、.ResX ファイルを編集して、/str オプションと共に
    // ResGen を実行し直すか、または VS プロジェクトをビルドし直します。
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   このクラスで使用されているキャッシュされた ResourceManager インスタンスを返します。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Suconbu.Sumacon.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   すべてについて、現在のスレッドの CurrentUICulture プロパティをオーバーライドします
        ///   現在のスレッドの CurrentUICulture プロパティをオーバーライドします。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   {device-id} - {device-model} ({device-name}) に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string DeviceLabelFormat {
            get {
                return ResourceManager.GetString("DeviceLabelFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Are you sure you want to delete {0} files? に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string DialogMessage_DeleteXFiles {
            get {
                return ResourceManager.GetString("DialogMessage_DeleteXFiles", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Delete file に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string DialogTitle_DeleteFile {
            get {
                return ResourceManager.GetString("DialogTitle_DeleteFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   {device-id} : &apos;HXC8KSKL99XYZ&apos;
        ///{device-model} : &apos;Nexus_9&apos;
        ///{device-name} : &apos;flounder&apos;
        ///{date} : &apos;2018-12-31&apos;
        ///{time} : &apos;132436&apos;
        ///{no} : Sequence no which based on application start. に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string FileNamePatternHelp {
            get {
                return ResourceManager.GetString("FileNamePatternHelp", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Stop
        ///({0} shots remains) に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string FormCapture_ButtonLabel_ContinousLimited {
            get {
                return ResourceManager.GetString("FormCapture_ButtonLabel_ContinousLimited", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Stop
        ///({0} captured) に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string FormCapture_ButtonLabel_ContinousLimitless {
            get {
                return ResourceManager.GetString("FormCapture_ButtonLabel_ContinousLimitless", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Capture に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string FormCapture_ButtonLabel_Start {
            get {
                return ResourceManager.GetString("FormCapture_ButtonLabel_Start", resourceCulture);
            }
        }
        
        /// <summary>
        ///   {device-model}_{date}_{time}_{no}.png に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string FormCapture_DefaultFileNamePattern {
            get {
                return ResourceManager.GetString("FormCapture_DefaultFileNamePattern", resourceCulture);
            }
        }
        
        /// <summary>
        ///   .\screencapture に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string FormCapture_DefaultSaveDirectoryPath {
            get {
                return ResourceManager.GetString("FormCapture_DefaultSaveDirectoryPath", resourceCulture);
            }
        }
        
        /// <summary>
        ///   🔋 {battery-level} % ({battery-status}) に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string FormMain_StatusBatteryFormat {
            get {
                return ResourceManager.GetString("FormMain_StatusBatteryFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   {device-model} ({device-name}) に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string FormMain_StatusDeviceFormat {
            get {
                return ResourceManager.GetString("FormMain_StatusDeviceFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   {screen-width}x{screen-height} ({screen-density} dpi) に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string FormMain_StatusScreenFormat {
            get {
                return ResourceManager.GetString("FormMain_StatusScreenFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Reset ALL properties に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string FormProperty_MenuItemLabel_ResetAll {
            get {
                return ResourceManager.GetString("FormProperty_MenuItemLabel_ResetAll", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Reset &apos;{0}&apos; properties に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string FormProperty_MenuItemLabel_ResetGroup {
            get {
                return ResourceManager.GetString("FormProperty_MenuItemLabel_ResetGroup", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Reset &apos;{0}&apos; に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string FormProperty_MenuItemLabel_ResetOne {
            get {
                return ResourceManager.GetString("FormProperty_MenuItemLabel_ResetOne", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Approx\. 0 MB に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string FormRecord_ApproxFormat {
            get {
                return ResourceManager.GetString("FormRecord_ApproxFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Stop
        ///({0} sec.) に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string FormRecord_ButtonLabel_Recording {
            get {
                return ResourceManager.GetString("FormRecord_ButtonLabel_Recording", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Record に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string FormRecord_ButtonLabel_Start {
            get {
                return ResourceManager.GetString("FormRecord_ButtonLabel_Start", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Stop に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string FormRecord_ButtonLabel_Stop {
            get {
                return ResourceManager.GetString("FormRecord_ButtonLabel_Stop", resourceCulture);
            }
        }
        
        /// <summary>
        ///   {device-model}_{date}_{time}_{width}x{height}_{no}.mp4 に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string FormRecord_DefaultFileNamePattern {
            get {
                return ResourceManager.GetString("FormRecord_DefaultFileNamePattern", resourceCulture);
            }
        }
        
        /// <summary>
        ///   .\screenrecord に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string FormRecord_DefaultSaveDirectoryPath {
            get {
                return ResourceManager.GetString("FormRecord_DefaultSaveDirectoryPath", resourceCulture);
            }
        }
        
        /// <summary>
        ///   .\command に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string FormShortcut_DirectoryPath {
            get {
                return ResourceManager.GetString("FormShortcut_DirectoryPath", resourceCulture);
            }
        }
        
        /// <summary>
        ///   *.txt に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string FormShortcut_FileNameFilter {
            get {
                return ResourceManager.GetString("FormShortcut_FileNameFilter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Date time に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string General_DateTime {
            get {
                return ResourceManager.GetString("General_DateTime", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Description に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string General_Description {
            get {
                return ResourceManager.GetString("General_Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Name に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string General_Name {
            get {
                return ResourceManager.GetString("General_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Size に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string General_Size {
            get {
                return ResourceManager.GetString("General_Size", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Time (sec) に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string General_TimeSecondsLength {
            get {
                return ResourceManager.GetString("General_TimeSecondsLength", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Copy image to clipboard に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Menu_CopyImageToClipboard {
            get {
                return ResourceManager.GetString("Menu_CopyImageToClipboard", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Delete に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Menu_Delete {
            get {
                return ResourceManager.GetString("Menu_Delete", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Open directory に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Menu_OpenDirectory {
            get {
                return ResourceManager.GetString("Menu_OpenDirectory", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Open file に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string Menu_OpenFile {
            get {
                return ResourceManager.GetString("Menu_OpenFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Consolas に類似しているローカライズされた文字列を検索します。
        /// </summary>
        internal static string MonospaceFontName {
            get {
                return ResourceManager.GetString("MonospaceFontName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   (アイコン) に類似した型 System.Drawing.Icon のローカライズされたリソースを検索します。
        /// </summary>
        internal static System.Drawing.Icon sumacon {
            get {
                object obj = ResourceManager.GetObject("sumacon", resourceCulture);
                return ((System.Drawing.Icon)(obj));
            }
        }
    }
}
