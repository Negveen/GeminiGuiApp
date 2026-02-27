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
        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // ВАША ИДЕЯ: Если мы уже вели диалог в старой папке, зачищаем его!
                    if (!_isFirstRequestInSession)
                    {
                        DeleteLatestSession(); // Убираемся в старой директории
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

        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    // ВАША ИДЕЯ: Если мы уже вели диалог в старой папке, зачищаем его!
                    if (!_isFirstRequestInSession)
                    {
                        DeleteLatestSession(); // Убираемся в старой директории
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

        private void btnClearChat_Click(object sender, EventArgs e)
        {
            // Сначала удаляем сессию с жесткого диска
            if (!_isFirstRequestInSession)
            {
                DeleteLatestSession();
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
        }

        private void DeleteLatestSession()
        {
            try
            {
                // 1. Восстанавливаем рабочую папку, где мы наследили
                string workingDir = string.Empty;
                if (!string.IsNullOrEmpty(_selectedPath))
                {
                    workingDir = _isFileSelected ? System.IO.Path.GetDirectoryName(_selectedPath) : _selectedPath;
                }

                // 2. Читаем список сессий
                Process listProcess = new Process();
                listProcess.StartInfo.FileName = "cmd.exe";
                listProcess.StartInfo.Arguments = "/C \"gemini --list-sessions\"";
                listProcess.StartInfo.UseShellExecute = false;
                listProcess.StartInfo.CreateNoWindow = true;
                listProcess.StartInfo.RedirectStandardOutput = true;

                if (!string.IsNullOrEmpty(workingDir) && System.IO.Directory.Exists(workingDir))
                {
                    listProcess.StartInfo.WorkingDirectory = workingDir;
                }

                listProcess.Start();
                string output = listProcess.StandardOutput.ReadToEnd();
                listProcess.WaitForExit(2000);

                // 3. Ищем последнюю цифру с помощью Regex
                // Шаблон: начало строки (^), возможные пробелы (\s*), захват цифр (\d+), точка (\.)
                var matches = System.Text.RegularExpressions.Regex.Matches(output, @"^\s*(\d+)\.", System.Text.RegularExpressions.RegexOptions.Multiline);

                if (matches.Count > 0)
                {
                    // Берем самую последнюю найденную цифру
                    string lastIndex = matches[matches.Count - 1].Groups[1].Value;

                    // 4. Безжалостно удаляем найденную сессию
                    Process deleteProcess = new Process();
                    deleteProcess.StartInfo.FileName = "cmd.exe";
                    deleteProcess.StartInfo.Arguments = $"/C \"gemini --delete-session {lastIndex}\"";
                    deleteProcess.StartInfo.UseShellExecute = false;
                    deleteProcess.StartInfo.CreateNoWindow = true;

                    if (!string.IsNullOrEmpty(workingDir) && System.IO.Directory.Exists(workingDir))
                    {
                        deleteProcess.StartInfo.WorkingDirectory = workingDir;
                    }

                    deleteProcess.Start();
                    deleteProcess.WaitForExit(1000);
                }
            }
            catch
            {
                // Молча игнорируем ошибки — это фоновая очистка
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Если мы хоть раз обращались к нейросети за время работы окна
            if (!_isFirstRequestInSession)
            {
                DeleteLatestSession();
            }
        }
    }
}