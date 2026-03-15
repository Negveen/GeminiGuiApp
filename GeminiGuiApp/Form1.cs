using System;
using System.Diagnostics;
using System.Drawing;
using System.Text.Json;
using System.Windows.Forms;


namespace GeminiGuiApp
{
    public partial class Form1 : Form
    {

        // ==========================================
        // 1. ГЛОБАЛЬНЫЕ ПЕРЕМЕННЫЕ ФОРМЫ
        // ==========================================

        /// <summary>
        /// Абсолютный путь к директории, где хранятся все сохраненные JSON-файлы чатов (наш "Сейф").
        /// </summary>
        private string _chatsDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini", "MyGeminiChats");

        /// <summary>
        /// Текущий путь к выбранной пользователем папке или конкретному файлу.
        /// </summary>
        private string _selectedPath = string.Empty;

        /// <summary>
        /// Флаг, указывающий, что в _selectedPath хранится путь к конкретному файлу, а не к директории.
        /// </summary>
        private bool _isFileSelected = false;

        /// <summary>
        /// "Якорь" памяти. Хранит путь к папке, в которой была запущена последняя нативная сессия CLI.
        /// Используется для вычисления необходимости дельта-синхронизации контекста.
        /// </summary>
        private string _lastCliWorkingDir = string.Empty;

        /// <summary>
        /// Текущий активный Воркспейс. Если равен null, значит пользователь находится в режиме начала нового диалога.
        /// </summary>
        private ChatSession _currentChat = null;

        /// <summary>
        /// Хранит сессию, по которой пользователь кликнул правой кнопкой мыши. 
        /// Позволяет управлять фоновыми чатами (удалять/переименовывать) без их загрузки на экран.
        /// </summary>
        private ChatSession _contextMenuTarget = null;

        // ==========================================
        // 2. КОНСТРУКТОР
        // ==========================================

        public Form1()
        {
            InitializeComponent();
        }

        // ==========================================
        // 3. ВНУТРЕННИЕ МЕТОДЫ
        // ==========================================       

        /// <summary>
        /// Вызывается при запуске программы. Инициализирует рабочую среду (создает папки при необходимости)
        /// и загружает историю предыдущих сессий в левую навигационную панель.
        /// </summary>
        private void Form1_Load(object sender, EventArgs e)
        {
            LoadChatsToList();
        }

        /// <summary>
        /// Универсальный метод сохранения. Сериализует переданный объект сессии (воркспейса) 
        /// и сохраняет его в JSON-файл на диске. Позволяет сохранять как активный чат, 
        /// так и фоновые чаты, с которыми пользователь взаимодействует через контекстное меню.
        /// </summary>
        private void SaveChatSession(ChatSession session)
        {
            if (session == null) return;
            try
            {
                if (!System.IO.Directory.Exists(_chatsDirectory))
                    System.IO.Directory.CreateDirectory(_chatsDirectory);

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    // Вот эта строчка отключает параноидальное кодирование символов
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                string jsonText = JsonSerializer.Serialize(session, options);
                System.IO.File.WriteAllText(session.FilePath, jsonText);
            }
            catch { /* Игнорируем ошибки доступа */ }
        }

        /// <summary>
        /// Обертка для быстрого сохранения текущего активного чата (_currentChat). 
        /// Вызывает универсальный метод SaveChatSession, ничего не ломая в логике.
        /// </summary>
        private void SaveCurrentChat() => SaveChatSession(_currentChat);

        /// <summary>
        /// Читает все сохраненные JSON-файлы из директории Сейфа, десериализует их в объекты ChatSession.
        /// Выполняет умную сортировку: сначала выводит закрепленные чаты, 
        /// а затем сортирует остальные по дате последнего изменения (самые свежие сверху).
        /// </summary>
        private void LoadChatsToList()
        {
            string currentChatId = _currentChat?.Id;
            lstChats.Items.Clear();
            if (!System.IO.Directory.Exists(_chatsDirectory)) return;

            string[] chatFiles = System.IO.Directory.GetFiles(_chatsDirectory, "*.json");
            List<ChatSession> sessions = new List<ChatSession>();

            // Читаем все файлы
            foreach (string file in chatFiles)
            {
                try
                {
                    string json = System.IO.File.ReadAllText(file);
                    ChatSession session = JsonSerializer.Deserialize<ChatSession>(json);
                    if (session != null)
                    {
                        session.FilePath = file; // Обновляем путь на всякий случай
                        sessions.Add(session);
                    }
                }
                catch { /* Игнорируем битые файлы */ }
            }

            // МАГИЯ СОРТИРОВКИ: Сначала закрепленные (IsPinned == true), затем самые новые по дате файла
            var sortedSessions = sessions
                .OrderByDescending(s => s.IsPinned)
                .ThenByDescending(s => System.IO.File.GetLastWriteTime(s.FilePath))
                .ToList();

            foreach (var session in sortedSessions)
            {
                lstChats.Items.Add(session);
                // Восстанавливаем выделение
                if (session.Id == currentChatId) lstChats.SelectedItem = session;
            }
        }

