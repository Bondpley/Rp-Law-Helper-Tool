using System.Drawing;
using System.Net.Http;
using System.Text;

namespace Overlay;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new OverlayForm());
    }
}

internal sealed record Rule(string Group, string Category, string Article, string Title, string Info);

internal static class NativeMethods
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr word, IntPtr data);

    public static void BeginWindowDrag(IntPtr handle)
    {
        ReleaseCapture();
        SendMessage(handle, 0xA1, new IntPtr(2), IntPtr.Zero);
    }
}

internal sealed class OverlayForm : Form
{
    private const string SheetId = "1pWVIYj4u3d8eFn_NzM99pd49G7ldt7cXUzJkwdTEeZk";
    private static readonly Color TransparentColor = Color.Magenta;
    private readonly List<Rule> _rules = [];
    private readonly FlowLayoutPanel _rulesPanel;
    private readonly TextBox _searchBox;
    private string _group = "КУпАП";
    private bool _isCompact;
    private bool _advocateUnlocked;

    public OverlayForm()
    {
        Text = "Overlay";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(450, 680);
        TopMost = true;
        BackColor = TransparentColor;
        TransparencyKey = TransparentColor;

        var surface = new Panel
        {
            BackColor = Color.FromArgb(255, 35, 35, 35),
            Location = Point.Empty,
            Size = ClientSize,
            Padding = new Padding(8)
        };
        Controls.Add(surface);

        var header = new Panel
        {
            BackColor = Color.FromArgb(255, 30, 31, 35),
            Location = Point.Empty,
            Size = new Size(surface.ClientSize.Width, 35),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        surface.Controls.Add(header);

        var title = new Label
        {
            Text = "Помічник Киэвського",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 11),
            AutoSize = true,
            Location = new Point(10, 8)
        };
        title.MouseDown += (_, _) => NativeMethods.BeginWindowDrag(Handle);
        header.Controls.Add(title);
        header.MouseDown += (_, _) => NativeMethods.BeginWindowDrag(Handle);

        var closeButton = CreateButton("X", 32);
        closeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        closeButton.Location = new Point(surface.ClientSize.Width - closeButton.Width - 8, 5);
        closeButton.Click += (_, _) => Close();
        header.Controls.Add(closeButton);

        var hideButton = CreateButton("-", 32);
        hideButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        hideButton.Location = new Point(closeButton.Left - hideButton.Width - 4, 5);
        header.Controls.Add(hideButton);

        var creditsButton = CreateButton("і", 32);
        creditsButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        creditsButton.Location = new Point(hideButton.Left - creditsButton.Width - 4, 5);
        creditsButton.Click += (_, _) => MessageBox.Show(
            "Автор програми: bondpley\nРозробник: Abdul\n\nДжерело таблиці порушень: Помічник by Sarmat and Freddy\n\nПосиання на таблицю: https://docs.google.com/spreadsheets/d/1pWVIYj4u3d8eFn_NzM99pd49G7ldt7cXUzJkwdTEeZk/edit",
            "Credits",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
        header.Controls.Add(creditsButton);

        var kupapButton = CreateButton("КУпАП", 72);
        kupapButton.Location = new Point(10, 38);
        kupapButton.Click += (_, _) => SelectGroup("КУпАП");
        surface.Controls.Add(kupapButton);

        var kkuButton = CreateButton("ККУ", 52);
        kkuButton.Location = new Point(88, 38);
        kkuButton.Click += (_, _) => SelectGroup("ККУ");
        surface.Controls.Add(kkuButton);

        var advocateButton = CreateButton("Адвокат", 80);
        advocateButton.Location = new Point(144, 38);
        advocateButton.Click += (_, _) => OpenAdvocateGuide();
        surface.Controls.Add(advocateButton);

        _searchBox = new TextBox
        {
            Location = new Point(10, 70),
            Width = 304,
            Height = 26,
            BackColor = Color.FromArgb(45, 45, 48),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10)
        };
        _searchBox.TextChanged += (_, _) => RefreshRules();
        surface.Controls.Add(_searchBox);

        var searchLabel = new Label
        {
            Text = "Пошук",
            ForeColor = Color.Gainsboro,
            AutoSize = true,
            Location = new Point(320, 74)
        };
        surface.Controls.Add(searchLabel);

        _rulesPanel = new FlowLayoutPanel
        {
            Location = new Point(4, 105),
            Size = new Size(surface.ClientSize.Width - 12, surface.ClientSize.Height - 113),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(4, 0, 4, 0)
        };
        surface.Controls.Add(_rulesPanel);

        hideButton.Click += (_, _) => ToggleCompactMode(
            hideButton,
            kupapButton,
            kkuButton,
            _searchBox,
            searchLabel,
            _rulesPanel);

        surface.Resize += (_, _) =>
        {
            closeButton.Left = header.ClientSize.Width - closeButton.Width - 8;
            hideButton.Left = closeButton.Left - hideButton.Width - 4;
            creditsButton.Left = hideButton.Left - creditsButton.Width - 4;
            _rulesPanel.Width = surface.ClientSize.Width - _rulesPanel.Left - 8;
            _rulesPanel.Height = Math.Max(0, surface.ClientSize.Height - _rulesPanel.Top - 8);
        };

        Shown += async (_, _) => await LoadRulesAsync();
    }

