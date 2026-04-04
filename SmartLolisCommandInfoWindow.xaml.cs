using System.Collections.Generic;
using System.Windows;

namespace VPet.Plugin.SmartLolis
{
    public partial class SmartLolisCommandInfoWindow : Window
    {
        public SmartLolisCommandInfoWindow(string language = "ru")
        {
            InitializeComponent();
            ApplyLocalization(language);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ApplyLocalization(string language)
        {
            Dictionary<string, string> text = language switch
            {
                "en" => BuildEnglishText(),
                "zh" => BuildChineseText(),
                _ => BuildRussianText()
            };

            Title = text["window_title"];
            txtWindowTitle.Text = text["window_title_header"];
            txtWindowSubtitle.Text = text["window_subtitle"];
            txtGeneralTitle.Text = text["general_title"];
            txtGeneralHint.Text = text["general_hint"];
            txtGeneralBody.Text = text["general_body"];
            txtActivityTitle.Text = text["activity_title"];
            txtActivityLine1.Text = text["activity_line1"];
            txtActivityLine2.Text = text["activity_line2"];
            txtActivityLine3.Text = text["activity_line3"];
            txtActivityLine4.Text = text["activity_line4"];
            txtActivityHint.Text = text["activity_hint"];
            txtActivityExampleTitle.Text = text["example_label"];
            txtActivityExample1.Text = text["activity_example1"];
            txtActivityExample2.Text = text["activity_example2"];
            txtActivityExample3.Text = text["activity_example3"];
            txtItemTitle.Text = text["item_title"];
            txtItemLine1.Text = text["item_line1"];
            txtItemLine2.Text = text["item_line2"];
            txtItemLine3.Text = text["item_line3"];
            txtItemLine4.Text = text["item_line4"];
            txtItemLine5.Text = text["item_line5"];
            txtItemHint.Text = text["item_hint"];
            txtItemExampleTitle.Text = text["examples_label"];
            txtItemExample1.Text = text["item_example1"];
            txtItemExample2.Text = text["item_example2"];
            txtItemExample3.Text = text["item_example3"];
            txtItemExample4.Text = text["item_example4"];
            txtItemExample5.Text = text["item_example5"];
            txtVoiceExampleTitle.Text = text["voice_title"];
            txtVoiceExample1.Text = text["voice_line1"];
            txtVoiceExample2.Text = text["voice_line2"];
            txtVoiceExample3.Text = text["voice_line3"];
            txtVoiceExample4.Text = text["voice_line4"];
            txtMoreExamplesTitle.Text = text["more_examples_title"];
            txtMoreExample1.Text = text["more_example1"];
            txtMoreExample2.Text = text["more_example2"];
            txtMoreExample3.Text = text["more_example3"];
            txtMoreExample4.Text = text["more_example4"];
            txtMoreExample5.Text = text["more_example5"];
            btnClose.Content = text["close"];
        }

        private static Dictionary<string, string> BuildRussianText() => new()
        {
            ["window_title"] = "Инфо по командам Smart Lolis",
            ["window_title_header"] = "ИНФО ПО КОМАНДАМ",
            ["window_subtitle"] = "Используй эти фразы в чате или голосом, чтобы управлять Smart Lolis.",
            ["general_title"] = "ОБЩЕЕ",
            ["general_hint"] = "Команды работают и в текстовом, и в голосовом вводе.",
            ["general_body"] = "Можно писать напрямую: `займись учебой`, или использовать префиксы `/`, `!`, `cmd`, `command`, `команда`, `приказ`.",
            ["activity_title"] = "КОМАНДЫ АКТИВНОСТЕЙ",
            ["activity_line1"] = "/займись учебой",
            ["activity_line2"] = "займись работой",
            ["activity_line3"] = "поиграй",
            ["activity_line4"] = "стоп",
            ["activity_hint"] = "Если вариантов несколько, Smart Lolis уточнит, что именно выбрать.",
            ["example_label"] = "Пример:",
            ["activity_example1"] = "Ты: займись учебой",
            ["activity_example2"] = "Smart Lolis: Какую именно учебу выбрать?",
            ["activity_example3"] = "Ты: рисование",
            ["item_title"] = "КОМАНДЫ ПРЕДМЕТОВ",
            ["item_line1"] = "покорми",
            ["item_line2"] = "напои",
            ["item_line3"] = "дай лекарство",
            ["item_line4"] = "купи подарок",
            ["item_line5"] = "/купи вода",
            ["item_hint"] = "Можно назвать конкретный предмет, когда Smart Lolis попросит уточнение.",
            ["examples_label"] = "Примеры:",
            ["item_example1"] = "Ты: покорми",
            ["item_example2"] = "Smart Lolis: Какую еду выбрать?",
            ["item_example3"] = "Ты: суши",
            ["item_example4"] = "Ты: дай лекарство",
            ["item_example5"] = "Ты: купи подарок",
            ["voice_title"] = "ПРИМЕР ГОЛОСОМ",
            ["voice_line1"] = "1. Нажми кнопку голосового ввода.",
            ["voice_line2"] = "2. Скажи: `займись учебой`.",
            ["voice_line3"] = "3. Если она спросит, назови конкретный вариант: `рисование`.",
            ["voice_line4"] = "4. Для предметов так же: `покорми`, потом `суп` или другое название.",
            ["more_examples_title"] = "ЕЩЁ ПРИМЕРЫ",
            ["more_example1"] = "`/займись работой`",
            ["more_example2"] = "`!стоп`",
            ["more_example3"] = "`команда поиграй`",
            ["more_example4"] = "`приказ напои`",
            ["more_example5"] = "`/купи подарок`",
            ["close"] = "Закрыть"
        };

        private static Dictionary<string, string> BuildEnglishText() => new()
        {
            ["window_title"] = "Smart Lolis Command Info",
            ["window_title_header"] = "COMMAND INFO",
            ["window_subtitle"] = "Use these phrases in chat or by voice to control Smart Lolis.",
            ["general_title"] = "GENERAL",
            ["general_hint"] = "These work as typed commands and as voice commands.",
            ["general_body"] = "You can use direct phrases like `start studying`, or prefixes such as `/`, `!`, `cmd`, `command`.",
            ["activity_title"] = "ACTIVITY COMMANDS",
            ["activity_line1"] = "/start studying",
            ["activity_line2"] = "start working",
            ["activity_line3"] = "go play",
            ["activity_line4"] = "stop",
            ["activity_hint"] = "If several variants exist, Smart Lolis will ask which one you mean.",
            ["example_label"] = "Example:",
            ["activity_example1"] = "You: start studying",
            ["activity_example2"] = "Smart Lolis: Which study do you want?",
            ["activity_example3"] = "You: drawing",
            ["item_title"] = "ITEM COMMANDS",
            ["item_line1"] = "feed yourself",
            ["item_line2"] = "drink water",
            ["item_line3"] = "take medicine",
            ["item_line4"] = "buy a gift",
            ["item_line5"] = "/buy water",
            ["item_hint"] = "You can also say a specific item name when Smart Lolis asks for clarification.",
            ["examples_label"] = "Examples:",
            ["item_example1"] = "You: feed yourself",
            ["item_example2"] = "Smart Lolis: Which food do you want?",
            ["item_example3"] = "You: sushi",
            ["item_example4"] = "You: take medicine",
            ["item_example5"] = "You: buy a gift",
            ["voice_title"] = "VOICE EXAMPLE",
            ["voice_line1"] = "1. Press the voice input button.",
            ["voice_line2"] = "2. Say: `start studying`.",
            ["voice_line3"] = "3. If she asks, name a specific option like `drawing`.",
            ["voice_line4"] = "4. For items it works the same way: `feed yourself`, then `soup` or another item name.",
            ["more_examples_title"] = "MORE EXAMPLES",
            ["more_example1"] = "`/start working`",
            ["more_example2"] = "`!stop`",
            ["more_example3"] = "`command go play`",
            ["more_example4"] = "`command drink water`",
            ["more_example5"] = "`/buy a gift`",
            ["close"] = "Close"
        };

        private static Dictionary<string, string> BuildChineseText() => new()
        {
            ["window_title"] = "Smart Lolis 命令说明",
            ["window_title_header"] = "命令说明",
            ["window_subtitle"] = "你可以在聊天或语音里使用这些短语来控制 Smart Lolis。",
            ["general_title"] = "通用",
            ["general_hint"] = "这些命令既支持文本输入，也支持语音输入。",
            ["general_body"] = "你可以直接输入：`去学习`，也可以使用前缀 `/`、`!`、`cmd`、`command`。",
            ["activity_title"] = "活动命令",
            ["activity_line1"] = "/去学习",
            ["activity_line2"] = "去工作",
            ["activity_line3"] = "去玩",
            ["activity_line4"] = "停止",
            ["activity_hint"] = "如果有多个选项，Smart Lolis 会继续问你具体要哪个。",
            ["example_label"] = "示例：",
            ["activity_example1"] = "你：去学习",
            ["activity_example2"] = "Smart Lolis：你想学哪一个？",
            ["activity_example3"] = "你：画画",
            ["item_title"] = "物品命令",
            ["item_line1"] = "喂她吃东西",
            ["item_line2"] = "让她喝水",
            ["item_line3"] = "给她吃药",
            ["item_line4"] = "买礼物",
            ["item_line5"] = "/买水",
            ["item_hint"] = "当 Smart Lolis 追问时，你也可以直接说出具体物品名。",
            ["examples_label"] = "示例：",
            ["item_example1"] = "你：喂她吃东西",
            ["item_example2"] = "Smart Lolis：你想选哪种食物？",
            ["item_example3"] = "你：寿司",
            ["item_example4"] = "你：给她吃药",
            ["item_example5"] = "你：买礼物",
            ["voice_title"] = "语音示例",
            ["voice_line1"] = "1. 点击语音输入按钮。",
            ["voice_line2"] = "2. 说：`去学习`。",
            ["voice_line3"] = "3. 如果她追问，就说具体选项，例如：`画画`。",
            ["voice_line4"] = "4. 物品也是一样：先说 `喂她吃东西`，再说 `汤` 或其他物品名。",
            ["more_examples_title"] = "更多示例",
            ["more_example1"] = "`/去工作`",
            ["more_example2"] = "`!停止`",
            ["more_example3"] = "`command 去玩`",
            ["more_example4"] = "`command 让她喝水`",
            ["more_example5"] = "`/买礼物`",
            ["close"] = "关闭"
        };
    }
}
