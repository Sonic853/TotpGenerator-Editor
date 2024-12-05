using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ZXing;
using ZXing.QrCode;


namespace Sonic853.TotpGen
{
    public class TotpGenerator
    {
        private static readonly string path = Path.Combine("Packages", "com.sonic853.totpgenerator");
        private static readonly string s_StyleSheetPath = Path.Combine(path, "StyleSheets", "TotpGenerator.uss");
        private static readonly string base_32_chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        private static readonly string[] Base32Chars = new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "2", "3", "4", "5", "6", "7" };
        private static readonly string lower = "abcdefghijklmnopqrstuvwxyz";
        private static readonly string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static readonly string numbers = "0123456789";
        private static readonly string special = @"!@#$%^&*()-_ []{}<>~`+=,.;:/?|";
        static Models.TotpSettings totpSettings = Models.TotpSettings.instance;
        static VisualElement root;
        static TextField labelField;
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
            var writer = new BarcodeWriter<Color32[]>
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Height = height,
                    Width = width
                },
                Renderer = new Color32Renderer()
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
            totpSettings = Models.TotpSettings.instance;
            labelField = new TextField(_("标签"))
            {
                value = totpSettings.label
            };
            labelField.RegisterValueChangedCallback((e) =>
            {
                totpSettings.label = e.newValue;
                totpSettings.Save();
            });
            root.Add(labelField);
            issuerField = new TextField(_("名称"))
            {
                value = totpSettings.issuer
            };
            issuerField.RegisterValueChangedCallback((e) =>
            {
                totpSettings.issuer = e.newValue;
                totpSettings.Save();
            });
            root.Add(issuerField);
            PopupField<string> tOTPTypeField = new PopupField<string>(new List<string> { "SHA1", "SHA256", "SHA512" }, totpSettings.algorithm)
            {
                label = _("TOTP 算法："),
            };
            tOTPTypeField.RegisterValueChangedCallback((e) =>
            {
                totpSettings.algorithm = e.newValue;
                totpSettings.Save();
            });
            // tOTPTypeField 只读
            // tOTPTypeField.SetEnabled(false);
            root.Add(tOTPTypeField);
            IntegerField intervalField = new IntegerField()
            {
                label = _("时间间隔："),
                value = totpSettings.period,
            };
            intervalField.RegisterValueChangedCallback((e) =>
            {
                totpSettings.period = e.newValue;
                totpSettings.Save();
            });
            root.Add(intervalField);
            IntegerField digitsField = new IntegerField()
            {
                label = _("验证码位数："),
                value = totpSettings.digits,
            };
            digitsField.RegisterValueChangedCallback((e) =>
            {
                totpSettings.digits = e.newValue;
                totpSettings.Save();
            });
            root.Add(digitsField);
            IntegerField toleranceField = new IntegerField()
            {
                label = _("容错倍数："),
                value = totpSettings.tolerance,
            };
            toleranceField.RegisterValueChangedCallback((e) =>
            {
                totpSettings.tolerance = e.newValue;
                totpSettings.Save();
            });
            root.Add(toleranceField);
            Toggle autoCreateKeyField = new Toggle(_("自动创建密钥"))
            {
                value = totpSettings.autoCreateKey,
            };
            root.Add(autoCreateKeyField);
            keyField = new TextField()
            {
                label = _("密钥："),
                value = totpSettings.key,
            };
            root.Add(keyField);
            autoCreateKeyField.RegisterValueChangedCallback((e) =>
            {
                totpSettings.autoCreateKey = e.newValue;
                keyField.isReadOnly = totpSettings.autoCreateKey;
                totpSettings.Save();
            });
            secretField = new TextField
            {
                label = _("编译后密钥："),
                value = totpSettings.secret,
                isReadOnly = true
            };
            // keyField.isPasswordField = true;
            keyField.RegisterValueChangedCallback((e) =>
            {
                totpSettings.key = e.newValue;
                secretField.value = totpSettings.secret;
                // totpSettings.Save();
            });
            // 当 keyField 失去焦点时保存
            keyField.RegisterCallback<BlurEvent>((e) =>
            {
                totpSettings.Save();
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
                totpSettings.Save();
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
                EditorGUIUtility.systemCopyBuffer = totpSettings.secret;
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
            totpSettings = Models.TotpSettings.instance;
            if (totpSettings.autoCreateKey || string.IsNullOrEmpty(totpSettings.key))
            {
                totpSettings.key = GeneratePassword(20, true, true);
            }
            if (totpSettings.key.Length != 20)
            {
                EditorUtility.DisplayDialog(_("错误"), _("密钥长度必须为20位"), _("确定"));
                return;
            }
            qrCodeImage.image = GenerateQR(totpSettings.GetUrl());
            verifyButton.SetEnabled(true);
            keyField.SetValueWithoutNotify(totpSettings.key);
            secretField.SetValueWithoutNotify(totpSettings.secret);
        }
        static void Verify(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                EditorUtility.DisplayDialog(_("错误"), _("请输入验证码"), _("确定"));
                return;
            }
            if (code.Length != totpSettings.digits)
            {
                EditorUtility.DisplayDialog(_("错误"), string.Format(_("验证码长度必须为{0}位"), totpSettings.digits.ToString()), _("确定"));
                return;
            }
            TOTP.key = totpSettings.key;
            TOTP.period = totpSettings.period;
            TOTP.digits = totpSettings.digits;
            TOTP.tolerance = totpSettings.tolerance;
            switch (totpSettings.algorithm)
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
            totpSettings = Models.TotpSettings.instance;
            totpSettings.key = "";
            keyField.SetValueWithoutNotify("");
            secretField.SetValueWithoutNotify("");
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
