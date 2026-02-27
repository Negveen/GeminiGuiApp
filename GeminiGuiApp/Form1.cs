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

        public Form1()
        {
            InitializeComponent();
        }

        // 1. Логика кнопки выбора файла
        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    _selectedPath = openFileDialog.FileName;
                    _isFileSelected = true;
                    lblSelectedPath.Text = $"Файл: {_selectedPath}";
                    lblSelectedPath.ForeColor = Color.Green;
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
            btnSend.Enabled = false;

            try
            {
                string response = await RunGeminiCliAsync(userPrompt);

                // Очищаем ответ от логов (убираем всё, что начинается с "(Log:")
                string cleanResponse = response;
                int logIndex = response.IndexOf("\n(Log:");
                if (logIndex > -1)
                {
                    cleanResponse = response.Substring(0, logIndex).Trim();
                }

                rtbOutput.AppendText($"[Gemini]: {cleanResponse}\n\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSend.Enabled = true;
            }
        }

        // 3. Метод-"обертка" для запуска консольной утилиты
        private System.Threading.Tasks.Task<string> RunGeminiCliAsync(string prompt)
        {
            return System.Threading.Tasks.Task.Run(() =>
            {
                Process process = new Process();
                process.StartInfo.FileName = "cmd.exe";

                // 1. САНИТИЗАЦИЯ ВВОДА (Защита от обрыва команды)
                // Убираем кавычки и заменяем все переносы строк на пробелы
                string safePrompt = prompt.Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " ");

                string workingDir = string.Empty;
                string contextInstruction = string.Empty;

                // 2. ЛОГИКА КОНТЕКСТА (Файл или Папка)
                if (!string.IsNullOrEmpty(_selectedPath))
                {
                    if (_isFileSelected)
                    {
                        // Если выбран файл: рабочая папка — это папка файла
                        workingDir = System.IO.Path.GetDirectoryName(_selectedPath);
                        string fileName = System.IO.Path.GetFileName(_selectedPath);

                        // Добавляем скрытую инструкцию для фокуса на конкретном файле
                        contextInstruction = $" [Системно: Сфокусируйся на работе с файлом '{fileName}']";
                    }
                    else
                    {
                        // Если выбрана папка: она и есть рабочая директория
                        workingDir = _selectedPath;
                    }
                }

                // 3. ФОРМИРОВАНИЕ КОМАНДЫ
                // Склеиваем запрос пользователя и системную подсказку (в одну плоскую строку!)
                safePrompt += contextInstruction;

                // Добавляем --yolo для автоматического подтверждения действий (запись, создание и т.д.)
                string command = $"gemini \"{safePrompt}\" --yolo";
                process.StartInfo.Arguments = $"/C \"{command}\"";

                // 4. НАСТРОЙКА РАБОЧЕЙ ДИРЕКТОРИИ
                if (!string.IsNullOrEmpty(workingDir) && System.IO.Directory.Exists(workingDir))
                {
                    process.StartInfo.WorkingDirectory = workingDir;
                }

                // 5. НАСТРОЙКИ СКРЫТОГО ПРОЦЕССА И ПЕРЕХВАТА ПОТОКОВ
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                // UTF8 обязателен, чтобы русские символы не превратились в знаки вопроса
                process.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
                process.StartInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;

                // 6. ЗАПУСК
                process.Start();

                // 7. ЧТЕНИЕ ОТВЕТА (Синхронное ожидание конца)
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                // 8. ОБРАБОТКА ОШИБОК И ВОЗВРАТ
                if (!string.IsNullOrEmpty(error) && process.ExitCode != 0)
                {
                    return $"Ошибка CLI (Код {process.ExitCode}): {error}";
                }

                // Если есть некритичные логи в Error, приклеиваем их в конец
                return output + (string.IsNullOrEmpty(error) ? "" : $"\n(Log: {error})");
            });
        }

        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    _selectedPath = folderDialog.SelectedPath;
                    _isFileSelected = false;
                    lblSelectedPath.Text = $"Папка: {_selectedPath}";
                    lblSelectedPath.ForeColor = Color.Blue;
                }
            }
        }
    }
}