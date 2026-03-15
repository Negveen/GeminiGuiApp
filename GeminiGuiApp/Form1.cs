using System;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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

        /// <summary>
        /// Глобальный компонент всплывающей подсказки. 
        /// Используется для отображения длинных названий чатов в списке слева и детализации статистики токенов при наведении на контекстный индикатор.
        /// </summary>
        private ToolTip _chatToolTip = new ToolTip();

        /// <summary>
        /// Хранит индекс элемента в списке чатов (lstChats), над которым в данный момент находится курсор мыши. 
        /// Помогает избежать мерцания и лишних перерисовок ToolTip при движении мыши внутри одного элемента.
        /// </summary>
        private int _lastHoveredIndex = -1;

        /// <summary>
        /// Глобальная настройка: отправлять запрос по Shift+Enter (true) или по обычному Enter (false).
        /// </summary>
        public static bool SendWithShiftEnter = false;

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
            btnResetPath_Click(this, EventArgs.Empty);
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
        /// Главный контроллер отправки запроса.
        /// Выполняет оркестрацию процесса: блокирует UI от двойных кликов, инициализирует новые сессии с умной генерацией заголовка, 
        /// проверяет валидность путей, рассчитывает Дельта-смещения для сохранения контекста при смене директорий,
        /// запускает асинхронную генерацию ответа и в финале парсит JSON-файлы кэша для вывода статистики использованных токенов.
        /// </summary>
        private async void btnSend_Click(object sender, EventArgs e)
        {

            string userText = txtPrompt.Text.Trim();
            if (string.IsNullOrEmpty(userText)) return;

            // Блокируем интерфейс от двойных кликов
            btnSend.Enabled = false;
            lstChats.Enabled = false;
            txtPrompt.Clear();
            rtbOutput.AppendText($"Вы: {userText}\n\nGemini:\n");

            // 1. ИНИЦИАЛИЗАЦИЯ НОВОГО ЧАТА
            bool isNewChat = false;
            if (_currentChat == null)
            {
                isNewChat = true;
                string title = userText;

                // Ищем конец первого предложения или абзаца
                int dotIndex = title.IndexOf('.');
                int newlineIndex = title.IndexOf('\n');

                int cutIndex = -1;
                if (dotIndex > 0 && newlineIndex > 0) cutIndex = Math.Min(dotIndex, newlineIndex);
                else if (dotIndex > 0) cutIndex = dotIndex;
                else if (newlineIndex > 0) cutIndex = newlineIndex;

                // Обрезаем до точки или энтера
                if (cutIndex > 0) title = title.Substring(0, cutIndex);

                // Страховка: если юзер написал 500 символов вообще без точек
                if (title.Length > 60) title = title.Substring(0, 60) + "...";

                string safeFileName = $"chat_{DateTime.Now:yyyyMMdd_HHmmss}.json";

                _currentChat = new ChatSession
                {
                    Id = safeFileName,
                    Title = title.Trim(),
                    FilePath = System.IO.Path.Combine(_chatsDirectory, safeFileName)
                };
                _lastCliWorkingDir = string.Empty;
            }

            // 2. СОХРАНЯЕМ ВОПРОС ПОЛЬЗОВАТЕЛЯ В НАШ ЧИСТЫЙ МОЗГ
            _currentChat.Messages.Add(new ChatMessage { Role = "User", Text = userText });

            // 3. ПОДГОТОВКА К ОТПРАВКЕ И ДЕЛЬТА-СИНХРОНИЗАЦИЯ
            string promptToSend = userText;          
            bool useResume = false;
            // Читаем то, что сейчас реально написано в текстовом поле
            string currentInputPath = txtSelectedPath.Text.Trim();

            if (currentInputPath != _selectedPath)
            {
                if (!string.IsNullOrEmpty(currentInputPath))
                {
                    // Проверяем, существует ли реально такой файл
                    if (System.IO.File.Exists(currentInputPath))
                    {
                        _isFileSelected = true;
                        _selectedPath = currentInputPath;
                    }
                    // Проверяем, существует ли реально такая папка
                    else if (System.IO.Directory.Exists(currentInputPath))
                    {
                        _isFileSelected = false;
                        _selectedPath = currentInputPath;
                    }
                    else
                    {
                        // Если юзер ввел кракозябры или путь с опечаткой - тормозим процесс!
                        MessageBox.Show("Указанный путь не существует. Проверьте правильность ввода.", "Ошибка пути", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        btnSend.Enabled = true;
                        return; // Прерываем отправку запроса
                    }
                }
                else
                {
                    // Если поле очистили
                    _selectedPath = string.Empty;
                    _isFileSelected = false;
                }
            }
            // ---------------------------------

            // Определяем целевую папку (Если пусто - используем песочницу ~/.gemini)
            string targetDir = string.IsNullOrEmpty(_selectedPath)
                ? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini")
                : _selectedPath;

            // Если всё-таки выбран конкретный файл, нам нужна папка, в которой он лежит
            if (_isFileSelected && !string.IsNullOrEmpty(_selectedPath))
            {
                targetDir = System.IO.Path.GetDirectoryName(_selectedPath);
            }

            // ПРОВЕРЯЕМ ЯКОРЬ: Сменили ли мы папку (или загрузили старый чат)?
            if (_lastCliWorkingDir != targetDir)
            {
                // 1. Сколько сообщений папка УЖЕ знает?
                int knownOffset = _currentChat.AnchorOffsets.ContainsKey(targetDir) ? _currentChat.AnchorOffsets[targetDir] : 0;

                // 2. Сколько сообщений она пропустила? (Всего минус 1 текущее минус известные)
                int missedMessagesCount = (_currentChat.Messages.Count - 1) - knownOffset;

                // 3. Были ли мы вообще в этой папке раньше в рамках этого чата?
                bool isFolderFamiliar = _currentChat.AnchorOffsets.ContainsKey(targetDir);

                // ИСПОЛЬЗУЕМ RESUME ТОЛЬКО ЕСЛИ: папка знакома И нет пропущенных сообщений
                if (!isFolderFamiliar || missedMessagesCount > 0)
                {
                    // Формируем Дельту (только если реально есть история)
                    if (missedMessagesCount > 0)
                    {
                        var deltaMessages = _currentChat.Messages.Skip(knownOffset).Take(missedMessagesCount).ToList();
                        string deltaText = string.Join("\n", deltaMessages.Select(m => $"{m.Role}: {m.Text}"));
                        promptToSend = $"[Системно: Пока мы не общались в этой рабочей директории, контекст нашего диалога был таким:\n{deltaText}\nКонец контекста.]\nМой текущий запрос: {userText}";
                    }

                    useResume = false; // Начинаем новую нативную сессию CLI!
                }
                else
                {
                    useResume = true; // Папка знакома и полностью актуальна
                }

                _lastCliWorkingDir = targetDir; // Бросаем якорь
            }
            else
            {
                // Папка не менялась с прошлого запроса
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
        

            try
            {
                string tmpDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini", "tmp");
                if (System.IO.Directory.Exists(tmpDir))
                {
                    // Получаем все .json файлы сессий, исключая logs.json. 
                    // GetLastWriteTime гарантирует, что мы возьмем файл, который ИЗМЕНИЛСЯ последним.
                    var sessionFiles = System.IO.Directory.GetFiles(tmpDir, "*.json", System.IO.SearchOption.AllDirectories)
                                          .Where(f => System.IO.Path.GetFileName(f) != "logs.json");

                    string latestSessionPath = sessionFiles.OrderByDescending(f => System.IO.File.GetLastWriteTime(f)).FirstOrDefault();

                    if (!string.IsNullOrEmpty(latestSessionPath))
                    {
                        string fileContent = System.IO.File.ReadAllText(latestSessionPath);

                        var matchTokensBlock = System.Text.RegularExpressions.Regex.Match(fileContent, @"\""tokens\""\s*:\s*\{([^}]+)\}");
                        if (matchTokensBlock.Success)
                        {
                            string tokensData = matchTokensBlock.Groups[1].Value;

                            var matchTotal = System.Text.RegularExpressions.Regex.Match(tokensData, @"\""total\""\s*:\s*(\d+)");
                            var matchInput = System.Text.RegularExpressions.Regex.Match(tokensData, @"\""input\""\s*:\s*(\d+)");
                            var matchOutput = System.Text.RegularExpressions.Regex.Match(tokensData, @"\""output\""\s*:\s*(\d+)");

                            if (matchTotal.Success)
                            {
                                long total = long.Parse(matchTotal.Groups[1].Value);
                                double maxContext = 1000000.0;
                                double percentRaw = (total / maxContext) * 100;
                                string percentDisplay = (percentRaw > 0 && percentRaw < 1) ? "< 1" : Math.Round(percentRaw).ToString();
                                lblContextUsage.Text = $"Контекст: {percentDisplay}%";
                                lblContextUsage.ForeColor = percentRaw > 80 ? Color.DarkRed : Color.DarkSlateGray;

                                if (matchInput.Success && matchOutput.Success)
                                {
                                    long input = long.Parse(matchInput.Groups[1].Value);
                                    long output = long.Parse(matchOutput.Groups[1].Value);

                                    string tooltipText = $"Всего токенов: {total:N0} из 1 000 000\n" +
                                                         $"Входящие (Промпт): {input:N0}\n" +
                                                         $"Исходящие (Ответ): {output:N0}";

                                    _chatToolTip.SetToolTip(lblContextUsage, tooltipText);
                                }
                                else _chatToolTip.SetToolTip(lblContextUsage, $"Токены: {total:N0}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка чтения токенов: " + ex.Message);
            }
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
            lstChats.Enabled = true;
        }

        /// <summary>
        /// Асинхронно запускает процесс Gemini CLI в указанной директории с поддержкой потокового чтения.
        /// Перехватывает каналы STDOUT (успешные ответы) и STDERR (системные ошибки), фильтрует технический мусор 
        /// от CLI в реальном времени, безопасно обновляет интерфейс чата и сохраняет итоговый ответ в файл истории.
        /// </summary>
        /// <param name="prompt">Сформированный текст запроса (включая системные Дельта-вставки, если необходимо).</param>
        /// <param name="WorkingDirectory">Целевая папка для выполнения команды (якорь).</param>
        /// <param name="useResume">Флаг, указывающий, нужно ли продолжать предыдущую сессию (--resume latest) или начать новую.</param>
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
                           
                            if (!e.Data.Contains("YOLO mode") && !e.Data.Contains("credentials") &&
                                !e.Data.Contains("Loaded cached credentials") &&
                            !e.Data.Contains("Detected terminal background color"))
                            {
                                fullResponse.AppendLine(e.Data);
                                Invoke(new Action(() =>
                                {
                                    rtbOutput.AppendText(e.Data + Environment.NewLine);
                                    rtbOutput.ScrollToCaret();
                                }));
                            }
                        }
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            // Игнорируем тот же системный мусор, так как CLI часто пишет логи в поток ошибок
                            if (!e.Data.Contains("YOLO mode is enabled") &&
                                !e.Data.Contains("Loaded cached credentials") &&
                                !e.Data.Contains("Detected terminal background color") &&
                                !e.Data.Contains("/stats model") && // Прячем нашу техническую команду
                                !e.Data.Contains("last_stats.json"))
                            {
                                string errorMsg = "[Системное сообщение CLI]: " + e.Data;
                                fullResponse.AppendLine(errorMsg);

                                Invoke(new Action(() =>
                                {
                                    rtbOutput.AppendText(errorMsg + Environment.NewLine);
                                    rtbOutput.ScrollToCaret();
                                }));
                            }
                        }
                    };
                    // Запускаем процесс и начинаем чтение
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

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

                    txtSelectedPath.Text = _selectedPath; // Выводим в TextBox
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

                    txtSelectedPath.Text = _selectedPath; // Выводим в TextBox
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

        /// <summary>
        /// Обработчик кнопки сброса пути (❌).
        /// Очищает выбранный путь и возвращает рабочую директорию программы к стандартной "песочнице" (~/.gemini).
        /// </summary>
        private void btnResetPath_Click(object sender, EventArgs e)
        {
            string defaultDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini");
            // Сбрасываем переменные
            _selectedPath = defaultDir;
            _isFileSelected = false;
            txtSelectedPath.Text = defaultDir;
        }

        /// <summary>
        /// Динамическая валидация "умной адресной строки". 
        /// Изменяет цвет текста "на лету": зеленый для файлов, синий для папок, красный для несуществующих путей и стандартный при пустом поле.
        /// </summary>
        private void txtSelectedPath_TextChanged(object sender, EventArgs e)
        {
            string path = txtSelectedPath.Text.Trim();

            if (string.IsNullOrEmpty(path))
            {
                txtSelectedPath.ForeColor = Color.Black; // Путь сброшен
            }
            else if (System.IO.File.Exists(path))
            {
                txtSelectedPath.ForeColor = Color.DarkGreen; // Это точный файл
            }
            else if (System.IO.Directory.Exists(path))
            {
                txtSelectedPath.ForeColor = Color.Blue; // Это папка
            }
            else
            {
                txtSelectedPath.ForeColor = Color.Red; // Путь с опечаткой или не существует
            }
        }

        /// <summary>
        /// Обработчик движения мыши над списком чатов.
        /// Вычисляет элемент под курсором и выводит полное, необрезанное название чата во всплывающую подсказку.
        /// </summary>
        private void lstChats_MouseMove(object sender, MouseEventArgs e)
        {
            int index = lstChats.IndexFromPoint(e.Location);
            // Если мышка над элементом и это новый элемент
            if (index != -1 && index != _lastHoveredIndex)
            {
                _lastHoveredIndex = index;
                ChatSession hoveredChat = (ChatSession)lstChats.Items[index];
                _chatToolTip.SetToolTip(lstChats, hoveredChat.Title); // Показываем полный Title
            }
            // Если мышка ушла в пустую область
            else if (index == -1 && _lastHoveredIndex != -1)
            {
                _lastHoveredIndex = -1;
                _chatToolTip.SetToolTip(lstChats, ""); // Прячем подсказку
            }
        }

        /// <summary>
        /// Перехватчик нажатия клавиш в основном поле ввода промпта.
        /// Реализует логику отправки сообщения по комбинации Enter или Shift+Enter в зависимости от глобальных настроек пользователя.
        /// </summary>
        private void txtPrompt_KeyDown(object sender, KeyEventArgs e)
        {
            bool isSendAction = SendWithShiftEnter
         ? (e.KeyCode == Keys.Enter && e.Shift)  // Если включена настройка: ждем Shift + Enter
         : (e.KeyCode == Keys.Enter && !e.Shift); // По умолчанию: ждем просто Enter (без Shift)

            if (isSendAction)
            {
                e.SuppressKeyPress = true; // Глушим системный звук "дзинь" и запрещаем перенос строки

                if (btnSend.Enabled)
                {
                    btnSend_Click(this, EventArgs.Empty);
                }
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