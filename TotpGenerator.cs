using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using ZXing;
using ZXing.QrCode;


namespace Sonic853.TotpGen
{
    public class TotpGenerator
    {
        private static readonly string path = "Assets/853Lab/TotpGenerator/";
        private static readonly string s_StyleSheetPath = path + "StyleSheets/TotpGenerator.uss";
        private static string base_32_chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        private static string[] Base32Chars = new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "2", "3", "4", "5", "6", "7" };
        private static string lower = "abcdefghijklmnopqrstuvwxyz";
        private static string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static string numbers = "0123456789";
        private static string special = @"!@#$%^&*()-_ []{}<>~`+=,.;:/?|";
        public static string issuer
        {
            get
            {
                return EditorPrefs.GetString("Sonic853.TotpGenerator.issuer", "");
            }
            set
            {
                EditorPrefs.SetString("Sonic853.TotpGenerator.issuer", value);
            }
        }
        public static string algorithm
        {
            get
            {
                return EditorPrefs.GetString("Sonic853.TotpGenerator.algorithm", "SHA1");
            }
            set
            {
                EditorPrefs.SetString("Sonic853.TotpGenerator.algorithm", value);
            }
        }
        public static int period
        {
            get
            {
                return EditorPrefs.GetInt("Sonic853.TotpGenerator.period", 30);
            }
            set
            {
                if (value < 1)
                {
                    value = 30;
                }
                EditorPrefs.SetInt("Sonic853.TotpGenerator.period", value);
            }
        }
        public static int digits
        {
            get
            {
                return EditorPrefs.GetInt("Sonic853.TotpGenerator.digits", 6);
            }
            set
            {
                if (value < 1)
                {
                    value = 6;
                }
                EditorPrefs.SetInt("Sonic853.TotpGenerator.digits", value);
            }
        }
        public static int tolerance
        {
            get
            {
                return EditorPrefs.GetInt("Sonic853.TotpGenerator.tolerance", 1);
            }
            set
            {
                if (value < 0)
                {
                    value = 0;
                }
                EditorPrefs.SetInt("Sonic853.TotpGenerator.tolerance", value);
            }
        }
        public static bool autoCreateKey
        {
            get
            {
                return EditorPrefs.GetBool("Sonic853.TotpGenerator.autoCreateKey", true);
            }
            set
            {
                EditorPrefs.SetBool("Sonic853.TotpGenerator.autoCreateKey", value);
            }
        }
        public static string key
        {
            get
            {
                return EditorPrefs.GetString("Sonic853.TotpGenerator.key", "");
            }
            set
            {
                EditorPrefs.SetString("Sonic853.TotpGenerator.key", value);
            }
        }
        public static string secret
        {
            get
            {
                if (string.IsNullOrEmpty(key))
                {
                    return "";
                }
                return Base32Encode(key);
            }
            // set
            // {
            //     key = Base32Decode(value);
            // }
        }
        static VisualElement root;
        static TextField issuerField;
        static TextField keyField;
        static TextField secretField;
        static Image qrCodeImage;
        static TextField verifyField;
        static Button verifyButton;
        static Button copyKeyButton;
        // static Button createTOTPKeypadButton;
        static string[] SplitString(string input, int length)
        {
            if (length < 1)
                return null;

            int stringLength = input.Length;
            int splitLength = (int)Math.Ceiling((double)stringLength / length);

            string[] result = new string[splitLength];

            for (int i = 0; i < splitLength; i++)
            {
                int startIndex = i * length;
                int currentLength = Math.Min(length, stringLength - startIndex);
                result[i] = input.Substring(startIndex, currentLength);
            }

            return result;
        }
        public static string Base32Encode(string input)
        {
            if (input == null || input.Trim().Length == 0)
            {
                return "";
            }
            string binaryString = "";
            foreach (char character in input.ToCharArray())
            {
                binaryString += Convert.ToString(character, 2).PadLeft(8, '0');
            }

            var five_bit_sections = SplitString(binaryString, 5);
            string base32String = "";
            foreach (string five_bit_section in five_bit_sections)
            {
                base32String += Base32Chars[Convert.ToInt32(five_bit_section.PadRight(5, '0'), 2)];
            }
            return base32String;
        }
        public static string Base32Decode(string input)
        {
            input = input.ToUpper();
            int l = input.Length;
            int n = 0;
            int j = 0;
            string binary = "";
            for (int i = 0; i < l; i++)
            {
                n <<= 5;
                n += base_32_chars.IndexOf(input[i]);
                j += 5;
                if (j >= 8)
                {
                    j -= 8;
                    binary += (char)((n & (0xFF << j)) >> j);
                }
            }
            return binary;
        }
        public static string GeneratePassword(int length, bool special_chars, bool extra_special_chars)
        {
            if (length <= 0)
                length = 12;
            string password = "";
            string characterList = lower + upper + numbers;
            if (special_chars)
                characterList += special;
            if (extra_special_chars)
                characterList += @"£€¥";
            System.Random rand = new System.Random();
            for (int i = 0; i < length; i++)
            {
                password += characterList[rand.Next(characterList.Length)];
            }
            return password;
        }
        private static Color32[] Encode(string textForEncoding, int width, int height)
        {
            BarcodeWriter writer = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Height = height,
                    Width = width
                }
            };
            return writer.Write(textForEncoding);
        }
        public static Texture2D GenerateQR(string text)
        {
            Texture2D encoded = new Texture2D(256, 256);
            Color32[] color32 = Encode(text, encoded.width, encoded.height);
            encoded.SetPixels32(color32);
            encoded.Apply();
            return encoded;
        }
        public static VisualElement CreateUI()
        {
            root = new VisualElement();
            root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(s_StyleSheetPath));
            issuerField = new TextField(_("名称"))
            {
                value = issuer
            };
            issuerField.RegisterValueChangedCallback((e) =>
            {
                issuer = e.newValue;
            });
            root.Add(issuerField);
            PopupField<string> tOTPTypeField = new PopupField<string>(new List<string> { "SHA1", "SHA256", "SHA512" }, algorithm)
            {
                label = _("TOTP 算法："),
            };
            tOTPTypeField.RegisterValueChangedCallback((e) =>
            {
                algorithm = e.newValue;
            });
            // tOTPTypeField 只读
            // tOTPTypeField.SetEnabled(false);
            root.Add(tOTPTypeField);
            IntegerField intervalField = new IntegerField()
            {
                label = _("时间间隔："),
                value = period,
            };
            intervalField.RegisterValueChangedCallback((e) =>
            {
                period = e.newValue;
            });
            root.Add(intervalField);
            IntegerField digitsField = new IntegerField()
            {
                label = _("验证码位数："),
                value = digits,
            };
            digitsField.RegisterValueChangedCallback((e) =>
            {
                digits = e.newValue;
            });
            root.Add(digitsField);
            IntegerField toleranceField = new IntegerField()
            {
                label = _("容错倍数："),
                value = tolerance,
            };
            toleranceField.RegisterValueChangedCallback((e) =>
            {
                tolerance = e.newValue;
            });
            root.Add(toleranceField);
            Toggle autoCreateKeyField = new Toggle(_("自动创建密钥"))
            {
                value = autoCreateKey,
            };
            root.Add(autoCreateKeyField);
            keyField = new TextField()
            {
                label = _("密钥："),
                value = key,
            };
            root.Add(keyField);
            autoCreateKeyField.RegisterValueChangedCallback((e) =>
            {
                autoCreateKey = e.newValue;
                keyField.isReadOnly = autoCreateKey;
            });
            secretField = new TextField
            {
                label = _("编译后密钥："),
                value = secret,
                isReadOnly = true
            };
            // keyField.isPasswordField = true;
            keyField.RegisterValueChangedCallback((e) =>
            {
                key = e.newValue;
                secretField.value = secret;
            });
            root.Add(secretField);
            Button resetKeyButton = new Button(() =>
            {
                Reset();
            })
            {
                text = _("重置"),
            };
            root.Add(resetKeyButton);
            Button generateKeyButton = new Button(() =>
            {
                Generate();
            })
            {
                text = _("生成密钥二维码"),
            };
            root.Add(generateKeyButton);
            qrCodeImage = new UnityEngine.UIElements.Image();
            qrCodeImage.style.height = qrCodeImage.style.width = root.resolvedStyle.width;
            root.Add(qrCodeImage);
            verifyField = new TextField()
            {
                label = _("当前动态密码："),
            };
            root.Add(verifyField);
            verifyButton = new Button(() =>
            {
                Verify(verifyField.value);
            })
            {
                text = _("验证动态密码"),
            };
            verifyButton.SetEnabled(false);
            root.Add(verifyButton);
            copyKeyButton = new Button(() =>
            {
                EditorGUIUtility.systemCopyBuffer = secret;
                EditorUtility.DisplayDialog(_("复制成功"), _("密钥已复制到剪贴板"), _("确定"));
            })
            {
                text = _("复制密钥"),
            };
            copyKeyButton.SetEnabled(false);
            root.Add(copyKeyButton);
            // createTOTPKeypadButton = new Button(() =>
            // {
            //     CreateTOTPKeypad();
            // })
            // {
            //     text = _("创建密码键盘至场景"),
            // };
            // createTOTPKeypadButton.SetEnabled(false);
            // root.Add(createTOTPKeypadButton);
            root.Add(CreateTips(_("生成密钥后会生成二维码，可以使用 Microsoft Authenticator 或 Google Authenticator 扫描二维码添加密钥。\n" +
            "生成后请点击“复制密钥”，随后在 UdonSharp 的 Totp 脚本下粘贴密钥，如改动了其它配置，请在 UdonSharp 的 Totp 配置好对应配置。\n" +
            "如果不想使用二维码，可以手动输入密钥。\n" +
            "请妥善保管好密钥。")));
            root.Add(CreateTips(_("代码参考了以下仓库：\n" +
            "Gorialis - Udon-HashLib https://github.com/Gorialis/vrchat-udon-hashlib\n" +
            "Michael Jahn - ZXing.Net https://github.com/micjahn/ZXing.Net")));
            return root;
        }
        public static void OnGUI()
        {
            qrCodeImage.style.height = qrCodeImage.style.width = root.resolvedStyle.width;
        }
        public static void OnDestroy()
        {
            if (qrCodeImage.image != null)
            {
                UnityEngine.Object.DestroyImmediate(qrCodeImage.image);
            }
        }
        static void Generate()
        {
            // 销毁 qrCodeImage.image
            if (qrCodeImage.image != null)
            {
                UnityEngine.Object.DestroyImmediate(qrCodeImage.image);
            }
            qrCodeImage.image = null;
            verifyButton.SetEnabled(false);
            copyKeyButton.SetEnabled(false);
            if (autoCreateKey || string.IsNullOrEmpty(key))
            {
                key = GeneratePassword(20, true, true);
            }
            if (key.Length != 20)
            {
                EditorUtility.DisplayDialog(_("错误"), _("密钥长度必须为20位"), _("确定"));
                return;
            }
            string _issuer = string.IsNullOrEmpty(issuer) ? ("TOTP%20" + secret.Substring(0, 16)) : UnityWebRequest.EscapeURL(issuer);
            // qrCodeImage.image = GenerateQR("otpauth://totp/VRChat%20Udon%20TOTP?algorithm=SHA1&digits=" + digits + "&period=" + period + "&issuer=" + _issuer + "&secret=" + Base32Encode(key));
            qrCodeImage.image = GenerateQR(string.Format("otpauth://totp/{0}?algorithm={1}&digits={2}&period={3}&issuer={4}&secret={5}", "VRChat%20Udon%20TOTP", algorithm, digits, period, _issuer, Base32Encode(key)));
            verifyButton.SetEnabled(true);
            keyField.value = key;
            secretField.value = secret;
        }
        static void Verify(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                EditorUtility.DisplayDialog(_("错误"), _("请输入验证码"), _("确定"));
                return;
            }
            if (code.Length != digits)
            {
                EditorUtility.DisplayDialog(_("错误"), string.Format(_("验证码长度必须为{0}位"), digits.ToString()), _("确定"));
                return;
            }
            TOTP.key = key;
            TOTP.period = period;
            TOTP.digits = digits;
            TOTP.tolerance = tolerance;
            switch (algorithm)
            {
                case "SHA1":
                {
                    TOTP.mode = 0;
                }
                break;
                case "SHA256":
                {
                    TOTP.mode = 1;
                }
                break;
                case "SHA512":
                {
                    TOTP.mode = 2;
                }
                break;
                default:
                {
                    TOTP.mode = 0;
                }
                break;
            }
            if (TOTP.VerifyTotp(code))
            {
                EditorUtility.DisplayDialog(_("验证成功"), _("验证码正确"), _("确定"));
                copyKeyButton.SetEnabled(true);
            }
            else
            {
                EditorUtility.DisplayDialog(_("验证失败"), _("验证码错误"), _("确定"));
            }
        }
        static void Reset()
        {
            key = "";
            keyField.value = "";
            secretField.value = "";
            if (qrCodeImage.image != null)
            {
                UnityEngine.Object.DestroyImmediate(qrCodeImage.image);
            }
            qrCodeImage.image = null;
            verifyField.value = "";
            verifyButton.SetEnabled(false);
            copyKeyButton.SetEnabled(false);
        }
        // static void CreateTOTPKeypad()
        // {
        //     string prefabpath = "Assets/853Lab/UdonSharp/UdonTotp/TotpKeypad/TotpKeypad.prefab";
        //     if (!AssetDatabase.LoadAssetAtPath(prefabpath, typeof(GameObject)))
        //     {
        //         EditorUtility.DisplayDialog(_("错误"), _("找不到 TotpKeypad.prefab"), _("确定"));
        //         return;
        //     }
        //     GameObject keypad = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath(prefabpath, typeof(GameObject))) as GameObject;
        //     // 获取 secret 的后6位
        //     keypad.name = "TotpKeypad_" + secret.Substring(secret.Length - 6, 6);
        //     keypad.transform.position = new Vector3(0, 0, 0);
        //     keypad.transform.rotation = Quaternion.identity;
        //     keypad.transform.localScale = new Vector3(1, 1, 1);
        //     // 设置 secret
        //     keypad.GetComponentInChildren<Sonic853.Udon.UdonKeypad.TotpKeypad>().secret = secret;
        // }
        public static Label CreateLabel(string text, string className)
        {
            Label label = new Label(text);
            label.AddToClassList(className);
            return label;
        }
        public static Label CreateTips(string tips)
        {
            return CreateLabel(tips, "tips");
        }
        static string _(string text)
        {
            return MainWindow.poReader._(text);
        }
    }
}
