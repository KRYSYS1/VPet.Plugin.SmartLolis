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
            ["window_subtitle"] = "Команды можно писать и говорить не только точной фразой. Smart Lolis обычно понимает и более свободные формулировки.",
            ["general_title"] = "ОБЩЕЕ",
            ["general_hint"] = "Работает и с клавиатуры, и через голосовой ввод.",
            ["general_body"] = "Можно писать коротко, можно по-человечески: `займись учебой`, `может, поучишься?`, `иди поработай немного`. Префиксы `/`, `!`, `cmd`, `command`, `команда`, `приказ` тоже поддерживаются, но они не обязательны.",
            ["activity_title"] = "АКТИВНОСТИ И ЗАНЯТИЯ",
            ["activity_line1"] = "займись учебой",
            ["activity_line2"] = "может, поработаешь?",
            ["activity_line3"] = "иди поиграй немного",
            ["activity_line4"] = "остановись / стоп / отмена",
            ["activity_hint"] = "Если у VPet несколько подходящих вариантов, Smart Lolis сама уточнит, что именно выбрать.",
            ["example_label"] = "Как это выглядит:",
            ["activity_example1"] = "Ты: займись учебой",
            ["activity_example2"] = "Smart Lolis: Какую именно учебу выбрать?",
            ["activity_example3"] = "Ты: рисование, математика, языки",
            ["item_title"] = "ПОКУПКИ И ПРЕДМЕТЫ",
            ["item_line1"] = "купи еду / поешь / возьми что-нибудь поесть",
            ["item_line2"] = "купи воды / попей / возьми что-нибудь попить",
            ["item_line3"] = "дай лекарство / полечись",
            ["item_line4"] = "купи подарок / возьми подарок",
            ["item_line5"] = "купи воду / купи сок / купи суп",
            ["item_hint"] = "Можно назвать и категорию, и конкретный предмет. Если вариантов несколько, Smart Lolis предложит выбрать.",
            ["examples_label"] = "Примеры:",
            ["item_example1"] = "Ты: купи что-нибудь попить",
            ["item_example2"] = "Smart Lolis: Что именно взять?",
            ["item_example3"] = "Ты: воду / сок / чай",
            ["item_example4"] = "Ты: купи что-нибудь вкусное",
            ["item_example5"] = "Ты: дай лекарство или купи подарок",
            ["voice_title"] = "КАК ЭТО РАБОТАЕТ ГОЛОСОМ",
            ["voice_line1"] = "1. Нажми кнопку голосового ввода.",
            ["voice_line2"] = "2. Скажи обычной фразой: `займись учебой` или `купи воды`.",
            ["voice_line3"] = "3. Если Smart Lolis уточнит, просто назови вариант: `рисование`, `суп`, `чай`.",
            ["voice_line4"] = "4. Для отмены можно сказать: `стоп`, `отмена`, `не надо`, `не важно`.",
            ["more_examples_title"] = "ЕЩЕ СВОБОДНЫЕ ПРИМЕРЫ",
            ["more_example1"] = "`может, поучишься немного`",
            ["more_example2"] = "`иди поработай`",
            ["more_example3"] = "`давай купим воды`",
            ["more_example4"] = "`купи что-нибудь поесть`",
            ["more_example5"] = "`ладно, отмена`",
            ["close"] = "Закрыть"
        };

        private static Dictionary<string, string> BuildEnglishText() => new()
        {
            ["window_title"] = "Smart Lolis Command Info",
            ["window_title_header"] = "COMMAND INFO",
            ["window_subtitle"] = "You do not need to use one exact phrase. Smart Lolis can usually understand more natural command wording too.",
            ["general_title"] = "GENERAL",
            ["general_hint"] = "Works with typed input and voice input.",
            ["general_body"] = "You can be short or natural: `start studying`, `maybe go study`, `go work for a bit`. Prefixes such as `/`, `!`, `cmd`, and `command` are supported, but not required.",
            ["activity_title"] = "ACTIVITIES AND TASKS",
            ["activity_line1"] = "start studying",
            ["activity_line2"] = "maybe go work",
            ["activity_line3"] = "go play for a while",
            ["activity_line4"] = "stop / cancel / never mind",
            ["activity_hint"] = "If more than one matching option exists, Smart Lolis will ask which one you want.",
            ["example_label"] = "How it looks:",
            ["activity_example1"] = "You: start studying",
            ["activity_example2"] = "Smart Lolis: Which study do you want?",
            ["activity_example3"] = "You: drawing, math, language",
            ["item_title"] = "SHOPPING AND ITEMS",
            ["item_line1"] = "buy food / get something to eat / pick some food",
            ["item_line2"] = "buy water / get a drink / pick something to drink",
            ["item_line3"] = "give medicine / heal up",
            ["item_line4"] = "buy a gift / get a present",
            ["item_line5"] = "buy water / buy juice / buy soup",
            ["item_hint"] = "You can name a category or a specific item. If there are several choices, Smart Lolis will ask for clarification.",
            ["examples_label"] = "Examples:",
            ["item_example1"] = "You: buy something to drink",
            ["item_example2"] = "Smart Lolis: What should I get?",
            ["item_example3"] = "You: water / juice / tea",
            ["item_example4"] = "You: buy something tasty",
            ["item_example5"] = "You: give medicine or buy a gift",
            ["voice_title"] = "VOICE INPUT EXAMPLE",
            ["voice_line1"] = "1. Press the voice input button.",
            ["voice_line2"] = "2. Say a normal phrase like `start studying` or `buy some water`.",
            ["voice_line3"] = "3. If she asks for details, just say the option: `drawing`, `soup`, `tea`.",
            ["voice_line4"] = "4. To cancel, say `stop`, `cancel`, `never mind`, or `doesn't matter`.",
            ["more_examples_title"] = "MORE NATURAL EXAMPLES",
            ["more_example1"] = "`maybe go study for a bit`",
            ["more_example2"] = "`go work`",
            ["more_example3"] = "`let's buy some water`",
            ["more_example4"] = "`buy something to eat`",
            ["more_example5"] = "`okay, cancel that`",
            ["close"] = "Close"
        };

        private static Dictionary<string, string> BuildChineseText() => new()
        {
            ["window_title"] = "Smart Lolis 命令说明",
            ["window_title_header"] = "命令说明",
            ["window_subtitle"] = "不需要只用一种固定说法。Smart Lolis 通常也能理解更自然、更口语化的命令。",
            ["general_title"] = "通用",
            ["general_hint"] = "文字输入和语音输入都可以这样用。",
            ["general_body"] = "你可以简短地下命令，也可以用更自然的说法，比如：`去学习`、`要不要去学习一下`、`去工作一会儿吧`。前缀 `/`、`!`、`cmd`、`command` 也支持，但不是必须的。",
            ["activity_title"] = "活动与任务",
            ["activity_line1"] = "去学习",
            ["activity_line2"] = "去工作一下吧",
            ["activity_line3"] = "去玩一会儿",
            ["activity_line4"] = "停止 / 取消 / 算了",
            ["activity_hint"] = "如果有多个匹配选项，Smart Lolis 会继续问你具体想选哪一个。",
            ["example_label"] = "效果示例：",
            ["activity_example1"] = "你：去学习",
            ["activity_example2"] = "Smart Lolis：你想学哪一个？",
            ["activity_example3"] = "你：画画、数学、语言",
            ["item_title"] = "购物与物品",
            ["item_line1"] = "吃点东西 / 买点吃的 / 弄点吃的",
            ["item_line2"] = "喝点水 / 买点喝的 / 买点水",
            ["item_line3"] = "吃药 / 治疗一下",
            ["item_line4"] = "买礼物 / 送个礼物",
            ["item_line5"] = "买水 / 买果汁 / 买汤",
            ["item_hint"] = "你可以说类别，也可以直接说具体物品。如果选项很多，Smart Lolis 会继续追问。",
            ["examples_label"] = "示例：",
            ["item_example1"] = "你：买点喝的",
            ["item_example2"] = "Smart Lolis：具体买什么？",
            ["item_example3"] = "你：水 / 果汁 / 茶",
            ["item_example4"] = "你：买点好吃的",
            ["item_example5"] = "你：吃药 或 买个礼物",
            ["voice_title"] = "语音输入示例",
            ["voice_line1"] = "1. 点击语音输入按钮。",
            ["voice_line2"] = "2. 直接说自然一点的话，比如：`去学习` 或 `买点水`。",
            ["voice_line3"] = "3. 如果有追问，就直接补充具体选项：`画画`、`汤`、`茶`。",
            ["voice_line4"] = "4. 如果想取消，可以说：`停止`、`取消`、`不用了`、`算了`。",
            ["more_examples_title"] = "更多自然说法",
            ["more_example1"] = "`要不要去学习一下`",
            ["more_example2"] = "`去工作一会儿吧`",
            ["more_example3"] = "`买点水吧`",
            ["more_example4"] = "`买点吃的`",
            ["more_example5"] = "`算了，取消吧`",
            ["close"] = "关闭"
        };
    }
}
