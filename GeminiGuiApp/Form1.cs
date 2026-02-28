using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace GeminiGuiApp
{
    public partial class Form1 : Form
    {
        // Переменная для хранения пути к выбранному файлу        
        private string _selectedPath = string.Empty;
        private bool _isFileSelected = false; // Флаг, чтобы понимать, файл это или папка
        private bool _isFirstRequestInSession = true;

        public Form1()
        {
            InitializeComponent();
            btnClearChat.Enabled = false;
        }

        // 1. Логика кнопки выбора файла
        private async void btnSelectFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // ВАША ИДЕЯ: Если мы уже вели диалог в старой папке, зачищаем его!
                    if (!_isFirstRequestInSession)
                    {
                        // Было: await WipeGeminiTracesAsync();
                        await System.Threading.Tasks.Task.Run(() => WipeGeminiTraces());
                        _isFirstRequestInSession = true; // Сбрасываем память для новой папки
                        rtbOutput.Clear(); // Очищаем экран от старого диалога
                        btnClearChat.Enabled = false;
                    }

                    _selectedPath = openFileDialog.FileName;
                    _isFileSelected = true;
                    lblSelectedPath.Text = $"Файл: {_selectedPath}";
                    lblSelectedPath.ForeColor = Color.Green;
                }
            }
        }

        private async void btnSelectFolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    // ВАША ИДЕЯ: Если мы уже вели диалог в старой папке, зачищаем его!
                    if (!_isFirstRequestInSession)
                    {
                        // Было: await WipeGeminiTracesAsync();
                        await System.Threading.Tasks.Task.Run(() => WipeGeminiTraces());
                        _isFirstRequestInSession = true; // Сбрасываем память для новой папки
                        rtbOutput.Clear(); // Очищаем экран от старого диалога
                        btnClearChat.Enabled = false;
                    }

                    _selectedPath = folderDialog.SelectedPath;
                    _isFileSelected = false;
                    lblSelectedPath.Text = $"Папка: {_selectedPath}";
                    lblSelectedPath.ForeColor = Color.Blue;
                }
            }
        }

        // 2. Логика кнопки отправки запроса
        private async void btnSend_Click(object sender, EventArgs e)
        {
            string userPrompt = txtPrompt.Text.Trim();
            if (string.IsNullOrEmpty(userPrompt)) return;

            txtPrompt.Clear();

            rtbOutput.AppendText($"[Вы]: {userPrompt}\n");
            rtbOutput.AppendText($"[Gemini]:\n");

            btnSend.Enabled = false;
            string originalBtnText = btnSend.Text;
            btnSend.Text = "Думает..."; // Индикатор старта

            // Флаг для отслеживания того, начался ли вывод
            bool isPrintingStarted = false;

            try
            {
                var progress = new Progress<string>(text =>
                {
                    // Как только получаем первую порцию текста, меняем статус (один раз)
                    if (!isPrintingStarted)
                    {
                        btnSend.Text = "Печатает...";
                        isPrintingStarted = true;
                    }

                    rtbOutput.AppendText(text + "\n");
                    rtbOutput.SelectionStart = rtbOutput.Text.Length;
                    rtbOutput.ScrollToCaret();
                });

                await RunGeminiCliStreamingAsync(userPrompt, progress);

                // НОВАЯ СТРОЧКА: Запрос прошел успешно, значит сессия начата!
                _isFirstRequestInSession = false;
                btnClearChat.Enabled = true; // Теперь у нас есть сессия, разрешаем её сбросить!

                rtbOutput.AppendText("\n[Ответ завершен]\n\n");
                rtbOutput.SelectionStart = rtbOutput.Text.Length;
                rtbOutput.ScrollToCaret();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка выполнения", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSend.Enabled = true;
                btnSend.Text = originalBtnText; // Возвращаем исходный текст ("Отправить")
            }
        }

        // 3. Метод-"обертка" для запуска консольной утилиты
        private System.Threading.Tasks.Task RunGeminiCliStreamingAsync(string prompt, IProgress<string> progress)
        {
            return System.Threading.Tasks.Task.Run(() =>
            {
                Process process = new Process();
                process.StartInfo.FileName = "cmd.exe";

                // 1. Санитизация и контекст (всё как было)
                string safePrompt = prompt.Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " ");
                string workingDir = string.Empty;
                string contextInstruction = string.Empty;

                if (!string.IsNullOrEmpty(_selectedPath))
                {
                    if (_isFileSelected)
                    {
                        workingDir = System.IO.Path.GetDirectoryName(_selectedPath);
                        string fileName = System.IO.Path.GetFileName(_selectedPath);
                        contextInstruction = $" [Системно: Сфокусируйся на работе с файлом '{fileName}']";
                    }
                    else
                    {
                        workingDir = _selectedPath;
                    }
                }

                // ЛОГИКА ПАМЯТИ СЕССИИ
                string sessionArg = "";
                if (!_isFirstRequestInSession)
                {
                    // Если это не первый запрос, просим CLI вспомнить прошлый контекст
                    sessionArg = "--resume latest";
                }

                safePrompt += contextInstruction;
                string command = $"gemini \"{safePrompt}\" --yolo {sessionArg}";
                process.StartInfo.Arguments = $"/C \"{command}\"";

                if (!string.IsNullOrEmpty(workingDir) && System.IO.Directory.Exists(workingDir))
                {
                    process.StartInfo.WorkingDirectory = workingDir;
                }

                // 2. Настройки процесса
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
                process.StartInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;

                // 3. ПОДПИСЫВАЕМСЯ НА СОБЫТИЯ ПОТОКОВОГО ВЫВОДА
                process.OutputDataReceived += (sender, args) =>
                {
                    // Если строка не пустая, отправляем её в наш интерфейс
                    if (args.Data != null)
                    {
                        progress.Report(args.Data);
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrWhiteSpace(args.Data))
                    {
                        // Фильтруем назойливые системные сообщения
                        if (args.Data.Contains("YOLO mode is enabled") ||
                            args.Data.Contains("Loaded cached credentials") ||
                            args.Data.Contains("Hook registry initialized"))
                        {
                            return; // Просто молча игнорируем эту строку
                        }

                        // Если это настоящая ошибка, то выводим её
                        progress.Report($"(Log: {args.Data})");
                    }
                };

                // 4. Запускаем процесс
                process.Start();

                // 5. НАЧИНАЕМ АСИНХРОННОЕ ЧТЕНИЕ (Вместо ReadToEnd)
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // 6. Ждем, пока процесс закончит свою работу
                process.WaitForExit();
            });
        }

        private async void btnClearChat_Click(object sender, EventArgs e)
        {
            string originalText = btnClearChat.Text;
            btnClearChat.Text = "Зачистка следов...";
            btnClearChat.Enabled = false;
            // Сначала удаляем сессию с жесткого диска
            if (!_isFirstRequestInSession)
            {
                // Было: await WipeGeminiTracesAsync();
                await System.Threading.Tasks.Task.Run(() => WipeGeminiTraces());
            }
            // 1. Очищаем экран
            rtbOutput.Clear();

            // 2. Сбрасываем память сессии!
            _isFirstRequestInSession = true;
            btnClearChat.Enabled = false; // Сессия сброшена, снова выключаем кнопку

            // (Опционально) Сбрасываем выбранный файл/папку
            _selectedPath = string.Empty;
            lblSelectedPath.Text = "Файл/Папка не выбраны";
            lblSelectedPath.ForeColor = Color.Black;
            btnClearChat.Text = originalText;
        }

        private void WipeGeminiTraces()
        {
            try
            {
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string geminiPath = System.IO.Path.Combine(userProfile, ".gemini");

                string tmpPath = System.IO.Path.Combine(geminiPath, "tmp");
                string historyPath = System.IO.Path.Combine(geminiPath, "history");

                // 1. Сносим папку с историей проектов целиком (тут нам ничего не нужно)
                if (System.IO.Directory.Exists(historyPath))
                {
                    System.IO.Directory.Delete(historyPath, true);
                }

                // 2. Умная "хирургическая" зачистка папки tmp
                if (System.IO.Directory.Exists(tmpPath))
                {
                    // Сначала безжалостно удаляем ВСЕ одиночные файлы внутри tmp (включая logs.json)
                    foreach (string file in System.IO.Directory.GetFiles(tmpPath))
                    {
                        System.IO.File.Delete(file);
                    }

                    // Затем перебираем все папки внутри tmp
                    foreach (string dir in System.IO.Directory.GetDirectories(tmpPath))
                    {
                        // Получаем только имя папки (без полного пути)
                        string dirName = System.IO.Path.GetFileName(dir);

                        // Если это НЕ папка bin (игнорируем регистр на всякий случай), то сносим её!
                        if (!dirName.Equals("bin", StringComparison.OrdinalIgnoreCase))
                        {
                            System.IO.Directory.Delete(dir, true); // true = вместе со всем содержимым
                        }
                    }
                }
            }
            catch
            {
                // Если какой-то лог прямо сейчас намертво занят системой, 
                // просто молча идем дальше, чтобы не крашить программу.
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Если мы хоть раз обращались к нейросети за время работы окна
            if (!_isFirstRequestInSession)
            {
                WipeGeminiTraces(); // Просто вызываем обычный метод
            }
        }
    }
}