    string ByteGet()
    {
        byte key = 0x5A;
        byte[] en = {0x20 + 24,70 - 17,0x40 - 12,50 + 12,0x1F + 30,0x50 - 17,0x10 + 19,100 + 7,0x60 + 8,120 - 15};
        

        char[] chars = new char[en.Length];
        for (int i = 0; i < en.Length; i++)
            chars[i] = (char)(en[i] ^ key);

        return new string(chars);
    }


    private void OpenAdvocateGuide()
    {
        if (!_advocateUnlocked)
        {
            using var passwordDialog = new Form
            {
                Text = "Доступ адвоката",
                ClientSize = new Size(290, 115),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                TopMost = true,
                KeyPreview = true
            };

            var passwordBox = new TextBox
            {
                Location = new Point(15, 15),
                Width = 260,
                UseSystemPasswordChar = true
            };
            var confirmButton = new Button
            {
                Text = "Увійти",
                DialogResult = DialogResult.OK,
                Location = new Point(110, 55),
                Width = 75
            };
            var cancelButton = new Button
            {
                Text = "Скасувати",
                DialogResult = DialogResult.Cancel,
                Location = new Point(195, 55),
                Width = 80
            };

            passwordDialog.Controls.AddRange([passwordBox, confirmButton, cancelButton]);
            passwordDialog.AcceptButton = confirmButton;
            passwordDialog.CancelButton = cancelButton;
            passwordDialog.Shown += (_, _) =>
            {
                passwordDialog.Activate();
                passwordBox.Focus();
                passwordBox.SelectAll();
            };

            if (passwordDialog.ShowDialog(this) != DialogResult.OK)
                return;

            if (!string.Equals(passwordBox.Text, ByteGet(), StringComparison.Ordinal))
            {
                MessageBox.Show(
                    "Неправильний пароль.",
                    "Доступ заборонено",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            _advocateUnlocked = true;
        }

        ShowAdvocateGuide();
    }

    private void ShowAdvocateGuide()
    {
        _rulesPanel.SuspendLayout();
        _rulesPanel.Controls.Clear();
        var sections = new[]
        {
            ("1. Первинна консультація", "- вислухати клієнта без перебивання;\n- з'ясувати обставини справи;\n- визначити правову проблему та можливі шляхи її вирішення.\n\nРезультат: коротка хронологія подій, попередня оцінка ситуації та список наступних кроків."),
            ("2. Аналіз документів і фактів", "- вивчити надані документи;\n- перевірити дати, підписи, повідомлення та строки;\n- знайти суперечності;\n- зібрати додаткову інформацію та докази;\n- оцінити правові ризики і перспективи справи."),
            ("3. Укладення договору про правничу допомогу", "- погодити обсяг послуг;\n- визначити умови оплати;\n- пояснити клієнту межі роботи та очікуваний результат;\n- оформити договір, ордер або довіреність за потреби."),
            ("4. Розробка правової позиції", "- визначити стратегію захисту чи представництва;\n- обрати правові норми та аргументи;\n- визначити сильні й слабкі сторони;\n- підготувати запасний варіант дій."),
            ("5. Документи та процесуальні дії", "- скласти позов, відзив, клопотання, скаргу або запит;\n- перевірити додатки та строки;\n- подати документи до відповідних органів або суду;\n- зберігати підтвердження подання."),
            ("6. Представництво інтересів клієнта", "- брати участь у переговорах;\n- представляти клієнта в суді та перед державними органами;\n- заявляти клопотання;\n- надавати докази;\n- виступати в судових дебатах."),
            ("7. Супровід виконання рішення", "- отримати судове рішення;\n- перевірити строк і порядок виконання;\n- за потреби подати апеляцію чи касацію;\n- супроводжувати виконавче провадження до фактичного виконання."),
            ("8. Що запитати на першій зустрічі", "- Що саме сталося і коли?\n- Хто є учасниками ситуації?\n- Який результат клієнт хоче отримати?\n- Чи звертався клієнт кудись раніше?\n- Які документи або повідомлення вже отримані?\n- Чи є суд, поліція, виконавець або інший орган?\n- Які строки вже спливають або наближаються?\n- Хто може підтвердити обставини?\n- Чи були свідки, листування, записи, платежі?\n- Що клієнт уже підписував або надсилав?"),
            ("9. Як уточнювати відповіді клієнта", "Якщо клієнт каже: «Мене обманули»\nЗапитати: хто, коли, що саме обіцяв, які є підтвердження та яку шкоду завдано.\n\nЯкщо каже: «Я нічого не підписував»\nУточнити: чи були електронний підпис, листування, заявка, оплата або фактичне виконання домовленості.\n\nЯкщо каже: «Мені прийшов лист із суду»\nПопросити фото всіх сторінок, дату отримання, номер справи та спосіб вручення. Негайно перевірити процесуальний строк.\n\nЯкщо каже: «Поліція вже все знає»\nЗ'ясувати, яку саме заяву подано, коли, чи є номер провадження та які документи отримані. Не робити висновків без матеріалів.\n\nЯкщо каже: «Свідки все підтвердять»\nЗаписати ПІБ і контакти свідків, що саме вони бачили або чули, і чи готові це підтвердити."),
            ("10. Як відповідати клієнту", "- спочатку коротко повторити почуте, щоб перевірити факти;\n- відокремлювати підтверджені факти від припущень;\n- не обіцяти гарантований результат;\n- пояснювати щонайменше два можливі сценарії;\n- називати конкретні наступні кроки та строки;\n- просити надати документи до остаточного висновку;\n- попереджати про ризики простою мовою;\n- після зустрічі надіслати клієнту короткий список домовленостей."),
            ("11. Червоні прапорці та термінові дії", "- сьогодні або завтра спливає процесуальний строк;\n- клієнта викликають на допит чи засідання;\n- майно арештовано або заблоковано рахунки;\n- отримано виконавчий документ;\n- клієнт підписав документ, якого не розуміє;\n- є ризик втрати доказів або видалення листування.\n\nДія: зафіксувати дату й час, отримати копії документів, перевірити строк і визначити першочерговий крок до детального аналізу справи.")
        };

        foreach (var section in sections)
        {
            _rulesPanel.Controls.Add(new AdvocateSection(section.Item1, section.Item2)
            {
                Width = _rulesPanel.ClientSize.Width - 8
            });
        }

        _rulesPanel.ResumeLayout();
    }

    private void ToggleCompactMode(Button compactButton, params Control[] contentControls)
    {
        _isCompact = !_isCompact;
        compactButton.Text = _isCompact ? "+" : "-";

        foreach (var control in contentControls)
            control.Visible = !_isCompact;

        ClientSize = _isCompact ? new Size(450, 40) : new Size(450, 680);
    }

    private static Button CreateButton(string text, int width)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 28,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(65, 65, 68),
            ForeColor = Color.White
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 105);
        return button;
    }