        /// <summary>
        /// Главный контроллер логики ("Мозг"). 
        /// Создает новые сессии, вычисляет "Дельту" (какую часть истории нужно передать ИИ 
        /// при смене папки), формирует финальный текстовый запрос, вызывает CLI 
        /// и инициирует сохранение полученного ответа в нашу базу.
        /// </summary>
        private async void btnSend_Click(object sender, EventArgs e)
        {
            string userText = txtPrompt.Text.Trim();
            if (string.IsNullOrEmpty(userText)) return;

            // Блокируем интерфейс от двойных кликов
            btnSend.Enabled = false;
            txtPrompt.Clear();
            rtbOutput.AppendText($"Вы: {userText}\n\nGemini:\n");

            // 1. ИНИЦИАЛИЗАЦИЯ НОВОГО ЧАТА (если нужно)
            bool isNewChat = false;
            if (_currentChat == null)
            {
                isNewChat = true;
                string title = userText.Length > 20 ? userText.Substring(0, 20) + "..." : userText;
                foreach (char c in System.IO.Path.GetInvalidFileNameChars()) title = title.Replace(c, '_');

                string fileName = $"chat_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                _currentChat = new ChatSession
                {
                    Id = title,
                    Title = title,
                    FilePath = System.IO.Path.Combine(_chatsDirectory, fileName)
                };
                _lastCliWorkingDir = string.Empty; // Сбрасываем якорь
            }

            // 2. СОХРАНЯЕМ ВОПРОС ПОЛЬЗОВАТЕЛЯ В НАШ ЧИСТЫЙ МОЗГ
            _currentChat.Messages.Add(new ChatMessage { Role = "User", Text = userText });

            // 3. ПОДГОТОВКА К ОТПРАВКЕ И ДЕЛЬТА-СИНХРОНИЗАЦИЯ
            string promptToSend = userText;
            bool useResume = false;

            // Определяем целевую папку 
            // Если папка не выбрана, используем безопасную песочницу ~/.gemini
            string targetDir = string.IsNullOrEmpty(_selectedPath)
                ? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini")
                : _selectedPath;
            if (_isFileSelected) targetDir = System.IO.Path.GetDirectoryName(_selectedPath); // Если выбран файл, берем его папку

            // ПРОВЕРЯЕМ ЯКОРЬ: Сменили ли мы папку (или загрузили старый чат)?
            if (_lastCliWorkingDir != targetDir)
            {
                int knownOffset = 0;
                if (_currentChat.AnchorOffsets.ContainsKey(targetDir))
                {
                    knownOffset = _currentChat.AnchorOffsets[targetDir];
                }

                // ВЫСЧИТЫВАЕМ: Сколько сообщений из прошлого эта папка пропустила?
                // (Всего сообщений минус 1 текущее минус те, что папка уже знает)
                int missedMessagesCount = (_currentChat.Messages.Count - 1) - knownOffset;

                if (missedMessagesCount > 0)
                {
                    // Берем ТОЛЬКО пропущенные сообщения
                    var deltaMessages = _currentChat.Messages.Skip(knownOffset).Take(missedMessagesCount).ToList();
                    string deltaText = string.Join("\n", deltaMessages.Select(m => $"{m.Role}: {m.Text}"));

                    promptToSend = $"[Системно: Пока мы не общались в этой рабочей директории, контекст нашего диалога был таким:\n{deltaText}\nКонец контекста.]\nМой текущий запрос: {userText}";

                    useResume = false; // Начинаем новую нативную сессию, подмешивая историю
                }
                else
                {
                    // МАГИЯ: Эта папка уже знает ВСЮ историю до текущего момента! 
                    // Значит нативная сессия CLI тут абсолютно актуальна.
                    useResume = true;
                }

                _lastCliWorkingDir = targetDir; // Бросаем новый якорь
            }
            else
            {
                // Папка не менялась, спокойно продолжаем
                useResume = true;
            }

            // Добавляем инструкцию для конкретного файла (если выбран файл, а не папка)
            if (_isFileSelected && !string.IsNullOrEmpty(_selectedPath))
            {
                promptToSend = $"[Системно: Работай конкретно с этим файлом: {_selectedPath}] " + promptToSend;
            }

            // 4. ЭКРАНИРУЕМ И ЗАПУСКАЕМ CLI
            string safePrompt = promptToSend.Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " ");

