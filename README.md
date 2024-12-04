# VRChat UdonSharp Dynamic Password Based on TOTP

## 中文

一种基于时间的动态密码锁实现（TOTP）
可以配合Microsoft Authenticator使用

### 如何使用？

1. 安装“Sonic853.TotpGen.unitypackage”
2. 安装插件后，请在Unity编辑器点击“Window”→“853Lab”→“Totp Generator”打开生成窗口。
3. 点击“生成密钥二维码”
4. 记录密钥
5. 打开手机的“Microsoft Authenticator”扫描二维码
6. 将扫描出来的动态密码输入到“当前动态密码”里，点击“验证动态密码”
7. 提示“验证码正确”则进行下一步
8. 将示例“TotpKeypad”放入世界
9. 为“TotpKeypad”里的“TOTP”脚本配置编译后密钥（secret）、时间间隔、验证码位数、容错倍数
10. 全部完成！

### 使用要求

请勿将此软件分发到任何网站下载。
你可以将此脚本放进你个人的VRChat World使用（包括你个人的公开VRChat World）。
作者不对任何内容创作负责。

## ENG

A Time-Based Dynamic Password Lock Implementation (TOTP)
Can be used with Microsoft Authenticator

### how to use?

1. Install "Sonic853.TotpGen.unitypackage"
2. After installing the plugin, please click "Window" → "853Lab" → "Totp Generator" in the Unity editor to open the generation window.
3. Click "Generate Key QR Code"
4. Record the key
5. Open the "Microsoft Authenticator" of the mobile phone and scan the QR code
6. Enter the scanned dynamic password into "Current dynamic password" and click "Verify dynamic password"
7. Prompt "Verification code is correct" and proceed to the next step
8. Put the example "TotpKeypad" into the world
9. Configure the secret, period, digits, and tolerance for the "TOTP" script in "TotpKeypad"
10. All done!

### Requirements

Do not distribute this software to any website for download.
You can use this script in your personal VRChat World (including your personal public VRChat World).
The author is not responsible for any content creation.

## License restrictions 许可限制

Individuals or companies in Chinese Mainland (except Hong Kong China, Macau China, and Taiwan Province of China) are prohibited from using this LGPL license and using this repository and products if the following circumstances apply:

1. The map/world is not created in an individual capacity
2. The map/world involves more than two participants in its creation (not including two participants)
3. Any individual user or company explicitly prohibited by Sonic853

中国大陆地区（中国香港、中国澳门和中国台湾除外）的个人或公司如含有以下任一情况禁止使用该 LGPL 许可并禁止使用此储存库以及商品（包括此储存库以及商品的任一文件）：

1. 不以个人名义创建的地图/世界
2. 地图/世界参与制作人数超过 2 人以上（不含 2 人）
3. 由 Sonic853 明确禁止的个人用户、公司

If you wish to obtain authorization to use this repository and its products, please contact the author Sonic853 (sonic853@qq.com) or manually acquire authorization via [爱发电](https://afdian.com/a/Sonic853).

如需获得使用此储存库以及商品的授权，请联系作者 Sonic853 (sonic853@qq.com) 获取授权或访问 [爱发电](https://afdian.com/a/Sonic853) 手动获取授权。