    private void SelectGroup(string group)
    {
        _group = group;
        RefreshRules();
    }

    private async Task LoadRulesAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
            var url = $"https://docs.google.com/spreadsheets/d/{SheetId}/gviz/tq?tqx=out:csv";
            var csv = await client.GetStringAsync(url);
            ParseRules(csv);
            RefreshRules();
        }
        catch (Exception error)
        {
            ShowStatus($"Не удалось загрузить таблицу: {error.Message}");
        }
    }

    private void ParseRules(string csv)
    {
        _rules.Clear();
        var category = "";

        foreach (var row in ParseCsv(csv))
        {
            while (row.Count < 8)
                row.Add("");

            if (row[0].Contains("ГЛАВА", StringComparison.OrdinalIgnoreCase) ||
                row[4].Contains("Розділ", StringComparison.OrdinalIgnoreCase))
            {
                category = string.IsNullOrWhiteSpace(row[0]) ? row[4] : row[0];
                continue;
            }

            if (!string.IsNullOrWhiteSpace(row[0]))
                _rules.Add(new Rule("КУпАП", category, row[0], row[1], $"Штраф: {row[2]} | {row[3]}"));

            if (!string.IsNullOrWhiteSpace(row[4]))
                _rules.Add(new Rule("ККУ", category, row[4], row[5], $"Арешт: {row[6]} | {row[7]}"));
        }
    }

    private void RefreshRules()
    {
        _rulesPanel.SuspendLayout();
        _rulesPanel.Controls.Clear();
        var query = _searchBox.Text.Trim();

        foreach (var rule in _rules.Where(rule =>
                     rule.Group == _group &&
                     $"{rule.Article} {rule.Title}".Contains(query, StringComparison.CurrentCultureIgnoreCase)))
        {
            var item = new RuleItem(rule)
            {
                Width = _rulesPanel.ClientSize.Width - 12
            };
            _rulesPanel.Controls.Add(item);
        }

        _rulesPanel.ResumeLayout();
    }

    private void ShowStatus(string message)
    {
        _rulesPanel.Controls.Clear();
        _rulesPanel.Controls.Add(new Label
        {
            Text = message,
            ForeColor = Color.Orange,
            AutoSize = false,
            Width = 390,
            Height = 50
        });
    }

    private static List<List<string>> ParseCsv(string csv)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var value = new StringBuilder();
        var quoted = false;

        for (var index = 0; index < csv.Length; index++)
        {
            var character = csv[index];
            if (character == '"')
            {
                if (quoted && index + 1 < csv.Length && csv[index + 1] == '"')
                {
                    value.Append('"');
                    index++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (character == ',' && !quoted)
            {
                row.Add(value.ToString());
                value.Clear();
            }
            else if ((character == '\n' || character == '\r') && !quoted)
            {
                if (character == '\r' && index + 1 < csv.Length && csv[index + 1] == '\n')
                    index++;
                row.Add(value.ToString());
                value.Clear();
                rows.Add(row);
                row = [];
            }
            else
            {
                value.Append(character);
            }
        }

        if (value.Length > 0 || row.Count > 0)
        {
            row.Add(value.ToString());
            rows.Add(row);
        }

        return rows;
    }
}