            // Вызываем CLI. Теперь метод должен возвращать полученный текст ответа!
            string aiResponse = await RunGeminiCliStreamingAsync(safePrompt, targetDir, useResume);

            // 5. СОХРАНЯЕМ ОТВЕТ ИИ И ЗАПИСЫВАЕМ СМЕЩЕНИЕ
            if (!string.IsNullOrEmpty(aiResponse))
            {
                _currentChat.Messages.Add(new ChatMessage { Role = "Gemini", Text = aiResponse });

                // Магия: теперь эта папка знает ВСЕ сообщения до текущего момента!
                _currentChat.AnchorOffsets[targetDir] = _currentChat.Messages.Count;

                SaveCurrentChat(); // Сохраняем наш чистый JSON на диск

                if (isNewChat) LoadChatsToList(); // Если это был первый запрос, обновляем меню слева
            }

            rtbOutput.AppendText("\n\n[Ответ завершен]\n");
            btnSend.Enabled = true;
        }

        /// <summary>
        /// Низкоуровневая обертка для работы с процессом cmd.exe и Gemini CLI.
        /// Запускает нейросеть в указанной папке, перехватывает потоковый вывод 
        /// для отображения в реальном времени, отсеивает системный мусор (логи) 
        /// и возвращает чистый текст ответа модели.
        /// </summary>
        private async System.Threading.Tasks.Task<string> RunGeminiCliStreamingAsync(string prompt, string targetDir, bool useResume)
        {
            // Сюда мы будем собирать ответ ИИ для сохранения в историю
            System.Text.StringBuilder fullResponse = new System.Text.StringBuilder();

            await System.Threading.Tasks.Task.Run(() =>
            {
                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();

                // Настраиваем команду запуска
                psi.FileName = "cmd.exe";

                // Формируем аргументы. Добавляем --yolo (если нужно) и --resume по условию
                string resumeFlag = useResume ? "--resume latest " : "";
                psi.Arguments = $"/c chcp 65001 > nul && gemini \"{prompt}\" {resumeFlag}--yolo";

                // Указываем ту самую папку, откуда CLI должен работать
                psi.WorkingDirectory = targetDir;

                // Настройки для скрытой работы и перехвата текста
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;
                psi.StandardOutputEncoding = System.Text.Encoding.UTF8;

                using (System.Diagnostics.Process process = new System.Diagnostics.Process { StartInfo = psi })
                {
                    // Перехватываем каждую новую строку, которую выдает консоль
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            // Игнорируем технические сообщения от самого CLI в истории
                            if (!e.Data.Contains("YOLO mode is enabled") &&
                                !e.Data.Contains("Loaded cached credentials") &&
                                !e.Data.Contains("Detected terminal background color"))
                            {
                                fullResponse.AppendLine(e.Data); // Записываем в память
                            }

                            // Выводим текст на экран в реальном времени (Invoke нужен для работы из фонового потока)
                            Invoke(new Action(() =>
                            {
                                rtbOutput.AppendText(e.Data + Environment.NewLine);
                                rtbOutput.ScrollToCaret(); // Автоматическая прокрутка вниз
                            }));
                        }
                    };

                    // Запускаем процесс и начинаем чтение
                    process.Start();
                    process.BeginOutputReadLine();

                    // Ждем, пока ИИ не закончит отвечать
                    process.WaitForExit();
                }
            });

            // Возвращаем чистый текст ответа без лишних пробелов по краям
            return fullResponse.ToString().Trim();
        }

        /// <summary>
        /// Открывает диалог выбора папки. Устанавливает выбранный путь в качестве рабочей директории 
        /// для будущих запросов к CLI, не прерывая текущую сессию общения.
        /// </summary>
        private async void btnSelectFolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    _selectedPath = folderDialog.SelectedPath;
                    _isFileSelected = false; // Помечаем, что выбрана именно папка

                    lblSelectedPath.Text = $"Папка: {_selectedPath}";
                    lblSelectedPath.ForeColor = Color.Blue;
                }
            }
        }

        /// <summary>
        /// Открывает диалог выбора файла. Указывает нейросети сфокусироваться на анализе 
        /// или редактировании конкретного документа в рамках текущего контекста.
        /// </summary>
        private async void btnSelectFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog fileDialog = new OpenFileDialog())
            {
                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    _selectedPath = fileDialog.FileName;
                    _isFileSelected = true; // Помечаем, что выбран конкретный файл

                    lblSelectedPath.Text = $"Файл: {_selectedPath}";
                    lblSelectedPath.ForeColor = Color.DarkGreen;
                }
            }
        }

        /// <summary>
        /// Сбрасывает визуальный интерфейс и очищает активный контекст "Мозга", 
        /// подготавливая программу к созданию новой, независимой истории диалога.
        /// </summary>
        private void btnNewChat_Click(object sender, EventArgs e)
        {
            // 1. Очищаем визуальную часть
            rtbOutput.Clear();
            txtPrompt.Clear();

            // 2. Сбрасываем "Мозг" программы
            _currentChat = null;

            // 3. Сбрасываем Якорь (чтобы следующий запрос точно не использовал чужой --resume)
            _lastCliWorkingDir = string.Empty;

            // 4. Снимаем выделение в списке слева (чтобы визуально показать, что старый чат закрыт)
            lstChats.ClearSelected();

            // Фокус на поле ввода, чтобы можно было сразу печатать
            txtPrompt.Focus();
        }

        /// <summary>
        /// Открывает модальное окно настроек приложения (управление системным кэшем CLI и прочее).
        /// </summary>
        private void btnSettings_Click(object sender, EventArgs e)
        {
            using (SettingsForm settingsForm = new SettingsForm())
            {
                settingsForm.ShowDialog();
            }
        }

        /// <summary>
        /// Обработчик выбора чата в боковой панели. 
        /// Загружает выбранную сессию в "Мозг" программы (_currentChat), сбрасывает Якорь CLI 
        /// (чтобы принудительно передать историю в новую папку) и выводит текст диалога на экран.
        /// </summary>
        private void lstChats_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Если ничего не выбрано (например, при очистке списка), выходим
            if (lstChats.SelectedItem == null) return;

            // Получаем объект чата, по которому кликнули
            ChatSession selectedSession = (ChatSession)lstChats.SelectedItem;

            // 1. Загружаем этот чат в "Мозг" программы
            _currentChat = selectedSession;

            // 2. СБРАСЫВАЕМ ЯКОРЬ! 
            // Это критически важно: так как мы загрузили старый чат, нативная сессия CLI в папке tmp 
            // скорее всего уже затерта другими чатами. Сброс якоря заставит программу 
            // при следующем запросе передать всю Дельту истории в новую нативную сессию.
            _lastCliWorkingDir = string.Empty;

            // 3. Выводим красивую историю на экран
            rtbOutput.Clear();
            foreach (var msg in _currentChat.Messages)
            {
                string roleName = msg.Role == "User" ? "Вы" : "Gemini";
                rtbOutput.AppendText($"{roleName}: {msg.Text}\n\n");
            }

            // Прокручиваем в самый низ
            rtbOutput.ScrollToCaret();
        }

        /// <summary>
        /// Перехватывает клик правой кнопкой мыши по списку чатов. 
        /// Определяет, над каким элементом находится курсор, и запоминает его в _contextMenuTarget 
        /// для работы контекстного меню, НЕ сбрасывая при этом текущий рабочий контекст пользователя.
        /// </summary>
        private void lstChats_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                int index = lstChats.IndexFromPoint(e.Location);
                if (index != ListBox.NoMatches)
                {
                    // Просто запоминаем цель для контекстного меню, НЕ меняя SelectedIndex!
                    _contextMenuTarget = (ChatSession)lstChats.Items[index];
                }
                else
                {
                    // Кликнули в пустое место
                    _contextMenuTarget = null;
                }
            }
        }

        /// <summary>
        /// Обработчик кнопки "Закрепить/Открепить" в контекстном меню. 
        /// Инвертирует статус IsPinned у целевого фонового чата, сохраняет изменения 
        /// и заставляет боковую панель перерисоваться, чтобы чат улетел наверх (или вернулся вниз).
        /// </summary>
        private void tsmPin_Click(object sender, EventArgs e)
        {
            if (_contextMenuTarget == null) return;

            _contextMenuTarget.IsPinned = !_contextMenuTarget.IsPinned;

            SaveChatSession(_contextMenuTarget); // Сохраняем фоновый чат!
            LoadChatsToList();
        }

        /// <summary>
        /// Обработчик кнопки "Переименовать" в контекстном меню. 
        /// Создает "на лету" диалоговое окно для ввода нового имени фонового чата. 
        /// При подтверждении обновляет объект, перезаписывает JSON-файл и обновляет список слева.
        /// </summary>
        private void tsmRename_Click(object sender, EventArgs e)
        {
            if (_contextMenuTarget == null) return;

            Form prompt = new Form()
            {
                Width = 400,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Переименование чата",
                StartPosition = FormStartPosition.CenterParent
            };
            TextBox textBox = new TextBox() { Left = 20, Top = 20, Width = 340, Text = _contextMenuTarget.Title };
            Button confirmation = new Button() { Text = "Сохранить", Left = 260, Width = 100, Top = 60, DialogResult = DialogResult.OK };

            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.AcceptButton = confirmation;

            if (prompt.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                _contextMenuTarget.Title = textBox.Text.Trim();
                SaveChatSession(_contextMenuTarget); // Сохраняем фоновый чат
                LoadChatsToList();
            }
        }

        /// <summary>
        /// Обработчик кнопки "Удалить" в контекстном меню. 
        /// Запрашивает подтверждение и безвозвратно удаляет JSON-файл целевого чата с диска. 
        /// Умная защита: если пользователь удаляет тот чат, который сейчас открыт на экране, 
        /// программа автоматически очищает интерфейс и сбрасывает "Мозг".
        /// </summary>
        private void tsmDelete_Click(object sender, EventArgs e)
        {
            if (_contextMenuTarget == null) return;

            var result = MessageBox.Show($"Вы точно хотите удалить диалог '{_contextMenuTarget.Title}'?\nЭто действие необратимо.",
                "Удаление чата", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    System.IO.File.Delete(_contextMenuTarget.FilePath);

                    // ВАЖНО: Если мы удалили ТОТ ЖЕ чат, что сейчас открыт - очищаем экран
                    if (_currentChat != null && _currentChat.Id == _contextMenuTarget.Id)
                    {
                        rtbOutput.Clear();
                        _currentChat = null;
                        _lastCliWorkingDir = string.Empty;
                    }

                    LoadChatsToList();
                }
                catch (Exception ex) { MessageBox.Show($"Ошибка удаления: {ex.Message}"); }
            }
        }

    }

    // ==========================================
    // 4. КЛАССЫ ДАННЫХ 
    // ==========================================

    /// <summary>
    /// Модель данных, представляющая одно сообщение в истории диалога.
    /// </summary>
    public class ChatMessage
    {
        /// <summary>
        /// Роль отправителя: "User" (пользователь) или "Gemini" (нейросеть).
        /// </summary>
        public string Role { get; set; }

        public string Text { get; set; }
    }

    /// <summary>
    /// Модель данных Воркспейса. Хранит полную, очищенную историю диалога 
    /// и управляет "памятью" папок для бесшовного переключения между директориями.
    /// </summary>
    public class ChatSession
    {
        public string Id { get; set; }

        public string Title { get; set; }

        /// <summary>
        /// Абсолютный путь к JSON-файлу, в котором сохранен данный воркспейс.
        /// </summary>
        public string FilePath { get; set; }

        public bool IsPinned { get; set; } = false;

        /// <summary>
        /// Полный список всех сообщений в рамках данного чата.
        /// </summary>
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

        /// <summary>
        /// Словарь смещений (Дельта-память). 
        /// Ключ - абсолютный путь к папке. Значение - количество сообщений, которое CLI уже "знает" в этой папке.
        /// </summary>
        public Dictionary<string, int> AnchorOffsets { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Переопределенный метод приведения объекта к строке. 
        /// Используется элементом ListBox для отображения названия чата. 
        /// Если чат закреплен (IsPinned), добавляет визуальный маркер (эмодзи булавки).
        /// </summary>
        public override string ToString() => IsPinned ? $"📌 {Title}" : Title;
    }

}