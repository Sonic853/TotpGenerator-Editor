
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sonic853.TotpGen.Models
{
    // [FilePath("853Lab/TotpGenerator/TotpSettings.asset",
    //           FilePathAttribute.Location.ProjectFolder)]
    public sealed class TotpSettings : ScriptableObject
    {
        private static string path = "Assets/853Lab/TotpGenerator/TotpSettings.asset";
        public static TotpSettings instance
        {
            get
            {
                var settings = AssetDatabase.LoadAssetAtPath<TotpSettings>(path);
                if (settings == null)
                {
                    settings = CreateInstance<TotpSettings>();
                    AssetDatabase.CreateAsset(settings, path);
                    AssetDatabase.SaveAssets();
                }
                return settings;
            }
        }
        public string label = "VRChat Udon TOTP";
        public string issuer = "";
        public string algorithm = "SHA1";
        [SerializeField] private int _period = 30;
        public int period
        {
            get
            {
                return _period;
            }
            set
            {
                if (value < 1) _period = 30;
                else _period = value;
            }
        }
        [SerializeField] private int _digits = 6;
        public int digits
        {
            get
            {
                return _digits;
            }
            set
            {
                if (value < 1) _digits = 6;
                else _digits = value;
            }
        }
        [SerializeField] private int _tolerance = 1;
        public int tolerance
        {
            get
            {
                return _tolerance;
            }
            set
            {
                if (value < 0) _tolerance = 0;
                else _tolerance = value;
            }
        }
        public bool autoCreateKey = true;
        public string key = "";
        public string secret { get => TotpGenerator.Base32Encode(key); }
        public void Save()
        {
            if (File.Exists(path))
            {
                EditorUtility.SetDirty(this);
            }
            else
            {
                AssetDatabase.CreateAsset(this, path);
            }
            AssetDatabase.SaveAssets();
        }
        void OnDisable() => Save();
        // label 需要转为 URL 编码
        public string GetUrl()
        {
            string _issuer = Uri.EscapeDataString(string.IsNullOrEmpty(issuer) ? ("TOTP " + secret.Substring(0, 16)) : issuer);
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(algorithm)) parameters.Add("algorithm", algorithm);
            if (digits != 6) parameters.Add("digits", digits.ToString());
            if (period != 30) parameters.Add("period", period.ToString());
            if (!string.IsNullOrEmpty(_issuer)) parameters.Add("issuer", _issuer);
            parameters.Add("secret", secret);
            return $"otpauth://totp/{Uri.EscapeDataString(label)}?{string.Join("&", new List<string>(parameters.Keys).ConvertAll(key => $"{key}={parameters[key]}").ToArray())}";
        }
    }
}