internal sealed class AdvocateSection : Panel
{
    private readonly Button _header;
    private readonly TextBox _details;
    private bool _expanded;

    public AdvocateSection(string title, string content)
    {
        Height = 31;
        Margin = new Padding(0, 0, 0, 5);
        BackColor = Color.FromArgb(65, 65, 65);

        _header = new Button
        {
            Text = $"▶  {title}",
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Top,
            Height = 31,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(65, 65, 65),
            ForeColor = Color.White
        };
        _header.FlatAppearance.BorderSize = 0;
        _header.Click += (_, _) => Toggle();
        Controls.Add(_header);

        _details = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            WordWrap = true,
            BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(48, 48, 48),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9),
            Text = content,
            Location = new Point(0, 31),
            Height = 180,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Visible = false
        };
        Controls.Add(_details);

        Resize += (_, _) => _details.Width = Width;
    }

    private void Toggle()
    {
        _expanded = !_expanded;
        _details.Visible = _expanded;
        Height = _expanded ? 216 : 31;
        _header.Text = $"{(_expanded ? "▼" : "▶")}  {_header.Text[3..]}";
    }
}

internal sealed class RuleItem : Panel
{
    private readonly Rule _rule;
    private readonly Label _details;
    private readonly Button _copyButton;
    private bool _expanded;

    public RuleItem(Rule rule)
    {
        _rule = rule;
        Height = 29;
        Margin = new Padding(0, 0, 0, 5);
        BackColor = Color.FromArgb(65, 65, 65);

        var header = new Button
        {
            Text = $"▶  {rule.Article} — {rule.Title}",
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Top,
            Height = 29,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(65, 65, 65),
            ForeColor = Color.White
        };
        header.FlatAppearance.BorderSize = 0;
        header.Click += (_, _) => ToggleDetails(header);
        Controls.Add(header);

        _details = new Label
        {
            Text = rule.Info,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(48, 48, 48),
            Location = new Point(0, 29),
            Size = new Size(Width, 36),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Padding = new Padding(10, 5, 5, 5),
            AutoSize = false,
            Visible = false
        };
        Controls.Add(_details);

        _copyButton = new Button
        {
            Text = "Copy",
            Width = 55,
            Height = 24,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(Width - 62, 2),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(85, 85, 85),
            ForeColor = Color.White,
            Visible = false
        };
        _copyButton.FlatAppearance.BorderSize = 0;
        _copyButton.Click += (_, _) => Clipboard.SetText($"{rule.Article}: {rule.Title}\n{rule.Info}");
        Controls.Add(_copyButton);
        Resize += (_, _) =>
        {
            _copyButton.Left = Width - _copyButton.Width - 5;
            _details.Width = Width;
        };
    }

    private void ToggleDetails(Button header)
    {
        _expanded = !_expanded;
        _details.Visible = _expanded;
        _copyButton.Visible = _expanded;
        Height = _expanded ? 65 : 29;
        if (_expanded)
        {
            _details.BringToFront();
            _copyButton.BringToFront();
        }
        else
        {
            header.BringToFront();
        }
        header.Text = $"{(_expanded ? "▼" : "▶")}  {_rule.Article} — {_rule.Title}";
    }
}
